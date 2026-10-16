using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: FEATURE_CATALOG.md RT-F039 (rows 61/62/156 values) plus an independent Python audit of the pinned Rogue_mp.itm
/// (eb1514ea) for the remaining rows. Asset tests report Skipped, never passed, without the canonical assets.
/// </summary>
public sealed class OriginalDisableEffectsTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    // row, recharge, fire, thrust, rotate
    private static readonly (int Row, int R, int F, int T, int Ro)[] Expected =
    [
        (11, 0, 0, 0, 50), (17, 0, 0, 0, 50), (61, 150, 150, 150, 150), (62, 150, 150, 150, 150),
        (74, 0, 0, 0, 50), (95, 0, 0, 0, 50), (104, 400, 0, 0, 0), (105, 50, 0, 50, 0),
        (156, 0, 0, 300, 0), (200, 0, 0, 0, 50),
    ];

    [RequiresOriginalAssetsFact]
    public void FindsExactlyTheTenDisablingRows()
    {
        var found = OriginalDisableEffects.Find(Catalog.Value);
        Assert.Equal(Expected, found.Select(e => (e.Item.RowNumber, e.Recharge, e.Fire, e.Thrust, e.Rotate)));
    }

    [RequiresOriginalAssetsFact]
    public void BlackoutBombCarriesNoDisableDurationDespiteShieldDescription()
    {
        var item = Catalog.Value.Items.Single(i => i.RowNumber == 155);
        Assert.Equal("Blackout Bomb", item.Name);
        Assert.True(OriginalProjectile.TryOf(item, out var p));
        Assert.All(new[] { p!.AntiRecharge, p.AntiFire, p.AntiThrust, p.AntiRotate }, v => Assert.Equal(0, v.Value));
        Assert.DoesNotContain(OriginalDisableEffects.Find(Catalog.Value), e => e.Item.RowNumber == 155);
    }

    [Fact]
    public void IsDisablingIsFalseForAllZero()
    {
        var item = SyntheticItem();
        Assert.False(new OriginalDisableEffect(item, 0, 0, 0, 0).IsDisabling);
        Assert.True(new OriginalDisableEffect(item, 0, 0, 0, 1).IsDisabling);
    }

    private static OriginalItem SyntheticItem() => default!;
}
