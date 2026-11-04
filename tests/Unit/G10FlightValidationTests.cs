using System.Numerics;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// Queue G10 flight and weapon slice: the "observed" column of docs/original-research/G10_VALIDATION.md is measured here by
/// running the prototype <see cref="WorldSimulation"/>, and the "expected" column is the pinned originals read through the
/// emulator's unit chain (<see cref="EmulatorFlightReading"/>, RECONSTRUCTED). The verdict test fails if either side moves,
/// so the table cannot silently go stale. Since the second slice the shipped ship and weapon take their values from
/// <see cref="OriginalShipPhysics"/>, and <c>ShippedShipAndWeaponAreTheOriginalAspAndPlasmaCannon</c> keeps the JSON in step.
/// </summary>
public sealed class G10FlightValidationTests
{
    private const double Tolerance = 0.02; // 2 %: a row MATCHES only if the two values agree this closely.

    private static readonly Lazy<OriginalHullCatalog> Hulls = new(() => OriginalHullCatalog.Load(OriginalAssets.Root!));
    private static readonly Lazy<OriginalItemCatalog> Items = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    // A world with the shipped ship and weapon but an empty sector, so nothing collides while measuring.
    private static WorldSimulation OpenSpace()
    {
        var external = TestWorld.ExternalCatalog();
        return new WorldSimulation(TestWorld.Catalog(external.Ship, external.Weapon));
    }

    // ---- Observed: the prototype, measured --------------------------------------------------------------------------

    internal static double ObservedTopSpeed()
    {
        var world = OpenSpace();
        TestWorld.Tick(world, 60 * 20, new ShipInput(1, 0, false));
        return world.Player.Velocity.Length();
    }

    internal static double ObservedAcceleration(float thrust)
    {
        var world = OpenSpace();
        TestWorld.Tick(world, 60, new ShipInput(thrust, 0, false));
        return world.Player.Velocity.Length();
    }

    internal static double ObservedSecondsToTopSpeed()
    {
        var world = OpenSpace();
        var top = world.Catalog.Ship.Physics.MaximumSpeed;
        var ticks = 0;
        while (world.Player.Velocity.Length() < top - 1e-3f && ticks < 60 * 60)
        {
            world.Tick(new ShipInput(1, 0, false));
            ticks++;
        }
        return ticks / (double)WorldSimulation.TickRate;
    }

    internal static double ObservedTurnDegreesPerSecond()
    {
        var world = OpenSpace();
        var start = world.Player.Heading;
        TestWorld.Tick(world, 6, new ShipInput(0, 1, false)); // a tenth of a second: no wrap-around
        return (world.Player.Heading - start) * 10 * 180 / Math.PI;
    }

    internal static double ObservedCoastRetainedPerSecond()
    {
        var world = OpenSpace();
        TestWorld.Tick(world, 60, new ShipInput(1, 0, false));
        var before = world.Player.Velocity.Length();
        TestWorld.Tick(world, 60);
        return world.Player.Velocity.Length() / before;
    }

    internal static double ObservedFireIntervalSeconds()
    {
        var world = OpenSpace();
        var shotTicks = new List<long>();
        for (var i = 0; i < 120; i++)
        {
            var before = world.ShotsFired;
            world.Tick(new ShipInput(0, 0, true));
            if (world.ShotsFired > before) shotTicks.Add(world.TickNumber);
        }
        return (shotTicks[1] - shotTicks[0]) / (double)WorldSimulation.TickRate;
    }

    [Fact]
    public void PrototypeFlightIsMeasuredAsTheTableSays()
    {
        Assert.Equal(325, ObservedTopSpeed(), 3);
        Assert.Equal(168.7, ObservedAcceleration(1), 2);
        Assert.Equal(168.7, ObservedAcceleration(-1), 2); // reverse equals forward (owner decision)
        Assert.Equal(116 / 60.0, ObservedSecondsToTopSpeed(), 3); // 325 / 168.7 = 1.926 s, reached on tick 116
        Assert.Equal(207, ObservedTurnDegreesPerSecond(), 2);
        Assert.Equal(1.0, ObservedCoastRetainedPerSecond(), 5); // no drag in open space
        Assert.Equal(0.25, ObservedFireIntervalSeconds(), 3);
        var catalog = TestWorld.ExternalCatalog();
        Assert.Equal(0.727f, catalog.Ship.Physics.AsteroidRestitution);
        Assert.Equal(700, catalog.Weapon.MuzzleVelocity);
        Assert.Equal(2.5f, catalog.Weapon.LifetimeSeconds);
        Assert.True(catalog.Weapon.InheritsShipVelocity);
        Assert.Equal(14, catalog.Ship.Radius);
    }

