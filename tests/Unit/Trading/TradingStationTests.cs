using RogueTrader.OriginalData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;
using RogueTrader.UnitTests.Persistence;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// M4 trading station: buy/sell through the M1 transaction core with a docking predicate, a price source and the M2 cargo
/// capacity. Every number below is a NEW/provisional test fixture, not game balance, unless a test says it reads original data.
/// </summary>
public sealed class TradingStationTests
{
    private const string Ore = "ore";            // NEW fixture item, unit weight 2
    private const string Gem = "gem";            // NEW fixture item, unit weight 5
    private const string Junk = "junk";          // NEW fixture item that no station trades
    private const long Capacity = 20;            // NEW fixture cargo capacity (weight units)
    private const float StationX = 100f, StationY = 50f, DockRadius = 30f; // NEW fixture world units

    private sealed class Rig
    {
        public required IProfileRepository Repo { get; init; }
        public required TransactionProcessor Proc { get; init; }
        public required TradingService Trading { get; init; }

        public PlayerProfile Snapshot => Proc.GetSnapshot(Fx.ProfileId)!;

        public Task<TransactionResult> Buy(string? id, string item, int qty, float x = StationX, float y = StationY, string station = Fx.Station) =>
            Trading.BuyAsync(Fx.ProfileId, x, y, new BuyRequest(id ?? Fx.NewId(), Fx.ProfileId, station, item, qty));

        public Task<TransactionResult> Sell(string? id, string item, int qty, float x = StationX, float y = StationY, string station = Fx.Station) =>
            Trading.SellAsync(Fx.ProfileId, x, y, new SellRequest(id ?? Fx.NewId(), Fx.ProfileId, station, item, qty));

        public static async Task<Rig> CreateAsync(long wallet = 1000, IEnumerable<(string, int)>? held = null, IProfileRepository? repo = null)
        {
            var options = new PersistenceOptions();
            repo ??= new InMemoryProfileRepository(options);
            var initial = PlayerProfile.CreateNew(Fx.ProfileId, wallet, Fx.Hull);
            if (held is not null)
                initial = initial with { Inventory = Fx_Items(held) };
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(initial)).Status);

            var cargo = new CargoCatalog(Capacity, [new CargoItemDefinition(Ore, 2), new CargoItemDefinition(Gem, 5), new CargoItemDefinition(Junk, 1)]);
            var prices = new StationPriceTable(Fx.Station, [
                (Ore, 10L, 7L),     // NEW provisional fixture prices
                (Gem, 100L, 80L),
                ("sellonly", null, 3L),
                ("buyonly", 4L, null),
            ]);
            var stations = new StationDirectory([new StationSite(Fx.Station, StationX, StationY, DockRadius)]);
            var time = new FixedTimeProvider(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
            var proc = new TransactionProcessor(repo, prices, new CatalogCargoPolicy(cargo), options, time);
            Assert.True((await proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
            return new Rig { Repo = repo, Proc = proc, Trading = new TradingService(proc, stations) };
        }

        private static System.Collections.Immutable.ImmutableSortedDictionary<string, int> Fx_Items(IEnumerable<(string, int)> items) =>
            System.Collections.Immutable.ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal,
                items.Select(i => new KeyValuePair<string, int>(i.Item1, i.Item2)));
    }

    // ---- Success -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Buy_Succeeds_DebitsWalletAndAddsItems()
    {
        var rig = await Rig.CreateAsync(wallet: 1000);

        var result = await rig.Buy(null, Ore, 3); // 3 * 10, weight 6 <= 20

        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(-30, result.CurrencyDelta);
        Assert.Equal(970, rig.Snapshot.Wallet);
        Assert.Equal(3, rig.Snapshot.Inventory[Ore]);
        Assert.Equal(1, rig.Snapshot.Revision);
    }

    [Fact]
    public async Task Sell_Succeeds_CreditsWalletAndRemovesItems()
    {
        var rig = await Rig.CreateAsync(wallet: 5, held: [(Ore, 4)]);

        var result = await rig.Sell(null, Ore, 4); // 4 * 7

        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(28, result.CurrencyDelta);
        Assert.Equal(33, rig.Snapshot.Wallet);
        Assert.DoesNotContain(Ore, rig.Snapshot.Inventory.Keys); // fully sold: no zero-quantity key left
    }

