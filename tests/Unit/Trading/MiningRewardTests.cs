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

    // NEW fixture: a point area (width 0) that drops one pile per event and refills only when empty, like the original rows.
    private static MineralSpawnDefinition Spawn(string id = "s", float x = 0, float y = -150, int quantity = 4, float minDistance = 0) =>
        new(id, "2003", quantity, x, y, 0, 0, 30, 120, 3.5f, MaxInArea: 1, MinPlayerDistance: minDistance);

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
    public void ShippedMineralField_IsLabelledAndReadsTheRawFieldsAsTheEmulatorDoes()
    {
        var document = Data("minerals", "mineral_spawns.json");
        Assert.Contains("\"evidenceClassification\": \"ORIGINAL+RECONSTRUCTED+NEW\"", document);
        var minerals = LoadMinerals();
        Assert.Equal(12, minerals.Spawns.Count); // Tellerium SpawnG (f[18]/f[19] = -1, no limits) is spawned too now
        var a = minerals.Spawns.Single(s => s.Id == "16"); // row 15 "Global Tellerium SpawnA"
        // f[21]=1 unit per drop, f[11]=3000 and f[12]=6000 tenths of a second, f[10]=1 initial event, f[18]=1 per area,
        // f[19]=100 in the sector, f[27]=15 drops per event, f[14..15] 0..500 players, [Terrain0] PrizeExpire=720 s.
        Assert.Equal(("2003", 1, 300 * 60, 600 * 60, 1, 1, 100, 15, 0, 500, 720L * 60, 1),
            (a.ItemId, a.Quantity, a.AttemptTicks, a.SucceedTicks, a.InitialCount, a.MaxInArea, a.MaxInLevel, a.ClumpQuantity,
                a.MinPlayers, a.MaxPlayers, a.ExpireTicks, a.Probability));
        Assert.Equal(350, a.ClumpRadius); // map pixels, 1:1
        Assert.Equal((0f, 0f), (a.MinPlayerDistance, a.MaxPlayerDistance)); // f[16]=0 and f[17]=99999: no distance checks
        Assert.Equal(new Vector2(19280 - 48, 10160 - 48), new Vector2(a.X, a.Y)); // LIO position minus the LVL offset
        Assert.Equal(80, a.PileRadius); // CFG [Arena] ItemPickupDistance
        Assert.Equal((10, 50), (minerals.Spawns.Single(s => s.Id == "27").ClumpQuantity, minerals.Spawns.Single(s => s.Id == "27").MaxInLevel));
        var cargo = LoadStation().CreateCargoCatalog(TestCargo.Capacity);
        Assert.All(minerals.Spawns, s => Assert.True(cargo.IsKnown(s.ItemId)));
        // Single player, the spawns that need 40-80 players stay quiet: Tellerium A-C, Krandazite A and Iron A-C run.
        Assert.Equal(new[] { "14", "15", "16", "17", "26", "27", "54" }, minerals.Spawns.Where(s => s.MinPlayers <= 1).Select(s => s.Id).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ShippedStation_HasTheOriginalRefineryRecipes()
    {
        var refinery = LoadStation().Refinery ?? throw new InvalidOperationException("The shipped station must carry the refinery.");
        Assert.Equal(new[] { ("2003", "2004"), ("2005", "2006"), ("2019", "2020") }, refinery.Recipes.Select(r => (r.InputItemId, r.OutputItemId)));
        Assert.Equal(new Vector2(10752 - 48, 624 - 48), new Vector2(refinery.X, refinery.Y)); // LIO row 13 minus the LVL offset
    }

    // ---- Simulation --------------------------------------------------------------------------------------------------

    [Fact]
    public void ShippedSpawns_TakeEachRuleFromItsOwnRawColumn()
    {
        using var document = System.Text.Json.JsonDocument.Parse(Data("minerals", "mineral_spawns.json"));
        foreach (var spawn in document.RootElement.GetProperty("spawns").EnumerateArray())
        {
            var raw = spawn.GetProperty("source").GetProperty("raw");
            int F(int index) => raw.GetProperty($"f{index}").GetInt32();
            Assert.Equal(F(21), spawn.GetProperty("quantity").GetInt32()); // HideQuantity, not f[18]
            Assert.Equal(F(18), spawn.GetProperty("maxInArea").GetInt32());
            Assert.Equal(F(19), spawn.GetProperty("maxInLevel").GetInt32());
            Assert.Equal(F(27), spawn.GetProperty("clumpQuantity").GetInt32());
            Assert.Equal(F(14), spawn.GetProperty("minPlayers").GetInt32());
            Assert.Equal(F(15), spawn.GetProperty("maxPlayers").GetInt32());
            Assert.Equal(F(10), spawn.GetProperty("initialCount").GetInt32());
            Assert.Equal(F(13), spawn.GetProperty("probability").GetInt32());
            Assert.Equal(F(11) * 6, spawn.GetProperty("attemptTicks").GetInt32()); // x100 ms at 60 Hz
            Assert.Equal(F(12) * 6, spawn.GetProperty("succeedTicks").GetInt32());
            Assert.Equal(F(20).ToString(System.Globalization.CultureInfo.InvariantCulture), spawn.GetProperty("itemId").GetString());
        }
    }

    [Fact]
    public void FreshSector_DropsOneClumpPerActiveSpawn_AndPlacementIsReproducible()
    {
        var a = World();
        var b = World();
        a.Tick(default);
        b.Tick(default);

        var piles = a.Collectibles.Where(p => p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal)).ToArray();
        Assert.All(piles, p => Assert.Equal(1, p.Quantity));
        Assert.All(piles, p => Assert.Equal(a.TickNumber + 720 * 60, p.ExpiresAt));
        // The emulator's arena start: 3 x 15 Tellerium, 10 Krandazite, 3 x 15 Iron Ore. A drop that would land in an asteroid
        // uses up a try, so a clump whose box is mostly rock comes up short: Iron SpawnA's area sits on asteroid_12 in this
        // sector's (NEW) layout and usually drops nothing.
        Assert.InRange(piles.Count(p => p.ItemId == "2003"), 40, 45);
        Assert.InRange(piles.Count(p => p.ItemId == "2005"), 8, 10);
        Assert.InRange(piles.Count(p => p.ItemId == "2019"), 25, 45);
        foreach (var spawn in LoadMinerals().Spawns)
        {
            var mine = piles.Where(p => p.Id.StartsWith($"mineral:{spawn.Id}:", StringComparison.Ordinal)).ToArray();
            if (spawn.MinPlayers > 1) Assert.Empty(mine);
            else Assert.InRange(mine.Length, spawn.Id == "14" ? 0 : 1, spawn.ClumpQuantity);
            // A clump lands in its box around an anchor inside the area.
            Assert.All(mine, p => Assert.True(MathF.Abs(p.Position.X - spawn.X) <= spawn.Width / 2 + spawn.ClumpRadius / 2 + 0.01f
                && MathF.Abs(p.Position.Y - spawn.Y) <= spawn.Height / 2 + spawn.ClumpRadius / 2 + 0.01f));
            if (mine.Length > 1)
                Assert.True(mine.Max(p => p.Position.X) - mine.Min(p => p.Position.X) <= spawn.ClumpRadius + 0.01f);
        }
        Assert.Equal(piles.Select(p => p.Position), b.Collectibles.Where(p => p.Id.StartsWith("mineral:", StringComparison.Ordinal)).Select(p => p.Position));
        Assert.Equal(WorldStateHasher.Hash(a), WorldStateHasher.Hash(b));
        foreach (var pile in piles)
            Assert.All(a.Asteroids, rock => Assert.True(Vector2.Distance(rock.Position, pile.Position) >= rock.Radius + pile.Radius));
        var other = World(new MineralFieldCatalog(LoadMinerals().Spawns, 7));
        other.Tick(default);
        Assert.NotEqual(piles.Select(p => p.Position), other.Collectibles.Where(p => p.Id.StartsWith("mineral:", StringComparison.Ordinal)).Select(p => p.Position));
    }

    [Fact]
    public void ShippedField_NeverHoldsMoreThanTheSectorCaps_AndRefillsOnlyEmptyAreas()
    {
        var world = World(stationPiles: false);
        world.Tick(default);
        var field = LoadMinerals();
        // Nobody collects for a long time: drops expire after 12 minutes and each area refills once empty, never past the caps.
        for (var hour = 0; hour < 6; hour++)
        {
            world.ElapseWithoutTicking(3600 * WorldSimulation.TickRate);
            var piles = world.Collectibles.ToArray();
            Assert.True(piles.Count(p => p.ItemId == "2003") <= 100);
            Assert.True(piles.Count(p => p.ItemId == "2019") <= 100);
            Assert.True(piles.Count(p => p.ItemId == "2005") <= 50);
            foreach (var spawn in field.Spawns)
                Assert.True(piles.Count(p => p.Id.StartsWith($"mineral:{spawn.Id}:", StringComparison.Ordinal)) <= spawn.ClumpQuantity,
                    $"Spawn {spawn.Id} dropped a second clump while its area still held one.");
        }

        // With enough players the gated spawns run too, and the sector caps hold across all of them.
        var crowded = World(stationPiles: false);
        crowded.PlayersInSector = 100;
        crowded.RestorePlayer(new Vector2(1e6f, 1e6f), 0, crowded.Player.Hull);
        var krandaziteB = false;
        for (var tick = 0; tick < 4 * 3600 * WorldSimulation.TickRate; tick += 600)
        {
            crowded.ElapseWithoutTicking(600);
            krandaziteB |= crowded.Collectibles.Any(p => p.Id.StartsWith("mineral:84:", StringComparison.Ordinal)); // Krandazite B needs 60
            Assert.True(crowded.Collectibles.Count(p => p.ItemId == "2005") <= 50);
            // Tellerium G has no caps (-1) and is held back only by expiry. The capped spawns stop once the sector holds 100
            // units of Tellerium (G's included); a clump that starts just under the cap can overshoot it by at most 14.
            Assert.True(crowded.Collectibles.Count(p => p.ItemId == "2003" && !p.Id.StartsWith("mineral:85:", StringComparison.Ordinal)) <= 100 + 14);
        }
        Assert.True(krandaziteB, "Krandazite B (60 players) must drop in a crowded sector.");
    }

    [Fact]
    public void AMinedOutArea_RefillsOnItsNextAttempt_AndAFullOneWaits()
    {
        var world = World(new MineralFieldCatalog([Spawn()], 1), stationPiles: false);
        world.Tick(default); // tick 1: the initial event drops the pile at (0, -150)
        Assert.Equal("mineral:s:1", Assert.Single(world.Collectibles).Id);
        TestWorld.Tick(world, 100); // attempts on ticks 31, 61 and 91 fail: the area still holds the pile
        Assert.Equal("mineral:s:1", Assert.Single(world.Collectibles).Id);
        world.RestorePlayer(new Vector2(0, -150), 0, world.Player.Hull);
        world.Tick(default); // tick 102: picked up
        Assert.Equal((0, "mineral:s:1", "2003", 4), (world.Pickups[0].Sequence, world.Pickups[0].PileId, world.Pickups[0].ItemId, world.Pickups[0].Quantity));
        Assert.True(world.Pickups[0].IsMineral);
        world.RestorePlayer(new Vector2(500, 500), 0, world.Player.Hull);
        while (world.TickNumber < 120) world.Tick(default);
        Assert.Empty(world.Collectibles);
        world.Tick(default); // tick 121: the next attempt finds the area empty
        Assert.Equal("mineral:s:2", Assert.Single(world.Collectibles).Id);
    }

    [Fact]
    public void Drops_ExpireAfterTheirLife_AndThenTheAreaRefills()
    {
        var world = World(new MineralFieldCatalog([Spawn() with { ExpireTicks = 50 }], 1), stationPiles: false);
        world.RestorePlayer(new Vector2(500, 500), 0, world.Player.Hull);
        world.Tick(default);
        Assert.Equal(51, Assert.Single(world.Collectibles).ExpiresAt);
        TestWorld.Tick(world, 49);
        Assert.Single(world.Collectibles);
        world.Tick(default); // tick 51: gone
        Assert.Empty(world.Collectibles);
        TestWorld.Tick(world, 10); // tick 61: the next attempt drops a new one
        Assert.Equal("mineral:s:2", Assert.Single(world.Collectibles).Id);
    }

    [Fact]
    public void Attempt_FailsWithTooFewPlayers_OrAPlayerTooClose_OrTheSectorFull()
    {
        var gated = World(new MineralFieldCatalog([Spawn() with { MinPlayers = 40 }], 1), stationPiles: false);
        TestWorld.Tick(gated, 100);
        Assert.Empty(gated.Collectibles);
        gated.PlayersInSector = 40;
        TestWorld.Tick(gated, 30);
        Assert.Single(gated.Collectibles);

        var near = World(new MineralFieldCatalog([Spawn("near", 0, 0, minDistance: 10)], 1), stationPiles: false);
        TestWorld.Tick(near, 100);
        Assert.Empty(near.Collectibles); // the ship sits on it

        var capped = World(new MineralFieldCatalog([Spawn("a") with { MaxInLevel = 4 }, Spawn("b", 40, -150) with { MaxInLevel = 4 }], 1), stationPiles: false);
        capped.RestorePlayer(new Vector2(500, 500), 0, capped.Player.Hull);
        TestWorld.Tick(capped, 200);
        Assert.Equal("mineral:a:1", Assert.Single(capped.Collectibles).Id); // a's 4 units fill the sector cap, so b never drops
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
    public void Catalog_RejectsBadSpawns_AndUnknownItems()
    {
        Assert.Throws<ArgumentException>(() => new MineralFieldCatalog([Spawn() with { Quantity = 0 }], 1));
        Assert.Throws<ArgumentException>(() => new MineralFieldCatalog([Spawn(), Spawn()], 1));
        Assert.Throws<ArgumentException>(() => new MineralFieldCatalog([Spawn() with { PileRadius = 0 }], 1));
        Assert.Throws<ArgumentException>(() => new MineralFieldCatalog([Spawn() with { MaxInLevel = -2 }], 1));
        Assert.Throws<ArgumentException>(() => new MineralFieldCatalog([Spawn() with { ExpireTicks = -1 }], 1));
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

        // Refined Tellerium then trades at a Trading Post at the original recipe value.
        var trade = session.Station.Trades.Single(t => t.InputItemId == "2004");
        Assert.Equal(TransactionStatus.Applied, (await session.SellAsync("2004", 40, session.Station.SitesOffering(SiteServices.Trade)[0].X, session.Station.SitesOffering(SiteServices.Trade)[0].Y)).Status);
        Assert.Equal(wallet + 40 * trade.CreditsPerUnit, session.Profile!.Wallet);
    }

    [Fact]
    public async Task Refine_StillWorks_ForASaveOverTheHoldCap()
    {
        // A save made under the old 4,000,000 cap: 60 Tellerium (60 x 50,000 = 3,000,000) is over the Asp's 2,500,000 StopWeight.
        var repo = new InMemoryProfileRepository();
        var old = new StationProfileSession(repo, LoadStation(), "starter_interceptor", 4_000_000);
        Assert.True((await old.OpenOrCreateAsync(Profile)).Succeeded);
        Assert.Equal(TransactionStatus.Applied, (await old.GrantPickupAsync("old", new CargoPickup(0, "mineral:16:1", "2003", 60, 1))).Status);

        var session = await OpenAsync(repo);
        var refinery = session.Station.Refinery!;
        var recipe = refinery.Recipes.Single(r => r.InputItemId == "2003");
        Assert.True(session.Cargo.WeightOf(session.Profile!.Inventory) > session.Cargo.Capacity);

        // Refining 10 makes the hold lighter (2,750,000) but leaves it over the cap: allowed, because it is no heavier than before.
        Assert.Equal(TransactionStatus.Applied, (await session.RefineAsync(recipe.Id, 10, refinery.X, refinery.Y)).Status);
        Assert.True(session.Cargo.WeightOf(session.Profile!.Inventory) > session.Cargo.Capacity);
        Assert.Equal(TransactionStatus.Applied, (await session.RefineAsync(recipe.Id, 50, refinery.X, refinery.Y)).Status);
        Assert.Equal(60, session.Profile!.Inventory["2004"]);
        Assert.False(session.Profile.Inventory.ContainsKey("2003"));
    }

    [Fact]
    public async Task OverCapSave_CannotGetHeavier()
    {
        var repo = new InMemoryProfileRepository();
        var old = new StationProfileSession(repo, LoadStation(), "starter_interceptor", 4_000_000);
        Assert.True((await old.OpenOrCreateAsync(Profile)).Succeeded);
        Assert.Equal(TransactionStatus.Applied, (await old.GrantPickupAsync("old", new CargoPickup(0, "mineral:16:1", "2003", 60, 1))).Status);

        var session = await OpenAsync(repo);
        var before = session.Profile!;
        var pickup = await session.GrantPickupAsync("new", new CargoPickup(0, "mineral:16:2", "2003", 1, 1));
        Assert.Equal(RejectionReason.CargoCapacityExceeded, pickup.Reason);
        Assert.Equal(before.Inventory.ToArray(), session.Profile!.Inventory.ToArray());
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
