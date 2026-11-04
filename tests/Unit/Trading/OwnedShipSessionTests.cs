using System.Text.Json.Nodes;
using RogueTrader.GameData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// Owned ships at the client boundary (Steve, Nov 3 2026): the shipped roster, hull prices at a station, ship selection and the
/// destruction penalty through <see cref="StationProfileSession"/>. Hull ids and prices below are NEW test fixtures.
/// </summary>
public sealed class OwnedShipSessionTests
{
    private const string Profile = "owned-ships-pilot";
    private const string Starter = "starter_interceptor";
    private const string SecondHull = "fixture-second-hull";

    private static string DataFile(params string[] parts) => Path.Combine([AppContext.BaseDirectory, "data", .. parts]);

    /// <summary>The shipped station, plus one NEW fixture hull for sale at a NEW fixture price and a NEW fixture starting wallet.</summary>
    private static StationDefinition StationSellingASecondHull()
    {
        var json = JsonNode.Parse(File.ReadAllText(DataFile("stations", "trading_outpost.json")))!.AsObject();
        json["startingWallet"] = 1000; // NEW test fixture wallet (the shipped outpost starts pilots at 0)
        // Replaces the shipped original hulls with one NEW fixture hull that has no requirement.
        json["ships"] = new JsonArray(new JsonObject { ["hullId"] = SecondHull, ["name"] = "Fixture Hull", ["price"] = 900, ["source"] = "NEW test fixture" });
        return StationDefinition.FromJson(json.ToJsonString());
    }

    private static async Task<StationProfileSession> OpenAsync(StationDefinition station, IProfileRepository? repo = null)
    {
        var session = new StationProfileSession(repo ?? new InMemoryProfileRepository(), station, Starter, TestCargo.Capacity);
        Assert.True((await session.OpenOrCreateAsync(Profile)).Succeeded);
        return session;
    }

    [Fact]
    public void TheShippedRosterFliesTheStarterAndEveryDefinitionMatchesItsHull()
    {
        var roster = ShipRoster.FromJson(File.ReadAllText(DataFile("ships", "roster.json")));
        Assert.Equal("2002", roster.StarterHullId); // the original Asp
        Assert.NotNull(roster.Find(Starter));       // older saves keep flying the Interceptor
        foreach (var entry in roster.Ships)
        {
            var shipJson = File.ReadAllText(DataFile("ships", entry.Definition));
            var catalog = GameCatalog.FromJson(shipJson, File.ReadAllText(DataFile("weapons", roster.WeaponDefinitionFor(GameCatalog.ShipFromJson(shipJson)))),
                File.ReadAllText(DataFile("sectors", "first_contact.json")));
            Assert.Equal(entry.HullId, catalog.Ship.Id);
        }
    }

    [Theory]
    [InlineData("""{ "starterHullId": "a", "ships": [] }""")]
    [InlineData("""{ "starterHullId": "b", "ships": [ { "hullId": "a", "definition": "a.json" } ] }""")]
    [InlineData("""{ "starterHullId": "a", "ships": [ { "hullId": "a", "definition": "a.json" }, { "hullId": "a", "definition": "b.json" } ] }""")]
    [InlineData("""{ "starterHullId": "a", "ships": [ { "hullId": "a", "definition": "../a.json" } ] }""")]
    [InlineData("""{ "starterHullId": "a", "ships": [ { "hullId": "a", "definition": "a.txt" } ] }""")]
    public void ARosterWithoutItsStarterDuplicatesOrUnsafePathsIsRefused(string json) =>
        Assert.ThrowsAny<Exception>(() => ShipRoster.FromJson(json));

    [Fact]
    public void TheShippedStationRoundTripsItsHullsAndPrices()
    {
        var text = File.ReadAllText(DataFile("stations", "trading_outpost.json"));
        var station = StationDefinition.FromJson(text);
        Assert.Equal(text.ReplaceLineEndings("\n"), station.ToJson());
        Assert.Equal(12, station.Ships!.Count);
        var table = station.CreatePriceTable();
        Assert.All(station.Ships, s => Assert.True(table.TryGetShipPrice(station.StationId, s.HullId, out var p) && p == s.Price));
        Assert.False(table.TryGetShipPrice(station.StationId, Starter, out _)); // the Interceptor is not sold
    }

