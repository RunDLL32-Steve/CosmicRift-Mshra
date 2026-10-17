using System.Text;
using System.Text.RegularExpressions;

namespace RogueTrader.OriginalData;

/// <summary>A CFG entry together with the section it came from (values stay raw text; nothing is scaled or defaulted).</summary>
public sealed record OriginalCfgKey(string Section, CfgEntry Entry)
{
    /// <summary>The value as a plain integer, or null when it is empty / not a plain integer (the raw text stays in <see cref="CfgEntry.Value"/>).</summary>
    public int? AsInt => int.TryParse(Entry.Value, System.Globalization.NumberStyles.AllowLeadingSign,
        System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
}

/// <summary>
/// The safety, damage, energy, prize, reward, spec and quit/arena-change keys of one rogue.cfg <c>[TerrainN]</c> block, all raw (RT-F065,
/// data clause only). Meaning of the numbers (units, how the server applies them, whether a key is enforced by the original executable)
/// is NOT decided here. <see cref="OtherKeys"/> lists the block's remaining keys by name so that nothing in the block is unaccounted for.
/// </summary>
public sealed class OriginalTerrainSafety
{
    /// <summary>The keys of a terrain block this view exposes, in file order (identical across all 16 blocks).</summary>
    public static readonly IReadOnlyList<string> SelectedKeys =
    [
        "Message", "Safety", "StoreEnabled", "SkillEnabled", "TrickleKill", "TeamChangeEnabled",
        "EnergyRate", "HealthRate", "RepairRate",
        "PrizeEnableMode", "PrizeExpire", "PrizeBountyMultiplier",
        "RepelVehicle", "RepelWeapons",
        "PercentOfCashReward", "PercentOfExperienceReward", "PercentOfPointsReward",
        "MaxTimeAllowed", "BountyAutoMax", "BountyAutoRate", "DeleteLiveWeapons", "EventString",
        "AllowChangeArena", "AllowGoToSpec", "QuitDelaySecs", "GoToSpecDelaySecs", "ChangeArenaDelaySecs", "AllowQuitting",
    ];

    internal OriginalTerrainSafety(int terrain, CfgSection section)
    {
        Terrain = terrain;
        Line = section.LineNumber;
        var map = new Dictionary<string, OriginalCfgKey>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in SelectedKeys)
        {
            var e = section.Find(k) ?? throw new OriginalDataException($"CFG [{section.Name}] has no key '{k}'.");
            map[k] = new OriginalCfgKey(section.Name, e);
        }
        Keys = map;
        OtherKeys = section.Entries.Where(e => !map.ContainsKey(e.Key)).Select(e => e.Key).ToList();
        EntryCount = section.Entries.Count;
    }

    /// <summary>All 16 terrain blocks in order (<see cref="OriginalTerrainAccess.TerrainCount"/>), reusing the raw <see cref="OriginalCfg"/> reader.</summary>
    public static IReadOnlyList<OriginalTerrainSafety> All(OriginalCfg cfg) =>
        Enumerable.Range(0, OriginalTerrainAccess.TerrainCount).Select(t => new OriginalTerrainSafety(t, cfg.Section($"Terrain{t}"))).ToList();

    public int Terrain { get; }
    /// <summary>1-based line of the <c>[TerrainN]</c> header.</summary>
    public int Line { get; }
    /// <summary>Number of key=value lines in the block (35 in all 16 blocks).</summary>
    public int EntryCount { get; }
    public IReadOnlyDictionary<string, OriginalCfgKey> Keys { get; }
    /// <summary>Block keys not in <see cref="SelectedKeys"/> (cosmetic or unrelated: StripShadows, SoccerEnabled, GoalFrequency, FontColor, Starfield, GoalPoints, FlagTimerSpeed).</summary>
    public IReadOnlyList<string> OtherKeys { get; }

