using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// Station defense satellites (RT-F278 GunSat, RT-F351..RT-F362): the twelve Rogue.lio hide rows that place VEH 410 for a team, read
/// with the emulator's hide loop, and the satellite firing lead-aimed ITM 1038 shots at enemy pilots. The readings that are NEW are
/// listed in the derived data; the feature entries stay open. The default test ship starts at (0, 0) heading up (negative y) at rest.
/// </summary>
public sealed class DefenseSatelliteTests
{
    private static string Data(params string[] parts) => File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "data", .. parts]));
    private static DefenseSatelliteCatalog LoadSatellites() => DefenseSatelliteCatalog.FromJson(Data("defenses", "defense_satellites.json"));

    private static string CommittedPath =>
        Path.Combine(OriginalAssets.RepoRoot() ?? throw new InvalidOperationException("Repository root not found."), "data", "defenses", "defense_satellites.json");

    // NEW fixture cannon: slow enough to count shots in flight, weak enough to count hits.
    private static readonly WeaponDefinition Cannon = new()
    {
        Id = "1038", Name = "PirateStationCannon", MuzzleVelocity = 600, ProjectileRadius = 4, LifetimeSeconds = 3, Damage = 5, ShieldDrain = 250,
        EnergyCost = 0, InheritsShipVelocity = false,
    };

    // NEW fixture satellite with the GunSat's timing: 15-tick shots, 5-shot clip, 90-tick reload, 0.5 hull every 30 ticks from 5 hull up.
    private static DefenseSatelliteDefinition Satellite(float range = 1000, int repairInterval = 30) =>
        new("410", "GunSat", 50, 36, range, range, 15, 5, 90, repairInterval, 0.5f, 5, true, Cannon);

    // A point area 300 units ahead of the practice sector's spawn, attempting every 600 ticks, one satellite per area.
    private static DefenseSatelliteSpawnDefinition Spawn(int team = 1, string id = "93", int attempt = 600, float x = 0, float y = -300) =>
        new(id, "NorthernDefSatA", team, x, y, 0, 0, attempt, attempt, MaxInArea: 1);

    private static DefenseSatelliteCatalog Catalog(DefenseSatelliteSpawnDefinition spawn, DefenseSatelliteDefinition? satellite = null) =>
        new(satellite ?? Satellite(), [spawn], 7);

    private static WorldSimulation World(DefenseSatelliteCatalog? satellites, int? team = 0, SectorMap? map = null, GameCatalog? catalog = null) =>
        new(catalog ?? Gun(), map: map, satellites: satellites) { PlayerTeam = team };

    private static void Run(WorldSimulation world, int ticks, ShipInput input = default)
    {
        for (var i = 0; i < ticks; i++) world.Tick(input);
    }

    /// <summary>
    /// The external test ship at (0, 0) heading up in an empty sector (no practice targets or asteroids in the line of fire), with a
    /// player cannon of <paramref name="damage"/> hull damage when given (NEW fixture).
    /// </summary>
    private static GameCatalog Gun(float? damage = null)
    {
        var c = TestWorld.ExternalCatalog();
        return new GameCatalog(c.Ship, damage is { } d ? c.Weapon with { Damage = d } : c.Weapon, new SectorDefinition { PlayerSpawn = c.Sector.PlayerSpawn });
    }

    /// <summary>Fires one player shot straight up and ticks until it lands on the satellite; returns the satellite.</summary>
    private static DefenseSatelliteState ShootOnce(WorldSimulation world)
    {
        var sat = world.DefenseSatellites[0];
        var before = sat.Hull;
        world.Tick(new ShipInput(0, 0, true));
        for (var i = 0; i < 120 && sat.Hull == before; i++) world.Tick(default);
        Assert.NotEqual(before, sat.Hull);
        return sat;
    }

    private static int SatelliteShots(WorldSimulation world) =>
        world.Projectiles.Count(p => p.OwnerShipId.StartsWith(WorldSimulation.DefenseSatellitePrefix, StringComparison.Ordinal));

    // ---- Derived data ------------------------------------------------------------------------------------------------

    [RequiresOriginalAssetsFact]
    public void CommittedDefenseSatellites_MatchPinnedOriginals()
    {
        var derived = OriginalDefenseSatelliteBuilder.Build(OriginalAssets.Root!).ToJson();
        if (Environment.GetEnvironmentVariable("RT_WRITE_DERIVED_SATELLITES") == "1") File.WriteAllText(CommittedPath, derived);
        Assert.Equal(derived, File.ReadAllText(CommittedPath).ReplaceLineEndings("\n"));
    }

    [RequiresOriginalAssetsFact]
    public void OriginalSatelliteRows_AreTheTwelveTeamOwnedActor410Hides()
    {
        var satellites = OriginalDefenseSatellites.Load(OriginalAssets.Root!);

        Assert.Equal(Enumerable.Range(65, 12), satellites.Spawns.Select(r => r.Row));
        Assert.Equal(new[]
        {
            "ColdHarbourDefSatA", "ColdHarbourDefSatB", "ColdHarbourDefSatC", "NorthernDefSatA", "NorthernDefSatB", "NorthernDefSatC",
            "CassandraDefSatA", "CassandraDefSatB", "CassandraDefSatC", "YankeeDefStatA", "YankeeDefStatB", "YankeeDefStatC",
        }, satellites.Spawns.Select(r => r.Name));
        Assert.Equal(new[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3 }, satellites.Spawns.Select(r => r.F(25)));
        Assert.All(satellites.Spawns, r =>
        {
            Assert.Equal((100, 100), (r.F(6), r.F(7)));
            Assert.Equal((1, 6000, 6000, 1), (r.F(10), r.F(11), r.F(12), r.F(13)));
            Assert.Equal((0, 500, 0, 99999, 1, -1), (r.F(14), r.F(15), r.F(16), r.F(17), r.F(18), r.F(19)));
            Assert.Equal(-410, r.F(20));
        });
        Assert.Equal((1504, 3920), (satellites.Spawns[0].F(4), satellites.Spawns[0].F(5)));

        var gunSat = satellites.Record;
        Assert.Equal(("GunSat", 50, 36), (gunSat.Name, gunSat.Hitpoints.Value, gunSat.PhysicalRadius.Value));
        Assert.Equal((2500, 2500, 50, 5, 1, 1), (gunSat.TrackingRadius.Value, gunSat.FireRadius.Value, gunSat.RepairRate.Value,
            gunSat.HitpointsRequiredToOperate.Value, gunSat.ObeyLos.Value, gunSat.Destroyable.Value));
        Assert.Equal((1038, "PirateStationCannon"), (satellites.Weapon.Id, satellites.Weapon.Name));
        Assert.Equal((25, 5, 150, 4000), (satellites.Projectile.FireDelay.Value, satellites.Projectile.AmmoCapacity.Value,
            satellites.ReloadDelayNormal.Value, satellites.Projectile.MuzzleVelocity.Value));
    }

    [Fact]
    public void ShippedSatellites_ReadTheRawFieldsAsTheEmulatorDoes()
    {
        var catalog = LoadSatellites();

        Assert.Equal(12, catalog.Spawns.Count);
        Assert.Equal(new[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3 }, catalog.Spawns.Select(s => s.Team));
        Assert.All(catalog.Spawns, s =>
        {
            // f[11]/f[12] are tenths of a second: 6000 = 600 s = 36,000 ticks; one satellite per 100 x 100 area.
            Assert.Equal((36000, 36000, 1, -1, 1), (s.AttemptTicks, s.SucceedTicks, s.MaxInArea, s.MaxInLevel, s.InitialCount));
            Assert.Equal((100f, 100f, 0f, 0f), (s.Width, s.Height, s.MinPlayerDistance, s.MaxPlayerDistance));
        });
        // LIO positions less the 48-pixel LVL offset.
        Assert.Equal(("90", "ColdHarbourDefSatA", 1456f, 3872f), (catalog.Spawns[0].Id, catalog.Spawns[0].Name, catalog.Spawns[0].X, catalog.Spawns[0].Y));

        var sat = catalog.Satellite;
        Assert.Equal(("410", "GunSat", 50f, 36f, 2500f, 2500f), (sat.VehicleId, sat.Name, sat.MaximumHull, sat.Radius, sat.TrackingRadius, sat.FireRadius));
        // 10 ms units: fireDelay 25 = 15 ticks, reload 150 = 90 ticks, RepairRate 50 = 0.5 hull every 30 ticks.
        Assert.Equal((15, 5, 90, 30, 0.5f, 5f, true), (sat.FireIntervalTicks, sat.ClipSize, sat.ReloadTicks, sat.RepairIntervalTicks,
            sat.RepairAmount, sat.HullToOperate, sat.ObeyLos));
        Assert.Equal("1038", sat.Cannon.Id);
        // Plasma Cannon scale: 2200 raw -> 700 px/s, 550 -> 2.5 s, 4 -> 3.
        Assert.Equal(MathF.Round(4000 * 700f / 2200, 3), sat.Cannon.MuzzleVelocity);
        Assert.Equal(MathF.Round(550 * 2.5f / 550, 3), sat.Cannon.LifetimeSeconds);
        Assert.Equal(MathF.Round(4 * 3f / 4, 3), sat.Cannon.ProjectileRadius);
        Assert.False(sat.Cannon.InheritsShipVelocity);
    }

    [Fact]
    public void ShippedSatellites_SitOnOpenTilesOfTheRogueMap()
    {
        var catalog = LoadSatellites();
        Assert.All(catalog.Spawns, s => Assert.False(TestWorld.RogueMap.IsBlocked(new Vector2(s.X, s.Y)), s.Name));
    }

    // ---- Spawning ----------------------------------------------------------------------------------------------------

    [Fact]
    public void FirstTick_PlacesOneSatellitePerSpawnerWithAFullHull()
    {
        var world = World(Catalog(Spawn()));
        Run(world, 1);

        var sat = Assert.Single(world.DefenseSatellites);
        Assert.Equal(("defense-sat:93:1", "93", 1, new Vector2(0, -300), 50f), (sat.Id, sat.SpawnId, sat.Team, sat.Position, sat.Hull));
        Run(world, 2000);
        Assert.Single(world.DefenseSatellites); // MaxTypeInArea 1: the spawner adds no second one while the first stands
    }

    [Fact]
    public void NoSpawnersOrNoCatalog_NoSatellites()
    {
        var world = World(null);
        Run(world, 100);
        Assert.Empty(world.DefenseSatellites);
        Assert.Null(world.DefenseSatellite);
    }

    // ---- Firing ------------------------------------------------------------------------------------------------------

    [Fact]
    public void AnEnemySatellite_FiresAClipThenReloads_AndItsShotsHitThePlayer()
    {
        // A satellite 300 units away fires a shot every 15 ticks; 600 units/s reach the player in under half a second.
        var world = World(Catalog(Spawn(team: 1)), team: 0);
        var startShield = world.Player.ShieldLeft;
        Run(world, 1);
        var shot = Assert.Single(world.Projectiles);
        Assert.Equal(("defense-sat:93:1", "1038"), (shot.OwnerShipId, shot.WeaponId));
        Assert.Equal(new Vector2(0, -290), shot.Position); // fired from the satellite and moved one tick toward the player

        // Five shots at ticks 1, 16, 31, 46 and 61; the clip is then empty and the sixth waits for the 90-tick reload (tick 151).
        var fired = new List<long>();
        long seen = 1;
        fired.Add(world.TickNumber);
        for (var i = 0; i < 200; i++)
        {
            world.Tick(default);
            var latest = world.Projectiles.Where(p => p.OwnerShipId.StartsWith("defense-sat:", StringComparison.Ordinal)).Select(p => p.Id).DefaultIfEmpty(0).Max();
            if (latest > seen)
            {
                seen = latest;
                fired.Add(world.TickNumber);
            }
        }
        Assert.Equal(new long[] { 1, 16, 31, 46, 61, 151, 166, 181, 196 }, fired);
        Assert.True(world.SatelliteHits >= 5);
        Assert.Equal(0, world.MeteorHits);
        Assert.Equal("defense-sat:93:1", world.Player.LastDamagedByShipId);
        Assert.True(world.Player.ShieldLeft < startShield);
    }

    [Fact]
    public void ASatelliteOfThePlayersTeam_NeverFiresAndTheirShotsPassThroughIt()
    {
        var world = World(Catalog(Spawn(team: 0)), team: 0);
        Run(world, 1);
        Run(world, 120, new ShipInput(0, 0, true)); // the player fires straight up at it
        Assert.Equal(0, SatelliteShots(world));
        Assert.Equal(50f, Assert.Single(world.DefenseSatellites).Hull);
        Assert.Empty(world.DefenseSatelliteKills);
    }

    [Fact]
    public void APilotOnNoTeam_IsAnEnemyOfEverySatellite()
    {
        var world = World(Catalog(Spawn(team: 0)), team: null);
        Run(world, 1);
        Assert.Equal(1, SatelliteShots(world));
    }

    [Fact]
    public void ASatellite_DoesNotFireBeyondItsRange()
    {
        var world = World(Catalog(Spawn(), Satellite(range: 200)));
        Run(world, 120);
        Assert.Single(world.DefenseSatellites);
        Assert.Equal(0, SatelliteShots(world));
    }

    [Fact]
    public void ASatellite_DoesNotFireThroughAWall()
    {
        // Open 40 x 40 tile map with a wall row at tile y = 10; the player at (320, 320) and the satellite above the wall at (320, 100).
        var tiles = new byte[40 * 40];
        for (var x = 0; x < 40; x++) tiles[10 * 40 + x] = SectorMap.BlockedBit;
        var map = new SectorMap(40, 40, tiles, [720, .. Enumerable.Repeat(120, 15)]);
        var catalog = TestWorld.Catalog(sector: new SectorDefinition { PlayerSpawn = new SpawnDefinition { X = 320, Y = 320 } });
        var hidden = World(Catalog(Spawn(x: 320, y: 100)), map: map, catalog: catalog);
        Run(hidden, 60);
        Assert.Single(hidden.DefenseSatellites);
        Assert.Equal(0, SatelliteShots(hidden));

        var open = World(Catalog(Spawn(x: 320, y: 200)), map: map, catalog: catalog);
        Run(open, 1);
        Assert.Equal(1, SatelliteShots(open));
    }

    [Fact]
    public void ASatellite_StopsFiringAtAWreck()
    {
        var world = World(Catalog(Spawn()));
        for (var i = 0; i < 60 * 120 && !world.Player.IsDestroyed; i++) world.Tick(default);
        Assert.True(world.Player.IsDestroyed);
        Run(world, 600);
        Assert.Equal(0, SatelliteShots(world)); // shots already in flight expire; no new ones at a wreck
    }

    // ---- Damage, repair and respawn -----------------------------------------------------------------------------------

    [Fact]
    public void ThePlayersShots_DestroyAnEnemySatellite_WhichComesBackOnlyOnALaterAttempt()
    {
        var world = World(Catalog(Spawn(attempt: 600)));
        Run(world, 1);
        for (var i = 0; i < 300 && world.DefenseSatellites.Count > 0; i++) world.Tick(new ShipInput(0, 0, true));

        Assert.Empty(world.DefenseSatellites);
        var kill = Assert.Single(world.DefenseSatelliteKills);
        Assert.Equal(("defense-sat:93:1", "93", 1, WorldSimulation.PlayerShipId), (kill.SatelliteId, kill.SpawnId, kill.Team, kill.ByShipId));
        Assert.Equal(0, world.DestroyedTargets); // not a practice-target kill
        Assert.Empty(world.Kills);

        // The spawner's next attempt is at tick 601 (one attempt delay after the first); nothing comes back before it.
        Run(world, (int)(600 - world.TickNumber));
        Assert.Empty(world.DefenseSatellites);
        Run(world, 1);
        Assert.Equal("defense-sat:93:2", Assert.Single(world.DefenseSatellites).Id);
    }

    [Fact]
    public void ADamagedSatellite_RepairsHalfAPointEvery30Ticks_AlsoWhileTheWorldIsNotTicking()
    {
        var world = World(Catalog(Spawn(), Satellite(range: 10)), catalog: Gun(28));
        Run(world, 1);
        var sat = ShootOnce(world);
        Assert.Equal(22f, sat.Hull);
        Run(world, 30);
        Assert.Equal(22.5f, sat.Hull);
        Run(world, 60);
        Assert.Equal(23.5f, sat.Hull);
        world.ElapseWithoutTicking(300); // docked: ten more repairs
        Assert.Equal(28.5f, sat.Hull);
        Run(world, 3600);
        Assert.Equal(50f, sat.Hull); // never above the maximum
    }

    [Fact]
    public void BelowItsOperatingHull_ASatelliteNeitherRepairsNorFires()
    {
        var world = World(Catalog(Spawn(team: 0)), team: null, catalog: Gun(46));
        Run(world, 1);
        var sat = ShootOnce(world);
        Assert.Equal(4f, sat.Hull);
        var shots = world.Projectiles.Where(p => p.OwnerShipId.StartsWith("defense-sat:", StringComparison.Ordinal)).Select(p => p.Id).DefaultIfEmpty(0).Max();
        Run(world, (int)(600 - world.TickNumber)); // up to the spawner's next attempt at tick 601
        Assert.Equal(4f, sat.Hull);
        Assert.Same(sat, Assert.Single(world.DefenseSatellites));
        Assert.Equal(0, world.Projectiles.Count(p => p.Id > shots && p.OwnerShipId.StartsWith("defense-sat:", StringComparison.Ordinal)));
    }

    [Fact]
    public void ADisabledSatellite_IsReplacedAtTheSpawnersNextAttempt_AndNotBefore()
    {
        // NEW (pending Steve's ruling): the emulator would leave it disabled, blocking its own respawn, for as long as the server runs.
        var world = World(Catalog(Spawn(team: 0, attempt: 600)), team: null, catalog: Gun(46));
        Run(world, 1);
        var sat = ShootOnce(world);
        Assert.Equal(4f, sat.Hull);

        Run(world, (int)(600 - world.TickNumber));
        Assert.Same(sat, Assert.Single(world.DefenseSatellites));
        Run(world, 1);
        var replacement = Assert.Single(world.DefenseSatellites);
        Assert.Equal(("defense-sat:93:2", 50f), (replacement.Id, replacement.Hull));
        Assert.Empty(world.DefenseSatelliteKills); // replaced, not destroyed: no kill notice

        // Also while docked: shot down to 4 again, the next attempt replaces it without a tick.
        var again = ShootOnce(world);
        Assert.Equal(4f, again.Hull);
        world.ElapseWithoutTicking(600);
        Assert.Equal("defense-sat:93:3", Assert.Single(world.DefenseSatellites).Id);
    }

    [Fact]
    public void AnOperatingSatellite_IsNeverReplaced()
    {
        var world = World(Catalog(Spawn(team: 0, attempt: 600)), team: null, catalog: Gun(28));
        Run(world, 1);
        var sat = ShootOnce(world);
        Assert.Equal(22f, sat.Hull);
        Run(world, 1300);
        Assert.Same(sat, Assert.Single(world.DefenseSatellites));
    }

    // ---- Real time and persistence -----------------------------------------------------------------------------------

    [Fact]
    public void SatelliteSpawners_KeepRunningWhileTheWorldIsNotTicking()
    {
        var world = World(Catalog(Spawn(attempt: 600)));
        Run(world, 1);
        for (var i = 0; i < 300 && world.DefenseSatellites.Count > 0; i++) world.Tick(new ShipInput(0, 0, true));
        Assert.Empty(world.DefenseSatellites);

        // Docked for 20 minutes: the spawner's attempts at ticks 601, 1201... happen meanwhile; one satellite comes back and no shot is fired.
        var shots = world.Projectiles.Count;
        world.ElapseWithoutTicking(72000);
        var sat = Assert.Single(world.DefenseSatellites);
        Assert.Equal("defense-sat:93:2", sat.Id);
        Assert.True(world.Projectiles.Count <= shots);
    }

    [Fact]
    public void ARestoredSector_KeepsItsSatellitesHullAndTimers()
    {
        var catalog = Catalog(Spawn(attempt: 600, team: 0));
        var world = World(catalog, team: 1, catalog: Gun(28));
        Run(world, 1);
        ShootOnce(world);
        var tick = world.TickNumber;
        var snapshot = world.CaptureSector();
        var saved = Assert.Single(snapshot.DefenseSatellites!);
        Assert.Equal("defense-sat:93:1", saved.Id);
        Assert.InRange(saved.Hull, 22f, 22.5f);
        Assert.Equal(601 - tick, Assert.Single(snapshot.SatelliteSpawns!).TicksUntilAttempt);

        // A relaunch neither repairs it nor adds a second one.
        var next = World(catalog);
        Assert.True(next.RestoreSector(snapshot));
        var restored = Assert.Single(next.DefenseSatellites);
        Assert.Equal(("defense-sat:93:1", 0, saved.Hull), (restored.Id, restored.Team, restored.Hull));

        // A satellite destroyed before the save stays gone until its spawner's next attempt.
        var destroyed = snapshot with { DefenseSatellites = [] };
        var empty = World(catalog);
        Assert.True(empty.RestoreSector(destroyed));
        Run(empty, (int)(600 - tick));
        Assert.Empty(empty.DefenseSatellites);
        Run(empty, 1);
        Assert.Equal("defense-sat:93:2", Assert.Single(empty.DefenseSatellites).Id);
    }

    [Fact]
    public void ASectorSavedBeforeSatellites_StartsItsSpawnersFresh()
    {
        var catalog = Catalog(Spawn());
        var old = World(null).CaptureSector() with { SatelliteSpawns = null, DefenseSatellites = null };
        var world = World(catalog);
        Assert.True(world.RestoreSector(old));
        Run(world, 1);
        Assert.Single(world.DefenseSatellites);
    }

    [Fact]
    public void ACatalog_RejectsBrokenDefinitions()
    {
        Assert.Throws<ArgumentException>(() => new DefenseSatelliteCatalog(Satellite() with { MaximumHull = 0 }, [Spawn()], 1));
        Assert.Throws<ArgumentException>(() => new DefenseSatelliteCatalog(Satellite(), [Spawn(), Spawn()], 1));
        Assert.Throws<ArgumentException>(() => new DefenseSatelliteCatalog(Satellite(), [Spawn(team: -1)], 1));
        Assert.Throws<ArgumentException>(() => new DefenseSatelliteCatalog(Satellite(), [Spawn(attempt: 0)], 1));
    }
}