    [Fact]
    public async Task ANewPilotOwnsTheStarterAndCanBuyAndFlyASecondHullThatKeepsEachShipsCondition()
    {
        var station = StationSellingASecondHull();
        var session = await OpenAsync(station);
        Assert.Equal(new[] { Starter }, session.Profile!.OwnedShips.Keys);
        Assert.True(station.CreatePriceTable().TryGetShipPrice(station.StationId, SecondHull, out var price) && price == 900);
        Assert.Equal("Fixture Hull", station.NameOf(SecondHull));

        // Away from the station the purchase is refused; docked at the spawn it goes through.
        var far = await session.BuyShipAsync(SecondHull, station.X + 10_000, station.Y);
        Assert.Equal(RejectionReason.StationAccessDenied, far.Reason);
        var bought = await session.BuyShipAsync(SecondHull, station.X, station.Y);
        Assert.Equal(TransactionStatus.Applied, bought.Status);
        Assert.Equal(station.StartingWallet - 900, session.Profile!.Wallet);
        Assert.Equal(RejectionReason.ShipAlreadyOwned, (await session.BuyShipAsync(SecondHull, station.X, station.Y)).Reason);

        Assert.True((await session.SaveShipAsync(60, 5, 6, 0.5f)).Saved);
        Assert.True((await session.SelectShipAsync(SecondHull)).Saved);
        Assert.Equal(SecondHull, session.Profile!.ShipHullId);
        Assert.Null(session.Profile.Ship);
        Assert.Equal(new ShipCondition(60, 5, 6, 0.5f), session.Profile.OwnedShips[Starter]);
        Assert.Equal(RejectionReason.ShipNotOwned, (await session.SelectShipAsync("fixture-unowned")).Reason);
    }

    [Fact]
    public async Task TheHangar_SwitchesShipsOnlyWhileDockedAtAHomeStore()
    {
        // Steve, Nov 4 2026: a pilot can change to another owned ship, from the hangar of a team's home store.
        var station = StationSellingASecondHull();
        var session = await OpenAsync(station);
        Assert.Equal(TransactionStatus.Applied, (await session.BuyShipAsync(SecondHull, station.X, station.Y)).Status);
        Assert.True((await session.SaveShipAsync(42, station.X, station.Y, 1f)).Saved);
        var before = session.Profile!;

        // Open space, a Trading Post and a Repair Dock have no hangar: refused, nothing saved.
        var post = station.SitesOffering(SiteServices.Trade)[0];
        var dock = station.SitesOffering(SiteServices.Repair)[0];
        foreach (var (x, y) in new[] { (station.X + 10_000, station.Y), (post.X, post.Y), (dock.X, dock.Y) })
            Assert.Equal(RejectionReason.StationAccessDenied, (await session.SwitchShipAsync(SecondHull, x, y)).Reason);
        Assert.Same(before, session.Profile);

        // At the home store the switch goes through and parks the ship flown in with its condition.
        var switched = await session.SwitchShipAsync(SecondHull, station.X, station.Y);
        Assert.True(switched.Saved);
        Assert.Equal(SecondHull, session.Profile!.ShipHullId);
        Assert.Equal(new ShipCondition(42, station.X, station.Y, 1f), session.Profile.OwnedShips[Starter]);
        Assert.Equal(RejectionReason.ShipNotOwned, (await session.SwitchShipAsync("fixture-unowned", station.X, station.Y)).Reason);

        // Every team's store has a hangar, so a pilot can switch back at another team's station too.
        var otherStore = station.SitesOffering(SiteServices.Store)[^1];
        Assert.True((await session.SwitchShipAsync(Starter, otherStore.X, otherStore.Y)).Saved);
        Assert.Equal(Starter, session.Profile!.ShipHullId);
        Assert.Equal(new ShipCondition(42, station.X, station.Y, 1f), session.Profile.Ship);
        Assert.Equal(Starter, session.Profile.ShipHullId);
    }

