using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RogueTrader.OriginalData;

/// <summary>
/// An NWS line with the nearest heading above it (the closest preceding line that starts with <c>*</c>), both verbatim.
/// <see cref="HeadingDate"/> is the raw <c>M/D/YY</c> text when the heading is a dated one (<c>*******9/08/01</c>) and null for an
/// undated section heading such as <c>********~BGame Overview~B*********</c>.
/// </summary>
public sealed record OriginalNwsEvidenceLine(OriginalNwsLine Line, OriginalNwsLine Heading, string? HeadingDate);

/// <summary>Lines of one pinned file whose text contains a scanned keyword (case-insensitive), with the keywords found.</summary>
public sealed record OriginalKeywordHit(string SourceFile, int LineNumber, IReadOnlyList<string> Keywords, string Text);

/// <summary>
/// Evidence view of RT-F068 "Original character/progression policy" (evidence only, no rule). It pins, from the hash-verified
/// canonical package (eb1514ea): NWS lines 26-29 (character wipe text, under the undated "Game Overview" heading) and 399 (same-race
/// hull purchase, under 9/08/01), NWS 218/220 (spec-mode losses, under 3/12/03); the CFG [StartGame], [Event] wipe/setup, [Owner]
/// Wipe/Profile, [PublicProfile] and [Stat] tracking entries; the RPG f[38] value of every hull row (upstream-parser name
/// CashAdjustment, from FIELD_INTERPRETATION.md); and a keyword scan of the five package files for save/persistence words.
/// <para>
/// What it shows: the package holds client-visible policy text and server event/config strings, but no save-record layout, field list,
/// ordering or storage format; RPG, VEH and ITM contain none of the scanned words. The historical backend schema is therefore not
/// recoverable from these files, and any durable profile layout is a RECONSTRUCTED (NEW) contract. Nothing here applies a wipe, an
/// exchange, a cash value or a reset; the event verbs are not interpreted (RT-F067) and which rule wins between NWS 28 and 399 is
/// not decided.
/// </para>
/// </summary>
public sealed class OriginalProgressionEvidence
{
    public const int NwsOverviewHeading = 25;
    public const int NwsCharacterWipeFirst = 26;
    public const int NwsCharacterWipeLast = 29;
    public const int NwsHullExchange = 399;
    public const int NwsSpecLosesMinerals = 218;
    public const int NwsSpecLosesNanobots = 220;
    /// <summary>RPG field read as the per-hull value; upstream parser (InfantryOnline) name CashAdjustment, recorded in FIELD_INTERPRETATION.md. Not executable evidence.</summary>
    public const int RpgCashAdjustmentField = 38;

    /// <summary>The scanned words (matched case-insensitively as substrings).</summary>
    public static readonly IReadOnlyList<string> Keywords = ["wipe", "character", "save", "persist", "profile", "reset", "account", "login", "logout"];

    /// <summary>[Event] keys (file order) whose names or strings concern wiping, setting up, resetting or changing a player's vehicle (raw, as pinned by RT-F067).</summary>
    public static readonly IReadOnlyList<string> ProgressionEventKeys = ["Hold1", "Hold2", "SysopWipe", "SelfWipe", "FirstTimeInvSetup", "KilledByTeam", "KilledByEnemy", "EnterSpawnNoScore", "ChangeDefaultVehicle", "FirstTimeSkillSetup"];

    /// <summary>[Stat] keys about tracking periods.</summary>
    public static readonly IReadOnlyList<string> StatTrackingKeys = ["TrackDaily", "TrackWeekly", "TrackMonthly", "TrackYearly"];

    private static readonly Regex DatedHeading = new(@"^\*+\s*(\d{1,2}/\d{1,2}/\d{2})\s*$", RegexOptions.CultureInvariant);

    private OriginalProgressionEvidence() { }

