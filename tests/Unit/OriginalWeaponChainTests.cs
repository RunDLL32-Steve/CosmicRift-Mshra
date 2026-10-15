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
        // The type-8 child lists the real sub-projectiles (pinned record row 16, entries from f[97] stride 6).
        Assert.Equal([1045, 1048, 1045, 1046, 1049, 1046], c.ChildEntries.Select(e => e.ItemId));
        Assert.Equal([-5, -10, -15, 5, 10, 15], c.ChildEntries.Select(e => e.RawOffset));
        Assert.All(c.ChildEntries, e => Assert.NotNull(e.Item));
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
        Assert.Equal(Enumerable.Repeat(1071, 5), Chain(1072).ChildEntries.Select(e => e.ItemId));
        Assert.Equal(Enumerable.Repeat(1057, 10), Chain(1050).ChildEntries.Select(e => e.ItemId));
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

    // Synthetic catalogs (no original assets): type-6 -> type-6 -> 0 chain, cycle stop, unresolved reference.
    private static string Proj(int id, int ammo, int explode)
    {
        var f = Enumerable.Repeat("0", 227).ToArray();
        f[0] = "6"; f[1] = "v57"; f[2] = id.ToString(); f[3] = "\"n\""; f[4] = "\"Weapons\"";
        f[31] = ammo.ToString(); f[142] = explode.ToString();
        return string.Join(",", f);
    }

    private static OriginalItemCatalog Synth(params string[] rows) =>
        OriginalItemCatalog.FromRecords(RawRecordReader.Parse(string.Join("\r\n", rows), "t.itm"));

    [Fact]
    public void SyntheticTransitiveChainFollowsType6ThenStops()
    {
        var cat = Synth(Proj(1, 0, 2), Proj(2, 0, 3), Proj(3, 0, 0));
        var c = OriginalWeaponChain.Resolve(cat, cat.ById(1));
        Assert.Equal([2, 3], c.Children.Select(i => i.Id));
        Assert.Empty(c.ChildEntries);
    }

    [Fact]
    public void SyntheticCycleStopsAtRepeatedId()
    {
        var cat = Synth(Proj(1, 0, 2), Proj(2, 0, 1));
        var c = OriginalWeaponChain.Resolve(cat, cat.ById(1));
        Assert.Equal([2], c.Children.Select(i => i.Id));
    }

    [Fact]
    public void SyntheticUnresolvedReferenceThrows()
    {
        var cat = Synth(Proj(1, 0, 99));
        Assert.Throws<OriginalItemException>(() => OriginalWeaponChain.Resolve(cat, cat.ById(1)));
    }
}
