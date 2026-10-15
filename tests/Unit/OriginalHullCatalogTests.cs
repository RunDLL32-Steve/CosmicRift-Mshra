using System.Text.Json;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: the values written in docs/original-research/FEATURE_CATALOG.md rows RT-F001..RT-F012, copied from that text.
/// Tests needing the canonical assets are reported as Skipped (never silently passed) when they are not found.
/// </summary>
public sealed class OriginalHullCatalogTests
{
    private sealed record Oracle(
        string Id, string Name, int RpgRow, int SkillId, int VehicleId, string Category, int VehRow,
        int Hull, int NormalWeight, int StopWeight, int[] Speed, string Roll);

    private static readonly Oracle[] Rows =
    [
        new("RT-F001", "Asp", 1, 2, 2002, "Ship Hull - Terran", 20, 50, 1500000, 2500000, [0,0,45,1380,3250,22,3500,50,-1,-1], "TerranShipsSmall/Asp"),
        new("RT-F002", "Excelsior", 2, 15, 2011, "Ship Hull - Terran", 28, 50, 1500000, 3000000, [0,0,45,1380,3575,22,3800,50,-1,-1], "TerranShipsSmall/Excelsior"),
        new("RT-F003", "Ravager", 3, 16, 2012, "Ship Hull - Terran", 29, 50, 1500000, 4000000, [0,0,42,1380,3250,22,3500,50,-1,-1], "TerranShipsSmall/Ravager"),
        new("RT-F004", "Hornet", 4, 8, 2000, "Ship Hull - Terran", 18, 50, 1500000, 2500000, [0,0,50,1656,3250,22,3500,55,-1,-1], "TerranShipsSmall/hornet"),
        new("RT-F005", "Skithist", 5, 1, 2001, "Ship Hull - Skithzar", 19, 50, 1500000, 3000000, [0,0,45,1380,3350,22,3600,50,-1,-1], "SkithzarShipsSmall/Skithist"),
        new("RT-F006", "Mastakith", 6, 4, 2004, "Ship Hull - Skithzar", 22, 50, 1500000, 2500000, [0,0,45,1794,3250,22,3750,60,-1,-1], "SkithzarShipsSmall/Mastakith"),
        new("RT-F007", "Tarthist", 7, 11, 2010, "Ship Hull - Skithzar", 27, 50, 1500000, 2500000, [0,0,45,1380,3250,22,3500,50,-1,-1], "SkithzarShipsSmall/Tarthist"),
        new("RT-F008", "Kragath", 8, 3, 2003, "Ship Hull - Skithzar", 21, 50, 1500000, 4000000, [0,0,42,1380,3250,22,3500,50,-1,-1], "SkithzarShipsSmall/Kragath"),
        new("RT-F009", "Rak'Kir", 9, 5, 2005, "Ship Hull - Grazi", 23, 50, 1500000, 2500000, [0,0,50,1600,3250,22,3500,55,-1,-1], "GraziShipsSmall/rakkir"),
        new("RT-F010", "Rip'Pir", 10, 6, 2006, "Ship Hull - Grazi", 24, 50, 1500000, 2500000, [0,0,45,1380,3300,22,3600,50,-1,-1], "GraziShipsSmall/rippir"),
        new("RT-F011", "Zap'Pir", 11, 9, 2008, "Ship Hull - Grazi", 25, 50, 1500000, 4000000, [0,0,42,1380,3250,22,3500,50,-1,-1], "GraziShipsSmall/Zappir"),
        new("RT-F012", "Zag'Gir", 12, 10, 2009, "Ship Hull - Grazi", 26, 50, 1500000, 3000000, [0,0,45,1380,3350,22,3500,50,-1,-1], "GraziShipsSmall/Zaggir"),
    ];

    public static IEnumerable<object[]> OracleIds => Rows.Select(r => new object[] { r.Id });

