namespace RogueTrader.OriginalData;

/// <summary>Raw rogue.cfg <c>[Bounty]</c> keys (RT-F051, data only). Percent scales, rounding and award order are NOT decoded; values and line numbers are as written.</summary>
public sealed record OriginalBountyConfig(int Line, int Start, int PercentToKillerBounty, int PercentToAssistBounty, int FixedToKillerBounty)
{
    public static OriginalBountyConfig From(OriginalCfg cfg)
    {
        var s = cfg.Section("Bounty");
        return new(s.LineNumber, s.Int("Start"), s.Int("PercentToKillerBounty"), s.Int("PercentToAssistBounty"), s.Int("FixedToKillerBounty"));
    }
}

/// <summary>Raw rogue.cfg score-style reward block; the same five keys appear in <c>[Cash]</c>, <c>[Experience]</c> and <c>[Point]</c>. Units and formula are NOT decoded.</summary>
public sealed record OriginalRewardBlock(string Section, int Line, int ShareRadius, int SharePercent, int PercentOfKiller, int PercentOfTarget, int KillReward)
{
    public static OriginalRewardBlock From(OriginalCfg cfg, string section)
    {
        var s = cfg.Section(section);
        return new(s.Name, s.LineNumber, s.Int("ShareRadius"), s.Int("SharePercent"), s.Int("PercentOfKiller"), s.Int("PercentOfTarget"), s.Int("KillReward"));
    }
}
