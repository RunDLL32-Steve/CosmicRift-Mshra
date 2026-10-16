using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RogueTrader.OriginalData;

/// <summary>One section header line (<c>~B~4Title~2~B</c>) of the archived keys.txt, with the colour codes kept raw and stripped.</summary>
public sealed record KeyListingSection(int LineNumber, string RawLine, string Title);

/// <summary>
/// One action line of the archived keys.txt. <see cref="Tokens"/> are the raw <c>~~&lt;n&gt;key</c> substitution token numbers in line
/// order (one or two per line); they are placeholders the original client replaced with the player's binding. They are NOT key
/// codes and are not mapped to any key here. <see cref="Separators"/> holds the raw text between consecutive tokens
/// (<c>Tokens.Count - 1</c> entries, e.g. " / "). <see cref="RawNote"/> is the trailing text after the last token, or null.
/// </summary>
public sealed record KeyListingAction(
    int LineNumber,
    int SectionIndex,
    string RawLine,
    string RawLabel,
    string Label,
    IReadOnlyList<int> Tokens,
    IReadOnlyList<string> Separators,
    string? RawNote,
    string? Note);

/// <summary>
/// Raw-preserving reader for the archived Rogue <c>keys.txt</c> (the "Keystroke Listing", RT-F026 / RT-F368 source). The file is
/// NOT in the pinned fowlmouth/cosmic-rift-assets clone and is NOT redistributed in this repository (licence unknown); it is read
/// from a caller-supplied path and verified against its pinned provenance (SOURCE_MANIFEST.json: InfantryOnline/Zone-Assets commit
/// 401633b9, git blob e8dc82bb, 1942 bytes, plus the SHA-256 recorded here) before parsing. Colour codes (<c>~B ~1 ~2 ~4 ~5</c>)
/// are kept raw and also stripped. Substitution tokens are not resolved to key bindings: that needs the native client's action
/// table and is the remaining acceptance work.
/// </summary>
public sealed class OriginalKeyListing
{
    public const string SourceRepository = "InfantryOnline/Zone-Assets";
    public const string SourceCommit = "401633b9b6829d9e5ae56b76a2563b1b95bbe4b9";
    public const string SourcePath = "Incompatible Zone Archives/SilentClientCrash/rogue/keys.txt";
    public const long Length = 1942;
    public const string GitBlobSha1 = "e8dc82bb2e4d11a6aa28d0b9ee68d5df3613d7ba";
    public const string Sha256 = "d004b97b69a97a2f3b4917dcf911d3a93f06d406e886a29dc5dc486a07e014f3";

    private static readonly Regex TokenPattern = new(@"~~(\d+)key", RegexOptions.CultureInvariant);
    private static readonly Regex ColourCode = new(@"~[0-9A-Za-z]", RegexOptions.CultureInvariant);

    private OriginalKeyListing(IReadOnlyList<KeyListingSection> sections, IReadOnlyList<KeyListingAction> actions, int lineCount)
    {
        Sections = sections;
        Actions = actions;
        LineCount = lineCount;
    }

    /// <summary>Number of text lines in the file (a final line terminator does not start a new line).</summary>
    public int LineCount { get; }

    public IReadOnlyList<KeyListingSection> Sections { get; }

    /// <summary>All action lines in file order.</summary>
    public IReadOnlyList<KeyListingAction> Actions { get; }

    /// <summary>The unique action whose stripped label equals <paramref name="label"/> (ordinal); throws if absent or duplicated.</summary>
    public KeyListingAction Action(string label)
    {
        var m = Actions.Where(a => string.Equals(a.Label, label, StringComparison.Ordinal)).ToList();
        if (m.Count != 1) throw new OriginalDataException($"keys.txt has {m.Count} actions labelled '{label}', expected exactly one.");
        return m[0];
    }

    public IReadOnlyList<KeyListingAction> ActionsIn(int sectionIndex) => Actions.Where(a => a.SectionIndex == sectionIndex).ToList();

