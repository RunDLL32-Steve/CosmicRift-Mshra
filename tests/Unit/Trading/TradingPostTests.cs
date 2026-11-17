using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;
using RogueTrader.UnitTests.Persistence;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// RT-F045 Trading Posts: the VEH id 404 ComputerProduct blocks read with the emulator's ArenaComputer rules (every unit held is
/// handed in, one credit item per unit, free, Time x 10 ms), used within CFG ComputerProduceRadius=250, one timed order at a
/// time across the refineries and Trading Posts. The shipped station data is the oracle; RT-F045 stays open.
/// </summary>
public sealed class TradingPostTests
{
    private const string Profile = "rt-f045-trader";

    private static StationDefinition LoadStation() =>
        StationDefinition.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "stations", "trading_outpost.json")));

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 17, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private static async Task<StationProfileSession> OpenAsync(ManualClock clock, IProfileRepository? repo = null)
    {
        var session = new StationProfileSession(repo ?? new InMemoryProfileRepository(), LoadStation(), "starter_interceptor", TestCargo.Capacity, time: clock);
        Assert.True((await session.OpenOrCreateAsync(Profile)).Succeeded);
        return session;
    }

    private static string TradeFor(StationProfileSession s, string input) => s.Station.Trades.Single(t => t.InputItemId == input).Id;

    [Fact]
    public void ShippedStation_HasTheOriginalTradingPostRecipes()
    {
        var station = LoadStation();
        Assert.Equal(new[] { ("2004", 55L, 150), ("2006", 80L, 150), ("2008", 300L, 0), ("2020", 20L, 0) },
            station.Trades.Select(t => (t.InputItemId, t.CreditsPerUnit, t.Time)));
        Assert.All(station.Trades, t => Assert.Equal((-1, -1, 0L), (t.OutputQuantity, t.InputQuantity, t.Cost)));
        Assert.Equal(TimeSpan.FromSeconds(1.5), station.Trade("trade_tellerium")!.Duration);
        Assert.Equal(TimeSpan.Zero, station.Trade("trade_wreckage")!.Duration);
        // Both posts are computers, used within CFG [Vehicle] ComputerProduceRadius=250.
        var posts = station.SitesOffering(SiteServices.Trade);
        Assert.Equal(2, posts.Count);
        Assert.All(posts, p => Assert.Equal(250, p.DockRadius));
    }

    [Fact]
    public async Task Trade_HandsInEveryUnitHeld_AndPaysAfterTheOriginalTime_OnlyAtATradingPost()
    {
        var clock = new ManualClock();
        var session = await OpenAsync(clock);
        var post = session.Station.SitesOffering(SiteServices.Trade)[0];
        Assert.Equal(TransactionStatus.Applied, (await session.GrantPickupAsync("a", new CargoPickup(0, "p:1", "2004", 12, 1))).Status);
        var id = TradeFor(session, "2004");

        Assert.Equal(RejectionReason.StationAccessDenied, session.OrderTrade(id, session.Station.X, session.Station.Y));
        Assert.Equal(RejectionReason.NotForSale, session.OrderTrade("no_such_trade", post.X, post.Y));
        Assert.Equal(RejectionReason.InsufficientInventory, session.OrderTrade(TradeFor(session, "2006"), post.X, post.Y));
        Assert.Equal(RejectionReason.StationAccessDenied, session.OrderTrade(id, post.X + 251, post.Y));
        var wallet = session.Profile!.Wallet;

        Assert.Equal(RejectionReason.None, session.OrderTrade(id, post.X + 249, post.Y));
        var order = Assert.Single(session.TradeOrders);
        Assert.Equal(12, order.Quantity); // PlayerItemNeededQuantity -1: every unit held
        Assert.Null(await session.DeliverTradedAsync());
        Assert.Equal(12, session.Profile!.Inventory["2004"]);
        Assert.Equal(wallet, session.Profile.Wallet);

        clock.Advance(TimeSpan.FromMilliseconds(1499)); // Time 150 x 10 ms
        Assert.False(session.TradeOrderDue);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        var (delivered, result) = (await session.DeliverTradedAsync())!.Value;
        Assert.Same(order, delivered);
        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.False(session.Profile!.Inventory.ContainsKey("2004"));
        Assert.Equal(wallet + 12 * 55, session.Profile.Wallet); // Tellerium Credits f[31] Cash 55 per credit item
        Assert.Empty(session.TradeOrders);
    }

    [Fact]
    public async Task TradingPosts_TakeOneTimedOrderAtATime_SharedWithTheRefinery_ButWreckageIsInstant()
    {
        var clock = new ManualClock();
        var session = await OpenAsync(clock);
        var post = session.Station.SitesOffering(SiteServices.Trade)[1];
        var refinery = session.Station.Refinery!;
        await session.GrantPickupAsync("a", new CargoPickup(0, "p:1", "2004", 3, 1));
        await session.GrantPickupAsync("b", new CargoPickup(1, "p:2", "2006", 2, 1));
        await session.GrantPickupAsync("c", new CargoPickup(2, "p:3", "2008", 1, 1));
        await session.GrantPickupAsync("d", new CargoPickup(3, "p:4", "2003", 4, 1));
        var wallet = session.Profile!.Wallet;

        Assert.Equal(RejectionReason.None, session.OrderTrade(TradeFor(session, "2004"), post.X, post.Y));
        // CFG ComputerProduceInParallel=0: "Unable to produce - object is busy", at this post and at the refinery.
        Assert.Equal(RejectionReason.SiteBusy, session.OrderTrade(TradeFor(session, "2006"), post.X, post.Y));
        Assert.Equal(RejectionReason.SiteBusy, session.OrderTrade(TradeFor(session, "2004"), post.X, post.Y));
        Assert.Equal(RejectionReason.SiteBusy, session.OrderRefine(refinery.Recipes.Single(r => r.InputItemId == "2003").Id, refinery.X, refinery.Y));
        // Trade Wreckage has Time 0, so it is paid at once even while the line is busy.
        Assert.Equal(TransactionStatus.Applied, (await session.TradeNowAsync(TradeFor(session, "2008"), post.X, post.Y)).Status);
        Assert.Equal(wallet + 300, session.Profile!.Wallet);

        clock.Advance(TimeSpan.FromSeconds(1.5));
        Assert.Equal(TransactionStatus.Applied, (await session.DeliverTradedAsync())!.Value.Result.Status);
        Assert.Equal(RejectionReason.None, session.OrderTrade(TradeFor(session, "2006"), post.X, post.Y));
        Assert.Equal(RejectionReason.SiteBusy, (await session.TradeNowAsync(TradeFor(session, "2006"), post.X, post.Y)).Reason);
        clock.Advance(TimeSpan.FromSeconds(1.5));
        await session.DeliverTradedAsync();
        Assert.Equal(wallet + 300 + 3 * 55 + 2 * 80, session.Profile!.Wallet);
        Assert.Equal(new Dictionary<string, int> { ["2003"] = 4 }, session.Profile.Inventory.ToDictionary());
    }

    [Fact]
    public async Task ATimedRefineBlocksATimedTrade()
    {
        var clock = new ManualClock();
        var session = await OpenAsync(clock);
        var refinery = session.Station.Refinery!;
        var post = session.Station.SitesOffering(SiteServices.Trade)[0];
        await session.GrantPickupAsync("a", new CargoPickup(0, "p:1", "2003", 4, 1));
        await session.GrantPickupAsync("b", new CargoPickup(1, "p:2", "2006", 2, 1));
        Assert.Equal(RejectionReason.None, session.OrderRefine(refinery.Recipes.Single(r => r.InputItemId == "2003").Id, refinery.X, refinery.Y));
        Assert.Equal(RejectionReason.SiteBusy, session.OrderTrade(TradeFor(session, "2006"), post.X, post.Y));
    }

    [Fact]
    public async Task AnOrderLostBeforeDelivery_PaysNothing_AndATradeIsPaidOnce()
    {
        var clock = new ManualClock();
        var repo = new InMemoryProfileRepository();
        var session = await OpenAsync(clock, repo);
        var post = session.Station.SitesOffering(SiteServices.Trade)[0];
        await session.GrantPickupAsync("a", new CargoPickup(0, "p:1", "2006", 5, 1));
        var wallet = session.Profile!.Wallet;
        Assert.Equal(RejectionReason.None, session.OrderTrade(TradeFor(session, "2006"), post.X, post.Y));

        // A relaunch before the trade is delivered: the new session has no order and the saved profile is unchanged.
        var relaunched = await OpenAsync(clock, repo);
        Assert.Empty(relaunched.TradeOrders);
        Assert.Equal((wallet, 5), (relaunched.Profile!.Wallet, relaunched.Profile.Inventory["2006"]));

        // The first session's order still pays exactly once; a second delivery has nothing left to do.
        clock.Advance(TimeSpan.FromSeconds(2));
        await relaunched.ReloadAsync();
        Assert.Equal(TransactionStatus.Applied, (await session.DeliverTradedAsync())!.Value.Result.Status);
        Assert.Null(await session.DeliverTradedAsync());
        await relaunched.ReloadAsync();
        Assert.Equal(wallet + 5 * 80, relaunched.Profile!.Wallet);
        Assert.False(relaunched.Profile.Inventory.ContainsKey("2006"));
    }

    [Fact]
    public async Task ATradeWhoseUnitsAreGoneByDelivery_IsRefusedAndChangesNothing()
    {
        var clock = new ManualClock();
        var session = await OpenAsync(clock);
        var post = session.Station.SitesOffering(SiteServices.Trade)[0];
        await session.GrantPickupAsync("a", new CargoPickup(0, "p:1", "2004", 6, 1));
        Assert.Equal(RejectionReason.None, session.OrderTrade(TradeFor(session, "2004"), post.X, post.Y));
        // The units are gone before the trade completes (here sold by a direct sell; a destroyed ship drops its hold the same way).
        Assert.Equal(TransactionStatus.Applied, (await session.SellAsync("2004", 6, post.X, post.Y)).Status);
        var wallet = session.Profile!.Wallet;
        clock.Advance(TimeSpan.FromSeconds(1.5));
        var (_, result) = (await session.DeliverTradedAsync())!.Value;
        Assert.Equal(TransactionStatus.Rejected, result.Status);
        Assert.Equal(wallet, session.Profile!.Wallet);
    }

    [Theory]
    [InlineData("\"inputQuantity\": 2")]
    [InlineData("\"outputQuantity\": 3")]
    [InlineData("\"cost\": 10")]
    [InlineData("\"time\": -1")]
    public void Station_RejectsATradeTheCoreCannotPay(string field)
    {
        var json = """
            { "stationId": "s", "name": "S", "dockRadius": 5,
              "goods": [ { "id": "a", "name": "A", "unitWeight": 2 } ],
              "trades": [ { "id": "x", "name": "X", "inputItemId": "a", "creditItemId": "c", "creditName": "C", "creditsPerUnit": 5, FIELD } ] }
            """.Replace("FIELD", field);
        Assert.Throws<InvalidDataException>(() => StationDefinition.FromJson(json));
    }
}
