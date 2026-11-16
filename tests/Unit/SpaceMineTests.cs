using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Simulation.Replay;
using RogueTrader.Trading;
using RogueTrader.UnitTests.Persistence;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F038 space mines with the RT-F088 Plasma Mine. The simulation tests use a NEW fixture mine whose numbers are the derived
/// Plasma Mine's (proximity 56, blast 100, drain 750 to 0, damage 20 to 5, 60 s, 1.5 s, 3 live); the original-data tests check
/// those numbers against the pinned ITM row. The default test ship starts at (0, 0) at rest.
/// </summary>
public sealed class SpaceMineTests
{
    private const string MineItem = "102";

    private static MineDefinition Mine(float armSeconds = 0, float lifetimeSeconds = 60) => new()
    {
        Id = MineItem,
        Name = "Plasma Mine",
        ItemId = MineItem,
        ProximityRadius = 56,
        BlastRadius = 100,
        ShieldDrainInner = 750,
        ShieldDrainOuter = 0,
        DamageInner = 20,
        DamageOuter = 5,
        DamageType = DamageType.Energy,
        ArmSeconds = armSeconds,
        LifetimeSeconds = lifetimeSeconds,
        DropIntervalSeconds = 1.5f,
        MaxLivePerPlayer = 3,
    };

    private static CargoCatalog Cargo() =>
        new(1_000, [new CargoItemDefinition(MineItem, 0) { MaxQuantity = 10, KeptOnDestruction = true }, new CargoItemDefinition("ore", 10)]);

    private static WorldSimulation World(int mines = 5, MineDefinition? mine = null, params TargetDefinition[] targets) =>
        new(TestWorld.Catalog(sector: new SectorDefinition { Targets = targets }), Cargo(),
            new Dictionary<string, int> { [MineItem] = mines }, playerMine: mine ?? Mine());

    private static readonly ShipInput Lay = new(0, 0, false, LayMine: true);

    [Fact]
    public void LayingTakesOneMineFromTheHoldAndRecordsItAsANegativePickup()
    {
        var world = World(mines: 2);
        world.Tick(Lay);
        Assert.Single(world.Mines);
        Assert.Equal(Vector2.Zero, world.Mines[0].Position);
        Assert.Equal(1, world.Cargo.Quantity(MineItem));
        var use = Assert.Single(world.Pickups);
        Assert.Equal((MineItem, -1, "mine:1"), (use.ItemId, use.Quantity, use.PileId));

        // Nothing is laid with an empty hold, and a world with no mine lays nothing at all.
        TestWorld.Tick(world, 200, Lay);
        Assert.Equal(2, world.MinesLaid);
        Assert.Equal(0, world.Cargo.Quantity(MineItem));
        var unarmed = new WorldSimulation(TestWorld.Catalog(), Cargo(), new Dictionary<string, int> { [MineItem] = 5 });
        unarmed.Tick(Lay);
        Assert.Empty(unarmed.Mines);
        Assert.Equal(5, unarmed.Cargo.Quantity(MineItem));
    }

    [Fact]
    public void MinesAreLaidOncePerDropIntervalAndAtMostThreeLieInSpace()
    {
        var world = World(mines: 10);
        world.Tick(Lay);
        TestWorld.Tick(world, 89, Lay); // 1.5 s = 90 ticks
        Assert.Single(world.Mines);
        world.Tick(Lay);
        Assert.Equal(2, world.Mines.Count);
        TestWorld.Tick(world, 600, Lay);
        Assert.Equal(3, world.Mines.Count);
        Assert.Equal(7, world.Cargo.Quantity(MineItem));
    }

    [Fact]
    public void AShipComingWithinTheProximityRadiusSetsTheMineOffAndTheBlastFallsOffWithDistance()
    {
        // Target edge 60 - 20 = 40 from the mine: inside the 56 proximity radius. At 60 from the centre the blast is 60% out:
        // drain 750 - 0.6 * 750 = 300, damage 20 - 0.6 * 15 = 11. The 40-point shield empties, so the hull takes the damage.
        var world = World(targets: new TargetDefinition { Id = "near", X = 0, Y = -60, Radius = 20, Hull = 100, Shield = 40 });
        world.Tick(Lay);
        Assert.Empty(world.Mines);
        var blast = Assert.Single(world.Detonations);
        Assert.Equal("near", blast.TriggeredByShipId);
        Assert.Equal(["near"], blast.ShipsHit);
        var target = world.Targets[0];
        Assert.Equal(0, target.Shield);
        Assert.Equal(89, target.Hull, 3);
        Assert.Equal(MineItem, target.LastDamageWeaponId);
        // The pilot who laid it, sitting on it, is never hurt.
        Assert.Equal(world.Player.Definition.Defense.MaximumHull, world.Player.Hull);
    }

