using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F061 (data clause only): VEH row 3 (id 402 MeteorShower), its ITM item 1007 "Meteor" and NWS line 188. Oracle values were read from
/// the pinned VEH/Rogue.veh, ITM/Rogue_mp.itm and NWS/Rogue.nws (eb1514ea) with an independent CSV dump before the assertions were written.
/// They document raw data only; they do not validate any trap event, spawn area/timing (LIO rows 18/49/52/57 are not read), meteor
/// movement, collision or damage. Asset tests are Skipped without the assets.
/// </summary>
public sealed class OriginalMeteorShowerTests
{
    private const string Nws188 = "There are naturally occuring hazards, like Meteor Showers, that will occasionally appear to catch the unwary.";

    private static readonly Lazy<OriginalMeteorShower> Data = new(() => OriginalMeteorShower.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void VehRow402AndNws188ArePinned()
    {
        var d = Data.Value;
        Assert.Equal((188, Nws188), (d.Nws.LineNumber, d.Nws.Text));
        Assert.Equal((402, "MeteorShower", 3, 5, "v53", 477), (d.Record.Id, d.Record.Name, d.Record.RowNumber, d.Record.Vehicle.RecordType, d.Record.Vehicle.VersionToken, d.Record.Vehicle.FieldCount));
        Assert.Equal(("0", true), (d.Record.Description.Value, d.Record.Description.WasQuoted));
        Assert.Equal("", d.Record.SkillLogic.Value);
    }

    [RequiresOriginalAssetsFact]
    public void RawScalarsAreExactlyTheFileValuesWithoutInterpretation()
    {
        var r = Data.Value.Record;
        Assert.Equal((50, 100000), (r.Hitpoints.Value, r.Weight.Value));
        Assert.Equal((8, 16, 8, 30), (r.PhysicalRadius.Value, r.TriggerRadius.Value, r.Int(VehComputerFields.LowZ).Value, r.Int(VehComputerFields.HighZ).Value));
        Assert.Equal((-1, -1), (r.PickupItemId.Value, r.RadarPrize.Value));
        Assert.Equal((1, 1700, 1150), (r.Int(VehComputerFields.IsWarpable).Value, Data.Value.EnergyMax.Value, Data.Value.EnergyRate.Value));
        Assert.Equal((2000, 0, 360, 50, 20000, 500000, 20000), (r.RotateSpeed.Value, r.AngleStart.Value, r.AngleLength.Value, r.TrackingTime.Value, r.TrackingRadius.Value, r.TrackingWeightHigh.Value, r.FireRadius.Value));
        Assert.Equal((10, 25, 5, 5, 1, 0), (r.RepairRate.Value, r.HitpointsRequiredToOperate.Value, r.RemoveGlobalTimer.Value, r.RandomizeAim.Value, r.ObeyLos.Value, r.Destroyable.Value));
        Assert.Equal((750, -1, -1, -1, -1), (r.DensityRadius.Value, r.DensityMaxType.Value, r.DensityMaxActive.Value, r.DensityMaxInactive.Value, r.MaxTypeByPlayerRegardlessOfTeam.Value));
        Assert.Equal((-1, -1, -1, -1, -1, -1), (r.FrequencyMaxType.Value, r.FrequencyMaxActive.Value, r.FrequencyMaxInactive.Value, r.FrequencyDensityMaxType.Value, r.FrequencyDensityMaxActive.Value, r.FrequencyDensityMaxInactive.Value));
        Assert.Equal((0, 0, -1, 0, 0), (r.ChainedTurretRequiredForBuilding.Value, r.ChainedTurretRequiredForOperation.Value, r.ChainedTurretId.Value, r.ChainedTurretRadius.Value, r.NumChainedTurretsRequired.Value));
        Assert.Equal(("", ""), (r.LogicTakeOwnership.Value, r.LogicStealOwnership.Value));
        Assert.All(Enumerable.Range(0, VehComputerFields.TerrainModifiersCount), k => Assert.Equal(-1, r.Int(VehComputerFields.TerrainModifier(k)).Value));
        Assert.All(r.Products, p => Assert.True(p.IsEmpty));
        Assert.Equal(0, r.ExplodeItemId.Value);
        Assert.All(r.ChildVehicles, c => Assert.Equal(0, c.Value));
    }

    [RequiresOriginalAssetsFact]
    public void SpriteBlobsAndRecordFingerprintArePinned()
    {
        var d = Data.Value;
        Assert.Equal((74, "effects", 75, "gfx00203"), (d.Sprite0BlobName.Index, d.Sprite0BlobName.Value, d.Sprite0BlobId.Index, d.Sprite0BlobId.Value));
        Assert.Equal(("None", "None", "None", "None"), (d.Record.SpriteBaseBlobName.Value, d.Record.SpriteBaseBlobId.Value, d.Record.SpriteTurretBlobName.Value, d.Record.SpriteTurretBlobId.Value));
        Assert.Equal(
            "0=5;1=v53;2=402;3=MeteorShower;6=50;7=100000;33=25;34=5;35=8;36=16;37=1;38=1;39=1;40=1;41=8;42=30;43=-1;45=1;50=1007;65=-1;74=effects;75=gfx00203;76=48;77=2;86=None;87=None;94=1700;95=1150;96=-1;97=-1;98=-1;102=1;103=-1;108=1;109=-1;110=-1;111=-1;112=-1;113=-1;114=-1;115=-1;116=-1;117=-1;118=-1;119=-1;120=-1;121=-1;122=-1;123=-1;124=-1;138=1;139=-1;140=1;141=-1;142=-1;143=-1;144=-1;145=-1;146=-1;147=-1;148=-1;149=-1;150=-1;151=-1;152=-1;153=-1;154=-1;155=-1;156=-1;157=-1;158=-1;159=-1;160=-1;161=-1;162=-1;163=-1;164=-1;165=-1;166=-1;167=-1;168=-1;169=-1;170=-1;171=-1;172=-1;173=-1;174=-1;175=-1;176=-1;177=-1;178=-1;179=-1;180=-1;181=-1;182=-1;183=-1;184=-1;185=-1;186=-1;187=-1;188=-1;189=-1;190=-1;191=-1;192=-1;193=-1;194=-1;195=-1;196=-1;197=-1;198=-1;199=-1;200=-1;201=-1;202=2000;204=360;205=50;206=20000;208=500000;209=20000;210=10;211=25;212=5;213=5;214=1;218=750;219=-1;220=-1;221=-1;222=-1;223=-1;224=-1;225=-1;226=-1;227=-1;228=-1;231=-1;237=None;238=None;245=None;246=None",
            OriginalComputerFieldsTests.Fingerprint(d.Record, 0, 477));
    }

    [RequiresOriginalAssetsFact]
    public void InventoryItem1007ResolvesToTheMeteorChildEffectProjectile()
    {
        var d = Data.Value;
        Assert.Equal(new[] { 1007, 0, 0, 0, 0, 0 }, d.InventoryItems.Select(i => i.Value));
        Assert.Equal(50, d.InventoryItems[0].FieldIndex);
        Assert.Equal((1007, "Meteor", "Child Effects", 6, 72, 227), (d.Weapon.Id, d.Weapon.Name, d.Weapon.Category, d.Weapon.RecordType, d.Weapon.RowNumber, d.Weapon.Record.FieldCount));
        Assert.Equal((1000, 0, -1), (d.Weapon.Mass.Value, d.Weapon.Buy.Value, d.Weapon.Sell.Value));
        // Sell f[15] = -1 is kept raw (negative sentinel candidate, native meaning unvalidated)
        // raw projectile view (names are the existing ITM view names; meanings are not validated for this item)
        Assert.NotNull(d.Projectile);
        var p = d.Projectile!;
        Assert.Equal((35, 0, 4250, 2000, 0, 0, 0, 0, 0, 0),
            (p.FireDelay.Value, p.InheritedSpeed.Value, p.MuzzleVelocity.Value, p.AliveTime.Value, p.MaxLivePerPlayer.Value, p.AntiRecharge.Value, p.AntiFire.Value, p.AntiThrust.Value, p.AntiRotate.Value, p.ExplodeItem.Value));
        // chain: f[31] ammo = 0 and f[142] explodeItem = 0, so no ammo, no children
        Assert.NotNull(d.Chain);
        var c = d.Chain!;
        Assert.Null(c.Ammo);
        Assert.False(c.AmmoIsSelf);
        Assert.Empty(c.Children);
        Assert.Empty(c.ChildEntries);
    }

    [RequiresOriginalAssetsFact]
    public void WeaponRowKeepsEveryNonDefaultRawField()
    {
        var f = Data.Value.Weapon.Record.Fields;
        var print = string.Join(";", f.Select((x, i) => (i, x.Value)).Where(x => x.Value != "0" && x.Value != "").Select(x => $"{x.i}={x.Value}"));
        Assert.Equal(
            "0=6;1=v57;2=1007;3=Meteor;4=Child Effects;5=!%1;7=1000;12=1;13=-1;15=-1;17=44800;18=1;20=6;23=None;24=None;32=1;33=10;54=35;56=-1;62=10000;66=400;71=None;72=None;79=None;80=None;86=1;87=1;93=10000;101=4250;103=1000;105=1000;106=1000;108=56;110=2000;118=2;119=8;120=72;121=300;123=20;125=50;129=72;130=10;141=-1;146=100;148=1;151=56;152=15000;153=5000;171=56;172=750000;174=2;175=None;176=None;177=24;178=1;183=creffects;184=Meteor;190=85;191=None;192=None;199=creffects;200=MeteorTrail;204=-5;205=-5;206=50;207=ExplosionEffects;208=bombhit3_;212=-5;213=-5;214=80;215=None;216=None;217=4;219=WeaponSounds;220=Explode1;221=3;223=None;224=None",
            print);
    }

    [RequiresOriginalAssetsFact]
    public void MissingOrChangedReferencesFailLoudly()
    {
        var vehicles = OriginalVehicleCatalog.Load(OriginalAssets.Root!);
        var items = OriginalItemCatalog.Load(OriginalAssets.Root!);
        var nws = OriginalNws.LoadRogue(OriginalAssets.Root!);
        Assert.Equal(402, OriginalMeteorShower.Resolve(vehicles, items, nws).Record.Id);
        // an ITM catalog without row 1007 must not silently produce a view
        var withoutMeteor = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(
            "6,v57,1,\"Other\",\"Cat\",\"\",\"\",0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0", "ITM/t.itm"));
        Assert.Throws<OriginalItemException>(() => OriginalMeteorShower.Resolve(vehicles, withoutMeteor, nws));
    }
}
