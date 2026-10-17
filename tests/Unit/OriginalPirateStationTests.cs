using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F060 (data clause only): VEH row 4 (id 403 PirateStation) beside NWS lines 21, 51, 338 and 340. Oracle values were read from the pinned
/// VEH/Rogue.veh, ITM/Rogue_mp.itm, RPG/Rogue.rpg and NWS/Rogue.nws (eb1514ea) before the assertions were written. They document raw data and visible
/// mismatches; they do not validate any controller, spawn, team, drop amount or presentation behaviour. Asset tests are Skipped without the assets.
/// </summary>
public sealed class OriginalPirateStationTests
{
    private static readonly Lazy<OriginalPirateStation> Data = new(() => OriginalPirateStation.Load(OriginalAssets.Root!));

    private const string Ownership = "5|6|9|10|1|3|4|11|2|8|15|16";

    [RequiresOriginalAssetsFact]
    public void RecordIdentityAndNwsLinesArePinned()
    {
        var d = Data.Value;
        Assert.Equal((403, "PirateStation", 4, 477), (d.Record.Id, d.Record.Name, d.Record.RowNumber, d.Record.Record.FieldCount));
        Assert.Equal(new[] { 21, 51, 338, 340 }, d.Nws.Select(l => l.LineNumber));
        Assert.Equal(new[]
        {
            "the area.  Each of the SpaceDocks in the Sector have created their own militias. There are also many bands of Pirates and Rogue Traders",
            "the role of Rogue Traders or Space Pirates depending on their demeanor and how they earn their credits.",
            "When a player ship is destroyed, they will drop more than 1 wreckage, AND will drop part of any ammo they are carrying.",
            "Null/Pirates drop a large amount of wreckage when they die.",
        }, d.Nws.Select(l => l.Text));
        Assert.Equal((340, "Null/Pirates drop a large amount of wreckage when they die."), (d.NwsWreckage.LineNumber, d.NwsWreckage.Text));
    }

    [RequiresOriginalAssetsFact]
    public void RawCombatAndBehaviourFieldsArePinned()
    {
        var d = Data.Value;
        Assert.Equal((300, 1, 1, 75, 50, 50), (d.Hitpoints.Value, d.IsControllable.Value, d.DisplayHealth.Value, d.BarrelLength.Value, d.PhysicalRadius.Value, d.TriggerRadius.Value));
        Assert.Equal((1200, 50, 2500, 0, 10000000, 2000), (d.RotateSpeed.Value, d.TrackingTime.Value, d.TrackingRadius.Value, d.TrackingWeightLow.Value, d.TrackingWeightHigh.Value, d.FireRadius.Value));
        Assert.Equal((33, 50, 1, 1, -1), (d.RepairRate.Value, d.HitpointsRequiredToOperate.Value, d.ObeyLos.Value, d.Destroyable.Value, d.ChainedTurretId.Value));
        Assert.Equal(new[] { 44, 6, 202, 205, 206, 207, 208, 209, 210, 211, 214, 217, 231 },
            new[] { d.IsControllable, d.Hitpoints, d.RotateSpeed, d.TrackingTime, d.TrackingRadius, d.TrackingWeightLow, d.TrackingWeightHigh, d.FireRadius, d.RepairRate, d.HitpointsRequiredToOperate, d.ObeyLos, d.Destroyable, d.ChainedTurretId }.Select(x => x.FieldIndex));
        // density and frequency limits f[218..228]: DensityRadius 0, everything else -1 (a negative sentinel candidate; meaning not established)
        Assert.Equal((0, -1, -1, -1, -1), (d.DensityRadius.Value, d.DensityMaxType.Value, d.DensityMaxActive.Value, d.DensityMaxInactive.Value, d.MaxTypeByPlayerRegardlessOfTeam.Value));
        Assert.Equal(new[] { -1, -1, -1, -1, -1, -1 }, new[] { d.FrequencyMaxType, d.FrequencyMaxActive, d.FrequencyMaxInactive, d.FrequencyDensityMaxType, d.FrequencyDensityMaxActive, d.FrequencyDensityMaxInactive }.Select(x => x.Value));
        Assert.Equal(new[] { 1, -1, 1, -1 }, new[] { d.DisplayOnFriendlyRadar, d.FriendlyRadarColor, d.DisplayOnEnemyRadar, d.EnemyRadarColor }.Select(x => x.Value));
        Assert.Equal(new[] { 138, 139, 140, 141 }, new[] { d.DisplayOnFriendlyRadar, d.FriendlyRadarColor, d.DisplayOnEnemyRadar, d.EnemyRadarColor }.Select(x => x.FieldIndex));
        Assert.Equal(new[] { 2, 2, 2, 2 }, new[] { 37, 38, 39, 40 }.Select(i => d.Record.Int(i).Value));
    }

