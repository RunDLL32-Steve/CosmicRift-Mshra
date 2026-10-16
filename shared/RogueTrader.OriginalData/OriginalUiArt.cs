using System.Globalization;

namespace RogueTrader.OriginalData;

/// <summary>
/// One <c>Key=archive,member</c> reference of a CFG UI section (<c>[Uiart]</c> / <c>[Uiwav]</c>), RT-F025 data only.
/// <see cref="RawValue"/> is the text after '=' exactly as stored. When it is empty (for example <c>NotepadBorder=</c>)
/// the entry is explicitly empty: <see cref="Archive"/> and <see cref="Member"/> are null and nothing is defaulted.
/// <see cref="Archive"/> is the first token (it names a BLO archive by its base name; the CFG spelling's case is preserved);
/// <see cref="Member"/> is the second token. What the member is (sprite, sound, record) is NOT established here, and
/// whether the member exists inside the archive is not checked (BLO content is not available to this view).
/// </summary>
public sealed record OriginalArchiveRef(int LineNumber, string Key, string RawValue, string? Archive, string? Member)
{
    public bool IsEmpty => RawValue.Length == 0;

    /// <summary>Parses a raw value; throws on anything other than empty or exactly two non-empty, whitespace-free tokens.</summary>
    public static OriginalArchiveRef Parse(CfgEntry entry)
    {
        var v = entry.Value;
        if (v.Length == 0) return new OriginalArchiveRef(entry.LineNumber, entry.Key, v, null, null);
        var parts = v.Split(',');
        if (parts.Length != 2 || parts.Any(p => p.Length == 0 || p.Any(char.IsWhiteSpace)))
            throw new OriginalDataException(
                $"CFG line {entry.LineNumber}: '{entry.Key}={v}' is not empty and not 'archive,member' (two non-empty, whitespace-free tokens).");
        return new OriginalArchiveRef(entry.LineNumber, entry.Key, v, parts[0], parts[1]);
    }
}

/// <summary>An <c>archive,member</c> reference section of the Rogue CFG in file order, with its header line number.</summary>
public sealed class OriginalArchiveRefSection
{
    internal OriginalArchiveRefSection(string name, int lineNumber, IReadOnlyList<OriginalArchiveRef> entries)
    {
        Name = name;
        LineNumber = lineNumber;
        Entries = entries;
    }

    public string Name { get; }
    /// <summary>Line number of the <c>[Name]</c> header.</summary>
    public int LineNumber { get; }
    public IReadOnlyList<OriginalArchiveRef> Entries { get; }

    public OriginalArchiveRef? Find(string key) =>
        Entries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Distinct non-empty archive names (case-insensitive), spelled as first seen, in first-seen order.</summary>
    public IReadOnlyList<string> Archives()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();
        foreach (var e in Entries)
            if (e.Archive is not null && seen.Add(e.Archive)) list.Add(e.Archive);
        return list;
    }

    /// <summary>
    /// For each distinct archive, whether a file named <c>&lt;archive&gt;.blo</c> (case-insensitive) is in
    /// <paramref name="bloFileNames"/> (bare file names, e.g. a listing of the clone's BLO/ folder). Filename existence only;
    /// the member inside the archive is not checked.
    /// </summary>
    public IReadOnlyList<(string Archive, bool BloFileListed)> ArchiveFiles(IEnumerable<string> bloFileNames)
    {
        var files = new HashSet<string>(bloFileNames, StringComparer.OrdinalIgnoreCase);
        return Archives().Select(a => (a, files.Contains(a + ".blo"))).ToList();
    }
}

/// <summary>Reader for the raw <c>[Uiart]</c> and <c>[Uiwav]</c> sections of CFG/rogue.cfg (RT-F025, data only).</summary>
public static class OriginalUiArt
{
    public const string UiartSection = "Uiart";
    public const string UiwavSection = "Uiwav";

    public static OriginalArchiveRefSection Uiart(OriginalCfg cfg) => Read(cfg, UiartSection);

