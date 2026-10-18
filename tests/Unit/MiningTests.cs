using System.Text.Json;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;
using RogueTrader.Simulation.Replay;

namespace RogueTrader.UnitTests;

// All quantities, equipment, capacities and geometry here are NEW/provisional; original extraction remains unknown.
public sealed class MiningTests
{
    private const string Ore = "fixture-ore";
    private static readonly ShipInput Mine = new(0, 0, false, true);
    private static MiningEquipmentDefinition Equipment(float range = 4, int cycle = 3, int yield = 2) =>
        new("fixture-miner", [Ore], range, cycle, yield, 1);
    private static GameCatalog Game(float x = 16.01f, bool second = false) => TestWorld.Catalog(sector: new SectorDefinition
    {
        Asteroids = second
            ? [new() { Id = "rock", X = x, Radius = 2 }, new() { Id = "second", X = -x, Radius = 2 }]
            : [new() { Id = "rock", X = x, Radius = 2 }]
    });
    private static WorldSimulation World(int quantity = 7, long capacity = 100, MiningEquipmentDefinition? equipment = null,
        float x = 16.01f, IEnumerable<KeyValuePair<string, int>>? initial = null, long weight = 10,
        CollectibleSpawn[]? piles = null) => new(Game(x), new CargoCatalog(capacity, [new(Ore, weight)], piles), initial,
            new MiningCatalog([new("rock", Ore, quantity)], equipment ?? Equipment()));
    private static long Total(WorldSimulation w) => w.Asteroids.Sum(a => (long)a.ResourceQuantity)
        + w.Collectibles.Sum(p => (long)p.Quantity) + w.Cargo.Quantity(Ore);

    [Fact]
    public void IntentCapabilityCompatibilityAndReachAreRequired()
    {
        var noIntent = World();
        noIntent.Tick(default);
        var noCapability = new WorldSimulation(Game(), new CargoCatalog(100, [new(Ore, 10)]),
            mining: new MiningCatalog([new("rock", Ore, 7)]));
        noCapability.Tick(Mine);
        var incompatible = new WorldSimulation(Game(), new CargoCatalog(100, [new(Ore, 10), new("other", 1)]),
            mining: new MiningCatalog([new("rock", Ore, 7)], Equipment() with { CompatibleItemIds = new[] { "other" } }));
        incompatible.Tick(Mine);
        var distant = World(x: 20.001f);
        distant.Tick(Mine);
        foreach (var w in new[] { noIntent, noCapability, incompatible, distant })
        {
            Assert.Equal(7, w.Asteroids[0].ResourceQuantity);
            Assert.Empty(w.Collectibles);
            Assert.Empty(w.Cargo.Items);
        }
        var disabled = new WorldSimulation(Game());
        disabled.Tick(Mine);
        Assert.Null(disabled.Asteroids[0].ResourceItemId);
        Assert.Empty(disabled.Collectibles);
    }

    [Fact]
    public void ExactSurfaceRangeAndCatalogOrderChooseOneDeposit()
    {
        var w = new WorldSimulation(Game(20, true), new CargoCatalog(0, [new(Ore, 10)]), mining:
            new MiningCatalog([new("second", Ore, 7), new("rock", Ore, 7)], Equipment()));
        w.Tick(Mine);
        Assert.Equal(new[] { 5, 7 }, w.Asteroids.Select(a => a.ResourceQuantity));
        var pile = Assert.Single(w.Collectibles);
        Assert.Equal(17, pile.Position.X);
        Assert.True(System.Numerics.Vector2.Distance(pile.Position, w.Asteroids[0].Position)
            >= w.Asteroids[0].Radius + pile.Radius);
    }

