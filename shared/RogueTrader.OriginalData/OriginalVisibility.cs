using System.Text.RegularExpressions;

namespace RogueTrader.OriginalData;

/// <summary>
/// One <c>[LosTypeN]</c> block of CFG/rogue.cfg (N = 0..7), raw. <see cref="SeeType"/> holds the eight <c>SeeType0..7</c> values,
/// <see cref="VisibleDistance"/> and <see cref="Solid"/> the other two keys. What a "LOS type" is, which map or vehicle element uses
/// each index, and the units of VisibleDistance (-1, 0, 4, 12, 16 occur) are NOT established.
/// </summary>
public sealed record OriginalLosType(int Index, int HeaderLine, IReadOnlyList<CfgEntry> SeeType, CfgEntry VisibleDistance, CfgEntry Solid, IReadOnlyList<CfgEntry> Entries)
{
    public const int Count = 8;
    public const int SeeTypeCount = 8;

    public static OriginalLosType Read(OriginalCfg cfg, int index)
    {
        var s = cfg.Section($"LosType{index}");
        CfgEntry Need(string k) => s.Find(k) ?? throw new OriginalDataException($"CFG [{s.Name}] has no key '{k}'.");
        var see = Enumerable.Range(0, SeeTypeCount).Select(i => Need($"SeeType{i}")).ToArray();
        return new OriginalLosType(index, s.LineNumber, see, Need("VisibleDistance"), Need("Solid"), s.Entries);
    }

    /// <summary>The eight SeeType values as plain integers (throws if one is not an integer).</summary>
    public IReadOnlyList<int> SeeTypeValues => SeeType.Select(SectionInt).ToArray();

    public int VisibleDistanceValue => SectionInt(VisibleDistance);
    public int SolidValue => SectionInt(Solid);

    private static int SectionInt(CfgEntry e) =>
        int.TryParse(e.Value, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v : throw new OriginalDataException($"CFG line {e.LineNumber}: '{e.Key}={e.Value}' is not a plain integer.");
}

/// <summary>One <c>[Radar]</c> color key: the raw text, the line, and the number parsed by <see cref="OriginalTeamInfo.ParseColor"/> (0x-hex as written; channel order is not established).</summary>
public sealed record OriginalRadarColor(CfgEntry Entry, int Value);

/// <summary>
/// The raw vision, line-of-sight and radar keys of CFG/rogue.cfg for RT-F064 (data only). Everything is a <see cref="CfgEntry"/> with its
/// line number and unmodified value text. The set of keys was enumerated by scanning every section of the pinned file for key names
/// containing Los/Vision/Radar/Xray/Fog/Sensor/Sight/Visib/TeamShar and reviewing the hits; unrelated hits (Loser*, Loss*, Lost*, Invisible*,
/// ComputerExplosionDamageComputer, ...) are excluded. Nothing is interpreted: units of distance, the angle scale, what Xray 0 means, the
/// meaning of the SeeEnergy flags, the use of the LosType blocks and the channel order of the radar colors are NOT established here.
/// </summary>
public sealed class OriginalVisibilityConfig
{
    private OriginalVisibilityConfig(OriginalCfg cfg)
    {
        Cfg = cfg;
        var los = cfg.Section("LOS");
        LosSection = los;
        CfgEntry Need(CfgSection s, string k) => s.Find(k) ?? throw new OriginalDataException($"CFG [{s.Name}] has no key '{k}'.");
        DefaultDistance = Need(los, "DefaultDistance");
        DefaultAngle = Need(los, "DefaultAngle");
        DefaultXray = Need(los, "DefaultXray");
        TeamSharing = Need(los, "TeamSharing");

        var arena = cfg.Section("Arena");
        TeamVisionDistance = Need(arena, "TeamVisionDistance");
        SpecsSeeEnergy = Need(arena, "SpecsSeeEnergy");
        FriendlySeeEnergy = Need(arena, "FriendlySeeEnergy");
        EnemySeeEnergy = Need(arena, "EnemySeeEnergy");
        LevelRadarPrizeDistance = Need(cfg.Section("Level"), "RadarPrizeDistance");
        var flag = cfg.Section("Flag");
        FlagCarrierOnRadar = Need(flag, "CarrierOnRadar");
        FlagCarrierLineOfSight = Need(flag, "CarrierLineOfSight");
        SoccerShowRadar = Need(cfg.Section("Soccer"), "ShowRadar");
        var ui = cfg.Section("Uiart");
        UiartVision = Need(ui, "Vision");
        UiartWarFog = Need(ui, "WarFog");
        UiartRadarBorder = Need(ui, "RadarBorder");

        RadarSection = cfg.Section("Radar");
        RadarColors = RadarSection.Entries.Where(e => e.Key.StartsWith("Color", StringComparison.Ordinal))
            .Select(e => new OriginalRadarColor(e, OriginalTeamInfo.ParseColor(e.Value))).ToArray();
        LosTypes = Enumerable.Range(0, OriginalLosType.Count).Select(i => OriginalLosType.Read(cfg, i)).ToArray();
    }

