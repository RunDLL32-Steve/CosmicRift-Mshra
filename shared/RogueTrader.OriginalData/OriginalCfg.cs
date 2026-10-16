using System.Text;

namespace RogueTrader.OriginalData;

/// <summary>One <c>key=value</c> line of a CFG file; <see cref="Value"/> is everything after the first '=' with no trimming or interpretation.</summary>
public sealed record CfgEntry(int LineNumber, string Key, string Value, string RawLine);

/// <summary>One <c>[Section]</c> of a CFG file in file order, with its entries in file order (duplicate keys are all kept).</summary>
public sealed class CfgSection
{
    internal CfgSection(int lineNumber, string name, List<CfgEntry> entries)
    {
        LineNumber = lineNumber;
        Name = name;
        Entries = entries;
    }

    public int LineNumber { get; }
    public string Name { get; }
    public IReadOnlyList<CfgEntry> Entries { get; }

    /// <summary>The first entry whose key equals <paramref name="key"/> (ordinal, case-insensitive), or null.</summary>
    public CfgEntry? Find(string key) =>
        Entries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Raw integer value of <paramref name="key"/>; throws if the key is absent or the value is not a plain integer.</summary>
    public int Int(string key)
    {
        var e = Find(key) ?? throw new OriginalDataException($"CFG [{Name}] has no key '{key}'.");
        if (!int.TryParse(e.Value, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var v))
            throw new OriginalDataException($"CFG [{Name}] line {e.LineNumber}: '{e.Key}={e.Value}' is not a plain integer.");
        return v;
    }
}

/// <summary>
/// Raw-preserving reader for an original CFG file (INI style, CRLF). Sections and entries keep file order and line numbers;
/// no value is interpreted, scaled or defaulted. Lines that are blank are skipped; any other line before the first section
/// or without '=' raises <see cref="OriginalDataException"/> so nothing is silently dropped.
/// </summary>
public sealed class OriginalCfg
{
    /// <summary>fowlmouth/cosmic-rift-assets CFG/rogue.cfg at <see cref="PinnedAssets.Commit"/> (ASSET_MANIFEST.json originalSha256).</summary>
    public static readonly PinnedAsset Rogue = new("CFG/rogue.cfg", 48399, "82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90");

    private OriginalCfg(IReadOnlyList<CfgSection> sections) => Sections = sections;

    public IReadOnlyList<CfgSection> Sections { get; }

    /// <summary>The unique section named <paramref name="name"/> (case-insensitive); throws if absent or duplicated.</summary>
    public CfgSection Section(string name)
    {
        var m = Sections.Where(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (m.Count != 1) throw new OriginalDataException($"CFG has {m.Count} sections named [{name}], expected exactly one.");
        return m[0];
    }

    public static OriginalCfg LoadRogue(string assetsRoot) =>
        Parse(AssetIntegrity.ReadVerified(assetsRoot, Rogue), Rogue.RelativePath);

    public static OriginalCfg Parse(ReadOnlySpan<byte> bytes, string sourceFile) =>
        Parse(Encoding.Latin1.GetString(bytes), sourceFile);

    public static OriginalCfg Parse(string text, string sourceFile)
    {
        var sections = new List<CfgSection>();
        string? name = null;
        var start = 0;
        List<CfgEntry>? entries = null;
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i].TrimEnd('\r');
            var no = i + 1;
            if (raw.Length == 0) continue;
            if (raw[0] == '[')
            {
                var close = raw.IndexOf(']');
                if (close < 0) throw new OriginalDataException($"{sourceFile} line {no}: unterminated section header.");
                if (close != raw.Length - 1) throw new OriginalDataException($"{sourceFile} line {no}: text after ']' on a section header.");
                if (name is not null) sections.Add(new CfgSection(start, name, entries!));
                name = raw[1..close];
                start = no;
                entries = [];
                continue;
            }
            var eq = raw.IndexOf('=');
            if (name is null || eq < 0)
                throw new OriginalDataException($"{sourceFile} line {no}: expected 'key=value' inside a section.");
            entries!.Add(new CfgEntry(no, raw[..eq], raw[(eq + 1)..], raw));
        }
        if (name is not null) sections.Add(new CfgSection(start, name, entries!));
        return new OriginalCfg(sections);
    }
}

/// <summary>
/// The raw per-terrain access flags of the Rogue arena for RT-F046 (data only): <c>[TerrainN]</c> StoreEnabled, SkillEnabled
/// and TeamChangeEnabled (N = 0..15). Their enforcement is not established here (see TASK_QUEUE.md decision on SkillEnabled).
/// </summary>
public sealed record OriginalTerrainAccess(int Terrain, int Line, int StoreEnabled, int SkillEnabled, int TeamChangeEnabled)
{
    public const int TerrainCount = 16;

    public static IReadOnlyList<OriginalTerrainAccess> All(OriginalCfg cfg)
    {
        var list = new List<OriginalTerrainAccess>();
        for (var t = 0; t < TerrainCount; t++)
        {
            var s = cfg.Section($"Terrain{t}");
            list.Add(new OriginalTerrainAccess(t, s.LineNumber, s.Int("StoreEnabled"), s.Int("SkillEnabled"), s.Int("TeamChangeEnabled")));
        }
        return list;
    }
}
