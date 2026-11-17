using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// The 60-second dock rule: CFG MaxTimeAllowed=60 on Terrain9 (Repair Dock) and Terrain15 (Refinery/Trade Zone), timed as the
/// InfantryOnline emulator's Arena.poll does (still since the last move or shot; warning at half; spectator past the limit). The
/// synthetic map is a NEW fixture; the Rogue checks read the derived <c>data/sectors/rogue_map.json</c>.
/// </summary>
public sealed class DockTimeLimitTests
{
    private const int Limit = 60;
    private const long LimitTicks = Limit * WorldSimulation.TickRate;
    private static readonly int[] Expire = [720, .. Enumerable.Repeat(120, 15)];

    /// <summary>A 40 x 40 open map: terrain 9 (timed) in tile columns 0..9 (x 0..159), terrain 10 (untimed) in columns 20..29.</summary>
    private static SectorMap Fixture()
    {
        var rules = Enumerable.Repeat(TerrainRule.Open, SectorMap.TerrainCount).ToArray();
        rules[9] = new TerrainRule(Safety: true, EnergyRate: -2000, MaxTimeAllowed: Limit);
        rules[10] = new TerrainRule(Safety: true, DeleteLiveWeapons: true, EnergyRate: -2000);
        var tiles = new byte[40 * 40];
        for (var y = 0; y < 40; y++)
        for (var x = 0; x < 40; x++)
            tiles[y * 40 + x] = (byte)(x < 10 ? 9 : x is >= 20 and < 30 ? 10 : 0);
        return new SectorMap(40, 40, tiles, Expire, terrainRules: rules);
    }

    private static WorldSimulation World(float x) =>
        new(TestWorld.Catalog(sector: new SectorDefinition { PlayerSpawn = new SpawnDefinition { X = x, Y = 320, Heading = 0 } }), map: Fixture());

    private static void Idle(WorldSimulation world, long ticks)
    {
        for (var i = 0; i < ticks; i++) world.Tick(default);
    }

    [Fact]
    public void A_ShipStandingStillInARepairDock_IsWarnedAtHalfTimeAndSentToSpectatorPastTheLimit()
    {
        var world = World(80);
        Assert.Equal(Limit, world.DockTimeLimitSecondsHere);
        Idle(world, LimitTicks / 2 - 1);
        Assert.Equal(0, world.DockTimeWarnings);
        world.Tick(default);
        Assert.Equal(1, world.DockTimeWarnings);
        Assert.True(world.DockTimeWarningGiven);
        Idle(world, LimitTicks / 2);
        Assert.False(world.PlayerIsSpectator); // exactly the limit is still allowed ("> maxTime")
        Assert.Equal(0, world.DockTimeLeftTicks);
        world.Tick(default);
        Assert.True(world.PlayerIsSpectator);
        Assert.Equal(1, world.TimesSentToSpectator);
        Assert.Equal(1, world.DockTimeWarnings); // one warning per still period
        Assert.Null(world.DockTimeLeftTicks);
    }

    [Fact]
    public void MovingAWholePixel_OrFiring_RestartsTheClock_ButTurningOrARefusedShotDoesNot()
    {
        var world = World(80);
        Idle(world, LimitTicks - 10);
        // Turning in place does not move the ship's pixel, and a shot in the safe zone is refused, so neither counts.
        world.Tick(new ShipInput(0, 1, true));
        Assert.Equal(0, world.ShotsFired);
        Assert.Equal(LimitTicks - 9, world.PlayerStillTicks);
        // Half a second of thrust moves the ship over a pixel boundary: the clock restarts and a new warning can come later.
        var start = world.Player.Position;
        for (var i = 0; i < 30; i++) world.Tick(new ShipInput(1, 0, false));
        Assert.NotEqual(MathF.Floor(start.X), MathF.Floor(world.Player.Position.X));
        Assert.True(world.PlayerStillTicks < 30, $"still for {world.PlayerStillTicks} ticks");
        Assert.False(world.DockTimeWarningGiven);
        Assert.False(world.PlayerIsSpectator);
    }

    [Fact]
    public void OpenSpaceAndTheSpaceDock_HaveNoLimit()
    {
        foreach (var x in new[] { 240f, 400f })
        {
            var world = World(x);
            Assert.Equal(0, world.DockTimeLimitSecondsHere);
            Idle(world, LimitTicks * 2);
            world.ElapseWithoutTicking(int.MaxValue / 2);
            Assert.False(world.PlayerIsSpectator);
            Assert.Equal(0, world.DockTimeWarnings);
        }
    }

