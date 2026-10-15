using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: the values written in docs/original-research/FIELD_INTERPRETATION.md ("Projectile fields and negative values")
/// and FEATURE_CATALOG.md rows RT-F036/F037/F038/F039/F040, copied from that text. Asset tests report Skipped, never passed,
/// when the canonical assets are missing.
/// </summary>
public sealed class OriginalProjectileTests
{
    //                       row  itemId name                          delay inherit muzzle alive explode
    private sealed record Oracle(int Row, int ItemId, string Name, int Delay, int Inherit, int Muzzle, int Alive, int Explode);

    private static readonly Oracle[] Rows =
    [
        new(1, 1004, "Plasma Cannon", 25, 1000, 2200, 550, 0),
        new(7, 1026, "Plasma Bomb", 150, 1000, 2100, 2000, 0),
        new(6, 1047, "MIRV Missile Launcher", 400, -550, 2750, -65, 3008),
        new(141, 1069, "X-Laser", 200, -550, 4000, 350, 0),
        new(153, 1073, "Laser Cannon", 30, -550, 4000, 550, 0),
        new(61, 1001, "Disruptor Mine", 400, 0, 0, 6000, 0),
    ];

    public static IEnumerable<object[]> RowNumbers => Rows.Select(r => new object[] { r.Row });

    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    private static OriginalProjectile View(int row) =>
        OriginalProjectile.Of(Catalog.Value.Items.Single(i => i.RowNumber == row));

    [RequiresOriginalAssetsTheory]
    [MemberData(nameof(RowNumbers))]
    public void RawFieldsMatchDocumentedRow(int row)
    {
        var o = Rows.Single(r => r.Row == row);
        var p = View(row);
        Assert.Equal(o.ItemId, p.Item.Id);
        Assert.Equal(o.Name, p.Item.Name);
        Assert.Equal(o.Delay, p.FireDelay.Value);
        Assert.Equal(o.Inherit, p.InheritedSpeed.Value);
        Assert.Equal(o.Muzzle, p.MuzzleVelocity.Value);
        Assert.Equal(o.Alive, p.AliveTime.Value);
        Assert.Equal(o.Explode, p.ExplodeItem.Value);
        Assert.Equal(o.Inherit < 0, p.InheritedSpeed.IsNegativeSentinel);
        Assert.Equal(o.Alive < 0, p.AliveTime.IsNegativeSentinel);
        Assert.Equal(54, p.FireDelay.FieldIndex);
        Assert.Equal(142, p.ExplodeItem.FieldIndex);
    }

    [RequiresOriginalAssetsFact]
    public void DisruptorMineLimitsAndAntiDurations()
    {
        var p = View(61);
        Assert.Equal(3, p.MaxLivePerPlayer.Value);
        Assert.All(new[] { p.AntiRecharge, p.AntiFire, p.AntiThrust, p.AntiRotate }, v => Assert.Equal(150, v.Value));
    }

    [RequiresOriginalAssetsFact]
    public void DisablerHasThrustDurationButZeroRotation()
    {
        var p = View(156);
        Assert.Equal(300, p.AntiThrust.Value);
        Assert.Equal(0, p.AntiRotate.Value);
    }

    [RequiresOriginalAssetsFact]
    public void AmmoRowsHaveNoProjectileView()
    {
        var ammo = Catalog.Value.Items.Where(i => i.Category.StartsWith("Ammo-", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(ammo);
        Assert.All(ammo, i => Assert.False(OriginalProjectile.TryOf(i, out _)));
    }

    [RequiresOriginalAssetsFact]
    public void NinetyTwoRowsHaveTheProjectileLayoutAndAllParse()
    {
        var views = Catalog.Value.Items.Where(i => OriginalProjectile.TryOf(i, out _)).ToList();
        Assert.Equal(92, views.Count);
        Assert.All(views, i => { Assert.Equal(6, i.RecordType); Assert.Equal(227, i.Record.FieldCount); });
        Assert.Contains(Catalog.Value.Items, i => i.RecordType == 8 && !OriginalProjectile.TryOf(i, out _));
    }

    [Fact]
    public void RejectsRowsTooShortForTheView()
    {
        var fields = Enumerable.Repeat("0", 31).ToArray();
        fields[0] = "6";
        fields[1] = "v57"; fields[2] = "1"; fields[3] = "\"n\""; fields[4] = "\"c\"";
        var catalog = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(string.Join(",", fields), "t.itm"));
        Assert.Throws<OriginalItemException>(() => OriginalProjectile.Of(catalog.Items[0]));
    }
}
