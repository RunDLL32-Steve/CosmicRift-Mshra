using System.Text.Json;
using System.Text.Json.Nodes;
using RogueTrader.GameData;

namespace RogueTrader.UnitTests;

public sealed class GameDataTests
{
    [Fact]
    public void ExternalDefinitionsLoadAndUseProvisionalEvidenceClassification()
    {
        var catalog = TestWorld.ExternalCatalog();
        Assert.Equal("starter_interceptor", catalog.Ship.Id);
        Assert.Equal("plasma_cannon", catalog.Weapon.Id);
        Assert.Equal("first_contact", catalog.Sector.Id);
        Assert.Equal("NEW", catalog.Ship.EvidenceClassification);
        Assert.Equal("NEW", catalog.Weapon.EvidenceClassification);
        Assert.Equal("NEW", catalog.Sector.EvidenceClassification);
        Assert.Equal(catalog.Ship.Physics.ForwardThrust, catalog.Ship.Physics.ReverseThrust);
        Assert.Equal(3, catalog.Sector.Targets.Count);
        Assert.Equal(12, catalog.Sector.Asteroids.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(float.NaN)]
    public void InvalidMassIsRejected(float mass) => Assert.Throws<ArgumentException>(() => TestWorld.Catalog(
        ship: new ShipDefinition { Physics = new ShipPhysics { Mass = mass } }));

    [Fact]
    public void DuplicateEntityIdsAreRejected() => Assert.Throws<ArgumentException>(() => TestWorld.Catalog(
        sector: new SectorDefinition
        {
            Targets = [new TargetDefinition { Id = "duplicate" }, new TargetDefinition { Id = "duplicate" }]
        }));

    [Fact]
    public void MisspelledJsonFieldsAreRejected() => Assert.Throws<JsonException>(() => GameCatalog.FromJson(
        "{\"id\":\"test\",\"raduis\":14}", "{}", "{}"));

    [Fact]
    public void ExternalSpawnsAndAsteroidsDoNotOverlap()
    {
        var catalog = TestWorld.ExternalCatalog();
        var spawn = catalog.Sector.PlayerSpawn;
        foreach (var asteroid in catalog.Sector.Asteroids)
        {
            Assert.True(Distance(spawn.X, spawn.Y, asteroid.X, asteroid.Y) > catalog.Ship.Radius + asteroid.Radius);
            foreach (var target in catalog.Sector.Targets)
                Assert.True(Distance(target.X, target.Y, asteroid.X, asteroid.Y) > target.Radius + asteroid.Radius);
            foreach (var other in catalog.Sector.Asteroids.Where(other => other.Id != asteroid.Id))
                Assert.True(Distance(other.X, other.Y, asteroid.X, asteroid.Y) > other.Radius + asteroid.Radius);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FinitePositiveExternalMassCannotOverflowAcceleration(bool overflowForward)
    {
        var (ship, weapon, sector) = ReadExternalDefinitions();
        var physics = ship["physics"]!;
        physics["mass"] = float.Epsilon;
        // Isolate both equations: finite forward/reverse forces divided by tiny finite mass.
        physics[overflowForward ? "reverseThrust" : "forwardThrust"] = 0;
        var exception = Assert.Throws<ArgumentException>(() => GameCatalog.FromJson(
            ship.ToJsonString(), weapon.ToJsonString(), sector.ToJsonString()));
        Assert.Contains(overflowForward ? "forward acceleration" : "reverse acceleration", exception.Message);
    }

    [Theory]
    [InlineData("ship", "radius")]
    [InlineData("ship", "physics.maximumSpeed")]
    [InlineData("weapon", "muzzleVelocity")]
    [InlineData("weapon", "projectileRadius")]
    [InlineData("sector", "targets.0.radius")]
    [InlineData("sector", "asteroids.0.radius")]
    public void ExternalMagnitudesWhoseSquaresOverflowAreRejected(string definition, string field)
    {
        var (ship, weapon, sector) = ReadExternalDefinitions();
        var node = definition switch { "ship" => ship, "weapon" => weapon, _ => sector };
        var path = field.Split('.');
        foreach (var segment in path[..^1])
            node = int.TryParse(segment, out var index) ? node[index]! : node[segment]!;
        node[path[^1]] = float.MaxValue;
        Assert.True(float.IsFinite(float.MaxValue));
        Assert.False(float.IsFinite(float.MaxValue * float.MaxValue));
        Assert.Throws<ArgumentException>(() => GameCatalog.FromJson(
            ship.ToJsonString(), weapon.ToJsonString(), sector.ToJsonString()));
    }

    private static (JsonNode Ship, JsonNode Weapon, JsonNode Sector) ReadExternalDefinitions() => (
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "ships", "starter_interceptor.json")))!,
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "weapons", "plasma_cannon.json")))!,
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "sectors", "first_contact.json")))!);
    private static float Distance(float x1, float y1, float x2, float y2) => MathF.Sqrt(
        (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
}

