using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F029 raw collision fields. Oracle values come from the independent Python audit scripts/audit-collision-fields.py at
/// pinned eb1514ea (12 hulls f[35]=21 / f[362]=727; rogue.cfg lines 63/125 = 500/400; Terrain10 and Terrain15 RepelVehicle=0).
/// Asset tests are Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalCollisionFieldsTests
{
    private static readonly Lazy<OriginalCollisionFields> Fields = new(() => OriginalCollisionFields.Load(OriginalAssets.Root!));

    [Fact]
    public void MissingCfgKeysFailLoudlyAndNothingIsDefaulted()
    {
        var hulls = OriginalHullCatalog.FromRecords(new List<RawRecord>(), new List<RawRecord>());
        Assert.Empty(hulls.Hulls);
        var noArena = OriginalCfg.Parse("[Vehicle]\r\nPhysicsDelay=400\r\n", "t.cfg");
        Assert.Throws<OriginalDataException>(() => OriginalCollisionFields.Resolve(hulls, noArena));
        var text = string.Concat(Enumerable.Range(0, 16).Select(t => $"[Terrain{t}]\r\nRepelVehicle=1\r\nRepelWeapons=1\r\n"));
        var missingKey = OriginalCfg.Parse(text + "[Arena]\r\nX=1\r\n[Vehicle]\r\nPhysicsDelay=400\r\n", "t.cfg");
        Assert.Throws<OriginalDataException>(() => OriginalCollisionFields.Resolve(hulls, missingKey));
        var nonInt = OriginalCfg.Parse(text + "[Arena]\r\nPointBlankDamageMultiplier=abc\r\n[Vehicle]\r\nPhysicsDelay=400\r\n", "t.cfg");
        Assert.Throws<OriginalDataException>(() => OriginalCollisionFields.Resolve(hulls, nonInt));
        var ok = OriginalCfg.Parse(text + "[Arena]\r\nPointBlankDamageMultiplier=7\r\n[Vehicle]\r\nPhysicsDelay=9\r\n", "t.cfg");
        var r = OriginalCollisionFields.Resolve(hulls, ok);
        Assert.Equal(7, r.PointBlankDamageMultiplier.Value);
        Assert.Equal(9, r.PhysicsDelay.Value);
        Assert.Equal(16, r.Terrains.Count);
    }

    [RequiresOriginalAssetsFact]
    public void AllTwelveHullsCarryTheSameRawRadiusAndBounceFields()
    {
        var hulls = Fields.Value.Hulls;
        Assert.Equal(12, hulls.Count);
        Assert.All(hulls, h =>
        {
            Assert.Equal(35, h.PhysicalRadiusCandidate.FieldIndex);
            Assert.Equal(21, h.PhysicalRadiusCandidate.Value);
            Assert.False(h.PhysicalRadiusCandidate.IsNegativeSentinel);
            Assert.Equal(362, h.BouncePercent.FieldIndex);
            Assert.Equal(727, h.BouncePercent.Value);
            Assert.False(h.BouncePercent.IsNegativeSentinel);
        });
        var asp = hulls.Single(h => h.Name == "Asp");
        Assert.Equal(20, asp.VehRow);
        Assert.Equal(new[] { 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 }, hulls.Select(h => h.VehRow).OrderBy(x => x));
    }

    [RequiresOriginalAssetsFact]
    public void RogueCfgPointBlankAndPhysicsDelayKeepLineNumbersAndRawText()
    {
        var f = Fields.Value;
        Assert.Equal(63, f.PointBlankDamageMultiplier.Entry.LineNumber);
        Assert.Equal("PointBlankDamageMultiplier", f.PointBlankDamageMultiplier.Entry.Key);
        Assert.Equal("500", f.PointBlankDamageMultiplier.Entry.Value);
        Assert.Equal(500, f.PointBlankDamageMultiplier.Value);
        Assert.Equal(125, f.PhysicsDelay.Entry.LineNumber);
        Assert.Equal("PhysicsDelay", f.PhysicsDelay.Entry.Key);
        Assert.Equal("400", f.PhysicsDelay.Entry.Value);
        Assert.Equal(400, f.PhysicsDelay.Value);
    }

    [RequiresOriginalAssetsFact]
    public void TerrainRepelFlagsMatchTheAudit()
    {
        var t = Fields.Value.Terrains;
        Assert.Equal(16, t.Count);
        Assert.Equal(new[] { 10, 15 }, t.Where(x => x.RepelVehicle.Value == 0).Select(x => x.Terrain).ToArray());
        Assert.All(t, x => Assert.Equal(1, x.RepelWeapons.Value));
        Assert.All(t, x => Assert.InRange(x.RepelVehicle.Value, 0, 1));
        Assert.Equal(261, t[0].RepelVehicle.Entry.LineNumber);
        Assert.Equal(262, t[0].RepelWeapons.Entry.LineNumber);
        Assert.Equal(631, t[10].RepelVehicle.Entry.LineNumber);
        Assert.Equal(632, t[10].RepelWeapons.Entry.LineNumber);
        Assert.Equal(816, t[15].RepelVehicle.Entry.LineNumber);
        Assert.Equal(817, t[15].RepelWeapons.Entry.LineNumber);
        Assert.Equal(248, t[0].SectionLine);
    }
}