    /// <summary>NWS 26..29 in order, each with the line-25 heading (undated).</summary>
    public required IReadOnlyList<OriginalNwsEvidenceLine> CharacterWipe { get; init; }
    /// <summary>NWS 399 with its 9/08/01 heading.</summary>
    public required OriginalNwsEvidenceLine HullExchange { get; init; }
    /// <summary>NWS 218 and 220 with their 3/12/03 heading.</summary>
    public required IReadOnlyList<OriginalNwsEvidenceLine> SpecModeLosses { get; init; }
    /// <summary>Every entry of CFG [StartGame] in file order.</summary>
    public required IReadOnlyList<CfgEntry> StartGame { get; init; }
    /// <summary>The <see cref="ProgressionEventKeys"/> entries of CFG [Event], in key-list (file) order.</summary>
    public required IReadOnlyList<CfgEntry> ProgressionEvents { get; init; }
    /// <summary>CFG [Owner] Wipe and Profile.</summary>
    public required IReadOnlyList<CfgEntry> OwnerFlags { get; init; }
    /// <summary>Every entry of CFG [PublicProfile].</summary>
    public required IReadOnlyList<CfgEntry> PublicProfile { get; init; }
    /// <summary>The <see cref="StatTrackingKeys"/> entries of CFG [Stat].</summary>
    public required IReadOnlyList<CfgEntry> StatTracking { get; init; }
    /// <summary>RPG f[38] of each hull, in RPG file order.</summary>
    public required IReadOnlyList<(OriginalHull Hull, OriginalInt Value)> RpgCashAdjustment { get; init; }
    /// <summary>Keyword hits per pinned file, keyed by the file's relative path (every scanned file is present, possibly with no hits).</summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<OriginalKeywordHit>> KeywordHits { get; init; }
    /// <summary>Where the pinned texts do not line up with each other. Informational; no value is changed.</summary>
    public IReadOnlyList<string> VisibleMismatches { get; private set; } = [];

    /// <summary>The five pinned files scanned, in scan order.</summary>
    public static IReadOnlyList<PinnedAsset> ScannedFiles { get; } = [PinnedAssets.Rpg, PinnedAssets.Veh, PinnedAssets.Itm, OriginalCfg.Rogue, OriginalNws.Rogue];

    /// <summary>Reads and hash-verifies all five files (never writes them) and builds the view.</summary>
    public static OriginalProgressionEvidence Load(string assetsRoot)
    {
        var texts = ScannedFiles.ToDictionary(p => p.RelativePath, p => Encoding.Latin1.GetString(AssetIntegrity.ReadVerified(assetsRoot, p)), StringComparer.Ordinal);
        var hulls = OriginalHullCatalog.Load(assetsRoot);
        var items = OriginalItemCatalog.Load(assetsRoot);
        var cfg = OriginalCfg.Parse(texts[OriginalCfg.Rogue.RelativePath], OriginalCfg.Rogue.RelativePath);
        var nws = OriginalNws.Parse(Encoding.Latin1.GetBytes(texts[OriginalNws.Rogue.RelativePath]));
        return Resolve(hulls, items, cfg, nws, texts);
    }

    /// <param name="fileTexts">Latin-1 text of each file in <see cref="ScannedFiles"/>, keyed by relative path (used for the keyword scan).</param>
    public static OriginalProgressionEvidence Resolve(OriginalHullCatalog hulls, OriginalItemCatalog items, OriginalCfg cfg, OriginalNws nws, IReadOnlyDictionary<string, string> fileTexts)
    {
        var wipe = nws.Range(NwsCharacterWipeFirst, NwsCharacterWipeLast).Select(l => WithHeading(nws, l)).ToList();
        Require(wipe[2].Line, "?wipecharacter");
        var exchange = WithHeading(nws, nws.Range(NwsHullExchange, NwsHullExchange)[0]);
        Require(exchange.Line, "same race as your existing ship hull");
        var losses = new[] { NwsSpecLosesMinerals, NwsSpecLosesNanobots }.Select(n => WithHeading(nws, nws.Range(n, n)[0])).ToList();
        Require(losses[0].Line, "When you go to spec mode you will lose");
        Require(losses[1].Line, "When you go to spec mode you will lose");

        CfgEntry Need(string section, string key) =>
            cfg.Section(section).Find(key) ?? throw new OriginalDataException($"CFG [{section}] has no key '{key}'.");

        var hits = new Dictionary<string, IReadOnlyList<OriginalKeywordHit>>(StringComparer.Ordinal);
        foreach (var file in ScannedFiles)
        {
            if (!fileTexts.TryGetValue(file.RelativePath, out var text))
                throw new OriginalDataException($"No text supplied for {file.RelativePath}.");
            hits[file.RelativePath] = Scan(file.RelativePath, text);
        }

        var view = new OriginalProgressionEvidence
        {
            CharacterWipe = wipe,
            HullExchange = exchange,
            SpecModeLosses = losses,
            StartGame = cfg.Section("StartGame").Entries,
            ProgressionEvents = ProgressionEventKeys.Select(k => Need("Event", k)).ToList(),
            OwnerFlags = [Need("Owner", "Wipe"), Need("Owner", "Profile")],
            PublicProfile = cfg.Section("PublicProfile").Entries,
            StatTracking = StatTrackingKeys.Select(k => Need("Stat", k)).ToList(),
            RpgCashAdjustment = hulls.Hulls.Select(h => (h, h.RpgRecord.Int(RpgCashAdjustmentField))).ToList(),
            KeywordHits = hits,
        };
        view.VisibleMismatches = DescribeMismatches(view, hulls, items);
        return view;
    }