    public OriginalCfgKey Key(string key) => Keys.TryGetValue(key, out var k) ? k : throw new KeyNotFoundException($"Terrain key '{key}' is not in this view.");
    public string Raw(string key) => Key(key).Entry.Value;
    /// <summary>Integer value of <paramref name="key"/>; throws when the raw value is empty or not a plain integer (BountyAutoMax is empty on Terrain9 and Terrain15).</summary>
    public int Int(string key) => Key(key).AsInt
        ?? throw new OriginalDataException($"CFG [Terrain{Terrain}] line {Key(key).Entry.LineNumber}: '{key}={Raw(key)}' is not a plain integer.");

    public string Message => Raw("Message");
    public int Safety => Int("Safety");
    public int EnergyRate => Int("EnergyRate");
    public int MaxTimeAllowed => Int("MaxTimeAllowed");
}

/// <summary>An NWS line with its dated heading, both verbatim (heading found by walking up to the nearest <c>********M/D/YY</c> line).</summary>
public sealed record OriginalSafeZoneNwsLine(OriginalNwsLine Heading, OriginalNwsLine Line);

/// <summary>A line of LIO/Rogue.lio seen as raw text only. No field is split or interpreted (the LIO reader is queue G6).</summary>
public sealed record OriginalLioTextSighting(int Line, string RawText);

/// <summary>
/// Data view of RT-F065 "Safe zones and docking boundaries" (data clause only): the safety/energy/prize/spec/quit keys of all 16 rogue.cfg
/// terrain blocks (reusing <see cref="OriginalCfg"/>), the arena-level keys tied to safe zones, spectating, team switching, docking and
/// doors, the pinned NWS lines 290, 344, 345, 419, 481 and 483 with dated headings, and raw-text sightings of the LIO docking rows. Nothing
/// here implements or validates safe-zone, docking, door or forced-spec behaviour; mismatches between the news text and the CFG are kept
/// visible in <see cref="VisibleMismatches"/>. Key names are the file's own text; any meaning attached to them by the InfantryOnline
/// emulator is an upstream-parser name, not original-executable evidence.
/// </summary>
public sealed class OriginalSafeZones
{
    /// <summary>fowlmouth/cosmic-rift-assets LIO/Rogue.lio at <see cref="PinnedAssets.Commit"/> (ASSET_MANIFEST.json originalSha256).</summary>
    public static readonly PinnedAsset LioRogue = new("LIO/Rogue.lio", 9782, "73d008c5168efa46f73eaccdd78c461a121d6ae281b6d2a5e95a73e43bec2f6a");

    public static readonly int[] NwsLineNumbers = [290, 344, 345, 419, 481, 483];
    public const string DockingSoundMarker = ",SFAmbient,docking_proceedure,";
    /// <summary>The text the catalog row quotes for the NWS 483 rule ("48360-second"); it does not occur in the file.</summary>
    public const string CatalogQuotedText = "48360-second";

    private static readonly Regex HeadingPattern = new(@"^\*+\s*(\d{1,2}/\d{1,2}/\d{2})\s*$", RegexOptions.CultureInvariant);

    private static readonly (string Section, string Key)[] ArenaKeyList =
    [
        ("Arena", "SpectatorVehicleId"), ("Arena", "SpectatorStore"), ("Arena", "SpectatorSkills"), ("Arena", "StartInSpectator"),
        ("Arena", "ExitSpectatorLogic"), ("Arena", "ExitSpectatorMessage"), ("Arena", "SpectatorVisualFrequency"), ("Arena", "SpectatorEnergyAmount"),
        ("Arena", "ExitSpectatorToggleSkills"), ("Arena", "SkillScreenEnergyNeeded"), ("Arena", "AllowManualTeamSwitch"),
        ("Arena", "TeamSwitchMinEnergy"), ("Arena", "InactivityTimeout"), ("Arena", "SuicidesAllowed"),
        ("Door", "AttemptTime"), ("Door", "RandomControlDelay"),
        ("Flag", "AllowSafety"),
        ("Soul", "EnergyDefaultMax"), ("Soul", "EnergyDefaultRate"), ("Soul", "EnergyDefaultStart"), ("Soul", "EnergyShieldMode"),
        ("Event", "Hold1"), ("Event", "Hold2"), ("Event", "JoinTeam"), ("Event", "ExitSpectatorMode"), ("Event", "ManualJoinTeam"),
    ];

