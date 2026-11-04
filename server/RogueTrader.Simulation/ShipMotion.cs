using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

/// <summary>The motion fields that can be restored without restoring a simulation world.</summary>
public readonly record struct ShipMotionState(Vector2 Position, Vector2 Velocity, float Heading);

/// <summary>
/// Motion-only adapter for a matching validated catalog. Copies static asteroid geometry in catalog order.
/// One caller owns this instance; stepping has no combat, cargo, mining or world-tick effects.
/// </summary>
public sealed class ShipMotion
{
    private readonly ShipState _ship;
    private readonly IReadOnlyList<AsteroidState> _asteroids;

    public ShipMotion(GameCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _ship = new ShipState(WorldSimulation.PlayerShipId, catalog.Ship, catalog.Weapon, Vector2.Zero, 0);
        _asteroids = Array.AsReadOnly(catalog.Sector.Asteroids.Select(a => new AsteroidState(a)).ToArray());
    }

    /// <summary>Advances only the supplied motion by one fixed simulation tick.</summary>
    public ShipMotionState Step(ShipMotionState state, ShipInput input)
    {
        _ship.Position = state.Position;
        _ship.Velocity = state.Velocity;
        _ship.Heading = state.Heading;
        Step(_ship, _asteroids, input);
        return new ShipMotionState(_ship.Position, _ship.Velocity, _ship.Heading);
    }

    // Authority and prediction share the exact operation order, clamp and swept collision implementation.
    internal static int Step(ShipState ship, IReadOnlyList<AsteroidState> asteroids, ShipInput input)
    {
        var turn = float.IsFinite(input.Turn) ? Math.Clamp(input.Turn, -1, 1) : 0;
        var thrust = float.IsFinite(input.Thrust) ? Math.Clamp(input.Thrust, -1, 1) : 0;
        var strafe = float.IsFinite(input.Strafe) ? Math.Clamp(input.Strafe, -1, 1) : 0;
        var physics = ship.Definition.Physics;
        ship.Heading = MathF.IEEERemainder(ship.Heading + turn * physics.RotationSpeed * WorldSimulation.TickDuration, MathF.Tau);
        // The Shift boost replaces the hull's thrust and top speed for this tick, only while thrusting and only when a whole
        // tick's energy is there to pay for it; otherwise the ship flies on its normal values (and is clamped back to them).
        var hyper = ship.Definition.Hyper;
        var hyperCost = hyper is null ? 0 : hyper.EnergyPerSecond * WorldSimulation.TickDuration;
        ship.IsHyperEngaged = input.Hyper && thrust != 0 && hyper is not null && ship.Energy >= hyperCost;
        if (ship.IsHyperEngaged) ship.Energy -= hyperCost;
        var forwardThrust = ship.IsHyperEngaged ? hyper!.ForwardThrust : physics.ForwardThrust;
        var reverseThrust = ship.IsHyperEngaged ? hyper!.ReverseThrust : physics.ReverseThrust;
        var maximumSpeed = ship.IsHyperEngaged ? hyper!.MaximumSpeed : physics.MaximumSpeed;
        var force = thrust >= 0 ? thrust * forwardThrust : thrust * reverseThrust;
        var direction = new Vector2(MathF.Cos(ship.Heading), MathF.Sin(ship.Heading));
        var velocity = ship.Velocity + direction * (force / physics.Mass * WorldSimulation.TickDuration);
        if (strafe != 0 && physics.StrafeThrust != 0)
        {
            var right = new Vector2(-direction.Y, direction.X);
            velocity += right * (strafe * physics.StrafeThrust / physics.Mass * WorldSimulation.TickDuration);
        }
        ship.Velocity = ClampSpeed(velocity, maximumSpeed);
        return CircleCollision.MoveShip(ship, asteroids, WorldSimulation.TickDuration);
    }

    internal static Vector2 ClampSpeed(Vector2 velocity, float maximumSpeed) =>
        velocity.LengthSquared() > maximumSpeed * maximumSpeed
            ? Vector2.Normalize(velocity) * maximumSpeed
            : velocity;
}
