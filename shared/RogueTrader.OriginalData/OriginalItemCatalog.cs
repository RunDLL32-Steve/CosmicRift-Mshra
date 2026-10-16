namespace RogueTrader.OriginalData;

/// <summary>
/// Loads every row of ITM/Rogue_mp.itm. Every non-blank row must carry version token <c>v57</c>, at least
/// <see cref="ItmFields.MinFieldCount"/> fields, an integer type and id, and ids must be unique; otherwise
/// <see cref="OriginalItemException"/> is thrown. Resources are the rows with type 4 and category "Resources"
/// (type 4 also holds ammo; HyperCore and Nanobot Control System have other types).
/// </summary>
public sealed class OriginalItemCatalog
{
    public const string SupportedVersion = "v57";
    public const int ResourceRecordType = 4;
    public const string ResourceCategory = "Resources";

    private OriginalItemCatalog(IReadOnlyList<OriginalItem> items) => Items = items;

    /// <summary>All items in file order.</summary>
    public IReadOnlyList<OriginalItem> Items { get; }

    /// <summary>The type-4 "Resources" items in file order.</summary>
    public IEnumerable<OriginalItem> Resources =>
        Items.Where(i => i.RecordType == ResourceRecordType && i.Category == ResourceCategory);

    public OriginalItem ById(int id) =>
        Items.FirstOrDefault(i => i.Id == id) ?? throw new KeyNotFoundException($"No original item with id {id}.");

    public static OriginalItemCatalog Load(string assetsRoot) =>
        FromRecords(RawRecordReader.Parse(AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Itm), PinnedAssets.Itm.RelativePath));

    public static OriginalItemCatalog FromRecords(IReadOnlyList<RawRecord> records)
    {
        var items = new List<OriginalItem>();
        var seen = new Dictionary<int, int>();
        foreach (var r in records)
        {
            if (r.VersionToken != SupportedVersion)
                throw new OriginalItemException($"{r.SourceFile} row {r.RowNumber}: unsupported version '{r.VersionToken}', expected {SupportedVersion}.");
            if (r.FieldCount < ItmFields.MinFieldCount)
                throw new OriginalItemException($"{r.SourceFile} row {r.RowNumber}: {r.FieldCount} fields, expected at least {ItmFields.MinFieldCount}.");
            var item = new OriginalItem(r);
            _ = item.RecordType;
            if (seen.TryGetValue(item.Id, out var first))
                throw new OriginalItemException($"{r.SourceFile} row {r.RowNumber}: id {item.Id} already used by row {first}.");
            seen[item.Id] = r.RowNumber;
            items.Add(item);
        }
        return new OriginalItemCatalog(items);
    }
}

public sealed class OriginalItemException(string message) : OriginalDataException(message);