    /// <summary>Git blob id of <paramref name="bytes"/>: lowercase hex of sha1("blob &lt;length&gt;\0" + bytes).</summary>
    public static string GitBlobSha1Hex(ReadOnlySpan<byte> bytes)
    {
        var header = Encoding.ASCII.GetBytes("blob " + bytes.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0");
        var all = new byte[header.Length + bytes.Length];
        header.CopyTo(all, 0);
        bytes.CopyTo(all.AsSpan(header.Length));
        return Convert.ToHexString(SHA1.HashData(all)).ToLowerInvariant();
    }

    /// <summary>Throws <see cref="AssetIntegrityException"/> unless the bytes match the pinned length, git blob SHA-1 and SHA-256.</summary>
    public static void Verify(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Length)
            throw new AssetIntegrityException(SourcePath, AssetIntegrityFailure.LengthMismatch, Length.ToString(), bytes.Length.ToString());
        var blob = GitBlobSha1Hex(bytes);
        if (!string.Equals(blob, GitBlobSha1, StringComparison.OrdinalIgnoreCase))
            throw new AssetIntegrityException(SourcePath, AssetIntegrityFailure.GitBlobSha1Mismatch, GitBlobSha1, blob);
        var sha = AssetIntegrity.Sha256Hex(bytes);
        if (!string.Equals(sha, Sha256, StringComparison.OrdinalIgnoreCase))
            throw new AssetIntegrityException(SourcePath, AssetIntegrityFailure.Sha256Mismatch, Sha256, sha);
    }

    /// <summary>Reads <paramref name="path"/> (a file outside the repository), verifies the pinned provenance, then parses it.</summary>
    public static OriginalKeyListing Load(string path)
    {
        if (!File.Exists(path))
            throw new AssetIntegrityException(SourcePath, AssetIntegrityFailure.Missing, "file present", "missing: " + path);
        var bytes = File.ReadAllBytes(path);
        Verify(bytes);
        return Parse(Encoding.Latin1.GetString(bytes), SourcePath);
    }

    /// <summary>
    /// Parses keys.txt text with no provenance check (synthetic tests). Blank lines are skipped; every other line must be a section
    /// header (<c>~B~4...</c>) or a tab-indented action line (<c>\t~5Label =~2 ~~Nkey[ sep ~~Mkey][ note]</c>) or
    /// <see cref="OriginalDataException"/> is raised, so nothing is silently dropped.
    /// </summary>
    public static OriginalKeyListing Parse(string text, string sourceFile)
    {
        var sections = new List<KeyListingSection>();
        var actions = new List<KeyListingAction>();
        var lines = text.Split('\n');
        var count = lines.Length;
        if (count > 0 && lines[^1].Length == 0) count--;
        for (var i = 0; i < count; i++)
        {
            var raw = lines[i].TrimEnd('\r');
            var no = i + 1;
            if (raw.Trim().Length == 0) continue;
            if (raw.StartsWith("~B~4", StringComparison.Ordinal))
            {
                sections.Add(new KeyListingSection(no, raw, Strip(raw, sourceFile, no)));
                continue;
            }
            if (!raw.StartsWith("\t~5", StringComparison.Ordinal) || sections.Count == 0)
                throw new OriginalDataException($"{sourceFile} line {no}: expected a section header or a tab-indented action line inside a section.");
            actions.Add(ParseAction(raw, no, sections.Count - 1, sourceFile));
        }
        return new OriginalKeyListing(sections, actions, count);
    }