    /// <summary>Same line splitting as <see cref="OriginalNws.Parse"/> (CRLF, LF or CR), so line numbers match for both NWS and CFG.</summary>
    internal static IReadOnlyList<OriginalKeywordHit> Scan(string sourceFile, string text)
    {
        var list = new List<OriginalKeywordHit>();
        foreach (var line in OriginalNws.Parse(Encoding.Latin1.GetBytes(text)).Lines)
        {
            var found = Keywords.Where(k => line.Text.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();
            if (found.Count > 0) list.Add(new OriginalKeywordHit(sourceFile, line.LineNumber, found, line.Text));
        }
        return list;
    }

    private static void Require(OriginalNwsLine line, string needle)
    {
        if (!line.Text.Contains(needle, StringComparison.Ordinal))
            throw new OriginalDataException($"NWS line {line.LineNumber} does not contain '{needle}': '{line.Text}'.");
    }

    private static OriginalNwsEvidenceLine WithHeading(OriginalNws nws, OriginalNwsLine line)
    {
        for (var n = line.LineNumber - 1; n >= 1; n--)
        {
            var h = nws.Lines[n - 1];
            if (h.Text.StartsWith('*'))
            {
                var m = DatedHeading.Match(h.Text);
                return new OriginalNwsEvidenceLine(line, h, m.Success ? m.Groups[1].Value : null);
            }
        }
        throw new OriginalDataException($"NWS line {line.LineNumber} has no heading above it.");
    }

    private static IReadOnlyList<string> DescribeMismatches(OriginalProgressionEvidence v, OriginalHullCatalog hulls, OriginalItemCatalog items)
    {
        var notes = new List<string>();
        notes.Add($"NWS {NwsCharacterWipeFirst + 1}-{NwsCharacterWipeLast} (under undated heading line {v.CharacterWipe[0].Heading.LineNumber} '{v.CharacterWipe[0].Heading.Text}') say a new ship cannot be selected without ?wipecharacter, "
            + $"but NWS {NwsHullExchange} (under '{v.HullExchange.Heading.Text}') adds buying a new hull of the same race; the overview text is undated, so which statement is the later policy is unproven.");
        var selfWipe = v.ProgressionEvents.Single(e => e.Key == "SelfWipe");
        var cfgMentions = v.KeywordHits[OriginalCfg.Rogue.RelativePath].Count(h => h.Text.Contains("wipecharacter", StringComparison.OrdinalIgnoreCase));
        if (cfgMentions == 0)
            notes.Add($"No CFG line contains 'wipecharacter'; CFG line {selfWipe.LineNumber} [Event] SelfWipe='{selfWipe.Value}' is the closest raw string, but the link between the ?wipecharacter command and that event, and what each verb does, are not established (RT-F067).");
        var change = v.ProgressionEvents.Single(e => e.Key == "ChangeDefaultVehicle");
        if (change.Value.Length == 0)
            notes.Add($"CFG line {change.LineNumber} [Event] ChangeDefaultVehicle='{change.Value}' is empty, so the NWS {NwsHullExchange} same-race hull purchase has no CFG event string; what the type-16 'Ship Hulls-*' items (RT-F035) keep or reset on a hull change is not in the package.");
        var cash = v.RpgCashAdjustment.Select(c => c.Value.Value).Distinct().ToList();
        var setup = v.ProgressionEvents.Single(e => e.Key == "FirstTimeInvSetup");
        if (setup.Value.Contains("setcash=0", StringComparison.Ordinal))
            notes.Add($"RPG f[{RpgCashAdjustmentField}] (upstream name CashAdjustment) is {string.Join("/", cash)} on all {hulls.Hulls.Count} hull rows while CFG line {setup.LineNumber} FirstTimeInvSetup='{setup.Value}' contains setcash=0; whether and in which order either sets starting cash is unproven.");
        var defaultV = v.PublicProfile.FirstOrDefault(e => e.Key == "DefaultVItemId");
        if (defaultV is not null && int.TryParse(defaultV.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var dv))
        {
            var item = items.Items.FirstOrDefault(i => i.Id == dv);
            var hull = hulls.Hulls.FirstOrDefault(h => h.VehicleId == dv);
            notes.Add($"CFG line {defaultV.LineNumber} [PublicProfile] DefaultVItemId={defaultV.Value}: ITM id {dv} is {(item is null ? "absent" : $"'{item.Name}'")} and hull VEH id {dv} is {(hull is null ? "absent" : $"'{hull.Name}'")}; its target and meaning are unproven.");
        }
        var start = string.Join(", ", v.StartGame.Select(e => $"{e.Key}={e.Value}"));
        notes.Add($"CFG [StartGame] ({start}) and [Owner] Wipe/Profile are raw switches; their native effect on retained progression is not validated.");
        notes.Add($"NWS {NwsSpecLosesMinerals}/{NwsSpecLosesNanobots} (under '{v.SpecModeLosses[0].Heading.Text}') name losses on spec mode; which items survive logout under the original rules is not decided here.");
        return notes;
    }
}

/// <summary>
/// The original ids a persisted profile may name, for binding the data-agnostic persistence core to the canonical catalogs (RT-F068).
/// <para>
/// The string encoding is a RECONSTRUCTED (NEW) contract, not historical: the historical save schema is not in the package. Each id is
/// the invariant decimal text of an original numeric id, with no sign, padding or whitespace (<see cref="Encode"/>). Hull ids are the
/// VEH vehicle id of one of the twelve RPG/VEH-linked hulls (RPG f[3] = VEH f[2]; Asp = <c>2002</c>). Item ids are any ITM/Rogue_mp.itm
/// id f[2] (all 200 rows). Upgrade ids are the ITM ids of the RT-F048 families (<see cref="OriginalUpgrade.Families"/>). Hull and item
/// ids are separate namespaces (for example 2011 is the Excelsior hull in VEH and the HyperCore item in ITM).
/// </para>
/// No quantity limit, held-category limit, price, cash value or race rule is applied: those stay with their own features.
/// </summary>
public sealed class OriginalProgressionIds
{
    private OriginalProgressionIds(IReadOnlySet<string> hulls, IReadOnlySet<string> items, IReadOnlySet<string> upgrades)
    {
        HullIds = hulls;
        ItemIds = items;
        UpgradeIds = upgrades;
    }

