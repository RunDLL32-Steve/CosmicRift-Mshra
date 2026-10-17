using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F063 (record/revision audit only): NWS/Rogue.nws lines 329-332 and a text search of every ITM/VEH/RPG/CFG record of the selected Rogue
/// package for the storm and radiation words. Oracle values were read from the pinned files (eb1514ea) and independently reproduced by
/// scripts/original-assets/audit_rt_f063.py. They document that no readable record implements the Space Storm; they do not validate any storm
/// behaviour. LIO is only plain-text searched in the last test (seen as raw text, not read or pinned). Asset tests are Skipped without the assets.
/// </summary>
public sealed class OriginalSpaceStormTests
{
    private static readonly Lazy<OriginalSpaceStorm> Data = new(() => OriginalSpaceStorm.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void NwsLines329To332AreVerbatimUnderTheirDatedHeading()
    {
        var d = Data.Value;
        Assert.Equal((327, "********2/20/03"), (d.Heading.LineNumber, d.Heading.Text));
        Assert.Equal(
            new[]
            {
                (329, "Added Space Storms to the map.  These storms start somewhere random and grow over time, then shrink and finally disappear."),
                (330, ""),
                (331, "The storms discharge various high intensity radiations.  These discharges seriously effect your ship and it's shields and can destroy a players"),
                (332, "ship if their energy gets low."),
            },
            d.Lines.Select(l => (l.LineNumber, l.Text)));
        Assert.Equal(new[] { (363, "adjusted Bio Storm."), (427, "Buffed up the Moltor Storm Launcher.") }, d.OtherNwsStormLines.Select(l => (l.LineNumber, l.Text)));
    }

    [RequiresOriginalAssetsFact]
    public void SearchCoversEveryRecordOfTheSelectedPackage()
    {
        var d = Data.Value;
        Assert.Equal((200, 30, 12, 2907), (d.ItemCount, d.VehicleCount, d.RpgCount, d.CfgLineCount));
        Assert.Equal(new[] { "storm", "radiat", "discharg" }, OriginalSpaceStorm.NameTerms);
        Assert.Equal(new[] { "radio", "grow", "shrink", "nebula", "cloud" }, OriginalSpaceStorm.MechanismTerms);
    }

    [RequiresOriginalAssetsFact]
    public void TextHitsArePinnedAndNoRecordIsASpaceStormCandidate()
    {
        var d = Data.Value;
        Assert.Equal(
            new[]
            {
                ("ITM/Rogue_mp.itm", 13, 6, "radio", "ITM 102 Plasma Mine"),
                ("ITM/Rogue_mp.itm", 68, 6, "radio", "ITM 2003 Tellerium"),
                ("ITM/Rogue_mp.itm", 69, 6, "radio", "ITM 2004 Refined Tellerium"),
                ("ITM/Rogue_mp.itm", 124, 3, "cloud", "ITM 1061 BioCloud-Child-LT"),
                ("ITM/Rogue_mp.itm", 125, 3, "cloud", "ITM 1062 BioCloud-Child-RT"),
                ("ITM/Rogue_mp.itm", 126, 3, "storm", "ITM 1063 BioMissile Storm Launcher"),
                ("ITM/Rogue_mp.itm", 127, 3, "storm", "ITM 3012 BioStorm-Blast"),
                ("ITM/Rogue_mp.itm", 127, 6, "storm", "ITM 3012 BioStorm-Blast"),
                ("ITM/Rogue_mp.itm", 128, 3, "storm", "ITM 2013 BioStorm Missiles"),
                ("ITM/Rogue_mp.itm", 128, 6, "storm", "ITM 2013 BioStorm Missiles"),
                ("ITM/Rogue_mp.itm", 129, 3, "cloud", "ITM 1064 BioCloud-Child-LT2"),
                ("ITM/Rogue_mp.itm", 130, 3, "cloud", "ITM 1066 BioCloud-Child-RT2"),
                ("VEH/Rogue.veh", 15, 267, "storm", "VEH 425 Manufactory - Missile"),
                ("CFG/rogue.cfg", 2724, 0, "grow", "CFG [Jackpot] TrickleGrow"),
            },
            d.Hits.Select(h => (h.Source, h.Row, h.FieldIndex, h.Term, h.Owner)));
        // no RPG row and no hit at all for radiat / discharg / shrink / nebula
        Assert.DoesNotContain(d.Hits, h => h.Source.StartsWith("RPG", StringComparison.Ordinal));
        Assert.DoesNotContain(d.Hits, h => h.Term is "radiat" or "discharg" or "shrink" or "nebula");
        Assert.Equal("TrickleGrow=0", d.Hits.Single(h => h.Source.StartsWith("CFG", StringComparison.Ordinal)).Text);
        Assert.Equal("Produce Biostorm Missiles 1:1 RT", d.Hits.Single(h => h.Source.StartsWith("VEH", StringComparison.Ordinal)).Text);
        // the six name hits are all BioStorm missile items or the Manufactory label that produces their ammo; each is dismissed with a reason
        Assert.Equal(6, d.NameHits.Count);
        Assert.Equal(6, d.Dismissals.Count);
        Assert.All(d.NameHits, h => Assert.Contains("bio", h.Owner + h.Text, StringComparison.OrdinalIgnoreCase));
        Assert.All(d.Dismissals, x => Assert.False(string.IsNullOrWhiteSpace(x.Reason)));
        Assert.Empty(d.SpaceStormCandidates);
    }

    [RequiresOriginalAssetsFact]
    public void VehComputerRowsAreAllNamedAndNoneIsAStorm()
    {
        var d = Data.Value;
        Assert.Equal(
            new[]
            {
                (400, "Refinery"), (401, "Null Death Orb"), (402, "MeteorShower"), (403, "PirateStation"), (404, "Trading Outpost"),
                (405, "Null Kill Station"), (407, "Repair Dock"), (408, "Micro Repair Dock"), (409, "Solar Mold"), (410, "GunSat"),
                (411, "BeamSat"), (412, "Micro Refinery"), (413, "Rogue Trader Outpost"), (414, "Mold Spore"),
                (425, "Manufactory - Missile"), (426, "Manufactory - Munitions"), (427, "Manufactory - Defense"),
            },
            d.Computers.Select(c => (c.Id, c.Name)));
        Assert.All(d.Computers, c => Assert.DoesNotContain("storm", c.Name, StringComparison.OrdinalIgnoreCase));
    }

    [RequiresOriginalAssetsFact]
    public void VisibleMismatchesAreRecordedAndNothingIsCorrected()
    {
        var d = Data.Value;
        Assert.Equal(4, d.VisibleMismatches.Count);
        Assert.StartsWith("NWS lines 329-332 (heading ********2/20/03, line 327) add Space Storms", d.VisibleMismatches[0], StringComparison.Ordinal);
        Assert.Contains("a plain-text search of LIO/Rogue.lio finds no line containing", d.VisibleMismatches[1], StringComparison.Ordinal);
        Assert.Contains("17 type-5 computer rows (400, 401, 402, 403, 404, 405, 407, 408, 409, 410, 411, 412, 413, 414, 425, 426, 427)", d.VisibleMismatches[2], StringComparison.Ordinal);
        Assert.Equal("NWS storm wording outside 329-332 (363, 427) names other storms ('Bio Storm', 'Moltor Storm Launcher'), not the map hazard.", d.VisibleMismatches[3]);
    }

    [RequiresOriginalAssetsFact]
    public void LioIsOnlyPlainTextSearchedSeenAsRawTextNotReadOrPinned()
    {
        // Plain substring search of the raw LIO lines; no parser. The selected level file (CFG [Level] LioFile=Rogue.lio) has no hit.
        var root = OriginalAssets.Root!;
        static string[] Lines(string path) => File.ReadAllLines(path, System.Text.Encoding.Latin1);
        bool Hit(string l) => OriginalSpaceStorm.NameTerms.Any(t => l.Contains(t, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Rogue.lio", OriginalCfg.LoadRogue(root).Section("Level").Find("LioFile")!.Value);
        Assert.DoesNotContain(Lines(Path.Combine(root, "LIO", "Rogue.lio")), Hit);
        // chaos3.lio (a different level package, not selected) is the only LIO file with the word; raw lines 261-264 only
        var hits = Directory.GetFiles(Path.Combine(root, "LIO"), "*.lio").OrderBy(p => p, StringComparer.Ordinal)
            .SelectMany(p => Lines(p).Select((l, i) => (File: Path.GetFileName(p), Line: i + 1, Text: l)).Where(x => Hit(x.Text)))
            .ToList();
        Assert.Equal(new[] { ("chaos3.lio", 261), ("chaos3.lio", 262), ("chaos3.lio", 263), ("chaos3.lio", 264) }, hits.Select(h => (h.File, h.Line)));
        Assert.Contains("\"Storm-Master\"", hits[0].Text, StringComparison.Ordinal);
        Assert.Contains("Space Storm Alert!!!", hits[0].Text, StringComparison.Ordinal);
    }
}
