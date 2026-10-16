using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: FEATURE_CATALOG.md RT-F043 (Asp VEH f[68..69]=2008,1) plus an independent Python audit of the pinned
/// Rogue.veh and Rogue_mp.itm (eb1514ea). Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalHullDropTests
{
    [RequiresOriginalAssetsFact]
    public void AllTwelveHullsDropOneWreckageItem()
    {
        var hulls = OriginalHullCatalog.Load(OriginalAssets.Root!);
        var items = OriginalItemCatalog.Load(OriginalAssets.Root!);
        Assert.Equal(12, hulls.Hulls.Count);
        foreach (var h in hulls.Hulls)
        {
            Assert.Equal(2008, h.DropItemId.Value);
            Assert.Equal(1, h.DropQuantity.Value);
            Assert.Equal(68, h.DropItemId.FieldIndex);
            Assert.Equal(69, h.DropQuantity.FieldIndex);
            var drop = items.ById(h.DropItemId.Value);
            Assert.Equal("Wreckage", drop.Name);
            Assert.Equal(87, drop.RowNumber);
        }
    }

    [RequiresOriginalAssetsFact]
    public void AspMatchesCatalogText()
    {
        var asp = OriginalHullCatalog.Load(OriginalAssets.Root!).ByName("Asp");
        Assert.Equal((2008, 1), (asp.DropItemId.Value, asp.DropQuantity.Value));
    }
}
