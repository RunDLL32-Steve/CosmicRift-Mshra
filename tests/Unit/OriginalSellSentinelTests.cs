using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F049 data audit: distribution of raw buy (f[8]) and sell (f[15]) over the 200 pinned Rogue_mp.itm rows, obtained by
/// an independent Python audit (eb1514ea). Nothing is interpreted: -1 stays a sentinel of unvalidated meaning.
/// </summary>
public sealed class OriginalSellSentinelTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void BuySellDistributionMatchesAudit()
    {
        var items = Catalog.Value.Items;
        Assert.Equal(200, items.Count);
        Assert.DoesNotContain(items, i => i.Buy.Value < 0);
        Assert.DoesNotContain(items, i => i.Sell.Value < -1);
        Assert.Equal(34, items.Count(i => i.Sell.Value == -1));
        Assert.Equal(94, items.Count(i => i.Sell.Value > 0));
        Assert.Equal(72, items.Count(i => i.Sell.Value == 0));
        Assert.Equal(114, items.Count(i => i.Buy.Value > 0));
    }

    [RequiresOriginalAssetsFact]
    public void SellSentinelAlwaysHasZeroBuy()
    {
        var neg = Catalog.Value.Items.Where(i => i.Sell.Value == -1).ToList();
        Assert.All(neg, i => Assert.Equal(0, i.Buy.Value));
        Assert.Equal(
            new[] { "BaseChild", "BaseLimitedUse", "Child Effects", "ChildProjectiles", "Defensive Weapons", "Resources", "SpecialEffects" },
            neg.Select(i => i.Category).Distinct().OrderBy(c => c, StringComparer.Ordinal));
        Assert.Equal(6, neg.Count(i => i.Category == "Resources"));
    }

    [RequiresOriginalAssetsFact]
    public void OnlyWakeCasterChildHasSellWithoutBuy()
    {
        var odd = Catalog.Value.Items.Where(i => i.Sell.Value > 0 && i.Buy.Value == 0).ToList();
        var one = Assert.Single(odd);
        Assert.Equal((165, 1082, "Wake Caster-Child", 500), (one.RowNumber, one.Id, one.Name, one.Sell.Value));
    }
}