    [Fact]
    public async Task Sell_PartialQuantity_KeepsRemainder()
    {
        var rig = await Rig.CreateAsync(wallet: 0, held: [(Ore, 4)]);

        var result = await rig.Sell(null, Ore, 1);

        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(3, rig.Snapshot.Inventory[Ore]);
        Assert.Equal(7, rig.Snapshot.Wallet);
    }

    // ---- Rejections --------------------------------------------------------------------------------------------

    [Fact]
    public async Task Buy_WithInsufficientFunds_IsRejectedAndNothingChanges()
    {
        var rig = await Rig.CreateAsync(wallet: 29);
        var before = rig.Snapshot;

        var result = await rig.Buy(null, Ore, 3); // costs 30

        Assert.Equal(RejectionReason.InsufficientFunds, result.Reason);
        Assert.Same(before, rig.Snapshot);
    }

    [Fact]
    public async Task Buy_BeyondCargoCapacity_IsRejectedAllOrNothing()
    {
        var rig = await Rig.CreateAsync(wallet: 10_000, held: [(Gem, 3)]); // weight 15 of 20
        var before = rig.Snapshot;

        var result = await rig.Buy(null, Ore, 3); // +6 -> 21 > 20, even though money is plentiful

        Assert.Equal(RejectionReason.CargoCapacityExceeded, result.Reason);
        Assert.Same(before, rig.Snapshot);
        var fits = await rig.Buy(null, Ore, 2); // +4 -> 19 <= 20
        Assert.Equal(TransactionStatus.Applied, fits.Status);
        var full = await rig.Buy(null, Ore, 1); // 19 + 2 = 21 > 20
        Assert.Equal(RejectionReason.CargoCapacityExceeded, full.Reason);
        // Junk is known to the cargo data but no station sells it.
        Assert.Equal(RejectionReason.NotForSale, (await rig.Buy(null, Junk, 1)).Reason);
    }

    [Fact]
    public async Task Buy_ExactlyFillingCargo_Applies()
    {
        var rig = await Rig.CreateAsync(wallet: 10_000);

        var result = await rig.Buy(null, Ore, 10); // weight 20 == capacity

        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(RejectionReason.CargoCapacityExceeded, (await rig.Buy(null, Ore, 1)).Reason);
    }

    [Fact]
    public async Task Sell_NeverBlockedByFullCargo()
    {
        var rig = await Rig.CreateAsync(wallet: 0, held: [(Ore, 10)]); // exactly full

        var result = await rig.Sell(null, Ore, 10);

        Assert.Equal(TransactionStatus.Applied, result.Status);
    }

    [Fact]
    public async Task Sell_MoreThanHeld_IsRejected()
    {
        var rig = await Rig.CreateAsync(wallet: 0, held: [(Ore, 2)]);
        var before = rig.Snapshot;

        var result = await rig.Sell(null, Ore, 3);

        Assert.Equal(RejectionReason.InsufficientInventory, result.Reason);
        Assert.Same(before, rig.Snapshot);
    }

    [Fact]
    public async Task OneSidedAndUnlistedItems_AreNotForSale()
    {
        var rig = await Rig.CreateAsync(wallet: 100, held: [("sellonly", 2), ("buyonly", 2)]);

        Assert.Equal(RejectionReason.NotForSale, (await rig.Buy(null, "sellonly", 1)).Reason);   // station only buys it
        Assert.Equal(RejectionReason.NotForSale, (await rig.Sell(null, "buyonly", 1)).Reason);   // station only sells it
        Assert.Equal(RejectionReason.NotForSale, (await rig.Buy(null, "nothing", 1)).Reason);
    }

    // ---- Docking / proximity -----------------------------------------------------------------------------------

    [Fact]
    public async Task OutOfRange_IsRejectedForBuyAndSellAndNothingChanges()
    {
        var rig = await Rig.CreateAsync(wallet: 1000, held: [(Ore, 2)]);
        var before = rig.Snapshot;

        var buy = await rig.Buy(null, Ore, 1, x: StationX + DockRadius + 0.5f);
        var sell = await rig.Sell(null, Ore, 1, y: StationY - DockRadius - 0.5f);

        Assert.Equal(RejectionReason.StationAccessDenied, buy.Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, sell.Reason);
        Assert.Same(before, rig.Snapshot);
    }