    private OriginalSafeZones(IReadOnlyList<OriginalTerrainSafety> terrains, IReadOnlyList<OriginalCfgKey> arenaKeys,
        IReadOnlyList<OriginalSafeZoneNwsLine> nws, bool nwsTextHas48360, IReadOnlyList<OriginalLioTextSighting> docking,
        IReadOnlyList<OriginalLioTextSighting> doorText, int lioLineCount)
    {
        Terrains = terrains;
        ArenaKeys = arenaKeys;
        Nws = nws;
        NwsTextContains48360 = nwsTextHas48360;
        DockingLioLines = docking;
        LioDoorTextLines = doorText;
        LioLineCount = lioLineCount;
        VisibleMismatches = DescribeMismatches(terrains, arenaKeys, nws, nwsTextHas48360, docking, doorText);
    }

    /// <summary>[Terrain0]..[Terrain15] in order.</summary>
    public IReadOnlyList<OriginalTerrainSafety> Terrains { get; }
    /// <summary>Arena-level keys in the order of the internal key list, each with its section and file line.</summary>
    public IReadOnlyList<OriginalCfgKey> ArenaKeys { get; }
    /// <summary>NWS/Rogue.nws lines 290, 344, 345, 419, 481, 483 with their dated headings, verbatim.</summary>
    public IReadOnlyList<OriginalSafeZoneNwsLine> Nws { get; }
    /// <summary>Whether the string "48360" occurs anywhere in NWS/Rogue.nws (it does not).</summary>
    public bool NwsTextContains48360 { get; }
    /// <summary>LIO/Rogue.lio lines containing <see cref="DockingSoundMarker"/>, raw text only (13 lines).</summary>
    public IReadOnlyList<OriginalLioTextSighting> DockingLioLines { get; }
    /// <summary>LIO/Rogue.lio lines containing "door" in any case, raw text only (none in the pinned file).</summary>
    public IReadOnlyList<OriginalLioTextSighting> LioDoorTextLines { get; }
    public int LioLineCount { get; }
    /// <summary>Notes where the news text, the catalog wording and the CFG values do not line up. Informational; no value is changed.</summary>
    public IReadOnlyList<string> VisibleMismatches { get; }

    public OriginalTerrainSafety Terrain(int n) => n >= 0 && n < Terrains.Count ? Terrains[n] : throw new ArgumentOutOfRangeException(nameof(n));

