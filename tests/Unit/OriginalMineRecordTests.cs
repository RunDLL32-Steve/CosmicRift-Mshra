using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>RT-F038 data: the seven catalogued mines carry f[94]=0, f[101]=0, f[110]=6000, f[131]=3 (FEATURE_CATALOG RT-F038 text), audited against the pinned Rogue_mp.itm. Units and arming/proximity semantics are not decided.</summary>
public sealed class OriginalMineRecordTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    // (catalog row, id, name) from the RT-F038 text.
    public static TheoryData<int, int, string> Mines => new()
    {
        { 13, 102, "Plasma Mine" }, { 15, 1044, "Moltor Mine" }, { 51, 1039, "Bio Mine" }, { 56, 1056, "Shard Mine" },
        { 61, 1001, "Disruptor Mine" }, { 62, 1005, "Concussion Mine" }, { 160, 1079, "Repulsor Mine" },
    };

    [RequiresOriginalAssetsTheory]
    [MemberData(nameof(Mines))]
    public void MineSharesRawPlacementFields(int row, int id, string name)
    {
        var item = Catalog.Value.ById(id);
        Assert.Equal(row, item.RowNumber);
        Assert.Equal(name, item.Name);
        var p = OriginalProjectile.Of(item);
        Assert.Equal(0, p.InheritedSpeed.Value);
        Assert.Equal(0, p.MuzzleVelocity.Value);
        Assert.Equal(6000, p.AliveTime.Value);
        Assert.Equal(3, p.MaxLivePerPlayer.Value);
    }

    [RequiresOriginalAssetsFact]
    public void OnlyShardMineHasExplodeChild()
    {
        var withChild = new[] { 102, 1044, 1039, 1056, 1001, 1005, 1079 }
            .Where(id => OriginalProjectile.Of(Catalog.Value.ById(id)).ExplodeItem.Value != 0).ToList();
        Assert.Equal([1056], withChild);
        Assert.Equal(3011, OriginalProjectile.Of(Catalog.Value.ById(1056)).ExplodeItem.Value);
    }

    [RequiresOriginalAssetsFact]
    public void SpaceMineCategoryRowsAreExactlyTheSixMinesAndChildMineIsDifferent()
    {
        var space = Catalog.Value.Items.Where(i => i.Category == "Space Mines").Select(i => i.Id).ToList();
        Assert.Equal([102, 1044, 1039, 1056, 1001, 1005], space);
        // ClusterMine-Child (row 54) has its own raw limits (f[101]=100, f[110]=2500, f[131]=2) and is not part of the catalogued seven.
        var child = OriginalProjectile.Of(Catalog.Value.ById(1055));
        Assert.Equal((100, 2500, 2), (child.MuzzleVelocity.Value, child.AliveTime.Value, child.MaxLivePerPlayer.Value));
    }
}
