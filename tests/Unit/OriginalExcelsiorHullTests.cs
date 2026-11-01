using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle for RT-F002 "Excelsior": RPG/Rogue.rpg row 2 linked to VEH/Rogue.veh row 28. The values below come from an independent
/// read of the pinned files (comma split with quote handling, run outside the repo code, assets pinned eb1514ea) and are asserted
/// against the shared <see cref="OriginalHullCatalog"/> loader, which is not modified. Data/reader clause only: no field is
/// interpreted beyond the names already in OriginalHull, and nothing here exercises behavior. Tests are Skipped without assets.
/// </summary>
public sealed class OriginalExcelsiorHullTests
{
    private static readonly Lazy<OriginalHullCatalog> Catalog = new(() => OriginalHullCatalog.Load(OriginalAssets.Root!));

    private static OriginalHull Excelsior => Catalog.Value.ByName("Excelsior");

    [RequiresOriginalAssetsFact]
    public void RpgRowRetainsEveryRawField()
    {
        var h = Excelsior;
        Assert.Equal(2, h.RpgRowNumber);
        Assert.Equal("RPG/Rogue.rpg", h.RpgRecord.SourceFile);
        Assert.Equal("v6", h.RpgRecord.VersionToken);
        Assert.Equal(51, h.RpgRecord.FieldCount);
        Assert.Equal(15, h.SkillId);
        Assert.Equal(2011, h.VehicleId);
        Assert.Equal("Ship Hull - Terran", h.Category);
        // First five fields "v6,15,1,2011,1", then the zero/one block, then f[38]=3499 before the quoted name.
        Assert.Equal(["v6", "15", "1", "2011", "1"], h.RpgRecord.Fields.Take(5).Select(f => f.Value));
        Assert.Equal("1", h.RpgRecord.Value(7));
        Assert.Equal("3499", h.RpgRecord.Value(38));
        // Trailing text/asset-name fields are kept raw (meaning of f[42..44] not established here).
        Assert.Equal(["Excelsior", "Ship Hull - Terran", "", "Terran Heavy Fighter", "ShipSelectMov", "ss_excelsior", "0", "0", "0", "0", "0", "550"],
            Enumerable.Range(39, 12).Select(i => h.RpgRecord.Value(i)));
    }

    [RequiresOriginalAssetsFact]
    public void VehRowIdentityAndScalarFields()
    {
        var h = Excelsior;
        Assert.Equal(28, h.VehRowNumber);
        Assert.Equal("VEH/Rogue.veh", h.VehRecord.SourceFile);
        Assert.Equal("v53", h.VehRecord.VersionToken);
        Assert.Equal(454, h.VehRecord.FieldCount);
        Assert.Equal("2", h.VehRecord.Value(0));
        Assert.Equal("2011", h.VehRecord.Value(2));
        Assert.Equal("Excelsior", h.VehRecord.Value(3));
        Assert.Equal("Terran Heavy Fighter", h.VehRecord.Value(5));

        Assert.Equal(new OriginalInt(6, 50, false), h.HullPoints);
        Assert.Equal(new OriginalInt(7, 10000, false), h.Weight);
        Assert.Equal(new OriginalInt(47, 1500000, false), h.NormalWeight);
        Assert.Equal(new OriginalInt(48, 3000000, false), h.StopWeight);
        Assert.Equal(new OriginalInt(68, 2008, false), h.DropItemId);
        Assert.Equal(new OriginalInt(69, 1, false), h.DropQuantity);
        Assert.Equal(new OriginalInt(94, 1700, false), h.EnergyMax);
        Assert.Equal(new OriginalInt(95, 1050, false), h.EnergyRate);
        Assert.Equal(new OriginalInt(96, 1700, false), h.EnergyMin);
        Assert.Equal(new OriginalInt(362, 727, false), h.BouncePercent);
        Assert.Equal(new OriginalInt(373, 1100, false), h.HyperEnergyCost);
    }

    [RequiresOriginalAssetsFact]
    public void TerrainZeroSpeedBlockAndRollAssets()
    {
        var h = Excelsior;
        var t = h.Terrain0;
        Assert.Equal(202, t.FirstFieldIndex);
        Assert.Equal(0, t.RollAllowed.Value);
        Assert.Equal(0, t.RollFriction.Value);
        Assert.Equal(45, t.RollThrust.Value);
        Assert.Equal(1380, t.RollRotate.Value);
        Assert.Equal(3575, t.RollTopSpeed.Value);
        Assert.Equal(22, t.StrafeThrust.Value);
        Assert.Equal(3800, t.HyperTopSpeed.Value);
        Assert.Equal(50, t.HyperThrust.Value);
        Assert.Equal(new OriginalInt(210, -1, true), t.BackwardThrust);
        Assert.Equal(new OriginalInt(211, -1, true), t.BackwardHyperThrust);
        Assert.Equal(["TerranShipsSmall", "Excelsior", "0", "0", "0", "0", "5", "0"],
            Enumerable.Range(382, 8).Select(i => h.VehRecord.Value(i)));
    }

    [RequiresOriginalAssetsFact]
    public void DiffersFromAspOnlyInKnownRawFields()
    {
        // Independent audit: the complete set of VEH fields where Excelsior differs from Asp. Fields 367, 368 and 413 are
        // returned raw; their meaning is not established (no claim is made here).
        var asp = Catalog.Value.ByName("Asp").VehRecord;
        var exc = Excelsior.VehRecord;
        Assert.Equal(asp.FieldCount, exc.FieldCount);
        var diffs = Enumerable.Range(0, exc.FieldCount)
            .Where(i => asp.Value(i) != exc.Value(i))
            .Select(i => (i, asp.Value(i), exc.Value(i)))
            .ToArray();
        Assert.Equal(
        [
            (2, "2002", "2011"),
            (3, "Asp", "Excelsior"),
            (5, "Terran Special Ops AeroSpace Fighter", "Terran Heavy Fighter"),
            (48, "2500000", "3000000"),
            (206, "3250", "3575"),
            (208, "3500", "3800"),
            (367, "0", "11"),
            (368, "18", "14"),
            (383, "Asp", "Excelsior"),
            (413, "51", "0"),
        ], diffs);
    }
}
