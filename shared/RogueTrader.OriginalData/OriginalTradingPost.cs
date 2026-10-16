namespace RogueTrader.OriginalData;

/// <summary>
/// One 14-field trade block of the public Trading Outpost (VEH Rogue.veh row 5, id 404) for RT-F045 (data only). The block
/// starts at a name field ("Trade Tellerium"); f+1 is the ITM id of the credit effect awarded, f+4 and f+5 are kept raw
/// (f+5 is an ITM resource id that equals the refined/raw input in all four pinned blocks). Quantities, eligibility and the
/// award flow are unvalidated; no value is scaled or interpreted.
/// </summary>
public sealed record OriginalTradeRecipe(
    string Name,
    int FirstFieldIndex,
    OriginalInt CreditItemId,
    OriginalInt RawField4,
    OriginalInt InputItemId,
    OriginalItem CreditItem,
    OriginalItem InputItem);

public static class OriginalTradingPost
{
    public const int VehicleId = 404;
    public const int FirstRecipeField = 253;
    public const int RecipeStride = 14;
    public const int RecipeCount = 4;

    /// <summary>The Trading Outpost's recipes (VEH row with id 404) resolved against the item catalog.</summary>
    public static IReadOnlyList<OriginalTradeRecipe> Load(string assetsRoot, OriginalItemCatalog items)
    {
        var veh = AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh);
        return FromRecords(RawRecordReader.Parse(veh, PinnedAssets.Veh.RelativePath), items);
    }

    public static IReadOnlyList<OriginalTradeRecipe> FromRecords(IReadOnlyList<RawRecord> vehRecords, OriginalItemCatalog items)
    {
        var rows = vehRecords.Where(v => v.FieldCount > 2 && v.Value(2) == VehicleId.ToString()).ToList();
        if (rows.Count != 1) throw new OriginalDataException($"VEH id {VehicleId} matches {rows.Count} rows, expected exactly one.");
        var r = rows[0];
        var list = new List<OriginalTradeRecipe>();
        for (var k = 0; k < RecipeCount; k++)
        {
            var f = FirstRecipeField + k * RecipeStride;
            var credit = r.Int(f + 1);
            var input = r.Int(f + 5);
            list.Add(new OriginalTradeRecipe(r.Value(f), f, credit, r.Int(f + 4), input, items.ById(credit.Value), items.ById(input.Value)));
        }
        return list;
    }
}
