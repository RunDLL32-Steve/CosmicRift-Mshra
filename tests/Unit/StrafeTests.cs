using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests;

public sealed class StrafeTests
{
    private static GameCatalog Catalog(float strafeForce = 6000, float maxSpeed = 500) => TestWorld.Catalog(
        ship: new ShipDefinition { Physics = new ShipPhysics {
            Mass = 100, ForwardThrust = 12000, ReverseThrust = 12000,
            StrafeThrust = strafeForce, RotationSpeed = MathF.PI, MaximumSpeed = maxSpeed } });

    [Theory]
    [InlineData(0, 1, 0, 1)]
    [InlineData(0, -1, 0, -1)]
    [InlineData(1.57079637f, 1, -1, 0)]
    [InlineData(1.57079637f, -1, 1, 0)]
    public void LateralImpulseUsesHeadingAndSign(float heading, float axis, float x, float y)
    {
        var motion = new ShipMotion(Catalog());
        var result = motion.Step(new ShipMotionState(Vector2.Zero, Vector2.Zero, heading),
            new ShipInput(0, 0, false, Strafe: axis));
        TestWorld.Near(new Vector2(x, y), result.Velocity, 0.00001f);
        var coasting = motion.Step(result, default);
        Assert.Equal(result.Velocity, coasting.Velocity);
    }

    [Fact]
    public void RotationPrecedesLateralImpulseAndIndependentForcesAreNotNormalized()
    {
        var motion = new ShipMotion(Catalog());
        var turned = motion.Step(new ShipMotionState(Vector2.Zero, Vector2.Zero, 0),
            new ShipInput(0, 1, false, Strafe: 1));
        var heading = MathF.PI / 60;
        TestWorld.Near(new Vector2(-MathF.Sin(heading), MathF.Cos(heading)), turned.Velocity, 0.00001f);

        var diagonal = motion.Step(new ShipMotionState(Vector2.Zero, Vector2.Zero, 0),
            new ShipInput(1, 0, false, Strafe: 1));
        TestWorld.Near(new Vector2(2, 1), diagonal.Velocity);
        var reverse = motion.Step(new ShipMotionState(Vector2.Zero, Vector2.Zero, 0),
            new ShipInput(-1, 0, false, Strafe: -1));
        TestWorld.Near(new Vector2(-2, -1), reverse.Velocity);
    }

    [Fact]
    public void AxisSanitizationAndCapApplyToCombinedVelocity()
    {
        var motion = new ShipMotion(Catalog(maxSpeed: 2));
        var start = new ShipMotionState(Vector2.Zero, Vector2.Zero, 0);
        Assert.Equal(Vector2.Zero, motion.Step(start, new ShipInput(0, 0, false, Strafe: float.NaN)).Velocity);
        Assert.Equal(Vector2.Zero, motion.Step(start, new ShipInput(0, 0, false, Strafe: float.PositiveInfinity)).Velocity);
        Assert.Equal(Vector2.Zero, motion.Step(start, new ShipInput(0, 0, false, Strafe: float.NegativeInfinity)).Velocity);
        var clamped = motion.Step(start, new ShipInput(0, 0, false, Strafe: 500));
        TestWorld.Near(new Vector2(0, 1), clamped.Velocity);
        TestWorld.Near(new Vector2(0, -1), motion.Step(start, new ShipInput(0, 0, false, Strafe: -500)).Velocity);
        var diagonal = motion.Step(start, new ShipInput(1, 0, false, Strafe: 1));
        Assert.InRange(diagonal.Velocity.Length(), 1.9999f, 2.0001f);
        Assert.True(diagonal.Velocity.X > diagonal.Velocity.Y);
    }

