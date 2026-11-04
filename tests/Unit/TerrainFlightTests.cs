using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// G10 F12: inside a Repair Dock (terrain 9), SpaceDock (10) or Trading Post / refinery zone (15) a hull flies that terrain's VEH
/// block (lower top speed, slower turn, friction, no boost). Measured on small hand-built maps so the tests need no LFS map; the
/// shipped hull files carry the values, and <see cref="G10FlightValidationTests"/> checks them against the pinned originals.
/// </summary>
public sealed class TerrainFlightTests
{
    private const float Centre = 256 * SectorMap.TileSize;
    private static readonly int[] Expire = Enumerable.Repeat(120, SectorMap.TerrainCount).ToArray();

    internal static ShipDefinition Hull(string file) => GameCatalog.ShipFromJson(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "ships", file)));

    // The hull's own starting weapon (the catalog insists on it), read through a catalog of the weaponless NEW Interceptor.
    private static WeaponDefinition Weapon(ShipDefinition ship)
    {
        var file = ship.WeaponId switch { "1004" => "original_plasma_cannon.json", "1036" => "original_bio_cannon.json", _ => "original_d_cannon.json" };
        string Read(string dir, string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", dir, name));
        return GameCatalog.FromJson(Read("ships", "starter_interceptor.json"), Read("weapons", file), "{}").Weapon;
    }

    /// <summary>A 512 x 512 tile map of one terrain; with <paramref name="openWest"/> the western half (x below the centre) is open space.</summary>
    internal static SectorMap Map(byte terrain, bool openWest = false)
    {
        const int size = 512;
        var tiles = new byte[size * size];
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tiles[y * size + x] = openWest && x < size / 2 ? (byte)0 : terrain;
        return new SectorMap(size, size, tiles, Expire);
    }

    internal static WorldSimulation World(ShipDefinition ship, SectorMap? map, float x = Centre, float velocityX = 0, float heading = 0)
    {
        var sector = new SectorDefinition { PlayerSpawn = new SpawnDefinition { X = x, Y = Centre + 8, Heading = heading, VelocityX = velocityX } };
        return new WorldSimulation(new GameCatalog(ship, Weapon(ship), sector), map: map);
    }

    internal static WorldSimulation Asp(byte terrain, float velocityX = 0) => World(Hull("original_asp.json"), Map(terrain), velocityX: velocityX);

    /// <summary>What the emulator's order (thrust, then friction, then clamp) gives at the 60 Hz step after <paramref name="ticks"/> ticks.</summary>
    private static double EmulatorSpeed(double start, double accelerationPerSecond, double keepPerTick, double top, int ticks)
    {
        var v = start;
        for (var i = 0; i < ticks; i++) v = Math.Min(top, (v + accelerationPerSecond / WorldSimulation.TickRate) * keepPerTick);
        return v;
    }

    [Fact]
    public void ShippedHullsCarryDockAndTradeZoneFlight()
    {
        var asp = Hull("original_asp.json");
        Assert.Equal(new[] { 9, 10, 15 }, asp.Terrains!.Select(t => t.Terrain));
        var dock = asp.TerrainFlight(9)!;
        Assert.Equal(300, dock.Physics.MaximumSpeed);
        Assert.Equal(150, dock.Physics.RotationSpeed * 180 / Math.PI, 3);
        Assert.Equal(0.0140625f, dock.FrictionPerTenMilliseconds);
        Assert.Null(dock.Hyper);
        Assert.Equal(dock, asp.TerrainFlight(10)! with { Terrain = 9, Source = dock.Source });
        var trade = asp.TerrainFlight(15)!;
        Assert.Equal(195, trade.Physics.RotationSpeed * 180 / Math.PI, 3);
        Assert.Equal(0.009375f, trade.FrictionPerTenMilliseconds);
        Assert.Null(trade.Hyper);
        Assert.Null(asp.TerrainFlight(0));
        Assert.Null(asp.TerrainFlight(3));
        // Hornet keeps its own thrust and turn in terrain 15 (raw 50 / 1656), the only hull that does.
        var hornet = Hull("original_hornet.json").TerrainFlight(15)!;
        Assert.Equal(187.5, hornet.Physics.ForwardThrust / hornet.Physics.Mass, 3);
        Assert.Equal(248.4, hornet.Physics.RotationSpeed * 180 / Math.PI, 1);
        // The legacy Interceptor is a NEW fixture with no terrain data: it flies its own values everywhere.
        Assert.Null(Hull("starter_interceptor.json").Terrains);
    }

    [Fact]
    public void ACoastingShipSlowsDownInADockAndKeepsItsSpeedInOpenSpace()
    {
        var dock = Asp(9, velocityX: 300);
        TestWorld.Tick(dock, 60);
        var keep = WorldSimulation.TerrainSpeedRetainedPerTick(dock.Player.Definition.TerrainFlight(9)!);
        Assert.Equal(0.9765625f, keep, 6);
        Assert.Equal(300 * Math.Pow(keep, 60), dock.Player.Velocity.Length(), 2); // 24 % left after one second
        Assert.Equal(0.2410, dock.Player.Velocity.Length() / 300, 3);

        var trade = Asp(15, velocityX: 300);
        TestWorld.Tick(trade, 60);
        Assert.Equal(0.3887, trade.Player.Velocity.Length() / 300, 3); // lighter drag in a trade zone

        var open = Asp(0, velocityX: 300);
        TestWorld.Tick(open, 60);
        Assert.Equal(300, open.Player.Velocity.Length(), 3);
    }

    [Fact]
    public void ThrustInADockFollowsTheEmulatorOrderAndSettlesBelowTheCap()
    {
        var world = Asp(9);
        TestWorld.Tick(world, 60 * 10, new ShipInput(1, 0, false));
        var keep = 0.9765625;
        var expected = EmulatorSpeed(0, 168.7, keep, 300, 600);
        Assert.Equal(expected, world.Player.Velocity.Length(), 2);
        // Terminal speed a·dt·f / (1 − f): about 117 px/s, well under the 300 px/s cap.
        Assert.Equal(168.7 / 60 * keep / (1 - keep), world.Player.Velocity.Length(), 1);
    }

    [Fact]
    public void EnteringADockAtFullSpeedIsClampedToTheDockCap()
    {
        // The western half is open space: the ship crosses into the dock at its 325 px/s top speed.
        var world = World(Hull("original_asp.json"), Map(10, openWest: true), x: Centre - 2, velocityX: 325);
        world.Tick(new ShipInput(1, 0, false));
        Assert.Equal(325, world.Player.Velocity.Length(), 3); // the tick began in open space
        Assert.Equal(10, world.Map!.TerrainAt(world.Player.Position));
        world.Tick(new ShipInput(1, 0, false));
        Assert.Equal(300, world.Player.Velocity.Length(), 3);
        world.Tick(new ShipInput(1, 0, false));
        Assert.Equal(EmulatorSpeed(300, 168.7, 0.9765625, 300, 1), world.Player.Velocity.Length(), 3);
        Assert.True(world.Player.Velocity.Length() < 300);
    }

    [Theory]
    [InlineData("original_asp.json", 9, 150.0)]
    [InlineData("original_asp.json", 10, 150.0)]
    [InlineData("original_asp.json", 15, 195.0)]
    [InlineData("original_hornet.json", 15, 248.4)]
    [InlineData("original_asp.json", 0, 207.0)]
    public void TurnRateFollowsTheTerrain(string file, byte terrain, double degreesPerSecond)
    {
        var world = World(Hull(file), Map(terrain));
        var start = world.Player.Heading;
        TestWorld.Tick(world, 6, new ShipInput(0, 1, false));
        Assert.Equal(degreesPerSecond, (world.Player.Heading - start) * 10 * 180 / Math.PI, 1);
    }

    [Fact]
    public void TheBoostDoesNothingInADockAndCostsNoEnergy()
    {
        var boosted = Asp(9);
        var plain = Asp(9);
        for (var i = 0; i < 120; i++)
        {
            boosted.Tick(new ShipInput(1, 0, false, Hyper: true));
            plain.Tick(new ShipInput(1, 0, false));
            Assert.False(boosted.Player.IsHyperEngaged);
        }
        Assert.Equal(plain.Player.Velocity, boosted.Player.Velocity);
        Assert.Equal(plain.Player.Energy, boosted.Player.Energy);
        Assert.Equal(boosted.Player.Definition.Reactor.MaximumEnergy, boosted.Player.Energy); // nothing was paid for a boost

        var open = Asp(0);
        open.Tick(new ShipInput(1, 0, false, Hyper: true));
        Assert.True(open.Player.IsHyperEngaged);
        Assert.True(open.Player.Energy < open.Player.Definition.Reactor.MaximumEnergy);
    }

    [Fact]
    public void TheShipKeepsItsOwnDefinitionAfterADockTick()
    {
        var world = Asp(9);
        var own = world.Player.Definition;
        world.Tick(new ShipInput(1, 1, false));
        Assert.Same(own, world.Player.Definition);
    }

    [Fact]
    public void WithoutAMapOrInAnUnlistedTerrainTheShipFliesItsOpenSpaceValues()
    {
        var asp = Hull("original_asp.json");
        var none = World(asp, null, velocityX: 300);
        var unlisted = World(asp, Map(3), velocityX: 300);
        TestWorld.Tick(none, 60, new ShipInput(1, 0, false));
        TestWorld.Tick(unlisted, 60, new ShipInput(1, 0, false));
        Assert.Equal(325, none.Player.Velocity.Length(), 3);
        Assert.Equal(none.Player.Velocity, unlisted.Player.Velocity);
    }

    [Fact]
    public void CatalogRejectsBadTerrainFlight()
    {
        var asp = Hull("original_asp.json");
        var dock = asp.TerrainFlight(9)!;
        void Rejects(ShipDefinition ship) => Assert.Throws<ArgumentException>(() => new GameCatalog(ship, Weapon(asp), new SectorDefinition()));
        Rejects(asp with { Terrains = [dock with { Terrain = 0 }] });
        Rejects(asp with { Terrains = [dock with { Terrain = 16 }] });
        Rejects(asp with { Terrains = [dock, dock] });
        Rejects(asp with { Terrains = [dock with { Physics = dock.Physics with { MaximumSpeed = 400 } }] });
        Rejects(asp with { Terrains = [dock with { Physics = dock.Physics with { Mass = 50 } }] });
        Rejects(asp with { Terrains = [dock with { FrictionPerTenMilliseconds = -0.01f }] });
        Rejects(asp with { Terrains = [dock with { FrictionPerTenMilliseconds = 0.1f }] });
        Rejects(asp with { Terrains = [dock with { Hyper = asp.Hyper! with { MaximumSpeed = 1000 } }] });
        _ = new GameCatalog(asp, Weapon(asp), new SectorDefinition());
    }

    [RequiresOriginalAssetsFact]
    public void ShippedTerrainFlightIsThePinnedVehBlocks()
    {
        var hulls = OriginalHullCatalog.Load(OriginalAssets.Root!);
        foreach (var hull in hulls.Hulls)
        {
            var ship = Hull(OriginalShipBuilder.FileName(hull));
            foreach (var t in new[] { 9, 10, 15 })
            {
                var reading = EmulatorFlightReading.From(hull, t);
                var flight = ship.TerrainFlight(t)!;
                Assert.Equal(OriginalShipPhysics.From(hull, t), flight.Physics);
                Assert.Equal(reading.FrictionLossPerTenMilliseconds, flight.FrictionPerTenMilliseconds, 6);
                Assert.Equal(0, reading.HyperTopSpeedPixelsPerSecond);
                Assert.Null(flight.Hyper);
            }
        }
    }
}
