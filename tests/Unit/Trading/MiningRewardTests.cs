using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Simulation.Replay;
using RogueTrader.Trading;
using RogueTrader.UnitTests.Persistence;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// Queue M7 mining slice: original mineral spawns (LIO type 5 rows) placed in the practice sector, every pickup granted once
/// through GrantReward with a stable id, and the original refinery recipes. The readings of the raw fields are NEW and listed in
/// the derived data; RT-F042 and RT-F044 stay open.
/// </summary>
public sealed class MiningRewardTests
{
    private const string Profile = "m7-miner";

    private static string Data(params string[] parts) => File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "data", .. parts]));
    private static StationDefinition LoadStation() => StationDefinition.FromJson(Data("stations", "trading_outpost.json"));
    private static MineralFieldCatalog LoadMinerals() => MineralFieldCatalog.FromJson(Data("minerals", "mineral_spawns.json"));

    private static string CommittedMineralsPath =>
        Path.Combine(OriginalAssets.RepoRoot() ?? throw new InvalidOperationException("Repository root not found."), "data", "minerals", "mineral_spawns.json");

    private static async Task<StationProfileSession> OpenAsync(IProfileRepository? repo = null)
    {
        var session = new StationProfileSession(repo ?? new InMemoryProfileRepository(), LoadStation(), "starter_interceptor", TestCargo.Capacity);
        Assert.True((await session.OpenOrCreateAsync(Profile)).Succeeded);
        return session;
    }

    private static WorldSimulation World(MineralFieldCatalog? minerals = null, bool stationPiles = true)
    {
        var cargo = LoadStation().CreateCargoCatalog(TestCargo.Capacity);
        if (!stationPiles) cargo = new CargoCatalog(cargo.Capacity, cargo.Items);
        return new(TestWorld.ExternalCatalog(), cargo, minerals: minerals ?? LoadMinerals());
    }

    private static MineralSpawnDefinition Spawn(string id = "s", float x = 0, float y = -150, int quantity = 4, float clear = 0) =>
        new(id, "2003", quantity, 1, x, y, 0, 0, 30, 120, clear, 3.5f);

    // ---- Derived data ------------------------------------------------------------------------------------------------

    [RequiresOriginalAssetsFact]
    public void CommittedMineralField_MatchesPinnedOriginals()
    {
        var derived = OriginalMineralFieldBuilder.Build(OriginalAssets.Root!).ToJson();
        if (Environment.GetEnvironmentVariable("RT_WRITE_DERIVED_MINERALS") == "1") File.WriteAllText(CommittedMineralsPath, derived);
        Assert.Equal(derived, File.ReadAllText(CommittedMineralsPath).ReplaceLineEndings("\n"));
    }

    [RequiresOriginalAssetsFact]
    public void OriginalMineralRows_AreCarriedRaw()
    {
        var items = OriginalItemCatalog.Load(OriginalAssets.Root!);
        var rows = OriginalMineralSpawns.Load(OriginalAssets.Root!, items);

        Assert.Equal(new[] { 15, 16, 17, 25, 26, 50, 51, 53, 54, 77, 78, 79 }, rows.Select(r => r.Row));
        Assert.Equal(7, rows.Count(r => r.Item.Name == "Tellerium"));
        Assert.Equal(2, rows.Count(r => r.Item.Name == "Krandazite"));
        Assert.Equal(3, rows.Count(r => r.Item.Name == "Iron Ore"));
        var row15 = rows[0];
        Assert.Equal(("Global Tellerium SpawnA", 19280, 10160, 2000, 2000), (row15.Name, row15.F(4), row15.F(5), row15.F(6), row15.F(7)));
        Assert.Equal((3000, 6000, 0, 1, 100, 2003), (row15.F(11), row15.F(12), row15.F(14), row15.F(18), row15.F(19), row15.F(20)));
        Assert.Equal((350, 15), (row15.F(26), row15.F(27)));
        Assert.Equal((-1, -1), (rows.Single(r => r.Row == 54).F(18), rows.Single(r => r.Row == 54).F(19)));
    }

    [Fact]
    public void ShippedMineralField_IsLabelledAndReadsTheRawFields()
    {
        var document = Data("minerals", "mineral_spawns.json");
        Assert.Contains("\"evidenceClassification\": \"ORIGINAL+NEW\"", document);
        Assert.Contains("\"lioRow\": 54", document); // the -1/-1 row is listed as skipped, not spawned
        var minerals = LoadMinerals();
        Assert.Equal(11, minerals.Spawns.Count);
        var a = minerals.Spawns.Single(s => s.Id == "16"); // row 15 "Global Tellerium SpawnA"
        Assert.Equal(("2003", 1, 100, 100, 1800, 3600), (a.ItemId, a.Quantity, a.MaxPiles, a.StartingPiles, a.AttemptTicks, a.SucceedTicks));
        Assert.Equal(new Vector2(3280 * OriginalMineralFieldBuilder.MapScale, -5840 * OriginalMineralFieldBuilder.MapScale), new Vector2(a.X, a.Y));
        Assert.Equal(80 * OriginalMineralFieldBuilder.MapScale, a.PileRadius);
        var cargo = LoadStation().CreateCargoCatalog(TestCargo.Capacity);
        Assert.All(minerals.Spawns, s => Assert.True(cargo.IsKnown(s.ItemId)));
    }

    [Fact]
    public void ShippedStation_HasTheOriginalRefineryRecipes()
    {
        var refinery = LoadStation().Refinery ?? throw new InvalidOperationException("The shipped station must carry the refinery.");
        Assert.Equal(new[] { ("2003", "2004"), ("2005", "2006"), ("2019", "2020") }, refinery.Recipes.Select(r => (r.InputItemId, r.OutputItemId)));
        Assert.Equal(new Vector2(-5248 * OriginalMineralFieldBuilder.MapScale, -15376 * OriginalMineralFieldBuilder.MapScale), new Vector2(refinery.X, refinery.Y));
    }

    // ---- Simulation --------------------------------------------------------------------------------------------------

    [Fact]
    public void FreshSector_IsStockedOnTheFirstTick_AndPlacementIsReproducible()
    {
        var a = World();
        var b = World();
        a.Tick(default);
        b.Tick(default);

        var piles = a.Collectibles.Where(p => p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal)).ToArray();
        var field = LoadMinerals();
        Assert.InRange(piles.Length, field.Spawns.Sum(s => s.MaxPiles) * 3 / 4, field.Spawns.Sum(s => s.MaxPiles));
        Assert.All(piles, p => Assert.Equal(1, p.Quantity));
        foreach (var spawn in field.Spawns)
            Assert.InRange(piles.Count(p => p.Id.StartsWith($"mineral:{spawn.Id}:", StringComparison.Ordinal)), 1, spawn.MaxPiles);
        Assert.Equal(piles.Select(p => p.Position), b.Collectibles.Where(p => p.Id.StartsWith("mineral:", StringComparison.Ordinal)).Select(p => p.Position));
        Assert.Equal(WorldStateHasher.Hash(a), WorldStateHasher.Hash(b));
        foreach (var pile in piles)
            Assert.All(a.Asteroids, rock => Assert.True(Vector2.Distance(rock.Position, pile.Position) >= rock.Radius + pile.Radius));
        var other = World(new MineralFieldCatalog(LoadMinerals().Spawns, 7));
        other.Tick(default);
        Assert.NotEqual(piles.Select(p => p.Position), other.Collectibles.Where(p => p.Id.StartsWith("mineral:", StringComparison.Ordinal)).Select(p => p.Position));
    }

    [Fact]
    public void ForWorld_SameSaltReplaysPlacement_AndADifferentSaltMovesIt()
    {
        static Vector2[] Placement(MineralFieldCatalog field)
        {
            var world = World(field, stationPiles: false);
            world.Tick(default);
            return world.Collectibles.Select(p => p.Position).ToArray();
        }
        var field = LoadMinerals();
        var salt = BitConverter.ToUInt64(new Guid(1, 0x534D, 0x4B45, 0, 0, 0, 0, 0, 0, 0, 0).ToByteArray(), 0);
        Assert.Equal(field.Seed ^ salt, field.ForWorld(salt).Seed);
        Assert.Equal(field.Spawns, field.ForWorld(salt).Spawns);
        Assert.Equal(Placement(field.ForWorld(salt)), Placement(field.ForWorld(salt)));
        Assert.NotEqual(Placement(field.ForWorld(salt)), Placement(field.ForWorld(salt + 1)));
    }

    [Fact]
    public void WorldWithoutMinerals_HashesAsBefore()
    {
        var plain = new WorldSimulation(TestWorld.ExternalCatalog());
        var empty = new WorldSimulation(TestWorld.ExternalCatalog(), minerals: null);
        plain.Tick(default);
        empty.Tick(default);
        Assert.Empty(plain.MineralSpawns);
        Assert.Equal(WorldStateHasher.Hash(plain), WorldStateHasher.Hash(empty));
    }

    [Fact]
    public void PickedUpMineral_IsLogged_AndRespawnsAfterTheSucceedDelay()
    {
        var world = World(new MineralFieldCatalog([Spawn()], 1), stationPiles: false);
        world.Tick(default); // tick 1: the pile appears at (0, -150), in the ship's path
        Assert.Equal("mineral:s:1", Assert.Single(world.Collectibles, p => p.Id.StartsWith("mineral:", StringComparison.Ordinal)).Id);
        for (var i = 0; i < 600 && world.Pickups.Count == 0; i++) world.Tick(new ShipInput(1, 0, false));

        var pickup = Assert.Single(world.Pickups);
        Assert.Equal((0, "mineral:s:1", "2003", 4), (pickup.Sequence, pickup.PileId, pickup.ItemId, pickup.Quantity));
        Assert.True(pickup.IsMineral);
        Assert.Equal(4, world.Cargo.Quantity("2003"));
        Assert.DoesNotContain(world.Collectibles, p => p.Id.StartsWith("mineral:", StringComparison.Ordinal));

        // The next attempt is due 120 ticks after the spawn on tick 1, i.e. on tick 121.
        while (world.TickNumber < 120) world.Tick(default);
        Assert.DoesNotContain(world.Collectibles, p => p.Id.StartsWith("mineral:", StringComparison.Ordinal));
        world.Tick(default);
        Assert.Contains(world.Collectibles, p => p.Id == "mineral:s:2");
    }

    [Fact]
    public void Attempt_FailsWhileAPileRemains_OrThePlayerIsTooClose()
    {
        var world = World(new MineralFieldCatalog([Spawn(), Spawn("near", 0, 0, clear: 10)], 1));
        world.Tick(default);
        Assert.Contains(world.Collectibles, p => p.Id == "mineral:s:1");
        Assert.DoesNotContain(world.Collectibles, p => p.Id.StartsWith("mineral:near:", StringComparison.Ordinal)); // ship sits on it
        for (var i = 0; i < 400; i++) world.Tick(default);
        Assert.Single(world.Collectibles, p => p.Id.StartsWith("mineral:s:", StringComparison.Ordinal)); // max one pile at a time
    }

    [Fact]
    public void Catalog_RejectsBadSpawns_AndUnknownItems()
    {
        Assert.Throws<ArgumentException>(() => new MineralFieldCatalog([Spawn() with { Quantity = 0 }], 1));
        Assert.Throws<ArgumentException>(() => new MineralFieldCatalog([Spawn(), Spawn()], 1));
        Assert.Throws<ArgumentException>(() => new MineralFieldCatalog([Spawn() with { PileRadius = 0 }], 1));
        Assert.Throws<ArgumentException>(() => World(new MineralFieldCatalog([Spawn() with { ItemId = "nope" }], 1)));
    }

    // ---- Rewards -----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task MinedPickup_IsSavedOnce_AcrossRetryAndReload()
    {
        var repo = new FaultyRepository(new InMemoryProfileRepository());
        var session = await OpenAsync(repo);
        var queue = new PickupCommitQueue();
        var pickup = new CargoPickup(0, "mineral:16:1", "2003", 40, 40);
        queue.Observe("sector-1", [pickup]);
        Assert.Equal(new Dictionary<string, int> { ["2003"] = 40 }, queue.HoldContents(session.Profile!.Inventory));

        // The save fails: the pickup stays queued, so the hold still holds it.
        repo.FailNextSave = true;
        var failed = await queue.CommitAsync(session);
        Assert.Equal(RejectionReason.PersistenceFailed, failed.Blocked);
        Assert.Equal(1, queue.Count);
        Assert.Empty(session.Profile!.Inventory);

        // The save succeeds but the acknowledgement is lost: after a reload the retry is a replay and leaves the queue.
        Assert.True((await session.ReloadAsync()).Succeeded);
        Assert.Equal(TransactionStatus.Applied, (await session.GrantPickupAsync("sector-1", pickup)).Status);
        // Review R1-2: the reloaded profile already holds it, so the hold must not count the still-queued pickup twice.
        var savedIds = session.Profile!.AppliedTransactions.Select(t => t.TransactionId);
        Assert.Equal(new Dictionary<string, int> { ["2003"] = 40 }, queue.HoldContents(session.Profile.Inventory, savedIds));
        Assert.Equal(1, queue.Count);
        var replay = await queue.CommitAsync(session);
        Assert.Equal(pickup, Assert.Single(replay.Replayed));
        Assert.Null(replay.Blocked);
        Assert.Equal(0, queue.Count);
        Assert.Equal(40, session.Profile!.Inventory["2003"]);
        Assert.Equal(new Dictionary<string, int> { ["2003"] = 40 }, queue.HoldContents(session.Profile.Inventory));
        Assert.Single(session.Profile.Ledger, e => e.TransactionId == StationProfileSession.PickupTransactionId("sector-1", 0));
    }

    [Fact]
    public async Task Queue_SeesEachPickupOnce_AndHandsBackUnsavedOnesOnANewWorld()
    {
        var session = await OpenAsync();
        var queue = new PickupCommitQueue();
        var pickups = new List<CargoPickup> { new(0, "mineral:16:1", "2003", 5, 1) };
        queue.Observe("a", pickups);
        queue.Observe("a", pickups);
        Assert.Equal(1, queue.Count);
        pickups.Add(new CargoPickup(1, "pile_alloy_west", "2020", 12, 2));
        queue.Observe("a", pickups);
        var outcome = await queue.CommitAsync(session);
        Assert.Equal(2, outcome.Applied.Count);
        Assert.Equal((5, 12), (session.Profile!.Inventory["2003"], session.Profile.Inventory["2020"]));

        queue.Observe("a", [.. pickups, new CargoPickup(2, "mineral:16:2", "2003", 3, 9)]);
        var lost = queue.StartNewWorld();
        Assert.Equal(3, Assert.Single(lost).Quantity);
        queue.Observe("b", [new CargoPickup(0, "mineral:16:1", "2003", 2, 1)]); // a new world starts its sequence at 0 again
        Assert.False(Assert.Single((await queue.CommitAsync(session)).Applied).Sequence != 0);
        Assert.Equal(7, session.Profile!.Inventory["2003"]);
    }

    [Fact]
    public async Task ResetThatKeepsTheSector_StillSavesTheSamePileAgain()
    {
        // Review R1-1: a reset that keeps the sector instance builds a new world whose pickup sequence restarts at 0. The client
        // keys pickups on a per-world id, so collecting the same starting pile again is a new grant, not a replay.
        var session = await OpenAsync();
        var queue = new PickupCommitQueue();
        var first = new CargoPickup(0, "pile_wreckage_launch", "2008", 5, 30);
        queue.Observe("world-1", [first]);
        Assert.Single((await queue.CommitAsync(session)).Applied);
        queue.StartNewWorld();
        queue.Observe("world-2", [first]);
        var again = await queue.CommitAsync(session);
        Assert.Single(again.Applied);
        Assert.Empty(again.Replayed);
        Assert.Equal(10, session.Profile!.Inventory["2008"]);
        // The hazard the per-world id avoids: reusing an id makes the core report the pickup as already saved.
        Assert.True((await session.GrantPickupAsync("world-1", first)).IsReplay);
    }

    [Fact]
    public async Task Queue_DropsAPickupTheHoldCanNeverTake()
    {
        var session = await OpenAsync();
        var queue = new PickupCommitQueue();
        queue.Observe("a", [new CargoPickup(0, "mineral:27:1", "2005", 26, 1)]); // 26 x 100,000 > 2,500,000 (the Asp StopWeight)
        var outcome = await queue.CommitAsync(session);
        Assert.Equal(RejectionReason.CargoCapacityExceeded, Assert.Single(outcome.Refused).Reason);
        Assert.Equal(0, queue.Count);
        Assert.Empty(session.Profile!.Inventory);
    }

    // ---- Refinery ----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Refine_TurnsOreIntoTheRefinedGood_OnlyAtTheRefinery()
    {
        var session = await OpenAsync();
        var refinery = session.Station.Refinery!;
        var recipe = refinery.Recipes.Single(r => r.InputItemId == "2003");
        Assert.Equal(TransactionStatus.Applied, (await session.GrantPickupAsync("a", new CargoPickup(0, "mineral:16:1", "2003", 40, 1))).Status);

        Assert.Equal(RejectionReason.StationAccessDenied, (await session.RefineAsync(recipe.Id, 10, session.Station.X, session.Station.Y)).Reason);
        Assert.True(session.IsDockedAtRefinery(refinery.X, refinery.Y));
        Assert.False(session.IsDocked(refinery.X, refinery.Y));
        Assert.Equal(RejectionReason.InsufficientInventory, (await session.RefineAsync(recipe.Id, 41, refinery.X, refinery.Y)).Reason);
        Assert.Equal(RejectionReason.NotForSale, (await session.RefineAsync("no_such_recipe", 1, refinery.X, refinery.Y)).Reason);
        var wallet = session.Profile!.Wallet;

        var refined = await session.RefineAsync(recipe.Id, 40, refinery.X, refinery.Y);
        Assert.Equal(TransactionStatus.Applied, refined.Status);
        Assert.Equal(new Dictionary<string, int> { ["2003"] = -40, ["2004"] = 40 }, refined.ItemDeltas);
        Assert.False(session.Profile!.Inventory.ContainsKey("2003"));
        Assert.Equal(40, session.Profile.Inventory["2004"]);
        Assert.Equal(wallet, session.Profile.Wallet);

        // Refined Tellerium then trades at the outpost at the original recipe value.
        var trade = session.Station.Trades.Single(t => t.InputItemId == "2004");
        Assert.Equal(TransactionStatus.Applied, (await session.SellAsync("2004", 40, session.Station.X, session.Station.Y)).Status);
        Assert.Equal(wallet + 40 * trade.CreditsPerUnit, session.Profile!.Wallet);
    }

    [Fact]
    public async Task RefineTransaction_SurvivesSaveAndReload_AtSchema4()
    {
        var repo = new InMemoryProfileRepository();
        var session = await OpenAsync(repo);
        var refinery = session.Station.Refinery!;
        await session.GrantPickupAsync("a", new CargoPickup(0, "mineral:14:1", "2019", 10, 1));
        Assert.Equal(TransactionStatus.Applied, (await session.RefineAsync(refinery.Recipes[2].Id, 10, refinery.X, refinery.Y)).Status);

        var reopened = await OpenAsync(repo);
        Assert.True(PlayerProfile.CurrentSchemaVersion >= 4);
        Assert.Equal(10, reopened.Profile!.Inventory["2020"]);
        Assert.Contains(reopened.Profile.AppliedTransactions, t => t.Kind == TransactionKind.Refine);
        var bytes = ProfileSerializer.Serialize(reopened.Profile);
        Assert.Equal(TransactionKind.Refine, ProfileSerializer.Decode(bytes, ProfileSchemaMigrations.BuiltIn).Profile!.AppliedTransactions.Last().Kind);
    }

    [Fact]
    public void Station_RejectsARefineryRecipeForAnUnknownGood()
    {
        const string json = """
            { "stationId": "s", "name": "S", "dockRadius": 5, "goods": [ { "id": "a", "name": "A", "unitWeight": 1 } ],
              "refinery": { "stationId": "r", "name": "R", "x": 0, "y": 0, "dockRadius": 5,
                "recipes": [ { "id": "x", "name": "X", "inputItemId": "a", "outputItemId": "b", "rawField4": 0 } ] } }
            """;
        Assert.Throws<InvalidDataException>(() => StationDefinition.FromJson(json));
    }

    /// <summary>Fails the next save once, as if the disk refused it.</summary>
    private sealed class FaultyRepository(IProfileRepository inner) : IProfileRepository
    {
        public bool FailNextSave { get; set; }
        public Task<ProfileLoadResult> LoadAsync(string profileId, CancellationToken ct = default) => inner.LoadAsync(profileId, ct);
        public Task<ProfileSaveResult> CreateAsync(PlayerProfile profile, CancellationToken ct = default) => inner.CreateAsync(profile, ct);
        public Task<ProfileSaveResult> SaveAsync(PlayerProfile profile, long expectedRevision, CancellationToken ct = default)
        {
            if (!FailNextSave) return inner.SaveAsync(profile, expectedRevision, ct);
            FailNextSave = false;
            throw new IOException("Simulated save failure.");
        }
    }
}
