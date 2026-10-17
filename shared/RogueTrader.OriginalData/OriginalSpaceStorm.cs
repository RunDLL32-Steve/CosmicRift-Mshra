using System.Globalization;
using System.Text.RegularExpressions;

namespace RogueTrader.OriginalData;

/// <summary>
/// One text field (ITM/VEH/RPG) or CFG line (section name, key or value) of the selected Rogue package whose text contains a search term,
/// case-insensitive substring. <see cref="Row"/> is the 1-based file row/line, <see cref="FieldIndex"/> the raw field index (0 for a CFG line),
/// <see cref="Owner"/> a short label of the record ("ITM 1063 BioMissile Storm Launcher"). A text hit only; it implies no behaviour.
/// </summary>
public sealed record OriginalStormTextHit(string Source, int Row, int FieldIndex, string Term, string Owner, string Text);

/// <summary>A name-term hit (storm/radiat/discharg) with the reason recorded here for not treating it as the Space Storm record. The reason is this audit's reading of the same raw row, not original-executable evidence.</summary>
public sealed record OriginalStormDismissal(OriginalStormTextHit Hit, string Reason);

/// <summary>
/// Evidence view of RT-F063 "Space Storm radiation" (record/revision audit only). It pins NWS/Rogue.nws lines 329-332 with their dated heading,
/// then searches every record of the selected package files already read by this repository (ITM/Rogue_mp.itm, VEH/Rogue.veh, RPG/Rogue.rpg,
/// CFG/rogue.cfg) for the storm words and for the mechanism words of those lines. The result pinned by the tests is that no ITM item, VEH
/// vehicle/computer, RPG row or CFG key/section names or describes a Space Storm or its radiation discharges: the only "storm" hits are the
/// Skithzar BioStorm missile items and the Manufactory product label that makes their ammo. The original storm (random start, growth,
/// shrink, disappearance, discharges affecting ship, shields and energy) is therefore NOT implemented by any record this repository can
/// read; it is not reproduced, simulated or validated here, and the LIO map file (queue G6) is not read. Field names are not used.
/// </summary>
public sealed class OriginalSpaceStorm
{
    public const int FirstNwsLine = 329;
    public const int LastNwsLine = 332;

    /// <summary>Terms naming a storm or its radiation (searched in every text field, case-insensitive).</summary>
    public static readonly IReadOnlyList<string> NameTerms = ["storm", "radiat", "discharg"];

    /// <summary>Terms taken from the mechanism wording of the NWS lines and from neighbouring natural hazards (growth, shrink, clouds); a hit is not a candidate by itself.</summary>
    public static readonly IReadOnlyList<string> MechanismTerms = ["radio", "grow", "shrink", "nebula", "cloud"];

    public static IReadOnlyList<string> AllTerms { get; } = [.. NameTerms, .. MechanismTerms];

    private static readonly Regex HeadingPattern = new(@"^\*+\s*(\d{1,2}/\d{1,2}/\d{2})\s*$", RegexOptions.CultureInvariant);

    private OriginalSpaceStorm(
        OriginalNwsLine heading, IReadOnlyList<OriginalNwsLine> lines, IReadOnlyList<OriginalStormTextHit> hits,
        IReadOnlyList<OriginalVehicleRecord> computers, int itemCount, int vehicleCount, int rpgCount, int cfgLineCount,
        IReadOnlyList<OriginalNwsLine> otherNwsStormLines)
    {
        Heading = heading;
        Lines = lines;
        Hits = hits;
        Computers = computers;
        ItemCount = itemCount;
        VehicleCount = vehicleCount;
        RpgCount = rpgCount;
        CfgLineCount = cfgLineCount;
        OtherNwsStormLines = otherNwsStormLines;
        NameHits = hits.Where(h => NameTerms.Contains(h.Term)).ToList();
        Dismissals = NameHits.Select(h => new OriginalStormDismissal(h, Reason(h))).ToList();
        SpaceStormCandidates = [];
        VisibleMismatches = DescribeMismatches();
    }

    /// <summary>The dated heading above the lines ("********2/20/03", line 326), verbatim.</summary>
    public OriginalNwsLine Heading { get; }
    /// <summary>NWS lines 329..332 inclusive, verbatim, blank line 330 kept.</summary>
    public IReadOnlyList<OriginalNwsLine> Lines { get; }
    /// <summary>Every NWS line outside 329-332 that contains "storm" (case-insensitive), verbatim: different named storms of other revisions.</summary>
    public IReadOnlyList<OriginalNwsLine> OtherNwsStormLines { get; }
    /// <summary>Every hit of <see cref="AllTerms"/> in text fields of ITM, VEH, RPG and CFG of the selected package, in file order.</summary>
    public IReadOnlyList<OriginalStormTextHit> Hits { get; }
    /// <summary>The hits of <see cref="NameTerms"/> only.</summary>
    public IReadOnlyList<OriginalStormTextHit> NameHits { get; }
    /// <summary>Each name hit with the reason it is not the Space Storm record.</summary>
    public IReadOnlyList<OriginalStormDismissal> Dismissals { get; }
    /// <summary>Records accepted as implementing the Space Storm. Empty: none was found in ITM, VEH, RPG or CFG of the selected package.</summary>
    public IReadOnlyList<OriginalStormTextHit> SpaceStormCandidates { get; }
    /// <summary>All VEH type-5 (computer) rows, the only record class that can hold a placed non-ship actor; none is named or described as a storm.</summary>
    public IReadOnlyList<OriginalVehicleRecord> Computers { get; }
    public int ItemCount { get; }
    public int VehicleCount { get; }
    public int RpgCount { get; }
    public int CfgLineCount { get; }
    /// <summary>Notes where the catalog wording and the pinned data do not line up. Informational; nothing is corrected.</summary>
    public IReadOnlyList<string> VisibleMismatches { get; }

