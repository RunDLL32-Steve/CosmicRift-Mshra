using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// Oracle for data/stations/trading_outpost.json: the committed file must equal what <see cref="OriginalStationBuilder"/>
/// derives from the pinned originals (eb1514ea). Set RT_WRITE_DERIVED_STATION=1 to rewrite it. Skipped without the assets.
/// </summary>
public sealed class OriginalStationTests
{
    private static string CommittedPath => Path.Combine(OriginalAssetsRepoRoot(), "data", "stations", "trading_outpost.json");

    private static string OriginalAssetsRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "ROGUE_TRADER_2_PLAN.md"))) return dir.FullName;
        throw new InvalidOperationException("Repository root not found.");
    }

    [RequiresOriginalAssetsFact]
    public void CommittedStationMatchesPinnedOriginals()
    {
        var derived = OriginalStationBuilder.Build(OriginalAssets.Root!, TestWorld.RogueMap, TestWorld.RogueCatalog().Sector.Teams).ToJson();
        if (Environment.GetEnvironmentVariable("RT_WRITE_DERIVED_STATION") == "1") File.WriteAllText(CommittedPath, derived);
        Assert.Equal(derived, File.ReadAllText(CommittedPath).ReplaceLineEndings("\n"));
    }

    [RequiresOriginalAssetsFact]
    public void OriginalValuesAreCarriedUnchanged()
    {
        var station = OriginalStationBuilder.Build(OriginalAssets.Root!, TestWorld.RogueMap, TestWorld.RogueCatalog().Sector.Teams);
        var items = OriginalItemCatalog.Load(OriginalAssets.Root!);

        Assert.Equal(items.Resources.Select(i => (i.Id.ToString(), i.Name, (long)i.Mass.Value)), station.Goods.Select(g => (g.Id, g.Name, g.UnitWeight)));
        Assert.All(station.Goods, g => Assert.Null(g.Buy)); // no original resource has a positive buy price
        Assert.Equal(new[] { ("2004", 55L), ("2006", 80L), ("2008", 300L), ("2020", 20L) }, station.Trades.Select(t => (t.InputItemId, t.CreditsPerUnit)));
        Assert.Equal(new[] { ("39", 5f), ("40", 10f), ("43", 50f) }, station.Repairs.Select(r => (r.Id, r.HullPoints)));
        Assert.Equal(35, station.Upgrades.Count); // 26 RT-F048 family rows + 9 Hull Armor rows
        var boosters = station.Upgrades.Single(u => u.Name == "TurboBoosters");
        Assert.Equal((5000L, 14), (boosters.Price, boosters.HeldCategory));
        Assert.Equal(new[] { 50, 50, 0 }, boosters.RawModifiers);
        Assert.Equal(new[] { -75, -75, -20 }, station.Upgrades.Single(u => u.Name == "Duraplas Armor").RawModifiers);
        Assert.All(station.Upgrades.Where(u => u.Family == "HyperDrive Systems"), u => Assert.Equal(UpgradeEffect.None, u.Effect));
        Assert.NotEmpty(station.NewValues);
    }
}

/// <summary>Upgrade effects and the shipped derived station, independent of the raw assets.</summary>
public sealed class UpgradeEffectsTests
{
    private static StationDefinition Station() => StationDefinition.FromJson(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "stations", "trading_outpost.json")));

    [Fact]
    public void NoUpgrades_LeavesShipUnchanged()
    {
        var ship = new ShipDefinition();
        Assert.Same(ship, UpgradeEffects.Apply(ship, Station(), []));
    }

    [Fact]
    public void Boosters_RaiseThrustAndSpeed_ArmorAddsHullButSlows()
    {
        var station = Station();
        var ship = new ShipDefinition();
        var boosted = UpgradeEffects.Apply(ship, station, [station.Upgrades.Single(u => u.Name == "TurboBoosters").Id]);
        Assert.Equal(ship.Physics.ForwardThrust * 1.05f, boosted.Physics.ForwardThrust, 3);
        Assert.Equal(ship.Physics.ReverseThrust * 1.05f, boosted.Physics.ReverseThrust, 3); // reverse follows forward
        Assert.Equal(ship.Physics.MaximumSpeed * 1.05f, boosted.Physics.MaximumSpeed, 3);

        var armored = UpgradeEffects.Apply(ship, station, [station.Upgrades.Single(u => u.Name == "Duraplas Armor").Id]);
        Assert.Equal(ship.Defense.MaximumHull + 25, armored.Defense.MaximumHull, 3);
        Assert.True(armored.Physics.MaximumSpeed < ship.Physics.MaximumSpeed);
    }

    [Fact]
    public void OnlyBestPricedUpgradePerHeldCategoryIsActive()
    {
        var station = Station();
        var turbo = station.Upgrades.Single(u => u.Name == "TurboBoosters");
        var hyper = station.Upgrades.Single(u => u.Name == "HyperBoosters");
        Assert.Equal(turbo.HeldCategory, hyper.HeldCategory);

        var active = UpgradeEffects.Active(station, [turbo.Id, hyper.Id]);

        Assert.Equal(new[] { hyper.Id }, active.Select(u => u.Id));
    }
}