    private static readonly Lazy<OriginalHullCatalog> Catalog = new(() => OriginalHullCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void ExactlyTwelveHullsInRpgOrder()
    {
        Assert.Equal(12, Catalog.Value.Hulls.Count);
        Assert.Equal(Rows.Select(r => r.Name), Catalog.Value.Hulls.Select(h => h.Name));
    }

    [RequiresOriginalAssetsTheory]
    [MemberData(nameof(OracleIds))]
    public void HullMatchesCatalogRow(string id)
    {
        var o = Rows.Single(r => r.Id == id);
        var h = Catalog.Value.ByName(o.Name);

        Assert.Equal(o.SkillId, h.SkillId);
        Assert.Equal(o.VehicleId, h.VehicleId);
        Assert.Equal(o.Category, h.Category);
        Assert.Equal(o.RpgRow, h.RpgRowNumber);
        Assert.Equal(o.VehRow, h.VehRowNumber);
        Assert.Equal("RPG/Rogue.rpg", h.RpgRecord.SourceFile);
        Assert.Equal("VEH/Rogue.veh", h.VehRecord.SourceFile);
        Assert.Equal("v6", h.RpgRecord.VersionToken);
        Assert.Equal("v53", h.VehRecord.VersionToken);

        // VEH identity: f[2] equals the RPG vehicle id, f[0] is the Car type, f[3] the same name.
        Assert.Equal(o.VehicleId.ToString(), h.VehRecord.Value(2));
        Assert.Equal("2", h.VehRecord.Value(0));
        Assert.Equal(o.Name, h.VehRecord.Value(3));

        Assert.Equal(new OriginalInt(6, o.Hull, false), h.HullPoints);
        Assert.Equal(new OriginalInt(47, o.NormalWeight, false), h.NormalWeight);
        Assert.Equal(new OriginalInt(48, o.StopWeight, false), h.StopWeight);

        Assert.Equal(10, h.Terrain0.Values.Count);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(202 + i, h.Terrain0.Values[i].FieldIndex);
            Assert.Equal(o.Speed[i], h.Terrain0.Values[i].Value);
            Assert.Equal(o.Speed[i] < 0, h.Terrain0.Values[i].IsNegativeSentinel);
        }

        Assert.Equal(o.Roll, h.VehRecord.Value(382) + "/" + h.VehRecord.Value(383));
    }

    [RequiresOriginalAssetsFact]
    public void AspMatchesFieldInterpretationRawValues()
    {
        var asp = Catalog.Value.ByName("Asp");

        // RPG row 1: first six raw fields "v6,2,1,2002,1,0".
        Assert.Equal(["v6", "2", "1", "2002", "1", "0"], asp.RpgRecord.Fields.Take(6).Select(f => f.Value));
        Assert.Equal(1, asp.RpgRowNumber);
        Assert.Equal(20, asp.VehRowNumber);

        // HP50, weight10000, NormalWeight1500000, StopWeight2500000, EnergyMax1700, EnergyRate1050, EnergyMin1700.
        Assert.Equal(50, asp.HullPoints.Value);
        Assert.Equal(new OriginalInt(7, 10000, false), asp.Weight);
        Assert.Equal(1500000, asp.NormalWeight.Value);
        Assert.Equal(2500000, asp.StopWeight.Value);
        Assert.Equal(new OriginalInt(94, 1700, false), asp.EnergyMax);
        Assert.Equal(new OriginalInt(95, 1050, false), asp.EnergyRate);
        Assert.Equal(new OriginalInt(96, 1700, false), asp.EnergyMin);

        // f[202..211] named values, f[362] BouncePercent 727, f[373] HyperEnergyCost 1100.
        var t = asp.Terrain0;
        Assert.Equal(0, t.RollAllowed.Value);
        Assert.Equal(0, t.RollFriction.Value);
        Assert.Equal(45, t.RollThrust.Value);
        Assert.Equal(1380, t.RollRotate.Value);
        Assert.Equal(3250, t.RollTopSpeed.Value);
        Assert.Equal(22, t.StrafeThrust.Value);
        Assert.Equal(3500, t.HyperTopSpeed.Value);
        Assert.Equal(50, t.HyperThrust.Value);
        Assert.Equal(new OriginalInt(210, -1, true), t.BackwardThrust);
        Assert.Equal(new OriginalInt(211, -1, true), t.BackwardHyperThrust);
        Assert.Equal(new OriginalInt(362, 727, false), asp.BouncePercent);
        Assert.Equal(new OriginalInt(373, 1100, false), asp.HyperEnergyCost);

        // f[382..389] = TerranShipsSmall,Asp,0,0,0,0,5,0
        Assert.Equal(["TerranShipsSmall", "Asp", "0", "0", "0", "0", "5", "0"],
            Enumerable.Range(382, 8).Select(i => asp.VehRecord.Value(i)));
    }