    [Fact]
    public async Task EveryOwnedShipMovesOntoTheMapOnItsOwn_EvenAfterAFailedProfileWrite()
    {
        // Review R1-2 on #118: the coordinate version belongs to each saved ship, not to the sector load.
        var station = StationSellingASecondHull();
        var repo = new RogueTrader.UnitTests.Persistence.FaultInjectingProfileRepository(new InMemoryProfileRepository());
        var session = await OpenAsync(station, repo);
        Assert.Equal(TransactionStatus.Applied, (await session.BuyShipAsync(SecondHull, station.X, station.Y)).Status);
        var map = TestWorld.RogueMap;
        var spawn = new System.Numerics.Vector2(1888, 1728);

        // Both ships were last saved in the old practice sector (as every schema 6 save was).
        Assert.True((await session.SaveShipAsync(30, 120, -80, 0.5f, ShipCondition.LegacyPracticeSector)).Saved);
        Assert.True((await session.SelectShipAsync(SecondHull)).Saved);
        Assert.True((await session.SaveShipAsync(20, -50, 60, 1f, ShipCondition.LegacyPracticeSector)).Saved);

        // The sector is saved on the map, then the profile write fails: the ship is still a practice-sector ship and starts at
        // the spawn with its hull and heading, instead of standing at (-50, 60) on the map.
        repo.Mode = RogueTrader.UnitTests.Persistence.FaultMode.FailBeforeWrite;
        Assert.False((await session.SaveShipAsync(20, 1900, 1700, 1f)).Saved);
        Assert.True((await session.ReloadAsync()).Succeeded);
        repo.Mode = RogueTrader.UnitTests.Persistence.FaultMode.None;
        var active = session.Profile!.Ship!;
        Assert.Equal((20f, 1f, ShipCondition.LegacyPracticeSector), (active.Hull, active.Heading, active.CoordinateVersion));
        Assert.Equal(spawn, ShipPlacement.Position(active, map, spawn));

        // The parked starter moves on its own when it is next flown, whichever ship moved first.
        Assert.True((await session.SaveShipAsync(20, 1900, 1700, 1f)).Saved); // flown on the map now
        Assert.Equal(new System.Numerics.Vector2(1900, 1700), ShipPlacement.Position(session.Profile!.Ship!, map, spawn));
        Assert.True((await session.SelectShipAsync(Starter)).Saved);
        var starter = session.Profile!.Ship!;
        Assert.Equal((30f, 0.5f, ShipCondition.LegacyPracticeSector), (starter.Hull, starter.Heading, starter.CoordinateVersion));
        Assert.Equal(spawn, ShipPlacement.Position(starter, map, spawn));
        Assert.Equal(ShipCondition.MapPixels, session.Profile.OwnedShips[SecondHull]!.CoordinateVersion);
        // Without a map (the practice sector) a practice-sector position is used as saved, and a map position is not.
        Assert.Equal(new System.Numerics.Vector2(120, -80), ShipPlacement.Position(starter, null, spawn));
        Assert.Equal(spawn, ShipPlacement.Position(session.Profile.OwnedShips[SecondHull]!, null, spawn));
    }

    [Fact]
    public async Task EachShipCarriesItsOwnHoldCap()
    {
        var session = await OpenAsync(StationSellingASecondHull());
        var heavy = session.Station.Goods.Where(g => g.UnitWeight > 0).MaxBy(g => g.UnitWeight)!;
        session.FlyShip(heavy.UnitWeight - 1); // a hold that cannot take even one unit
        var refused = await session.GrantPickupAsync(Guid.NewGuid().ToString("N"), new CargoPickup(0, "fixture-pile", heavy.Id, 1, 0));
        Assert.Equal(RejectionReason.CargoCapacityExceeded, refused.Reason);
        session.FlyShip(TestCargo.Capacity);
        var accepted = await session.GrantPickupAsync(Guid.NewGuid().ToString("N"), new CargoPickup(0, "fixture-pile", heavy.Id, 1, 0));
        Assert.Equal(TransactionStatus.Applied, accepted.Status);
    }

    [Fact]
    public async Task ADestructionCostsTwentyPercentOnceAcrossARelaunch()
    {
        using var dir = new RogueTrader.UnitTests.Persistence.TempDirectory();
        var station = StationSellingASecondHull();
        long expected;
        using (var repo = FileProfileRepository.Open(dir.Path, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn))
        {
            var session = await OpenAsync(station, repo);
            var wallet = session.Profile!.Wallet;
            Assert.True((await session.SaveShipAsync(30, 7, 8, 0)).Saved);
            var result = await session.RecordShipDestroyedAsync("world-a", 1234);
            Assert.Equal(TransactionStatus.Applied, result.Status);
            expected = wallet - wallet / 5;
            Assert.Equal(expected, session.Profile!.Wallet);
            Assert.Null(session.Profile.Ship);
        }
        using (var repo = FileProfileRepository.Open(dir.Path, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn))
        {
            var session = await OpenAsync(station, repo);
            var replay = await session.RecordShipDestroyedAsync("world-a", 1234);
            Assert.True(replay.IsReplay);
            Assert.Equal(expected, session.Profile!.Wallet);
        }
        Assert.NotEqual(StationProfileSession.DestructionTransactionId("world-a", 1234), StationProfileSession.DestructionTransactionId("world-a", 1235));
        Assert.NotEqual(StationProfileSession.DestructionTransactionId("world-a", 1234), StationProfileSession.DestructionTransactionId("world-b", 1234));
    }

    [Fact]
    public void DamageDestroysThePlayerAtZeroAndNeverBelow()
    {
        var world = new WorldSimulation(TestWorld.ExternalCatalog());
        var maximum = world.Player.Definition.Defense.MaximumHull;
        Assert.Equal(10, world.DamagePlayer(10));
        Assert.Equal(maximum - 10, world.Player.Hull);
        Assert.Equal(maximum - 10, world.DamagePlayer(float.MaxValue));
        Assert.True(world.Player.IsDestroyed);
        Assert.Equal(0, world.Player.Hull);
        Assert.Throws<ArgumentOutOfRangeException>(() => world.DamagePlayer(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => world.DamagePlayer(float.NaN));
    }
}
