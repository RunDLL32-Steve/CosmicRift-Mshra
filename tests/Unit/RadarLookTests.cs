using System.Text.Json;
using RogueTrader.GameData;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F064 radar rendering step: data/ui/radar.json carries the pinned CFG/rogue.cfg [Radar] colours and [Uiart] RadarBorder art reference.
/// The asset test checks each value and line against the original file; it is Skipped without the assets.
/// </summary>
public sealed class RadarLookTests
{
    private static RadarLook Shipped() =>
        RadarLook.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "ui", "radar.json")));

    [Fact]
    public void ShippedRadarLookParsesAsGreenYellowRed()
    {
        var look = Shipped();
        Assert.Equal((0x00ff00, 0xffff00, 0xff4040), (look.SelfRgb, look.FriendlyRgb, look.EnemyRgb));
        Assert.Equal(("cruiart", "newradar"), (look.BorderArchive, look.BorderMember));
    }

    [Theory]
    [InlineData("00ff00")]
    [InlineData("0x")]
    [InlineData("0x1000000")]
    [InlineData("0xgg0000")]
    [InlineData("")]
    public void BadColourThrowsInsteadOfDefaulting(string token) =>
        Assert.Throws<JsonException>(() => RadarLook.Parse(token));

    [Fact]
    public void MissingBorderArtThrows() =>
        Assert.Throws<JsonException>(() => RadarLook.FromJson("""{"colorSelf":"0x00ff00","colorFriendly":"0xffff00","colorEnemy":"0xff4040"}"""));

    [RequiresOriginalAssetsFact]
    public void ShippedRadarLookMatchesThePinnedConfig()
    {
        var config = OriginalVisibilityConfig.Of(OriginalCfg.LoadRogue(OriginalAssets.Root!));
        var look = Shipped();
        Assert.Equal(new[] { ("ColorSelf", 2278, look.SelfRgb), ("ColorFriendly", 2279, look.FriendlyRgb), ("ColorEnemy", 2280, look.EnemyRgb) },
            new[] { "ColorSelf", "ColorFriendly", "ColorEnemy" }.Select(k => config.RadarColor(k)).Select(c => (c.Entry.Key, c.Entry.LineNumber, c.Value)));
        Assert.Equal(("RadarBorder", 2751), (config.UiartRadarBorder.Key, config.UiartRadarBorder.LineNumber));
        Assert.Equal($"{look.BorderArchive},{look.BorderMember}", config.UiartRadarBorder.Value);
    }
}
