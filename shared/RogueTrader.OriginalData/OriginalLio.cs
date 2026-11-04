namespace RogueTrader.OriginalData;

/// <summary>
/// LIO object kinds by the raw f[0] type number. Names follow the InfantryOnline emulator
/// (Infantry-Online-Server bb97579, dotnetcore/Assets/Lio/LioInfo.cs); they are labels, not native evidence.
/// </summary>
public enum OriginalLioKind
{
    Door = 1,
    Switch = 2,
    Flag = 3,
    WarpField = 4,
    Hide = 5,
    Portal = 6,
    Sound = 7,
    Text = 8,
    Parallax = 9,
    Nested = 10,
}

/// <summary>
/// One LIO row (queue G6, data only): the ten common header fields every emulator LIO type reads (f[0]..f[9]) as raw
/// integers/text, plus the whole <see cref="RawRecord"/> so every type-specific field stays available by index.
/// X/Y/Width/Height are the file's own numbers; the emulator treats them as pixels with a top-left origin, which is
/// RECONSTRUCTED (the hide points of the refineries, trading posts and repair docks land on LVL terrain 15/9 tiles
/// when read that way, see OriginalLevelTests). No type-specific meaning is decoded here.
/// </summary>
public sealed record OriginalLioObject(
    RawRecord Record,
    OriginalLioKind Kind,
    string Version,
    int Id,
    string Name,
    int X,
    int Y,
    int Width,
    int Height,
    int RelativeId,
    int HuntFrequency)
{
    /// <summary>1-based physical line of the row in the LIO file.</summary>
    public int Row => Record.RowNumber;

    /// <summary>Fields after the common header (f[10] onward), raw.</summary>
    public IReadOnlyList<RawField> TypeFields => Record.Fields.Skip(OriginalLio.CommonFieldCount).ToArray();
}

/// <summary>
/// Queue G6 LIO reader: parses a hash-verified LIO file into <see cref="OriginalLioObject"/> rows. Only version
/// <c>v23</c> (every row of the pinned Rogue.lio) and the ten emulator type numbers are accepted; anything else
/// throws <see cref="OriginalDataFormatException"/> rather than being guessed. Per-kind field counts of the pinned
/// file are fixed in <see cref="RogueFieldCounts"/> and enforced by <see cref="LoadRogue"/>.
/// </summary>
public static class OriginalLio
{
    public const int CommonFieldCount = 10;
    public const string SupportedVersion = "v23";

    /// <summary>LIO/Rogue.lio at the canonical pin (same pin as <see cref="OriginalSafeZones.LioRogue"/>).</summary>
    public static PinnedAsset LioRogue => OriginalSafeZones.LioRogue;

    /// <summary>Field count of every row of each kind in the pinned Rogue.lio (VERIFIED for those bytes).</summary>
    public static IReadOnlyDictionary<OriginalLioKind, int> RogueFieldCounts { get; } = new Dictionary<OriginalLioKind, int>
    {
        [OriginalLioKind.WarpField] = 18,
        [OriginalLioKind.Hide] = 28,
        [OriginalLioKind.Sound] = 30,
        [OriginalLioKind.Text] = 13,
        [OriginalLioKind.Parallax] = 21,
    };

    /// <summary>Reads, verifies and parses the pinned Rogue.lio, then checks each row against <see cref="RogueFieldCounts"/>.</summary>
    public static IReadOnlyList<OriginalLioObject> LoadRogue(string assetsRoot)
    {
        var rows = Parse(AssetIntegrity.ReadVerified(assetsRoot, LioRogue), LioRogue.RelativePath);
        foreach (var row in rows)
        {
            if (!RogueFieldCounts.TryGetValue(row.Kind, out var expected) || row.Record.FieldCount != expected)
                throw new OriginalDataFormatException(row.Record.SourceFile, row.Row, null,
                    $"{row.Kind} row has {row.Record.FieldCount} fields; the pinned file has {(RogueFieldCounts.TryGetValue(row.Kind, out var e) ? e.ToString() : "no rows of this kind")}.");
        }
        return rows;
    }

    public static IReadOnlyList<OriginalLioObject> Parse(ReadOnlySpan<byte> bytes, string sourceFile) =>
        FromRecords(RawRecordReader.Parse(bytes, sourceFile));

    public static IReadOnlyList<OriginalLioObject> FromRecords(IReadOnlyList<RawRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        var result = new List<OriginalLioObject>(records.Count);
        foreach (var r in records)
        {
            if (r.FieldCount < CommonFieldCount)
                throw new OriginalDataFormatException(r.SourceFile, r.RowNumber, null, $"LIO row has {r.FieldCount} fields; the common header needs {CommonFieldCount}.");
            var type = r.Int(0).Value;
            if (!Enum.IsDefined(typeof(OriginalLioKind), type))
                throw new OriginalDataFormatException(r.SourceFile, r.RowNumber, 0, $"unknown LIO type {type}.");
            var version = r.Value(1);
            if (version != SupportedVersion)
                throw new OriginalDataFormatException(r.SourceFile, r.RowNumber, 1, $"unsupported LIO version '{version}'; only {SupportedVersion} is supported.");
            result.Add(new OriginalLioObject(r, (OriginalLioKind)type, version, r.Int(2).Value, r.Value(3),
                r.Int(4).Value, r.Int(5).Value, r.Int(6).Value, r.Int(7).Value, r.Int(8).Value, r.Int(9).Value));
        }
        return result;
    }
}