    public IReadOnlySet<string> HullIds { get; }
    public IReadOnlySet<string> ItemIds { get; }
    public IReadOnlySet<string> UpgradeIds { get; }

    /// <summary>The single accepted text form of an original numeric id (invariant decimal).</summary>
    public static string Encode(int originalId) => originalId.ToString(CultureInfo.InvariantCulture);

    public static string HullId(OriginalHull hull) => Encode(hull.VehicleId);
    public static string ItemId(OriginalItem item) => Encode(item.Id);

    public bool IsHull(string? id) => id is not null && HullIds.Contains(id);
    public bool IsItem(string? id) => id is not null && ItemIds.Contains(id);
    public bool IsUpgrade(string? id) => id is not null && UpgradeIds.Contains(id);

    public static OriginalProgressionIds Load(string assetsRoot) =>
        FromCatalogs(OriginalHullCatalog.Load(assetsRoot), OriginalItemCatalog.Load(assetsRoot));

    public static OriginalProgressionIds FromCatalogs(OriginalHullCatalog hulls, OriginalItemCatalog items) => new(
        hulls.Hulls.Select(HullId).ToHashSet(StringComparer.Ordinal),
        items.Items.Select(ItemId).ToHashSet(StringComparer.Ordinal),
        OriginalUpgrade.All(items).Select(u => ItemId(u.Item)).ToHashSet(StringComparer.Ordinal));
}
