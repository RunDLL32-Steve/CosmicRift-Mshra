using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>Oracle: FEATURE_CATALOG.md RT-F050 (CFG rows 72, 75, 83) plus an independent audit of the pinned rogue.cfg (eb1514ea).</summary>
public sealed class OriginalClassChangePruneTests
{
    [Fact]
    public void MissingKeyIsReportedNotDefaulted()
    {
        var cfg = OriginalCfg.Parse("[Arena]\r\nDeleteWeaponsOnClassChange=1\r\n", "t.cfg");
        Assert.Throws<OriginalDataException>(() => OriginalClassChangePrune.Of(cfg));
    }

    [RequiresOriginalAssetsFact]
    public void ThreeArenaFlagsKeepRawValuesAndLines()
    {
        var p = OriginalClassChangePrune.Of(OriginalCfg.LoadRogue(OriginalAssets.Root!));
        Assert.Equal((72, "1"), (p.DeleteWeaponsOnClassChange.LineNumber, p.DeleteWeaponsOnClassChange.Value));
        Assert.Equal((75, "1"), (p.DeathForceUtilityOff.LineNumber, p.DeathForceUtilityOff.Value));
        Assert.Equal((83, "500000"), (p.TeamSwitchMinEnergy.LineNumber, p.TeamSwitchMinEnergy.Value));
    }
}
