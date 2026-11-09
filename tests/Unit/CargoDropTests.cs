using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests;

/// <summary>
/// A destroyed ship's cargo spills into space where it died (Steve, Nov 4 2026). Weights, quantities and positions are NEW
/// test fixtures. The default test ship starts at (0, 0) heading up (negative y) at rest.
/// </summary>
public sealed class CargoDropTests
{
    private const string OreA = "fixture-ore-a";
    private const string OreB = "fixture-ore-b";

    private static WorldSimulation World(long capacity = 1000) =>
        new(TestWorld.Catalog(), new CargoCatalog(capacity, [new(OreA, 10), new(OreB, 25)]));

    private static Dictionary<string, int> Hold(int a, int b) => new() { [OreA] = a, [OreB] = b };

    [Fact]
    public void EachItemBecomesOnePileAroundTheWreckThatExpiresAfter720Seconds()
    {
        var world = World();
        var at = new Vector2(0, -300); // beyond the 80-pixel pickup distance of the ship at rest at (0, 0)
        var piles = world.DropCargo("w1:77", at, Hold(3, 2));

        Assert.Equal(new[] { ($"{WorldSimulation.WreckPilePrefix}w1:77:{OreA}", OreA, 3), ($"{WorldSimulation.WreckPilePrefix}w1:77:{OreB}", OreB, 2) },
            piles.Select(p => (p.Id, p.ItemId, p.Quantity)));
        Assert.All(piles, p => Assert.Equal(WorldSimulation.WreckSpreadRadius, Vector2.Distance(p.Position, at), 3));
        Assert.All(piles, p => Assert.Equal(720L * WorldSimulation.TickRate, p.ExpiresAt));
        Assert.Equal(piles, world.Collectibles);

        TestWorld.Tick(world, (int)(720L * WorldSimulation.TickRate) - 1, new ShipInput(0, 0, false));
        Assert.Equal(2, world.Collectibles.Count);
        TestWorld.Tick(world, 1, new ShipInput(0, 0, false));
        Assert.Empty(world.Collectibles); // expiry runs even though this world has no mineral field
    }

    [Fact]
    public void TheSameDestructionNeverSpillsTwiceAndUnknownOrEmptyItemsAreSkipped()
    {
        var world = World();
        Assert.Single(world.DropCargo("w1:5", Vector2.Zero, new Dictionary<string, int> { [OreA] = 4, [OreB] = 0, ["unknown"] = 9 }));
        Assert.Empty(world.DropCargo("w1:5", Vector2.Zero, Hold(4, 0)));
        Assert.Equal(4, world.Collectibles.Single().Quantity);
        Assert.Throws<ArgumentOutOfRangeException>(() => world.DropCargo("w1:6", new Vector2(float.NaN, 0), Hold(1, 0)));
    }

    [Fact]
    public void SpilledCargoIsCarriedIntoTheRespawnedWorldWithItsRemainingLifeAndCanBePickedUp()
    {
        var world = World();
        world.DropCargo("w1:9", new Vector2(0, -300), Hold(3, 0));
        TestWorld.Tick(world, 600, new ShipInput(0, 0, false));

        var next = World();
        Assert.True(next.RestoreSector(world.CaptureSector()));
        var pile = Assert.Single(next.Collectibles);
        Assert.Equal((OreA, 3, 720L * WorldSimulation.TickRate - 600), (pile.ItemId, pile.Quantity, pile.ExpiresAt));

        TestWorld.Tick(next, 120, new ShipInput(1, 0, false)); // fly onto it
        Assert.Equal(3, next.Cargo.Quantity(OreA));
        Assert.Empty(next.Collectibles);
        Assert.Equal(pile.Id, Assert.Single(next.Pickups).PileId);
    }

    [Fact]
    public void TheHullsOwnWreckDropsWithTheCargoEvenFromAnEmptyHold()
    {
        // RT-F043: the hull's VEH f[68..69] drop is produced by the destruction, so it is added to the lost cargo (summed when the
        // hold carried the same item) and still drops when the hold was empty. OreB stands in for Wreckage here (NEW fixture).
        var wreck = new ShipWreck { ItemId = OreB, Quantity = 1 };
        Assert.Equal(new[] { KeyValuePair.Create(OreA, 3), KeyValuePair.Create(OreB, 3) }, WorldSimulation.DestructionDrop(Hold(3, 2), wreck));
        Assert.Equal(new[] { KeyValuePair.Create(OreB, 1) }, WorldSimulation.DestructionDrop(Hold(0, 0), wreck));
        Assert.Equal(new[] { KeyValuePair.Create(OreA, 3) }, WorldSimulation.DestructionDrop(Hold(3, -1), null));
        Assert.Empty(WorldSimulation.DestructionDrop([], null));

        var world = World();
        var pile = Assert.Single(world.DropCargo("w1:12", new Vector2(0, -300), WorldSimulation.DestructionDrop(Hold(0, 0), wreck)));
        Assert.Equal(($"{WorldSimulation.WreckPilePrefix}w1:12:{OreB}", 1), (pile.Id, pile.Quantity));
        Assert.Empty(world.DropCargo("w1:12", new Vector2(0, -300), WorldSimulation.DestructionDrop(Hold(0, 0), wreck))); // never twice
    }

    [Fact]
    public void AWreckWithNoItemOrNoQuantityIsRefusedByTheCatalog()
    {
        var catalog = TestWorld.Catalog();
        Assert.Throws<ArgumentException>(() => new GameCatalog(catalog.Ship with { Wreck = new ShipWreck { ItemId = "", Quantity = 1 } }, catalog.Weapon, catalog.Sector));
        Assert.Throws<ArgumentException>(() => new GameCatalog(catalog.Ship with { Wreck = new ShipWreck { ItemId = "2008", Quantity = 0 } }, catalog.Weapon, catalog.Sector));
    }
}