    [RequiresOriginalAssetsFact]
    public void ShippedShipAndWeaponAreTheOriginalAspAndPlasmaCannon()
    {
        var catalog = TestWorld.ExternalCatalog();
        var expected = OriginalShipPhysics.From(Hulls.Value.ByName("Asp"));
        var actual = catalog.Ship.Physics;
        Assert.Equal(expected.Mass, actual.Mass);
        Assert.Equal(expected.ForwardThrust, actual.ForwardThrust, 2);
        Assert.Equal(expected.ReverseThrust, actual.ReverseThrust, 2);
        Assert.Equal(expected.RotationSpeed, actual.RotationSpeed, 6);
        Assert.Equal(expected.MaximumSpeed, actual.MaximumSpeed);
        Assert.Equal(expected.AsteroidRestitution, actual.AsteroidRestitution);
        Assert.Equal(OriginalShipPhysics.FireIntervalSeconds(OriginalProjectile.Of(Items.Value.ById(1004))), catalog.Weapon.CooldownSeconds);
    }

    // name, top speed px/s, forward = reverse acceleration px/s², turn deg/s (from the G8 raw oracle through the emulator units)
    [RequiresOriginalAssetsTheory]
    [InlineData("Asp", 325.0, 168.7, 207.0)]
    [InlineData("Excelsior", 357.5, 168.7, 207.0)]
    [InlineData("Ravager", 325.0, 157.5, 207.0)]
    [InlineData("Hornet", 325.0, 187.5, 248.4)]
    [InlineData("Skithist", 335.0, 168.7, 207.0)]
    [InlineData("Mastakith", 325.0, 168.7, 269.1)]
    [InlineData("Tarthist", 325.0, 168.7, 207.0)]
    [InlineData("Kragath", 325.0, 157.5, 207.0)]
    [InlineData("Rak'Kir", 325.0, 187.5, 240.0)]
    [InlineData("Rip'Pir", 330.0, 168.7, 207.0)]
    [InlineData("Zap'Pir", 325.0, 157.5, 207.0)]
    [InlineData("Zag'Gir", 335.0, 168.7, 207.0)]
    public void EveryOriginalHullHasItsOwnPhysics(string name, double top, double acceleration, double turnDegrees)
    {
        var all = OriginalShipPhysics.All(Hulls.Value);
        Assert.Equal(12, all.Count);
        var physics = all[name];
        Assert.Equal(top, physics.MaximumSpeed, 3);
        Assert.Equal(acceleration, physics.ForwardThrust / physics.Mass, 3);
        Assert.Equal(physics.ForwardThrust, physics.ReverseThrust);
        Assert.Equal(turnDegrees, physics.RotationSpeed * 180 / Math.PI, 1);
        Assert.Equal(0.727f, physics.AsteroidRestitution);
    }

