namespace RogueTrader.OriginalData;

/// <summary>
/// One original outpost "production" item (ITM type 7, category "Equipment": Micro Refinery, GunSat, BeamSat, Micro Repair Dock)
/// linked to its player-made VEH v53 type-5 vehicle row by ITM f[93] == VEH f[2] (RT-F056, data only). Both records are kept
/// raw. Manufacturing cost, deployment, defense, repair/refining behavior, stacking limits and the meaning of the item's
/// own f[7] (mass field) and VEH f[6] are undecoded; nothing is scaled.
/// </summary>
public sealed class OriginalProductionItem
{
    public const int ItemRecordType = 7;
    public const int VehicleLinkField = 93;
    public const int VehicleRecordType = 5;

    internal OriginalProductionItem(OriginalItem item, RawRecord vehicle)
    {
        Item = item;
        Vehicle = vehicle;
    }

    public OriginalItem Item { get; }
    public RawRecord Vehicle { get; }
    public OriginalInt VehicleLink => Item.Record.Int(VehicleLinkField);
    public string VehicleName => Vehicle.Value(3);
}

public static class OriginalProductionItems
{
    /// <summary>Item ids 46..49 (Micro Refinery, GunSat, BeamSat, Micro Repair Dock) in file order.</summary>
    public static readonly int[] ItemIds = [46, 47, 48, 49];
    public const int ItemFieldCount = 94;

    public static IReadOnlyList<OriginalProductionItem> Load(string assetsRoot, OriginalItemCatalog items)
    {
        var veh = AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh);
        return FromRecords(RawRecordReader.Parse(veh, PinnedAssets.Veh.RelativePath), items);
    }

    public static IReadOnlyList<OriginalProductionItem> FromRecords(IReadOnlyList<RawRecord> vehRecords, OriginalItemCatalog items)
    {
        var list = new List<OriginalProductionItem>();
        foreach (var item in items.Items.Where(i => i.RecordType == OriginalProductionItem.ItemRecordType && ItemIds.Contains(i.Id)))
        {
            if (item.Record.FieldCount != ItemFieldCount)
                throw new OriginalDataException($"{item.Record.SourceFile} row {item.RowNumber}: {item.Record.FieldCount} fields, expected {ItemFieldCount}.");
            var link = item.Record.Int(OriginalProductionItem.VehicleLinkField).Value.ToString();
            var rows = vehRecords.Where(v => v.FieldCount > 3 && v.Value(0) == OriginalProductionItem.VehicleRecordType.ToString()
                && v.VersionToken == OriginalHullCatalog.SupportedVehVersion && v.Value(2) == link).ToList();
            if (rows.Count != 1)
                throw new OriginalDataException($"{item.Record.SourceFile} row {item.RowNumber}: VEH id {link} matches {rows.Count} type-5 v53 rows, expected exactly one.");
            list.Add(new OriginalProductionItem(item, rows[0]));
        }
        if (list.Count != ItemIds.Length)
            throw new OriginalDataException($"Found {list.Count} production items, expected {ItemIds.Length}.");
        return list;
    }
}
