using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>[Fact] skipped when the original assets or the Git LFS content of LVL/Rogue.lvl are not present.</summary>
public sealed class RequiresOriginalLevelFactAttribute : FactAttribute
{
    public RequiresOriginalLevelFactAttribute()
    {
        if (OriginalAssets.Root == null) { Skip = OriginalAssets.SkipReason; return; }
        var path = Path.Combine(OriginalAssets.Root, "LVL", "Rogue.lvl");
        if (!File.Exists(path) || new FileInfo(path).Length != OriginalLevel.LvlRogue.Length)
            Skip = "LVL/Rogue.lvl is missing or still a Git LFS pointer (run `git lfs pull --include LVL/Rogue.lvl` in the assets checkout). SKIPPED, not passed.";
    }
}

/// <summary>
/// Queue G6 (RT-F021/F022 data only): LVL reader, collision grid and LIO reader. Synthetic fixtures are NEW and only
/// exercise the reader's own byte rules. Canonical oracle values were read from LVL/Rogue.lvl (SHA-256 70344768…cf02f,
/// the same bytes in fowlmouth eb1514e LFS and InfantryOnline/Zone-Assets 401633b) and LIO/Rogue.lio with the independent
/// script scripts/original-assets/audit_g6_level.py. Field meanings are the emulator's (RECONSTRUCTED).
/// </summary>
public sealed class OriginalLevelTests
{
    private static readonly Lazy<OriginalLevel> Rogue = new(() => OriginalLevel.LoadRogue(OriginalAssets.Root!));

    // ---------- synthetic fixtures (NEW) ----------

