using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F059 (data clause only): VEH ids 401 Null Death Orb and 405 Null Kill Station beside NWS lines 190, 191, 268, 340 and 367 and the Null Wreckage
/// audit. Oracle values were read from the pinned VEH/Rogue.veh, ITM/Rogue_mp.itm and NWS/Rogue.nws (eb1514ea) with the field map in
/// scripts/original-assets/veh_computer_field_map.py. They document raw data and visible mismatches; they do not validate any NPC AI, faction,
/// spawn, warning or drop behaviour. Asset tests are Skipped without the assets.
/// </summary>
public sealed class OriginalNullNpcsTests
{
    private const string Nws190 = "Although Northern Cross is on the frontiers, it does not mean that it is immune to the depradations of the Null.  Occasionally Null Death Orbs will appear";
    private const string Nws191 = "along with Null Kill Stations.  They can cause untold amounts of damage before destroyed.";
    private const string Nws268 = "Null sats now drop a unique item: Null Wreckage.  Null Wreckage can be returned to Public Trading Posts for a 100 Credit Bounty.";
    private const string Nws340 = "Null/Pirates drop a large amount of wreckage when they die.";
    private const string Nws367 = "Fixed Mold Spores and Killsat/Deathorb Spawns.";

    private static readonly Lazy<OriginalNullNpcs> Data = new(() => OriginalNullNpcs.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void TwoNullNpcRowsAndFiveNwsLinesArePinned()
    {
        var d = Data.Value;
        Assert.Equal(new[] { 401, 405 }, d.Npcs.Select(n => n.VehicleId));
        Assert.Equal(new[] { "Null Death Orb", "Null Kill Station" }, d.Npcs.Select(n => n.Name));
        Assert.Equal(new[] { 2, 6 }, d.Npcs.Select(n => n.RowNumber));
        Assert.All(d.Npcs, n => Assert.Equal((5, "v53", 477), (n.Record.Vehicle.RecordType, n.Record.Vehicle.VersionToken, n.Record.Vehicle.FieldCount)));
        Assert.Equal(405, d.ById(405).VehicleId);
        Assert.Throws<KeyNotFoundException>(() => d.ById(403));
        Assert.Equal(new[] { (190, Nws190), (191, Nws191), (268, Nws268), (340, Nws340), (367, Nws367) }, d.Nws.Select(l => (l.LineNumber, l.Text)));
        Assert.Equal(Nws268, d.NwsLine(268).Text);
        Assert.Throws<KeyNotFoundException>(() => d.NwsLine(295));
    }

    [RequiresOriginalAssetsFact]
    public void RawCombatAndLoadoutFieldsArePinned()
    {
        var d = Data.Value;
        Assert.All(d.Npcs, n =>
        {
            // (Hitpoints f6, RotateSpeed f202, TrackingTime f205, TrackingRadius f206, FireRadius f209, RepairRate f210, HitpointsRequiredToOperate f211, ObeyLos f214, Destroyable f217)
            Assert.Equal((500, 1500, 50, 2500, 2000, 33, 50, 1, 1),
                (n.Hitpoints.Value, n.RotateSpeed.Value, n.TrackingTime.Value, n.TrackingRadius.Value, n.FireRadius.Value, n.RepairRate.Value,
                 n.HitpointsRequiredToOperate.Value, n.ObeyLos.Value, n.Destroyable.Value));
            Assert.Equal((6, 202, 205, 206, 209, 210, 211, 214, 217),
                (n.Hitpoints.FieldIndex, n.RotateSpeed.FieldIndex, n.TrackingTime.FieldIndex, n.TrackingRadius.FieldIndex, n.FireRadius.FieldIndex,
                 n.RepairRate.FieldIndex, n.HitpointsRequiredToOperate.FieldIndex, n.ObeyLos.FieldIndex, n.Destroyable.FieldIndex));
            Assert.Equal(10000000, n.Record.TrackingWeightHigh.Value);
            Assert.Equal(0, n.Record.TrackingWeightLow.Value);
            // density/frequency limits are all -1 (native meaning of the sentinel unvalidated); relative id 0
            Assert.Equal(new[] { -1, -1, -1, -1, -1, -1 }, new[] { n.DensityMaxType.Value, n.DensityMaxActive.Value, n.DensityMaxInactive.Value, n.FrequencyMaxType.Value, n.FrequencyMaxActive.Value, n.FrequencyMaxInactive.Value });
            Assert.Equal(-1, n.Record.ChainedTurretId.Value);
            Assert.Equal(0, n.RelativeId.Value);
            // ownership logic strings (f234/f235) are empty text for both
            Assert.Equal((234, ""), (n.LogicTakeOwnership.Index, n.LogicTakeOwnership.Value));
            Assert.Equal((235, ""), (n.LogicStealOwnership.Index, n.LogicStealOwnership.Value));
        });
    }

    [RequiresOriginalAssetsFact]
    public void SpriteBlobReferencesAndKeptNonDefaultFieldsArePinned()
    {
        var d = Data.Value;
        Assert.Equal(new[] { ("None", "None"), ("SpaceStations", "rt_station_ring_center") }, d.Npcs.Select(n => (n.SpriteBaseBlobName.Value, n.SpriteBaseBlobId.Value)));
        Assert.Equal(new[] { ("SpaceStations", "rt_station_eye"), ("SpaceStations", "rt_station_ring_turret") }, d.Npcs.Select(n => (n.SpriteTurretBlobName.Value, n.SpriteTurretBlobId.Value)));
        Assert.Equal(new[] { 45, 50 }, d.Npcs.Select(n => n.Record.Int(VehComputerFields.SpriteBaseFirst + 4).Value));
        Assert.Equal(
            "0=5;1=v53;2=401;3=Null Death Orb;6=500;33=75;35=50;36=50;37=2;38=2;39=2;40=2;42=50;43=-1;49=1;50=1014;64=3004;68=2008;69=1;74=None;75=None;86=None;87=None;138=1;139=-1;140=1;141=-1;202=1500;205=50;206=2500;208=10000000;209=2000;210=33;211=50;214=1;217=1;219=-1;220=-1;221=-1;222=-1;223=-1;224=-1;225=-1;226=-1;227=-1;228=-1;231=-1;237=None;238=None;241=45;245=SpaceStations;246=rt_station_eye",
            OriginalComputerFieldsTests.Fingerprint(d.ById(401).Record, 0, 477));
        Assert.Equal(
            "0=5;1=v53;2=405;3=Null Kill Station;6=500;33=75;35=50;36=50;37=2;38=2;39=2;40=2;42=50;43=-1;49=1;50=1037;64=3004;68=2008;69=1;74=None;75=None;86=None;87=None;138=1;139=-1;140=1;141=-1;202=1500;205=50;206=2500;208=10000000;209=2000;210=33;211=50;214=1;217=1;219=-1;220=-1;221=-1;222=-1;223=-1;224=-1;225=-1;226=-1;227=-1;228=-1;231=-1;237=SpaceStations;238=rt_station_ring_center;241=50;245=SpaceStations;246=rt_station_ring_turret",
            OriginalComputerFieldsTests.Fingerprint(d.ById(405).Record, 0, 477));
        // neither row has a product block or child vehicle
        Assert.All(d.Npcs, n => Assert.All(n.Record.Products, p => Assert.True(p.IsEmpty)));
        Assert.All(d.Npcs, n => Assert.All(n.Record.ChildVehicles, c => Assert.Equal(0, c.Value)));
    }

    [RequiresOriginalAssetsFact]
    public void InventoryExplodeAndDropIdsResolveAgainstItmWithoutGuessing()
    {
        var d = Data.Value;
        var orb = d.ById(401);
        var station = d.ById(405);
        Assert.Equal(new[] { 1014, 0, 0, 0, 0, 0 }, orb.Inventory.Select(i => i.RawId));
        Assert.Equal(new[] { 1037, 0, 0, 0, 0, 0 }, station.Inventory.Select(i => i.RawId));
        Assert.Equal(new[] { 50, 51, 52, 53, 54, 55 }, orb.Inventory.Select(i => i.FieldIndex));
        Assert.Equal(("DeathOrbCannon", 6, "SpecialEffects"), (orb.Inventory[0].ItemName, orb.Inventory[0].ItemRecordType, orb.Inventory[0].ItemCategory));
        Assert.Equal(("DeathRay", 6, "SpecialEffects"), (station.Inventory[0].ItemName, station.Inventory[0].ItemRecordType, station.Inventory[0].ItemCategory));
        // unused slots are raw 0, which no ITM row has: they stay visible and unresolved
        Assert.All(orb.Inventory.Skip(1).Concat(station.Inventory.Skip(1)), i => Assert.False(i.Resolved));
        Assert.All(d.Npcs, n =>
        {
            Assert.Equal((64, 3004, "SpaceStationExplode", 8, "Effects"), (n.Explode.FieldIndex, n.Explode.RawId, n.Explode.ItemName, n.Explode.ItemRecordType, n.Explode.ItemCategory));
            Assert.Equal((68, 2008, "Wreckage", 4, "Resources"), (n.Drop.FieldIndex, n.Drop.RawId, n.Drop.ItemName, n.Drop.ItemRecordType, n.Drop.ItemCategory));
            Assert.Equal((69, 1), (n.DropItemQuantity.FieldIndex, n.DropItemQuantity.Value));
        });
    }

    [RequiresOriginalAssetsFact]
    public void NullWreckageAuditFindsNoDistinctItemAndTheDropIsOrdinaryWreckage()
    {
        var a = Data.Value.WreckageAudit;
        Assert.Empty(a.ExactMatches);
        Assert.Empty(a.CaseInsensitiveMatches);
        Assert.Empty(a.ItemNamesContainingNull);
        Assert.Equal((2008, "Wreckage", 4, "Resources"), (a.OrdinaryWreckage!.Id, a.OrdinaryWreckage.Name, a.OrdinaryWreckage.RecordType, a.OrdinaryWreckage.Category));
        Assert.Equal(new[] { 2008, 2008 }, a.NpcDrops.Select(d => d.RawId));
        Assert.Same(a.OrdinaryWreckage, a.NpcDrops[0].Item);
        // PirateStation 403, GunSat 410, BeamSat 411, Rogue Trader Outpost 413 and Mold Spore 414 drop the very same ordinary Wreckage x1
        Assert.Equal(new[] { 403, 410, 411, 413, 414 }, a.OtherComputerRowsWithSameDrop);
        Assert.Equal(new[]
        {
            "NWS 268 names a unique 'Null Wreckage' item but no ITM row is named exactly 'Null Wreckage' (nor case-insensitively).",
            "Null Death Orb (401) f[68] DropItemId=2008 is ordinary 'Wreckage' x1, not a distinct 'Null Wreckage'.",
            "Null Kill Station (405) f[68] DropItemId=2008 is ordinary 'Wreckage' x1, not a distinct 'Null Wreckage'.",
            "The same DropItemId is also set on VEH ids 403, 410, 411, 413, 414, so the drop is not unique to the Null NPCs.",
        }, a.VisibleMismatches);
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
            Assert.Throws<AssetIntegrityException>(() => OriginalNullNpcs.Load(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    // ---- pure tests on synthetic data ----

    private static string VehLine(int id, string name, int dropId, int dropQty)
    {
        var f = new List<string> { "5", "v53", id.ToString(), $"\"{name}\"", "\"\"", "\"d\"", "500" };
        while (f.Count < VehComputerFields.FieldCount) f.Add(VehComputerFields.IsStringSlot(f.Count) ? "\"\"" : "0");
        f[VehComputerFields.DropItemId] = dropId.ToString();
        f[VehComputerFields.DropItemQuantity] = dropQty.ToString();
        return string.Join(",", f);
    }

    private static string ItmLine(int type, int id, string name)
    {
        var f = new List<string> { type.ToString(), "v57", id.ToString(), $"\"{name}\"", "\"Resources\"" };
        while (f.Count < ItmFields.MinFieldCount) f.Add("0");
        return string.Join(",", f);
    }

    private static OriginalNws SyntheticNws() =>
        OriginalNws.Parse(Encoding.Latin1.GetBytes(string.Join("\r\n", Enumerable.Range(1, 367).Select(n => n switch
        {
            190 => Nws190,
            191 => Nws191,
            268 => Nws268,
            340 => Nws340,
            367 => Nws367,
            _ => "x",
        }))));

    private static OriginalVehicleCatalog Veh(params string[] lines) =>
        OriginalVehicleCatalog.FromRecords(RawRecordReader.Parse(string.Join("\r\n", lines), "VEH/t.veh"));

    private static OriginalItemCatalog Itm(params string[] lines) =>
        OriginalItemCatalog.FromRecords(RawRecordReader.Parse(string.Join("\r\n", lines), "ITM/t.itm"));

    [Fact]
    public void SyntheticDistinctNullWreckageItemIsFoundAndReportsNoNameMismatch()
    {
        var veh = Veh(VehLine(401, "Null Death Orb", 7, 3), VehLine(405, "Null Kill Station", 7, 3), VehLine(403, "Pirate", 9, 1));
        var itm = Itm(ItmLine(4, 7, "Null Wreckage"), ItmLine(4, 9, "Wreckage"));
        var d = OriginalNullNpcs.Resolve(veh, itm, SyntheticNws());
        Assert.Equal(new[] { 7 }, d.WreckageAudit.ExactMatches.Select(i => i.Id));
        Assert.Equal(new[] { 7 }, d.WreckageAudit.CaseInsensitiveMatches.Select(i => i.Id));
        Assert.Equal(new[] { "Null Death Orb (401) f[68] DropItemId=7 resolves to 'Null Wreckage' (ITM type 4)." , "Null Kill Station (405) f[68] DropItemId=7 resolves to 'Null Wreckage' (ITM type 4)." },
            d.WreckageAudit.VisibleMismatches);
        Assert.Empty(d.WreckageAudit.OtherComputerRowsWithSameDrop);
        Assert.Equal(new[] { 3, 3 }, d.Npcs.Select(n => n.DropItemQuantity.Value));
    }

    [Fact]
    public void SyntheticCaseInsensitiveOnlyMatchAndUnresolvedDropStayVisible()
    {
        var veh = Veh(VehLine(401, "A", 77, 1), VehLine(405, "B", 77, 1));
        var itm = Itm(ItmLine(4, 5, "null wreckage"), ItmLine(4, 9, "Wreckage"));
        var d = OriginalNullNpcs.Resolve(veh, itm, SyntheticNws());
        Assert.Empty(d.WreckageAudit.ExactMatches);
        Assert.Equal(new[] { 5 }, d.WreckageAudit.CaseInsensitiveMatches.Select(i => i.Id));
        Assert.Equal(new[] { 5 }, d.WreckageAudit.ItemNamesContainingNull.Select(i => i.Id));
        Assert.Equal(new[]
        {
            "NWS 268 names a unique 'Null Wreckage' item but no ITM row is named exactly 'Null Wreckage' (case-insensitive matches: 5).",
            "A (401) f[68] DropItemId=77 resolves to no ITM row.",
            "B (405) f[68] DropItemId=77 resolves to no ITM row.",
        }, d.WreckageAudit.VisibleMismatches);
        Assert.All(d.Npcs, n => Assert.Null(n.Drop.Item));
        Assert.All(d.Npcs, n => Assert.False(n.Drop.Resolved));
    }

    [Fact]
    public void SyntheticResolveRejectsMissingRowsHullRowsAndWrongNwsText()
    {
        var itm = Itm(ItmLine(4, 9, "Wreckage"));
        var nws = SyntheticNws();
        var onlyOne = Veh(VehLine(401, "A", 9, 1));
        Assert.Throws<OriginalDataException>(() => OriginalNullNpcs.Resolve(onlyOne, itm, nws));
        var hull = Veh(VehLine(401, "A", 9, 1).Replace("5,v53,401", "2,v53,401"), VehLine(405, "B", 9, 1));
        Assert.Throws<OriginalDataException>(() => OriginalNullNpcs.Resolve(hull, itm, nws));
        var ok = Veh(VehLine(401, "A", 9, 1), VehLine(405, "B", 9, 1));
        Assert.Throws<OriginalDataException>(() => OriginalNullNpcs.Resolve(ok, itm, OriginalNws.Parse(Encoding.Latin1.GetBytes("short"))));
        var wrong = Encoding.Latin1.GetBytes(string.Join("\r\n", Enumerable.Range(1, 367).Select(n => n == 268 ? "nothing here" : "x")));
        Assert.Throws<OriginalDataException>(() => OriginalNullNpcs.Resolve(ok, itm, OriginalNws.Parse(wrong)));
    }
}
