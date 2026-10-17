using System.Collections.Immutable;
using System.Text.Json;
using RogueTrader.OriginalData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests;

/// <summary>
/// Cargo capacity and collectible pickup (milestone queue item M2). Every capacity, weight, quantity and position below is a
/// NEW/provisional test fixture, not game balance. The default test ship starts at (0, 0) heading up (negative y) at rest.
/// </summary>
public sealed class CargoPickupTests
{
    private const string OreA = "fixture-ore-a"; // NEW provisional: unit weight 10
    private const string OreB = "fixture-ore-b"; // NEW provisional: unit weight 25
    private const string Feather = "fixture-feather"; // NEW provisional: weightless

    private static CargoItemDefinition[] Items() =>
        [new(OreA, 10), new(OreB, 25), new(Feather, 0)];

    private static CargoCatalog Catalog(long capacity, params CollectibleSpawn[] piles) => new(capacity, Items(), piles);

    // Ship radius 14 + pile radius 10 reach: a pile 60 units ahead (y = -60) is reached after a short burn.
    private static CollectibleSpawn Pile(string id, string item, int quantity, float y = -60) =>
        new(id, item, quantity, 0, y, 10);

    private static WorldSimulation World(CargoCatalog cargo, IEnumerable<KeyValuePair<string, int>>? initial = null) =>
        new(TestWorld.Catalog(), cargo, initial);

    private static void Burn(WorldSimulation world, int ticks = 120) =>
        TestWorld.Tick(world, ticks, new ShipInput(1, 0, false));

    private static int Ground(WorldSimulation world, string item) =>
        world.Collectibles.Where(c => c.ItemId == item).Sum(c => c.Quantity);

    [Fact]
    public void HoldAcceptsUntilCapacityThenRefusesWithoutCreatingOrLosingUnits()
    {
        var hold = new CargoHold(Catalog(100));
        Assert.Equal(CargoPickupOutcome.Full, hold.Add(OreA, 6).Outcome);
        var partial = hold.Add(OreA, 6); // free weight 40 -> 4 more units
        Assert.Equal(new CargoPickupResult(6, 4), partial);
        Assert.Equal(CargoPickupOutcome.Partial, partial.Outcome);
        Assert.Equal(2, partial.Refused);
        var refused = hold.Add(OreA, 1);
        Assert.Equal(CargoPickupOutcome.Refused, refused.Outcome);
        Assert.Equal(10, hold.Quantity(OreA));
        Assert.Equal(100, hold.TotalWeight);
        Assert.Equal(0, hold.FreeWeight);
    }

    [Fact]
    public void ExactFitIsAcceptedAndOneUnitOverIsNot()
    {
        var hold = new CargoHold(Catalog(100));
        Assert.Equal(CargoPickupOutcome.Full, hold.Add(OreB, 4).Outcome);
        Assert.Equal(CargoPickupOutcome.Refused, hold.Add(OreB, 1).Outcome);
        Assert.Equal(CargoPickupOutcome.Refused, new CargoHold(Catalog(24)).Add(OreB, 1).Outcome);
    }

    [Fact]
    public void WeightsAreMixedAcrossItemTypes()
    {
        var hold = new CargoHold(Catalog(100));
        hold.Add(OreB, 3); // 75
        Assert.Equal(new CargoPickupResult(5, 2), hold.Add(OreA, 5)); // 25 free -> 2 x 10
        Assert.Equal(95, hold.TotalWeight);
        Assert.Equal(new CargoPickupResult(1, 0), hold.Add(OreB, 1));
    }

    [Fact]
    public void WeightlessItemsAreOnlyBoundedByTheIntegerRange()
    {
        var hold = new CargoHold(Catalog(0));
        Assert.Equal(CargoPickupOutcome.Full, hold.Add(Feather, int.MaxValue).Outcome);
        Assert.Equal(CargoPickupOutcome.Refused, hold.Add(Feather, 1).Outcome);
        Assert.Equal(0, hold.TotalWeight);
        Assert.Equal(CargoPickupOutcome.Refused, hold.Add(OreA, 1).Outcome);
    }