    [RequiresOriginalAssetsFact]
    public void SpritesAreRawText()
    {
        var d = Data.Value;
        Assert.Equal(new[] { "SpaceStations", "stationB_baseV1", "SpaceStations", "stationB_turretV1" },
            new[] { d.SpriteBaseBlobName, d.SpriteBaseBlobId, d.SpriteTurretBlobName, d.SpriteTurretBlobId }.Select(f => f.Value));
        Assert.Equal(new[] { 237, 238, 245, 246 }, new[] { d.SpriteBaseBlobName, d.SpriteBaseBlobId, d.SpriteTurretBlobName, d.SpriteTurretBlobId }.Select(f => f.Index));
        Assert.Equal((-5, -5, -5, -5), (d.Record.Int(242).Value, d.Record.Int(243).Value, d.Record.Int(250).Value, d.Record.Int(251).Value));
    }

    [RequiresOriginalAssetsFact]
    public void InventoryChildrenExplodeAndDropResolveToItmAndVehRows()
    {
        var d = Data.Value;
        Assert.Equal(6, d.Inventory.Count);
        Assert.Equal(new[] { 3002, 0, 0, 0, 0, 0 }, d.Inventory.Select(s => s.Raw.Value));
        Assert.Equal(new[] { 50, 51, 52, 53, 54, 55 }, d.Inventory.Select(s => s.Raw.FieldIndex));
        var gun = d.Inventory[0].Item!;
        Assert.Equal((3002, "BaseStationCannon", "SpecialEffects", 8, 73), (gun.Id, gun.Name, gun.Category, gun.RecordType, gun.RowNumber));
        Assert.All(d.Inventory.Skip(1), s => Assert.Null(s.Item));
        // ChildVehicles f[56..63]: all 0, no child ships named
        Assert.Equal(8, d.ChildVehicles.Count);
        Assert.All(d.ChildVehicles, c => { Assert.Equal(0, c.Raw.Value); Assert.Null(c.Vehicle); });
        Assert.Equal(56, d.ChildVehicles[0].Raw.FieldIndex);
        Assert.Equal((3004, "SpaceStationExplode", "Effects", 89), (d.Explode.Raw.Value, d.Explode.Item!.Name, d.Explode.Item.Category, d.Explode.Item.RowNumber));
        Assert.Equal((64, 8), (d.Explode.Raw.FieldIndex, d.Explode.Item!.RecordType));
        Assert.Equal((2008, "Wreckage", "Resources", 87, 4), (d.Drop.Raw.Value, d.Drop.Item!.Name, d.Drop.Item.Category, d.Drop.Item.RowNumber, d.Drop.Item.RecordType));
        Assert.Equal((68, 69, 1), (d.Drop.Raw.FieldIndex, d.DropItemQuantity.FieldIndex, d.DropItemQuantity.Value));
    }

