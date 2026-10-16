using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F025 (data only): synthetic tests (always run) plus oracle tests pinning CFG/rogue.cfg [Uiart], [Uiwav] and [Render]
/// at eb1514ea, cross-checked by scripts/original-assets/audit_rt_f025.py. Asset tests are Skipped, never passed, without the assets.
/// BLO member existence is not checked (BLO content is LFS-only); only the BLO/&lt;archive&gt;.blo file name is.
/// </summary>
public sealed class OriginalUiArtTests
{
    private static readonly Lazy<OriginalCfg> Rogue = new(() => OriginalCfg.LoadRogue(OriginalAssets.Root!));

    [Fact]
    public void ParsesArchiveMemberPairsAndKeepsEmptyValuesExplicit()
    {
        var cfg = OriginalCfg.Parse("[Uiart]\r\nA=x,y\r\nB=\r\nC=X,z\r\n", "t.cfg");
        var s = OriginalUiArt.Uiart(cfg);
        Assert.Equal(1, s.LineNumber);
        Assert.Equal(new[] { 2, 3, 4 }, s.Entries.Select(e => e.LineNumber));
        Assert.Equal(("x", "y"), (s.Entries[0].Archive, s.Entries[0].Member));
        Assert.True(s.Entries[1].IsEmpty);
        Assert.Null(s.Entries[1].Archive);
        Assert.Null(s.Entries[1].Member);
        Assert.Equal("", s.Entries[1].RawValue);
        Assert.Equal(new[] { "x" }, s.Archives());
        Assert.Equal(new[] { ("x", true) }, s.ArchiveFiles(new[] { "X.BLO" }));
        Assert.Equal(new[] { ("x", false) }, s.ArchiveFiles(new[] { "other.blo" }));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("a,")]
    [InlineData(",b")]
    [InlineData("a,b,c")]
    [InlineData(" a,b")]
    [InlineData("a, b")]
    [InlineData("a,b ")]
    public void RejectsMalformedArchiveMemberValues(string value)
    {
        var cfg = OriginalCfg.Parse($"[Uiwav]\r\nK={value}\r\n", "t.cfg");
        Assert.Throws<OriginalDataException>(() => OriginalUiArt.Uiwav(cfg));
    }

    [Fact]
    public void RenderSettingsKeepRawTextAndParseStrictly()
    {
        var ok = "[Render]\r\nStatic=0\r\nBackgroundColor=0x00FF10\r\nFlattenLights=0\r\nTrailsFirst=1\r\nVehTrailsFirst=1\r\nStarfieldGapNear=2\r\nStarfieldGapFar=3\r\n";
        var r = OriginalRenderSettings.Read(OriginalCfg.Parse(ok, "t.cfg"));
        Assert.Equal("0x00FF10", r.BackgroundColorText);
        Assert.Equal(0x00FF10u, r.BackgroundColorHex);
        Assert.Equal(3, r.BackgroundColorLine);
        Assert.Equal(new OriginalRenderCfgInt(7, "2", 2), r.StarfieldGapNear);
        Assert.Equal(7, r.Entries.Count);
        Assert.Throws<OriginalDataException>(() => OriginalRenderSettings.Read(OriginalCfg.Parse(ok.Replace("0x00FF10", "00FF10"), "t.cfg")));
        Assert.Throws<OriginalDataException>(() => OriginalRenderSettings.Read(OriginalCfg.Parse(ok.Replace("0x00FF10", "0xZZ"), "t.cfg")));
        Assert.Throws<OriginalDataException>(() => OriginalRenderSettings.Read(OriginalCfg.Parse(ok.Replace("StarfieldGapFar=3", "StarfieldGapFar=x"), "t.cfg")));
        Assert.Throws<OriginalDataException>(() => OriginalRenderSettings.Read(OriginalCfg.Parse(ok.Replace("Static=0\r\n", ""), "t.cfg")));
    }

