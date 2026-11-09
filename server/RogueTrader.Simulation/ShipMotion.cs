using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

/// <summary>
/// The motion fields that can be restored without restoring a simulation world. <paramref name="Energy"/> is carried because
/// it pays for the Shift boost; leaving it at 0 means the boost cannot engage on the next step.
/// </summary>
public readonly record struct ShipMotionState(Vector2 Position, Vector2 Velocity, float Heading, float Energy = 0);

/// <summary>
/// Motion-only adapter for a matching validated catalog. Copies static asteroid geometry in catalog order and, when given, the
/// sector's tile map for wall bounces. One caller owns this instance; stepping has no combat, cargo, mining or world-tick effects.
/// </summary>
public sealed class ShipMotion
{
    private readonly ShipState _ship;
    private readonly IReadOnlyList<AsteroidState> _asteroids;
    private readonly SectorMap? _map;

    /// <param name="map">The walls the authority flies on (<see cref="WorldSimulation.Map"/>); null for open space.</param>
    public ShipMotion(GameCatalog catalog, SectorMap? map = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _ship = new ShipState(WorldSimulation.PlayerShipId, catalog.Ship, catalog.Weapon, Vector2.Zero, 0);
        _asteroids = Array.AsReadOnly(catalog.Sector.Asteroids.Select(a => new AsteroidState(a)).ToArray());
        _map = map;
    }

    /// <summary>
    /// Advances only the supplied motion by one fixed simulation tick: turn, thrust (with the Shift boost when it engages),
    /// asteroid and wall bounces, then the tick's energy recharge. Energy spent by firing, mining or hits is not modelled here.
    /// </summary>
    public ShipMotionState Step(ShipMotionState state, ShipInput input)
    {
        _ship.Position = state.Position;
        _ship.Velocity = state.Velocity;
        _ship.Heading = state.Heading;
        _ship.Energy = state.Energy;
        Step(_ship, _asteroids, _map, input);
        Recharge(_ship);
        return new ShipMotionState(_ship.Position, _ship.Velocity, _ship.Heading, _ship.Energy);
    }

    // Authority and prediction share the exact operation order, clamp, swept asteroid collision and wall bounce.
    internal static (int AsteroidHits, bool HitWall) Step(ShipState ship, IReadOnlyList<AsteroidState> asteroids, SectorMap? map,
        ShipInput input)
    {
        var previous = ship.Position;
        var turn = float.IsFinite(input.Turn) ? Math.Clamp(input.Turn, -1, 1) : 0;
        var thrust = float.IsFinite(input.Thrust) ? Math.Clamp(input.Thrust, -1, 1) : 0;
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
        ship.Velocity = ClampSpeed(ship.Velocity + direction * (force / physics.Mass * WorldSimulation.TickDuration), maximumSpeed);
        var asteroidHits = CircleCollision.MoveShip(ship, asteroids, WorldSimulation.TickDuration);
        var hitWall = map is not null && MapCollision.MoveShip(ship, previous, map, WorldSimulation.TickDuration);
        return (asteroidHits, hitWall);
    }

    /// <summary>One tick of reactor recharge, paused while the boost is engaged unless the hull recharges while boosting.</summary>
    internal static void Recharge(ShipState ship)
    {
        if (ship.IsHyperEngaged && !ship.Definition.Hyper!.RechargeWhileEngaged) return;
        ship.Energy = MathF.Min(ship.Definition.Reactor.MaximumEnergy,
            ship.Energy + ship.Definition.Reactor.EnergyRegenerationPerSecond * WorldSimulation.TickDuration);
    }

    internal static Vector2 ClampSpeed(Vector2 velocity, float maximumSpeed) =>
        velocity.LengthSquared() > maximumSpeed * maximumSpeed
            ? Vector2.Normalize(velocity) * maximumSpeed
            : velocity;
}
