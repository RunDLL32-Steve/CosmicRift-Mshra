using System.Text;

namespace RogueTrader.OriginalData;

/// <summary>
/// The fields of the 8 ITM type-7 rows (94 fields each) used by the Outpost data view (RT-F055, data only).
/// f[93] (the last field) holds the id of a VEH type-5 row in all eight rows (45 -> 413, 46 -> 412, 47 -> 410,
/// 48 -> 411, 49 -> 408, 83 -> 425, 84 -> 426, 85 -> 427). The link is established by that 8-of-8 match against
/// existing VEH ids, not by documentation. f[34] and f[35] are exposed raw only (candidate "required item id / count":
/// 0/0 for rows 45-49, 2023/1 for the three Manufactories; not validated).
/// </summary>
public static class ItmType7Fields
{
    public const int RecordType = 7;
    public const int FieldCount = 94;
    public const int VehicleIdField = 93;
    public const int Raw34 = 34;
    public const int Raw35 = 35;
}

/// <summary>An ITM type-7 row linked through f[93] to exactly one VEH type-5 row, with the raw f[34]/f[35] pair.</summary>
public sealed record OriginalType7VehicleLink(OriginalItem Item, OriginalInt VehicleIdField, OriginalVehicleRecord Vehicle, OriginalInt Raw34, OriginalInt Raw35);

public static class OriginalType7Links
{
    /// <summary>
    /// Every ITM type-7 row in file order, each linked to its VEH type-5 row. A type-7 row that is not 94 fields long,
    /// or whose f[93] is not the id of exactly one type-5 VEH row, throws <see cref="OriginalDataException"/>.
    /// </summary>
    public static IReadOnlyList<OriginalType7VehicleLink> Resolve(OriginalItemCatalog items, OriginalVehicleCatalog vehicles)
    {
        var list = new List<OriginalType7VehicleLink>();
        foreach (var item in items.Items.Where(i => i.RecordType == ItmType7Fields.RecordType))
        {
            if (item.Record.FieldCount != ItmType7Fields.FieldCount)
                throw new OriginalDataException($"{item.Record.SourceFile} row {item.RowNumber}: type-7 row has {item.Record.FieldCount} fields, expected {ItmType7Fields.FieldCount}.");
            var vid = item.Record.Int(ItmType7Fields.VehicleIdField);
            var matches = vehicles.Computers.Where(v => v.Id == vid.Value).ToList();
            if (matches.Count != 1)
                throw new OriginalDataException($"{item.Record.SourceFile} row {item.RowNumber}: f[{ItmType7Fields.VehicleIdField}]={vid.Value} matches {matches.Count} VEH type-5 rows, expected exactly one.");
            list.Add(new OriginalType7VehicleLink(item, vid, matches[0], item.Record.Int(ItmType7Fields.Raw34), item.Record.Int(ItmType7Fields.Raw35)));
        }
        return list;
    }
}

/// <summary>One physical line of an NWS text file (line numbers count blank lines).</summary>
public sealed record OriginalNwsLine(int LineNumber, string Text);

/// <summary>Hash-guarded raw text of NWS/Rogue.nws (Latin-1, lines split on CRLF/LF/CR, blank lines kept and counted).</summary>
public sealed class OriginalNws
{
    /// <summary>NWS/Rogue.nws at <see cref="PinnedAssets.Commit"/> (ASSET_MANIFEST.json originalSha256).</summary>
    public static readonly PinnedAsset Rogue = new("NWS/Rogue.nws", 18753, "7fff837b85d52a78b93d50b5b8721d6e567b72b5989c5a96f4212add90aaa895");

    private OriginalNws(IReadOnlyList<OriginalNwsLine> lines) => Lines = lines;

    public IReadOnlyList<OriginalNwsLine> Lines { get; }

    /// <summary>The text of 1-based <paramref name="lineNumber"/>.</summary>
    public string Text(int lineNumber) =>
        lineNumber >= 1 && lineNumber <= Lines.Count ? Lines[lineNumber - 1].Text
        : throw new OriginalDataException($"NWS has {Lines.Count} lines; no line {lineNumber}.");

    public IReadOnlyList<OriginalNwsLine> Range(int first, int last)
    {
        _ = Text(first);
        _ = Text(last);
        return Lines.Skip(first - 1).Take(last - first + 1).ToList();
    }

    public static OriginalNws LoadRogue(string assetsRoot) => Parse(AssetIntegrity.ReadVerified(assetsRoot, Rogue));

    public static OriginalNws Parse(ReadOnlySpan<byte> bytes)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var lines = new List<OriginalNwsLine>();
        var start = 0;
        while (start < text.Length)
        {
            var end = start;
            while (end < text.Length && text[end] != '\r' && text[end] != '\n') end++;
            lines.Add(new OriginalNwsLine(lines.Count + 1, text.Substring(start, end - start)));
            if (end >= text.Length) break;
            start = end + 1;
            if (text[end] == '\r' && start < text.Length && text[start] == '\n') start++;
        }
        return new OriginalNws(lines);
    }
}