    [Fact]
    public void StationActivity_KeepsTheClockAtZero_AndTheLimitStartsAgainAfterwards()
    {
        // Steve, Nov 17 2026: an open station screen (shopping, repairing) counts as activity.
        var world = World(80);
        for (var frame = 0; frame < LimitTicks * 3; frame++)
        {
            world.RegisterPlayerActivity();
            world.ElapseWithoutTicking(1);
        }
        Assert.False(world.PlayerIsSpectator);
        Assert.Equal(0, world.DockTimeWarnings);
        Assert.Equal(1, world.PlayerStillTicks);
        Idle(world, LimitTicks - 1);
        Assert.False(world.PlayerIsSpectator);
        world.Tick(default);
        Assert.True(world.PlayerIsSpectator);
        world.RegisterPlayerActivity(); // does not bring a spectator back
        Assert.True(world.PlayerIsSpectator);
    }

    [Fact]
    public void PausedOrAwayTime_CountsTowardTheLimit()
    {
        var world = World(80);
        Idle(world, 60);
        world.ElapseWithoutTicking((int)LimitTicks / 2);
        Assert.Equal(1, world.DockTimeWarnings);
        Assert.False(world.PlayerIsSpectator);
        world.ElapseWithoutTicking((int)LimitTicks / 2);
        Assert.True(world.PlayerIsSpectator);
        Assert.Equal(0, world.TickNumber - 60); // nothing ticked while elapsing
    }

    [Fact]
    public void ASpectatorsShip_IsOutOfTheGame_AndReturnsAtTheSpawnStillDamaged()
    {
        var world = World(80);
        world.DamagePlayer(30);
        Idle(world, LimitTicks + 1);
        Assert.True(world.PlayerIsSpectator);
        var hull = world.Player.Hull;
        var at = world.Player.Position;
        var energy = world.Player.Energy;
        world.Tick(new ShipInput(1, 1, true));
        Assert.Equal(at, world.Player.Position);
        Assert.Equal(energy, world.Player.Energy); // no recharge or drain while out of the game
        Assert.Equal(0, world.ShotsFired);
        Assert.Equal(0, world.DamagePlayer(10));
        Assert.Equal(hull, world.Player.Hull);

        Assert.True(world.ReturnFromSpectator(new Vector2(400, 320), 1f));
        Assert.False(world.PlayerIsSpectator);
        Assert.Equal(new Vector2(400, 320), world.Player.Position);
        Assert.Equal(Vector2.Zero, world.Player.Velocity);
        Assert.Equal(hull, world.Player.Hull); // never a free repair
        Assert.Equal(0, world.PlayerStillTicks);
        Assert.False(world.ReturnFromSpectator(new Vector2(400, 320), 1f));
    }

    [Fact]
    public void ARestoredShip_CarriesItsStillTime_AndAnOverstayedOneGoesStraightToSpectator()
    {
        var world = World(240);
        Assert.True(world.RestorePlayer(new Vector2(80, 320), 0, 50));
        Assert.Equal(0, world.PlayerStillTicks); // a placement restarts the clock...
        world.ContinuePlayerStillness(LimitTicks - 5); // ...until the saved still time is carried over
        Assert.False(world.PlayerIsSpectator);
        world.Tick(default);
        Assert.Equal(1, world.DockTimeWarnings);
        Idle(world, 5);
        Assert.True(world.PlayerIsSpectator);

        var away = World(240);
        away.RestorePlayer(new Vector2(80, 320), 0, 50);
        away.ContinuePlayerStillness(LimitTicks + 1);
        Assert.True(away.PlayerIsSpectator);
    }

    [Fact]
    public void TheSavedStillTime_UsesWallClockTime_SoTimeAwayCounts()
    {
        var now = new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);
        var since = ShipPlacement.StillSince(LimitTicks / 2, now);
        Assert.Equal(now.AddSeconds(-30), since);
        var ship = new ShipCondition(50, 80, 320, 0, ShipCondition.MapPixels, since);
        Assert.Equal(LimitTicks / 2, ShipPlacement.StillTicks(ship, now));
        Assert.Equal(LimitTicks / 2 + 10 * WorldSimulation.TickRate, ShipPlacement.StillTicks(ship, now.AddSeconds(10)));
        // Saved before schema 8, or dated in the future (a clock set back): a ship that has just moved.
        Assert.Equal(0, ShipPlacement.StillTicks(ship with { StillSinceUtc = null }, now));
        Assert.Equal(0, ShipPlacement.StillTicks(ship, now.AddMinutes(-5)));
    }

    [Fact]
    public void TheRogueMap_LimitsTheRepairDocksAndTradeZonesTo60Seconds()
    {
        var map = TestWorld.RogueMap;
        for (var terrain = 0; terrain < SectorMap.TerrainCount; terrain++)
            Assert.Equal(terrain is 9 or 15 ? 60 : 0, map.TerrainRules[terrain].MaxTimeAllowed);
        // Every team spawns in its untimed SpaceDock, so coming back from spectator mode never lands in a timed zone.
        var sector = TestWorld.RogueCatalog().Sector;
        Assert.All(sector.Teams, team => Assert.Equal(0, map.RuleAt(new Vector2(team.Spawn.X, team.Spawn.Y)).MaxTimeAllowed));
    }
}