    [Fact]
    public async Task DockBoundary_IsInclusiveInWorldUnits()
    {
        var rig = await Rig.CreateAsync(wallet: 1000);

        var onEdge = await rig.Buy(null, Ore, 1, x: StationX + DockRadius);
        var justOutside = await rig.Buy(null, Ore, 1, x: StationX + DockRadius + 0.01f);

        Assert.Equal(TransactionStatus.Applied, onEdge.Status);
        Assert.Equal(RejectionReason.StationAccessDenied, justOutside.Reason);
    }

    [Fact]
    public async Task UnknownStationAndNonFinitePosition_AreDenied()
    {
        var rig = await Rig.CreateAsync(wallet: 1000);

        Assert.Equal(RejectionReason.StationAccessDenied, (await rig.Buy(null, Ore, 1, station: "unknown-station")).Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, (await rig.Buy(null, Ore, 1, x: float.NaN)).Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, (await rig.Buy(null, Ore, 1, y: float.PositiveInfinity)).Reason);
        Assert.Equal(0, rig.Snapshot.Revision);
    }

    [Theory]
    [InlineData(0f, 0f, 3f, 4f, 5f, true)]    // 3-4-5 triangle lands exactly on the radius
    [InlineData(0f, 0f, 3f, 4f, 4.99f, false)]
    [InlineData(-10f, -10f, -10f, -10f, 0.5f, true)]
    [InlineData(1e7f, 1e7f, 1e7f, 1e7f + 8f, 8f, true)] // large coordinates, exact in double arithmetic
    public void Docking_PredicateGeometry(float sx, float sy, float px, float py, float radius, bool expected)
    {
        var site = new StationSite("s", sx, sy, radius);
        Assert.Equal(expected, Docking.IsDocked(site, px, py));
    }

    [Fact]
    public void StationDefinitions_AreValidated()
    {
        Assert.Throws<ArgumentException>(() => new StationSite("", 0, 0, 1));
        Assert.Throws<ArgumentException>(() => new StationSite("s", 0, 0, 0));
        Assert.Throws<ArgumentException>(() => new StationSite("s", float.NaN, 0, 1));
        Assert.Throws<ArgumentException>(() => new StationSite("s", 0, 0, float.PositiveInfinity));
        Assert.Throws<ArgumentException>(() => new StationDirectory([new StationSite("s", 0, 0, 1), new StationSite("s", 5, 5, 1)]));
    }

    // ---- Idempotency -------------------------------------------------------------------------------------------

    [Fact]
    public async Task ReplayedTransaction_ReturnsOriginalResultAndAppliesOnce()
    {
        var rig = await Rig.CreateAsync(wallet: 1000);
        var id = Fx.NewId();

        var first = await rig.Buy(id, Ore, 2);
        var replay = await rig.Buy(id, Ore, 2);

        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(first.CurrencyDelta, replay.CurrencyDelta);
        Assert.Equal(first.WalletAfter, replay.WalletAfter);
        Assert.Equal(980, rig.Snapshot.Wallet);
        Assert.Equal(2, rig.Snapshot.Inventory[Ore]);
        Assert.Equal(1, rig.Snapshot.Revision);
    }

    [Fact]
    public async Task ReplayAfterUndocking_StillReturnsTheOriginalResult()
    {
        var rig = await Rig.CreateAsync(wallet: 1000, held: [(Ore, 5)]);
        var id = Fx.NewId();
        var first = await rig.Sell(id, Ore, 5);

        var replay = await rig.Sell(id, Ore, 5, x: 99_999f); // retry after the ack was lost and the ship moved away

        Assert.Equal(TransactionStatus.Applied, replay.Status);
        Assert.True(replay.IsReplay);
        Assert.Equal(first.WalletAfter, replay.WalletAfter);
        Assert.Equal(1035, rig.Snapshot.Wallet);
    }

