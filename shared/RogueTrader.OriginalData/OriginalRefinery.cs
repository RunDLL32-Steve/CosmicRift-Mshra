namespace RogueTrader.OriginalData;

/// <summary>
/// One 14-field block of the public Refinery (VEH Rogue.veh row 1, id 400) for RT-F044 (data only), same layout as the
/// Trading Outpost blocks: name, f+1 output item id, f+4 raw, f+5 input item id. Quantities, radius, conditions and the
/// conversion flow are unvalidated.
/// </summary>
public sealed record OriginalRefineRecipe(
    string Name,
    int FirstFieldIndex,
    OriginalInt OutputItemId,
    OriginalInt RawField4,
    OriginalInt InputItemId,
    OriginalItem Output,
    OriginalItem Input);

public static class OriginalRefinery
{
    public const int VehicleId = 400;
    public const int RecipeCount = 3;

    public static IReadOnlyList<OriginalRefineRecipe> Load(string assetsRoot, OriginalItemCatalog items)
    {
        var veh = AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh);
        return FromRecords(RawRecordReader.Parse(veh, PinnedAssets.Veh.RelativePath), items);
    }

    public static IReadOnlyList<OriginalRefineRecipe> FromRecords(IReadOnlyList<RawRecord> vehRecords, OriginalItemCatalog items)
    {
        var rows = vehRecords.Where(v => v.FieldCount > 2 && v.Value(2) == VehicleId.ToString()).ToList();
        if (rows.Count != 1) throw new OriginalDataException($"VEH id {VehicleId} matches {rows.Count} rows, expected exactly one.");
        var r = rows[0];
        var list = new List<OriginalRefineRecipe>();
        for (var k = 0; k < RecipeCount; k++)
        {
            var f = OriginalTradingPost.FirstRecipeField + k * OriginalTradingPost.RecipeStride;
            var output = r.Int(f + 1);
            var input = r.Int(f + 5);
            list.Add(new OriginalRefineRecipe(r.Value(f), f, output, r.Int(f + 4), input, items.ById(output.Value), items.ById(input.Value)));
        }
        return list;
    }
}
