namespace RogueTrader.OriginalData;

/// <summary>
/// One 14-field production block of a player-made Nanobot Manufactory VEH row (RT-F057, data only), same stride as the trade
/// blocks (first name at f[253]). Layout read from the pinned names ("Produce GunSat 1:25 A"): f = name, f+1 output ITM id,
/// f+2 output quantity, f+4 raw, f+5 input ITM id, f+6 input quantity. Every value stays raw; the f+6 sentinel -1 on the
/// "Biostorm" block contradicts its "1:1" name and is preserved, not corrected. Cost, build time, radius and the production
/// flow are undecoded.
/// </summary>
public sealed record OriginalManufactoryRecipe(
    string Name,
    int FirstFieldIndex,
    OriginalInt OutputItemId,
    OriginalInt OutputQuantity,
    OriginalInt RawField4,
    OriginalInt InputItemId,
    OriginalInt InputQuantity,
    OriginalItem? Output,
    OriginalItem? Input);

/// <summary>An original manufactory: ITM type-7 item, its VEH v53 type-5 row (via ITM f[93]), the ITM f[34] required computer and its production blocks.</summary>
public sealed class OriginalManufactory
{
    internal OriginalManufactory(OriginalItem item, RawRecord vehicle, OriginalItem? requiredComputer, IReadOnlyList<OriginalManufactoryRecipe> recipes)
    {
        Item = item;
        Vehicle = vehicle;
        RequiredComputer = requiredComputer;
        Recipes = recipes;
    }

    public OriginalItem Item { get; }
    public RawRecord Vehicle { get; }
    /// <summary>Raw ITM f[34] (an item id) and the item it names, if present in the catalog.</summary>
    public OriginalInt RequiredComputerId => Item.Record.Int(OriginalManufactories.RequiredComputerField);
    public OriginalItem? RequiredComputer { get; }
    public IReadOnlyList<OriginalManufactoryRecipe> Recipes { get; }
}

public static class OriginalManufactories
{
    /// <summary>Missile (83), Munitions (84) and Defense (85) Manufactory item ids.</summary>
    public static readonly int[] ItemIds = [83, 84, 85];
    private const int ItemRecordType = 7;
    private const int ItemFieldCount = 94;
    private const int VehicleLinkField = 93;
    private const int VehicleRecordType = 5;
    public const int RequiredComputerField = 34;
    public const int RequiredComputerFlagField = 35;
    public const int FirstRecipeField = OriginalTradingPost.FirstRecipeField;
    public const int RecipeStride = OriginalTradingPost.RecipeStride;

    public static IReadOnlyList<OriginalManufactory> Load(string assetsRoot, OriginalItemCatalog items)
    {
        var veh = AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh);
        return FromRecords(RawRecordReader.Parse(veh, PinnedAssets.Veh.RelativePath), items);
    }

    public static IReadOnlyList<OriginalManufactory> FromRecords(IReadOnlyList<RawRecord> vehRecords, OriginalItemCatalog items)
    {
        var list = new List<OriginalManufactory>();
        foreach (var item in items.Items.Where(i => i.RecordType == ItemRecordType && ItemIds.Contains(i.Id)))
        {
            if (item.Record.FieldCount != ItemFieldCount)
                throw new OriginalDataException($"{item.Record.SourceFile} row {item.RowNumber}: {item.Record.FieldCount} fields, expected {ItemFieldCount}.");
            var link = item.Record.Int(VehicleLinkField).Value.ToString();
            var rows = vehRecords.Where(v => v.FieldCount > 3 && v.Value(0) == VehicleRecordType.ToString()
                && v.VersionToken == OriginalHullCatalog.SupportedVehVersion && v.Value(2) == link).ToList();
            if (rows.Count != 1)
                throw new OriginalDataException($"{item.Record.SourceFile} row {item.RowNumber}: VEH id {link} matches {rows.Count} type-5 v53 rows, expected exactly one.");
            var veh = rows[0];
            var recipes = new List<OriginalManufactoryRecipe>();
            for (var f = FirstRecipeField; f + 6 < veh.FieldCount && veh.Value(f).Length > 0; f += RecipeStride)
            {
                var output = veh.Int(f + 1);
                var input = veh.Int(f + 5);
                recipes.Add(new OriginalManufactoryRecipe(veh.Value(f), f, output, veh.Int(f + 2), veh.Int(f + 4), input, veh.Int(f + 6),
                    items.Items.FirstOrDefault(i => i.Id == output.Value), items.Items.FirstOrDefault(i => i.Id == input.Value)));
            }
            var computerId = item.Record.Int(RequiredComputerField).Value;
            list.Add(new OriginalManufactory(item, veh, items.Items.FirstOrDefault(i => i.Id == computerId), recipes));
        }
        if (list.Count != ItemIds.Length)
            throw new OriginalDataException($"Found {list.Count} manufactories, expected {ItemIds.Length}.");
        return list;
    }
}