    public OriginalCfg Cfg { get; }

    /// <summary>The <c>[LOS]</c> section: header line 118, entries on lines 119..122.</summary>
    public CfgSection LosSection { get; }
    /// <summary>[LOS] DefaultDistance (line 119).</summary>
    public CfgEntry DefaultDistance { get; }
    /// <summary>[LOS] DefaultAngle (line 120).</summary>
    public CfgEntry DefaultAngle { get; }
    /// <summary>[LOS] DefaultXray (line 121).</summary>
    public CfgEntry DefaultXray { get; }
    /// <summary>[LOS] TeamSharing (line 122).</summary>
    public CfgEntry TeamSharing { get; }

    /// <summary>[Arena] TeamVisionDistance (line 32).</summary>
    public CfgEntry TeamVisionDistance { get; }
    /// <summary>[Arena] SpecsSeeEnergy (line 60), FriendlySeeEnergy (61), EnemySeeEnergy (62); the names suggest energy visibility but that is not established.</summary>
    public CfgEntry SpecsSeeEnergy { get; }
    public CfgEntry FriendlySeeEnergy { get; }
    public CfgEntry EnemySeeEnergy { get; }
    /// <summary>[Level] RadarPrizeDistance (line 23).</summary>
    public CfgEntry LevelRadarPrizeDistance { get; }
    /// <summary>[Flag] CarrierOnRadar (line 866) and CarrierLineOfSight (867).</summary>
    public CfgEntry FlagCarrierOnRadar { get; }
    public CfgEntry FlagCarrierLineOfSight { get; }
    /// <summary>[Soccer] ShowRadar (line 2099).</summary>
    public CfgEntry SoccerShowRadar { get; }
    /// <summary>[Uiart] Vision (2739), WarFog (2742) and RadarBorder (2751): "blob,name" art references, raw text.</summary>
    public CfgEntry UiartVision { get; }
    public CfgEntry UiartWarFog { get; }
    public CfgEntry UiartRadarBorder { get; }

    /// <summary>The <c>[Radar]</c> section: header line 2276, entries on lines 2277..2284 (eight keys in the pinned file).</summary>
    public CfgSection RadarSection { get; }
    /// <summary>The five <c>Color*</c> keys of [Radar] in file order: ColorSoccerBall, ColorSelf, ColorFriendly, ColorEnemy, ColorFlag.</summary>
    public IReadOnlyList<OriginalRadarColor> RadarColors { get; }
    public OriginalRadarColor RadarColor(string key) =>
        RadarColors.FirstOrDefault(c => string.Equals(c.Entry.Key, key, StringComparison.Ordinal)) ?? throw new KeyNotFoundException($"CFG [Radar] has no color key '{key}'.");
    public CfgEntry RadarEntry(string key) => RadarSection.Find(key) ?? throw new KeyNotFoundException($"CFG [Radar] has no key '{key}'.");

    /// <summary>[LosType0]..[LosType7] in file order (header lines 2767, 2779, 2791, ...).</summary>
    public IReadOnlyList<OriginalLosType> LosTypes { get; }

