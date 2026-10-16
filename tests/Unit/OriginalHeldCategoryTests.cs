using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>Oracle: CFG/rogue.cfg [HeldCategory] lines 2967-3026 and [Soul] UtilitySlots* (independent Python read, pinned eb1514ea; catalog RT-F033). Asset tests are Skipped without assets.</summary>
public sealed class OriginalHeldCategoryTests
{
    private static readonly Lazy<OriginalCfg> Rogue = new(() => OriginalCfg.LoadRogue(OriginalAssets.Root!));

    [Fact]
    public void ParsesSyntheticPairs()
    {
        var text = "[HeldCategory]\r\n" + string.Concat(Enumerable.Range(1, 30).Select(i => $"Limit{i}={(i == 1 ? 5 : -1)}\r\nExtendedLimit{i}={i}\r\n"));
        var all = OriginalHeldCategoryLimit.All(OriginalCfg.Parse(text, "t.cfg"));
        Assert.Equal(30, all.Count);
        Assert.Equal(5, all[0].Limit);
        Assert.False(all[0].IsUnlimited);
        Assert.True(all[1].IsUnlimited);
        Assert.Equal(30, all[29].ExtendedLimit);
        Assert.Equal(2, all[0].LimitLine);
        Assert.Equal(3, all[0].ExtendedLimitLine);
    }

    [Fact]
    public void RejectsWrongEntryCount()
    {
        Assert.Throws<OriginalDataException>(() => OriginalHeldCategoryLimit.All(OriginalCfg.Parse("[HeldCategory]\r\nLimit1=1\r\n", "t.cfg")));
    }

    [RequiresOriginalAssetsFact]
    public void RogueHeldCategoryRowsMatchTheFile()
    {
        var all = OriginalHeldCategoryLimit.All(Rogue.Value);
        Assert.Equal(30, all.Count);
        Assert.Equal(2967, all[0].LimitLine);
        Assert.Equal(3026, all[29].ExtendedLimitLine);
        (int id, int limit, int ext)[] expected =
        [
            (1, 500, -1), (2, -1, -1), (3, -1, -1), (4, 1, -1), (5, 1, -1), (6, 1, -1), (7, 1, -1), (8, 1, -1), (9, 1, -1),
            (10, 1, -1), (11, 1, -1), (12, 1, -1), (13, 1, -1), (14, 1, -1), (15, 2, 50), (16, 2, 25), (17, 2, 50),
            (18, 1, -1), (19, -1, -1), (20, 2, 35), (21, 1, -1), (22, 1, -1),
            (23, -1, -1), (24, -1, -1), (25, -1, -1), (26, -1, -1), (27, -1, -1), (28, -1, -1), (29, -1, -1), (30, -1, -1),
        ];
        Assert.Equal(expected, all.Select(x => (x.CategoryId, x.Limit, x.ExtendedLimit)));
        Assert.All(all, x => Assert.Equal(x.LimitLine + 1, x.ExtendedLimitLine));
    }

    [RequiresOriginalAssetsFact]
    public void RogueUtilitySlotsAreTenEverywhere()
    {
        var u = OriginalUtilitySlots.From(Rogue.Value);
        Assert.Equal(10, u.Default);
        Assert.Equal(new[] { "UtilitySlots1-99", "UtilitySlots100-199", "UtilitySlots200-299", "UtilitySlots300-399", "UtilitySlots400-499" }, u.Ranges.Select(r => r.Key));
        Assert.All(u.Ranges, r => Assert.Equal(10, r.Slots));
        Assert.Equal(99, Rogue.Value.Section("Soul").Find("UtilitySlots")!.LineNumber);
    }
}
