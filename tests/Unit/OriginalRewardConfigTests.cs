using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>Oracle: CFG/rogue.cfg [Bounty] line 176, [Cash] 234, [Experience] 241, [Point] 844 (independent read of the pinned file; catalog RT-F051 cites 177-180, 235-239, 845-849). Asset tests are Skipped without assets.</summary>
public sealed class OriginalRewardConfigTests
{
    private static readonly Lazy<OriginalCfg> Rogue = new(() => OriginalCfg.LoadRogue(OriginalAssets.Root!));

    [Fact]
    public void ParsesSyntheticBlocks()
    {
        var cfg = OriginalCfg.Parse("[Bounty]\r\nStart=1\r\nPercentToKillerBounty=2\r\nPercentToAssistBounty=3\r\nFixedToKillerBounty=4\r\n[Cash]\r\nShareRadius=5\r\nSharePercent=6\r\nPercentOfKiller=7\r\nPercentOfTarget=8\r\nKillReward=9\r\n", "t.cfg");
        Assert.Equal(new OriginalBountyConfig(1, 1, 2, 3, 4), OriginalBountyConfig.From(cfg));
        Assert.Equal(new OriginalRewardBlock("Cash", 6, 5, 6, 7, 8, 9), OriginalRewardBlock.From(cfg, "Cash"));
    }

    [Fact]
    public void MissingKeyThrows()
    {
        Assert.Throws<OriginalDataException>(() => OriginalBountyConfig.From(OriginalCfg.Parse("[Bounty]\r\nStart=1\r\n", "t.cfg")));
    }

    [RequiresOriginalAssetsFact]
    public void RogueBountyMatchesTheFile()
    {
        Assert.Equal(new OriginalBountyConfig(176, 1, 80, 20, 0), OriginalBountyConfig.From(Rogue.Value));
    }

    [RequiresOriginalAssetsFact]
    public void RogueCashExperienceAndPointMatchTheFile()
    {
        Assert.Equal(new OriginalRewardBlock("Cash", 234, 0, 0, 100, 1000, 75), OriginalRewardBlock.From(Rogue.Value, "Cash"));
        Assert.Equal(new OriginalRewardBlock("Experience", 241, 0, 0, 0, 0, 0), OriginalRewardBlock.From(Rogue.Value, "Experience"));
        Assert.Equal(new OriginalRewardBlock("Point", 844, 500, 250, 100, 1000, 0), OriginalRewardBlock.From(Rogue.Value, "Point"));
    }
}
