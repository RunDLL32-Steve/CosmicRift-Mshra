using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;
using RogueTrader.UnitTests.Persistence;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// Queue M8 client boundary: the station session the Godot HUD and station screen drive, over the shipped derived Trading
/// Outpost (data/stations/trading_outpost.json). Wallets granted below are NEW test fixtures.
/// </summary>
public sealed class StationProfileSessionTests
{
    private const string Profile = "m8-pilot";
    private const float FullHull = 100f;

    private static StationDefinition LoadStation() =>
        StationDefinition.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "stations", "trading_outpost.json")));

    private static async Task<StationProfileSession> OpenAsync(IProfileRepository? repo = null, StationDefinition? station = null)
    {
        var session = new StationProfileSession(repo ?? new InMemoryProfileRepository(), station ?? LoadStation(), "starter_interceptor");
        Assert.True((await session.OpenOrCreateAsync(Profile)).Succeeded);
        return session;
    }

    private static async Task Give(StationProfileSession session, string item, int quantity)
    {
        var hold = new Dictionary<string, int>(session.Profile!.Inventory) { [item] = session.Profile.Inventory.GetValueOrDefault(item) + quantity };
        Assert.Equal(TransactionStatus.Applied, (await session.CommitPickupsAsync(hold))!.Status);
    }

    [Fact]
    public void ShippedDefinition_IsDerivedAndInternallyConsistent()
    {
        var station = LoadStation();

        Assert.Equal("ORIGINAL+NEW", station.EvidenceClassification);
        Assert.NotEmpty(station.NewValues);
        var cargo = station.CreateCargoCatalog();
        Assert.All(station.Collectibles, pile => Assert.True(cargo.IsKnown(pile.ItemId)));
        Assert.All(station.Trades, t => Assert.Equal(t.CreditsPerUnit, station.SellPrice(t.InputItemId)));
        var sector = TestWorld.ExternalCatalog().Sector;
        Assert.True(Docking.IsDocked(station.Site, sector.PlayerSpawn.X, sector.PlayerSpawn.Y));
    }

    [Fact]
    public void Definition_RejectsNonPositiveRepair()
    {
        const string json = """{ "stationId": "s", "name": "S", "dockRadius": 5, "repairs": [ { "id": "r", "name": "R", "price": 1, "hullPoints": 0 } ] }""";
        Assert.Throws<InvalidDataException>(() => StationDefinition.FromJson(json));
    }

    [Fact]
    public async Task OpenOrCreate_CreatesWithStartingWallet_ThenLoadsExisting()
    {
        var repo = new InMemoryProfileRepository();
        var station = LoadStation();
        var session = new StationProfileSession(repo, station, "starter_interceptor");

        var first = await session.OpenOrCreateAsync(Profile);
        Assert.True(first.Succeeded && first.Created);
        Assert.Equal(station.StartingWallet, session.Profile!.Wallet);
        Assert.Null(session.Profile.Ship);

        var again = await new StationProfileSession(repo, station, "starter_interceptor").OpenOrCreateAsync(Profile);
        Assert.True(again.Succeeded);
        Assert.False(again.Created);
    }

    [Fact]
    public async Task TradingOutpost_PaysRecipeCreditsPerUnit_AndUpgradeUsesOriginalPrice()
    {
        var station = LoadStation();
        var session = await OpenAsync(station: station);
        var trade = station.Trades.OrderByDescending(t => t.CreditsPerUnit).First();
        await Give(session, trade.InputItemId, 9);

        var sold = await session.SellAsync(trade.InputItemId, 9, station.X, station.Y);

        Assert.Equal(TransactionStatus.Applied, sold.Status);
        Assert.Equal(station.StartingWallet + 9 * trade.CreditsPerUnit, session.Profile!.Wallet);
        var affordable = station.Upgrades.Where(u => u.Price <= session.Profile.Wallet).OrderBy(u => u.Price).First();
        Assert.Equal(TransactionStatus.Applied, (await session.BuyUpgradeAsync(affordable.Id, station.X, station.Y)).Status);
        Assert.Equal(station.StartingWallet + 9 * trade.CreditsPerUnit - affordable.Price, session.Profile.Wallet);
        Assert.Equal(RejectionReason.UpgradeAlreadyOwned, (await session.BuyUpgradeAsync(affordable.Id, station.X, station.Y)).Reason);
    }

    [Fact]
    public async Task ResourceWithoutRecipe_IsNotTraded()
    {
        var station = LoadStation();
        var session = await OpenAsync(station: station);
        var noRecipe = station.Goods.First(g => station.Trades.All(t => t.InputItemId != g.Id));
        await Give(session, noRecipe.Id, 1);

        Assert.Equal(RejectionReason.NotForSale, (await session.SellAsync(noRecipe.Id, 1, station.X, station.Y)).Reason);
        Assert.Equal(RejectionReason.NotForSale, (await session.BuyAsync(noRecipe.Id, 1, station.X, station.Y)).Reason);
    }

    [Theory]
    [InlineData(FullHull)]
    [InlineData(0f)]
    public async Task Repair_AtFullHullOrDestroyed_IsRefusedWithoutCharge(float hull)
    {
        var station = LoadStation();
        var session = await OpenAsync(station: station);
        await Give(session, station.Trades[0].InputItemId, 20);
        await session.SellAsync(station.Trades[0].InputItemId, 20, station.X, station.Y);
        var before = session.Profile!;

        var result = await session.RepairAsync(station.Repairs[^1].Id, station.X, station.Y, hull, FullHull);

        Assert.Equal(RejectionReason.NothingToRepair, result.Reason);
        Assert.Same(before, session.Profile);
    }

    [Fact]
    public async Task Repair_WhenDamaged_ChargesAndReportsOriginalAmount()
    {
        var station = LoadStation();
        var session = await OpenAsync(station: station);
        var trade = station.Trades.OrderByDescending(t => t.CreditsPerUnit).First();
        await Give(session, trade.InputItemId, 5);
        await session.SellAsync(trade.InputItemId, 5, station.X, station.Y);
        var minor = station.Repairs[0];
        var wallet = session.Profile!.Wallet;

        Assert.Equal(TransactionStatus.Applied, (await session.RepairAsync(minor.Id, station.X, station.Y, 60, FullHull)).Status);
        Assert.Equal(wallet - minor.Price, session.Profile.Wallet);
        Assert.Equal(minor.HullPoints, session.RepairHullPoints(minor.Id));
    }

    [Fact]
    public async Task StationServices_OutsideDockRadius_AreRefused()
    {
        var station = LoadStation();
        var session = await OpenAsync(station: station);
        var trade = station.Trades[0];
        await Give(session, trade.InputItemId, 2);
        var before = session.Profile;
        var far = station.X + station.DockRadius + 1;

        Assert.False(session.IsDocked(far, station.Y));
        Assert.Equal(RejectionReason.StationAccessDenied, (await session.SellAsync(trade.InputItemId, 1, far, station.Y)).Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, (await session.RepairAsync(station.Repairs[0].Id, far, station.Y, 50, FullHull)).Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, (await session.BuyUpgradeAsync(station.Upgrades[0].Id, far, station.Y)).Reason);
        Assert.Same(before, session.Profile);
    }

    [Fact]
    public async Task CommitPickups_GrantsOnlyNewUnits_AndRespectsCapacity()
    {
        var station = LoadStation();
        var session = await OpenAsync(station: station);
        var pile = station.Collectibles[0];

        Assert.Null(await session.CommitPickupsAsync(new Dictionary<string, int>()));
        var granted = await session.CommitPickupsAsync(new Dictionary<string, int> { [pile.ItemId] = pile.Quantity });
        Assert.Equal(TransactionStatus.Applied, granted!.Status);
        Assert.Equal(pile.Quantity, session.Profile!.Inventory[pile.ItemId]);
        Assert.Null(await session.CommitPickupsAsync(new Dictionary<string, int> { [pile.ItemId] = pile.Quantity }));

        var heavy = station.Goods.OrderByDescending(g => g.UnitWeight).First();
        var overflow = await session.CommitPickupsAsync(new Dictionary<string, int>(session.Profile.Inventory)
            { [heavy.Id] = (int)(station.CargoCapacity / heavy.UnitWeight) + 1 });
        Assert.Equal(RejectionReason.CargoCapacityExceeded, overflow!.Reason);
    }

    [Fact]
    public async Task ShipCondition_IsSaved_AndSurvivesRelaunch()
    {
        using var dir = new TempDirectory();
        var station = LoadStation();
        PlayerProfile beforeExit;
        using (var repo = FileProfileRepository.Open(dir.Path, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn))
        {
            var session = await OpenAsync(repo, station);
            await Give(session, station.Trades[0].InputItemId, 3);
            var saved = await session.SaveShipAsync(42.5f, -120f, 33f, 1.25f);
            Assert.True(saved.Saved);
            Assert.Equal(new ShipCondition(42.5f, -120f, 33f, 1.25f), session.Profile!.Ship);
            Assert.Equal(saved.Revision, session.Profile.Revision);
            var unchanged = await session.SaveShipAsync(42.5f, -120f, 33f, 1.25f);
            Assert.Equal(saved.Revision, unchanged.Revision); // identical condition is not rewritten
            Assert.True((await session.ReloadAsync()).Succeeded);
            beforeExit = session.Profile!;
        }

        using (var repo = FileProfileRepository.Open(dir.Path, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn))
        {
            var session = await OpenAsync(repo, station);
            var after = session.Profile!;
            Assert.Equal(beforeExit.Revision, after.Revision);
            Assert.Equal(beforeExit.Wallet, after.Wallet);
            Assert.Equal(beforeExit.Inventory.ToArray(), after.Inventory.ToArray());
            Assert.Equal(beforeExit.Ship, after.Ship);
        }
    }

    [Fact]
    public async Task ShipCondition_RejectsNonFiniteValues()
    {
        var session = await OpenAsync();
        var before = session.Profile;

        var result = await session.SaveShipAsync(float.NaN, 0, 0, 0);

        Assert.False(result.Saved);
        Assert.Equal(RejectionReason.InvalidRequest, result.Reason);
        Assert.Same(before, session.Profile);
    }

    [Fact]
    public async Task CorruptSave_IsReportedAndNeverReplacedWithAFreshProfile()
    {
        using var dir = new TempDirectory();
        var station = LoadStation();
        File.WriteAllText(dir.File(Profile + ".profile.json"), "{ not json");
        using var repo = FileProfileRepository.Open(dir.Path, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn);
        var session = new StationProfileSession(repo, station, null);

        var outcome = await session.OpenOrCreateAsync(Profile);

        Assert.False(outcome.Succeeded || outcome.Created);
        Assert.Equal(ProfileLoadStatus.Corrupt, outcome.Status);
        Assert.Null(session.Profile);
        Assert.Equal(RejectionReason.ProfileNotOpen, (await session.BuyUpgradeAsync(station.Upgrades[0].Id, station.X, station.Y)).Reason);
    }

    [Fact]
    public void CargoHold_ReplaceContents_MatchesAuthoritativeInventory()
    {
        var catalog = new CargoCatalog(20, [new CargoItemDefinition("a", 2), new CargoItemDefinition("b", 1)]);
        var hold = new CargoHold(catalog, [new("a", 3)]);

        hold.ReplaceContents([new("b", 4)]);

        Assert.Equal(0, hold.Quantity("a"));
        Assert.Equal(4, hold.Quantity("b"));
        Assert.Equal(4, hold.TotalWeight);
        Assert.Throws<ArgumentException>(() => hold.ReplaceContents([new("unknown", 1)]));
        Assert.Equal(4, hold.Quantity("b"));
    }

    [Fact]
    public void RepairAndRestore_ClampToMaximum_AndRejectBadValues()
    {
        var world = new WorldSimulation(TestWorld.ExternalCatalog());
        var max = world.Player.Definition.Defense.MaximumHull;

        Assert.Equal(0, world.RepairPlayerHull(50));
        Assert.Throws<ArgumentOutOfRangeException>(() => world.RepairPlayerHull(float.NaN));
        Assert.True(world.RestorePlayer(new System.Numerics.Vector2(10, -20), 0.5f, max / 2));
        Assert.Equal(max / 2, world.Player.Hull);
        Assert.Equal(new System.Numerics.Vector2(10, -20), world.Player.Position);
        Assert.Equal(max / 2, world.RepairPlayerHull(max * 3));
        Assert.False(world.RestorePlayer(new System.Numerics.Vector2(0, 0), 0, 0)); // a destroyed save keeps the fresh spawn
    }
}