    [Fact]
    public void SectorCrossingTimeIsTheOriginalMapAtTopSpeed()
    {
        // F10: the game now flies the Rogue map at map-pixel scale (data/sectors/rogue_map.json, 1999 tiles x 16 px), so
        // crossing it at the Asp's measured top speed takes the original reading's 98.4 s.
        var map = SectorMap.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "sectors", "rogue_map.json")));
        Assert.Equal(31984, map.PixelWidth);
        Assert.Equal(98.4, map.PixelWidth / ObservedTopSpeed(), 1);
    }

    // ---- Expected: the originals through the emulator's unit chain -------------------------------------------------

    [Fact]
    public void ThrustFactorUsesIntegerDivisionLikeTheEmulator()
    {
        Assert.Equal(1687, EmulatorFlightReading.ThrustVelocityUnitsPerSecond(45));
        Assert.Equal(1575, EmulatorFlightReading.ThrustVelocityUnitsPerSecond(42));
        Assert.Equal(1875, EmulatorFlightReading.ThrustVelocityUnitsPerSecond(50));
        Assert.Equal(825, EmulatorFlightReading.ThrustVelocityUnitsPerSecond(22));
        Assert.Equal(-37, EmulatorFlightReading.ThrustVelocityUnitsPerSecond(-1)); // what the bot does with the sentinel
    }

    [RequiresOriginalAssetsFact]
    public void AspOpenSpaceReading()
    {
        var asp = EmulatorFlightReading.From(Hulls.Value.ByName("Asp"));
        Assert.Equal(325, asp.TopSpeedPixelsPerSecond, 6);
        Assert.Equal(168.7, asp.ForwardAccelerationPixelsPerSecondSquared, 6);
        Assert.True(asp.ReverseUsesForwardThrust);
        Assert.Equal(168.7, asp.ReverseAccelerationPixelsPerSecondSquared, 6);
        Assert.Equal(82.5, asp.StrafeAccelerationPixelsPerSecondSquared, 6);
        Assert.Equal(207, asp.TurnDegreesPerSecond, 6);
        Assert.Equal(1.739, asp.SecondsPerRevolution, 3);
        Assert.Equal(1.926, asp.SecondsToTopSpeed, 3);
        Assert.Equal(350, asp.HyperTopSpeedPixelsPerSecond, 6);
        Assert.Equal(0.727, asp.WallBounceRetained, 6);
        Assert.Equal(1.0, asp.FrictionRetainedPerSecond(10), 9);
        Assert.Equal(1.0, asp.FrictionRetainedPerSecond(100), 9);
    }

    [RequiresOriginalAssetsFact]
    public void DockTerrainsSlowTheShipAndTheRateDependsOnTheStep()
    {
        var asp = Hulls.Value.ByName("Asp");
        var repairDock = EmulatorFlightReading.From(asp, 9);
        Assert.Equal(300, repairDock.TopSpeedPixelsPerSecond, 6);
        Assert.Equal(150, repairDock.TurnDegreesPerSecond, 6);
        Assert.Equal(0.2426, repairDock.FrictionRetainedPerSecond(10), 4);
        Assert.Equal(0.2197, repairDock.FrictionRetainedPerSecond(100), 4);
        var tradeZone = EmulatorFlightReading.From(asp, 15);
        Assert.Equal(195, tradeZone.TurnDegreesPerSecond, 6);
        Assert.Equal(0.3899, tradeZone.FrictionRetainedPerSecond(10), 4);
        Assert.Equal(0, tradeZone.HyperTopSpeedPixelsPerSecond);
        Assert.Throws<ArgumentOutOfRangeException>(() => EmulatorFlightReading.From(asp, 16));
    }

    [RequiresOriginalAssetsTheory]
    [InlineData("Excelsior", 357.5, 168.7, 207.0)]
    [InlineData("Hornet", 325.0, 187.5, 248.4)]
    [InlineData("Mastakith", 325.0, 168.7, 269.1)]
    [InlineData("Ravager", 325.0, 157.5, 207.0)]
    public void OtherHullsReadTheSameWay(string name, double top, double thrust, double turn)
    {
        var reading = EmulatorFlightReading.From(Hulls.Value.ByName(name));
        Assert.Equal(top, reading.TopSpeedPixelsPerSecond, 6);
        Assert.Equal(thrust, reading.ForwardAccelerationPixelsPerSecondSquared, 6);
        Assert.Equal(turn, reading.TurnDegreesPerSecond, 1);
    }

    // ---- Verdicts: one row per feature in the G10 table ------------------------------------------------------------

    internal enum Verdict { Match, Mismatch }

    private static Verdict Compare(double observed, double expected) =>
        Math.Abs(observed - expected) <= Tolerance * Math.Abs(expected) ? Verdict.Match : Verdict.Mismatch;

    [RequiresOriginalAssetsFact]
    public void TableVerdictsAreCurrent()
    {
        var asp = EmulatorFlightReading.From(Hulls.Value.ByName("Asp"));
        var plasma = OriginalProjectile.Of(Items.Value.ById(1004));
        Assert.Equal("Plasma Cannon", plasma.Item.Name);
        // Map crossing: the game flies the derived Rogue map at map-pixel scale. Rogue.lvl is 1999 tiles wide (pinned by
        // OriginalLevelTests; read here as a constant so this runs without the LFS map).
        const double originalMapWidth = 1999.0 * OriginalCollisionGrid.TileSizePixels;
        var flownMapWidth = (double)TestWorld.RogueMap.PixelWidth;
        var rows = new (string Row, double Observed, double Expected, Verdict Status)[]
        {
            ("F1 top speed (units/s vs px/s)", ObservedTopSpeed(), asp.TopSpeedPixelsPerSecond, Verdict.Match),
            ("F2 forward acceleration", ObservedAcceleration(1), asp.ForwardAccelerationPixelsPerSecondSquared, Verdict.Match),
            ("F3 reverse equals forward", ObservedAcceleration(-1) / ObservedAcceleration(1),
                asp.ReverseAccelerationPixelsPerSecondSquared / asp.ForwardAccelerationPixelsPerSecondSquared, Verdict.Match),
            ("F4 seconds to top speed", ObservedSecondsToTopSpeed(), asp.SecondsToTopSpeed, Verdict.Match),
            ("F5 turn rate", ObservedTurnDegreesPerSecond(), asp.TurnDegreesPerSecond, Verdict.Match),
            ("F6 drag in open space", ObservedCoastRetainedPerSecond(), asp.FrictionRetainedPerSecond(10), Verdict.Match),
            ("F9 bounce keeps", TestWorld.ExternalCatalog().Ship.Physics.AsteroidRestitution, asp.WallBounceRetained, Verdict.Match),
            ("F10 seconds to cross the map", flownMapWidth / ObservedTopSpeed(), originalMapWidth / asp.TopSpeedPixelsPerSecond, Verdict.Match),
            ("W1 fire interval (s)", ObservedFireIntervalSeconds(), plasma.FireDelay.Value * 10 / 1000.0, Verdict.Match),
        };
        foreach (var row in rows)
            Assert.True(Compare(row.Observed, row.Expected) == row.Status,
                $"{row.Row}: observed {row.Observed:0.###}, expected {row.Expected:0.###}, table says {row.Status}");
        Assert.Equal(1000, plasma.InheritedSpeed.Value); // W4: full inheritance under the per-mille reading; prototype inherits
        Assert.Equal(2200, plasma.MuzzleVelocity.Value);  // W2: unit open
        Assert.Equal(550, plasma.AliveTime.Value);         // W3: unit open
    }
}
