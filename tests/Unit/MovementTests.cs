using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests;

public sealed class MovementTests
{
    [Fact]
    public void IdenticalInputReplaysProduceIdenticalWorlds()
    {
        var first = new WorldSimulation(TestWorld.ExternalCatalog());
        var second = new WorldSimulation(TestWorld.ExternalCatalog());
        for (var tick = 1; tick <= 1800; tick++)
        {
            var input = InputAtTick(tick);
            first.Tick(input);
            second.Tick(input);
        }
        TestWorld.SameWorld(first, second);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(144)]
    public void RenderFrameRateDoesNotChangeFixedStepResults(int framesPerSecond)
    {
        var expected = new WorldSimulation(TestWorld.Catalog());
        var actual = new WorldSimulation(TestWorld.Catalog());
        for (var tick = 1; tick <= 600; tick++)
            expected.Tick(InputAtTick(tick));
        var runner = new FixedStepRunner(actual);
        for (var frame = 0; frame < framesPerSecond * 10; frame++)
            runner.Advance(1.0 / framesPerSecond, InputAtTick);
        Assert.Equal(600, actual.TickNumber);
        TestWorld.SameWorld(expected, actual);
    }

    [Fact]
    public void ReleasedThrustPreservesVelocityAndDrift()
    {
        var world = new WorldSimulation(TestWorld.Catalog());
        TestWorld.Tick(world, 60, new ShipInput(1, 0, false));
        var velocity = world.Player.Velocity;
        var position = world.Player.Position;
        TestWorld.Tick(world, 60);
        TestWorld.Near(velocity, world.Player.Velocity);
        TestWorld.Near(position + velocity, world.Player.Position, 0.01f);
    }

    [Fact]
    public void RotationChangesHeadingWithoutChangingVelocity()
    {
        var world = new WorldSimulation(TestWorld.Catalog());
        TestWorld.Tick(world, 60, new ShipInput(1, 0, false));
        var velocity = world.Player.Velocity;
        var heading = world.Player.Heading;
        TestWorld.Tick(world, 30, new ShipInput(0, 1, false));
        Assert.NotEqual(heading, world.Player.Heading);
        TestWorld.Near(velocity, world.Player.Velocity);
    }

    [Fact]
    public void EqualReverseThrustMatchesForwardSpeedAndCanBrake()
    {
        var forward = new WorldSimulation(TestWorld.Catalog());
        var reverse = new WorldSimulation(TestWorld.Catalog());
        TestWorld.Tick(forward, 60, new ShipInput(1, 0, false));
        TestWorld.Tick(reverse, 60, new ShipInput(-1, 0, false));
        TestWorld.Near(-forward.Player.Velocity, reverse.Player.Velocity);
        TestWorld.Tick(forward, 60, new ShipInput(-1, 0, false));
        TestWorld.Near(Vector2.Zero, forward.Player.Velocity);
    }

    [Fact]
    public void SpeedCapAppliesToForwardAndReverse()
    {
        var forward = new WorldSimulation(TestWorld.Catalog());
        var reverse = new WorldSimulation(TestWorld.Catalog());
        TestWorld.Tick(forward, 1000, new ShipInput(1, 0, false));
        TestWorld.Tick(reverse, 1000, new ShipInput(-1, 0, false));
        Assert.InRange(forward.Player.Velocity.Length(), 359.999f, 360.001f);
        Assert.InRange(reverse.Player.Velocity.Length(), 359.999f, 360.001f);
    }

    [Fact]
    public void HeavierFixtureHasLessAccelerationWithSameThrust()
    {
        var light = new ShipDefinition();
        var heavy = light with { Physics = light.Physics with { Mass = light.Physics.Mass * 4 } };
        var lightWorld = new WorldSimulation(TestWorld.Catalog(ship: light));
        var heavyWorld = new WorldSimulation(TestWorld.Catalog(ship: heavy));
        TestWorld.Tick(lightWorld, 60, new ShipInput(1, 0, false));
        TestWorld.Tick(heavyWorld, 60, new ShipInput(1, 0, false));
        Assert.InRange(lightWorld.Player.Velocity.Length() / heavyWorld.Player.Velocity.Length(), 3.999f, 4.001f);
    }

    [Fact]
    public void AsteroidContactRemainsFiniteAndOutsideSolidDuringSustainedThrust()
    {
        var world = new WorldSimulation(TestWorld.Catalog(sector: new SectorDefinition
        {
            PlayerSpawn = new SpawnDefinition { X = -120, Heading = 0 },
            Asteroids = [new AsteroidDefinition { Radius = 30 }]
        }));
        for (var tick = 0; tick < 1200; tick++)
        {
            world.Tick(new ShipInput(1, 0, false));
            Assert.True(float.IsFinite(world.Player.Position.X));
            Assert.True(float.IsFinite(world.Player.Velocity.X));
            Assert.True(Vector2.Distance(world.Player.Position, world.Asteroids[0].Position)
                >= world.Player.Radius + world.Asteroids[0].Radius - 0.001f);
        }
        Assert.True(world.AsteroidCollisions > 0);
        Assert.True(world.Player.Position.X < 0);
    }

    [Fact]
    public void HighSpeedShipCannotTunnelThroughAsteroid()
    {
        var ship = new ShipDefinition { Physics = new ShipPhysics { MaximumSpeed = 12000 } };
        var world = new WorldSimulation(TestWorld.Catalog(ship: ship, sector: new SectorDefinition
        {
            PlayerSpawn = new SpawnDefinition { X = -100, Heading = 0, VelocityX = 12000 },
            Asteroids = [new AsteroidDefinition { Radius = 20 }]
        }));
        world.Tick(default);
        Assert.Equal(1, world.AsteroidCollisions);
        Assert.True(world.Player.Position.X < -34);
        Assert.True(world.Player.Velocity.X < 0);
    }

    [Fact]
    public void OverlappingSpawnIsSeparatedWithoutNaN()
    {
        var world = new WorldSimulation(TestWorld.Catalog(sector: new SectorDefinition
        {
            Asteroids = [new AsteroidDefinition { Radius = 20 }]
        }));
        world.Tick(default);
        Assert.True(float.IsFinite(world.Player.Position.X));
        Assert.True(world.Player.Position.Length() >= 34);
        TestWorld.Near(Vector2.Zero, world.Player.Velocity);
    }

    [Fact]
    public void NonfiniteAndOutOfRangeInputCannotCorruptWorld()
    {
        var world = new WorldSimulation(TestWorld.Catalog());
        world.Tick(new ShipInput(float.NaN, float.PositiveInfinity, false));
        TestWorld.Near(Vector2.Zero, world.Player.Velocity);
        Assert.True(float.IsFinite(world.Player.Heading));
        world.Tick(new ShipInput(500, 500, false));
        Assert.InRange(world.Player.Velocity.Length(), 1.999f, 2.001f);
    }

    [Fact]
    public void RunnerRejectsInvalidElapsedTime()
    {
        var runner = new FixedStepRunner(new WorldSimulation(TestWorld.Catalog()));
        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Advance(-1, default(ShipInput)));
        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Advance(double.NaN, default(ShipInput)));
    }

    private static ShipInput InputAtTick(long tick) => new(
        tick % 180 < 120 ? 1 : -0.5f,
        tick % 240 < 120 ? 0.35f : -0.2f,
        tick % 90 < 30);
}
