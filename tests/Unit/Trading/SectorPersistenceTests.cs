using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;
using RogueTrader.UnitTests.Persistence;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// Owner rule, November 2, 2026: the world keeps its state for as long as the server runs, so a client relaunch, a logout or a
/// sector reset never refills piles or brings targets back early. The local build hosts the world in the client, so the
/// sector is carried between worlds by <see cref="SectorSnapshot"/> and kept in the save folder by <see cref="SectorStateStore"/>.
/// Fixture values below are NEW test data.
/// </summary>
public sealed class SectorPersistenceTests
{
    private static string Data(params string[] parts) => File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "data", .. parts]));
    private static StationDefinition LoadStation() => StationDefinition.FromJson(Data("stations", "trading_outpost.json"));
    private static MineralFieldCatalog LoadMinerals() => MineralFieldCatalog.FromJson(Data("minerals", "mineral_spawns.json"));

    private static WorldSimulation ShippedWorld(ulong salt) =>
        new(TestWorld.ExternalCatalog(), LoadStation().CreateCargoCatalog(TestCargo.Capacity), minerals: LoadMinerals().ForWorld(salt));

    private static WorldSimulation TargetWorld() => new(TestWorld.Catalog(sector: new SectorDefinition
    {
        Targets = [new TargetDefinition { Id = "t1" }],
        TargetRespawnSeconds = 60,
    }));

    private static void FireUntilKill(WorldSimulation world)
    {
        var kills = world.Kills.Count;
        for (var tick = 0; tick < 3000 && world.Kills.Count == kills; tick++) world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(kills + 1, world.Kills.Count);
    }

    private static (string Id, int Quantity, Vector2 Position)[] Piles(WorldSimulation world) =>
        world.Collectibles.Select(p => (p.Id, p.Quantity, p.Position)).ToArray();

    [Fact]
    public void CollectedPiles_StayGoneInEveryLaterWorld_AndTheMineralFieldIsNotRestocked()
    {
        var first = ShippedWorld(salt: 1);
        first.Tick(default); // every mineral spawn stocks its starting piles
        var pile = first.Collectibles.First(p => !p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal));
        first.RestorePlayer(pile.Position, 0, first.Player.Hull);
        first.Tick(default);
        Assert.Contains(first.Pickups, p => p.PileId == pile.Id);
        Assert.DoesNotContain(first.Collectibles, p => p.Id == pile.Id);
        var left = Piles(first);

        // A relaunch: a new world with another mineral salt, continued from the saved sector.
        var second = ShippedWorld(salt: 2);
        Assert.True(second.RestoreSector(first.CaptureSector()));
        Assert.Equal(left, Piles(second));
        second.Tick(default);
        Assert.DoesNotContain(second.Collectibles, p => p.Id == pile.Id);
        Assert.All(second.Collectibles.Where(p => p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal)),
            p => Assert.Contains(left, l => l.Id == p.Id && l.Position == p.Position)); // no restock on the first tick

        // A fresh sector, by contrast, has the pile again: that is exactly what a relaunch used to give.
        Assert.Contains(ShippedWorld(salt: 1).Collectibles, p => p.Id == pile.Id);
    }

    [Fact]
    public void ContinuedMineralSpawns_KeepTheirTimersAndNeverReuseAPileId()
    {
        var first = ShippedWorld(salt: 3);
        first.Tick(default);
        var mineral = first.Collectibles.First(p => p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal));
        first.RestorePlayer(mineral.Position, 0, first.Player.Hull);
        first.Tick(default);
        var snapshot = first.CaptureSector();
        Assert.All(snapshot.MineralSpawns, s => Assert.True(s.TicksUntilAttempt >= 1));

        var second = ShippedWorld(salt: 4);
        Assert.True(second.RestoreSector(snapshot));
        var seen = new HashSet<string>(first.Collectibles.Select(p => p.Id).Append(mineral.Id));
        for (var tick = 0; tick < 20_000; tick++)
        {
            second.Tick(default);
            foreach (var p in second.Collectibles) seen.Add(p.Id);
        }
        var replay = ShippedWorld(salt: 4);
        replay.RestoreSector(snapshot);
        TestWorld.Tick(replay, 20_000);
        Assert.Equal(Piles(second), Piles(replay)); // placement continues from the saved random state, not the world salt
        Assert.DoesNotContain(second.Collectibles, p => p.Id == mineral.Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(600)]
    [InlineData(20_000)]
    public void MineralSpawns_RunWhileDocked_ExactlyAsInFlight(int ticks)
    {
        // Owner rule, Nov 2 2026: mineral respawns keep running in real time while docked or paused. The ship is parked far
        // from every spawn so flight cannot sweep a pile up and both worlds see the same clear-radius checks.
        WorldSimulation Parked()
        {
            var world = ShippedWorld(salt: 9);
            world.Tick(default);
            foreach (var pile in world.Collectibles.Where(p => p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal)).Take(40).ToArray())
            {
                world.RestorePlayer(pile.Position, 0, world.Player.Hull);
                world.Tick(default);
            }
            world.RestorePlayer(new Vector2(1e6f, 1e6f), 0, world.Player.Hull);
            world.Tick(default);
            return world;
        }
        var flying = Parked();
        var docked = Parked();
        var tick = docked.TickNumber;
        TestWorld.Tick(flying, ticks);
        docked.ElapseWithoutTicking(ticks);
        Assert.Equal(tick, docked.TickNumber);
        Assert.Equal(Piles(flying), Piles(docked));
        Assert.Equal(flying.CaptureSector().MineralSpawns, docked.CaptureSector().MineralSpawns);
        Assert.Equal(flying.CaptureSector().MineralRandomState, docked.CaptureSector().MineralRandomState);
    }

    [Fact]
    public void ALongAbsence_RefillsEachSpawnOnlyToItsLimit()
    {
        var world = ShippedWorld(salt: 10);
        world.Tick(default);
        var stationPiles = world.Collectibles.Count(p => !p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal));
        foreach (var pile in world.Collectibles.Where(p => p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal)).Take(30).ToArray())
        {
            world.RestorePlayer(pile.Position, 0, world.Player.Hull);
            world.Tick(default);
        }
        world.RestorePlayer(new Vector2(1e6f, 1e6f), 0, world.Player.Hull);
        world.ElapseWithoutTicking(int.MaxValue); // about 414 days closed
        foreach (var spawn in world.MineralSpawns)
            Assert.True(world.Collectibles.Count(p => p.Id.StartsWith($"{WorldSimulation.MineralPilePrefix}{spawn.Id}:", StringComparison.Ordinal)) <= spawn.ClumpQuantity);
        Assert.True(world.Collectibles.Count(p => p.ItemId == "2003") <= 100);
        Assert.True(world.Collectibles.Count(p => p.ItemId == "2005") <= 50);
        Assert.True(world.Collectibles.Count(p => !p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal)) <= stationPiles);
    }

    [Fact]
    public void ADrop_IsThereAt719Seconds_AndGoneAt720_AcrossASaveAndReload()
    {
        var first = ShippedWorld(salt: 11);
        first.Tick(default);
        var drop = first.Collectibles.First(p => p.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal));
        first.ElapseWithoutTicking(600 * WorldSimulation.TickRate); // 10 minutes docked
        var snapshot = first.CaptureSector();
        Assert.Equal(120 * WorldSimulation.TickRate, snapshot.Piles.Single(p => p.Id == drop.Id).ExpiresInTicks);

        var second = ShippedWorld(salt: 12);
        Assert.True(second.RestoreSector(snapshot));
        second.ElapseWithoutTicking(119 * WorldSimulation.TickRate);
        Assert.Contains(second.Collectibles, p => p.Id == drop.Id); // 719 s after it landed
        second.ElapseWithoutTicking(WorldSimulation.TickRate);
        Assert.DoesNotContain(second.Collectibles, p => p.Id == drop.Id); // 720 s
    }

    [Fact]
    public void TheSectorCaps_HoldAcrossSavesAndReloads()
    {
        var world = ShippedWorld(salt: 13);
        world.PlayersInSector = 100; // every spawn runs, so the f[19] caps are what holds the field back
        world.Tick(default);
        for (var round = 1; round <= 12; round++)
        {
            world.ElapseWithoutTicking(1200 * WorldSimulation.TickRate);
            var next = ShippedWorld(salt: (ulong)(100 + round));
            next.PlayersInSector = 100;
            Assert.True(next.RestoreSector(world.CaptureSector()));
            world = next;
            Assert.True(world.Collectibles.Count(p => p.ItemId == "2005") <= 50);
            Assert.True(world.Collectibles.Count(p => p.ItemId == "2019") <= 100);
            Assert.True(world.Collectibles.Count(p => p.ItemId == "2003" && !p.Id.StartsWith("mineral:85:", StringComparison.Ordinal)) <= 100 + 14);
        }
    }

    [Fact]
    public void DestroyedTargets_StayDownAcrossWorlds_UntilTheRealTimeDelayRunsOut()
    {
        var first = TargetWorld();
        FireUntilKill(first);
        var killTick = first.Kills[0].Tick;
        TestWorld.Tick(first, 600); // 10 s later
        var snapshot = first.CaptureSector();
        Assert.Equal(50 * WorldSimulation.TickRate, snapshot.TargetRespawnTicksRemaining);

        var second = TargetWorld();
        Assert.True(second.RestoreSector(snapshot));
        Assert.True(second.Targets[0].IsDestroyed);
        Assert.Equal(0, second.TargetWave);
        for (var tick = 0; tick < 600; tick++) second.Tick(new ShipInput(0, 0, true));
        Assert.Empty(second.Kills); // nothing to shoot: the relaunched sector did not re-arm the target

        // 41 s pass with nobody in the sector (the client was closed): the group is due and comes back as wave 1.
        second.ElapseWithoutTicking(41 * WorldSimulation.TickRate);
        Assert.False(second.Targets[0].IsDestroyed);
        Assert.Equal(1, second.TargetWave);
        FireUntilKill(second);
        Assert.NotEqual(StationProfileSession.KillTransactionId("s", first.Kills[0].Wave, "t1"),
            StationProfileSession.KillTransactionId("s", second.Kills[0].Wave, "t1"));
        Assert.True(killTick > 0);
    }

    [Fact]
    public void Restore_RefusesAnotherSector_AndOnlyBeforeTheFirstTick()
    {
        var world = TargetWorld();
        var other = new WorldSimulation(TestWorld.Catalog(sector: new SectorDefinition { Id = "elsewhere" }));
        Assert.False(world.RestoreSector(other.CaptureSector()));
        world.Tick(default);
        Assert.Throws<InvalidOperationException>(() => world.RestoreSector(TargetWorld().CaptureSector()));
    }

    [Fact]
    public void Restore_ClampsOddValues_AndDropsPilesOfUnknownItems()
    {
        var world = ShippedWorld(salt: 5);
        var snapshot = world.CaptureSector() with
        {
            Targets = [new TargetSnapshot("target_alpha", float.NaN, 1e9f)],
            Piles = [new PileSnapshot("ghost", "not-an-item", 5, 0, 0, 5), new PileSnapshot("dup", "2003", 1, 0, 0, 5), new PileSnapshot("dup", "2003", 9, 0, 0, 5)],
        };
        var restored = ShippedWorld(salt: 6);
        Assert.True(restored.RestoreSector(snapshot));
        var alpha = restored.Targets.Single(t => t.Id == "target_alpha");
        Assert.Equal(alpha.Definition.Defense.MaximumHull, alpha.Hull);
        Assert.Equal(alpha.Definition.Defense.MaximumShield, alpha.Shield);
        Assert.Equal(new[] { ("dup", 1) }, restored.Collectibles.Select(p => (p.Id, p.Quantity)));
    }

    // ---- Store ----------------------------------------------------------------------------------------------------------

    private static SectorStateDocument Document(WorldSimulation world, DateTimeOffset at) => new("instance-1", at, world.CaptureSector(),
        [new PendingPickupGrant("pilot", "world-1", new CargoPickup(0, "wreckage_1", "2003", 1, 5))],
        [new PendingKillGrant("pilot", "instance-1", new TargetKill("t1", WorldSimulation.PlayerShipId, 9, 0))]);

    [Fact]
    public void Store_RoundTripsTheSector_AndKeepsOneGenerationBack()
    {
        using var dir = new TempDirectory();
        var time = new FixedTimeProvider(new DateTimeOffset(2026, 11, 2, 6, 0, 0, TimeSpan.Zero));
        var store = new SectorStateStore(dir.Path, time);
        Assert.Equal(SectorStateLoadStatus.NotFound, store.Load("first_contact").Status);

        var world = ShippedWorld(salt: 7);
        world.Tick(default);
        var saved = Document(world, time.GetUtcNow());
        store.Save(saved);
        var loaded = store.Load(world.Catalog.Sector.Id);
        Assert.Equal(SectorStateLoadStatus.Loaded, loaded.Status);
        var document = loaded.Document!;
        Assert.Equal(saved.SectorInstanceId, document.SectorInstanceId);
        Assert.Equal(saved.SavedAtUtc, document.SavedAtUtc);
        Assert.Equal(saved.PendingPickups, document.PendingPickups);
        Assert.Equal(saved.PendingKills, document.PendingKills);
        var copy = ShippedWorld(salt: 8);
        Assert.True(copy.RestoreSector(document.Sector));
        Assert.Equal(Piles(world), Piles(copy));
        Assert.Equal(world.CaptureSector().MineralRandomState, document.Sector.MineralRandomState);

        // A second save keeps the first as the backup; a damaged primary falls back to it and is quarantined.
        store.Save(saved with { SectorInstanceId = "instance-2" });
        var primary = Directory.GetFiles(Path.Combine(dir.Path, "world"), "*.sector.json").Single();
        File.WriteAllText(primary, "{ not json");
        var recovered = store.Load(world.Catalog.Sector.Id);
        Assert.Equal(SectorStateLoadStatus.RecoveredFromBackup, recovered.Status);
        Assert.Equal("instance-1", recovered.Document!.SectorInstanceId);
        Assert.Single(Directory.GetFiles(Path.Combine(dir.Path, "world", "quarantine")));
        Assert.Equal(SectorStateLoadStatus.Loaded, store.Load(world.Catalog.Sector.Id).Status);
    }

    [Fact]
    public void Store_QuarantinesUnreadableFiles_AndNeverServesAnotherSector()
    {
        using var dir = new TempDirectory();
        var store = new SectorStateStore(dir.Path);
        var world = TargetWorld();
        store.Save(Document(world, DateTimeOffset.UnixEpoch));
        var folder = Path.Combine(dir.Path, "world");
        var file = Directory.GetFiles(folder, "*.sector.json").Single();
        File.Copy(file, Path.Combine(folder, "elsewhere.sector.json"));
        Assert.Equal(SectorStateLoadStatus.Corrupt, store.Load("elsewhere").Status);

        File.WriteAllText(file, "[]");
        var result = store.Load(world.Catalog.Sector.Id);
        Assert.Equal(SectorStateLoadStatus.Corrupt, result.Status);
        Assert.Null(result.Document);
        Assert.False(File.Exists(file));
        Assert.Equal(2, Directory.GetFiles(Path.Combine(folder, "quarantine")).Length);
    }

    // ---- Carrying the practice sector over to the full-size map (review R1-1 on #118) ---------------------------------------

    private const string Legacy = "first_contact", Rogue = "rogue";

    private static SectorStateDocument AsSector(SectorStateDocument document, string sectorId, string instance) =>
        document with { SectorInstanceId = instance, Sector = document.Sector with { SectorId = sectorId } };

    [Fact]
    public void LegacySector_IsCarriedOverOnlyWhileTheMapSectorWasNeverSaved_AndRetiredOnItsFirstSave()
    {
        using var dir = new TempDirectory();
        var store = new SectorStateStore(dir.Path);
        store.Save(AsSector(Document(TargetWorld(), DateTimeOffset.UnixEpoch), Legacy, "legacy-instance"));

        // Never saved: carried over, again on a relaunch before the first save (a crash loses nothing).
        foreach (var attempt in new[] { store, new SectorStateStore(dir.Path) })
        {
            var moved = attempt.LoadOrMigrate(Rogue, Legacy);
            Assert.Equal(SectorStateLoadStatus.Migrated, moved.Status);
            Assert.Equal((Rogue, "legacy-instance"), (moved.Document!.Sector.SectorId, moved.Document.SectorInstanceId));
        }

        // The first save under the new id retires the legacy save; it is kept in world/retired, never deleted.
        var carried = store.LoadOrMigrate(Rogue, Legacy).Document!;
        store.Save(carried);
        Assert.Equal(SectorStateLoadStatus.NotFound, store.Load(Legacy).Status);
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(dir.Path, "world", "retired")));
        Assert.Equal(SectorStateLoadStatus.Loaded, new SectorStateStore(dir.Path).LoadOrMigrate(Rogue, Legacy).Status);
    }

    [Fact]
    public void ACorruptMapSector_NeverRewindsToTheLegacySave_OnThisOrAnyLaterLoad()
    {
        using var dir = new TempDirectory();
        var store = new SectorStateStore(dir.Path);
        var world = TargetWorld();
        store.Save(AsSector(Document(world, DateTimeOffset.UnixEpoch), Legacy, "legacy-instance"));
        store.Save(AsSector(Document(world, DateTimeOffset.UnixEpoch), Rogue, "rogue-1"));
        store.Save(AsSector(Document(world, DateTimeOffset.UnixEpoch), Rogue, "rogue-2")); // primary and backup both exist
        var folder = Path.Combine(dir.Path, "world");
        foreach (var file in Directory.GetFiles(folder, "rogue.sector*.json")) File.WriteAllText(file, "{ not json");

        var first = store.LoadOrMigrate(Rogue, Legacy);
        Assert.Equal(SectorStateLoadStatus.Corrupt, first.Status);
        Assert.Null(first.Document);
        // The second load, and a relaunch with a new store, find no map sector file, but the quarantine remembers it: the
        // sector starts afresh and the legacy state is never restored.
        foreach (var later in new[] { store, new SectorStateStore(dir.Path) })
        {
            var again = later.LoadOrMigrate(Rogue, Legacy);
            Assert.Equal(SectorStateLoadStatus.NotFound, again.Status);
            Assert.Null(again.Document);
            Assert.True(later.EverSaved(Rogue));
        }
        Assert.Equal(SectorStateLoadStatus.NotFound, store.Load(Legacy).Status); // retired at the corrupt load
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(folder, "retired")));
    }

    [Fact]
    public void ARecoveredMapSector_IsUsed_AndTheLegacySaveIsLeftAlone()
    {
        using var dir = new TempDirectory();
        var store = new SectorStateStore(dir.Path);
        var world = TargetWorld();
        store.Save(AsSector(Document(world, DateTimeOffset.UnixEpoch), Legacy, "legacy-instance"));
        store.Save(AsSector(Document(world, DateTimeOffset.UnixEpoch), Rogue, "rogue-1"));
        store.Save(AsSector(Document(world, DateTimeOffset.UnixEpoch), Rogue, "rogue-2"));
        File.WriteAllText(Path.Combine(dir.Path, "world", "rogue.sector.json"), "{ not json");
        var recovered = store.LoadOrMigrate(Rogue, Legacy);
        Assert.Equal(SectorStateLoadStatus.RecoveredFromBackup, recovered.Status);
        Assert.Equal("rogue-1", recovered.Document!.SectorInstanceId);
    }

    // ---- Owed rewards ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task PickupsOwedFromASavedSector_AreGrantedOnce_AfterARelaunch()
    {
        var session = new StationProfileSession(new InMemoryProfileRepository(), LoadStation(), "starter_interceptor", TestCargo.Capacity);
        Assert.True((await session.OpenOrCreateAsync("pilot")).Succeeded);
        var pickup = new CargoPickup(0, "wreckage_1", "2003", 1, 5);

        // The first process granted it, then crashed before the sector file dropped it from the owed list.
        var before = new PickupCommitQueue();
        before.Observe("world-1", [pickup]);
        Assert.Single((await before.CommitAsync(session)).Applied);

        var relaunched = new PickupCommitQueue();
        relaunched.Restore([("world-1", pickup), ("world-1", pickup)]);
        Assert.Equal(1, relaunched.Count);
        relaunched.ContinueInNewWorld();
        Assert.Equal(1, relaunched.Count); // a new world of the same sector keeps what is owed
        var outcome = await relaunched.CommitAsync(session);
        Assert.Single(outcome.Replayed);
        Assert.Empty(outcome.Applied);
        Assert.Equal(1, session.Profile!.Inventory["2003"]);
    }
}
