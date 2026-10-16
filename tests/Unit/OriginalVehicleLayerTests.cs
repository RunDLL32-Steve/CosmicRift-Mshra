using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F024 (data only). Oracle: FEATURE_CATALOG.md RT-F024 (Asp f[398..405], f[406..413], sound refs) plus an independent
/// Python audit (scripts/original-assets/audit_rt_f024.py) of the pinned VEH/Rogue.veh (eb1514ea). BLO members are not
/// verified (BLO is Git-LFS only in the pinned clone); only BLO/&lt;archive&gt;.blo filename existence is checked.
/// </summary>
public sealed class OriginalVehicleLayerTests
{
    // ---- synthetic, always run ----

    private static string Line(Func<int, string> field) => string.Join(",", Enumerable.Range(0, OriginalVehicleLayers.RecordFieldCount).Select(field));

    private static string Synthetic(int i) => i switch
    {
        382 => "ArcA", 383 => "mem", 384 => "1", 385 => "2", 386 => "3", 387 => "4", 388 => "-7", 389 => "9",
        442 => "Snd", 443 => "x", 444 => "5", 445 => "6",
        >= 390 and < 438 when (i - 382) % 8 < 2 => "None",
        438 or 439 or 446 or 447 or 450 or 451 => "None",
        _ => "0",
    };

    [Fact]
    public void SyntheticRecordIsReadVerbatim()
    {
        var rec = RawRecordReader.Parse(Line(Synthetic), "synthetic")[0];
        var v = OriginalVehicleLayers.Of(rec);
        Assert.Equal(7, v.Layers.Count);
        Assert.Equal(4, v.Sounds.Count);
        Assert.Equal("ArcA", v.Roll.Archive);
        Assert.Equal("mem", v.Roll.Member);
        Assert.Equal(382, v.Roll.FirstFieldIndex);
        Assert.Equal(389, v.Roll.LastFieldIndex);
        Assert.Equal(new[] { 1, 2, 3, 4, -7, 9 }, v.Roll.Ints.Select(x => x.Value));
        Assert.Equal(new[] { 384, 385, 386, 387, 388, 389 }, v.Roll.Ints.Select(x => x.FieldIndex));
        Assert.True(v.Roll.Ints[4].IsNegativeSentinel);
        Assert.Equal("BLO/ArcA.blo", v.Roll.BloRelativePath);
        Assert.Equal(398, v.DeathExplosion.FirstFieldIndex);
        Assert.Equal(406, v.Thrust.FirstFieldIndex);
        Assert.Equal(new[] { 382, 390, 398, 406, 414, 422, 430 }, v.Layers.Select(l => l.FirstFieldIndex));
        Assert.Equal(new[] { 438, 442, 446, 450 }, v.Sounds.Select(s => s.FirstFieldIndex));
        Assert.Equal(453, v.Sounds[3].LastFieldIndex);
        Assert.Equal("Snd", v.Sounds[1].Archive);
        Assert.Equal(new[] { 5, 6 }, v.Sounds[1].Ints.Select(x => x.Value));
    }

    [Fact]
    public void WrongFieldCountOrNonIntegerThrows()
    {
        var shortRec = RawRecordReader.Parse("1,2,3", "synthetic")[0];
        Assert.Throws<OriginalDataFormatException>(() => OriginalVehicleLayers.Of(shortRec));
        var bad = RawRecordReader.Parse(Line(i => i == 386 ? "abc" : Synthetic(i)), "synthetic")[0];
        var ex = Assert.Throws<OriginalDataFormatException>(() => OriginalVehicleLayers.Of(bad));
        Assert.Equal(386, ex.FieldIndex);
    }

