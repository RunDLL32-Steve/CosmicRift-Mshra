using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>Oracle: RT-F040 text (row 6 id 1047 f[142]=3008, ammo item 2002 row 82) and the audit of the pinned Rogue_mp.itm recorded in the branch log.</summary>
public sealed class OriginalWeaponChainTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    private static OriginalWeaponChain Chain(int itemId) =>
        OriginalWeaponChain.Resolve(Catalog.Value, Catalog.Value.ById(itemId));

    [RequiresOriginalAssetsFact]
    public void MirvChainIsLauncherAmmoChild()
    {
        var c = Chain(1047);
        Assert.Equal(6, c.Launcher.RowNumber);
        Assert.Equal(2002, c.Ammo!.Id);
        Assert.Equal(82, c.Ammo.RowNumber);
        Assert.False(c.AmmoIsSelf);
        var child = Assert.Single(c.Children);
        Assert.Equal(3008, child.Id);
        Assert.Equal("MIRV Missile-Blast", child.Name);
    }

    [RequiresOriginalAssetsFact]
    public void MinesReferenceThemselvesAndPlasmaCannonHasNoChain()
    {
        var mine = Chain(1001);
        Assert.True(mine.AmmoIsSelf);
        Assert.Empty(mine.Children);
        var cannon = Chain(1004);
        Assert.Null(cannon.Ammo);
        Assert.Empty(cannon.Children);
    }

    [RequiresOriginalAssetsFact]
    public void ShrapnelAndMoltorShowerResolveChildren()
    {
        Assert.Equal([3011], Chain(1050).Children.Select(i => i.Id));
        Assert.Equal(2009, Chain(1050).Ammo!.Id);
        Assert.Equal(3015, Chain(1072).Children[0].Id);
    }

    [RequiresOriginalAssetsFact]
    public void EveryProjectileRowResolvesWithoutError()
    {
        var launchers = Catalog.Value.Items.Where(i => OriginalProjectile.TryOf(i, out _)).ToList();
        Assert.Equal(92, launchers.Count);
        Assert.All(launchers, i => OriginalWeaponChain.Resolve(Catalog.Value, i));
        Assert.Equal(20, launchers.Count(i => OriginalWeaponChain.Resolve(Catalog.Value, i).Ammo != null));
    }

    [RequiresOriginalAssetsFact]
    public void NonProjectileRowIsRejected() =>
        Assert.Throws<OriginalItemException>(() => Chain(2002));
}
