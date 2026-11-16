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

    /// <param name="map">The walls and terrain the authority flies on (<see cref="WorldSimulation.Map"/>); null for open space.</param>
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
        var (_, _, engagedHyper) = Step(_ship, _asteroids, _map, input);
        Recharge(_ship, _map, engagedHyper);
        return new ShipMotionState(_ship.Position, _ship.Velocity, _ship.Heading, _ship.Energy);
    }

    // Authority and prediction share the exact operation order, clamp, swept asteroid collision and wall bounce.
    internal static (int AsteroidHits, bool HitWall, ShipHyper? EngagedHyper) Step(ShipState ship, IReadOnlyList<AsteroidState> asteroids, SectorMap? map,
        ShipInput input)
    {
        var previous = ship.Position;
        var turn = float.IsFinite(input.Turn) ? Math.Clamp(input.Turn, -1, 1) : 0;
        var thrust = float.IsFinite(input.Thrust) ? Math.Clamp(input.Thrust, -1, 1) : 0;
        // F12 samples the terrain at the start of the tick, before any movement or collision.
        var flight = map is null ? null : ship.Definition.TerrainFlight(map.TerrainAt(previous));
        var physics = flight?.Physics ?? ship.Definition.Physics;
        var hyper = flight is null ? ship.Definition.Hyper : flight.Hyper;
        ship.Heading = MathF.IEEERemainder(ship.Heading + turn * physics.RotationSpeed * WorldSimulation.TickDuration, MathF.Tau);
        // The Shift boost replaces the hull's thrust and top speed only while thrusting with a full tick's energy.
        var hyperCost = hyper is null ? 0 : hyper.EnergyPerSecond * WorldSimulation.TickDuration;
        var engagedHyper = input.Hyper && thrust != 0 && ship.Energy >= hyperCost ? hyper : null;
        ship.IsHyperEngaged = engagedHyper is not null;
        if (engagedHyper is not null) ship.Energy -= hyperCost;
        var forwardThrust = engagedHyper?.ForwardThrust ?? physics.ForwardThrust;
        var reverseThrust = engagedHyper?.ReverseThrust ?? physics.ReverseThrust;
        var maximumSpeed = engagedHyper?.MaximumSpeed ?? physics.MaximumSpeed;
        var force = thrust >= 0 ? thrust * forwardThrust : thrust * reverseThrust;
        var direction = new Vector2(MathF.Cos(ship.Heading), MathF.Sin(ship.Heading));
        // The emulator applies friction to the combined velocity and thrust before the speed clamp.
        var keep = flight is null ? 1 : WorldSimulation.TerrainSpeedRetainedPerTick(flight);
        var velocity = ship.Velocity + direction * (force / physics.Mass * WorldSimulation.TickDuration);
        ship.Velocity = ClampSpeed(velocity * keep, maximumSpeed);
        var asteroidHits = CircleCollision.MoveShip(ship, asteroids, WorldSimulation.TickDuration, physics.AsteroidRestitution);
        var hitWall = map is not null && MapCollision.MoveShip(ship, previous, map, WorldSimulation.TickDuration);
        return (asteroidHits, hitWall, engagedHyper);
    }

    /// <summary>Applies reactor recharge and the energy rate of the terrain at the end of movement.</summary>
    internal static void Recharge(ShipState ship, SectorMap? map, ShipHyper? engagedHyper)
    {
        var recharge = engagedHyper is { RechargeWhileEngaged: false } ? 0 : ship.Definition.Reactor.EnergyRegenerationPerSecond;
        var terrainRate = map?.RuleAt(ship.Position).EnergyPerSecond ?? 0;
        ship.Energy = Math.Clamp(ship.Energy + (recharge + terrainRate) * WorldSimulation.TickDuration, 0, ship.Definition.Reactor.MaximumEnergy);
    }

    internal static Vector2 ClampSpeed(Vector2 velocity, float maximumSpeed) =>
        velocity.LengthSquared() > maximumSpeed * maximumSpeed
            ? Vector2.Normalize(velocity) * maximumSpeed
            : velocity;
}
