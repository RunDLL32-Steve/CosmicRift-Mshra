using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F064 (data clause only): the raw CFG [LOS]/[Arena]/[Radar]/[LosTypeN] vision keys, the vision/radar fields f[65] and f[132..141]
/// of all 30 VEH rows, and NWS lines 258/305-315/325. Oracle values were read from the pinned CFG/rogue.cfg, VEH/Rogue.veh, ITM/Rogue_mp.itm and
/// NWS/Rogue.nws (eb1514ea) and reproduced by scripts/original-assets/audit_rt_f064.py. VEH field names are upstream-parser names, not
/// executable evidence. They document raw data and visible mismatches; they do not validate any line-of-sight, team-vision, fog, sensor or radar
/// behaviour. Asset tests are Skipped without the assets.
/// </summary>
public sealed class OriginalVisibilityTests
{
    private static readonly Lazy<OriginalVisibility> Data = new(() => OriginalVisibility.Load(OriginalAssets.Root!));

    private const string Synthetic =
        "[Level]\r\nRadarPrizeDistance=7\r\n\r\n[Arena]\r\nTeamVisionDistance=9\r\nSpecsSeeEnergy=1\r\nFriendlySeeEnergy=1\r\nEnemySeeEnergy=0\r\n\r\n" +
        "[LOS]\r\nDefaultDistance=1\r\nDefaultAngle=2\r\nDefaultXray=3\r\nTeamSharing=4\r\n\r\n[Flag]\r\nCarrierOnRadar=0\r\nCarrierLineOfSight=0\r\n\r\n" +
        "[Soccer]\r\nShowRadar=1\r\n\r\n[Uiart]\r\nVision=a,b\r\nWarFog=c,d\r\nRadarBorder=e,f\r\n\r\n" +
        "[Radar]\r\nColorSelf=0x00ff00\r\nFontColorSelf=2\r\n\r\n";