    public static OriginalVisibilityConfig Of(OriginalCfg cfg) => new(cfg);
}

/// <summary>
/// The vision/radar-related fields of one VEH row, raw. Field indexes f[65] and f[132]..f[141] are the shared vehicle base block of the
/// upstream-emulator parser (InfantryOnline bb97579e VehInfo.cs; names are upstream-parser names, NOT original-executable evidence). The
/// base block is shared by the type-5 Computer map (<see cref="VehComputerFields"/>) and, by the same indexes the repo already uses for
/// Car rows (Hitpoints 6, Weight 7, NormalWeight 47, DropItemId 68, EnergyMax 94 ...), the type-2 hull rows; the type-4 Spectator row
/// has 202 fields so f[132]..f[141] exist there too. No value is scaled or interpreted; in particular whether 0 or -1 means "use the CFG default"
/// is NOT established.
/// </summary>
public sealed record OriginalVehicleVisibilityFields(
    int RowNumber, int Id, string Name, int RecordType, int FieldCount,
    OriginalInt RadarPrize, OriginalInt LosDistance, OriginalInt LosAngle, OriginalInt LosXRay, OriginalInt CombatAwarenessTime,
    OriginalInt DisplayOnFriendlyRadar, OriginalInt FriendlyRadarColor, OriginalInt DisplayOnEnemyRadar, OriginalInt EnemyRadarColor)
{
    public static OriginalVehicleVisibilityFields Of(OriginalVehicleRecord v) => new(
        v.RowNumber, v.Id, v.Name, v.RecordType, v.FieldCount,
        v.Int(VehComputerFields.RadarPrize), v.Int(VehComputerFields.LosDistance), v.Int(VehComputerFields.LosAngle), v.Int(VehComputerFields.LosXRay),
        v.Int(VehComputerFields.CombatAwarenessTime),
        v.Int(VehComputerFields.DisplayOnFriendlyRadar), v.Int(VehComputerFields.FriendlyRadarColor),
        v.Int(VehComputerFields.DisplayOnEnemyRadar), v.Int(VehComputerFields.EnemyRadarColor));

    /// <summary>f[132], f[133], f[134] as one tuple.</summary>
    public (int Distance, int Angle, int XRay) Los => (LosDistance.Value, LosAngle.Value, LosXRay.Value);

    /// <summary>f[138]..f[141] as one tuple (DisplayOnFriendlyRadar, FriendlyRadarColor, DisplayOnEnemyRadar, EnemyRadarColor).</summary>
    public (int Friendly, int FriendlyColor, int Enemy, int EnemyColor) Radar =>
        (DisplayOnFriendlyRadar.Value, FriendlyRadarColor.Value, DisplayOnEnemyRadar.Value, EnemyRadarColor.Value);
}

/// <summary>
/// Data view of RT-F064 "LOS/team radar" (data clause only): the raw CFG vision/LOS/radar keys, the vision-related fields of every VEH row
/// (the twelve hull Car rows, the seventeen Computer rows and Spectator Mode), and the pinned NWS lines with their dated headings. It
/// does not implement or verify line-of-sight, angle or distance rules, team vision sharing, X-ray, fog, sensors, radar blips or rendering;
/// the box stays open. See <see cref="VisibleMismatches"/> for where the pinned data and the catalog wording disagree.
/// </summary>
public sealed class OriginalVisibility
{
    /// <summary>The NWS line the catalog calls "261"; it is blank in the pinned file (the text sits on <see cref="NwsTeamVisionLine"/>).</summary>
    public const int CatalogNwsTeamVisionLine = 261;
    public const int NwsTeamVisionLine = 258;
    public const int NwsBaseLosLine = 325;
    /// <summary>NWS lines 305..315 name the stealth, impeller, sensor and combat-sensor items added under the same 2/24/03 heading as line 325.</summary>
    public const int NwsItemFirstLine = 305;
    public const int NwsItemLastLine = 315;

    private static readonly Regex HeadingPattern = new(@"^\*+\s*(\d{1,2}/\d{1,2}/\d{2})\s*$", RegexOptions.CultureInvariant);

    private OriginalVisibility(OriginalVisibilityConfig config, IReadOnlyList<OriginalVehicleVisibilityFields> vehicles,
        OriginalDatedNwsLine teamVision, OriginalDatedNwsLine baseLos, IReadOnlyList<OriginalDatedNwsLine> itemLines, IReadOnlyList<OriginalItem> sensorLikeItems)
    {
        Config = config;
        Vehicles = vehicles;
        NwsTeamVision = teamVision;
        NwsBaseLos = baseLos;
        NwsItemLines = itemLines;
        ItemsNamedLikeNwsSensorOrStealth = sensorLikeItems;
        VisibleMismatches = DescribeMismatches();
    }

    public OriginalVisibilityConfig Config { get; }
    /// <summary>One entry per VEH row in file order (30 rows).</summary>
    public IReadOnlyList<OriginalVehicleVisibilityFields> Vehicles { get; }
    public IEnumerable<OriginalVehicleVisibilityFields> Hulls => Vehicles.Where(v => v.RecordType == OriginalVehicleCatalog.CarRecordType);
    public IEnumerable<OriginalVehicleVisibilityFields> Computers => Vehicles.Where(v => v.RecordType == OriginalVehicleCatalog.ComputerRecordType);
    public OriginalVehicleVisibilityFields Vehicle(int id) =>
        Vehicles.FirstOrDefault(v => v.Id == id) ?? throw new KeyNotFoundException($"No original VEH row with id {id}.");

    /// <summary>NWS line 258 "Re-Enabled Team vision sharing." with its heading (2/27/03).</summary>
    public OriginalDatedNwsLine NwsTeamVision { get; }
    /// <summary>NWS line 325 "Base LOS has been narrowed to 225 degrees. ..." with its heading (2/24/03).</summary>
    public OriginalDatedNwsLine NwsBaseLos { get; }
    /// <summary>NWS lines 305..315 (stealth, impeller, sensor items) with the shared heading, verbatim; context for "sensors" only.</summary>
    public IReadOnlyList<OriginalDatedNwsLine> NwsItemLines { get; }
    /// <summary>ITM rows whose name contains Sensor, Stealth or Distortion (case-insensitive); empty in the pinned file, so the NWS 305..315 items are not present in ITM/Rogue_mp.itm under those names.</summary>
    public IReadOnlyList<OriginalItem> ItemsNamedLikeNwsSensorOrStealth { get; }
    public IReadOnlyList<string> VisibleMismatches { get; }

