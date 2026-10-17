using RogueTrader.Simulation;

namespace RogueTrader.AI;

/// <summary>Builds immutable controller views from the authoritative simulation. Copies values; hands out no live state.</summary>
public static class SimulationAiViews
{
    public static AiShipView Describe(ShipState ship, string team) => new(
        ship.Id, team, ship.Position, ship.Velocity, ship.Heading, ship.Radius,
        ship.Hull, ship.Definition.Defense.MaximumHull, ship.Shield, ship.Definition.Defense.MaximumShield,
        ship.Energy, ship.IsDestroyed, ship.LastDamagedByShipId);

    public static AiShipCapabilities Capabilities(ShipState ship) => new(
        ship.Definition.Physics.MaximumSpeed,
        ship.Definition.Physics.RotationSpeed,
        ship.Definition.Physics.ForwardThrust / ship.Definition.Physics.Mass,
        ship.Definition.Physics.ReverseThrust / ship.Definition.Physics.Mass,
        ship.Weapon.MuzzleVelocity,
        ship.Weapon.LifetimeSeconds,
        ship.Weapon.EnergyCost,
        ship.Weapon.InheritsShipVelocity);

    /// <summary>View for <paramref name="self"/> observing the listed ships (excluding itself) and the world's asteroids.</summary>
    public static AiWorldView Build(WorldSimulation world, ShipState self, string selfTeam,
        IEnumerable<ShipState> others, Func<ShipState, string> teamOf)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(self);
        ArgumentNullException.ThrowIfNull(others);
        ArgumentNullException.ThrowIfNull(teamOf);
        return new AiWorldView(
            world.TickNumber,
            Describe(self, selfTeam),
            Capabilities(self),
            others.Where(ship => !ReferenceEquals(ship, self)).Select(ship => Describe(ship, teamOf(ship))).ToArray(),
            world.Asteroids.Select(asteroid => new AiObstacleView(asteroid.Id, asteroid.Position, asteroid.Radius)).ToArray());
    }
}
