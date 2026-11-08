using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F061 meteor showers: the four Rogue.lio hide rows that place VEH 402, read with the emulator's hide loop, and the shower firing
/// lead-aimed ITM 1007 meteors at the player. The readings that are NEW are listed in the derived data; RT-F061 stays open.
/// </summary>
public sealed class MeteorShowerTests
{
    private static string Data(params string[] parts) => File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "data", .. parts]));
    private static MeteorShowerCatalog LoadShowers() => MeteorShowerCatalog.FromJson(Data("hazards", "meteor_showers.json"));

    private static string CommittedPath =>
        Path.Combine(OriginalAssets.RepoRoot() ?? throw new InvalidOperationException("Repository root not found."), "data", "hazards", "meteor_showers.json");

    // NEW fixture meteor: fast, short-lived and harmless enough to count hits.
    private static readonly WeaponDefinition Meteor = new()
    {
        Id = "1007", Name = "Meteor", MuzzleVelocity = 600, ProjectileRadius = 10, LifetimeSeconds = 5, Damage = 15, ShieldDrain = 750, EnergyCost = 0,
        InheritsShipVelocity = false,
    };

    private static MeteorShowerCatalog Catalog(MeteorShowerSpawnDefinition spawn, long lifetimeTicks = 300, float fireRadius = 20000, int interval = 21) =>
        new(new MeteorShowerDefinition("402", fireRadius, interval, lifetimeTicks, Meteor), [spawn], 7);

    // A point area 400 units ahead of the practice sector's spawn, attempting every 60 ticks.
    private static MeteorShowerSpawnDefinition Spawn(string id = "46", float minDistance = 0, int maxInLevel = 3, int attempt = 60, int succeed = 60,
        float x = 0, float y = -400) => new(id, x, y, 0, 0, attempt, succeed, MinPlayerDistance: minDistance, MaxInLevel: maxInLevel,
        Announcement: "Meteor Shower Detected!");

    private static WorldSimulation World(MeteorShowerCatalog? meteors) => new(TestWorld.ExternalCatalog(), meteors: meteors);

    private static void Run(WorldSimulation world, int ticks)
    {
        for (var i = 0; i < ticks; i++) world.Tick(default);
    }

    // ---- Derived data ------------------------------------------------------------------------------------------------

    [RequiresOriginalAssetsFact]
    public void CommittedMeteorShowers_MatchPinnedOriginals()
    {
        var derived = OriginalMeteorShowerBuilder.Build(OriginalAssets.Root!).ToJson();
        if (Environment.GetEnvironmentVariable("RT_WRITE_DERIVED_METEORS") == "1") File.WriteAllText(CommittedPath, derived);
        Assert.Equal(derived, File.ReadAllText(CommittedPath).ReplaceLineEndings("\n"));
    }

    [RequiresOriginalAssetsFact]
    public void OriginalMeteorShowerRows_AreTheFourActor402Hides()
    {
        var rows = OriginalMeteorShowerSpawns.Load(OriginalAssets.Root!);

        Assert.Equal(new[] { 18, 49, 52, 57 }, rows.Select(r => r.Row));
        Assert.Equal(new[] { "MeteorShowerA", "MeteorShowerB", "MeteorShowerC", "MeteorShowerD" }, rows.Select(r => r.Name));
        Assert.All(rows, r =>
        {
            Assert.Equal((16064, 13872, 15000, 15000), (r.F(4), r.F(5), r.F(6), r.F(7)));
            Assert.Equal((1, 1, 0, 500, 500, 99999, -1, 3), (r.F(10), r.F(13), r.F(14), r.F(15), r.F(16), r.F(17), r.F(18), r.F(19)));
            Assert.Equal(-402, r.F(20));
            Assert.Equal((0, 9999), (r.F(24), r.F(25)));
            Assert.Equal("Meteor Shower Detected!", r.Announcement);
        });
        Assert.Equal(new[] { (6000, 6000), (6000, 12000), (3000, 6000), (3000, 6000) }, rows.Select(r => (r.F(11), r.F(12))));
    }

    [Fact]
    public void ShippedMeteorShowers_ReadTheRawFieldsAsTheEmulatorDoes()
    {
        var catalog = LoadShowers();

        Assert.Equal(4, catalog.Spawns.Count);
        Assert.All(catalog.Spawns, s =>
        {
            Assert.Equal((16016f, 13824f, 15000f, 15000f), (s.X, s.Y, s.Width, s.Height));
            Assert.Equal((500f, 0f, 3), (s.MinPlayerDistance, s.MaxPlayerDistance, s.MaxInLevel));
            Assert.Equal("Meteor Shower Detected!", s.Announcement);
        });
        // f[11]/f[12] are tenths of a second: 6000 = 600 s = 36,000 ticks.
        Assert.Equal((36000, 36000), (catalog.Spawns.Single(s => s.Id == "46").AttemptTicks, catalog.Spawns.Single(s => s.Id == "46").SucceedTicks));
        var shower = catalog.Shower;
        Assert.Equal(("402", 20000f, 21), (shower.VehicleId, shower.FireRadius, shower.FireIntervalTicks));
        Assert.Equal(300, shower.LifetimeTicks); // RemoveGlobalTimer 5 read as seconds (NEW, Steve's call pending)
        Assert.Equal(("1007", 15f, 750f), (shower.Meteor.Id, shower.Meteor.Damage, shower.Meteor.ShieldDrain));
        // Plasma Cannon scale: 2200 raw -> 700 px/s, 550 -> 2.5 s, 4 -> 3.
        Assert.Equal(MathF.Round(4250 * 700f / 2200, 3), shower.Meteor.MuzzleVelocity);
        Assert.Equal(MathF.Round(2000 * 2.5f / 550, 3), shower.Meteor.LifetimeSeconds);
        Assert.Equal(MathF.Round(56 * 3f / 4, 3), shower.Meteor.ProjectileRadius);
    }

    // ---- Spawning ----------------------------------------------------------------------------------------------------

    [Fact]
    public void FirstTick_PlacesAShowerAndAnnouncesIt()
    {
        var world = World(Catalog(Spawn()));
        Run(world, 1);

        var shower = Assert.Single(world.MeteorShowers);
        Assert.Equal(new Vector2(0, -400), shower.Position);
        Assert.Equal("meteor-shower:46:1", shower.Id);
        var appeared = Assert.Single(world.MeteorShowerEvents);
        Assert.Equal(("Meteor Shower Detected!", shower.Id, 1L), (appeared.Announcement, appeared.ShowerId, appeared.Tick));
    }

    [Fact]
    public void NoShower_WhileThePlayerIsNearTheAreaCentre()
    {
        var world = World(Catalog(Spawn(minDistance: 500)));
        Run(world, 200);

        Assert.Empty(world.MeteorShowers);
        Assert.Empty(world.MeteorShowerEvents);
    }

    [Fact]
    public void ShowersStopAtTheSectorLimit_CountingEveryShower()
    {
        var spawns = new[] { Spawn("a", maxInLevel: 3), Spawn("b", maxInLevel: 3, x: 300) };
        var world = World(new MeteorShowerCatalog(new MeteorShowerDefinition("402", 20000, 21, 0, Meteor), spawns, 1));
        Run(world, 60 * 10);

        Assert.Equal(3, world.MeteorShowers.Count);
        Assert.Equal(3, world.MeteorShowerEvents.Count);
    }

    [Fact]
    public void AShower_IsRemovedWhenItsTimeIsUp()
    {
        var world = World(Catalog(Spawn(attempt: 100000, succeed: 100000), lifetimeTicks: 300));
        Run(world, 300);
        Assert.Single(world.MeteorShowers);
        Run(world, 1);
        Assert.Empty(world.MeteorShowers);
    }

    [Fact]
    public void NoSpawnersOrNoCatalog_NoShowers()
    {
        var world = World(null);
        Run(world, 100);
        Assert.Empty(world.MeteorShowers);
        Assert.Null(world.MeteorShower);
    }

    // ---- Firing and hits ---------------------------------------------------------------------------------------------

    [Fact]
    public void AShower_FiresEveryIntervalAndItsMeteorsDamageThePlayer()
    {
        var world = World(Catalog(Spawn(attempt: 100000, succeed: 100000), lifetimeTicks: 0));
        var startEnergy = world.Player.Energy;
        Run(world, 1);
        var meteor = Assert.Single(world.Projectiles);
        Assert.Equal("meteor-shower:46:1", meteor.OwnerShipId);
        Assert.Equal("1007", meteor.WeaponId);
        // Fired from the shower and moved one tick (600 units/s) towards the player.
        Assert.Equal(new Vector2(0, -390), meteor.Position);

        // The next meteor leaves 21 ticks after the first (fireDelay 35 = 0.35 s), before the first one arrives.
        Run(world, 20);
        Assert.Single(world.Projectiles);
        Run(world, 1);
        Assert.Equal(2, world.Projectiles.Count);

        // 400 units at 600 units/s, less both radii: the first meteor lands within a second.
        Run(world, 40);
        Assert.True(world.MeteorHits >= 1);
        Assert.Equal("meteor-shower:46:1", world.Player.LastDamagedByShipId);
        Assert.Equal("1007", world.Player.LastDamageWeaponId);
        Assert.True(world.Player.ShieldLeft < startEnergy);
        Assert.Equal(0, world.DestroyedTargets);
        Assert.Empty(world.Kills);
    }

    [Fact]
    public void AShower_DoesNotFireBeyondItsFireRadius()
    {
        var world = World(Catalog(Spawn(attempt: 100000, succeed: 100000), fireRadius: 100));
        Run(world, 120);
        Assert.Single(world.MeteorShowers);
        Assert.Empty(world.Projectiles);
        Assert.Equal(0, world.MeteorHits);
    }

    [Fact]
    public void Meteors_CanDestroyThePlayersShip_AndStopFiringAtAWreck()
    {
        var world = World(Catalog(Spawn(attempt: 100000, succeed: 100000), lifetimeTicks: 0, interval: 1));
        for (var i = 0; i < 60 * 30 && !world.Player.IsDestroyed; i++) world.Tick(default);
        Assert.True(world.Player.IsDestroyed);
        Run(world, 600);
        Assert.Empty(world.Projectiles); // meteors already in flight expire; no new ones at a wreck
    }

    [Fact]
    public void LeadDirection_MeetsATargetHoldingItsCourse()
    {
        var offset = new Vector2(1000, 0);
        var velocity = new Vector2(0, 300);
        const float speed = 600;
        var direction = WorldSimulation.LeadDirection(offset, velocity, speed);

        // Solve for the meeting time and check the meteor and the target are at the same point.
        var t = 1000 / MathF.Sqrt(speed * speed - 300 * 300);
        var meteorAt = direction * speed * t;
        var targetAt = offset + velocity * t;
        Assert.True(Vector2.Distance(meteorAt, targetAt) < 0.5f);
        // No solution (target faster and moving away): straight at it.
        Assert.Equal(Vector2.UnitX, WorldSimulation.LeadDirection(offset, new Vector2(5000, 0), speed));
    }

    // ---- Real time and persistence -----------------------------------------------------------------------------------

    [Fact]
    public void ShowerTimers_KeepRunningWhileTheWorldIsNotTicking()
    {
        var world = World(Catalog(Spawn(attempt: 600, succeed: 600), lifetimeTicks: 300));
        Run(world, 1);
        Assert.Single(world.MeteorShowers);

        // Docked for the shower's life: it is gone when flying resumes, and no meteor was fired meanwhile.
        world.ElapseWithoutTicking(300);
        Assert.Empty(world.MeteorShowers);
        // The spawner's next attempt (tick 601 on the shared clock) happened in time that did not tick.
        world.ElapseWithoutTicking(300);
        Assert.Single(world.MeteorShowers);
        Assert.Equal(2, world.MeteorShowerEvents.Count);
    }

    [Fact]
    public void ARestoredSector_KeepsItsShowersAndTimers()
    {
        var catalog = Catalog(Spawn(attempt: 600, succeed: 600), lifetimeTicks: 300);
        var world = World(catalog);
        Run(world, 100);
        var snapshot = world.CaptureSector();
        var saved = Assert.Single(snapshot.MeteorShowers!);
        Assert.Equal(201, saved.RemoveInTicks);
        Assert.Equal(501, Assert.Single(snapshot.MeteorSpawns!).TicksUntilAttempt);

        var next = World(catalog);
        Assert.True(next.RestoreSector(snapshot));
        Assert.Equal("meteor-shower:46:1", Assert.Single(next.MeteorShowers).Id);
        // A relaunch neither adds a shower early nor brings the first one back after it is gone.
        Run(next, 201);
        Assert.Empty(next.MeteorShowers);
        Run(next, 299);
        Assert.Empty(next.MeteorShowers);
        Run(next, 1);
        Assert.Single(next.MeteorShowers);
        Assert.Equal("meteor-shower:46:2", next.MeteorShowers[0].Id);
    }

    [Fact]
    public void ASectorSavedBeforeShowers_StartsItsSpawnersFresh()
    {
        var catalog = Catalog(Spawn());
        var old = World(null).CaptureSector() with { MeteorSpawns = null, MeteorShowers = null };
        var world = World(catalog);
        Assert.True(world.RestoreSector(old));
        Run(world, 1);
        Assert.Single(world.MeteorShowers);
    }
}
