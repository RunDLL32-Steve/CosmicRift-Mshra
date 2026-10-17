using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F062 (data clause only): VEH rows 9 and 14 (ids 409 Solar Mold, 414 Mold Spore) beside NWS lines 278/336/337/367 and ITM 1088/2008/3004.
/// Oracle values were read from the pinned VEH/Rogue.veh, ITM/Rogue_mp.itm and NWS/Rogue.nws (eb1514ea). They document raw data and visible
/// mismatches; they do not validate any spawn, combat, drop or hazard behaviour. LIO rows 80/81 are not read here. Asset tests are Skipped without the assets.
/// </summary>
public sealed class OriginalSolarMoldTests
{
    private static readonly Lazy<OriginalSolarMold> Data = new(() => OriginalSolarMold.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void BothRecordsAreValidatedType5RowsWithTheirRawIdentity()
    {
        var d = Data.Value;
        Assert.Equal((409, "Solar Mold", 9), (d.SolarMold.Id, d.SolarMold.Name, d.SolarMold.Record.RowNumber));
        Assert.Equal((414, "Mold Spore", 14), (d.MoldSpore.Id, d.MoldSpore.Name, d.MoldSpore.Record.RowNumber));
        Assert.All(new[] { d.SolarMold, d.MoldSpore }, a => Assert.Equal((5, "v53", 477), (a.Record.Vehicle.RecordType, a.Record.Vehicle.VersionToken, a.Record.Vehicle.FieldCount)));
        Assert.Equal("A unique sort of spaceborn mold, Solar Mold will release spores that are very dangerous to passing spacecraft.", d.SolarMold.Description.Value);
        Assert.True(d.SolarMold.Description.WasQuoted);
        Assert.Equal("Deadly Solar Mold Spore...", d.MoldSpore.Description.Value);
        Assert.Equal(new[] { ("SpaceMold", "spacePlantV0"), ("None", "None") }, new[] { (d.SolarMold.SpriteBase.Name.Value, d.SolarMold.SpriteBase.Id.Value), (d.SolarMold.SpriteTurret.Name.Value, d.SolarMold.SpriteTurret.Id.Value) });
        Assert.Equal(new[] { ("None", "None"), ("SpaceMold", "spacePlantSporeV0") }, new[] { (d.MoldSpore.SpriteBase.Name.Value, d.MoldSpore.SpriteBase.Id.Value), (d.MoldSpore.SpriteTurret.Name.Value, d.MoldSpore.SpriteTurret.Id.Value) });
    }

    [RequiresOriginalAssetsFact]
    public void RawCombatFieldsArePinned()
    {
        var d = Data.Value;
        var m = d.SolarMold.Combat;
        var s = d.MoldSpore.Combat;
        // Hitpoints f6, RepairRate f210, HitpointsRequiredToOperate f211, Destroyable f217, RelativeId f137
        Assert.Equal((500, 100, 0, 1, 20), (m.Hitpoints.Value, m.RepairRate.Value, m.HitpointsRequiredToOperate.Value, m.Destroyable.Value, m.RelativeId.Value));
        Assert.Equal((75, 50, 5, 1, 0), (s.Hitpoints.Value, s.RepairRate.Value, s.HitpointsRequiredToOperate.Value, s.Destroyable.Value, s.RelativeId.Value));
        Assert.Equal((6, 210, 211, 217, 137), (m.Hitpoints.FieldIndex, m.RepairRate.FieldIndex, m.HitpointsRequiredToOperate.FieldIndex, m.Destroyable.FieldIndex, m.RelativeId.FieldIndex));
        // BarrelLength f33, PhysicalRadius f35, TriggerRadius f36
        Assert.Equal((0, 60, 50), (m.BarrelLength.Value, m.PhysicalRadius.Value, m.TriggerRadius.Value));
        Assert.Equal((15, 36, 36), (s.BarrelLength.Value, s.PhysicalRadius.Value, s.TriggerRadius.Value));
        // EnergyRate f95, EnergyMin f96
        Assert.Equal((0, 0), (m.EnergyRate.Value, m.EnergyMin.Value));
        Assert.Equal((500, 1000), (s.EnergyRate.Value, s.EnergyMin.Value));
        // RotateSpeed f202, TrackingTime f205, TrackingRadius f206, TrackingWeightHigh f208, FireRadius f209, ObeyLos f214
        Assert.Equal((0, 0, 0, 0, 0, 0), (m.RotateSpeed.Value, m.TrackingTime.Value, m.TrackingRadius.Value, m.TrackingWeightHigh.Value, m.FireRadius.Value, m.ObeyLos.Value));
        Assert.Equal((1500, 25, 2500, 10000000, 2500, 1), (s.RotateSpeed.Value, s.TrackingTime.Value, s.TrackingRadius.Value, s.TrackingWeightHigh.Value, s.FireRadius.Value, s.ObeyLos.Value));
        // ownership logic f234 / f235: empty on the mold, "!" on the spore
        Assert.Equal(("", ""), (d.SolarMold.LogicTakeOwnership.Value, d.SolarMold.LogicStealOwnership.Value));
        Assert.Equal(("!", "!"), (d.MoldSpore.LogicTakeOwnership.Value, d.MoldSpore.LogicStealOwnership.Value));
        Assert.Equal((234, 235), (d.MoldSpore.LogicTakeOwnership.Index, d.MoldSpore.LogicStealOwnership.Index));
    }

    [RequiresOriginalAssetsFact]
    public void RawSpawnLimitFieldsArePinned()
    {
        var d = Data.Value;
        // f218 DensityRadius, f219 DensityMaxType, f220 Active, f221 Inactive, f222 MaxTypeByPlayerRegardlessOfTeam,
        // f223 FrequencyMaxType, f224 Active, f225 Inactive, f226 FrequencyDensityMaxType, f227 Active, f228 Inactive
        static int[] Values(OriginalMoldSpawnLimits l) => new[]
        {
            l.DensityRadius.Value, l.DensityMaxType.Value, l.DensityMaxActive.Value, l.DensityMaxInactive.Value, l.MaxTypeByPlayerRegardlessOfTeam.Value,
            l.FrequencyMaxType.Value, l.FrequencyMaxActive.Value, l.FrequencyMaxInactive.Value,
            l.FrequencyDensityMaxType.Value, l.FrequencyDensityMaxActive.Value, l.FrequencyDensityMaxInactive.Value,
        };
        Assert.Equal(new[] { 0, 0, 0, 0, -1, 0, 0, 0, 0, 0, 0 }, Values(d.SolarMold.SpawnLimits));
        Assert.Equal(new[] { 3500, 4, -1, -1, -1, 8, -1, -1, 4, -1, -1 }, Values(d.MoldSpore.SpawnLimits));
        Assert.Equal((218, 228), (d.MoldSpore.SpawnLimits.DensityRadius.FieldIndex, d.MoldSpore.SpawnLimits.FrequencyDensityMaxInactive.FieldIndex));
    }

    [RequiresOriginalAssetsFact]
    public void ProductBlocksAreAllEmptyAndNothingElseReferencesTheMoldIds()
    {
        var d = Data.Value;
        foreach (var a in new[] { d.SolarMold, d.MoldSpore })
        {
            Assert.Equal(16, a.Record.Products.Count);
            Assert.All(a.Record.Products, p => Assert.True(p.IsEmpty));
            Assert.Empty(a.NonEmptyProducts);
            Assert.All(a.Record.ChildVehicles, c => Assert.Equal(0, c.Value));
            Assert.Empty(a.OtherFieldsEqualToId);
        }
    }

    [RequiresOriginalAssetsFact]
    public void ReferencedItemsResolveInItm()
    {
        var d = Data.Value;
        Assert.Empty(d.SolarMold.ItemReferences);
        Assert.Equal(new[] { 0, 0, 0, 0, 0, 0 }, d.SolarMold.Record.InventoryItems.Select(i => i.Value));
        Assert.Equal(new[] { 1088, 0, 0, 0, 0, 0 }, d.MoldSpore.Record.InventoryItems.Select(i => i.Value));
        Assert.Equal(
            new[] { ("InventoryItems0", 50, 1088, "MoldSporeBeam", 6, "SpecialEffects"), ("ExplodeItemId", 64, 3004, "SpaceStationExplode", 8, "Effects"), ("DropItemId", 68, 2008, "Wreckage", 4, "Resources") },
            d.MoldSpore.ItemReferences.Select(r => (r.FieldRole, r.FieldIndex, r.ItemId, r.Item.Name, r.Item.RecordType, r.Item.Category)));
        Assert.Equal(1, d.MoldSpore.Record.DropItemQuantity.Value);
        Assert.Equal((0, 0, 0), (d.SolarMold.Record.ExplodeItemId.Value, d.SolarMold.Record.DropItemId.Value, d.SolarMold.Record.DropItemQuantity.Value));
    }

    [RequiresOriginalAssetsFact]
    public void NwsLinesAreVerbatimUnderTheirDatedHeadings()
    {
        var d = Data.Value;
        Assert.Equal(
            new[]
            {
                (264, "********2/26/03", 278, "More than 1 Solar Mold will now spawn. In addition, few number of spores/sprites will spawn around them."),
                (334, "********2/19/03", 336, "Mold Spores drop prizes upon destruction."),
                (334, "********2/19/03", 337, "Mold Sprites have 35 HP now."),
                (365, "********5/2/02", 367, "Fixed Mold Spores and Killsat/Deathorb Spawns."),
            },
            d.Nws.Select(n => (n.Heading.LineNumber, n.Heading.Text, n.Line.LineNumber, n.Line.Text)));
        Assert.Equal(337, d.NwsLine(OriginalSolarMold.NwsSpriteHitpoints).Line.LineNumber);
        Assert.Throws<KeyNotFoundException>(() => d.NwsLine(295));
    }

    [RequiresOriginalAssetsFact]
    public void VisibleMismatchesAreRecordedAndNothingIsCorrected()
    {
        var d = Data.Value;
        Assert.Equal(new[]
        {
            "NWS line 337 (********2/19/03) says 'Mold Sprites have 35 HP now' but VEH 414 Mold Spore f[6] Hitpoints=75; no VEH row is named Sprite, the link between 'sprites' and this row is unproven.",
            "NWS line 336 says Mold Spores drop prizes; VEH 414 f[68] DropItemId=2008 x1 resolves in ITM to 'Wreckage' (VEH id 2008 is a different row in VEH); whether that is the prize is unproven.",
            "NWS line 367 sits under heading '********5/2/02' (2002 if read as M/D/YY), outside the 2003 window the catalog row names.",
            "NWS line 278 (********2/26/03) says more than 1 Solar Mold spawns with spores/sprites around them, but VEH 409 has Density/Frequency spawn-limit fields f[218]..f[228] DensityMaxType, FrequencyMaxType, FrequencyDensityMaxType all 0 (the limits sit on VEH 414: 4/8/4); mold count, spawn time and radius live in LIO, which is not read here.",
            "VEH 409 has no non-empty product block and no item or vehicle reference to 414; how it 'releases spores' is not expressed in its VEH row.",
            "f[137] RelativeId is 20 on VEH 409 but 0 on VEH 414; the catalog's 'faction 20' cannot be read from the spore row and the meaning of RelativeId is unvalidated.",
        }, d.VisibleMismatches);
        // raw values stay untouched by the notes
        Assert.Equal(75, d.MoldSpore.Combat.Hitpoints.Value);
    }
}