    private static KeyListingAction ParseAction(string raw, int no, int sectionIndex, string sourceFile)
    {
        var body = raw[1..];
        var eq = body.IndexOf('=');
        if (eq < 0) throw new OriginalDataException($"{sourceFile} line {no}: action line has no '='.");
        var rawLabel = body[..eq];
        var rest = body[(eq + 1)..];
        var matches = TokenPattern.Matches(rest);
        if (matches.Count is < 1 or > 2)
            throw new OriginalDataException($"{sourceFile} line {no}: expected one or two ~~<n>key tokens, found {matches.Count}.");
        var tokens = new List<int>();
        var separators = new List<string>();
        for (var m = 0; m < matches.Count; m++)
        {
            if (!int.TryParse(matches[m].Groups[1].Value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n))
                throw new OriginalDataException($"{sourceFile} line {no}: token '{matches[m].Value}' is not a plain number.");
            tokens.Add(n);
            if (m > 0)
            {
                var prev = matches[m - 1];
                separators.Add(rest[(prev.Index + prev.Length)..matches[m].Index]);
            }
        }
        var last = matches[^1];
        var tail = rest[(last.Index + last.Length)..];
        string? rawNote = null;
        string? note = null;
        if (tail.Trim().Length > 0)
        {
            rawNote = tail.TrimStart();
            note = Strip(rawNote, sourceFile, no).Trim();
        }
        return new KeyListingAction(no, sectionIndex, raw, rawLabel, Strip(rawLabel, sourceFile, no).Trim(), tokens, separators, rawNote, note);
    }

    /// <summary>Removes the two-character <c>~X</c> colour codes; a '~' that does not start such a code is an error.</summary>
    private static string Strip(string raw, string sourceFile, int no)
    {
        var stripped = ColourCode.Replace(raw, "");
        if (stripped.Contains('~'))
            throw new OriginalDataException($"{sourceFile} line {no}: unrecognised '~' sequence after removing colour codes.");
        return stripped;
    }
}

/// <summary>
/// Raw text evidence for RT-F026 from the pinned fowlmouth/cosmic-rift-assets clone (eb1514ea): the Rogue arena's
/// <c>ExitSpectatorMessage</c> (CFG/rogue.cfg line 41, "...Press f11.") and the build-system news line (NWS/Rogue.nws line 108,
/// "...activate it (Press Home)."). Only the source text is pinned; the keys/actions themselves are not implemented here.
/// </summary>
public sealed record OriginalControlReferences(int CfgLine, string CfgSection, string CfgText, int NwsLine, string NwsText)
{
    /// <summary>fowlmouth/cosmic-rift-assets NWS/Rogue.nws at <see cref="PinnedAssets.Commit"/> (ASSET_MANIFEST.json originalSha256, git blob 9eefd664).</summary>
    public static readonly PinnedAsset NwsRogue = new("NWS/Rogue.nws", 18753, "7fff837b85d52a78b93d50b5b8721d6e567b72b5989c5a96f4212add90aaa895");

    public const int ExpectedCfgLine = 41;
    public const int ExpectedNwsLine = 108;

    public static OriginalControlReferences Load(string assetsRoot)
    {
        var cfg = OriginalCfg.LoadRogue(assetsRoot);
        var entry = cfg.Sections.SelectMany(s => s.Entries.Select(e => (Section: s, Entry: e)))
            .Where(x => x.Entry.LineNumber == ExpectedCfgLine).ToList();
        if (entry.Count != 1 || !string.Equals(entry[0].Entry.Key, "ExitSpectatorMessage", StringComparison.Ordinal))
            throw new OriginalDataException($"CFG/rogue.cfg line {ExpectedCfgLine} is not the ExitSpectatorMessage entry.");

        var nws = Encoding.Latin1.GetString(AssetIntegrity.ReadVerified(assetsRoot, NwsRogue)).Split('\n');
        if (nws.Length < ExpectedNwsLine) throw new OriginalDataException($"NWS/Rogue.nws has fewer than {ExpectedNwsLine} lines.");
        return new OriginalControlReferences(ExpectedCfgLine, entry[0].Section.Name, entry[0].Entry.Value,
            ExpectedNwsLine, nws[ExpectedNwsLine - 1].TrimEnd('\r'));
    }
}