/// <summary>
/// One 14-field block in the Rogue Trader Outpost VEH row (f[253 + 14k], k = 0..7), all fields kept raw. f+1 is an ITM id:
/// the credit effect for the four "Trade" blocks, the type-7 item (46..49) for the four "Build" blocks. f+5 is an ITM id
/// (refined resource or Alloy 2020). For the Build blocks f+6 and f+7 equal the alloy and credit counts written in
/// NWS lines 112-120 (10/200, 20/400, 20/500, 20/500); they are exposed as raw candidates only, since the native
/// production flow is not validated. Layout of f+0..f+7 beyond that is unnamed.
/// </summary>
public sealed record OriginalOutpostBlock(
    string Name,
    int FirstFieldIndex,
    IReadOnlyList<RawField> Fields,
    OriginalInt ItemIdField,
    OriginalInt Raw2,
    OriginalInt InputItemIdField,
    OriginalInt Raw6,
    OriginalInt Raw7,
    OriginalItem Item,
    OriginalItem InputItem)
{
    /// <summary>True when f+1 resolves to an ITM type-7 row (a Build block) rather than a credit effect.</summary>
    public bool ItemIsType7 => Item.RecordType == ItmType7Fields.RecordType;
}

/// <summary>
/// Raw data view of the Rogue Trader Outpost (RT-F055, data clause only): ITM row 169 "Trading Post" (id 45, type 7),
/// its linked VEH row 13 "Rogue Trader Outpost" (id 413, type 5), ITM row 199 "Nanobot Control System" (id 2023, type 4),
/// the eight production/trade blocks of VEH row 13, and the hash-guarded NWS text lines 106-110. No placement, collision,
/// owner, safe-zone, factory-prerequisite or cost behavior is implemented or validated here.
/// </summary>
public sealed class OriginalRogueTraderOutpost
{
    public const int TradingPostItemId = 45;
    public const int OutpostVehicleId = 413;
    public const int NanobotControlItemId = 2023;
    public const int FirstBlockField = OriginalTradingPost.FirstRecipeField;
    public const int BlockStride = OriginalTradingPost.RecipeStride;
    public const int BlockCount = 8;
    public const int NwsFirstLine = 106;
    public const int NwsLastLine = 110;

    private OriginalRogueTraderOutpost(OriginalType7VehicleLink link, OriginalItem nanobot, IReadOnlyList<OriginalOutpostBlock> blocks, IReadOnlyList<OriginalNwsLine> nws)
    {
        Link = link;
        NanobotControl = nanobot;
        Blocks = blocks;
        NwsPurchaseText = nws;
    }

    /// <summary>ITM row 169 linked to VEH row 13.</summary>
    public OriginalType7VehicleLink Link { get; }
    public OriginalItem TradingPostItem => Link.Item;
    public OriginalVehicleRecord Vehicle => Link.Vehicle;
    /// <summary>ITM row 199, id 2023.</summary>
    public OriginalItem NanobotControl { get; }
    /// <summary>The eight 14-field blocks of VEH row 13 in file order.</summary>
    public IReadOnlyList<OriginalOutpostBlock> Blocks { get; }
    /// <summary>NWS/Rogue.nws lines 106-110, verbatim.</summary>
    public IReadOnlyList<OriginalNwsLine> NwsPurchaseText { get; }

    public static OriginalRogueTraderOutpost Load(string assetsRoot) =>
        Resolve(OriginalItemCatalog.Load(assetsRoot), OriginalVehicleCatalog.Load(assetsRoot), OriginalNws.LoadRogue(assetsRoot));

    public static OriginalRogueTraderOutpost Resolve(OriginalItemCatalog items, OriginalVehicleCatalog vehicles, OriginalNws nws)
    {
        var link = OriginalType7Links.Resolve(items, vehicles).SingleOrDefault(l => l.Item.Id == TradingPostItemId)
            ?? throw new OriginalDataException($"No ITM type-7 row with id {TradingPostItemId}.");
        if (link.Vehicle.Id != OutpostVehicleId)
            throw new OriginalDataException($"ITM id {TradingPostItemId} links to VEH id {link.Vehicle.Id}, expected {OutpostVehicleId}.");
        var veh = link.Vehicle;
        var blocks = new List<OriginalOutpostBlock>();
        for (var k = 0; k < BlockCount; k++)
        {
            var f = FirstBlockField + k * BlockStride;
            var fields = Enumerable.Range(f, BlockStride).Select(veh.Record.Field).ToList();
            var item = veh.Int(f + 1);
            var input = veh.Int(f + 5);
            blocks.Add(new OriginalOutpostBlock(veh.Record.Value(f), f, fields, item, veh.Int(f + 2), input, veh.Int(f + 6), veh.Int(f + 7),
                items.ById(item.Value), items.ById(input.Value)));
        }
        return new OriginalRogueTraderOutpost(link, items.ById(NanobotControlItemId), blocks, nws.Range(NwsFirstLine, NwsLastLine));
    }
}
