using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: FEATURE_CATALOG.md RT-F056 (ITM46/47/48/49 rows 170-172/179; VEH 8/10-12) plus an independent Python audit of the
/// pinned Rogue_mp.itm and Rogue.veh (eb1514ea). Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalProductionItemTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void FourItemsLinkToTheirVehicleRowsWithRawFieldsKept()
    {
        var p = OriginalProductionItems.Load(OriginalAssets.Root!, Catalog.Value);
        Assert.Equal(new[] { 170, 171, 172, 179 }, p.Select(x => x.Item.RowNumber));
        Assert.Equal(new[] { 46, 47, 48, 49 }, p.Select(x => x.Item.Id));
        Assert.Equal(new[] { "Micro Refinery", "GunSat", "BeamSat", "Micro Repair Dock" }, p.Select(x => x.Item.Name));
        Assert.Equal(new[] { 412, 410, 411, 408 }, p.Select(x => x.VehicleLink.Value));
        Assert.Equal(new[] { 8, 10, 11, 12 }.OrderBy(x => x), p.Select(x => x.Vehicle.RowNumber).OrderBy(x => x));
        Assert.Equal(new[] { "Micro Refinery", "GunSat", "BeamSat", "Micro Repair Dock" }, p.Select(x => x.VehicleName));
        Assert.Equal(new[] { 2000000, 500000, 500000, 2000000 }, p.Select(x => x.Item.Record.Int(7).Value));
        Assert.All(p, x => { Assert.Equal(0, x.Item.Buy.Value); Assert.Equal(-1, x.Item.Record.Int(13).Value); Assert.Equal(94, x.Item.Record.FieldCount); Assert.Equal("Equipment", x.Item.Category); });
    }
}
