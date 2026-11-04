using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Persistence;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// Oracle for the twelve original hull files in data/ships and data/ships/roster.json: the committed files must equal what
/// <see cref="OriginalShipBuilder"/> derives from the pinned originals (eb1514ea). Set RT_WRITE_DERIVED_SHIPS=1 to rewrite
/// them. Skipped without the assets.
/// </summary>
public sealed class OriginalShipTests
{
    private static string ShipsDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "ROGUE_TRADER_2_PLAN.md"))) return Path.Combine(dir.FullName, "data", "ships");
        throw new InvalidOperationException("Repository root not found.");
    }

    private static OriginalHullCatalog Hulls => OriginalHullCatalog.Load(OriginalAssets.Root!);
    private static OriginalItemCatalog Items => OriginalItemCatalog.Load(OriginalAssets.Root!);
    private static string WeaponsDir() => Path.Combine(Path.GetDirectoryName(ShipsDir())!, "weapons");

    [RequiresOriginalAssetsFact]
    public void CommittedShipsAndRosterMatchPinnedOriginals()
    {
        var write = Environment.GetEnvironmentVariable("RT_WRITE_DERIVED_SHIPS") == "1";
        var expected = Hulls.Hulls.Select(h => (OriginalShipBuilder.FileName(h), OriginalShipBuilder.ToJson(OriginalShipBuilder.Ship(h, Items))))
            .Append(("roster.json", OriginalShipBuilder.ToJson(OriginalShipBuilder.Roster(Hulls, Items)))).ToArray();
        var weapons = OriginalWeaponBuilder.StartingWeapons(Hulls, Items)
            .Select(w => (Path.Combine(WeaponsDir(), OriginalWeaponBuilder.FileName(w)), OriginalWeaponBuilder.ToJson(OriginalWeaponBuilder.Weapon(w))));
        foreach (var (file, json) in expected.Select(e => (Path.Combine(ShipsDir(), e.Item1), e.Item2)).Concat(weapons))
        {
            var path = file;
            if (write) File.WriteAllText(path, json);
            Assert.Equal(json, File.ReadAllText(path).ReplaceLineEndings("\n"));
        }
    }

    [RequiresOriginalAssetsFact]
    public void EveryHullCarriesItsOriginalFlightHoldAndHullValues()
    {
        var hulls = Hulls;
        Assert.Equal(12, hulls.Hulls.Count);
        foreach (var hull in hulls.Hulls)
        {
            var ship = OriginalShipBuilder.Ship(hull, Items);
            Assert.Equal(OriginalShipPhysics.From(hull), ship.Physics);
            var rawStrafe = hull.TerrainSpeedBlocks[0].StrafeThrust;
            Assert.Equal(22, rawStrafe.Value);
            Assert.Equal(207, rawStrafe.FieldIndex);
            Assert.Equal(82.5f, ship.Physics.StrafeThrust / ship.Physics.Mass);
            Assert.Equal(8250f, ship.Physics.StrafeThrust);
            var generated = OriginalShipBuilder.ToJson(ship);
            Assert.Equal(ship.Physics.StrafeThrust, GameCatalog.ShipFromJson(generated).Physics.StrafeThrust);
            Assert.Equal(hull.StopWeight.Value, ship.Cargo.Capacity);
            Assert.Equal(hull.HullPoints.Value, ship.Defense.MaximumHull);
            Assert.Equal(hull.Name, ship.Name);
            Assert.NotEmpty(ship.NewValues!);
        }
        // Spot values (G10 pins the flight; StopWeight differs by hull).
        Assert.Equal(4_000_000, OriginalShipBuilder.Ship(hulls.ByName("Ravager"), Items).Cargo.Capacity);
        Assert.Equal(3_000_000, OriginalShipBuilder.Ship(hulls.ByName("Excelsior"), Items).Cargo.Capacity);
        Assert.Equal(357.5f, OriginalShipBuilder.Ship(hulls.ByName("Excelsior"), Items).Physics.MaximumSpeed);
    }

    [RequiresOriginalAssetsFact]
    public void FullRepairCostsFiveHundredCreditsAndRestoresAFullHullOnEveryHull()
    {
        // Steve (Nov 4 2026): a damaged ship keeps its damage across a relaunch or ship pick; the repair costs 500 credits.
        var fullRepair = OriginalStationBuilder.Build(OriginalAssets.Root!, TestWorld.RogueMap, TestWorld.RogueCatalog().Sector.Teams).Repairs.Single(r => r.Name == "Full Repair");
        Assert.Equal(500, fullRepair.Price);
        Assert.All(Hulls.Hulls, h => Assert.True(fullRepair.HullPoints >= OriginalShipBuilder.Ship(h, Items).Defense.MaximumHull, h.Name));
        // It fills any hull, including the legacy Interceptor's 100 points (review R3-1); Minor and Major add their points.
        var station = OriginalStationBuilder.Build(OriginalAssets.Root!, TestWorld.RogueMap, TestWorld.RogueCatalog().Sector.Teams);
        Assert.Equal(new[] { "43" }, station.Repairs.Where(r => r.RestoresFullHull).Select(r => r.Id));
        var session = new StationProfileSession(new InMemoryProfileRepository(), station, "starter_interceptor", 4_000_000);
        Assert.Equal(100f, session.RepairHullPoints("43", 100f));
        Assert.Equal(5f, session.RepairHullPoints("39", 100f));
    }

    [RequiresOriginalAssetsFact]
    public void ShippedRosterAndDefinitionsLoadAsGameCatalogs()
    {
        var roster = ShipRoster.FromJson(File.ReadAllText(Path.Combine(ShipsDir(), "roster.json")));
        Assert.Equal("2002", roster.StarterHullId); // the Asp, RPG row 1
        Assert.Equal(13, roster.Ships.Count);       // twelve originals plus the legacy Interceptor
        Assert.Contains(roster.Ships, s => s.HullId == OriginalShipBuilder.LegacyStarterHullId);
    }

    [RequiresOriginalAssetsFact]
    public void TheStationSellsEachHullAtItsOriginalPriceToAPilotOfTheSameRace()
    {
        var hulls = Hulls;
        var ships = OriginalShipBuilder.StationShips(hulls, OriginalItemCatalog.Load(OriginalAssets.Root!));
        Assert.Equal(12, ships.Count);
        Assert.All(ships, s => Assert.Equal(15000, s.Price));
        string Id(string name) => OriginalShipBuilder.HullId(hulls.ByName(name));
        var hornet = ships.Single(s => s.Name == "Hornet");
        Assert.Equal(new[] { Id("Asp"), Id("Excelsior"), Id("Ravager"), OriginalShipBuilder.LegacyStarterHullId }, hornet.RequiresOneOf);
        var ripPir = ships.Single(s => s.Name == "Rip'Pir");
        Assert.Equal(new[] { Id("Rak'Kir"), Id("Zap'Pir"), Id("Zag'Gir") }, ripPir.RequiresOneOf);
        // The legacy Interceptor flies as an Asp, so it opens every Terran hull, the Asp included, and nothing else.
        Assert.Contains(OriginalShipBuilder.LegacyStarterHullId, ships.Single(s => s.Name == "Asp").RequiresOneOf!);
        Assert.Equal(4, ships.Count(s => s.RequiresOneOf!.Contains(OriginalShipBuilder.LegacyStarterHullId)));
        Assert.All(ships, s => Assert.All(s.RequiresOneOf!.Where(r => r != OriginalShipBuilder.LegacyStarterHullId),
            r => Assert.Equal(hulls.Hulls.Single(h => OriginalShipBuilder.HullId(h) == s.HullId).Category, hulls.Hulls.Single(h => OriginalShipBuilder.HullId(h) == r).Category)));
    }

    [RequiresOriginalAssetsFact]
    public async Task ATerranPilotCanBuyATerranHullButNotAGraziOne()
    {
        var station = OriginalStationBuilder.Build(OriginalAssets.Root!, TestWorld.RogueMap, TestWorld.RogueCatalog().Sector.Teams);
        var hulls = Hulls;
        string Id(string name) => OriginalShipBuilder.HullId(hulls.ByName(name));
        var funded = StationDefinition.FromJson(System.Text.Json.Nodes.JsonNode.Parse(station.ToJson())!.AsObject().Also(o => o["startingWallet"] = 40_000).ToJsonString()); // NEW fixture wallet
        var session = new StationProfileSession(new InMemoryProfileRepository(), funded, Id("Asp"), TestCargo.Capacity);
        Assert.True((await session.OpenOrCreateAsync("terran-pilot")).Succeeded);
        Assert.Equal(RejectionReason.ShipRequirementNotMet, (await session.BuyShipAsync(Id("Rip'Pir"), funded.X, funded.Y)).Reason);
        Assert.Equal(TransactionStatus.Applied, (await session.BuyShipAsync(Id("Hornet"), funded.X, funded.Y)).Status);
        Assert.Equal(25_000, session.Profile!.Wallet);
        Assert.Equal(RejectionReason.ShipAlreadyOwned, (await session.BuyShipAsync(Id("Asp"), funded.X, funded.Y)).Reason);
    }
}

internal static class JsonObjectExtensions
{
    public static System.Text.Json.Nodes.JsonObject Also(this System.Text.Json.Nodes.JsonObject o, Action<System.Text.Json.Nodes.JsonObject> change)
    {
        change(o);
        return o;
    }
}
