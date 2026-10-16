using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Queue G7: raw loader of all 30 rows of VEH/Rogue.veh. The oracle table was produced by the independent
/// scripts/original-assets/audit_rt_f055_g7.py against the pinned file (eb1514ea). Oracle tests are Skipped, never passed,
/// without the assets; the synthetic tests always run.
/// </summary>
public sealed class OriginalVehicleCatalogTests
{
    private sealed record Row(int RowNumber, int Type, int Id, string Name, int Fields);

    private static readonly Row[] Pinned =
    [
        new(1, 5, 400, "Refinery", 477), new(2, 5, 401, "Null Death Orb", 477), new(3, 5, 402, "MeteorShower", 477),
        new(4, 5, 403, "PirateStation", 477), new(5, 5, 404, "Trading Outpost", 477), new(6, 5, 405, "Null Kill Station", 477),
        new(7, 5, 407, "Repair Dock", 477), new(8, 5, 408, "Micro Repair Dock", 477), new(9, 5, 409, "Solar Mold", 477),
        new(10, 5, 410, "GunSat", 477), new(11, 5, 411, "BeamSat", 477), new(12, 5, 412, "Micro Refinery", 477),
        new(13, 5, 413, "Rogue Trader Outpost", 477), new(14, 5, 414, "Mold Spore", 477),
        new(15, 5, 425, "Manufactory - Missile", 477), new(16, 5, 426, "Manufactory - Munitions", 477), new(17, 5, 427, "Manufactory - Defense", 477),
        new(18, 2, 2000, "Hornet", 454), new(19, 2, 2001, "Skithist", 454), new(20, 2, 2002, "Asp", 454),
        new(21, 2, 2003, "Kragath", 454), new(22, 2, 2004, "Mastakith", 454), new(23, 2, 2005, "Rak'Kir", 454),
        new(24, 2, 2006, "Rip'Pir", 454), new(25, 2, 2008, "Zap'Pir", 454), new(26, 2, 2009, "Zag'Gir", 454),
        new(27, 2, 2010, "Tarthist", 454), new(28, 2, 2011, "Excelsior", 454), new(29, 2, 2012, "Ravager", 454),
        new(30, 4, 2030, "Spectator Mode", 202),
    ];

