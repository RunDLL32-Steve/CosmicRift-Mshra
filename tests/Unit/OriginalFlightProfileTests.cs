using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: the values printed by `python scripts/audit-flight-fields.py` (independent csv reader) against the pinned
/// VEH/Rogue.veh. Raw, uninterpreted data only (RT-F027/RT-F028 data clauses); native flight meaning stays unverified.
/// Skipped, never silently passed, when the canonical assets are not found.
/// </summary>
public sealed class OriginalFlightProfileTests
{
    // name, terrain-0 block f[202..211], f[373]
    private static readonly (string Name, int[] T0, int HyperEnergyCost)[] Hulls =
    [
        ("Asp", [0,0,45,1380,3250,22,3500,50,-1,-1], 1100),
        ("Excelsior", [0,0,45,1380,3575,22,3800,50,-1,-1], 1100),
        ("Ravager", [0,0,42,1380,3250,22,3500,50,-1,-1], 1100),
        ("Hornet", [0,0,50,1656,3250,22,3500,55,-1,-1], 1100),
        ("Skithist", [0,0,45,1380,3350,22,3600,50,-1,-1], 990),
        ("Mastakith", [0,0,45,1794,3250,22,3750,60,-1,-1], 1200),
        ("Tarthist", [0,0,45,1380,3250,22,3500,50,-1,-1], 990),
        ("Kragath", [0,0,42,1380,3250,22,3500,50,-1,-1], 990),
        ("Rak'Kir", [0,0,50,1600,3250,22,3500,55,-1,-1], 1100),
        ("Rip'Pir", [0,0,45,1380,3300,22,3600,50,-1,-1], 1100),
        ("Zap'Pir", [0,0,42,1380,3250,22,3500,50,-1,-1], 1100),
        ("Zag'Gir", [0,0,45,1380,3350,22,3500,50,-1,-1], 1100),
    ];

    public static IEnumerable<object[]> HullNames => Hulls.Select(h => new object[] { h.Name });

    private static readonly Lazy<OriginalHullCatalog> Catalog = new(() => OriginalHullCatalog.Load(OriginalAssets.Root!));

    private static int[] Values(TerrainSpeedBlock b) => b.Values.Select(v => v.Value).ToArray();

    [RequiresOriginalAssetsFact]
    public void TwelveHullsCovered()
    {
        Assert.Equal(12, Catalog.Value.Hulls.Count);
        Assert.Equal(12, Hulls.Length);
        foreach (var (name, _, _) in Hulls) Assert.Equal(name, Catalog.Value.ByName(name).Name);
    }

    [RequiresOriginalAssetsTheory]
    [MemberData(nameof(HullNames))]
    public void ProfileMatchesAudit(string name)
    {
        var expected = Hulls.Single(h => h.Name == name);
        var hull = Catalog.Value.ByName(name);
        var profile = OriginalFlightProfile.From(hull);

        // Terrain 0 full block (ten raw values, source indexes 202..211).
        Assert.Equal(expected.T0, Values(hull.Terrain0));

        // Every block ends in the -1 reverse pair, so every terrain is non-zero; the "active" terrains (positions 0..7) are 0, 9, 10, 15.
        Assert.Equal(Enumerable.Range(0, 16).ToArray(), profile.NonZeroTerrainIndexes.ToArray());
        Assert.Equal(new[] { 0, 9, 10, 15 }, profile.ActiveTerrainIndexes.ToArray());

        // Terrains 9 and 10 are identical for all hulls; terrain 15 differs only for Hornet; the rest are 0*8,-1,-1.
        var t9 = new[] { 1, 45, 45, 1000, 3000, 15, 0, 0, -1, -1 };
        var t15 = name == "Hornet" ? new[] { 1, 30, 50, 1656, 3000, 15, 0, 0, -1, -1 } : new[] { 1, 30, 45, 1300, 3000, 15, 0, 0, -1, -1 };
        var empty = new[] { 0, 0, 0, 0, 0, 0, 0, 0, -1, -1 };
        for (var t = 0; t < 16; t++)
        {
            var want = t == 0 ? expected.T0 : t == 9 || t == 10 ? t9 : t == 15 ? t15 : empty;
            Assert.Equal(want, Values(hull.TerrainSpeedBlocks[t]));
        }

        // Sentinels: exactly the 32 reverse fields (positions 8 and 9 of each block), all -1; none in f[373].
        Assert.Equal(32, profile.Sentinels.Count);
        for (var t = 0; t < 16; t++)
        {
            Assert.Equal(new OriginalInt(210 + 10 * t, -1, true), profile.Sentinels[2 * t]);
            Assert.Equal(new OriginalInt(211 + 10 * t, -1, true), profile.Sentinels[2 * t + 1]);
        }
        Assert.Equal(new OriginalInt(373, expected.HyperEnergyCost, false), profile.HyperEnergyCost);
        Assert.False(profile.HyperEnergyCost.IsNegativeSentinel);
    }

    [RequiresOriginalAssetsTheory]
    [MemberData(nameof(HullNames))]
    public void StrafeHyperReverseGroupIsRawPerTerrain(string name)
    {
        var expected = Hulls.Single(h => h.Name == name);
        var profile = OriginalFlightProfile.From(Catalog.Value.ByName(name));
        Assert.Equal(16, profile.StrafeHyperReverse.Count);

        var g0 = profile.StrafeHyperReverse[0];
        Assert.Equal(0, g0.TerrainIndex);
        Assert.Equal(new OriginalInt(207, 22, false), g0.StrafeThrust);
        Assert.Equal(new OriginalInt(208, expected.T0[6], false), g0.HyperTopSpeed);
        Assert.Equal(new OriginalInt(209, expected.T0[7], false), g0.HyperThrust);
        Assert.Equal(new OriginalInt(210, -1, true), g0.BackwardThrust);
        Assert.Equal(new OriginalInt(211, -1, true), g0.BackwardHyperThrust);

        // Strafe is 15 in terrains 9, 10 and 15 and 0 in the other terrains; hyper fields are 0 outside terrain 0.
        foreach (var g in profile.StrafeHyperReverse.Skip(1))
        {
            Assert.Equal(g.TerrainIndex is 9 or 10 or 15 ? 15 : 0, g.StrafeThrust.Value);
            Assert.Equal(207 + 10 * g.TerrainIndex, g.StrafeThrust.FieldIndex);
            Assert.Equal(0, g.HyperTopSpeed.Value);
            Assert.Equal(0, g.HyperThrust.Value);
            Assert.Equal(-1, g.BackwardThrust.Value);
            Assert.Equal(-1, g.BackwardHyperThrust.Value);
        }
    }

    [RequiresOriginalAssetsFact]
    public void HyperEnergyCostDiffersByHull()
    {
        var costs = Catalog.Value.Hulls.ToDictionary(h => h.Name, h => OriginalFlightProfile.From(h).HyperEnergyCost.Value);
        Assert.Equal(990, costs["Skithist"]);
        Assert.Equal(990, costs["Tarthist"]);
        Assert.Equal(990, costs["Kragath"]);
        Assert.Equal(1200, costs["Mastakith"]);
        Assert.Equal(8, costs.Values.Count(c => c == 1100));
    }
}