    [Fact]
    public void AShipOutsideTheProximityRadiusDoesNotSetItOffButAShieldThatTakesTheDrainKeepsTheHull()
    {
        var far = World(targets: new TargetDefinition { Id = "far", X = 0, Y = -77, Radius = 20 });
        TestWorld.Tick(far, 10, Lay);
        Assert.Empty(far.Detonations);
        Assert.Single(far.Mines);

        var shielded = World(targets: new TargetDefinition { Id = "t", X = 0, Y = -50, Radius = 20, Hull = 100, Shield = 800 });
        shielded.Tick(Lay);
        Assert.Equal(800 - 375, shielded.Targets[0].Shield, 3); // half way out: drain 375
        Assert.Equal(100, shielded.Targets[0].Hull);
    }

    [Fact]
    public void AMineThatDestroysATargetIsAKillForItsOwner()
    {
        var world = World(targets: new TargetDefinition { Id = "weak", X = 0, Y = -30, Radius = 20, Hull = 5, Shield = 0 });
        world.Tick(Lay);
        var kill = Assert.Single(world.Kills);
        Assert.Equal(("weak", WorldSimulation.PlayerShipId), (kill.TargetId, kill.KillerShipId));
        Assert.Equal(1, world.DestroyedTargets);
    }

    [Fact]
    public void AMineArmsAfterItsInactiveTimeAndVanishesWithoutABlastWhenItsLifeRunsOut()
    {
        var armed = World(mine: Mine(armSeconds: 0.5f), targets: new TargetDefinition { Id = "t", X = 0, Y = -60 });
        armed.Tick(Lay);
        TestWorld.Tick(armed, 28);
        Assert.Empty(armed.Detonations);
        armed.Tick(default);
        Assert.Single(armed.Detonations);

        var brief = World(mine: Mine(lifetimeSeconds: 1));
        brief.Tick(Lay);
        TestWorld.Tick(brief, 58);
        Assert.Single(brief.Mines);
        brief.Tick(default);
        Assert.Empty(brief.Mines);
        Assert.Empty(brief.Detonations);

        // Time with no ticks (docked, closed) ages mines too.
        var idle = World(mine: Mine(lifetimeSeconds: 10));
        idle.Tick(Lay);
        idle.ElapseWithoutTicking(300);
        Assert.Equal(301, Assert.Single(idle.Mines).AgeTicks);
        idle.ElapseWithoutTicking(300);
        Assert.Empty(idle.Mines);
    }

    [Fact]
    public void LaidMinesAreSavedWithTheSectorAndKeepTheirAge()
    {
        var world = World();
        world.Tick(Lay);
        TestWorld.Tick(world, 100);
        var snapshot = world.CaptureSector();
        var saved = Assert.Single(snapshot.Mines!);
        Assert.Equal(101, saved.AgeTicks);

        var next = World(mines: 4);
        Assert.True(next.RestoreSector(snapshot));
        var restored = Assert.Single(next.Mines);
        Assert.Equal((101, Vector2.Zero, WorldSimulation.PlayerShipId), (restored.AgeTicks, restored.Position, restored.OwnerShipId));
        // The restored mine counts toward the limit, and new mines get new ids.
        next.Tick(Lay);
        Assert.Equal(new long[] { 1, 2 }, next.Mines.Select(m => m.Id));

        // A world without the mine's definition, or a snapshot from before mines, restores none.
        var plain = new WorldSimulation(TestWorld.Catalog(), Cargo());
        Assert.True(plain.RestoreSector(snapshot));
        Assert.Empty(plain.Mines);
        Assert.True(World().RestoreSector(snapshot with { Mines = null }));
    }