    /// <summary>Builds a version-6 LVL: 2 × 2 tiles, one object and one floor reference, given plane streams and entity bytes.</summary>
    private static byte[] Build(int version = 6, int width = 2, int height = 2, int entities = 1, byte[][]? planes = null, byte[]? tail = null, int objects = 1, int floors = 1)
    {
        var ms = new MemoryStream();
        var header = new byte[OriginalLevel.HeaderSize];
        void Put(int o, int v) => BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(o), v);
        Put(0, version); Put(4, width); Put(8, height); Put(12, entities); Put(16, floors); Put(20, objects);
        Put(2072, 3); Put(2076, -4);
        Put(2080 + 4 * 0, 0); Put(2080 + 4 * 1, 25); // lookup 1 -> 25, reduced to 9
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(2592), 257);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(2656 + 2), 256);
        ms.Write(header);
        for (var i = 0; i < objects + floors; i++)
        {
            var blob = new byte[64];
            Encoding.ASCII.GetBytes(i < objects ? "o_test.blo" : "f_test.blo").CopyTo(blob, 0);
            Encoding.ASCII.GetBytes($"m{i}.cfs").CopyTo(blob, 32);
            ms.Write(blob);
        }
        planes ??=
        [
            [1, 0x01, 3, 0x80],          // plane0: tile0 lookup 1, tiles1-3 lookup 0 with bit 7 set
            [0, 0, 4, 0x07],             // plane1: escaped 16-bit run of 4
            [2, 0x00, 1, 0x21, 1, 0x05], // plane2: clear, clear, physics 1 vision 1, physics 5
        ];
        foreach (var p in planes) ms.Write(p);
        ms.Write(tail ?? Enumerable.Range(0, 17).Select(i => (byte)i).ToArray());
        return ms.ToArray();
    }

    [Fact]
    public void SyntheticLevelDecodesHeaderBlobsPlanesAndEntities()
    {
        var bytes = Build();
        var level = OriginalLevel.Parse(bytes, "t.lvl");
        Assert.Equal((6, 2, 2, 1, 1, 1), (level.Version, level.Width, level.Height, level.EntityCount, level.FloorCount, level.ObjectCount));
        Assert.Equal((3, -4), (level.OffsetX, level.OffsetY));
        Assert.Equal(25, level.TerrainLookupRaw[1]);
        Assert.Equal((short)257, level.PhysicsLow[0]);
        Assert.Equal((short)256, level.PhysicsHigh[1]);
        Assert.Equal(bytes[..OriginalLevel.HeaderSize], level.HeaderBytes);
        Assert.Equal(("o_test.blo", "m0.cfs"), (level.ObjectBlobs[0].FileName, level.ObjectBlobs[0].MemberId));
        Assert.Equal(("f_test.blo", "m1.cfs"), (level.FloorBlobs[0].FileName, level.FloorBlobs[0].MemberId));
        Assert.Equal(new byte[] { 1, 0x80, 0x80, 0x80 }, level.Plane0);
        Assert.Equal(new byte[] { 7, 7, 7, 7 }, level.Plane1);
        Assert.Equal(new byte[] { 0, 0, 0x21, 5 }, level.Plane2);
        Assert.Equal(new long[] { 5152 + 128, 5152 + 132, 5152 + 136 }, level.PlaneOffsets);
        Assert.Equal(Enumerable.Range(0, 17).Select(i => (byte)i), level.Entities.Single().RawBytes);

        var grid = level.CollisionGrid;
        Assert.Equal(9, grid.TerrainAt(0, 0));
        Assert.Equal(0, grid.TerrainAt(1, 0));
        Assert.False(grid.IsBlocked(1, 0));
        Assert.Equal((1, 1), (grid.PhysicsAt(0, 1), grid.VisionAt(0, 1)));
        Assert.True(grid.IsBlocked(1, 1));
        Assert.Equal(5, grid.PhysicsAt(1, 1));
        Assert.Equal(2, grid.BlockedTileCount());
        Assert.Equal((1, 1), grid.TileAtPixel(31, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.TileAtPixel(32, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.PhysicsAt(-1, 0));

        var again = OriginalLevel.Parse(bytes, "t.lvl");
        Assert.Equal(level.Plane2, again.Plane2);
        Assert.Equal(level.Entities.Single().RawBytes, again.Entities.Single().RawBytes);
    }

    public static IEnumerable<object[]> Malformed()
    {
        var good = Build();
        yield return [good[..100], OriginalLevelFailure.TruncatedHeader];
        yield return [Build(version: 5), OriginalLevelFailure.UnsupportedVersion];
        yield return [Build(version: 7), OriginalLevelFailure.UnsupportedVersion];
        yield return [Build(width: 0), OriginalLevelFailure.InvalidDimensions];
        yield return [Build(width: 5000, height: 5000), OriginalLevelFailure.InvalidDimensions];
        yield return [Build(entities: -1), OriginalLevelFailure.InvalidDimensions];
        yield return [good[..(OriginalLevel.HeaderSize + 70)], OriginalLevelFailure.TruncatedBlobReferences];
        var manyObjects = (byte[])good.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(manyObjects.AsSpan(20), 1000);
        yield return [manyObjects, OriginalLevelFailure.TruncatedBlobReferences];
        yield return [good[..(OriginalLevel.HeaderSize + 128 + 1)], OriginalLevelFailure.TruncatedPlane];
        yield return [Build(planes: [[0, 0, 0, 1], [4, 0], [4, 0]]), OriginalLevelFailure.ZeroLengthRun];
        yield return [Build(planes: [[5, 1], [4, 0], [4, 0]]), OriginalLevelFailure.PlaneOverrun];
        yield return [Build(planes: [[2, 1, 3, 1], [4, 0], [4, 0]]), OriginalLevelFailure.PlaneOverrun];
        yield return [Build(tail: new byte[16]), OriginalLevelFailure.EntityTableSizeMismatch];
        yield return [Build(tail: new byte[18]), OriginalLevelFailure.EntityTableSizeMismatch];
        yield return [Build(entities: 2), OriginalLevelFailure.EntityTableSizeMismatch];
    }

    [Theory]
    [MemberData(nameof(Malformed))]
    public void MalformedLevelsFailExplicitly(byte[] bytes, OriginalLevelFailure expected)
    {
        var ex = Assert.Throws<OriginalLevelFormatException>(() => OriginalLevel.Parse(bytes, "bad.lvl"));
        Assert.Equal(expected, ex.Failure);
        Assert.Equal("bad.lvl", ex.SourceFile);
    }

    [Fact]
    public void SyntheticLioRowsKeepCommonHeaderAndRejectUnknownTypesAndVersions()
    {
        var text = "5,v23,11,\"RefineryA\",10752,624,0,0,0,-3,     1,0,0\r\n4,v23,8,\"Warp\",0,0,250,250,10,-3,  0,500\r\n";
        var rows = OriginalLio.Parse(Encoding.Latin1.GetBytes(text), "t.lio");
        Assert.Equal(2, rows.Count);
        var r = rows[0];
        Assert.Equal((OriginalLioKind.Hide, "v23", 11, "RefineryA", 10752, 624, 0, 0, 0, -3),
            (r.Kind, r.Version, r.Id, r.Name, r.X, r.Y, r.Width, r.Height, r.RelativeId, r.HuntFrequency));
        Assert.Equal(new[] { "1", "0", "0" }, r.TypeFields.Select(f => f.Value));
        Assert.Equal("     1", r.TypeFields[0].RawText);
        Assert.Equal(OriginalLioKind.WarpField, rows[1].Kind);

        Assert.Throws<OriginalDataFormatException>(() => OriginalLio.Parse(Encoding.Latin1.GetBytes("11,v23,1,\"x\",0,0,0,0,0,0\r\n"), "t.lio"));
        Assert.Throws<OriginalDataFormatException>(() => OriginalLio.Parse(Encoding.Latin1.GetBytes("5,v25,1,\"x\",0,0,0,0,0,0\r\n"), "t.lio"));
        Assert.Throws<OriginalDataFormatException>(() => OriginalLio.Parse(Encoding.Latin1.GetBytes("5,v23,1,\"x\",0,0,0,0,0\r\n"), "t.lio"));
        Assert.Throws<OriginalDataFormatException>(() => OriginalLio.Parse(Encoding.Latin1.GetBytes("5,v23,1,\"x\",zero,0,0,0,0,0\r\n"), "t.lio"));
    }

    // ---------- canonical LVL/Rogue.lvl ----------

    [RequiresOriginalLevelFact]
    public void RogueLevelHeaderMatchesThePinnedBytes()
    {
        var l = Rogue.Value;
        Assert.Equal((6, 1999, 1998, 22356, 9, 213), (l.Version, l.Width, l.Height, l.EntityCount, l.FloorCount, l.ObjectCount));
        Assert.Equal((3, 3), (l.OffsetX, l.OffsetY));
        Assert.Equal(new[] { 0, 9, 15, 15, 10, 10, 10, 10, 10 }, l.TerrainLookupRaw.Take(9));
        Assert.All(l.TerrainLookupRaw.Skip(9), v => Assert.Equal(0, v));
        Assert.Equal(new short[] { 257, 0, 0, 0, 0, 0, 257, 257, 257, 257 }, l.PhysicsLow.Take(10));
        Assert.All(l.PhysicsLow.Skip(10), v => Assert.Equal(0, v));
        Assert.Equal(new short[] { 0, 256 }, l.PhysicsHigh.Take(2));
        Assert.All(l.PhysicsHigh.Skip(2), v => Assert.Equal(257, v));
        Assert.All(l.LightColors, v => Assert.Equal(0x01010101u, v));
        Assert.Equal(new uint[] { 2214592512, 2214657080, 2216696856, 2230845440 }, l.MinimapPalette.Take(4));
        Assert.All(l.HeaderBytes.Skip(2736), b => Assert.Equal(0, b));
        Assert.Equal(OriginalLevel.LvlRogue.Length, l.EntityTableOffset + 22356L * 17);
    }

    [RequiresOriginalLevelFact]
    public void RogueLevelBlobReferencesArePinned()
    {
        var l = Rogue.Value;
        Assert.Equal(("O_Animated_Walls.blo", "A000_ani_.cfs"), (l.ObjectBlobs[0].FileName, l.ObjectBlobs[0].MemberId));
        Assert.Equal(("o_mapborder.blo", "cr_edge04.cfs"), (l.ObjectBlobs[^1].FileName, l.ObjectBlobs[^1].MemberId));
        Assert.Equal(("f_default.blo", "default.cfs"), (l.FloorBlobs[0].FileName, l.FloorBlobs[0].MemberId));
        Assert.Equal(("f_colors.blo", "color9.cfs"), (l.FloorBlobs[^1].FileName, l.FloorBlobs[^1].MemberId));
        var byFile = l.ObjectBlobs.GroupBy(b => b.FileName).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(new Dictionary<string, int>
        {
            ["O_Neutral_Walls.blo"] = 81, ["O_Animated_Walls.blo"] = 58, ["O_SpaceStation.blo"] = 49, ["O_Asteroids.blo"] = 9,
            ["O_GasClouds.blo"] = 5, ["O_Specialtiles.blo"] = 5, ["o_mapborder.blo"] = 5, ["o_crmisc.blo"] = 1,
        }, byFile);
        Assert.Equal(new[] { "f_colors.blo", "f_default.blo" }, l.FloorBlobs.Select(b => b.FileName).Distinct().Order(StringComparer.Ordinal));
    }

    [RequiresOriginalLevelFact]
    public void RoguePlanesAndEntitiesDecodeToPinnedHashes()
    {
        var l = Rogue.Value;
        Assert.Equal(new long[] { 19360, 258404, 683624 }, l.PlaneOffsets);
        Assert.Equal(910942, l.EntityTableOffset);
        Assert.All(new[] { l.Plane0, l.Plane1, l.Plane2 }, p => Assert.Equal(1999 * 1998, p.Length));
        Assert.Equal("d0b5f509a520ed121447930fc62c2a6357acfe1b543dda20020b848d7aaea6bd", Sha(l.Plane0));
        Assert.Equal("0b61c6af4d53fdd89e736481fe630cb46c8b319e8bdc89c20145722147ef0fba", Sha(l.Plane1));
        Assert.Equal("0d0ba28e3cca5593f8f511b7f36fdd3a014bbbba810fc03256b10be7dfbc3699", Sha(l.Plane2));
        Assert.Equal("068a21dc11aac76420a48bb7cafe678c019e6b4c1ddcfe266a81aa60e59b1e90", Sha(l.Entities.SelectMany(e => e.RawBytes).ToArray()));
        Assert.Equal("00000000d0006009000000100000000000", Convert.ToHexString(l.Entities[0].RawBytes).ToLowerInvariant());
        Assert.Equal("d07ad07bd3004006000000100000008000", Convert.ToHexString(l.Entities[^1].RawBytes).ToLowerInvariant());
    }

    [RequiresOriginalLevelFact]
    public void RogueCollisionGridCountsMatchTheIndependentDecode()
    {
        var l = Rogue.Value;
        var g = l.CollisionGrid;
        Assert.Equal((31984, 31968), (g.PixelWidth, g.PixelHeight));
        Assert.Equal(116065, g.BlockedTileCount());

        var physics = new int[32];
        var vision = new int[8];
        var terrain = new int[16];
        for (var y = 0; y < g.Height; y++)
            for (var x = 0; x < g.Width; x++)
            {
                physics[g.PhysicsAt(x, y)]++;
                vision[g.VisionAt(x, y)]++;
                terrain[g.TerrainAt(x, y)]++;
            }
        Assert.Equal(new[] { 3877937, 86796, 7305, 7326, 7311, 7327 }, physics.Take(6));
        Assert.All(physics.Skip(6), c => Assert.Equal(0, c));
        Assert.Equal(new[] { 3898024, 95978 }, vision.Take(2));
        Assert.All(vision.Skip(2), c => Assert.Equal(0, c));
        Assert.Equal(new Dictionary<int, int> { [0] = 3976963, [9] = 2600, [10] = 10343, [15] = 4096 },
            terrain.Select((c, i) => (c, i)).Where(t => t.c > 0).ToDictionary(t => t.i, t => t.c));
    }

    [RequiresOriginalLevelFact]
    public void RogueLioStationHidesLandOnTheirLevelTerrain()
    {
        // Cross-check of two independent files under the emulator's pixel reading: refinery/trading post hides sit on
        // terrain 15 "Refinery Zone/Trade Zone" and repair dock hides on terrain 9 "Repair Dock" (rogue.cfg names).
        var g = Rogue.Value.CollisionGrid;
        var lio = OriginalLio.LoadRogue(OriginalAssets.Root!);
        var expected = new Dictionary<string, int>
        {
            ["RefineryA"] = 15, ["RefineryB"] = 15, ["TradingPostA"] = 15, ["TradingPostB"] = 15,
            ["RepairDockA"] = 9, ["RepairDockB"] = 9, ["RepairDockC"] = 9, ["RepairDockD"] = 9, ["RepairDockE"] = 9,
        };
        foreach (var (name, terrain) in expected)
        {
            var row = lio.Single(o => o.Name == name);
            Assert.Equal(OriginalLioKind.Hide, row.Kind);
            var (tx, ty) = g.TileAtPixel(row.X, row.Y);
            Assert.Equal(terrain, g.TerrainAt(tx, ty));
            Assert.False(g.IsBlocked(tx, ty));
        }
    }

    // ---------- canonical LIO/Rogue.lio ----------

    [RequiresOriginalAssetsFact]
    public void RogueLioHasEightyOneV23RowsByKind()
    {
        var lio = OriginalLio.LoadRogue(OriginalAssets.Root!);
        Assert.Equal(81, lio.Count);
        Assert.All(lio, o => Assert.Equal("v23", o.Version));
        Assert.Equal(lio.Count, lio.Select(o => o.Id).Distinct().Count());
        var byKind = lio.GroupBy(o => o.Kind).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(new Dictionary<OriginalLioKind, int>
        {
            [OriginalLioKind.WarpField] = 13, [OriginalLioKind.Hide] = 42, [OriginalLioKind.Sound] = 15,
            [OriginalLioKind.Text] = 6, [OriginalLioKind.Parallax] = 5,
        }, byKind);
        Assert.All(lio, o => Assert.Equal(-3, o.HuntFrequency));

        var first = lio[0];
        Assert.Equal((1, OriginalLioKind.Parallax, 2, "Planets - Jupiter", 7824, 5504), (first.Row, first.Kind, first.Id, first.Name, first.X, first.Y));
        var warp = lio.Single(o => o.Id == 8);
        Assert.Equal(("HyperBuoyWarp", 0, 0, 250, 250, 10), (warp.Name, warp.X, warp.Y, warp.Width, warp.Height, warp.RelativeId));
        var mold = lio.Single(o => o.Name == "MoldSporeSpawn");
        Assert.Equal((OriginalLioKind.Hide, 19, 500, 500, 20), (mold.Kind, mold.Id, mold.Width, mold.Height, mold.RelativeId));
    }

    private static string Sha(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