    [Fact]
    public void SyntheticConfigKeepsRawEntriesAndLines()
    {
        var text = Synthetic + string.Concat(Enumerable.Range(0, 8).Select(i =>
            $"[LosType{i}]\r\nSeeType0=1\r\nSeeType1=1\r\nSeeType2=1\r\nSeeType3=1\r\nSeeType4=1\r\nSeeType5=0\r\nSeeType6=1\r\nSeeType7=1\r\nVisibleDistance={i - 1}\r\nSolid=0\r\n\r\n"));
        var v = OriginalVisibilityConfig.Of(OriginalCfg.Parse(text, "t.cfg"));
        Assert.Equal((11, "DefaultDistance", "1"), (v.DefaultDistance.LineNumber, v.DefaultDistance.Key, v.DefaultDistance.Value));
        Assert.Equal((5, "9"), (v.TeamVisionDistance.LineNumber, v.TeamVisionDistance.Value));
        Assert.Equal(0x00ff00, v.RadarColor("ColorSelf").Value);
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 0, 1, 1 }, v.LosTypes[3].SeeTypeValues);
        Assert.Equal(2, v.LosTypes[3].VisibleDistanceValue);
    }

    [Fact]
    public void MissingKeyThrowsInsteadOfDefaulting()
    {
        var cfg = OriginalCfg.Parse(Synthetic.Replace("TeamSharing=4\r\n", ""), "t.cfg");
        Assert.Throws<OriginalDataException>(() => OriginalVisibilityConfig.Of(cfg));
    }

    [RequiresOriginalAssetsFact]
    public void LosSectionIsOnTheCatalogLines()
    {
        var c = Data.Value.Config;
        Assert.Equal(118, c.LosSection.LineNumber);
        Assert.Equal(new[] { ("DefaultDistance", "250", 119), ("DefaultAngle", "200", 120), ("DefaultXray", "0", 121), ("TeamSharing", "1", 122) },
            c.LosSection.Entries.Select(e => (e.Key, e.Value, e.LineNumber)));
        Assert.Equal(4, c.LosSection.Entries.Count);
        Assert.Equal("DefaultDistance=250", c.DefaultDistance.RawLine);
    }

    [RequiresOriginalAssetsFact]
    public void ArenaAndOtherRelatedKeysArePinned()
    {
        var c = Data.Value.Config;
        Assert.Equal(("TeamVisionDistance", "1200", 32), (c.TeamVisionDistance.Key, c.TeamVisionDistance.Value, c.TeamVisionDistance.LineNumber));
        Assert.Equal(new[] { ("SpecsSeeEnergy", "1", 60), ("FriendlySeeEnergy", "1", 61), ("EnemySeeEnergy", "0", 62) },
            new[] { c.SpecsSeeEnergy, c.FriendlySeeEnergy, c.EnemySeeEnergy }.Select(e => (e.Key, e.Value, e.LineNumber)));
        Assert.Equal(("RadarPrizeDistance", "2000", 23), (c.LevelRadarPrizeDistance.Key, c.LevelRadarPrizeDistance.Value, c.LevelRadarPrizeDistance.LineNumber));
        Assert.Equal(new[] { ("CarrierOnRadar", "0", 866), ("CarrierLineOfSight", "0", 867) },
            new[] { c.FlagCarrierOnRadar, c.FlagCarrierLineOfSight }.Select(e => (e.Key, e.Value, e.LineNumber)));
        Assert.Equal(("ShowRadar", "1", 2099), (c.SoccerShowRadar.Key, c.SoccerShowRadar.Value, c.SoccerShowRadar.LineNumber));
        Assert.Equal(new[] { ("Vision", "cruiart,vision", 2739), ("WarFog", "cruiart,warfog", 2742), ("RadarBorder", "cruiart,newradar", 2751) },
            new[] { c.UiartVision, c.UiartWarFog, c.UiartRadarBorder }.Select(e => (e.Key, e.Value, e.LineNumber)));
        // key names in the pinned file for the sections the scan covered: no Fog/Sensor/Xray key other than the ones above
        var names = c.Cfg.Sections.SelectMany(s => s.Entries.Select(e => (Section: s.Name, e.Key)))
            .Where(t => new[] { "xray", "fog", "sensor", "sight" }.Any(w => t.Key.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .Select(t => $"{t.Section}.{t.Key}").ToArray();
        Assert.Equal(new[] { "LOS.DefaultXray", "Flag.CarrierLineOfSight", "Uiart.WarFog" }, names);
    }

    [RequiresOriginalAssetsFact]
    public void RadarSectionIsPinned()
    {
        var c = Data.Value.Config;
        Assert.Equal(2276, c.RadarSection.LineNumber);
        Assert.Equal(new[]
        {
            ("ColorSoccerBall", "0xffffff", 2277), ("ColorSelf", "0x00ff00", 2278), ("ColorFriendly", "0xffff00", 2279), ("ColorEnemy", "0xff4040", 2280),
            ("ColorFlag", "0xffffff", 2281), ("FontColorSelf", "2", 2282), ("FontColorFriendly", "4", 2283), ("FontColorEnemy", "1", 2284),
        }, c.RadarSection.Entries.Select(e => (e.Key, e.Value, e.LineNumber)));
        Assert.Equal(new[] { 0xffffff, 0x00ff00, 0xffff00, 0xff4040, 0xffffff }, c.RadarColors.Select(r => r.Value));
        Assert.Equal(0xff4040, c.RadarColor("ColorEnemy").Value);
    }

    [RequiresOriginalAssetsFact]
    public void LosTypeBlocksArePinned()
    {
        var t = Data.Value.Config.LosTypes;
        Assert.Equal(8, t.Count);
        Assert.Equal(Enumerable.Range(0, 8).Select(i => 2767 + 12 * i), t.Select(x => x.HeaderLine));
        Assert.Equal(new[] { -1, -1, -1, 0, 4, -1, 12, 16 }, t.Select(x => x.VisibleDistanceValue));
        Assert.Equal(new[] { 0, 1, 1, 0, 0, 0, 0, 0 }, t.Select(x => x.SolidValue));
        Assert.All(t.Where(x => x.Index != 5), x => Assert.Equal(new[] { 1, 1, 1, 1, 1, 0, 1, 1 }, x.SeeTypeValues));
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 1, 1, 1 }, t[5].SeeTypeValues);
        Assert.Equal((2768, 2775, 2776, 2777), (t[0].SeeType[0].LineNumber, t[0].SeeType[7].LineNumber, t[0].VisibleDistance.LineNumber, t[0].Solid.LineNumber));
    }

    [RequiresOriginalAssetsFact]
    public void AllThirtyVehRowsAreCoveredAndHullsHaveNoLosOverride()
    {
        var d = Data.Value;
        Assert.Equal(30, d.Vehicles.Count);
        Assert.Equal(12, d.Hulls.Count());
        Assert.Equal(17, d.Computers.Count());
        Assert.Equal(new[] { "Hornet", "Skithist", "Asp", "Kragath", "Mastakith", "Rak'Kir", "Rip'Pir", "Zap'Pir", "Zag'Gir", "Tarthist", "Excelsior", "Ravager" }, d.Hulls.Select(h => h.Name));
        Assert.Equal(Enumerable.Range(18, 12), d.Hulls.Select(h => h.RowNumber));
        // every hull: LosDistance f132, LosAngle f133, LosXRay f134 = 0; CombatAwarenessTime f135 = 0; f138..f141 = 1,-1,1,-1; RadarPrize f65 = -1
        Assert.All(d.Hulls, h =>
        {
            Assert.Equal((454, 2), (h.FieldCount, h.RecordType));
            Assert.Equal((0, 0, 0), h.Los);
            Assert.Equal((132, 133, 134), (h.LosDistance.FieldIndex, h.LosAngle.FieldIndex, h.LosXRay.FieldIndex));
            Assert.Equal(0, h.CombatAwarenessTime.Value);
            Assert.Equal((1, -1, 1, -1), h.Radar);
            Assert.Equal((138, 139, 140, 141), (h.DisplayOnFriendlyRadar.FieldIndex, h.FriendlyRadarColor.FieldIndex, h.DisplayOnEnemyRadar.FieldIndex, h.EnemyRadarColor.FieldIndex));
            Assert.Equal((65, -1), (h.RadarPrize.FieldIndex, h.RadarPrize.Value));
        });
    }

    [RequiresOriginalAssetsFact]
    public void ComputerAndSpectatorRowsAreAllZeroLosAndDefaultRadar()
    {
        var d = Data.Value;
        Assert.All(d.Vehicles, v =>
        {
            Assert.Equal((0, 0, 0), v.Los);
            Assert.Equal((1, -1, 1, -1), v.Radar);
        });
        Assert.Equal(new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, d.Vehicles.Select(v => v.LosDistance.Value));
        // CombatAwarenessTime f135 is 0 on every row
        Assert.All(d.Vehicles, v => Assert.Equal((135, 0), (v.CombatAwarenessTime.FieldIndex, v.CombatAwarenessTime.Value)));
        // RadarPrize f65: -1 on 402 MeteorShower, the 12 hulls and Spectator Mode (2030), 0 on the other 16 computers
        Assert.Equal(new[] { 402 }, d.Computers.Where(v => v.RadarPrize.Value == -1).Select(v => v.Id));
        Assert.Equal(16, d.Computers.Count(v => v.RadarPrize.Value == 0));
        var spec = d.Vehicle(2030);
        Assert.Equal((30, 4, 202, -1), (spec.RowNumber, spec.RecordType, spec.FieldCount, spec.RadarPrize.Value));
        Assert.Equal(477, d.Vehicle(409).FieldCount);
        Assert.Equal((1, -1, 1, -1), d.Vehicle(409).Radar);
        Assert.Equal(new[] { 400, 401, 402, 403, 404, 405, 407, 408, 409, 410, 411, 412, 413, 414, 425, 426, 427 }, d.Computers.Select(v => v.Id));
    }

    [RequiresOriginalAssetsFact]
    public void NwsLinesArePinnedWithDatedHeadings()
    {
        var d = Data.Value;
        Assert.Equal((258, "Re-Enabled Team vision sharing."), (d.NwsTeamVision.Line.LineNumber, d.NwsTeamVision.Line.Text));
        Assert.Equal((249, "********2/27/03"), (d.NwsTeamVision.Heading.LineNumber, d.NwsTeamVision.Heading.Text));
        Assert.Equal((325, "Base LOS has been narrowed to 225 degrees. This will leave a definite \"blind spot\" on your tail."), (d.NwsBaseLos.Line.LineNumber, d.NwsBaseLos.Line.Text));
        Assert.Equal((293, "********2/24/03"), (d.NwsBaseLos.Heading.LineNumber, d.NwsBaseLos.Heading.Text));
        // the catalog's line 261 is blank
        Assert.Equal("", OriginalNws.LoadRogue(OriginalAssets.Root!).Text(OriginalVisibility.CatalogNwsTeamVisionLine));
        Assert.Equal(11, d.NwsItemLines.Count);
        Assert.Equal("New Item Added: Stealth - Active", d.NwsItemLines[0].Line.Text);
        Assert.Equal(new[] { "New Item Added: Ship Sensors - Enhanced", "New Item Added: Ship Sensors - Superior", "New Item Added: Ship Sensor Array", "New Item Added: Terran Combat Sensors" },
            d.NwsItemLines.Skip(7).Select(l => l.Line.Text));
        Assert.All(d.NwsItemLines, l => Assert.Equal(293, l.Heading.LineNumber));
    }

    [RequiresOriginalAssetsFact]
    public void NoItmRowIsNamedLikeTheNwsSensorOrStealthItems()
    {
        Assert.Empty(Data.Value.ItemsNamedLikeNwsSensorOrStealth);
    }

    [RequiresOriginalAssetsFact]
    public void VisibleMismatchesAreReported()
    {
        var m = Data.Value.VisibleMismatches;
        Assert.Contains(m, s => s.Contains("line 261") && s.Contains("blank") && s.Contains("line 258") && s.Contains("2/27/03"));
        Assert.Contains(m, s => s.Contains("225 degrees") && s.Contains("DefaultAngle") && s.Contains("200"));
        Assert.Contains(m, s => s.Contains("identical LOS triple") && s.Contains("(0, 0, 0)"));
        Assert.Contains(m, s => s.Contains("Sensor") && s.Contains("305..315"));
        Assert.Contains(m, s => s.Contains("0xff4040") && s.Contains("eight keys"));
        Assert.Contains(m, s => s.Contains("50 [TeamInfoN] RadarColor") && s.Contains("0x000000"));
    }
}