    public static OriginalSpaceStorm Load(string assetsRoot) =>
        Resolve(
            OriginalVehicleCatalog.Load(assetsRoot), OriginalItemCatalog.Load(assetsRoot), OriginalNws.LoadRogue(assetsRoot),
            RawRecordReader.Parse(AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Rpg), PinnedAssets.Rpg.RelativePath),
            OriginalCfg.LoadRogue(assetsRoot));

    public static OriginalSpaceStorm Resolve(
        OriginalVehicleCatalog vehicles, OriginalItemCatalog items, OriginalNws nws, IReadOnlyList<RawRecord> rpg, OriginalCfg cfg)
    {
        var lines = nws.Range(FirstNwsLine, LastNwsLine);
        if (!lines[0].Text.Contains("Added Space Storms to the map", StringComparison.Ordinal)
            || !lines[2].Text.Contains("The storms discharge various high intensity radiations", StringComparison.Ordinal))
            throw new OriginalDataException($"NWS lines {FirstNwsLine}-{LastNwsLine} do not hold the Space Storm text: '{lines[0].Text}' / '{lines[2].Text}'.");
        OriginalNwsLine? heading = null;
        for (var n = FirstNwsLine; n >= 1 && heading is null; n--)
            if (HeadingPattern.IsMatch(nws.Lines[n - 1].Text)) heading = nws.Lines[n - 1];
        if (heading is null) throw new OriginalDataException($"NWS line {FirstNwsLine} has no dated heading above it.");

        var hits = new List<OriginalStormTextHit>();
        foreach (var i in items.Items)
            Scan(hits, i.Record, $"ITM {i.Id} {i.Name}");
        foreach (var v in vehicles.All)
            Scan(hits, v.Record, $"VEH {v.Id} {v.Name}");
        foreach (var r in rpg)
            Scan(hits, r, $"RPG row {r.RowNumber}");
        var cfgLines = 0;
        foreach (var s in cfg.Sections)
        {
            cfgLines++;
            AddCfg(hits, s.LineNumber, $"CFG [{s.Name}]", s.Name);
            foreach (var e in s.Entries)
            {
                cfgLines++;
                AddCfg(hits, e.LineNumber, $"CFG [{s.Name}] {e.Key}", e.Key + "=" + e.Value);
            }
        }

        var others = nws.Lines
            .Where(l => (l.LineNumber < FirstNwsLine || l.LineNumber > LastNwsLine) && l.Text.Contains("storm", StringComparison.OrdinalIgnoreCase))
            .ToList();
        return new OriginalSpaceStorm(heading, lines, hits, vehicles.Computers.ToList(), items.Items.Count, vehicles.All.Count, rpg.Count, cfgLines, others);
    }

    private static void Scan(List<OriginalStormTextHit> hits, RawRecord record, string owner)
    {
        foreach (var f in record.Fields)
        {
            if (int.TryParse(f.Value.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)) continue;
            foreach (var term in AllTerms)
                if (f.Value.Contains(term, StringComparison.OrdinalIgnoreCase))
                    hits.Add(new OriginalStormTextHit(record.SourceFile, record.RowNumber, f.Index, term, owner, f.Value));
        }
    }

    private static void AddCfg(List<OriginalStormTextHit> hits, int line, string owner, string text)
    {
        foreach (var term in AllTerms)
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                hits.Add(new OriginalStormTextHit(OriginalCfg.Rogue.RelativePath, line, 0, term, owner, text));
    }

    private static string Reason(OriginalStormTextHit h)
    {
        if (h.Source.StartsWith("ITM", StringComparison.Ordinal))
            return $"{h.Owner} is a Skithzar BioStorm missile item (launcher, ammo or child effect); the NWS names that weapon separately as 'Bio Storm', not a map storm.";
        if (h.Source.StartsWith("VEH", StringComparison.Ordinal))
            return $"{h.Owner} f[{h.FieldIndex}] is the product label '{h.Text}' of a Manufactory build block, the ammo of the BioStorm missile.";
        return $"{h.Owner} is not a Space Storm record.";
    }

    private IReadOnlyList<string> DescribeMismatches()
    {
        var notes = new List<string>
        {
            $"NWS lines {FirstNwsLine}-{LastNwsLine} (heading {Heading.Text}, line {Heading.LineNumber}) add Space Storms that start at a random place, grow, shrink and disappear and discharge high-intensity radiations that affect ship, shields and energy, but no ITM item, VEH row, RPG row or CFG key/section of the selected package names or describes a map storm, radiation or discharge (no field contains 'radiat' or 'discharg'); the only 'storm' hits are the Skithzar BioStorm missile items and a Manufactory product label.",
            $"The catalog row says selected LIO names do not identify a Space Storm record; a plain-text search of LIO/Rogue.lio finds no line containing 'storm', 'radiat' or 'discharg' either (seen as raw text, not read or pinned, no LIO reader).",
            $"VEH has {Computers.Count} type-5 computer rows ({string.Join(", ", Computers.Select(c => c.Id))}); none is a storm, so a placed storm cannot be expressed through a VEH row of this package.",
        };
        notes.Add($"NWS storm wording outside 329-332 ({string.Join(", ", OtherNwsStormLines.Select(l => l.LineNumber))}) names other storms ('Bio Storm', 'Moltor Storm Launcher'), not the map hazard.");
        return notes;
    }
}