    public static OriginalVisibility Load(string assetsRoot) =>
        Resolve(OriginalCfg.LoadRogue(assetsRoot), OriginalVehicleCatalog.Load(assetsRoot), OriginalItemCatalog.Load(assetsRoot), OriginalNws.LoadRogue(assetsRoot));

    public static OriginalVisibility Resolve(OriginalCfg cfg, OriginalVehicleCatalog vehicles, OriginalItemCatalog items, OriginalNws nws)
    {
        var config = OriginalVisibilityConfig.Of(cfg);
        var veh = vehicles.All.Select(OriginalVehicleVisibilityFields.Of).ToArray();

        OriginalDatedNwsLine Pin(int number, string needle)
        {
            var line = nws.Range(number, number)[0];
            if (!line.Text.Contains(needle, StringComparison.Ordinal))
                throw new OriginalDataException($"NWS line {number} does not contain '{needle}': '{line.Text}'.");
            return new OriginalDatedNwsLine(FindHeading(nws, number), line);
        }
        var teamVision = Pin(NwsTeamVisionLine, "Re-Enabled Team vision sharing");
        var baseLos = Pin(NwsBaseLosLine, "Base LOS has been narrowed to 225 degrees");
        var heading = FindHeading(nws, NwsItemFirstLine);
        var itemLines = nws.Range(NwsItemFirstLine, NwsItemLastLine).Select(l => new OriginalDatedNwsLine(heading, l)).ToArray();
        var sensorLike = items.Items.Where(i => new[] { "sensor", "stealth", "distortion" }.Any(w => i.Name.Contains(w, StringComparison.OrdinalIgnoreCase))).ToArray();
        return new OriginalVisibility(config, veh, teamVision, baseLos, itemLines, sensorLike);
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

    private IReadOnlyList<string> DescribeMismatches()
    {
        var notes = new List<string>();
        notes.Add($"The catalog cites NWS line {CatalogNwsTeamVisionLine} for team vision; that line is blank in the pinned file. The text 'Re-Enabled Team vision sharing.' is on line {NwsTeamVision.Line.LineNumber} under heading '{NwsTeamVision.Heading.Text}' (line {NwsTeamVision.Heading.LineNumber}).");
        notes.Add($"NWS line {NwsBaseLos.Line.LineNumber} ({NwsBaseLos.Heading.Text}) says base LOS was narrowed to 225 degrees; CFG [LOS] DefaultAngle (line {Config.DefaultAngle.LineNumber}) is {Config.DefaultAngle.Value}. The angle unit/scale and any relation between 200 and 225 are unproven; both are kept raw.");
        var hullLos = Hulls.Select(h => h.Los).Distinct().ToList();
        if (hullLos.Count == 1)
            notes.Add($"All {Hulls.Count()} hull Car rows carry the identical LOS triple f[132..134] = ({hullLos[0].Distance}, {hullLos[0].Angle}, {hullLos[0].XRay}); no hull has a distinct per-hull LOS override in VEH. Whether 0 means 'use the [LOS] default' is not established.");
        if (ItemsNamedLikeNwsSensorOrStealth.Count == 0)
            notes.Add($"NWS lines {NwsItemFirstLine}..{NwsItemLastLine} announce stealth, distortion, impeller and sensor items, but no ITM row name contains 'Sensor', 'Stealth' or 'Distortion'; sensor behaviour has no item row to resolve in the pinned data.");
        var red = Config.RadarColor("ColorEnemy");
        notes.Add($"The catalog calls the [Radar] enemy color 'red'; ColorEnemy (line {red.Entry.LineNumber}) is the raw text {red.Entry.Value}; the RGB/BGR channel order is not established. [Radar] has eight keys (lines {Config.RadarSection.Entries[0].LineNumber}..{Config.RadarSection.Entries[^1].LineNumber}), not only the three cited.");
        var teamRadar = OriginalTeamInfo.All(Config.Cfg).Select(t => t.RadarColorRaw).Distinct().ToList();
        if (teamRadar.Count == 1)
            notes.Add($"All {OriginalTeamInfo.TeamCount} [TeamInfoN] RadarColor keys are the raw text {teamRadar[0]} (see OriginalTeamInfo); whether they override [Radar] colors is not established.");
        return notes;
    }
}
