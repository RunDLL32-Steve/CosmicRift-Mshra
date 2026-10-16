using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F052/RT-F053 data clauses. Synthetic tests always run; oracle tests pin values from the pinned CFG/rogue.cfg (eb1514ea)
/// and match scripts/original-assets/audit_rt_f052_f053.py. Asset tests are Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalTeamInfoTests
{
    private static readonly Lazy<OriginalCfg> Rogue = new(() => OriginalCfg.LoadRogue(OriginalAssets.Root!));

    private const string Block =
        "[TeamInfo7]\r\nName=Zed\r\nDefaultVehicle=3\r\nRelativeVehicle=1\r\nColors=0x10,255,-2\r\nRadarColor=0xFF00FF\r\nNameFontColor=-1\r\n" +
        "MaxPlayers=4\r\nEventString=ev\r\nhAdjust=1\r\nsAdjust=2\r\nvAdjust=3\r\nDisallowWinFlagGame=1\r\n" +
        "RtsState0=-1\r\nRtsState1=0\r\nRtsState2=2\r\nRtsState3=3\r\nRtsState4=4\r\nRtsState5=5\r\nRtsState6=6\r\nRtsState7=7\r\nTeamEventStartGame=go\r\n";

    [Fact]
    public void ReadsEveryFieldRawFromASyntheticBlockRegardlessOfKeyOrder()
    {
        var cfg = OriginalCfg.Parse("[Pad]\r\na=1\r\n" + Block, "t.cfg");
        var t = OriginalTeamInfo.Read(cfg, 7);
        Assert.Equal(7, t.Index);
        Assert.Equal(3, t.HeaderLine);
        Assert.Equal(4, t.NameLine);
        Assert.Equal("Zed", t.Name);
        Assert.Equal(3, t.DefaultVehicle);
        Assert.Equal(1, t.RelativeVehicle);
        Assert.Equal("0x10,255,-2", t.ColorsRaw);
        Assert.Equal(new[] { 16, 255, -2 }, t.ColorValues);
        Assert.Equal("0xFF00FF", t.RadarColorRaw);
        Assert.Equal(0xFF00FF, t.RadarColor);
        Assert.Equal(-1, t.NameFontColor);
        Assert.Equal(4, t.MaxPlayers);
        Assert.Equal("ev", t.EventString);
        Assert.Equal((1, 2, 3), (t.HAdjust, t.SAdjust, t.VAdjust));
        Assert.Equal(1, t.DisallowWinFlagGame);
        Assert.Equal(new[] { -1, 0, 2, 3, 4, 5, 6, 7 }, t.RtsStates);
        Assert.Equal("go", t.TeamEventStartGame);
        Assert.Equal(21, t.Entries.Count);
        Assert.False(t.NameIsPublicIndexPattern);
    }

    [Fact]
    public void ParsesColorTokensAndRejectsGarbage()
    {
        Assert.Equal(0xedeae0, OriginalTeamInfo.ParseColor("0xedeae0"));
        Assert.Equal(-1, OriginalTeamInfo.ParseColor("-1"));
        Assert.Equal(-1, OriginalTeamInfo.ParseColor("0xFFFFFFFF"));
        Assert.Throws<OriginalDataException>(() => OriginalTeamInfo.ParseColor("red"));
        Assert.Throws<OriginalDataException>(() => OriginalTeamInfo.ParseColor("0x"));
    }

    [Fact]
    public void EmptyColorsYieldsNoValuesAndMissingKeysOrSectionsThrow()
    {
        var cfg = OriginalCfg.Parse(Block.Replace("Colors=0x10,255,-2", "Colors="), "t.cfg");
        Assert.Empty(OriginalTeamInfo.Read(cfg, 7).ColorValues);
        Assert.Throws<OriginalDataException>(() => OriginalTeamInfo.Read(cfg, 8));
        var missing = OriginalCfg.Parse(Block.Replace("MaxPlayers=4\r\n", ""), "t.cfg");
        Assert.Throws<OriginalDataException>(() => OriginalTeamInfo.Read(missing, 7));
    }

    [Fact]
    public void PublicIndexPatternIsAPureNameTextCheck()
    {
        var cfg = OriginalCfg.Parse(Block.Replace("Name=Zed", "Name=Public7"), "t.cfg");
        Assert.True(OriginalTeamInfo.Read(cfg, 7).NameIsPublicIndexPattern);
    }

    [Fact]
    public void ArenaFrequenciesReadRawValuesWithLineNumber()
    {
        var cfg = OriginalCfg.Parse("[Arena]\r\nFrequencyMax=9\r\nMaxPerFrequency=8\r\nDesiredFrequencies=7\r\nAllowPrivateFrequencies=0\r\n", "t.cfg");
        var f = OriginalArenaFrequencies.Read(cfg);
        Assert.Equal((0, 5, 9, 8, 7), (f.AllowPrivateFrequencies, f.AllowPrivateFrequenciesLine, f.FrequencyMax, f.MaxPerFrequency, f.DesiredFrequencies));
        Assert.Throws<OriginalDataException>(() => OriginalArenaFrequencies.Read(OriginalCfg.Parse("[Arena]\r\nFrequencyMax=9\r\n", "t.cfg")));
    }

    [RequiresOriginalAssetsFact]
    public void FiftyTeamInfoBlocksAreContiguousAndKeepAllRawEntries()
    {
        var teams = OriginalTeamInfo.All(Rogue.Value);
        Assert.Equal(50, teams.Count);
        Assert.Equal(50, Rogue.Value.Sections.Count(s => s.Name.StartsWith("TeamInfo", StringComparison.Ordinal)));
        Assert.All(teams, t => Assert.Equal(21, t.Entries.Count));
        Assert.Equal(940, teams[0].HeaderLine);
        Assert.Equal(2067, teams[49].HeaderLine);
        Assert.All(teams, t => Assert.Equal(t.HeaderLine + 1, t.NameLine));
        Assert.All(teams, t =>
        {
            Assert.Equal("0xffffff,0xedeae0,0xf1ddc4", t.ColorsRaw);
            Assert.Equal(new[] { 0xffffff, 0xedeae0, 0xf1ddc4 }, t.ColorValues);
            Assert.Equal("0x000000", t.RadarColorRaw);
            Assert.Equal(0, t.RadarColor);
            Assert.Equal(-1, t.NameFontColor);
            Assert.Equal(-1, t.DefaultVehicle);
            Assert.Equal(0, t.RelativeVehicle);
            Assert.Equal("", t.EventString);
            Assert.Equal("", t.TeamEventStartGame);
            Assert.Equal(0, t.DisallowWinFlagGame);
            Assert.Equal(0, t.SAdjust);
            Assert.Equal(0, t.VAdjust);
            Assert.Equal(Enumerable.Repeat(-1, 8), t.RtsStates);
        });
    }

    [RequiresOriginalAssetsFact]
    public void NamedTeamsAreTeamInfo0To12AndTheRestArePublicPlaceholders()
    {
        var teams = OriginalTeamInfo.All(Rogue.Value);
        Assert.DoesNotContain(teams, t => t.Name.Length == 0);
        Assert.Equal(
            new[] { "Cold Harbor", "Northern Cross", "Cassandra", "Yankee Station", "Free Traders", "Kurandis", "Chaeron", "Artemis", "Oort", "Methlus", "Cormus", "Flyers", "Fearless" },
            teams.Take(13).Select(t => t.Name));
        Assert.Equal(Enumerable.Range(0, 13), teams.Where(t => t.MaxPlayers > 0).Select(t => t.Index));
        Assert.Equal(Enumerable.Range(13, 37), teams.Where(t => t.NameIsPublicIndexPattern).Select(t => t.Index));
        Assert.All(teams.Skip(13), t => Assert.Equal(0, t.MaxPlayers));
        Assert.Equal(new[] { 10, 10, 10, 10, 10, 9, 9, 9, 9, 9, 9, 9, 9 }, teams.Take(13).Select(t => t.MaxPlayers));
        Assert.Equal(new[] { 25, 0, 65, 75, 85, 65, 15, 5, 95, 75, 35, 0, 0 }, teams.Take(13).Select(t => t.HAdjust));
        Assert.All(teams.Skip(13), t => Assert.Equal(0, t.HAdjust));
    }

    [RequiresOriginalAssetsFact]
    public void MilitiaTeamsAndFreeTradersMatchTheCatalogLines()
    {
        var teams = OriginalTeamInfo.All(Rogue.Value);
        Assert.Equal(new[] { 941, 964, 987, 1010 }, teams.Take(4).Select(t => t.NameLine));
        Assert.Equal("Free Traders", teams[4].Name);
        Assert.Equal(1033, teams[4].NameLine);
        Assert.Equal(1032, teams[4].HeaderLine);
        Assert.Equal(10, teams[4].MaxPlayers);
        Assert.Equal(85, teams[4].HAdjust);
    }

    [RequiresOriginalAssetsFact]
    public void ArenaAllowPrivateFrequenciesIsOneAtLine31()
    {
        var f = OriginalArenaFrequencies.Read(Rogue.Value);
        Assert.Equal(1, f.AllowPrivateFrequencies);
        Assert.Equal(31, f.AllowPrivateFrequenciesLine);
        Assert.Equal((5000, 5, 2), (f.FrequencyMax, f.MaxPerFrequency, f.DesiredFrequencies));
    }
}