    [Fact]
    public async Task ReusedTransactionIdWithDifferentPayload_IsRejected()
    {
        var rig = await Rig.CreateAsync(wallet: 1000);
        var id = Fx.NewId();
        await rig.Buy(id, Ore, 1);

        var other = await rig.Buy(id, Ore, 2);

        Assert.Equal(RejectionReason.TransactionIdReused, other.Reason);
        Assert.Equal(990, rig.Snapshot.Wallet);
    }

    // ---- Atomicity ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task FailedWrite_LeavesWalletAndInventoryBothUntouched()
    {
        var inner = new InMemoryProfileRepository(new PersistenceOptions());
        var repo = new FaultInjectingProfileRepository(inner);
        var rig = await Rig.CreateAsync(wallet: 1000, held: [(Ore, 3)], repo: repo);
        var before = rig.Snapshot;
        repo.Mode = FaultMode.FailBeforeWrite;

        var sell = await rig.Sell(null, Ore, 3);

        Assert.Equal(TransactionStatus.Rejected, sell.Status);
        Assert.Equal(RejectionReason.PersistenceFailed, sell.Reason);
        Assert.Same(before, rig.Snapshot);                           // neither credits nor removal visible
        var stored = (await inner.LoadAsync(Fx.ProfileId)).Profile!;
        Assert.Equal(1000, stored.Wallet);
        Assert.Equal(3, stored.Inventory[Ore]);
        // Further trades are refused until the profile is reopened, then work normally.
        repo.Mode = FaultMode.None;
        Assert.Equal(RejectionReason.ProfileNotOpen, (await rig.Sell(null, Ore, 1)).Reason);
        Assert.True((await rig.Proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
        Assert.Equal(TransactionStatus.Applied, (await rig.Sell(null, Ore, 3)).Status);
        Assert.Equal(1021, rig.Snapshot.Wallet);
    }

    [Fact]
    public async Task AckLostAfterWrite_RetryAfterReopenAppliesExactlyOnce()
    {
        var inner = new InMemoryProfileRepository(new PersistenceOptions());
        var repo = new FaultInjectingProfileRepository(inner);
        var rig = await Rig.CreateAsync(wallet: 1000, repo: repo);
        repo.Mode = FaultMode.ThrowAfterWrite;
        var id = Fx.NewId();

        var lost = await rig.Buy(id, Gem, 1);
        Assert.Equal(RejectionReason.PersistenceFailed, lost.Reason);

        repo.Mode = FaultMode.None;
        Assert.True((await rig.Proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
        var retry = await rig.Buy(id, Gem, 1);

        Assert.Equal(TransactionStatus.Applied, retry.Status);
        Assert.True(retry.IsReplay);                 // the write had landed: the retry is recognised, not charged again
        Assert.Equal(900, rig.Snapshot.Wallet);
        Assert.Equal(1, rig.Snapshot.Inventory[Gem]);
    }

    // ---- Conservation ------------------------------------------------------------------------------------------

    [Fact]
    public async Task BuyThenSell_RoundTripLosesExactlyTheSpread()
    {
        var rig = await Rig.CreateAsync(wallet: 1000);

        await rig.Buy(null, Gem, 3);   // -300
        await rig.Sell(null, Gem, 3);  // +240

        Assert.Equal(940, rig.Snapshot.Wallet);        // spread (100-80)*3 = 60
        Assert.Empty(rig.Snapshot.Inventory);
    }

    [Fact]
    public async Task RandomTradeSequence_ConservesItemsAndCreditsAndNeverBreaksInvariants()
    {
        const long startWallet = 500;
        var rig = await Rig.CreateAsync(wallet: startWallet);
        var cargo = new CargoCatalog(Capacity, [new CargoItemDefinition(Ore, 2), new CargoItemDefinition(Gem, 5), new CargoItemDefinition(Junk, 1)]);
        var rng = new Random(20261016); // fixed seed
        var buyPrice = new Dictionary<string, long> { [Ore] = 10, [Gem] = 100 };
        var sellPrice = new Dictionary<string, long> { [Ore] = 7, [Gem] = 80 };
        var expectedWallet = startWallet;
        var expectedItems = new Dictionary<string, int> { [Ore] = 0, [Gem] = 0 };

        for (var i = 0; i < 400; i++)
        {
            var item = rng.Next(2) == 0 ? Ore : Gem;
            var qty = rng.Next(1, 8);
            var near = rng.Next(5) != 0;
            var x = near ? StationX + rng.Next(-20, 21) : StationX + 500;
            var result = rng.Next(2) == 0 ? await rig.Buy(null, item, qty, x: x) : await rig.Sell(null, item, qty, x: x);
            var wasBuy = result.CurrencyDelta < 0;
            if (result.Status == TransactionStatus.Applied)
            {
                Assert.True(near);
                expectedWallet += result.CurrencyDelta;
                foreach (var (k, v) in result.ItemDeltas) expectedItems[k] += v;
                Assert.Equal(wasBuy ? -buyPrice[item] * qty : sellPrice[item] * qty, result.CurrencyDelta);
            }
            else
            {
                Assert.Equal(0, result.CurrencyDelta);
                Assert.Empty(result.ItemDeltas);
            }

            var snap = rig.Snapshot;
            Assert.Equal(expectedWallet, snap.Wallet);
            Assert.True(snap.Wallet >= 0);
            Assert.All(snap.Inventory.Values, q => Assert.True(q > 0));
            Assert.True(cargo.Fits(snap.Inventory), "cargo weight exceeded capacity");
            foreach (var (k, v) in expectedItems) Assert.Equal(v, snap.Inventory.GetValueOrDefault(k));
            // ledger sum equals wallet movement (retention is large enough that nothing is trimmed in 400 steps)
            Assert.Equal(snap.Wallet, startWallet + snap.Ledger.Sum(e => e.Delta));
        }
    }

    // ---- Price source ------------------------------------------------------------------------------------------

    [Fact]
    public void ExplicitPriceTable_RejectsNegativeAndEmptyEntries()
    {
        Assert.Throws<ArgumentException>(() => new StationPriceTable("s", [("a", -1L, null)]));
        Assert.Throws<ArgumentException>(() => new StationPriceTable("s", [("", 1L, null)]));
        Assert.Throws<ArgumentException>(() => new StationPriceTable([""], [("a", 1L, null)]));
        Assert.Throws<ArgumentException>(() => new StationPriceTable("s", [("a", 1L, null), ("a", null, 2L)])); // no silent overwrite
    }

    [Fact]
    public void OriginalBinding_UsesRawValuesAndTreatsSentinelAsUntraded()
    {
        // Synthetic rows in the pinned 31-field ITM layout: f[7] mass, f[8] buy, f[15] sell.
        string Row(int id, string name, int mass, int buy, int sell)
        {
            var f = Enumerable.Repeat("0", 31).ToArray();
            f[0] = "4"; f[1] = "v57"; f[2] = id.ToString(); f[3] = name; f[4] = "Resources";
            f[7] = mass.ToString(); f[8] = buy.ToString(); f[15] = sell.ToString();
            return string.Join(",", f);
        }
        var text = string.Join("\n", Row(1, "Both", 2, 50, 20), Row(2, "Sentinel", 1, 0, -1), Row(3, "Free", 1, 0, 0), Row(4, "SellOnly", 1, 0, 500));
        var catalog = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(text, "ITM/synthetic.itm"));

        var table = StationPriceTable.FromOriginalItems(["st-1", "st-2"], catalog);

        Assert.True(table.TryGetBuyUnitPrice("st-2", "1", out var buy));
        Assert.Equal(50, buy);
        Assert.True(table.TryGetSellUnitPrice("st-1", "1", out var sell));
        Assert.Equal(20, sell);
        Assert.False(table.TryGetSellUnitPrice("st-1", "2", out _));   // raw -1 is not traded
        Assert.False(table.TryGetBuyUnitPrice("st-1", "2", out _));
        Assert.False(table.TryGetSellUnitPrice("st-1", "3", out _));   // raw 0 is not traded
        Assert.False(table.TryGetBuyUnitPrice("st-1", "4", out _));    // sell without buy: station buys, never sells
        Assert.True(table.TryGetSellUnitPrice("st-1", "4", out var odd));
        Assert.Equal(500, odd);
        Assert.False(table.TryGetBuyUnitPrice("other-station", "1", out _));
        Assert.Contains(table.RawPrices, p => p.ItemId == "2" && p.RawBuy == 0 && p.RawSell == -1); // sentinel kept raw
        Assert.False(table.TryGetUpgradePrice("st-1", "anything", out _));
    }

    [RequiresOriginalAssetsFact]
    public async Task OriginalBinding_AgainstPinnedItmCatalog_TradesEndToEnd()
    {
        var catalog = OriginalItemCatalog.Load(OriginalAssets.Root!);
        var table = StationPriceTable.FromOriginalItems([Fx.Station], catalog);

        Assert.Equal(200, table.RawPrices.Count);
        foreach (var item in catalog.Items)
        {
            var id = item.Id.ToString();
            Assert.Equal(item.Buy.Value > 0, table.TryGetBuyUnitPrice(Fx.Station, id, out var b));
            if (item.Buy.Value > 0) Assert.Equal(item.Buy.Value, b);
            Assert.Equal(item.Sell.Value > 0, table.TryGetSellUnitPrice(Fx.Station, id, out var s));
            if (item.Sell.Value > 0) Assert.Equal(item.Sell.Value, s);
        }
        Assert.Equal(34, table.RawPrices.Count(p => p.RawSell == -1)); // RT-F049 audit: sentinel rows stay raw

        // Priced items trade through the full pipeline; cargo weights are the raw ITM masses and the capacity is a NEW fixture.
        var traded = catalog.Items.First(i => i.Buy.Value > 0 && i.Sell.Value > 0);
        var cargo = new CargoCatalog(long.MaxValue / 4, catalog.Items.Select(i => new CargoItemDefinition(i.Id.ToString(), i.Mass.Value)));
        var options = new PersistenceOptions();
        var repo = new InMemoryProfileRepository(options);
        await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, 1_000_000, Fx.Hull));
        var proc = new TransactionProcessor(repo, table, new CatalogCargoPolicy(cargo), options);
        await proc.OpenProfileAsync(Fx.ProfileId);
        var trading = new TradingService(proc, new StationDirectory([new StationSite(Fx.Station, 0, 0, 10)]));

        var buy = await trading.BuyAsync(Fx.ProfileId, 1, 1, new BuyRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, traded.Id.ToString(), 2));
        Assert.Equal(TransactionStatus.Applied, buy.Status);
        Assert.Equal(-2L * traded.Buy.Value, buy.CurrencyDelta);
        var sell = await trading.SellAsync(Fx.ProfileId, 1, 1, new SellRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, traded.Id.ToString(), 2));
        Assert.Equal(2L * traded.Sell.Value, sell.CurrencyDelta);
        Assert.Empty(proc.GetSnapshot(Fx.ProfileId)!.Inventory);