    [RequiresOriginalAssetsFact]
    public void OwnershipLogicRawAndSkillLogicParseResult()
    {
        var d = Data.Value;
        Assert.Equal((234, Ownership), (d.LogicTakeOwnershipRaw.Index, d.LogicTakeOwnershipRaw.Value));
        Assert.Equal((235, ""), (d.LogicStealOwnershipRaw.Index, d.LogicStealOwnershipRaw.Value));
        // the existing skill-logic parser reads f[234] as an all-'|' chain of twelve plain skill ids and reproduces the text exactly
        Assert.IsType<SkillLogicChain>(d.OwnershipLogic);
        var chain = (SkillLogicChain)d.OwnershipLogic;
        Assert.Equal(Ownership, chain.ToString());
        Assert.Equal(12, chain.Terms.Count);
        Assert.All(chain.Ops, op => Assert.Equal(SkillLogicOp.Or, op));
        Assert.Equal(new[] { 5, 6, 9, 10, 1, 3, 4, 11, 2, 8, 15, 16 }, d.OwnershipTerms.Select(t => t.SkillId));
        // each id equals the RPG f[1] skill id of one of the twelve hull rows (Asp = 2); this repeats a data fact, not a rule for what the expression gates
        Assert.Equal(new[] { "Rak'Kir", "Rip'Pir", "Zap'Pir", "Zag'Gir", "Skithist", "Kragath", "Mastakith", "Tarthist", "Asp", "Hornet", "Excelsior", "Ravager" },
            d.OwnershipTerms.Select(t => t.Hull!.Name));
        Assert.True(d.OwnershipTermsAreExactlyTheTwelveHullSkillIds);
    }

    [RequiresOriginalAssetsFact]
    public void NoProductBlockIsNonDefault()
    {
        var d = Data.Value;
        Assert.Empty(d.NonEmptyProducts);
        Assert.Equal(16, d.Record.Products.Count);
        Assert.All(d.Record.Products, p => Assert.True(p.IsEmpty));
    }

    [RequiresOriginalAssetsFact]
    public void DropIsComparedWithNullStructuresAndMismatchWithNwsStaysVisible()
    {
        var d = Data.Value;
        Assert.Equal(new[]
        {
            new OriginalPirateStationDropRow(401, "Null Death Orb", 500, 2008, 1, 3004),
            new OriginalPirateStationDropRow(405, "Null Kill Station", 500, 2008, 1, 3004),
        }, d.NullDropComparison);
        Assert.Equal(new[]
        {
            "NWS line 340 says Pirates drop 'a large amount of wreckage' but f[68] DropItemId=2008 (Wreckage) has f[69] DropItemQuantity=1; the multiple-wreckage amount is not in these two fields.",
            "VEH 401 Null Death Orb and 405 Null Kill Station carry the same drop 2008 x1, so these fields do not set Pirates apart from the other 'Null' structures.",
            "f[235] LogicStealOwnership is empty (LogicTakeOwnership f[234] is not).",
            "All 16 product blocks are default: the station sells and produces nothing in the VEH row.",
            "ChildVehicles f[56..63] are all 0: no child ships are named by the VEH row.",
        }, d.VisibleMismatches);
        Assert.Equal(1, d.DropItemQuantity.Value);
        Assert.NotEmpty(OriginalPirateStation.RemainingWork);
        Assert.Contains(OriginalPirateStation.RemainingWork, w => w.Contains("LIO/Rogue.lio row 20", StringComparison.Ordinal));
    }

    [RequiresOriginalAssetsFact]
    public void RecordKeepsEveryNonDefaultRawField()
    {
        var d = Data.Value;
        Assert.Equal(
            "0=5;1=v53;2=403;3=PirateStation;6=300;33=75;35=50;36=50;37=2;38=2;39=2;40=2;42=50;43=-1;44=1;49=1;50=3002;64=3004;68=2008;69=1;74=None;75=None;86=None;87=None;138=1;139=-1;140=1;141=-1;202=1200;205=50;206=2500;208=10000000;209=2000;210=33;211=50;214=1;217=1;219=-1;220=-1;221=-1;222=-1;223=-1;224=-1;225=-1;226=-1;227=-1;228=-1;231=-1;234=" + Ownership + ";237=SpaceStations;238=stationB_baseV1;242=-5;243=-5;245=SpaceStations;246=stationB_turretV1;250=-5;251=-5",
            OriginalComputerFieldsTests.Fingerprint(d.Record, 0, 253));
        Assert.Equal("", OriginalComputerFieldsTests.Fingerprint(d.Record, 253, 477));
    }