    [Fact]
    public void CyclesDepletionAndPartialCargoConserveEveryTick()
    {
        var w = World(capacity: 25);
        for (var tick = 1; tick <= 15; tick++)
        {
            w.Tick(Mine);
            Assert.Equal(7, Total(w));
            Assert.All(w.Asteroids, a => Assert.True(a.ResourceQuantity >= 0));
            Assert.All(w.Collectibles, p => Assert.True(p.Quantity > 0));
            Assert.Equal(Math.Max(0, 7 - ((tick - 1) / 3 + 1) * 2), w.Asteroids[0].ResourceQuantity);
        }
        Assert.Equal(2, w.Cargo.Quantity(Ore));
        Assert.Equal(5, w.Collectibles.Sum(p => p.Quantity));
        Assert.Equal(20, w.Cargo.TotalWeight);
    }

    [Fact]
    public void FullAndIntegerLimitedHoldsLeaveRemaindersAndIdsNeverCollide()
    {
        var w = World(quantity: int.MaxValue, capacity: 0, weight: 0,
            equipment: Equipment(yield: int.MaxValue), initial: [new(Ore, int.MaxValue - 1)],
            piles: [new("mining:1", Ore, 1, 100, 0, 1)]);
        var total = Total(w);
        w.Tick(Mine);
        Assert.Equal(total, Total(w));
        Assert.Equal(int.MaxValue, w.Cargo.Quantity(Ore));
        Assert.Equal(new[] { "mining:1", "mining:2" }, w.Collectibles.Select(p => p.Id));
        Assert.Equal(int.MaxValue - 1, w.Collectibles[1].Quantity);
        var full = World(capacity: 0);
        TestWorld.Tick(full, 12, Mine);
        Assert.Empty(full.Cargo.Items);
        Assert.Equal(7, Total(full));
        Assert.Equal(0, full.Asteroids[0].ResourceQuantity);
    }

