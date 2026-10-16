using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: FEATURE_CATALOG.md RT-F047 (Minor/Major/Full Repair rows 149/150/162) plus an independent Python audit of the
/// pinned Rogue_mp.itm (eb1514ea). Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalRepairItemTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void RepairRowsKeepRawAmountBuyAndSell()
    {
        var r = OriginalRepairItems.Resolve(Catalog.Value);
        Assert.Equal(new[] { 149, 150, 162 }, r.Select(x => x.Item.RowNumber));
        Assert.Equal(new[] { 39, 40, 43 }, r.Select(x => x.Item.Id));
        Assert.Equal(new[] { "Minor Repair", "Major Repair", "Full Repair" }, r.Select(x => x.Item.Name));
        Assert.Equal(new[] { 5, 10, 50 }, r.Select(x => x.Amount.Value));
        Assert.All(r, x => { Assert.Equal(33, x.Amount.FieldIndex); Assert.Equal(0, x.Item.Buy.Value); Assert.Equal(0, x.Item.Sell.Value); Assert.Equal(1, x.Item.RecordType); });
    }

    [RequiresOriginalAssetsFact]
    public void OnlyTheThreeRepairRowsUseF33AndCreditRowsUseF31()
    {
        var all = OriginalEffectAmountRow.All(Catalog.Value);
        Assert.Equal(new[] { 149, 150, 162 }, all.Select(x => x.Item.RowNumber));
        var credits = Catalog.Value.Items.Where(i => i.RowNumber is 84 or 85 or 90 or 168).ToList();
        Assert.Equal(new[] { 17, 18, 19, 44 }, credits.Select(i => i.Id));
        Assert.Equal(new[] { 80, 55, 300, 20 }, credits.Select(i => i.Record.Int(31).Value));
        Assert.All(credits, i => { Assert.Equal(0, i.Record.Int(33).Value); Assert.Equal(-1, i.Sell.Value); });
    }
}