    [RequiresOriginalAssetsFact]
    public void UiartMatchesTheAudit()
    {
        var s = OriginalUiArt.Uiart(Rogue.Value);
        Assert.Equal(2728, s.LineNumber);
        Assert.Equal(27, s.Entries.Count);
        Assert.Equal(Enumerable.Range(2729, 27), s.Entries.Select(e => e.LineNumber));
        var expected = new (string Key, string Value)[]
        {
            ("Button1", "cruiart,button1"), ("Button2", "cruiart,button2"), ("Button3", "cruiart,button3"),
            ("Button4", "cruiart,button4"), ("Button5", "cruiart,button5"), ("MenuTitle", "cruiart,header"),
            ("CashBack", "cruiart,cashback"), ("Header1", "cruiart,header"), ("Header2", "cruiart,header"),
            ("Physics", "cruiart,physics"), ("Vision", "cruiart,vision"), ("Cursors", "cruiart,cursor"),
            ("Warp", "cruiart,warpPorter"), ("WarFog", "cruiart,warfog"), ("MainBorder", "cruiart,mainb"),
            ("Scroll1", "cruiart,scrollb1"), ("Scroll2", "cruiart,scrollb2"), ("Portal", "utilityeffects,portalgraphic"),
            ("Guage", "cruiart,gguage"), ("MessageBorder", "cruiart,messageb"), ("TinyNumbers", "cruiart,tinynum"),
            ("NotepadBorder", ""), ("RadarBorder", "cruiart,newradar"), ("Logo", "cruiart,logo"),
            ("NotepadInteriorPlayer", "cruiart,notepadp"), ("NotepadInteriorInventory", "cruiart,notepada"),
            ("GuageBack", "cruiart,gback"),
        };
        Assert.Equal(expected, s.Entries.Select(e => (e.Key, e.RawValue)));
        var empty = s.Entries.Single(e => e.IsEmpty);
        Assert.Equal(("NotepadBorder", 2750), (empty.Key, empty.LineNumber));
        Assert.Equal(("cruiart", "logo", 2752), (s.Find("Logo")!.Archive, s.Find("Logo")!.Member, s.Find("Logo")!.LineNumber));
        Assert.Equal(new[] { "cruiart", "utilityeffects" }, s.Archives());
        Assert.Equal(25, s.Entries.Count(e => e.Archive == "cruiart"));
    }

    [RequiresOriginalAssetsFact]
    public void UiwavMatchesTheAudit()
    {
        var s = OriginalUiArt.Uiwav(Rogue.Value);
        Assert.Equal(2757, s.LineNumber);
        Assert.Equal(Enumerable.Range(2758, 8), s.Entries.Select(e => e.LineNumber));
        Assert.Equal(
            new[] { "Melt", "ChangeIn", "RClick", "CantFire", "PickMenu", "WarpIn", "NoAmmo", "Pickup" },
            s.Entries.Select(e => e.Key));
        Assert.Equal(
            new[] { "melt", "changein", "rclick", "cantfire", "pickmenu", "warpin", "noammo", "pickup" },
            s.Entries.Select(e => e.Member));
        Assert.All(s.Entries, e => Assert.Equal("crwav", e.Archive));
        Assert.Equal(new[] { "crwav" }, s.Archives());
    }

    [RequiresOriginalAssetsFact]
    public void RenderSettingsMatchTheAudit()
    {
        var r = OriginalRenderSettings.Read(Rogue.Value);
        Assert.Equal(2184, r.LineNumber);
        Assert.Equal(new[] { 2185, 2186, 2187, 2188, 2189, 2190, 2191 }, r.Entries.Select(e => e.LineNumber));
        Assert.Equal(
            new[] { "Static", "BackgroundColor", "FlattenLights", "TrailsFirst", "VehTrailsFirst", "StarfieldGapNear", "StarfieldGapFar" },
            r.Entries.Select(e => e.Key));
        Assert.Equal(new OriginalRenderCfgInt(2185, "0", 0), r.Static);
        Assert.Equal(("0x000000", 0u, 2186), (r.BackgroundColorText, r.BackgroundColorHex, r.BackgroundColorLine));
        Assert.Equal(new OriginalRenderCfgInt(2187, "0", 0), r.FlattenLights);
        Assert.Equal(new OriginalRenderCfgInt(2188, "1", 1), r.TrailsFirst);
        Assert.Equal(new OriginalRenderCfgInt(2189, "1", 1), r.VehTrailsFirst);
        Assert.Equal(new OriginalRenderCfgInt(2190, "250", 250), r.StarfieldGapNear);
        Assert.Equal(new OriginalRenderCfgInt(2191, "350", 350), r.StarfieldGapFar);
    }

    [RequiresOriginalAssetsFact]
    public void ReferencedArchivesHaveBloFilesListedInTheClone()
    {
        var files = Directory.GetFiles(Path.Combine(OriginalAssets.Root!, "BLO")).Select(Path.GetFileName).Select(f => f!).ToList();
        Assert.Equal(89, files.Count);
        var art = OriginalUiArt.Uiart(Rogue.Value).ArchiveFiles(files);
        Assert.Equal(new[] { ("cruiart", true), ("utilityeffects", true) }, art);
        Assert.Equal(new[] { ("crwav", true) }, OriginalUiArt.Uiwav(Rogue.Value).ArchiveFiles(files));
    }
}
