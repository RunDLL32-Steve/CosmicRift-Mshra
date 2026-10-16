using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: FEATURE_CATALOG.md RT-F045 (VEH row5 id404; ITM credit effects id17/18/19/44 rows 84/85/90/168) plus an independent
/// Python audit of the pinned Rogue.veh/Rogue_mp.itm (eb1514ea). Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalTradingPostTests
{
    private static readonly Lazy<OriginalItemCatalog> Items = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void FourTradeBlocksResolveCreditEffectAndInputResource()
    {
        var r = OriginalTradingPost.Load(OriginalAssets.Root!, Items.Value);
        Assert.Equal(new[] { "Trade Tellerium", "Trade Krandazite", "Trade Wreckage", "Trade Alloy" }, r.Select(x => x.Name));
        Assert.Equal(new[] { 253, 267, 281, 295 }, r.Select(x => x.FirstFieldIndex));
        Assert.Equal(new[] { 18, 17, 19, 44 }, r.Select(x => x.CreditItemId.Value));
        Assert.Equal(new[] { "Tellerium Credits", "Krandazite Credits", "Scrap Credits", "Alloy Credits" }, r.Select(x => x.CreditItem.Name));
        Assert.Equal(new[] { 85, 84, 90, 168 }, r.Select(x => x.CreditItem.RowNumber));
        Assert.Equal(new[] { 2004, 2006, 2008, 2020 }, r.Select(x => x.InputItemId.Value));
        Assert.Equal(new[] { "Refined Tellerium", "Refined Krandazite", "Wreckage", "Alloy" }, r.Select(x => x.InputItem.Name));
        Assert.Equal(new[] { 150, 150, 0, 0 }, r.Select(x => x.RawField4.Value));
    }

    [RequiresOriginalAssetsFact]
    public void CreditEffectRowsKeepTheirRawValueInF31()
    {
        // Rows are type 1, Resources; f[31] holds 55 (Tellerium), 80 (Krandazite), 300 (Scrap), 20 (Alloy): meaning/scale undecoded.
        var credits = new[] { (17, 80), (18, 55), (19, 300), (44, 20) };
        foreach (var (id, v) in credits)
        {
            var i = Items.Value.ById(id);
            Assert.Equal(1, i.RecordType);
            Assert.Equal("Resources", i.Category);
            Assert.Equal(v, i.Record.Int(31).Value);
        }
    }
}