    public static OriginalArchiveRefSection Uiwav(OriginalCfg cfg) => Read(cfg, UiwavSection);

    private static OriginalArchiveRefSection Read(OriginalCfg cfg, string name)
    {
        var s = cfg.Section(name);
        return new OriginalArchiveRefSection(s.Name, s.LineNumber, s.Entries.Select(OriginalArchiveRef.Parse).ToList());
    }
}

/// <summary>A raw integer CFG value together with its text and line number; the unit and meaning are not interpreted.</summary>
public readonly record struct OriginalCfgInt(int LineNumber, string Text, int Value);

/// <summary>
/// The raw <c>[Render]</c> section of CFG/rogue.cfg (RT-F025, data only). Values are kept unscaled and uninterpreted:
/// the units of <c>StarfieldGapNear/Far</c>, the meaning of <c>Static</c>, <c>FlattenLights</c>, <c>TrailsFirst</c> and
/// <c>VehTrailsFirst</c>, and the byte order of <c>BackgroundColor</c> are NOT established. <see cref="BackgroundColorText"/>
/// is the stored text (<c>0x000000</c>); <see cref="BackgroundColorHex"/> is only that hex number parsed, with no
/// channel interpretation.
/// </summary>
public sealed class OriginalRenderSettings
{
    public const string SectionName = "Render";

    private OriginalRenderSettings(CfgSection s)
    {
        LineNumber = s.LineNumber;
        Entries = s.Entries;
        Static = Plain(s, "Static");
        FlattenLights = Plain(s, "FlattenLights");
        TrailsFirst = Plain(s, "TrailsFirst");
        VehTrailsFirst = Plain(s, "VehTrailsFirst");
        StarfieldGapNear = Plain(s, "StarfieldGapNear");
        StarfieldGapFar = Plain(s, "StarfieldGapFar");
        var bg = Require(s, "BackgroundColor");
        BackgroundColorLine = bg.LineNumber;
        BackgroundColorText = bg.Value;
        BackgroundColorHex = ParseHex(bg);
    }

    public int LineNumber { get; }
    /// <summary>Every entry of the section in file order (including any key not named below).</summary>
    public IReadOnlyList<CfgEntry> Entries { get; }

    public OriginalCfgInt Static { get; }
    public OriginalCfgInt FlattenLights { get; }
    public OriginalCfgInt TrailsFirst { get; }
    public OriginalCfgInt VehTrailsFirst { get; }
    public OriginalCfgInt StarfieldGapNear { get; }
    public OriginalCfgInt StarfieldGapFar { get; }
    public int BackgroundColorLine { get; }
    public string BackgroundColorText { get; }
    /// <summary>The number written after "0x"; no RGB/BGR split is implied.</summary>
    public uint BackgroundColorHex { get; }

    public static OriginalRenderSettings Read(OriginalCfg cfg) => new(cfg.Section(SectionName));

    private static CfgEntry Require(CfgSection s, string key) =>
        s.Find(key) ?? throw new OriginalDataException($"CFG [{s.Name}] has no key '{key}'.");

    private static OriginalCfgInt Plain(CfgSection s, string key)
    {
        var e = Require(s, key);
        if (!int.TryParse(e.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v))
            throw new OriginalDataException($"CFG [{s.Name}] line {e.LineNumber}: '{e.Key}={e.Value}' is not a plain integer.");
        return new OriginalCfgInt(e.LineNumber, e.Value, v);
    }

    private static uint ParseHex(CfgEntry e)
    {
        var t = e.Value;
        if (t.Length < 3 || !(t.StartsWith("0x", StringComparison.Ordinal) || t.StartsWith("0X", StringComparison.Ordinal))
            || !uint.TryParse(t.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var v))
            throw new OriginalDataException($"CFG line {e.LineNumber}: '{e.Key}={t}' is not a 0x-prefixed hexadecimal number.");
        return v;
    }
}
