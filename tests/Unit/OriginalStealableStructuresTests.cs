using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F058 (data clause only): VEH rows 8, 12 and 13 (ids 408, 412, 413) beside NWS line 295. Oracle values were read from the pinned
/// VEH/Rogue.veh and NWS/Rogue.nws (eb1514ea) with scripts/original-assets/veh_computer_field_map.py. They document raw data and visible
/// mismatches; they do not validate any steal threshold, access check or ownership transfer. Asset tests are Skipped without the assets.
/// </summary>
public sealed class OriginalStealableStructuresTests
{
    private const string Nws295 = "Micro Refinery, Micro Repair and Rogue Trader Outpost can be stolen when they have been taken down to 10% HP (when their HP are in the red)";

    private static readonly Lazy<OriginalStealableStructures> Data = new(() => OriginalStealableStructures.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void Nws295TextAndThreeStructuresArePinned()
    {
        var d = Data.Value;
        Assert.Equal((295, Nws295), (d.Nws.LineNumber, d.Nws.Text));
        Assert.Equal(new[] { 408, 412, 413 }, d.Structures.Select(s => s.VehicleId));
        Assert.Equal(new[] { "Micro Repair Dock", "Micro Refinery", "Rogue Trader Outpost" }, d.Structures.Select(s => s.Name));
        Assert.Equal(new[] { 8, 12, 13 }, d.Structures.Select(s => s.Record.RowNumber));
        Assert.Equal(new[] { "Player Made Repair Dock", "Player created Deep Space Refinery", "Player Created Trading Post" }, d.Structures.Select(s => s.Record.Description.Value));
        Assert.Equal(412, d.ById(412).VehicleId);
        Assert.Throws<KeyNotFoundException>(() => d.ById(410));
    }

    [RequiresOriginalAssetsFact]
    public void RawThresholdAccessAndOwnerFieldsArePinned()
    {
        var d = Data.Value;
        // (Hitpoints f6, HitpointsRequiredToOperate f211, Destroyable f217, VehicleGetInMode f66, RepairRate f210, DropItemId f68, DropItemQuantity f69)
        Assert.Equal(new[] { (250, 5, 1, 0, 50, 0, 0), (250, 5, 1, 0, 50, 0, 0), (500, 10, 1, 0, 250, 2008, 1) },
            d.Structures.Select(s => (s.Hitpoints.Value, s.HitpointsRequiredToOperate.Value, s.Destroyable.Value, s.VehicleGetInMode.Value, s.RepairRate.Value, s.DropItemId.Value, s.DropItemQuantity.Value)));
        Assert.Equal(new[] { 6, 6, 6 }, d.Structures.Select(s => s.Hitpoints.FieldIndex));
        Assert.Equal(new[] { 211, 211, 211 }, d.Structures.Select(s => s.HitpointsRequiredToOperate.FieldIndex));
        // LogicTakeOwnership f[234] / LogicStealOwnership f[235]: empty text in all three rows
        Assert.All(d.Structures, s =>
        {
            Assert.Equal((234, ""), (s.LogicTakeOwnership.Index, s.LogicTakeOwnership.Value));
            Assert.Equal((235, ""), (s.LogicStealOwnership.Index, s.LogicStealOwnership.Value));
        });
        // ExplodeItemId f64 (0, 3004, 3004), InventoryItems f50..55, ChildVehicles f56..63
        Assert.Equal(new[] { 0, 3004, 3004 }, d.Structures.Select(s => s.Record.ExplodeItemId.Value));
        Assert.Equal(new[] { 0, 0, 3002 }, d.Structures.Select(s => s.Record.InventoryItems[0].Value));
        Assert.All(d.Structures, s => Assert.All(s.Record.ChildVehicles, c => Assert.Equal(0, c.Value)));
        Assert.Equal(new[] { 0, 0, 10 }, d.Structures.Select(s => s.Record.RelativeId.Value));
    }

    [RequiresOriginalAssetsFact]
    public void TenPercentCandidateIsShownNextToTheRawRequiredToOperateValueWithoutClaimingEither()
    {
        var d = Data.Value;
        Assert.Equal(new[]
        {
            new NewsTenPercentCandidate(250, 25, true, 5, false),
            new NewsTenPercentCandidate(250, 25, true, 5, false),
            new NewsTenPercentCandidate(500, 50, true, 10, false),
        }, d.Structures.Select(s => s.Candidate));
    }

    [RequiresOriginalAssetsFact]
    public void VisibleMismatchesAreRecordedAndNothingIsCorrected()
    {
        var d = Data.Value;
        Assert.Equal(new[]
        {
            "f[211] HitpointsRequiredToOperate=5 is not 10% of f[6] Hitpoints=250 (integer 10% = 25).",
            "f[235] LogicStealOwnership is empty.",
            "f[234] LogicTakeOwnership is empty.",
        }, d.ById(408).VisibleMismatches);
        Assert.Equal(d.ById(408).VisibleMismatches, d.ById(412).VisibleMismatches);
        Assert.Equal(new[]
        {
            "f[211] HitpointsRequiredToOperate=10 is not 10% of f[6] Hitpoints=500 (integer 10% = 50).",
            "f[235] LogicStealOwnership is empty.",
            "f[234] LogicTakeOwnership is empty.",
        }, d.ById(413).VisibleMismatches);
        // the raw values are still the file's values
        Assert.Equal(new[] { 5, 5, 10 }, d.Structures.Select(s => s.HitpointsRequiredToOperate.Value));
    }

    [RequiresOriginalAssetsFact]
    public void RecordsKeepEveryNonDefaultRawFieldOfTheThreeRows()
    {
        var d = Data.Value;
        Assert.Equal(
            "0=5;1=v53;2=408;3=Micro Repair Dock;5=Player Made Repair Dock;6=250;9=250000;35=25;36=25;37=2;38=2;42=60;43=-1;49=1;74=None;75=None;86=None;87=None;138=1;139=-1;140=1;141=-1;210=50;211=5;217=1;218=3500;219=1;220=-1;221=-1;222=-1;223=1;224=-1;225=-1;226=1;227=-1;228=-1;231=-1;237=defstructures;238=repairStationSmV0;244=150;245=None;246=None",
            OriginalComputerFieldsTests.Fingerprint(d.ById(408).Record, 0, 253));
        Assert.Equal(
            "0=5;1=v53;2=412;3=Micro Refinery;5=Player created Deep Space Refinery;6=250;9=250000;35=25;36=25;37=2;38=2;42=60;43=-1;49=1;64=3004;74=None;75=None;86=None;87=None;138=1;139=-1;140=1;141=-1;210=50;211=5;217=1;218=3500;219=2;220=-1;221=-1;222=-1;223=2;224=-1;225=-1;226=2;227=-1;228=-1;231=-1;237=defstructures;238=refineryStationV1;242=10;244=100;245=None;246=None;250=10;252=250",
            OriginalComputerFieldsTests.Fingerprint(d.ById(412).Record, 0, 253));
        Assert.Equal(
            "0=5;1=v53;2=413;3=Rogue Trader Outpost;5=Player Created Trading Post;6=500;9=250000;33=75;35=50;36=50;37=2;38=2;42=50;43=-1;49=1;50=3002;64=3004;68=2008;69=1;74=None;75=None;86=None;87=None;137=10;138=1;139=-1;140=1;141=-1;202=1500;205=25;206=2500;208=100000;209=2000;210=250;211=10;214=1;217=1;218=3500;219=1;220=-1;221=-1;222=-1;223=1;224=-1;225=-1;226=1;227=-1;228=-1;231=-1;237=SpaceStations;238=stationB_baseV1;242=5;245=spacestations;246=stationB_turretV1;250=-5;251=-5",
            OriginalComputerFieldsTests.Fingerprint(d.ById(413).Record, 0, 253));
        Assert.Equal(
            "253=Repair 5 Hull Points;254=39;255=1;257=150;260=5;267=Repair 10 Hull Points;268=40;269=1;271=150;274=10;281=Remove Armor;282=41;283=1;285=150;288=50",
            OriginalComputerFieldsTests.Fingerprint(d.ById(408).Record, 253, 477));
        Assert.Equal(
            "253=Refine Tellerium;254=2004;255=-1;257=150;258=2003;259=-1;267=Refine Krandazite;268=2006;269=-1;271=150;272=2005;273=-1;281=Refine Iron Ore;282=2020;283=-1;286=2019;287=-1",
            OriginalComputerFieldsTests.Fingerprint(d.ById(412).Record, 253, 477));
        Assert.Equal(
            "253=Trade Tellerium;254=18;255=-1;258=2004;259=-1;267=Trade Krandazite;268=17;269=-1;272=2006;273=-1;281=Trade Wreckage;282=19;283=-1;286=2008;287=-1;295=Trade Alloy;296=44;297=-1;300=2020;301=-1;309=Build Gunsat;310=47;311=1;314=2020;315=10;316=200;323=Build BeamSat;324=48;325=1;328=2020;329=20;330=400;337=Build Micro Refinery;338=46;339=1;342=2020;343=20;344=500;351=Build Micro Repair Dock;352=49;353=1;356=2020;357=20;358=500",
            OriginalComputerFieldsTests.Fingerprint(d.ById(413).Record, 253, 477));
    }

    [RequiresOriginalAssetsFact]
    public void ModifiedVehBytesAreRejectedByTheIntegrityGuard()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rt-veh-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "VEH"));
        try
        {
            var bytes = File.ReadAllBytes(Path.Combine(OriginalAssets.Root!, "VEH", "Rogue.veh"));
            bytes[300] ^= 1;
            File.WriteAllBytes(Path.Combine(dir, "VEH", "Rogue.veh"), bytes);
            Assert.Throws<AssetIntegrityException>(() => OriginalStealableStructures.Load(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void TenPercentCandidateHelperUsesTruncatingIntegerMath()
    {
        Assert.Equal(new NewsTenPercentCandidate(250, 25, true, 25, true), OriginalStealableStructures.TenPercentCandidate(250, 25));
        Assert.Equal(new NewsTenPercentCandidate(255, 25, false, 26, false), OriginalStealableStructures.TenPercentCandidate(255, 26));
        Assert.Equal(new NewsTenPercentCandidate(9, 0, false, 0, true), OriginalStealableStructures.TenPercentCandidate(9, 0));
        Assert.Equal(new NewsTenPercentCandidate(0, 0, true, -1, false), OriginalStealableStructures.TenPercentCandidate(0, -1));
        Assert.Throws<OriginalDataException>(() => OriginalStealableStructures.TenPercentCandidate(-1, 0));
    }

    private static string VehLine(int id, string name, int hitpoints)
    {
        var f = new List<string> { "5", "v53", id.ToString(), $"\"{name}\"", "\"\"", "\"d\"", hitpoints.ToString() };
        while (f.Count < VehComputerFields.FieldCount) f.Add(VehComputerFields.IsStringSlot(f.Count) ? "\"\"" : "0");
        f[VehComputerFields.HitpointsRequiredToOperate] = (hitpoints / 10).ToString();
        return string.Join(",", f);
    }

    [Fact]
    public void SyntheticResolveReportsNoMismatchWhenRawValuesAgreeAndRejectsMissingRowsOrNwsText()
    {
        var veh = OriginalVehicleCatalog.FromRecords(RawRecordReader.Parse(
            string.Join("\r\n", VehLine(408, "A", 250), VehLine(412, "B", 250), VehLine(413, "C", 500)), "VEH/t.veh"));
        var nws295 = Encoding.Latin1.GetBytes(string.Join("\r\n", Enumerable.Range(1, 294).Select(_ => "x").Append(Nws295)));
        var d = OriginalStealableStructures.Resolve(veh, OriginalNws.Parse(nws295));
        Assert.Equal(new[] { 408, 412, 413 }, d.Structures.Select(s => s.VehicleId));
        Assert.All(d.Structures, s => Assert.True(s.Candidate.RequiredToOperateEqualsTenPercentFloor));
        // only the two empty ownership-logic notes remain, so the threshold note is conditional on the data, not hard-coded
        Assert.All(d.Structures, s => Assert.Equal(new[] { "f[235] LogicStealOwnership is empty.", "f[234] LogicTakeOwnership is empty." }, s.VisibleMismatches));

        var missing = OriginalVehicleCatalog.FromRecords(RawRecordReader.Parse(string.Join("\r\n", VehLine(408, "A", 250), VehLine(412, "B", 250)), "VEH/t.veh"));
        Assert.Throws<OriginalDataException>(() => OriginalStealableStructures.Resolve(missing, OriginalNws.Parse(nws295)));
        Assert.Throws<OriginalDataException>(() => OriginalStealableStructures.Resolve(veh, OriginalNws.Parse(Encoding.Latin1.GetBytes("short"))));
        var otherText = Encoding.Latin1.GetBytes(string.Join("\r\n", Enumerable.Range(1, 294).Select(_ => "x").Append("nothing here")));
        Assert.Throws<OriginalDataException>(() => OriginalStealableStructures.Resolve(veh, OriginalNws.Parse(otherText)));
        var hull = OriginalVehicleCatalog.FromRecords(RawRecordReader.Parse(
            string.Join("\r\n", VehLine(408, "A", 250).Replace("5,v53,408", "2,v53,408"), VehLine(412, "B", 250), VehLine(413, "C", 500)), "VEH/t.veh"));
        Assert.Throws<OriginalDataException>(() => OriginalStealableStructures.Resolve(hull, OriginalNws.Parse(nws295)));
    }
}