    private static readonly Lazy<OriginalVehicleCatalog> Catalog = new(() => OriginalVehicleCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void AllThirtyRowsArePinnedByRowTypeIdNameAndFieldCount()
    {
        var all = Catalog.Value.All;
        Assert.Equal(30, all.Count);
        for (var i = 0; i < Pinned.Length; i++)
        {
            var p = Pinned[i];
            var v = all[i];
            Assert.Equal((p.RowNumber, p.Type, p.Id, p.Name, p.Fields), (v.RowNumber, v.RecordType, v.Id, v.Name, v.FieldCount));
            Assert.Equal("v53", v.VersionToken);
        }
    }

    [RequiresOriginalAssetsFact]
    public void EighteenNonHullRowsAreSeventeenComputersAndSpectator()
    {
        var c = Catalog.Value;
        Assert.Equal(18, c.NonHullRows.Count());
        Assert.Equal(17, c.Computers.Count());
        Assert.Equal(Enumerable.Range(1, 17), c.Computers.Select(v => v.RowNumber));
        Assert.Equal(12, c.CarRows.Count());
        var spectator = c.ById(2030);
        Assert.True(spectator.IsSpectator);
        Assert.Equal("Spectator Mode", spectator.Name);
        Assert.Equal(spectator, c.ByName("Spectator Mode"));
        Assert.Equal(413, c.ByName("Rogue Trader Outpost").Id);
        // A naive comma split gives 478 pieces for Solar Mold: its quoted description f[5] contains a comma. The quote-aware
        // reader counts 477 fields like the other sixteen computer rows.
        var mold = c.ById(409);
        Assert.Equal(477, mold.FieldCount);
        Assert.True(mold.Field(5).WasQuoted);
        Assert.Contains(',', mold.Field(5).Value);
        Assert.Equal(478, mold.Record.RawLine.Split(',').Length);
        Assert.All(c.Computers, v => Assert.Equal(477, v.FieldCount));
    }

    [RequiresOriginalAssetsFact]
    public void TheTwelveCarRowsAreExactlyTheHullCatalogRows()
    {
        var hulls = OriginalHullCatalog.Load(OriginalAssets.Root!).Hulls;
        var cars = Catalog.Value.CarRows.ToList();
        Assert.Equal(12, hulls.Count);
        Assert.Equal(hulls.Select(h => h.VehRowNumber).OrderBy(x => x), cars.Select(v => v.RowNumber));
        Assert.Equal(hulls.Select(h => h.VehicleId).OrderBy(x => x), cars.Select(v => v.Id));
        foreach (var h in hulls)
        {
            var v = Catalog.Value.ById(h.VehicleId);
            Assert.True(v.IsCar);
            Assert.Equal(h.Name, v.Name);
            Assert.Equal(h.VehRowNumber, v.RowNumber);
            Assert.Equal(h.VehRecord.RawLine, v.Record.RawLine);
        }
    }

    [RequiresOriginalAssetsFact]
    public void ModifiedBytesAreRejectedByTheIntegrityGuard()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rt-veh-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "VEH"));
        try
        {
            var bytes = File.ReadAllBytes(Path.Combine(OriginalAssets.Root!, "VEH", "Rogue.veh"));
            bytes[100] ^= 1;
            File.WriteAllBytes(Path.Combine(dir, "VEH", "Rogue.veh"), bytes);
            Assert.Throws<AssetIntegrityException>(() => OriginalVehicleCatalog.Load(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    private static IReadOnlyList<RawRecord> Parse(string text) => RawRecordReader.Parse(text, "VEH/test.veh");

    [Fact]
    public void SyntheticRowsKeepRawFieldsAndClassifyByRecordType()
    {
        var c = OriginalVehicleCatalog.FromRecords(Parse(
            "5,v53,900,\"Comp A\",,x,1,2\r\n\r\n2,v53,901,\"Hull B\",z\r\n4,v53,902,\"Spec\"\r\n"));
        Assert.Equal(new[] { 1, 3, 4 }, c.All.Select(v => v.RowNumber));
        Assert.Equal(new[] { 900 }, c.Computers.Select(v => v.Id));
        Assert.Equal(new[] { 901 }, c.CarRows.Select(v => v.Id));
        Assert.Equal(new[] { 900, 902 }, c.NonHullRows.Select(v => v.Id));
        Assert.Equal("Comp A", c.ById(900).Name);
        Assert.Equal("x", c.ById(900).Field(5).Value);
        Assert.Equal(2, c.ById(900).Int(7).Value);
        Assert.Equal(8, c.ById(900).FieldCount);
        Assert.Throws<KeyNotFoundException>(() => c.ById(1));
    }

    [Fact]
    public void SyntheticRejectsBadVersionDuplicateIdShortRowAndNonIntegerType()
    {
        Assert.Throws<OriginalVehicleException>(() => OriginalVehicleCatalog.FromRecords(Parse("5,v52,1,\"A\"")));
        Assert.Throws<OriginalVehicleException>(() => OriginalVehicleCatalog.FromRecords(Parse("5,v53,1,\"A\"\r\n2,v53,1,\"B\"")));
        Assert.Throws<OriginalVehicleException>(() => OriginalVehicleCatalog.FromRecords(Parse("5,v53,1")));
        Assert.Throws<OriginalDataFormatException>(() => OriginalVehicleCatalog.FromRecords(Parse("x,v53,1,\"A\"")));
        Assert.Throws<OriginalDataFormatException>(() => OriginalVehicleCatalog.FromRecords(Parse("5,v53,y,\"A\"")));
    }
}