    [Fact]
    public void BloFileCheckIsCaseInsensitiveAndFilenameOnly()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rtf024-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "BLO"));
        try
        {
            File.WriteAllText(Path.Combine(dir, "BLO", "FooBar.blo"), "pointer");
            Assert.True(OriginalBloFiles.ArchiveFileExists(dir, "foobar"));
            Assert.True(OriginalBloFiles.ArchiveFileExists(dir, "FooBar"));
            Assert.False(OriginalBloFiles.ArchiveFileExists(dir, "Other"));
            Assert.False(OriginalBloFiles.ArchiveFileExists(Path.Combine(dir, "nope"), "FooBar"));
        }
        finally { Directory.Delete(dir, true); }
    }

    // ---- oracle (pinned original data) ----

    private sealed record Oracle(string Name, int VehRow, string Roll, string DeathMember, int[] Death, string ThrustMember, int[] Thrust);

    private static readonly Oracle[] Rows =
    [
        new("Asp", 20, "TerranShipsSmall/Asp", "DeathExplosion03_", [0,0,0,0,0,85], "thrust3", [0,0,0,0,-10,51]),
        new("Excelsior", 28, "TerranShipsSmall/Excelsior", "DeathExplosion03_", [0,0,0,0,0,85], "thrust3", [0,0,0,0,-10,0]),
        new("Ravager", 29, "TerranShipsSmall/Ravager", "DeathExplosion03_", [0,0,0,0,0,0], "thrust3", [0,0,5,0,-10,0]),
        new("Hornet", 18, "TerranShipsSmall/hornet", "DeathExplosion03_", [0,0,80,0,0,85], "thrust3", [0,0,0,0,0,0]),
        new("Skithist", 19, "SkithzarShipsSmall/Skithist", "DeathExplosion05_", [0,0,5,0,0,0], "purpletrail", [0,0,25,25,10,0]),
        new("Mastakith", 22, "SkithzarShipsSmall/Mastakith", "DeathExplosion05_", [0,0,95,-5,0,100], "thrust3", [0,0,0,0,0,0]),
        new("Tarthist", 27, "SkithzarShipsSmall/Tarthist", "DeathExplosion05_", [0,0,2,0,0,0], "thrust3", [0,0,0,0,0,0]),
        new("Kragath", 21, "SkithzarShipsSmall/Kragath", "DeathExplosion05_", [0,0,98,0,0,0], "purpletrail", [0,0,25,25,10,0]),
        new("Rak'Kir", 23, "GraziShipsSmall/rakkir", "DeathExplosion04_", [0,0,0,0,0,100], "purpletrail", [0,0,25,25,10,0]),
        new("Rip'Pir", 24, "GraziShipsSmall/rippir", "DeathExplosion04_", [0,0,0,0,0,100], "purpletrail", [0,0,25,25,10,0]),
        new("Zap'Pir", 25, "GraziShipsSmall/Zappir", "DeathExplosion04_", [0,0,0,-5,0,100], "purpletrail", [0,0,25,25,10,0]),
        new("Zag'Gir", 26, "GraziShipsSmall/Zaggir", "DeathExplosion04_", [0,0,0,-5,0,100], "thrust3", [0,0,80,0,0,0]),
    ];

    public static IEnumerable<object[]> HullNames => Rows.Select(r => new object[] { r.Name });

    private static readonly Lazy<OriginalHullCatalog> Catalog = new(() => OriginalHullCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsTheory]
    [MemberData(nameof(HullNames))]
    public void HullLayersAndSoundsMatchTheFile(string name)
    {
        var o = Rows.Single(r => r.Name == name);
        var h = Catalog.Value.ByName(name);
        Assert.Equal(o.VehRow, h.VehRowNumber);
        var v = OriginalVehicleLayers.Of(h);

        // graphic layers: first-field positions, and the roll slot repeats the catalog roll sequence
        Assert.Equal(new[] { 382, 390, 398, 406, 414, 422, 430 }, v.Layers.Select(l => l.FirstFieldIndex));
        Assert.Equal(o.Roll, v.Roll.Archive + "/" + v.Roll.Member);
        // roll tuple ints: only the fifth (HSV Value per FIELD_INTERPRETATION) is non-zero, 5 for Terran and Grazi hulls
        Assert.Equal(v.Roll.Archive == "SkithzarShipsSmall" ? new[] { 0, 0, 0, 0, 0, 0 } : new[] { 0, 0, 0, 0, 5, 0 }, v.Roll.Ints.Select(x => x.Value));
        Assert.All(v.Layers, l => Assert.Equal(6, l.Ints.Count));
        Assert.All(v.Layers, l => Assert.Equal(Enumerable.Range(l.FirstFieldIndex + 2, 6), l.Ints.Select(x => x.FieldIndex)));

        Assert.Equal("ExplosionEffects", v.DeathExplosion.Archive);
        Assert.Equal(o.DeathMember, v.DeathExplosion.Member);
        Assert.Equal(o.Death, v.DeathExplosion.Ints.Select(x => x.Value));
        Assert.Equal("Death explosion", v.DeathExplosion.SlotName);
        Assert.Equal("ExplosionEffects", v.Thrust.Archive);
        Assert.Equal(o.ThrustMember, v.Thrust.Member);
        Assert.Equal(o.Thrust, v.Thrust.Ints.Select(x => x.Value));
        Assert.Equal("Thrust", v.Thrust.SlotName);

        // slots 1, 4, 5, 6 are empty (None,None, all-zero ints) in every hull
        foreach (var s in new[] { 1, 4, 5, 6 })
        {
            Assert.True(v.Layers[s].IsNone);
            Assert.Null(v.Layers[s].BloRelativePath);
            Assert.All(v.Layers[s].Ints, x => Assert.Equal(0, x.Value));
        }

        // sound references: identical for all twelve hulls
        Assert.Equal(new[] { 438, 442, 446, 450 }, v.Sounds.Select(s => s.FirstFieldIndex));
        Assert.True(v.Sounds[0].IsNone);
        Assert.Equal(("UtilitySounds", "Thrust"), (v.Sounds[1].Archive, v.Sounds[1].Member));
        Assert.Equal(("UtilitySounds", "Rev"), (v.Sounds[2].Archive, v.Sounds[2].Member));
        Assert.Equal(("WeaponSounds", "explode2"), (v.Sounds[3].Archive, v.Sounds[3].Member));
        Assert.All(v.Sounds, s => Assert.All(s.Ints, x => Assert.Equal(0, x.Value)));
        Assert.Equal(6, v.NonEmpty.Count()); // roll, death, thrust + three sound references
    }

    [RequiresOriginalAssetsFact]
    public void LayerTextPositionsAreIdenticalInAllTwelveHulls()
    {
        var hulls = Catalog.Value.Hulls;
        Assert.Equal(12, hulls.Count);
        Assert.All(hulls, h => Assert.Equal(OriginalVehicleLayers.RecordFieldCount, h.VehRecord.FieldCount));
        int[] Text(OriginalHull h) => h.VehRecord.Fields.Where(f => f.Index >= 374 && !int.TryParse(f.Value, out _)).Select(f => f.Index).ToArray();
        var expected = new[] { 382, 383, 390, 391, 398, 399, 406, 407, 414, 415, 422, 423, 430, 431, 438, 439, 442, 443, 446, 447, 450, 451 };
        Assert.All(hulls, h => Assert.Equal(expected, Text(h)));
        // f[380], f[381] precede the first layer: raw 8 and 30 in every hull (meaning undecoded)
        Assert.All(hulls, h => { Assert.Equal("8", h.VehRecord.Value(380)); Assert.Equal("30", h.VehRecord.Value(381)); });
        // the only other archive/member-looking text pairs in the record: None,None at f[74..75] and f[86..87]
        Assert.All(hulls, h =>
        {
            Assert.Equal(("None", "None"), (h.VehRecord.Value(74), h.VehRecord.Value(75)));
            Assert.Equal(("None", "None"), (h.VehRecord.Value(86), h.VehRecord.Value(87)));
        });
    }

    [RequiresOriginalAssetsFact]
    public void AspMatchesCatalogTuples()
    {
        var f = Catalog.Value.ByName("Asp").VehRecord;
        Assert.Equal("ExplosionEffects,DeathExplosion03_,0,0,0,0,0,85", string.Join(",", Enumerable.Range(398, 8).Select(f.Value)));
        Assert.Equal("ExplosionEffects,thrust3,0,0,0,0,-10,51", string.Join(",", Enumerable.Range(406, 8).Select(f.Value)));
        Assert.Equal("UtilitySounds,Thrust", f.Value(442) + "," + f.Value(443));
        Assert.Equal("UtilitySounds,Rev", f.Value(446) + "," + f.Value(447));
    }

    [RequiresOriginalAssetsFact]
    public void ReferencedArchiveNamesHaveBloFilenamesInTheClone()
    {
        var archives = Catalog.Value.Hulls.SelectMany(h => OriginalVehicleLayers.Of(h).NonEmpty).Select(r => r.Archive).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "ExplosionEffects", "GraziShipsSmall", "SkithzarShipsSmall", "TerranShipsSmall", "UtilitySounds", "WeaponSounds" }, archives);
        // filename existence only; members inside the archives are NOT verified (BLO content is LFS-only here)
        Assert.All(archives, a => Assert.True(OriginalBloFiles.ArchiveFileExists(OriginalAssets.Root!, a), a));
        Assert.False(OriginalBloFiles.ArchiveFileExists(OriginalAssets.Root!, "NoSuchArchive"));
    }
}
