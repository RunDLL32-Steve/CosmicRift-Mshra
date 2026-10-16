using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F055 (data clause only): raw Rogue Trader Outpost view. Oracle values come from FEATURE_CATALOG.md RT-F055 and the
/// independent scripts/original-assets/audit_rt_f055_g7.py against the pinned files (eb1514ea). Oracle tests are Skipped
/// without the assets; synthetic tests always run. Nothing here proves placement, cost or production behavior.
/// </summary>
public sealed class OriginalOutpostTests
{
    private static readonly Lazy<OriginalItemCatalog> Items = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));
    private static readonly Lazy<OriginalVehicleCatalog> Vehicles = new(() => OriginalVehicleCatalog.Load(OriginalAssets.Root!));
    private static readonly Lazy<OriginalNws> Nws = new(() => OriginalNws.LoadRogue(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void CatalogClaimsForTradingPostRow169AreConfirmed()
    {
        var o = OriginalRogueTraderOutpost.Resolve(Items.Value, Vehicles.Value, Nws.Value);
        var t = o.TradingPostItem;
        Assert.Equal((169, 7, 45, "Trading Post"), (t.RowNumber, t.RecordType, t.Id, t.Name));
        Assert.Equal("Trading Post", t.Category);
        Assert.Equal(94, t.Record.FieldCount);
        Assert.Equal(0, t.Mass.Value);
        Assert.Equal((8, 5000), (t.Buy.FieldIndex, t.Buy.Value));
        Assert.Equal((15, 5000), (t.Sell.FieldIndex, t.Sell.Value));
        Assert.Equal((13, 5, 413, "Rogue Trader Outpost", 477), (o.Vehicle.RowNumber, o.Vehicle.RecordType, o.Vehicle.Id, o.Vehicle.Name, o.Vehicle.FieldCount));
        Assert.Equal("Player Created Trading Post", o.Vehicle.Field(5).Value);
        var n = o.NanobotControl;
        Assert.Equal((199, 4, 2023, "Nanobot Control System", 31), (n.RowNumber, n.RecordType, n.Id, n.Name, n.Record.FieldCount));
        Assert.Equal((5000, 2500), (n.Buy.Value, n.Sell.Value));
    }

    [RequiresOriginalAssetsFact]
    public void EveryType7ItemLinksThroughF93ToAType5VehicleRow()
    {
        var links = OriginalType7Links.Resolve(Items.Value, Vehicles.Value);
        var got = links.Select(l => (l.Item.RowNumber, l.Item.Id, l.Item.Name, l.VehicleIdField.FieldIndex, l.Vehicle.Id, l.Vehicle.RowNumber, l.Raw34.Value, l.Raw35.Value, l.Item.Buy.Value, l.Item.Sell.Value)).ToList();
        var expected = new (int, int, string, int, int, int, int, int, int, int)[]
        {
            (169, 45, "Trading Post", 93, 413, 13, 0, 0, 5000, 5000),
            (170, 46, "Micro Refinery", 93, 412, 12, 0, 0, 0, 0),
            (171, 47, "GunSat", 93, 410, 10, 0, 0, 0, 0),
            (172, 48, "BeamSat", 93, 411, 11, 0, 0, 0, 0),
            (179, 49, "Micro Repair Dock", 93, 408, 8, 0, 0, 0, 0),
            (196, 83, "Missile Manufactory", 93, 425, 15, 2023, 1, 1200, 500),
            (197, 84, "Munitions Manufactory", 93, 426, 16, 2023, 1, 1000, 500),
            (198, 85, "Defense Manufactory", 93, 427, 17, 2023, 1, 750, 200),
        };
        Assert.Equal(expected, got);
        Assert.All(links, l => Assert.True(l.Vehicle.IsComputer));
        Assert.Equal(8, Items.Value.Items.Count(i => i.RecordType == 7));
    }

    [RequiresOriginalAssetsFact]
    public void OutpostVehicleRowKeepsEightRawBlocks()
    {
        var b = OriginalRogueTraderOutpost.Resolve(Items.Value, Vehicles.Value, Nws.Value).Blocks;
        var got = b.Select(x => (x.FirstFieldIndex, x.Name, x.ItemIdField.Value, x.Raw2.Value, x.InputItemIdField.Value, x.Raw6.Value, x.Raw7.Value, x.Item.Name, x.ItemIsType7, x.InputItem.Name)).ToList();
        var expected = new (int, string, int, int, int, int, int, string, bool, string)[]
        {
            (253, "Trade Tellerium", 18, -1, 2004, -1, 0, "Tellerium Credits", false, "Refined Tellerium"),
            (267, "Trade Krandazite", 17, -1, 2006, -1, 0, "Krandazite Credits", false, "Refined Krandazite"),
            (281, "Trade Wreckage", 19, -1, 2008, -1, 0, "Scrap Credits", false, "Wreckage"),
            (295, "Trade Alloy", 44, -1, 2020, -1, 0, "Alloy Credits", false, "Alloy"),
            (309, "Build Gunsat", 47, 1, 2020, 10, 200, "GunSat", true, "Alloy"),
            (323, "Build BeamSat", 48, 1, 2020, 20, 400, "BeamSat", true, "Alloy"),
            (337, "Build Micro Refinery", 46, 1, 2020, 20, 500, "Micro Refinery", true, "Alloy"),
            (351, "Build Micro Repair Dock", 49, 1, 2020, 20, 500, "Micro Repair Dock", true, "Alloy"),
        };
        Assert.Equal(expected, got);
        Assert.All(b, x => Assert.Equal(14, x.Fields.Count));
    }

    [RequiresOriginalAssetsFact]
    public void NwsTextLines106To110AreVerbatimAndLaterLinesCorroborateBuildCosts()
    {
        var o = OriginalRogueTraderOutpost.Resolve(Items.Value, Vehicles.Value, Nws.Value);
        Assert.Equal(new[] { 106, 107, 108, 109, 110 }, o.NwsPurchaseText.Select(l => l.LineNumber));
        Assert.Equal(
            new[]
            {
                "You can purchase a Trading Post at the SpaceDock",
                "",
                "You take the trading post to an area of space you wish to place it and activate it (Press Home).",
                "",
                "You can than trade refined minerals as you do at Public Trading Posts.",
            },
            o.NwsPurchaseText.Select(l => l.Text));
        Assert.Equal(483, Nws.Value.Lines.Count);
        Assert.Equal("GunSats cost 200 credits and 10 Alloy to produce.", Nws.Value.Text(114));
        Assert.Equal("BeamSats cost 400 credits and 20 Alloy to produce.", Nws.Value.Text(116));
        Assert.Equal("Micro Refineries cost 500 credits and 20 Alloy to produce.", Nws.Value.Text(118));
        Assert.Equal("Micro Repair Docks cost 500 credits and 20 Alloy to produce.", Nws.Value.Text(120));
    }

    [RequiresOriginalAssetsFact]
    public void ModifiedNwsBytesAreRejectedByTheIntegrityGuard()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rt-nws-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "NWS"));
        try
        {
            var bytes = File.ReadAllBytes(Path.Combine(OriginalAssets.Root!, "NWS", "Rogue.nws"));
            bytes[200] ^= 1;
            File.WriteAllBytes(Path.Combine(dir, "NWS", "Rogue.nws"), bytes);
            Assert.Throws<AssetIntegrityException>(() => OriginalNws.LoadRogue(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void SyntheticNwsKeepsBlankLinesAndNumbersPhysicalLines()
    {
        var nws = OriginalNws.Parse(Encoding.Latin1.GetBytes("a\r\n\r\nb\nc\rd"));
        Assert.Equal(new[] { "a", "", "b", "c", "d" }, nws.Lines.Select(l => l.Text));
        Assert.Equal(3, nws.Lines[2].LineNumber);
        Assert.Equal(new[] { "", "b" }, nws.Range(2, 3).Select(l => l.Text));
        Assert.Throws<OriginalDataException>(() => nws.Text(6));
    }

    private static string Row(string type, int id, string name, int count, string tail)
    {
        var fields = new List<string> { type, "v57", id.ToString(), $"\"{name}\"", "\"Equipment\"" };
        while (fields.Count < count - 1) fields.Add("0");
        fields.Add(tail);
        return string.Join(",", fields);
    }

    [Fact]
    public void SyntheticType7LinkRequiresExactlyOneType5VehicleRow()
    {
        var items = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(
            Row("7", 1, "T", 94, "900") + "\r\n" + Row("4", 2, "N", 31, "0"), "ITM/t.itm"));
        var veh = OriginalVehicleCatalog.FromRecords(RawRecordReader.Parse("5,v53,900,\"Comp\"\r\n2,v53,901,\"Hull\"", "VEH/t.veh"));
        var l = Assert.Single(OriginalType7Links.Resolve(items, veh));
        Assert.Equal(900, l.Vehicle.Id);
        Assert.Equal(93, l.VehicleIdField.FieldIndex);

        var toHull = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(Row("7", 1, "T", 94, "901"), "ITM/t.itm"));
        Assert.Throws<OriginalDataException>(() => OriginalType7Links.Resolve(toHull, veh));
        var missing = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(Row("7", 1, "T", 94, "777"), "ITM/t.itm"));
        Assert.Throws<OriginalDataException>(() => OriginalType7Links.Resolve(missing, veh));
        var shortRow = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(Row("7", 1, "T", 93, "900"), "ITM/t.itm"));
        Assert.Throws<OriginalDataException>(() => OriginalType7Links.Resolve(shortRow, veh));
    }
}