    [Fact]
    public void JsonFixtureIsExplicitAndStrict()
    {
        var mining = MiningCatalog.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "mining", "new-test-fixture.json")));
        var w = new WorldSimulation(Game(), new CargoCatalog(100, [new(Ore, 10)]), mining: mining);
        w.Tick(Mine);
        Assert.Equal(5, w.Asteroids[0].ResourceQuantity);
        Assert.Equal(2, w.Cargo.Quantity(Ore));
        Assert.Throws<JsonException>(() => MiningCatalog.FromJson("{}"));
        Assert.Throws<JsonException>(() => MiningCatalog.FromJson("{\"deposits\":[],\"unknown\":1}"));
        Assert.Throws<JsonException>(() => MiningCatalog.FromJson("null"));
    }

    [Fact]
    public void InvalidDefinitionsAndReferencesAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new MiningCatalog(null!));
        foreach (var d in new MiningDepositDefinition[] { null!, new("", Ore, 1), new("rock", "", 1), new("rock", Ore, 0), new("rock", Ore, -1) })
            Assert.Throws<ArgumentException>(() => new MiningCatalog([d]));
        Assert.Throws<ArgumentException>(() => new MiningCatalog([new("rock", Ore, 1), new("rock", Ore, 1)]));
        foreach (var e in new[] { Equipment() with { Id = "" }, Equipment() with { CompatibleItemIds = null! },
            Equipment() with { CompatibleItemIds = Array.Empty<string>() }, Equipment() with { CompatibleItemIds = new[] { "" } },
            Equipment() with { CompatibleItemIds = new[] { Ore, Ore } }, Equipment(range: -1), Equipment(range: 0), Equipment(range: float.NaN),
            Equipment(cycle: 0), Equipment(cycle: -1), Equipment(yield: 0), Equipment(yield: -1),
            Equipment() with { DropRadius = 0 }, Equipment() with { DropRadius = float.PositiveInfinity } })
            Assert.Throws<ArgumentException>(() => new MiningCatalog([], e));
        var cargo = new CargoCatalog(100, [new(Ore, 1)]);
        foreach (var d in new[] { new MiningDepositDefinition("missing", Ore, 1), new("rock", "missing", 1) })
            Assert.Throws<ArgumentException>(() => new WorldSimulation(Game(), cargo, mining: new MiningCatalog([d])));
        Assert.Throws<ArgumentException>(() => new WorldSimulation(Game(), cargo,
            mining: new MiningCatalog([], Equipment() with { CompatibleItemIds = new[] { "missing" } })));
        Assert.Throws<ArgumentException>(() => World(equipment: Equipment(range: float.MaxValue)));
        Assert.Throws<ArgumentException>(() => World(equipment: Equipment() with { DropRadius = float.MaxValue }));
    }

    [Fact]
    public void DefinitionsSnapshotCallerLists()
    {
        var ids = new[] { Ore };
        var definitions = new[] { new MiningDepositDefinition("rock", Ore, 7) };
        var catalog = new MiningCatalog(definitions, Equipment() with { CompatibleItemIds = ids });
        ids[0] = "missing";
        definitions[0] = new("missing", Ore, 99);
        var w = new WorldSimulation(Game(), new CargoCatalog(100, [new(Ore, 10)]), mining: catalog);
        w.Tick(Mine);
        Assert.Equal(2, w.Cargo.Quantity(Ore));
    }

    [Fact]
    public void ReplayRoundTripPreservesMiningInputsDepositsCooldownAndDrops()
    {
        WorldSimulation Create() => World(capacity: 25);
        var recorder = new SimulationRecorder(Create(), 1);
        for (var tick = 0; tick < 20; tick++) recorder.Tick(tick % 2 == 0 ? Mine : default);
        var recording = SimulationRecording.FromBytes(recorder.Finish().ToBytes());
        Assert.True(recording.Inputs[0].Mine);
        Assert.False(recording.Inputs[1].Mine);
        var replay = SimulationReplayer.Replay(Create, recording);
        Assert.True(replay.Matched);
        Assert.Equal(WorldStateHasher.Hash(recorder.World), WorldStateHasher.Hash(replay.World));
        Assert.Equal(7, Total(replay.World));
        Assert.NotEqual(WorldStateHasher.Hash(World()), WorldStateHasher.Hash(World(quantity: 8)));
        // Identical extraction state and tick, but a different pending cooldown must change the hash.
        var fast = World(equipment: Equipment(cycle: 3));
        var slow = World(equipment: Equipment(cycle: 4));
        fast.Tick(Mine); slow.Tick(Mine);
        Assert.Equal(fast.Cargo.Items, slow.Cargo.Items);
        Assert.NotEqual(WorldStateHasher.Hash(fast), WorldStateHasher.Hash(slow));
        // Same IDs and quantities, but mined ground-pile geometry differs.
        var near = World(capacity: 0, x: 18);
        var far = World(capacity: 0, x: 19);
        near.Tick(Mine); far.Tick(Mine);
        Assert.NotEqual(WorldStateHasher.Hash(near), WorldStateHasher.Hash(far));
        var legacy = recording.ToBytes(); legacy[4] = 1;
        Assert.Contains("Unsupported recording version", Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(legacy)).Message);
        var invalidMine = recording.ToBytes(); invalidMine[25] = 2;
        Assert.Contains("mine flag", Assert.Throws<InvalidDataException>(() => SimulationRecording.FromBytes(invalidMine)).Message);
    }

    [RequiresOriginalAssetsFact]
    public void OriginalResourceKeysAndMassesArePluggableWithoutChangingOriginalRules()
    {
        var resources = OriginalItemCatalog.Load(OriginalAssets.Root!).Resources.ToArray();
        var items = resources.Select(r => new CargoItemDefinition(r.Id.ToString(), r.Mass.Value)).ToArray();
        Assert.Equal(7, items.Length);
        var cargo = new CargoCatalog(50_000, items); // NEW capacity, deposit and equipment; raw Tellerium mass/key retained.
        var w = new WorldSimulation(Game(), cargo, mining: new MiningCatalog([new("rock", "2003", 3)],
            Equipment(yield: 3) with { CompatibleItemIds = new[] { "2003" } }));
        w.Tick(Mine);
        Assert.Equal(50_000, w.Cargo.TotalWeight);
        Assert.Equal(1, w.Cargo.Quantity("2003"));
        Assert.Equal(2, Assert.Single(w.Collectibles).Quantity);
        Assert.Equal("Tellerium", resources.Single(r => r.Id == 2003).Name);
    }
}