    public OriginalCfgKey ArenaKey(string section, string key) =>
        ArenaKeys.FirstOrDefault(k => k.Section == section && string.Equals(k.Entry.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Arena-level key [{section}] {key} is not in this view.");

    public OriginalSafeZoneNwsLine NwsLine(int lineNumber) =>
        Nws.FirstOrDefault(n => n.Line.LineNumber == lineNumber) ?? throw new KeyNotFoundException($"NWS line {lineNumber} is not part of this view.");

    public static OriginalSafeZones Load(string assetsRoot)
    {
        var cfg = OriginalCfg.LoadRogue(assetsRoot);
        var nws = OriginalNws.LoadRogue(assetsRoot);
        var lio = Encoding.Latin1.GetString(AssetIntegrity.ReadVerified(assetsRoot, LioRogue));
        return Resolve(cfg, nws, lio);
    }

    public static OriginalSafeZones Resolve(OriginalCfg cfg, OriginalNws nws, string lioText)
    {
        var terrains = OriginalTerrainSafety.All(cfg);

        var arena = new List<OriginalCfgKey>();
        foreach (var (section, key) in ArenaKeyList)
        {
            var e = cfg.Section(section).Find(key) ?? throw new OriginalDataException($"CFG [{section}] has no key '{key}'.");
            arena.Add(new OriginalCfgKey(section, e));
        }

        var lines = new List<OriginalSafeZoneNwsLine>();
        foreach (var (number, needle) in new[]
        {
            (290, "change teams from a safe zone"),
            (344, "doors at"),
            (345, "not currently functional"),
            (419, "lose energy when you sit in a safe zone"),
            (481, "change teams from the Space Dock area"),
            (483, "safe zones protecting Refineries or Trading Posts for 60 seconds"),
        })
        {
            var line = nws.Range(number, number)[0];
            if (!line.Text.Contains(needle, StringComparison.Ordinal))
                throw new OriginalDataException($"NWS line {number} does not contain '{needle}': '{line.Text}'.");
            lines.Add(new OriginalSafeZoneNwsLine(FindHeading(nws, number), line));
        }
        var has48360 = nws.Lines.Any(l => l.Text.Contains("48360", StringComparison.Ordinal));

        var lioLines = lioText.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        if (lioLines.Count > 0 && lioLines[^1].Length == 0) lioLines.RemoveAt(lioLines.Count - 1);
        var docking = new List<OriginalLioTextSighting>();
        var doors = new List<OriginalLioTextSighting>();
        for (var i = 0; i < lioLines.Count; i++)
        {
            if (lioLines[i].Contains(DockingSoundMarker, StringComparison.Ordinal)) docking.Add(new OriginalLioTextSighting(i + 1, lioLines[i]));
            if (lioLines[i].Contains("door", StringComparison.OrdinalIgnoreCase)) doors.Add(new OriginalLioTextSighting(i + 1, lioLines[i]));
        }
        return new OriginalSafeZones(terrains, arena, lines, has48360, docking, doors, lioLines.Count);
    }

    private static OriginalNwsLine FindHeading(OriginalNws nws, int lineNumber)
    {
        for (var n = lineNumber; n >= 1; n--)
        {
            var l = nws.Lines[n - 1];
            if (HeadingPattern.IsMatch(l.Text)) return l;
        }
        throw new OriginalDataException($"NWS line {lineNumber} has no dated heading above it.");
    }

    private static string List(IEnumerable<int> terrains) => string.Join(",", terrains);

    private static IReadOnlyList<string> DescribeMismatches(IReadOnlyList<OriginalTerrainSafety> terrains, IReadOnlyList<OriginalCfgKey> arena,
        IReadOnlyList<OriginalSafeZoneNwsLine> nws, bool has48360, IReadOnlyList<OriginalLioTextSighting> docking, IReadOnlyList<OriginalLioTextSighting> doorText)
    {
        var notes = new List<string>();
        var safe = terrains.Where(t => t.Safety != 0).ToList();
        var loss = terrains.Where(t => t.EnergyRate != 0).ToList();
        var timed = terrains.Where(t => t.MaxTimeAllowed != 0).ToList();
        var noTeam = terrains.Where(t => t.Int("TeamChangeEnabled") == 0).ToList();
        var teamOk = terrains.Where(t => t.Int("TeamChangeEnabled") != 0).ToList();
        string Desc(OriginalTerrainSafety t) => $"Terrain{t.Terrain} '{t.Message}'";
        OriginalSafeZoneNwsLine N(int n) => nws.Single(x => x.Line.LineNumber == n);
        string Head(int n) => N(n).Heading.Text;

        if (!has48360)
            notes.Add($"The catalog row quotes '{CatalogQuotedText}', which does not occur anywhere in NWS/Rogue.nws; NWS line 483 ({Head(483)}) reads '60 seconds ... sent to spec', so the catalog text is not a quotation; it appears to run 'NWS 483' together with a '60-second' rule (an inference, not evidence).");

        notes.Add($"NWS 483 ({Head(483)}): 60 seconds in the safe zones protecting Refineries or Trading Posts, then sent to spec. CFG MaxTimeAllowed is non-zero ({string.Join(", ", timed.Select(t => t.MaxTimeAllowed))}) only on {string.Join(" and ", timed.Select(Desc))}; "
                  + $"Terrain{string.Join(",", safe.Select(t => t.Terrain))} are Safety=1 but {string.Join("; ", safe.Where(t => t.MaxTimeAllowed == 0).Select(Desc))} has MaxTimeAllowed=0. No terrain message names a Trading Post, and no CFG key names a forced move to spec (AllowGoToSpec={terrains[0].Raw("AllowGoToSpec")} and GoToSpecDelaySecs={terrains[0].Raw("GoToSpecDelaySecs")} are the same on all 16 terrains), so the link between MaxTimeAllowed=60 and 'sent to spec' is only a numeric match until the executable is read.");

        notes.Add($"NWS 419 ({Head(419)}): lose energy in a safe zone surrounding a refinery/trading post. CFG EnergyRate is non-zero ({string.Join(", ", loss.Select(t => t.EnergyRate).Distinct())}) on Terrain{List(loss.Select(t => t.Terrain))} ({string.Join("; ", loss.Select(Desc))}), the same terrains that have Safety=1 (Terrain{List(safe.Select(t => t.Terrain))}), so the loss is on the Repair Dock and SpaceDock zones as well, not only the refinery/trade zone. The sign fits 'lose' but the unit of -2000 is undecoded (context: [Soul] EnergyDefaultMax={arena.First(k => k.Entry.Key == "EnergyDefaultMax").Entry.Value}, EnergyDefaultRate={arena.First(k => k.Entry.Key == "EnergyDefaultRate").Entry.Value}).");

        var minEnergy = arena.First(k => k.Entry.Key == "TeamSwitchMinEnergy");
        notes.Add($"NWS 290 ({Head(290)}): change teams from a safe zone with at least 500 units of energy. CFG [Arena] TeamSwitchMinEnergy={minEnergy.Entry.Value} (line {minEnergy.Entry.LineNumber}) is 1000 times the text, and TeamChangeEnabled=0 on Terrain{List(noTeam.Select(t => t.Terrain))}, which includes the Safety=1 terrains {List(safe.Where(t => t.Int("TeamChangeEnabled") == 0).Select(t => t.Terrain))}; only {string.Join("; ", safe.Where(t => t.Int("TeamChangeEnabled") != 0).Select(Desc))} is both a safe zone and team-change enabled. The scale (500 vs {minEnergy.Entry.Value}) is not proven.");

        notes.Add($"NWS 481 ({Head(481)}): teams can only be changed from the Space Dock area. CFG TeamChangeEnabled=1 on {teamOk.Count} terrains (Terrain{List(teamOk.Select(t => t.Terrain))}), only one of which is the message-labelled '{teamOk.First(t => t.Message.Contains("SpaceDock", StringComparison.Ordinal)).Message}'; whether the other terrains are Space Dock area is a map (LVL) question, and LVL is not available as text.");

        notes.Add($"NWS 345 ({Head(345)}, with 344) says the doors at the spacedock entrances 'are not currently functional'; the catalog row words it 'not yet functional'. CFG has a [Door] block (AttemptTime={arena.First(k => k.Entry.Key == "AttemptTime").Entry.Value}, RandomControlDelay={arena.First(k => k.Entry.Key == "RandomControlDelay").Entry.Value}) but LIO/Rogue.lio contains {doorText.Count} lines with 'door' in any case, so the spacedock doors are not in the LIO text; any door placement would be in the LVL map (Git LFS, not available). The 'current door state' the acceptance asks for is therefore not decodable here.");

        notes.Add($"The catalog names LIO docking rows 12, 40-48, 60-62 ({docking.Count} rows); raw-text sightings of '{DockingSoundMarker}' are on lines {string.Join(",", docking.Select(d => d.Line))}, which matches exactly. Those rows are not read: no coordinates, sizes, ids or boundaries are decoded, so no docking boundary exists in this view.");

        notes.Add("No CFG, NWS or LIO text defines a safe-zone entry/exit trigger geometry; terrain permissions are per-terrain-id flags and the terrain id of each map cell comes from LVL/BLO data that is not available. Zone triggers/timing beyond MaxTimeAllowed/EnergyRate and original forced-spec behaviour remain undecoded.");
        return notes;
    }
}