    [Fact]
    public void StrafeOnlyNeverEngagesBoostButConcurrentThrustKeepsNormalSideForce()
    {
        var ship = new ShipDefinition { Physics = Catalog().Ship.Physics, Hyper = new ShipHyper {
            ForwardThrust = 30000, ReverseThrust = 20000, MaximumSpeed = 600,
            EnergyPerSecond = 60 } };
        var sector = new SectorDefinition { PlayerSpawn = new SpawnDefinition { Heading = 0 } };
        var alone = new WorldSimulation(TestWorld.Catalog(ship: ship, sector: sector));
        alone.Tick(new ShipInput(0, 0, false, Hyper: true, Strafe: 1));
        Assert.False(alone.Player.IsHyperEngaged);
        TestWorld.Near(new Vector2(0, 1), alone.Player.Velocity);
        Assert.Equal(ship.Reactor.MaximumEnergy, alone.Player.Energy);
        var combined = new WorldSimulation(TestWorld.Catalog(ship: ship, sector: sector));
        combined.Tick(new ShipInput(1, 0, false, Hyper: true, Strafe: 1));
        Assert.True(combined.Player.IsHyperEngaged);
        TestWorld.Near(new Vector2(5, 1), combined.Player.Velocity);
        Assert.Equal(ship.Reactor.MaximumEnergy - 1, combined.Player.Energy);
        var noEnergy = new WorldSimulation(TestWorld.Catalog(ship: ship with { Reactor = new ShipReactor {
            MaximumEnergy = 0, EnergyRegenerationPerSecond = 0 } }, sector: sector));
        noEnergy.Tick(new ShipInput(1, 0, false, Hyper: true, Strafe: 1));
        Assert.False(noEnergy.Player.IsHyperEngaged);
        TestWorld.Near(new Vector2(2, 1), noEnergy.Player.Velocity);
        Assert.Equal(0, noEnergy.Player.Energy);
        var capped = new WorldSimulation(TestWorld.Catalog(ship: ship with { Hyper = ship.Hyper! with {
            MaximumSpeed = 3 } }, sector: sector));
        capped.Tick(new ShipInput(1, 0, false, Hyper: true, Strafe: 1));
        Assert.InRange(capped.Player.Velocity.Length(), 2.9999f, 3.0001f);
    }

    [Fact]
    public void CatalogRejectsInvalidOrOverflowingStrafeForceAndLegacyJsonDefaultsToZero()
    {
        var baseShip = new ShipDefinition();
        foreach (var force in new[] { -1f, float.NaN, float.PositiveInfinity, float.MaxValue })
            Assert.Throws<ArgumentException>(() => TestWorld.Catalog(ship: baseShip with { Physics = baseShip.Physics with {
                Mass = force == float.MaxValue ? float.Epsilon : 100, StrafeThrust = force } }));
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "ships", "starter_interceptor.json"));
        Assert.DoesNotContain("strafeThrust", json);
        Assert.Equal(0, GameCatalog.ShipFromJson(json).Physics.StrafeThrust);
    }

    [Fact]
    public void ZeroAxisRetainsPreStrafeFloatOperationOrder()
    {
        var catalog = Catalog();
        var motion = new ShipMotion(catalog);
        var state = new ShipMotionState(Vector2.Zero, new Vector2(17.25f, -3.5f), 0.2f);
        var oldVelocity = state.Velocity;
        var oldHeading = state.Heading;
        var physics = catalog.Ship.Physics;
        for (var i = 0; i < 120; i++)
        {
            var input = new ShipInput(i % 3 == 0 ? -0.5f : 1, i % 4 == 0 ? 0.25f : -0.125f,
                false);
            oldHeading = MathF.IEEERemainder(oldHeading + input.Turn * physics.RotationSpeed *
                WorldSimulation.TickDuration, MathF.Tau);
            var force = input.Thrust >= 0 ? input.Thrust * physics.ForwardThrust : input.Thrust * physics.ReverseThrust;
            var direction = new Vector2(MathF.Cos(oldHeading), MathF.Sin(oldHeading));
            oldVelocity = LegacyClamp(oldVelocity + direction * (force / physics.Mass * WorldSimulation.TickDuration),
                physics.MaximumSpeed);
            state = motion.Step(state, input);
            Assert.Equal(BitConverter.SingleToInt32Bits(oldHeading), BitConverter.SingleToInt32Bits(state.Heading));
            Assert.Equal(BitConverter.SingleToInt32Bits(oldVelocity.X), BitConverter.SingleToInt32Bits(state.Velocity.X));
            Assert.Equal(BitConverter.SingleToInt32Bits(oldVelocity.Y), BitConverter.SingleToInt32Bits(state.Velocity.Y));
        }
    }

    private static Vector2 LegacyClamp(Vector2 velocity, float maximumSpeed) =>
        velocity.LengthSquared() > maximumSpeed * maximumSpeed
            ? Vector2.Normalize(velocity) * maximumSpeed
            : velocity;
}
