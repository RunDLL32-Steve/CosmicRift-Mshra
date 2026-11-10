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
    public void TheHullsOwnWreckDropsOnceEvenFromAnEmptyHoldAsItsOwnPile()
    {
        // RT-F043: the hull's VEH f[68..69] drop is produced by the destruction, so it drops even when the hold was empty, and it
        // is its own pile apart from cargo of the same item. OreB stands in for Wreckage here (NEW fixture).
        var wreck = new ShipWreck { ItemId = OreB, Quantity = 1 };
        var world = World();
        Assert.Single(world.DropCargo("w1:12", new Vector2(0, -300), Hold(0, 2)));
        var pile = world.DropHullWreck("w1:12", new Vector2(0, -300), wreck, "pilot-a");
        Assert.NotNull(pile);
        Assert.Equal(($"{WorldSimulation.WreckPilePrefix}w1:12:{WorldSimulation.HullWreckSegment}{OreB}", OreB, 1, "pilot-a"),
            (pile.Id, pile.ItemId, pile.Quantity, pile.DroppedBy));
        Assert.Equal(720L * WorldSimulation.TickRate, pile.ExpiresAt);
        Assert.Null(world.DropHullWreck("w1:12", new Vector2(0, -300), wreck, "pilot-a")); // never twice
        Assert.Null(world.DropHullWreck("w1:13", Vector2.Zero, null, "pilot-a")); // a hull with no drop
        Assert.Null(world.DropHullWreck("w1:14", Vector2.Zero, new ShipWreck { ItemId = "unknown", Quantity = 1 }, "pilot-a"));
        Assert.Equal(2, world.Collectibles.Count);
    }

    [Fact]
    public void ADeathNeverPaysThePilotWhoDied()
    {
        // Review R1-1: the 20% penalty is below the wreck's 300-credit trade value under 1,500 credits, so the pilot who died must
        // not collect their own hull's wreck; anyone else can. The pilot can only take back the cargo they lost, so what they own
        // after a death (wallet less the penalty, plus all they can pick up) is never more than before it.
        var wreck = new ShipWreck { ItemId = OreB, Quantity = 1 };
        var dead = World(capacity: 10_000);
        dead.DropCargo("w1:20", new Vector2(0, -300), Hold(3, 0));
        dead.DropHullWreck("w1:20", new Vector2(0, -300), wreck, "pilot-a");
        var sector = dead.CaptureSector();
        Assert.Equal("pilot-a", sector.Piles.Single(p => p.ItemId == OreB).DroppedBy);

        var same = new WorldSimulation(TestWorld.Catalog(), new CargoCatalog(10_000, [new(OreA, 10), new(OreB, 25)])) { PilotId = "pilot-a" };
        Assert.True(same.RestoreSector(sector));
        TestWorld.Tick(same, 120, new ShipInput(1, 0, false)); // fly through both piles
        Assert.Equal((3, 0), (same.Cargo.Quantity(OreA), same.Cargo.Quantity(OreB)));
        var left = Assert.Single(same.Collectibles);
        Assert.Equal((OreB, "pilot-a"), (left.ItemId, left.DroppedBy));
        Assert.All(same.Pickups, p => Assert.NotEqual(left.Id, p.PileId));
        for (var wallet = 0L; wallet <= 2_000; wallet += 50)
        {
            var penalty = wallet / 100 * RogueTrader.Persistence.TransactionProcessor.DestructionPenaltyPercent
                + wallet % 100 * RogueTrader.Persistence.TransactionProcessor.DestructionPenaltyPercent / 100;
            var collectable = same.Cargo.Quantity(OreB) * 300L; // the wreck's trade value, if it could be taken back
            Assert.True(wallet - penalty + collectable <= wallet, $"A death at {wallet} credits must not pay.");
        }

        var other = new WorldSimulation(TestWorld.Catalog(), new CargoCatalog(10_000, [new(OreA, 10), new(OreB, 25)])) { PilotId = "pilot-b" };
        Assert.True(other.RestoreSector(sector));
        TestWorld.Tick(other, 120, new ShipInput(1, 0, false));
        Assert.Equal((3, 1), (other.Cargo.Quantity(OreA), other.Cargo.Quantity(OreB))); // anyone else collects it
        Assert.Empty(other.Collectibles);
    }

    [Fact]
    public void NoProfileFromTheSameComputerCanCollectTheWreck()
    {
        // Review R2: a second profile on the same computer shares the sector save, so a 0-credit alt could die and the main profile
        // could sell its 300-credit wreck. The wreck records the computer it was dropped from, and no pilot from there collects it;
        // a pilot from anywhere else still can.
        var dead = World(capacity: 10_000);
        dead.DropHullWreck("w1:30", new Vector2(0, -300), new ShipWreck { ItemId = OreB, Quantity = 1 }, "alt", "pc-1");
        var sector = dead.CaptureSector();
        Assert.Equal(("alt", "pc-1"), (sector.Piles.Single().DroppedBy, sector.Piles.Single().DroppedFrom));

        foreach (var (pilot, origin, collects) in new[] { ("main", "pc-1", false), ("alt", "pc-2", false), ("main", "pc-2", true), ("main", (string?)null, true) })
        {
            var world = new WorldSimulation(TestWorld.Catalog(), new CargoCatalog(10_000, [new(OreA, 10), new(OreB, 25)])) { PilotId = pilot, PilotOrigin = origin };
            Assert.True(world.RestoreSector(sector));
            TestWorld.Tick(world, 120, new ShipInput(1, 0, false)); // fly through the wreck
            Assert.Equal(collects ? 1 : 0, world.Cargo.Quantity(OreB));
            Assert.Equal(collects, world.Collectibles.Count == 0);
        }
    }

    [Fact]
    public void AWreckWithNoItemOrNoQuantityIsRefusedByTheCatalog()
    {
        var catalog = TestWorld.Catalog();
        Assert.Throws<ArgumentException>(() => new GameCatalog(catalog.Ship with { Wreck = new ShipWreck { ItemId = "", Quantity = 1 } }, catalog.Weapon, catalog.Sector));
        Assert.Throws<ArgumentException>(() => new GameCatalog(catalog.Ship with { Wreck = new ShipWreck { ItemId = "2008", Quantity = 0 } }, catalog.Weapon, catalog.Sector));
    }
}
