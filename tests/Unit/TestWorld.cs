using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests;

internal static class TestWorld
{
    internal static GameCatalog Catalog(ShipDefinition? ship = null, WeaponDefinition? weapon = null,
        SectorDefinition? sector = null) => new(ship ?? new ShipDefinition(), weapon ?? new WeaponDefinition(),
        sector ?? new SectorDefinition());

    internal static GameCatalog ExternalCatalog() => GameCatalog.FromJson(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "ships", "starter_interceptor.json")),
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "weapons", "plasma_cannon.json")),
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "sectors", "first_contact.json")));

    internal static void Tick(WorldSimulation world, int count, ShipInput input = default)
    {
        for (var tick = 0; tick < count; tick++)
            world.Tick(input);
    }

    internal static void Near(Vector2 expected, Vector2 actual, float tolerance = 0.001f) =>
        Assert.True(Vector2.Distance(expected, actual) <= tolerance, $"Expected {expected}, actual {actual}.");

    internal static void SameWorld(WorldSimulation expected, WorldSimulation actual)
    {
        Assert.Equal(expected.TickNumber, actual.TickNumber);
        Assert.Equal(expected.Player.Position, actual.Player.Position);
        Assert.Equal(expected.Player.Velocity, actual.Player.Velocity);
        Assert.Equal(expected.Player.Heading, actual.Player.Heading);
        Assert.Equal(expected.Player.Energy, actual.Player.Energy);
        Assert.Equal(expected.ShotsFired, actual.ShotsFired);
        Assert.Equal(expected.AsteroidCollisions, actual.AsteroidCollisions);
        Assert.Equal(expected.DestroyedTargets, actual.DestroyedTargets);
        Assert.Equal(expected.Targets.Count, actual.Targets.Count);
        for (var index = 0; index < expected.Targets.Count; index++)
        {
            Assert.Equal(expected.Targets[index].Hull, actual.Targets[index].Hull);
            Assert.Equal(expected.Targets[index].Shield, actual.Targets[index].Shield);
            Assert.Equal(expected.Targets[index].DestroyedByShipId, actual.Targets[index].DestroyedByShipId);
        }
        Assert.Equal(expected.Projectiles.Count, actual.Projectiles.Count);
        for (var index = 0; index < expected.Projectiles.Count; index++)
        {
            Assert.Equal(expected.Projectiles[index].Id, actual.Projectiles[index].Id);
            Assert.Equal(expected.Projectiles[index].Position, actual.Projectiles[index].Position);
            Assert.Equal(expected.Projectiles[index].Velocity, actual.Projectiles[index].Velocity);
            Assert.Equal(expected.Projectiles[index].AgeTicks, actual.Projectiles[index].AgeTicks);
        }
    }
}
