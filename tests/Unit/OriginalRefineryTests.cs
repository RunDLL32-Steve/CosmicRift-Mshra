using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: FEATURE_CATALOG.md RT-F044 (VEH row1 id400) plus an independent Python audit of the pinned Rogue.veh (eb1514ea).
/// Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalRefineryTests
{
    private static readonly Lazy<OriginalItemCatalog> Items = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void ThreeRefineBlocksResolveOutputAndInput()
    {
        var r = OriginalRefinery.Load(OriginalAssets.Root!, Items.Value);
        Assert.Equal(new[] { "Refine Tellerium", "Refine Krandazite", "Refine Iron Ore" }, r.Select(x => x.Name));
        Assert.Equal(new[] { 253, 267, 281 }, r.Select(x => x.FirstFieldIndex));
        Assert.Equal(new[] { 2004, 2006, 2020 }, r.Select(x => x.OutputItemId.Value));
        Assert.Equal(new[] { "Refined Tellerium", "Refined Krandazite", "Alloy" }, r.Select(x => x.Output.Name));
        Assert.Equal(new[] { 2003, 2005, 2019 }, r.Select(x => x.InputItemId.Value));
        Assert.Equal(new[] { "Tellerium", "Krandazite", "Iron Ore" }, r.Select(x => x.Input.Name));
        Assert.Equal(new[] { 150, 150, 0 }, r.Select(x => x.RawField4.Value));
        // Emulator ComputerProduct layout: f+2 Quantity, f+4 Time, f+6 PlayerItemNeededQuantity, f+7 Cost.
        Assert.Equal(new[] { 150, 150, 0 }, r.Select(x => x.Time.Value));
        Assert.All(r, x => Assert.Equal((-1, -1, 0), (x.OutputQuantity.Value, x.InputQuantity.Value, x.Cost.Value)));
        Assert.Equal(new[] { 255, 269, 283 }, r.Select(x => x.OutputQuantity.FieldIndex));
        Assert.Equal(new[] { 259, 273, 287 }, r.Select(x => x.InputQuantity.FieldIndex));
    }

    [RequiresOriginalAssetsFact]
    public void PinnedRefineryHasNoWreckageToAlloyBlock()
    {
        // The catalog cites NWS row270 "wreckage->Alloy"; the pinned VEH record converts Iron Ore (2019) to Alloy (2020), not Wreckage (2008).
        var r = OriginalRefinery.Load(OriginalAssets.Root!, Items.Value);
        Assert.DoesNotContain(r, x => x.InputItemId.Value == 2008);
        Assert.Contains(r, x => x.OutputItemId.Value == 2020 && x.InputItemId.Value == 2019);
    }
}