    [RequiresOriginalAssetsFact]
    public void ResolveRejectsWrongNwsAndModifiedBytes()
    {
        var root = OriginalAssets.Root!;
        var veh = OriginalVehicleCatalog.Load(root);
        var itm = OriginalItemCatalog.Load(root);
        var hulls = OriginalHullCatalog.Load(root);
        var shifted = Encoding.Latin1.GetBytes(string.Join("\r\n", Enumerable.Range(1, 340).Select(i => i == 340 ? "nothing here" : "x")));
        Assert.Throws<OriginalDataException>(() => OriginalPirateStation.Resolve(veh, itm, hulls, OriginalNws.Parse(shifted)));
        Assert.Throws<OriginalDataException>(() => OriginalPirateStation.Resolve(veh, itm, hulls, OriginalNws.Parse(Encoding.Latin1.GetBytes("short"))));

        var dir = Path.Combine(Path.GetTempPath(), "rt-veh-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "VEH"));
        try
        {
            var bytes = File.ReadAllBytes(Path.Combine(root, "VEH", "Rogue.veh"));
            bytes[300] ^= 1;
            File.WriteAllBytes(Path.Combine(dir, "VEH", "Rogue.veh"), bytes);
            Assert.Throws<AssetIntegrityException>(() => OriginalPirateStation.Load(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void MismatchHelperReportsOnlyWhatTheRawValuesShow()
    {
        string Row(int dropQty, string steal)
        {
            var f = new List<string> { "5", "v53", "403", "\"PirateStation\"", "\"\"", "\"d\"", "300" };
            while (f.Count < VehComputerFields.FieldCount) f.Add(VehComputerFields.IsStringSlot(f.Count) ? "\"\"" : "0");
            f[VehComputerFields.DropItemId] = "2008";
            f[VehComputerFields.DropItemQuantity] = dropQty.ToString();
            f[VehComputerFields.LogicTakeOwnership] = "\"" + Ownership + "\"";
            f[VehComputerFields.LogicStealOwnership] = "\"" + steal + "\"";
            f[VehComputerFields.ChildVehicle(0)] = "7";
            f[VehComputerFields.Product(0, 0)] = "\"Thing\"";
            return string.Join(",", f);
        }
        var rec = new OriginalComputerRecord(OriginalVehicleCatalog.FromRecords(RawRecordReader.Parse(Row(25, Ownership), "VEH/t.veh")).All.Single());
        var dropSlot = new OriginalPirateStationItemSlot(rec.DropItemId, null);
        var other = new OriginalPirateStationDropRow(401, "X", 500, 66, 5, 0);
        // quantity 25 is not "1", steal logic is present, a product and a child vehicle exist, the comparison row differs: no note at all
        Assert.Empty(OriginalPirateStation.DescribeMismatches(rec, dropSlot, new[] { other }, rec.Products.Where(p => !p.IsEmpty).ToList()));
        var rec1 = new OriginalComputerRecord(OriginalVehicleCatalog.FromRecords(RawRecordReader.Parse(Row(1, ""), "VEH/t.veh")).All.Single());
        var notes = OriginalPirateStation.DescribeMismatches(rec1, dropSlot, new[] { new OriginalPirateStationDropRow(401, "X", 500, 2008, 1, 0) }, Array.Empty<OriginalComputerProduct>());
        Assert.Equal(4, notes.Count);
        Assert.StartsWith("NWS line 340 says Pirates drop", notes[0]);
        Assert.Equal("f[235] LogicStealOwnership is empty (LogicTakeOwnership f[234] is not).", notes[2]);
    }
}
