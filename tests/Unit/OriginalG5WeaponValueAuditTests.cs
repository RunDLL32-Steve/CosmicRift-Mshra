using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// G5 value audit oracles. Every expected value is copied from docs/original-research/G5_WEAPON_VALUE_AUDIT.md, which records
/// the output of scripts/original-assets/audit_g5_weapon_values.py over the pinned Rogue_mp.itm (hash-verified). The tests only
/// check that the accessors preserve the raw values; they make no firing, lifetime, trigger or unit claim.
/// </summary>
public sealed class OriginalG5WeaponValueAuditTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    private static OriginalItem Item(int id) => Catalog.Value.ById(id);
    private static OriginalProjectile View(int id) => OriginalProjectile.Of(Item(id));
    private static IReadOnlyList<OriginalMultiItemEntry> Entries(int id) => OriginalWeaponChain.MultiItemEntries(Catalog.Value, Item(id));
    private static IEnumerable<OriginalProjectile> Projectiles =>
        Catalog.Value.Items.Select(i => OriginalProjectile.TryOf(i, out var v) ? v : null).Where(v => v != null)!;

    [RequiresOriginalAssetsFact]
    public void EveryTypeEightRowIsWholeSixFieldEntries()
    {
        var multi = Catalog.Value.Items.Where(i => i.RecordType == OriginalWeaponChain.MultiItemType).ToList();
        Assert.Equal(20, multi.Count);
        Assert.All(multi, i => Assert.Equal(0, (i.Record.FieldCount - OriginalWeaponChain.MultiItemFirstEntryField) % OriginalWeaponChain.MultiItemEntryStride));
        var all = multi.SelectMany(i => Entries(i.Id)).ToList();
        Assert.All(all, e => Assert.Equal(6, e.RawFields.Count));
        Assert.All(all, e => Assert.Equal(e.ItemId, e.RawFields[0]));
        Assert.All(all, e => Assert.Equal(0, e.DeltaZ));
        Assert.All(all, e => Assert.Equal(0, e.MultiLinkMode));
    }

    [RequiresOriginalAssetsFact]
    public void WakeCasterKeepsBothDeltasAndTheAngle()
    {
        var e = Entries(3018);
        Assert.Equal([1082, 5, -25, 0, 135, 0], e[0].RawFields);
        Assert.Equal([1082, -5, -25, 0, 225, 0], e[1].RawFields);
        Assert.Equal([135, 225], e.Select(x => x.RawOffset));
    }

    [RequiresOriginalAssetsFact]
    public void SideBySideLaunchersUseDeltaXAndBaseCannonDeltaY()
    {
        Assert.Equal([20, 10, -20, -10], Entries(3003).Select(e => e.DeltaX));
        Assert.Equal([1016, 1021, 1022, 1023], Entries(3003).Select(e => e.ItemId));
        Assert.Equal([10, -10], Entries(3002).Select(e => e.DeltaX));
        Assert.Equal([15, 15], Entries(3002).Select(e => e.DeltaY));
        foreach (var dual in new[] { 3005, 3006, 3013, 3016, 3017 })
            Assert.Equal([20, -20], Entries(dual).Select(e => e.DeltaX));
        Assert.Equal([-20, 20], Entries(3019).Select(e => e.DeltaX));
        Assert.Equal([-5, -10, -15, 5, 10, 15], Entries(3012).Select(e => e.DeltaAngle));
    }

    [RequiresOriginalAssetsFact]
    public void InheritedSpeedNegativesAreOnlyRocketsBeamsAndWakeCasterChild()
    {
        var neg = Projectiles.Where(p => p.InheritedSpeed.IsNegativeSentinel).ToDictionary(p => p.Item.Id, p => p.InheritedSpeed.Value);
        int[] minus550 = [1047, 1006, 1016, 1021, 1022, 1023, 1040, 1043, 1050, 1054, 1063, 1069, 1072, 1073, 1077];
        Assert.Equal(16, neg.Count);
        Assert.All(minus550, id => Assert.Equal(-550, neg[id]));
        Assert.Equal(-250, neg[1082]);
        Assert.All(neg.Keys, id => Assert.Equal(94, View(id).InheritedSpeed.FieldIndex));
    }

    [RequiresOriginalAssetsFact]
    public void AliveTimeNegativesKeepTheirRawValues()
    {
        var counts = Projectiles.Where(p => p.AliveTime.IsNegativeSentinel).GroupBy(p => p.AliveTime.Value).ToDictionary(g => g.Key, g => g.Count());
        var expected = new Dictionary<int, int> { [-300] = 10, [-2000] = 6, [-550] = 5, [-65] = 2, [-85] = 1, [-80] = 1, [-50] = 1, [-5] = 1, [-1] = 1 };
        Assert.Equal(expected.OrderBy(k => k.Key), counts.OrderBy(k => k.Key));
    }

    [RequiresOriginalAssetsFact]
    public void MirvAndBioStormPairNegativeLifeWithInactiveTime()
    {
        foreach (var (id, child) in new[] { (1047, 3008), (1063, 3012) })
        {
            var p = View(id);
            Assert.Equal(-65, p.AliveTime.Value);
            Assert.Equal(65, p.InactiveTime.Value);
            Assert.Equal(0, p.ProximityRadius.Value);
            Assert.Equal(-1, p.PreventPointBlank.Value);
            Assert.Equal(child, p.ExplodeItem.Value);
        }
        Assert.Equal(-85, View(1072).AliveTime.Value);
        Assert.Equal(50, View(1072).InactiveTime.Value);
    }

    [RequiresOriginalAssetsFact]
    public void XLaserBounceCountSentinelAndBeamProximity()
    {
        var x = View(1069);
        Assert.Equal(-1, x.BounceCount.Value);
        Assert.True(x.BounceCount.IsNegativeSentinel);
        Assert.Equal(24, x.ProximityRadius.Value);
        Assert.Equal(2, x.DamageMode.Value);
        Assert.Equal(2, View(1073).BounceCount.Value);
        Assert.Equal(4, View(1073).ProximityRadius.Value);
        Assert.Equal(1, Projectiles.Count(p => p.BounceCount.IsNegativeSentinel));
    }

    [RequiresOriginalAssetsFact]
    public void AmmoRowsAreTypeFourWithZeroMassAndPerShotCounts()
    {
        int[] ammo = [2000, 2001, 2002, 2007, 2009, 2010, 2012, 2013, 2014, 2015, 2016, 2017, 2018];
        Assert.All(ammo, id =>
        {
            var a = Item(id);
            Assert.Equal(4, a.RecordType);
            Assert.Equal(31, a.Record.FieldCount);
            Assert.Equal(0, a.Mass.Value);
            Assert.False(OriginalProjectile.TryOf(a, out _));
        });
        Assert.Equal((85, 85), (Item(2002).Buy.Value, Item(2002).Sell.Value));
        Assert.Equal((60, 40), (Item(2001).Buy.Value, Item(2001).Sell.Value));
        Assert.Equal(1, View(1047).AmmoUsedPerShot.Value);
        Assert.Equal(5, View(1035).AmmoCapacity.Value);
        Assert.Equal(3, View(1077).AmmoCapacity.Value);
        foreach (var dual in new[] { 3006, 3013, 3017 })
            Assert.Equal(2, Item(dual).Record.Int(WeaponChainFields.AmmoUsedPerShot).Value);
    }

    [RequiresOriginalAssetsFact]
    public void ResolverInventoryNamesTheNewFields()
    {
        var resolver = OriginalItemReferenceResolver.Load(OriginalAssets.Root!);
        var wake = resolver.Resolve(3018);
        foreach (var f in new[] { 98, 99, 100, 101, 102, 104, 105, 106, 107, 108 })
            Assert.DoesNotContain(f, wake.UnnamedFieldIndices);
        Assert.Contains("deltaX", wake.NamedFields[98]);
        Assert.Contains("multiLinkMode", wake.NamedFields[102]);
        var laser = resolver.Resolve(1069);
        foreach (var f in new[] { 32, 33, 104, 108, 115, 116, 141 })
            Assert.DoesNotContain(f, laser.UnnamedFieldIndices);
    }

    [Fact]
    public void SyntheticTruncatedEntryIsRejected()
    {
        var f = Enumerable.Repeat("0", 100).ToArray();
        f[0] = "8"; f[1] = "v57"; f[2] = "5"; f[3] = "\"m\""; f[4] = "\"c\"";
        f[97] = "5";
        var cat = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(string.Join(",", f), "t.itm"));
        Assert.Throws<OriginalItemException>(() => OriginalWeaponChain.MultiItemEntries(cat, cat.ById(5)));
    }
}
