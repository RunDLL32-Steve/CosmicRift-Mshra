using RogueTrader.GameData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;
using RogueTrader.UnitTests.Persistence;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// Queue M8 client boundary: the station session the Godot HUD and station screen drive. Uses the shipped NEW/provisional
/// station definition (data/stations/first_contact_station.json); none of its numbers are original values.
/// </summary>
public sealed class StationProfileSessionTests
{
    private const string Profile = "m8-pilot";

    private static StationDefinition LoadStation() =>
        StationDefinition.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "stations", "first_contact_station.json")));

    private static (float X, float Y) Dock(StationDefinition s) => (s.X, s.Y);

    [Fact]
    public void ShippedDefinition_IsNewProvisionalAndInternallyConsistent()
    {
        var station = LoadStation();

        Assert.Equal("NEW", station.EvidenceClassification);
        Assert.NotEmpty(station.Goods);
        Assert.NotEmpty(station.Repairs);
        Assert.NotEmpty(station.Upgrades);
        var cargo = station.CreateCargoCatalog();
        Assert.All(station.Collectibles, pile => Assert.True(cargo.IsKnown(pile.ItemId)));
        // The player spawns inside the dock radius so a new pilot can trade immediately.
        var sector = TestWorld.ExternalCatalog().Sector;
        Assert.True(Docking.IsDocked(station.Site, sector.PlayerSpawn.X, sector.PlayerSpawn.Y));
    }

    [Fact]
    public void Definition_RejectsBadRepairFraction()
    {
        const string json = """{ "stationId": "s", "name": "S", "dockRadius": 5, "repairs": [ { "id": "r", "name": "R", "price": 1, "hullRestoreFraction": 1.5 } ] }""";
        Assert.Throws<InvalidDataException>(() => StationDefinition.FromJson(json));
    }

    [Fact]
    public async Task OpenOrCreate_CreatesWithStartingWallet_ThenLoadsExisting()
    {
        var repo = new InMemoryProfileRepository();
        var station = LoadStation();
        var session = new StationProfileSession(repo, station, "starter_interceptor");

        var first = await session.OpenOrCreateAsync(Profile);
        Assert.True(first.Succeeded);
        Assert.True(first.Created);
        Assert.Equal(station.StartingWallet, session.Profile!.Wallet);
        Assert.Equal("starter_interceptor", session.Profile.ShipHullId);

        var again = await new StationProfileSession(repo, station, "starter_interceptor").OpenOrCreateAsync(Profile);
        Assert.True(again.Succeeded);
        Assert.False(again.Created);
    }

    [Fact]
    public async Task StationFlow_BuySellRepairUpgrade_AreAuthoritativeAndDockGated()
    {
        var station = LoadStation();
        var session = new StationProfileSession(new InMemoryProfileRepository(), station, null);
        await session.OpenOrCreateAsync(Profile);
        var (x, y) = Dock(station);
        var ore = station.Goods.First(g => g.Buy is not null && g.Sell is not null);

        var bought = await session.BuyAsync(ore.Id, 3, x, y);
        Assert.Equal(TransactionStatus.Applied, bought.Status);
        Assert.Equal(station.StartingWallet - 3 * ore.Buy!.Value, session.Profile!.Wallet);
        Assert.Equal(3, session.Profile.Inventory[ore.Id]);

        var sold = await session.SellAsync(ore.Id, 1, x, y);
        Assert.Equal(TransactionStatus.Applied, sold.Status);
        Assert.Equal(2, session.Profile.Inventory[ore.Id]);

        var repair = station.Repairs[0];
        var walletBeforeRepair = session.Profile.Wallet;
        Assert.Equal(TransactionStatus.Applied, (await session.RepairAsync(repair.Id, x, y)).Status);
        Assert.Equal(walletBeforeRepair - repair.Price, session.Profile.Wallet);
        Assert.Equal(repair.HullRestoreFraction * 200f, session.RepairHullPoints(repair.Id, 200f));

        var cheapest = station.Upgrades.OrderBy(u => u.Price).First();
        Assert.Equal(TransactionStatus.Applied, (await session.BuyUpgradeAsync(cheapest.Id, x, y)).Status);
        Assert.Contains(cheapest.Id, session.Profile.Upgrades);
        Assert.Equal(RejectionReason.UpgradeAlreadyOwned, (await session.BuyUpgradeAsync(cheapest.Id, x, y)).Reason);

        // Outside the dock radius every station service is refused and nothing changes.
        var before = session.Profile;
        var far = x + station.DockRadius + 1;
        Assert.False(session.IsDocked(far, y));
        Assert.Equal(RejectionReason.StationAccessDenied, (await session.BuyAsync(ore.Id, 1, far, y)).Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, (await session.SellAsync(ore.Id, 1, far, y)).Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, (await session.RepairAsync(repair.Id, far, y)).Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, (await session.BuyUpgradeAsync(station.Upgrades[^1].Id, far, y)).Reason);
        Assert.Same(before, session.Profile);
    }

    [Fact]
    public async Task Buy_BeyondCargoCapacity_IsRefused()
    {
        var station = LoadStation();
        var session = new StationProfileSession(new InMemoryProfileRepository(), station, null);
        await session.OpenOrCreateAsync(Profile);
        var heavy = station.Goods.OrderByDescending(g => g.UnitWeight).First(g => g.Buy is not null);
        var tooMany = (int)(station.CargoCapacity / heavy.UnitWeight) + 1;

        var result = await session.BuyAsync(heavy.Id, tooMany, station.X, station.Y);

        Assert.Equal(TransactionStatus.Rejected, result.Status);
        Assert.Contains(result.Reason, new[] { RejectionReason.CargoCapacityExceeded, RejectionReason.InsufficientFunds });
    }

    [Fact]
    public async Task CommitPickups_GrantsOnlyNewUnits_AndWorksAwayFromStation()
    {
        var station = LoadStation();
        var session = new StationProfileSession(new InMemoryProfileRepository(), station, null);
        await session.OpenOrCreateAsync(Profile);
        var pile = station.Collectibles[0];

        Assert.Null(await session.CommitPickupsAsync(new Dictionary<string, int>()));
        var granted = await session.CommitPickupsAsync(new Dictionary<string, int> { [pile.ItemId] = pile.Quantity });
        Assert.Equal(TransactionStatus.Applied, granted!.Status);
        Assert.Equal(pile.Quantity, session.Profile!.Inventory[pile.ItemId]);
        Assert.Equal(station.StartingWallet, session.Profile.Wallet);
        // Same hold again: nothing new to commit.
        Assert.Null(await session.CommitPickupsAsync(new Dictionary<string, int> { [pile.ItemId] = pile.Quantity }));
    }

    [Fact]
    public async Task FileRepository_StateSurvivesRelaunch_AndReloadDiscardsNothingCommitted()
    {
        using var dir = new TempDirectory();
        var station = LoadStation();
        PlayerProfile beforeExit;
        using (var repo = FileProfileRepository.Open(dir.Path, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn))
        {
            var session = new StationProfileSession(repo, station, "starter_interceptor");
            await session.OpenOrCreateAsync(Profile);
            var good = station.Goods.First(g => g.Buy is not null);
            await session.BuyAsync(good.Id, 2, station.X, station.Y);
            await session.BuyUpgradeAsync(station.Upgrades[0].Id, station.X, station.Y);
            var reload = await session.ReloadAsync();
            Assert.True(reload.Succeeded);
            Assert.Equal(ProfileLoadStatus.Loaded, reload.Status);
            beforeExit = session.Profile!;
        }

        using (var repo = FileProfileRepository.Open(dir.Path, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn))
        {
            var session = new StationProfileSession(repo, station, "starter_interceptor");
            var opened = await session.OpenOrCreateAsync(Profile);
            Assert.True(opened.Succeeded);
            Assert.False(opened.Created);
            var after = session.Profile!;
            Assert.Equal(beforeExit.Revision, after.Revision);
            Assert.Equal(beforeExit.Wallet, after.Wallet);
            Assert.Equal(beforeExit.Inventory.ToArray(), after.Inventory.ToArray());
            Assert.Equal(beforeExit.Upgrades.ToArray(), after.Upgrades.ToArray());
        }
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

        Assert.False(outcome.Succeeded);
        Assert.False(outcome.Created);
        Assert.Equal(ProfileLoadStatus.Corrupt, outcome.Status);
        Assert.Null(session.Profile);
        Assert.Equal(RejectionReason.ProfileNotOpen, (await session.BuyAsync(station.Goods[0].Id, 1, station.X, station.Y)).Reason);
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
    public void RepairPlayerHull_ClampsToMaximumAndRejectsBadAmounts()
    {
        var world = new WorldSimulation(TestWorld.ExternalCatalog());
        var max = world.Player.Definition.Defense.MaximumHull;

        Assert.Equal(0, world.RepairPlayerHull(50));
        Assert.Equal(max, world.Player.Hull);
        Assert.Throws<ArgumentOutOfRangeException>(() => world.RepairPlayerHull(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => world.RepairPlayerHull(-1));
    }
}
