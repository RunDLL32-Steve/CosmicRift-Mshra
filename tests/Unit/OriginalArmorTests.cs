using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: FEATURE_CATALOG.md RT-F041 (Remove Armor id41 row151, ArmorRemoveHack id42 row152, hull armor rows) plus an
/// independent Python audit of the pinned Rogue_mp.itm (eb1514ea). Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalArmorTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void NineHullArmorRowsInFileOrder()
    {
        var a = OriginalArmor.All(Catalog.Value);
        Assert.Equal(new[] { 21, 22, 23, 63, 66, 108, 109, 136, 137 }, a.Select(x => x.Item.RowNumber));
        Assert.Equal(new[] { 4, 5, 6, 13, 16, 23, 24, 37, 38 }, a.Select(x => x.Item.Id));
    }

    [RequiresOriginalAssetsFact]
    public void FirstFiveArmorsKeepRawModifiersAndBuyPrice()
    {
        var a = OriginalArmor.All(Catalog.Value).Take(5).ToList();
        Assert.Equal(new[] { "Duraplas Armor", "Carbolux Armor", "Maxilite Armor", "Sillicate Armor", "Tiathamate Armor" }, a.Select(x => x.Item.Name));
        Assert.Equal(new[] { 5000, 10000, 2000, 12000, 15000 }, a.Select(x => x.Item.Buy.Value));
        Assert.Equal(new[] { -75, -100, -50, -60, -100 }, a.Select(x => x.ModifierFieldA.Value));
        Assert.Equal(new[] { -75, -100, -50, -50, -100 }, a.Select(x => x.ModifierFieldB.Value));
        Assert.Equal(new[] { -20, -25, -15, -25, -35 }, a.Select(x => x.ModifierFieldC.Value));
        Assert.Equal(42, a[0].ModifierFieldA.FieldIndex);
    }

    [RequiresOriginalAssetsFact]
    public void RemoveArmorPairsEveryArmorWithTheHackEffect()
    {
        var item = Catalog.Value.ById(41);
        Assert.Equal(151, item.RowNumber);
        Assert.Equal("Remove Armor", item.Name);
        var pairs = OriginalRemoveArmor.Pairs(Catalog.Value);
        Assert.Equal(new[] { 37, 38, 23, 5, 4, 24, 6, 13, 16 }, pairs.Select(p => p.Armor.Id));
        Assert.All(pairs, p => { Assert.Equal(42, p.Effect.Id); Assert.Equal("ArmorRemoveHack", p.Effect.Name); Assert.Equal(152, p.Effect.RowNumber); });
        var armorIds = OriginalArmor.All(Catalog.Value).Select(x => x.Item.Id).OrderBy(x => x);
        Assert.Equal(armorIds, pairs.Select(p => p.Armor.Id).OrderBy(x => x));
    }
}
