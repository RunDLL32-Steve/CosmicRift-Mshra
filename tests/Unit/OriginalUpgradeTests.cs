using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Oracle: independent Python audit of the pinned Rogue_mp.itm (eb1514ea). The catalog text for RT-F048 lists some rows
/// one off (engine "rows 17-19" are rows 18-20); the audited rows are pinned here. Skipped, never passed, without assets.
/// </summary>
public sealed class OriginalUpgradeTests
{
    private static readonly Lazy<OriginalItemCatalog> Catalog = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    [RequiresOriginalAssetsFact]
    public void FamilyRowsAndCounts()
    {
        var all = OriginalUpgrade.All(Catalog.Value);
        Assert.Equal(new[] { 18, 19, 20, 64, 65, 193, 194, 195 }, all.Where(u => u.Family == "Engine Mods").Select(u => u.Item.RowNumber));
        Assert.Equal(new[] { 24, 25, 92, 131, 132, 133 }, all.Where(u => u.Family == "Thruster Mods").Select(u => u.Item.RowNumber));
        Assert.Equal(new[] { 26, 27, 28, 29, 91, 93 }, all.Where(u => u.Family == "Power Mods").Select(u => u.Item.RowNumber));
        Assert.Equal(new[] { 110, 111, 116, 117, 118, 119 }, all.Where(u => u.Family == "HyperDrive Systems").Select(u => u.Item.RowNumber));
        Assert.Equal(26, all.Count);
    }

    [RequiresOriginalAssetsFact]
    public void RawPricesLogicAndModifierBlocks()
    {
        var by = OriginalUpgrade.All(Catalog.Value).ToDictionary(u => u.Item.Name);
        Assert.Equal((10000, 7500, "10|1|3|15|16"), (by["SuperBoosters"].Item.Buy.Value, by["SuperBoosters"].Item.Sell.Value, by["SuperBoosters"].Logic));
        Assert.Equal(new[] { 80, 100, 0 }, by["SuperBoosters"].ModifierBlock.Select(v => v.Value));
        Assert.Equal(new[] { 0, 75, 0 }, by["Turbo AfterBurners"].ModifierBlock.Select(v => v.Value));
        Assert.Equal(new[] { 0, 0, 150 }, by["Bio Reactor Pods"].ModifierBlock.Select(v => v.Value));
        Assert.Equal(new[] { 0, 0, 0 }, by["Super Capacitor"].ModifierBlock.Select(v => v.Value));
        Assert.Equal(15000, by["Super Capacitor"].Item.Buy.Value);
        Assert.Equal("", by["TurboBoosters"].Logic);
        // Hyper abilities are type 17 (113 fields), HyperCore is type 4; neither has the type-13 block.
        Assert.Empty(by["Hyper Jump"].ModifierBlock);
        Assert.Equal(17, by["Hyper Jump"].Item.RecordType);
        Assert.Equal(4, by["HyperCore"].Item.RecordType);
        Assert.Equal("!1&!2&!3&!4&!5&!6&!8&!9&!10&!11&!15&!16", by["Hyper Beacon System"].Logic);
    }

    [RequiresOriginalAssetsFact]
    public void PowerAndManeuverThrusterRowsHoldZeroModifierBlocks()
    {
        // Capacitor/Supercharger and Maneuver Thruster rows hold 0,0,0 in the modifier block: their effect lives in other fields (undecoded).
        var all = OriginalUpgrade.All(Catalog.Value).Where(u => u.Item.RecordType == 13).ToList();
        var zero = all.Where(u => u.ModifierBlock.All(v => v.Value == 0)).Select(u => u.Item.Name).ToList();
        Assert.Contains("Turbo Capacitor", zero);
        Assert.Contains("Turbo Maneuver Thrusters", zero);
        Assert.Contains("Bio Repellor Pods", zero);
        Assert.Equal(20, all.Count);
    }
}