        var sentinel = catalog.Items.First(i => i.Sell.Value == -1);
        var held = PlayerProfile.CreateNew("fixture-profile-2", 0, Fx.Hull) with
        {
            Inventory = System.Collections.Immutable.ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal,
                [new KeyValuePair<string, int>(sentinel.Id.ToString(), 1)])
        };
        await repo.CreateAsync(held);
        await proc.OpenProfileAsync("fixture-profile-2");
        var refused = await trading.SellAsync("fixture-profile-2", 1, 1, new SellRequest(Fx.NewId(), "fixture-profile-2", Fx.Station, sentinel.Id.ToString(), 1));
        Assert.Equal(RejectionReason.NotForSale, refused.Reason);
    }

    // ---- Cargo policy adapter ----------------------------------------------------------------------------------

    [Fact]
    public void CatalogCargoPolicy_MatchesCatalogFits_AndRefusesUnknownItems()
    {
        var cargo = new CargoCatalog(10, [new CargoItemDefinition(Ore, 2)]);
        var policy = new CatalogCargoPolicy(cargo);
        var profile = PlayerProfile.CreateNew(Fx.ProfileId, 0, null);

        Assert.True(policy.Fits(profile, new Dictionary<string, int> { [Ore] = 5 }));
        Assert.False(policy.Fits(profile, new Dictionary<string, int> { [Ore] = 6 }));
        Assert.False(policy.Fits(profile, new Dictionary<string, int> { ["mystery"] = 1 }));
        Assert.Throws<ArgumentNullException>(() => new CatalogCargoPolicy(null!));
    }
}
