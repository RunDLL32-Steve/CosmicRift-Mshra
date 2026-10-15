using RogueTrader.Persistence;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>Master plan 20.2 economy bullets plus the 9.3 validation steps. All fixture numbers are NEW provisional test data.</summary>
public sealed class EconomyTransactionTests
{
    // ---- 20.2 (1) Cannot spend money they do not have ------------------------------------------------------

    [Fact]
    public async Task Buy_WithInsufficientFunds_IsRejectedAndProfileUnchanged()
    {
        var env = await Env.CreateAsync(wallet: 5);
        var before = env.Snapshot;

        var result = await env.Buy(null, Fx.OreA, 1); // price 10 > wallet 5

        Assert.Equal(TransactionStatus.Rejected, result.Status);
        Assert.Equal(RejectionReason.InsufficientFunds, result.Reason);
        Assert.Same(before, env.Snapshot);
        var stored = await env.Repo.LoadAsync(Fx.ProfileId);
        ProfileAssert.Equivalent(before, stored.Profile!);
    }

    [Fact]
    public async Task BuyUpgrade_WithInsufficientFunds_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 499); // upgrade price 500

        var result = await env.BuyUpgrade(null, Fx.UpgradeX);

        Assert.Equal(RejectionReason.InsufficientFunds, result.Reason);
        Assert.Empty(env.Snapshot.Upgrades);
        Assert.Equal(499, env.Snapshot.Wallet);
        Assert.Equal(0, env.Snapshot.Revision);
    }

    [Fact]
    public async Task Buy_ExactlyAffordable_Applies()
    {
        var env = await Env.CreateAsync(wallet: 30);

        var result = await env.Buy(null, Fx.OreA, 3);

        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(0, env.Snapshot.Wallet);
    }

    // ---- 20.2 (2) Cargo cannot become negative --------------------------------------------------------------

    [Fact]
    public async Task Sell_MoreThanHeld_IsRejectedAndInventoryUnchanged()
    {
        var env = await Env.CreateAsync();
        await env.Reward(null, 0, (Fx.OreA, 3));
        var before = env.Snapshot;

        var result = await env.Sell(null, Fx.OreA, 4);

        Assert.Equal(RejectionReason.InsufficientInventory, result.Reason);
        Assert.Same(before, env.Snapshot);
        Assert.Equal(3, env.Snapshot.Inventory[Fx.OreA]);
    }

    [Fact]
    public async Task Sell_ItemNotHeld_IsRejected()
    {
        var env = await Env.CreateAsync();

        var result = await env.Sell(null, Fx.OreA, 1);

        Assert.Equal(RejectionReason.InsufficientInventory, result.Reason);
        Assert.Empty(env.Snapshot.Inventory);
    }

    [Fact]
    public async Task Sell_AllUnits_RemovesEntry()
    {
        var env = await Env.CreateAsync(wallet: 0);
        await env.Reward(null, 0, (Fx.OreA, 3), (Fx.OddItem, 1));

        var result = await env.Sell(null, Fx.OreA, 3);

        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.False(env.Snapshot.Inventory.ContainsKey(Fx.OreA));
        Assert.Equal(1, env.Snapshot.Inventory[Fx.OddItem]);
        Assert.Empty(ProfileInvariants.Validate(env.Snapshot, env.Options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task NonPositiveQuantity_IsRejected(int quantity)
    {
        var env = await Env.CreateAsync();
        await env.Reward(null, 0, (Fx.OreA, 3));
        var before = env.Snapshot;

        Assert.Equal(RejectionReason.InvalidRequest, (await env.Buy(null, Fx.OreA, quantity)).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Sell(null, Fx.OreA, quantity)).Reason);
        Assert.Same(before, env.Snapshot);
    }

    // ---- 20.2 (3) Duplicate requests create no duplicate rewards -------------------------------------------

    [Fact]
    public async Task GrantReward_DuplicateId_IsNoOpReturningOriginalResult()
    {
        var env = await Env.CreateAsync(wallet: 100);
        var first = await env.Reward("fixture-tx-reward-1", 50, (Fx.OreA, 2));
        var afterFirst = env.Snapshot;

        var second = await env.Reward("fixture-tx-reward-1", 50, (Fx.OreA, 2));

        Assert.False(first.IsReplay);
        Assert.True(second.IsReplay);
        Assert.Equal(TransactionStatus.Applied, second.Status);
        Assert.Equal(first.WalletAfter, second.WalletAfter);
        Assert.Equal(first.Revision, second.Revision);
        Assert.Equal(first.CurrencyDelta, second.CurrencyDelta);
        Assert.Equal(first.ItemDeltas.ToArray(), second.ItemDeltas.ToArray());
        Assert.Same(afterFirst, env.Snapshot);
        Assert.Equal(150, env.Snapshot.Wallet);
        Assert.Equal(2, env.Snapshot.Inventory[Fx.OreA]);
    }

    [Fact]
    public async Task GrantReward_DuplicateAfterRestart_IsNoOp()
    {
        using var dir = new TempDirectory();
        var options = new PersistenceOptions();
        using (var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>()))
        {
            var env = await Env.CreateAsync(wallet: 100, repo: repo, options: options);
            var first = await env.Reward("fixture-tx-reward-1", 50, (Fx.OreA, 2));
            Assert.Equal(TransactionStatus.Applied, first.Status);
        }

        using (var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>()))
        {
            var env = await Env.CreateAsync(repo: repo, options: options, createProfile: false);
            var again = await env.Reward("fixture-tx-reward-1", 50, (Fx.OreA, 2));

            Assert.True(again.IsReplay);
            Assert.Equal(TransactionStatus.Applied, again.Status);
            Assert.Equal(150, again.WalletAfter);
            Assert.Equal(150, env.Snapshot.Wallet);
            Assert.Equal(2, env.Snapshot.Inventory[Fx.OreA]);
            Assert.Equal(1, env.Snapshot.Revision);
        }
    }

    [Fact]
    public async Task Buy_DuplicateId_DoesNotChargeTwice()
    {
        var env = await Env.CreateAsync(wallet: 100);
        var first = await env.Buy("fixture-tx-buy-1", Fx.OreA, 4);

        // Even if funds changed since, the replay returns the original outcome.
        await env.Reward(null, 0, (Fx.OddItem, 1));
        var again = await env.Buy("fixture-tx-buy-1", Fx.OreA, 4);

        Assert.True(again.IsReplay);
        Assert.Equal(first.WalletAfter, again.WalletAfter);
        Assert.Equal(60, env.Snapshot.Wallet);
        Assert.Equal(4, env.Snapshot.Inventory[Fx.OreA]);
    }

    [Fact]
    public async Task TransactionId_ReusedWithDifferentPayload_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 100);
        await env.Buy("fixture-tx-1", Fx.OreA, 1);
        var before = env.Snapshot;

        var reuse = await env.Buy("fixture-tx-1", Fx.OreA, 2);
        var reuseKind = await env.Sell("fixture-tx-1", Fx.OreA, 1);

        Assert.Equal(RejectionReason.TransactionIdReused, reuse.Reason);
        Assert.Equal(RejectionReason.TransactionIdReused, reuseKind.Reason);
        Assert.False(reuse.IsReplay);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task TransactionId_ReusedWithSeparatorCharactersInIds_IsStillDistinguished()
    {
        // Fingerprints are length-prefixed: "a|b" + "c" must not equal "a" + "b|c".
        var env = await Env.CreateAsync(wallet: 1000);
        env.Prices.Buy[("st|a", "x")] = 1;  // NEW provisional test fixture
        env.Prices.Buy[("st", "a|x")] = 1;  // NEW provisional test fixture
        await env.Buy("fixture-tx-1", "x", 1, station: "st|a");

        var other = await env.Buy("fixture-tx-1", "a|x", 1, station: "st");

        Assert.Equal(RejectionReason.TransactionIdReused, other.Reason);
    }

    // ---- 20.2 (4) Prices calculated correctly -----------------------------------------------------------------

    [Fact]
    public async Task Buy_ChargesAuthoritativeUnitPriceTimesQuantity()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        env.Prices.Buy[(Fx.Station, Fx.OreA)] = 13; // NEW provisional test fixture

        var result = await env.Buy(null, Fx.OreA, 7);

        Assert.Equal(-91, result.CurrencyDelta);
        Assert.Equal(909, result.WalletAfter);
        Assert.Equal(909, env.Snapshot.Wallet);
        Assert.Equal(7, env.Snapshot.Inventory[Fx.OreA]);
        Assert.Equal(7, result.ItemDeltas[Fx.OreA]);
    }

    [Fact]
    public async Task Sell_CreditsAuthoritativeUnitPriceTimesQuantity()
    {
        var env = await Env.CreateAsync(wallet: 0);
        env.Prices.Sell[(Fx.Station, Fx.OreA)] = 9; // NEW provisional test fixture
        await env.Reward(null, 0, (Fx.OreA, 10));

        var result = await env.Sell(null, Fx.OreA, 6);

        Assert.Equal(54, result.CurrencyDelta);
        Assert.Equal(54, env.Snapshot.Wallet);
        Assert.Equal(4, env.Snapshot.Inventory[Fx.OreA]);
        Assert.Equal(-6, result.ItemDeltas[Fx.OreA]);
    }

    [Fact]
    public async Task Buy_UnpricedItem_IsRejectedAsNotForSale()
    {
        var env = await Env.CreateAsync();
        await env.Reward(null, 0, (Fx.OreA, 1));

        Assert.Equal(RejectionReason.NotForSale, (await env.Buy(null, "fixture-unpriced-item", 1)).Reason);
        Assert.Equal(RejectionReason.NotForSale, (await env.Buy(null, Fx.OreA, 1, station: Fx.OtherStation)).Reason);
        Assert.Equal(RejectionReason.NotForSale, (await env.Sell(null, Fx.OreA, 1, station: Fx.OtherStation)).Reason);
        Assert.Equal(RejectionReason.NotForSale, (await env.BuyUpgrade(null, "fixture-unpriced-upgrade")).Reason);
    }

    [Fact]
    public async Task NegativePrice_IsRejectedAsInvalidPrice()
    {
        var env = await Env.CreateAsync();
        await env.Reward(null, 0, (Fx.OreA, 1));
        env.Prices.Buy[(Fx.Station, Fx.OreA)] = -1;       // NEW provisional test fixture (deliberately invalid)
        env.Prices.Sell[(Fx.Station, Fx.OreA)] = -1;      // NEW provisional test fixture (deliberately invalid)
        env.Prices.Upgrade[(Fx.Station, Fx.UpgradeX)] = -1; // NEW provisional test fixture (deliberately invalid)
        var before = env.Snapshot;

        Assert.Equal(RejectionReason.InvalidPrice, (await env.Buy(null, Fx.OreA, 1)).Reason);
        Assert.Equal(RejectionReason.InvalidPrice, (await env.Sell(null, Fx.OreA, 1)).Reason);
        Assert.Equal(RejectionReason.InvalidPrice, (await env.BuyUpgrade(null, Fx.UpgradeX)).Reason);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task Buy_TotalPriceOverflow_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: long.MaxValue);
        env.Prices.Buy[(Fx.Station, Fx.OreA)] = long.MaxValue / 2; // NEW provisional test fixture
        var before = env.Snapshot;

        var result = await env.Buy(null, Fx.OreA, 3);

        Assert.Equal(RejectionReason.ArithmeticOverflow, result.Reason);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task Sell_WalletOverflow_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: long.MaxValue - 10);
        await env.Reward(null, 0, (Fx.OreA, 1));
        env.Prices.Sell[(Fx.Station, Fx.OreA)] = 100; // NEW provisional test fixture
        var before = env.Snapshot;

        var result = await env.Sell(null, Fx.OreA, 1);

        Assert.Equal(RejectionReason.ArithmeticOverflow, result.Reason);
        Assert.Same(before, env.Snapshot);
        Assert.Equal(1, env.Snapshot.Inventory[Fx.OreA]);
    }

    [Fact]
    public async Task Sell_PriceTimesQuantityOverflow_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 0);
        await env.Reward(null, 0, (Fx.OreA, 3));
        env.Prices.Sell[(Fx.Station, Fx.OreA)] = long.MaxValue / 2; // NEW provisional test fixture

        Assert.Equal(RejectionReason.ArithmeticOverflow, (await env.Sell(null, Fx.OreA, 3)).Reason);
    }

    [Fact]
    public async Task Reward_CurrencyOverflow_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: long.MaxValue - 5);

        var result = await env.Reward(null, 6);

        Assert.Equal(RejectionReason.ArithmeticOverflow, result.Reason);
        Assert.Equal(long.MaxValue - 5, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Reward_ItemQuantityOverflow_IsRejected()
    {
        var env = await Env.CreateAsync(cargo: new FixtureCargoPolicy { Limit = long.MaxValue });
        Assert.Equal(TransactionStatus.Applied, (await env.Reward(null, 0, (Fx.OreA, int.MaxValue))).Status);

        var result = await env.Reward(null, 0, (Fx.OreA, 1));

        Assert.Equal(RejectionReason.ArithmeticOverflow, result.Reason);
        Assert.Equal(int.MaxValue, env.Snapshot.Inventory[Fx.OreA]);
    }

    [Fact]
    public async Task Buy_ItemQuantityOverflow_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 100, cargo: new FixtureCargoPolicy { Limit = long.MaxValue });
        await env.Reward(null, 0, (Fx.OreA, int.MaxValue));

        var result = await env.Buy(null, Fx.OreA, 1);

        Assert.Equal(RejectionReason.ArithmeticOverflow, result.Reason);
        Assert.Equal(100, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Applied_AppendsLedgerEntryWithBalanceAfter()
    {
        var env = await Env.CreateAsync(wallet: 100);

        var buy = await env.Buy("fixture-tx-1", Fx.OreA, 3);       // -30
        var reward = await env.Reward("fixture-tx-2", 0, (Fx.OddItem, 1)); // delta 0 still ledgered
        var sell = await env.Sell("fixture-tx-3", Fx.OreA, 2);       // +14

        var ledger = env.Snapshot.Ledger;
        Assert.Equal(3, ledger.Count);
        Assert.Equal(new[] { 1L, 2L, 3L }, ledger.Select(e => e.Sequence));
        Assert.Equal(new[] { -30L, 0L, 14L }, ledger.Select(e => e.Delta));
        Assert.Equal(new[] { 70L, 70L, 84L }, ledger.Select(e => e.BalanceAfter));
        Assert.Equal(new[] { "fixture-tx-1", "fixture-tx-2", "fixture-tx-3" }, ledger.Select(e => e.TransactionId));
        Assert.Equal(new[] { TransactionKind.Buy, TransactionKind.GrantReward, TransactionKind.Sell }, ledger.Select(e => e.Kind));
        Assert.Equal(4, env.Snapshot.NextLedgerSequence);
        Assert.Equal(new[] { 1L, 2L, 3L }, new[] { buy.Revision, reward.Revision, sell.Revision });
        Assert.All(ledger, e => Assert.Equal(env.Time.GetUtcNow(), e.AtUtc));
    }

    [Fact]
    public async Task BuyUpgrade_DebitsPriceAndRecordsUpgrade()
    {
        var env = await Env.CreateAsync(wallet: 800);

        var result = await env.BuyUpgrade(null, Fx.UpgradeX);

        Assert.Equal(Fx.UpgradeX, result.UpgradeId);
        Assert.Equal(-500, result.CurrencyDelta);
        Assert.Equal(300, env.Snapshot.Wallet);
        Assert.Contains(Fx.UpgradeX, env.Snapshot.Upgrades);
    }

    // ---- 20.2 (5) Save/load preserves currency and inventory --------------------------------------------------

    [Fact]
    public async Task ExitAndReturn_RewardSellUpgrade_ProgressionIntact()
    {
        using var dir = new TempDirectory();
        var options = new PersistenceOptions();
        PlayerProfile beforeExit;
        using (var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>()))
        {
            var env = await Env.CreateAsync(wallet: 0, repo: repo, options: options);
            Assert.Equal(TransactionStatus.Applied, (await env.Reward(null, 0, (Fx.OreA, 100), (Fx.OddItem, 5))).Status);
            Assert.Equal(TransactionStatus.Applied, (await env.Sell(null, Fx.OreA, 80)).Status);    // +560 (NEW fixture price 7)
            Assert.Equal(TransactionStatus.Applied, (await env.BuyUpgrade(null, Fx.UpgradeX)).Status); // -500
            beforeExit = env.Snapshot;
        }

        using (var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>()))
        {
            var env = await Env.CreateAsync(repo: repo, options: options, createProfile: false);
            ProfileAssert.Equivalent(beforeExit, env.Snapshot);
            Assert.Equal(60, env.Snapshot.Wallet);
            Assert.Equal(20, env.Snapshot.Inventory[Fx.OreA]);
            Assert.Equal(5, env.Snapshot.Inventory[Fx.OddItem]);
            Assert.Contains(Fx.UpgradeX, env.Snapshot.Upgrades);
            Assert.Equal(Fx.Hull, env.Snapshot.ShipHullId);

            // The returned player can keep trading.
            Assert.Equal(TransactionStatus.Applied, (await env.Sell(null, Fx.OreA, 20)).Status);
            Assert.Equal(200, env.Snapshot.Wallet);
        }
    }

    // ---- Validation and integrity -------------------------------------------------------------------------------

    [Fact]
    public async Task Identity_Mismatch_IsRejected()
    {
        var env = await Env.CreateAsync();
        var before = env.Snapshot;

        var result = await env.Proc.BuyAsync(new TransactionContext("fixture-profile-OTHER", true),
            new BuyRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, Fx.OreA, 1));

        Assert.Equal(RejectionReason.IdentityMismatch, result.Reason);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task ProfileNotOpen_IsRejected()
    {
        var env = await Env.CreateAsync();
        var fresh = new TransactionProcessor(env.Repo, env.Prices, env.Cargo, env.Options);

        var result = await fresh.BuyAsync(env.Ctx(), new BuyRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, Fx.OreA, 1));

        Assert.Equal(RejectionReason.ProfileNotOpen, result.Reason);
        Assert.Null(fresh.GetSnapshot(Fx.ProfileId));
    }

    [Fact]
    public async Task StationAccess_NotVerified_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 100);
        await env.Reward(null, 0, (Fx.OreA, 1));
        var before = env.Snapshot;

        Assert.Equal(RejectionReason.StationAccessDenied, (await env.Buy(null, Fx.OreA, 1, stationAccess: false)).Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, (await env.Sell(null, Fx.OreA, 1, stationAccess: false)).Reason);
        Assert.Equal(RejectionReason.StationAccessDenied, (await env.BuyUpgrade(null, Fx.UpgradeX, stationAccess: false)).Reason);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task GrantReward_DoesNotRequireStationAccess()
    {
        var env = await Env.CreateAsync(wallet: 0);

        var result = await env.Proc.GrantRewardAsync(env.Ctx(stationAccess: false),
            new GrantRewardRequest(Fx.NewId(), Fx.ProfileId, "fixture-source-1", 25, new Dictionary<string, int> { [Fx.OreA] = 2 }));

        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(25, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Buy_ExceedingCargoPolicy_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 1000, cargo: new FixtureCargoPolicy { Limit = 5 }); // NEW fixture limit
        await env.Buy(null, Fx.OreA, 4);
        var before = env.Snapshot;

        var result = await env.Buy(null, Fx.OreA, 2);

        Assert.Equal(RejectionReason.CargoCapacityExceeded, result.Reason);
        Assert.Same(before, env.Snapshot);
        Assert.Equal(TransactionStatus.Applied, (await env.Buy(null, Fx.OreA, 1)).Status);
    }

    [Fact]
    public async Task Sell_WhenOverCargoLimit_IsStillAllowed()
    {
        // The cargo policy is consulted only when a quantity increases.
        var env = await Env.CreateAsync(wallet: 0, cargo: new FixtureCargoPolicy { Limit = 100 });
        await env.Reward(null, 0, (Fx.OreA, 10));
        env.Cargo.Limit = 1; // NEW fixture: shrink the limit below current holdings

        Assert.Equal(TransactionStatus.Applied, (await env.Sell(null, Fx.OreA, 2)).Status);
    }

    [Fact]
    public async Task GrantReward_ExceedingCargoPolicy_IsRejectedWhole()
    {
        var env = await Env.CreateAsync(wallet: 0, cargo: new FixtureCargoPolicy { Limit = 5 }); // NEW fixture limit
        var before = env.Snapshot;

        var result = await env.Reward(null, 100, (Fx.OreA, 3), (Fx.OddItem, 3));

        Assert.Equal(RejectionReason.CargoCapacityExceeded, result.Reason);
        Assert.Same(before, env.Snapshot);
        Assert.Equal(0, env.Snapshot.Wallet);
        Assert.Empty(env.Snapshot.Inventory);
    }

    [Fact]
    public async Task BuyUpgrade_AlreadyOwned_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 2000);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        var before = env.Snapshot;

        var result = await env.BuyUpgrade(null, Fx.UpgradeX);

        Assert.Equal(RejectionReason.UpgradeAlreadyOwned, result.Reason);
        Assert.Same(before, env.Snapshot);
        Assert.Equal(1500, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task InvalidShapes_AreRejected()
    {
        var env = await Env.CreateAsync(wallet: 100);
        var before = env.Snapshot;
        var ctx = env.Ctx();

        Assert.Equal(RejectionReason.InvalidRequest, (await env.Proc.BuyAsync(ctx, new BuyRequest("", Fx.ProfileId, Fx.Station, Fx.OreA, 1))).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Proc.BuyAsync(ctx, new BuyRequest(new string('t', 129), Fx.ProfileId, Fx.Station, Fx.OreA, 1))).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Proc.BuyAsync(ctx, new BuyRequest(Fx.NewId(), Fx.ProfileId, "", Fx.OreA, 1))).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Proc.BuyAsync(ctx, new BuyRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, "", 1))).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Proc.BuyUpgradeAsync(ctx, new BuyUpgradeRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, ""))).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Reward(null, -1, (Fx.OreA, 1))).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Reward(null, 0)).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Reward(null, 5, (Fx.OreA, 0))).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Reward(null, 5, (Fx.OreA, -3))).Reason);
        Assert.Equal(RejectionReason.InvalidRequest, (await env.Reward(null, 5, ("", 3))).Reason);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task RejectedTransactionId_CanBeRetried()
    {
        var env = await Env.CreateAsync(wallet: 5);
        Assert.Equal(RejectionReason.InsufficientFunds, (await env.Buy("fixture-tx-1", Fx.OreA, 1)).Reason);
        await env.Reward(null, 100);

        var retry = await env.Buy("fixture-tx-1", Fx.OreA, 1);

        Assert.Equal(TransactionStatus.Applied, retry.Status);
        Assert.False(retry.IsReplay);
    }

    [Fact]
    public async Task IdempotencyWindow_EvictsOldestBeyondCapacity()
    {
        var options = new PersistenceOptions { IdempotencyWindow = 3, LedgerRetention = 4 }; // NEW fixture limits
        var env = await Env.CreateAsync(wallet: 0, options: options);
        for (var i = 1; i <= 5; i++) await env.Reward($"fixture-tx-{i}", 10);

        Assert.Equal(new[] { "fixture-tx-3", "fixture-tx-4", "fixture-tx-5" }, env.Snapshot.AppliedTransactions.Select(t => t.TransactionId));
        Assert.Equal(new[] { 2L, 3L, 4L, 5L }, env.Snapshot.Ledger.Select(e => e.Sequence));
        Assert.Equal(6, env.Snapshot.NextLedgerSequence);

        // Inside the window: replay. Beyond it: indistinguishable from a new request (documented limit).
        Assert.True((await env.Reward("fixture-tx-5", 10)).IsReplay);
        var evicted = await env.Reward("fixture-tx-1", 10);
        Assert.False(evicted.IsReplay);
        Assert.Equal(60, env.Snapshot.Wallet);
        Assert.Empty(ProfileInvariants.Validate(env.Snapshot, options));
    }

    [Fact]
    public async Task SaveFailure_RollsBackAndRequiresReopen()
    {
        var inner = new InMemoryProfileRepository();
        var faulty = new FaultInjectingProfileRepository(inner);
        var env = await Env.CreateAsync(wallet: 100, repo: faulty);
        var before = env.Snapshot;
        faulty.Mode = FaultMode.FailBeforeWrite;

        var failed = await env.Buy("fixture-tx-1", Fx.OreA, 2);

        Assert.Equal(RejectionReason.PersistenceFailed, failed.Reason);
        Assert.Same(before, env.Snapshot);
        Assert.Equal(100, env.Snapshot.Wallet);

        // Until the profile is reopened, nothing else is processed.
        faulty.Mode = FaultMode.None;
        Assert.Equal(RejectionReason.ProfileNotOpen, (await env.Buy("fixture-tx-2", Fx.OreA, 1)).Reason);

        Assert.True((await env.Proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
        var retry = await env.Buy("fixture-tx-1", Fx.OreA, 2);
        Assert.Equal(TransactionStatus.Applied, retry.Status);
        Assert.False(retry.IsReplay);
        Assert.Equal(80, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task SaveThrowsAfterWrite_RetryAfterReopenReturnsReplay()
    {
        var faulty = new FaultInjectingProfileRepository(new InMemoryProfileRepository());
        var env = await Env.CreateAsync(wallet: 100, repo: faulty);
        faulty.Mode = FaultMode.ThrowAfterWrite;

        var failed = await env.Buy("fixture-tx-1", Fx.OreA, 2);
        Assert.Equal(RejectionReason.PersistenceFailed, failed.Reason); // unacknowledged although the write landed

        faulty.Mode = FaultMode.None;
        Assert.True((await env.Proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
        var retry = await env.Buy("fixture-tx-1", Fx.OreA, 2);

        Assert.True(retry.IsReplay);
        Assert.Equal(TransactionStatus.Applied, retry.Status);
        Assert.Equal(80, retry.WalletAfter);
        Assert.Equal(80, env.Snapshot.Wallet); // charged exactly once
        Assert.Equal(2, env.Snapshot.Inventory[Fx.OreA]);
    }
}