    [Fact]
    public void ReplaysCarryTheLayMineFlagAndDigestTheMines()
    {
        static WorldSimulation Create() => World(targets: new TargetDefinition { Id = "t", X = 300, Y = 0 });
        var recorder = new SimulationRecorder(Create());
        for (var i = 0; i < 240; i++) recorder.Tick(new ShipInput(i < 120 ? 1 : 0, 0.3f, false, LayMine: i % 50 == 0));
        var recording = SimulationRecording.FromBytes(recorder.Finish().ToBytes());
        Assert.Contains(recording.Inputs, input => input.LayMine);
        var replay = SimulationReplayer.Replay(Create, recording);
        Assert.True(replay.Matched);
        Assert.Equal(recorder.World.MinesLaid, replay.World.MinesLaid);

        var laid = World();
        var idle = World();
        laid.Tick(Lay);
        idle.Tick(default);
        Assert.NotEqual(WorldStateHasher.Hash(laid), WorldStateHasher.Hash(idle));
    }

    [Fact]
    public void NoMineIsLaidInASafeZone()
    {
        var map = TestWorld.RogueMap;
        var safe = Enumerable.Range(0, (int)(map.PixelWidth / 16)).SelectMany(x => Enumerable.Range(0, (int)(map.PixelHeight / 16)).Select(y => new Vector2(x * 16 + 8, y * 16 + 8)))
            .First(p => map.IsSafeAt(p) && !map.IsBlocked(p));
        var world = new WorldSimulation(TestWorld.RogueCatalog(), Cargo(), new Dictionary<string, int> { [MineItem] = 5 }, map: map, playerMine: Mine());
        Assert.True(world.RestorePlayer(safe, 0, world.Player.Hull));
        world.Tick(Lay);
        Assert.Empty(world.Mines);
        Assert.Equal(5, world.Cargo.Quantity(MineItem));
    }

    [Fact]
    public void CarryLimitCapsTheHoldAndTheCargoRule()
    {
        var cargo = Cargo();
        var hold = new CargoHold(cargo, new Dictionary<string, int> { [MineItem] = 8 });
        Assert.Equal(new CargoPickupResult(5, 2), hold.Add(MineItem, 5));
        Assert.False(cargo.Fits(new Dictionary<string, int> { [MineItem] = 11 }));
        Assert.True(cargo.Fits(new Dictionary<string, int> { [MineItem] = 10 }));
        Assert.False(cargo.AllowsChange(new Dictionary<string, int> { [MineItem] = 10 }, new Dictionary<string, int> { [MineItem] = 11 }));
        // A hold already over the limit (the data changed) may only shrink there.
        Assert.True(cargo.AllowsChange(new Dictionary<string, int> { [MineItem] = 12 }, new Dictionary<string, int> { [MineItem] = 11 }));
        Assert.True(cargo.KeptOnDestruction(MineItem));
        Assert.False(cargo.KeptOnDestruction("ore"));
        Assert.True(hold.TryRemove(MineItem, 10));
        Assert.False(hold.TryRemove(MineItem, 1));
    }

    [Fact]
    public void AQueuedUseCountsAgainstTheHoldUntilSaved()
    {
        var queue = new PickupCommitQueue();
        queue.Observe("w", [new CargoPickup(0, "mine:1", MineItem, -1, 3), new CargoPickup(1, "mine:2", MineItem, -1, 99)]);
        Assert.Equal(3, queue.HoldContents(new Dictionary<string, int> { [MineItem] = 5 })[MineItem]);
        Assert.False(queue.HoldContents(new Dictionary<string, int> { [MineItem] = 1 }).ContainsKey(MineItem));
    }