    [RequiresOriginalAssetsFact]
    public void SentinelsAreReturnedAsIsAndFlagged()
    {
        foreach (var hull in Catalog.Value.Hulls)
        {
            Assert.Equal(-1, hull.Terrain0.BackwardThrust.Value);
            Assert.True(hull.Terrain0.BackwardThrust.IsNegativeSentinel);
            Assert.Equal(-1, hull.Terrain0.BackwardHyperThrust.Value);
            Assert.True(hull.Terrain0.BackwardHyperThrust.IsNegativeSentinel);
            Assert.False(hull.Terrain0.RollThrust.IsNegativeSentinel);
        }
        // A raw -1 in another block of the same record: Asp terrain 1 (f[212..221]) ends "...,0,0,-1,-1".
        var asp = Catalog.Value.ByName("Asp");
        Assert.Equal(new OriginalInt(220, -1, true), asp.TerrainSpeedBlocks[1].BackwardThrust);
        Assert.Equal(new OriginalInt(221, -1, true), asp.TerrainSpeedBlocks[1].BackwardHyperThrust);
        Assert.Equal(new OriginalInt(214, 0, false), asp.TerrainSpeedBlocks[1].RollThrust);
    }

    [RequiresOriginalAssetsFact]
    public void AllSixteenTerrainBlocksAreAvailableAsRawData()
    {
        foreach (var hull in Catalog.Value.Hulls)
        {
            Assert.Equal(16, hull.TerrainSpeedBlocks.Count);
            for (var t = 0; t < 16; t++)
            {
                var block = hull.TerrainSpeedBlocks[t];
                Assert.Equal(t, block.TerrainIndex);
                Assert.Equal(202 + 10 * t, block.FirstFieldIndex);
                Assert.Equal(10, block.Values.Count);
                for (var i = 0; i < 10; i++)
                    Assert.Equal(hull.VehRecord.Int(202 + 10 * t + i), block.Values[i]);
            }
            // The blocks end right before BouncePercent.
            Assert.Equal(362, hull.TerrainSpeedBlocks[15].FirstFieldIndex + 10);
            Assert.Equal(362, hull.BouncePercent.FieldIndex);
        }
    }

    [RequiresOriginalAssetsFact]
    public void EveryRawFieldIsRetained()
    {
        foreach (var hull in Catalog.Value.Hulls)
        {
            Assert.Equal(51, hull.RpgRecord.FieldCount);
            Assert.True(hull.VehRecord.FieldCount > 373);
            for (var i = 0; i < hull.VehRecord.FieldCount; i++)
                Assert.Equal(i, hull.VehRecord.Fields[i].Index);
        }
    }

    [RequiresOriginalAssetsFact]
    public void LoadingLeavesOriginalFilesUntouched()
    {
        string Digest() => string.Join("|", PinnedAssets.All.Select(a =>
        {
            var path = Path.Combine(OriginalAssets.Root!, a.RelativePath);
            return AssetIntegrity.Sha256Hex(File.ReadAllBytes(path)) + ":" + new FileInfo(path).Length + ":" + File.GetLastWriteTimeUtc(path).Ticks;
        }));

        var before = Digest();
        var catalog = OriginalHullCatalog.Load(OriginalAssets.Root!);
        Assert.Equal(12, catalog.Hulls.Count);
        Assert.Equal(before, Digest());
        foreach (var a in PinnedAssets.All)
            Assert.Equal(a.Sha256, AssetIntegrity.Sha256Hex(File.ReadAllBytes(Path.Combine(OriginalAssets.Root!, a.RelativePath))));
    }

