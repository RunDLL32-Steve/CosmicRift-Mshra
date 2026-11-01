namespace RogueTrader.OriginalData;

/// <summary>
/// One mineral row of LIO/Rogue.lio (RT-F042, data only): a type 5 v23 record whose f[20] is a positive Resources item id.
/// Every field is kept raw by index; nothing here names or interprets a field beyond the row id f[2], the quoted name f[3] and
/// the item id f[20]. This is a narrow row view through <see cref="RawRecordReader"/>, not the queue G6 LIO reader.
/// </summary>
public sealed record OriginalMineralSpawn(int Row, int Id, string Name, OriginalItem Item, IReadOnlyList<OriginalInt> Raw)
{
    /// <summary>Raw integer field <paramref name="index"/> (f[4] onwards; f[23] is empty in every row and is not exposed).</summary>
    public int F(int index) => Raw.Single(r => r.FieldIndex == index).Value;
}

public static class OriginalMineralSpawns
{
    public const int RecordType = 5;
    public const int FieldCount = 28;
    public const int ItemField = 20;
    /// <summary>Integer fields kept raw: f[4..22] and f[24..27] (f[23] is empty).</summary>
    public static readonly IReadOnlyList<int> RawFieldIndexes = [.. Enumerable.Range(4, 19), 24, 25, 26, 27];

    public static IReadOnlyList<OriginalMineralSpawn> Load(string assetsRoot, OriginalItemCatalog items)
    {
        var lio = AssetIntegrity.ReadVerified(assetsRoot, OriginalSafeZones.LioRogue);
        return FromRecords(RawRecordReader.Parse(lio, OriginalSafeZones.LioRogue.RelativePath), items);
    }

    public static IReadOnlyList<OriginalMineralSpawn> FromRecords(IReadOnlyList<RawRecord> lio, OriginalItemCatalog items)
    {
        ArgumentNullException.ThrowIfNull(lio);
        ArgumentNullException.ThrowIfNull(items);
        var resources = items.Resources.ToDictionary(i => i.Id);
        var list = new List<OriginalMineralSpawn>();
        foreach (var r in lio)
        {
            if (r.FieldCount == 0 || r.Value(0) != "5") continue;
            if (r.FieldCount != FieldCount || r.VersionToken != "v23")
                throw new OriginalDataException($"LIO row {r.RowNumber}: type 5 record has {r.FieldCount} fields ({r.VersionToken}), expected {FieldCount} (v23).");
            var item = r.Int(ItemField).Value;
            if (item <= 0) continue; // negative f[20] rows place actors (for example MeteorShower -402), not minerals
            if (!resources.TryGetValue(item, out var resource))
                throw new OriginalDataException($"LIO row {r.RowNumber}: f[20]={item} is not a Resources item.");
            list.Add(new OriginalMineralSpawn(r.RowNumber, r.Int(2).Value, r.Value(3), resource,
                RawFieldIndexes.Select(r.Int).ToArray()));
        }
        return list;
    }
}