    private static StationDefinition LoadStation() =>
        StationDefinition.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "stations", "trading_outpost.json")));

    [Fact]
    public async Task MinesAreBoughtAtHomeStoresUsedOnceAndKeptWhenTheShipIsDestroyed()
    {
        var station = LoadStation();
        var munition = Assert.Single(station.Munitions);
        Assert.Equal(munition.Mine, station.PlayerMine);
        var session = new StationProfileSession(new InMemoryProfileRepository(), station, "starter_interceptor", TestCargo.Capacity);
        Assert.True((await session.OpenOrCreateAsync("mine-pilot")).Succeeded);
        // Credits from a Trading Post recipe (the starting wallet is 0).
        var trade = station.Trades.OrderByDescending(t => t.CreditsPerUnit).First();
        var post = station.SitesOffering(SiteServices.Trade)[0];
        Assert.Equal(TransactionStatus.Applied, (await session.GrantPickupAsync("w0", new CargoPickup(0, "p", trade.InputItemId, 10, 0))).Status);
        Assert.Equal(TransactionStatus.Applied, (await session.SellAsync(trade.InputItemId, 10, post.X, post.Y)).Status);
        Assert.True(session.Profile!.Wallet >= munition.Buy * munition.MaxCarried);

        // Mines sell only at the home stores, never at a Trading Post, and a hold carries ten.
        Assert.NotEqual(TransactionStatus.Applied, (await session.BuyMunitionAsync(munition.Id, 1, post.X, post.Y)).Status);
        var store = station.SitesOffering(SiteServices.Store)[^1];
        Assert.Equal(TransactionStatus.Applied, (await session.BuyMunitionAsync(munition.Id, 9, store.X, store.Y)).Status);
        Assert.Equal(RejectionReason.CargoCapacityExceeded, (await session.BuyMunitionAsync(munition.Id, 2, store.X, store.Y)).Reason);
        Assert.Equal(TransactionStatus.Applied, (await session.BuyMunitionAsync(munition.Id, 1, station.X, station.Y)).Status);
        var wallet = session.Profile!.Wallet;
        Assert.Equal(TransactionStatus.Applied, (await session.SellMunitionAsync(munition.Id, 1, store.X, store.Y)).Status);
        Assert.Equal(wallet + munition.Sell, session.Profile!.Wallet);

        // A laid mine (a pickup of -1) is taken from the profile once, however often the commit is retried.
        var use = new CargoPickup(1, "mine:1", munition.Id, -1, 5);
        Assert.Equal(TransactionStatus.Applied, (await session.GrantPickupAsync("w1", use)).Status);
        var again = await session.GrantPickupAsync("w1", use);
        Assert.True(again.IsReplay);
        Assert.Equal(8, session.Profile!.Inventory[munition.Id]);
        Assert.Equal(RejectionReason.InsufficientInventory,
            (await session.GrantPickupAsync("w1", new CargoPickup(2, "mine:2", munition.Id, -9, 6))).Reason);

        // Destruction loses the cargo but the pilot keeps the mines (bought items remain, Steve, Nov 3 2026).
        Assert.Equal(TransactionStatus.Applied, (await session.GrantPickupAsync("w1", new CargoPickup(3, "p", trade.InputItemId, 2, 7))).Status);
        var destroyed = await session.RecordShipDestroyedAsync("w1", 8);
        Assert.Equal(TransactionStatus.Applied, destroyed.Status);
        Assert.False(destroyed.ItemDeltas.ContainsKey(munition.Id));
        Assert.Equal(-2, destroyed.ItemDeltas[trade.InputItemId]);
        Assert.Equal(8, session.Profile!.Inventory[munition.Id]);
        Assert.False(session.Profile!.Inventory.ContainsKey(trade.InputItemId));
    }

    [RequiresOriginalAssetsFact]
    public void PlasmaMineIsDerivedFromItsOriginalItemRow()
    {
        var munition = OriginalMineBuilder.PlasmaMine(OriginalItemCatalog.Load(OriginalAssets.Root!));
        Assert.Equal(("102", "Plasma Mine", 0L, 25L, 25L, 10), (munition.Id, munition.Name, munition.UnitWeight, munition.Buy, munition.Sell, munition.MaxCarried));
        var m = munition.Mine;
        Assert.Equal((56f, 100f, 750f, 0f, 20f, 5f, (DamageType?)DamageType.Energy), (m.ProximityRadius, m.BlastRadius, m.ShieldDrainInner, m.ShieldDrainOuter, m.DamageInner, m.DamageOuter, m.DamageType));
        Assert.Equal((0f, 60f, 1.5f, 3), (m.ArmSeconds, m.LifetimeSeconds, m.DropIntervalSeconds, m.MaxLivePerPlayer));
        Assert.Equal(munition, LoadStation().Munitions.Single() with { Mine = munition.Mine }); // shipped data matches the derivation
        Assert.Equal(m with { NewValues = null }, LoadStation().PlayerMine! with { NewValues = null });
    }

    [Fact]
    public void FixtureMineMatchesTheShippedPlasmaMine()
    {
        var shipped = LoadStation().PlayerMine!;
        Assert.Equal(Mine() with { Source = shipped.Source, NewValues = shipped.NewValues }, shipped);
    }
}