    [Fact]
    public void InitialInventoryOverCapacityAcceptsNothingMoreAndNeverGoesNegative()
    {
        var hold = new CargoHold(Catalog(50), new Dictionary<string, int> { [OreA] = 8 });
        Assert.Equal(80, hold.TotalWeight);
        Assert.Equal(0, hold.FreeWeight);
        Assert.Equal(0, hold.MaxAccept(OreA));
        Assert.Equal(CargoPickupOutcome.Refused, hold.Add(OreA, 1).Outcome);
        Assert.Equal(8, hold.Quantity(OreA));
    }

    [Fact]
    public void InvalidRequestsAndDefinitionsAreRejected()
    {
        var hold = new CargoHold(Catalog(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => hold.Add(OreA, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => hold.Add(OreA, -3));
        Assert.Throws<ArgumentException>(() => hold.Add("fixture-unknown", 1));
        Assert.Throws<ArgumentException>(() => new CargoHold(Catalog(100), new Dictionary<string, int> { ["fixture-unknown"] = 1 }));
        Assert.Throws<ArgumentException>(() => new CargoHold(Catalog(100), new Dictionary<string, int> { [OreA] = -1 }));
        Assert.Throws<ArgumentException>(() => new CargoCatalog(-1, Items()));
        Assert.Throws<ArgumentException>(() => new CargoCatalog(10, [new(OreA, -1)]));
        Assert.Throws<ArgumentException>(() => new CargoCatalog(10, [new(OreA, 1), new(OreA, 2)]));
        Assert.Throws<ArgumentException>(() => Catalog(10, Pile("p", "fixture-unknown", 1)));
        Assert.Throws<ArgumentException>(() => Catalog(10, Pile("p", OreA, 0)));
        Assert.Throws<ArgumentException>(() => Catalog(10, Pile("p", OreA, 1), Pile("p", OreA, 1)));
        Assert.Throws<ArgumentException>(() => Catalog(10, new CollectibleSpawn("p", OreA, 1, float.NaN, 0, 10)));
        Assert.Throws<ArgumentException>(() => Catalog(10, new CollectibleSpawn("p", OreA, 1, 0, 0, 0)));
    }

    [Fact]
    public void RandomAddSequenceKeepsCapacityAndConservationInvariants()
    {
        var catalog = Catalog(1000);
        var hold = new CargoHold(catalog);
        var random = new Random(20261016); // fixed seed: deterministic
        var offered = new Dictionary<string, long>();
        var accepted = new Dictionary<string, long>();
        var refused = new Dictionary<string, long>();
        string[] ids = [OreA, OreB, Feather];
        for (var step = 0; step < 2000; step++)
        {
            var id = ids[random.Next(ids.Length)];
            var requested = random.Next(1, 40);
            var result = hold.Add(id, requested);
            Assert.Equal(requested, result.Accepted + result.Refused);
            offered[id] = offered.GetValueOrDefault(id) + requested;
            accepted[id] = accepted.GetValueOrDefault(id) + result.Accepted;
            refused[id] = refused.GetValueOrDefault(id) + result.Refused;
            Assert.InRange(hold.TotalWeight, 0, hold.Capacity);
            Assert.Equal(catalog.WeightOf(hold.Items), hold.TotalWeight);
        }
        foreach (var id in ids)
        {
            Assert.Equal(accepted.GetValueOrDefault(id), hold.Quantity(id));
            Assert.Equal(offered.GetValueOrDefault(id), accepted.GetValueOrDefault(id) + refused.GetValueOrDefault(id));
        }
    }

    [Fact]
    public void CatalogLoadsFromJsonAndRejectsUnknownMembers()
    {
        var catalog = CargoCatalog.FromJson("""
            { "capacity": 100,
              "items": [ { "id": "fixture-ore-a", "unitWeight": 10 } ],
              "collectibles": [ { "id": "p1", "itemId": "fixture-ore-a", "quantity": 3, "x": 0, "y": -60, "radius": 10 } ] }
            """);
        Assert.Equal(100, catalog.Capacity);
        Assert.Equal(10, catalog.UnitWeight(OreA));
        var spawn = Assert.Single(catalog.Collectibles);
        Assert.Equal(("p1", OreA, 3), (spawn.Id, spawn.ItemId, spawn.Quantity));
        Assert.Throws<JsonException>(() => CargoCatalog.FromJson("""{ "capacity": 1, "items": [], "bogus": 1 }"""));
        Assert.Throws<ArgumentException>(() => CargoCatalog.FromJson("""{ "capacity": -5, "items": [] }"""));
    }

    [Fact]
    public void FlyingIntoAPileCollectsItAllWhenItFits()
    {
        var world = World(Catalog(100, Pile("p1", OreA, 4)));
        Burn(world);
        Assert.Empty(world.Collectibles);
        Assert.Equal(4, world.Cargo.Quantity(OreA));
        Assert.Equal(40, world.Cargo.TotalWeight);
    }

    [Fact]
    public void PileOutOfReachIsLeftAlone()
    {
        var world = World(Catalog(100, Pile("p1", OreA, 4, y: 500))); // behind the ship (heading is up)
        Burn(world, 30);
        Assert.Equal(4, Assert.Single(world.Collectibles).Quantity);
        Assert.Empty(world.Cargo.Items);
    }

    [Fact]
    public void PartialPickupLeavesTheRemainderInSpaceAndConservesUnits()
    {
        var world = World(Catalog(25, Pile("p1", OreA, 5)));
        Burn(world);
        Assert.Equal(2, world.Cargo.Quantity(OreA));
        Assert.Equal(3, Assert.Single(world.Collectibles).Quantity);
        Assert.Equal(5, world.Cargo.Quantity(OreA) + Ground(world, OreA));
    }

    [Fact]
    public void FullHoldRefusesFurtherPilesAndTheyStayInSpace()
    {
        var world = World(Catalog(20, Pile("p1", OreA, 2, y: -60), Pile("p2", OreA, 5, y: -120)));
        Burn(world, 240);
        Assert.Equal(2, world.Cargo.Quantity(OreA));
        Assert.Equal(20, world.Cargo.TotalWeight);
        var left = Assert.Single(world.Collectibles);
        Assert.Equal(("p2", 5), (left.Id, left.Quantity));
    }

    [Fact]
    public void EarlierPileInCatalogOrderTakesCapacityFirst()
    {
        // Two piles overlapped by the start position: catalog order wins, the second gets only the leftover capacity.
        var world = World(Catalog(60, Pile("first", OreB, 1, y: 0), Pile("second", OreB, 4, y: 0)));
        world.Tick(default);
        Assert.Equal(2, world.Cargo.Quantity(OreB));
        var left = Assert.Single(world.Collectibles);
        Assert.Equal(("second", 3), (left.Id, left.Quantity));
    }

    [Fact]
    public void PersistedInventorySeedsTheHoldAndCountsAgainstCapacity()
    {
        var world = World(Catalog(100, Pile("p1", OreA, 8)), new Dictionary<string, int> { [OreA] = 5 });
        Burn(world);
        Assert.Equal(10, world.Cargo.Quantity(OreA));
        Assert.Equal(3, Assert.Single(world.Collectibles).Quantity);
        Assert.Equal(13, world.Cargo.Quantity(OreA) + Ground(world, OreA));
    }

    [Fact]
    public void PickupConservesEveryItemAcrossAMixedFlight()
    {
        var piles = new[]
        {
            Pile("a1", OreA, 3, y: -50), Pile("b1", OreB, 7, y: -100), Pile("a2", OreA, 9, y: -150),
            Pile("f1", Feather, 2, y: -200),
        };
        var world = World(Catalog(120, piles));
        var before = Items().ToDictionary(i => i.Id, i => piles.Where(p => p.ItemId == i.Id).Sum(p => p.Quantity));
        for (var tick = 0; tick < 600; tick++)
        {
            world.Tick(new ShipInput(tick < 300 ? 1 : 0, 0, false));
            foreach (var item in Items())
                Assert.Equal(before[item.Id], world.Cargo.Quantity(item.Id) + Ground(world, item.Id));
            Assert.InRange(world.Cargo.TotalWeight, 0, 120);
        }
        Assert.True(world.Cargo.Quantity(OreA) > 0);
    }

    [Fact]
    public void IdenticalInputsProduceIdenticalCargoAtAnyRenderRate()
    {
        var piles = new[] { Pile("a1", OreA, 3, y: -50), Pile("b1", OreB, 7, y: -100), Pile("a2", OreA, 9, y: -150) };
        ShipInput Input(long tick) => new(tick < 200 ? 1 : 0, tick % 90 < 20 ? 0.1f : 0, false);
        var expected = World(Catalog(80, piles));
        var actual = World(Catalog(80, piles));
        var replay = World(Catalog(80, piles));
        for (var tick = 1; tick <= 600; tick++)
        {
            expected.Tick(Input(tick));
            replay.Tick(Input(tick));
        }
        var runner = new FixedStepRunner(actual);
        for (var frame = 0; frame < 30 * 10; frame++)
            runner.Advance(1.0 / 30, Input);
        foreach (var other in new[] { actual, replay })
        {
            TestWorld.SameWorld(expected, other);
            Assert.Equal(expected.Cargo.Items, other.Cargo.Items);
            Assert.Equal(expected.Cargo.TotalWeight, other.Cargo.TotalWeight);
            Assert.Equal(expected.Collectibles.Select(c => (c.Id, c.Quantity)), other.Collectibles.Select(c => (c.Id, c.Quantity)));
        }
        Assert.True(expected.Cargo.TotalWeight > 0);
    }

    [Fact]
    public void WorldWithoutCargoDataBehavesAsBefore()
    {
        var world = new WorldSimulation(TestWorld.Catalog());
        Burn(world, 10);
        Assert.Empty(world.Collectibles);
        Assert.Empty(world.Cargo.Items);
        Assert.Equal(0, world.Cargo.Capacity);
    }

    [Fact]
    public void CatalogFitsAnswersTheSamePolicyQuestionAsThePersistenceInterface()
    {
        var catalog = Catalog(100);
        ICargoPolicy policy = new CatalogCargoPolicy(catalog);
        var profile = PlayerProfile.CreateNew("fixture-profile-1", 0, null);
        Assert.True(policy.Fits(profile, new Dictionary<string, int> { [OreA] = 10 }));
        Assert.False(policy.Fits(profile, new Dictionary<string, int> { [OreA] = 11 }));
        Assert.False(policy.Fits(profile, new Dictionary<string, int> { ["fixture-unknown"] = 1 }));
        Assert.False(policy.Fits(profile, new Dictionary<string, int> { [OreA] = -1 }));
        Assert.False(policy.Fits(profile, new Dictionary<string, int> { [OreA] = int.MaxValue, [OreB] = int.MaxValue }));
        var hold = new CargoHold(catalog, profile.Inventory.Add(OreA, 10));
        Assert.True(policy.Fits(profile, hold.Items));
    }

    [RequiresOriginalAssetsFact]
    public void OriginalResourceMassesDriveTheSameRules()
    {
        // Weights are the raw unscaled ITM mass field of the seven original Resources records (RT-F013..F019).
        // The capacity is a NEW fixture: the native per-hull threshold formula (RT-F030) is not resolved.
        var items = OriginalItemCatalog.Load(OriginalAssets.Root!).Resources
            .Select(i => new CargoItemDefinition(i.Id.ToString(), i.Mass.Value)).ToArray();
        Assert.Equal(7, items.Length);
        var catalog = new CargoCatalog(300_000, items);
        Assert.Equal(50_000, catalog.UnitWeight("2003")); // Tellerium
        Assert.Equal(200_000, catalog.UnitWeight("2008")); // Wreckage
        var hold = new CargoHold(catalog);
        Assert.Equal(new CargoPickupResult(10, 6), hold.Add("2003", 10)); // 300000 / 50000
        Assert.Equal(CargoPickupOutcome.Refused, hold.Add("2008", 1).Outcome);
        Assert.Equal(300_000, hold.TotalWeight);
    }

    private sealed class CatalogCargoPolicy(CargoCatalog catalog) : ICargoPolicy
    {
        public bool Fits(PlayerProfile current, IReadOnlyDictionary<string, int> proposedInventory) =>
            catalog.Fits(proposedInventory);
    }
}