    [Fact]
    public void PinnedHashesMatchAssetManifest()
    {
        var root = OriginalAssets.RepoRoot();
        Assert.NotNull(root);
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!, "docs", "original-research", "ASSET_MANIFEST.json")));
        Assert.Equal(PinnedAssets.Commit, doc.RootElement.GetProperty("canonicalSource").GetProperty("commit").GetString());
        foreach (var pinned in PinnedAssets.All)
        {
            var entry = doc.RootElement.GetProperty("assets").EnumerateArray()
                .Single(a => a.GetProperty("originalPath").GetString() == pinned.RelativePath);
            Assert.Equal(pinned.Sha256, entry.GetProperty("originalSha256").GetString());
            Assert.Equal(pinned.Length, entry.GetProperty("originalSizeBytes").GetInt64());
        }
    }

    // ---- Linking rules on inline fixtures (no assets needed) ----

    private static string RpgRow(string name, int skill, int vehicle, string type = "1") =>
        $"v6,{skill},{type},{vehicle},1," + string.Join(",", Enumerable.Repeat("0", 33)) + $",3499,\"{name}\",\"Ship Hull - Test\",\"\",\"desc\",A,B,0,0,0,0,0,550\r\n";

    private static string VehRow(string name, int id, string type = "2", string version = "v53")
    {
        var fields = Enumerable.Repeat("0", 400).ToArray();
        fields[0] = type; fields[1] = version; fields[2] = id.ToString(); fields[3] = $"\"{name}\""; fields[4] = "\"\""; fields[5] = "\"d\"";
        return string.Join(",", fields) + "\r\n";
    }

    private static OriginalHullCatalog Link(string rpg, string veh) => OriginalHullCatalog.FromRecords(
        RawRecordReader.Parse(rpg, "RPG/test.rpg"), RawRecordReader.Parse(veh, "VEH/test.veh"));

    [Fact]
    public void LinkMatchesVehicleIdToVehF2()
    {
        var catalog = Link(RpgRow("Alpha", 7, 2100), VehRow("Other", 2099) + VehRow("Alpha", 2100));
        var hull = Assert.Single(catalog.Hulls);
        Assert.Equal(2, hull.VehRowNumber);
        Assert.Equal(1, hull.RpgRowNumber);
        Assert.Equal(7, hull.SkillId);
    }

    [Fact]
    public void LinkRejectsMissingDuplicateWrongTypeAndNameMismatch()
    {
        Assert.Throws<OriginalHullLinkException>(() => Link(RpgRow("Alpha", 7, 2100), VehRow("Alpha", 2101)));
        Assert.Throws<OriginalHullLinkException>(() => Link(RpgRow("Alpha", 7, 2100), VehRow("Alpha", 2100) + VehRow("Alpha", 2100)));
        Assert.Throws<OriginalHullLinkException>(() => Link(RpgRow("Alpha", 7, 2100), VehRow("Alpha", 2100, type: "5")));
        Assert.Throws<OriginalHullLinkException>(() => Link(RpgRow("Alpha", 7, 2100), VehRow("Alpha", 2100, version: "v52")));
        Assert.Throws<OriginalHullLinkException>(() => Link(RpgRow("Alpha", 7, 2100), VehRow("Beta", 2100)));
        Assert.Throws<OriginalHullLinkException>(() => Link(RpgRow("Alpha", 7, 2100) + RpgRow("Beta", 7, 2101), VehRow("Alpha", 2100) + VehRow("Beta", 2101)));
    }

    [Fact]
    public void NonHullRpgRowsAreIgnoredButOtherVersionsAreRejected()
    {
        var catalog = Link(RpgRow("Alpha", 7, 2100) + RpgRow("Skill", 8, 1, type: "3"), VehRow("Alpha", 2100));
        Assert.Single(catalog.Hulls);
        Assert.Throws<OriginalHullLinkException>(() => Link(RpgRow("Alpha", 7, 2100).Replace("v6,", "v7,"), VehRow("Alpha", 2100)));
    }
}
