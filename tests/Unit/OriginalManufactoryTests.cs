using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: FEATURE_CATALOG.md RT-F057 (VEH15-17 ids 425/426/427; ITM196-198 ids 83/84/85) plus an independent Python audit of the
/// pinned Rogue_mp.itm and Rogue.veh (eb1514ea). Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalManufactoryTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    private static IReadOnlyList<OriginalManufactory> Load() => OriginalManufactories.Load(OriginalAssets.Root!, Catalog.Value);

    [RequiresOriginalAssetsFact]
    public void ThreeManufactoriesLinkVehicleRowsAndRequireNanobotControl()
    {
        var m = Load();
        Assert.Equal(new[] { 196, 197, 198 }, m.Select(x => x.Item.RowNumber));
        Assert.Equal(new[] { 83, 84, 85 }, m.Select(x => x.Item.Id));
        Assert.Equal(new[] { 15, 16, 17 }, m.Select(x => x.Vehicle.RowNumber));
        Assert.Equal(new[] { "425", "426", "427" }, m.Select(x => x.Vehicle.Value(2)));
        Assert.All(m, x =>
        {
            Assert.Equal(2023, x.RequiredComputerId.Value);
            Assert.Equal(1, x.Item.Record.Int(OriginalManufactories.RequiredComputerFlagField).Value);
            Assert.Equal("Nanobot Control System", x.RequiredComputer!.Name);
            Assert.Equal(199, x.RequiredComputer.RowNumber);
        });
        Assert.Equal(new[] { 1200, 1000, 750 }, m.Select(x => x.Item.Buy.Value));
        Assert.Equal("!1&!2&!3&!4&!5&!6&!8&!9&!10&!11&!15&!16", m[1].Item.Record.Value(5));
    }

    [RequiresOriginalAssetsFact]
    public void RecipeBlocksKeepRawQuantitiesAndVisibleMismatches()
    {
        var m = Load();
        Assert.Equal(new[] { 9, 5, 2 }, m.Select(x => x.Recipes.Count));
        var bio = m[0].Recipes[0];
        Assert.Equal(("Produce Bio Missiles 2:1 RT", 2012, 2, 150, 2004, 1), (bio.Name, bio.OutputItemId.Value, bio.OutputQuantity.Value, bio.RawField4.Value, bio.InputItemId.Value, bio.InputQuantity.Value));
        Assert.Equal("Bio Missiles", bio.Output!.Name);
        Assert.Equal("Refined Tellerium", bio.Input!.Name);
        var storm = m[0].Recipes[1];
        Assert.Equal("Produce Biostorm Missiles 1:1 RT", storm.Name);
        Assert.Equal(-1, storm.InputQuantity.Value);
        var slugs = m[1].Recipes.Single(r => r.Name.Contains("Pulse Cannon"));
        Assert.Equal(2021, slugs.OutputItemId.Value);
        Assert.Null(slugs.Output);
        Assert.Equal(new[] { ("Produce GunSat 1:25 A", 47, 1, 2020, 25), ("Produce BeamSat 1:35 A", 48, 1, 2020, 35) },
            m[2].Recipes.Select(r => (r.Name, r.OutputItemId.Value, r.OutputQuantity.Value, r.InputItemId.Value, r.InputQuantity.Value)));
        Assert.All(m.SelectMany(x => x.Recipes), r => Assert.Equal(150, r.RawField4.Value));
    }
}
