using RogueTrader.OriginalData;
using RogueTrader.Persistence;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>
/// Queue M5: repair purchases and per-family upgrade purchases through the transaction core. Every id, price and wallet below is a
/// NEW provisional test fixture, not game balance, unless a test says it reads original data. Original RT-F047/F048/F049 stay open.
/// </summary>
public sealed class RepairAndUpgradeTests
{
    // ---- Repair ------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Repair_DebitsPriceAndTouchesNothingElse()
    {
        var env = await Env.CreateAsync(wallet: 500);
        await env.Reward(null, 0, ("fixture-ore-a", 3));
        var before = env.Snapshot;

        var result = await env.Repair(null, Fx.RepairMinor);

        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(Fx.RepairMinor, result.RepairId);
        Assert.Equal(-40, result.CurrencyDelta);
        Assert.Equal(460, result.WalletAfter);
        Assert.Equal(460, env.Snapshot.Wallet);
        Assert.Equal(before.Inventory.ToArray(), env.Snapshot.Inventory.ToArray());
        Assert.Equal(before.Upgrades.ToArray(), env.Snapshot.Upgrades.ToArray());
        Assert.Equal(before.ShipHullId, env.Snapshot.ShipHullId);
        Assert.Equal(before.Revision + 1, env.Snapshot.Revision);
        Assert.Equal(TransactionKind.Repair, env.Snapshot.Ledger[^1].Kind);
    }

    [Fact]
    public async Task Repair_CanBeBoughtRepeatedly()
    {
        var env = await Env.CreateAsync(wallet: 500);

        Assert.Equal(TransactionStatus.Applied, (await env.Repair(null, Fx.RepairMinor)).Status);
        Assert.Equal(TransactionStatus.Applied, (await env.Repair(null, Fx.RepairMinor)).Status);

        Assert.Equal(420, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Repair_InsufficientFunds_ChangesNothing()
    {
        var env = await Env.CreateAsync(wallet: 100);
        var before = env.Snapshot;

        var result = await env.Repair(null, Fx.RepairFull);

        Assert.Equal(RejectionReason.InsufficientFunds, result.Reason);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task Repair_ExactFunds_LeavesZero()
    {
        var env = await Env.CreateAsync(wallet: 200);

        Assert.Equal(TransactionStatus.Applied, (await env.Repair(null, Fx.RepairFull)).Status);

        Assert.Equal(0, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Repair_NotOfferedAtThisStationOrNotAtAll_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 500);

        Assert.Equal(RejectionReason.NotForSale, (await env.Repair(null, "fixture-unpriced-repair")).Reason);
        Assert.Equal(RejectionReason.NotForSale, (await env.Repair(null, Fx.RepairMinor, station: Fx.OtherStation)).Reason);
        Assert.Equal(500, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Repair_RequiresVerifiedStationAccess()
    {
        var env = await Env.CreateAsync(wallet: 500);

        Assert.Equal(RejectionReason.StationAccessDenied, (await env.Repair(null, Fx.RepairMinor, stationAccess: false)).Reason);
        Assert.Equal(500, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Repair_NegativePrice_IsRejectedAsInvalid()
    {
        var env = await Env.CreateAsync(wallet: 500);
        env.Prices.Repair[(Fx.Station, Fx.RepairMinor)] = -1;

        Assert.Equal(RejectionReason.InvalidPrice, (await env.Repair(null, Fx.RepairMinor)).Reason);
        Assert.Equal(500, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Repair_Replay_ReturnsOriginalResultWithoutSecondCharge()
    {
        var env = await Env.CreateAsync(wallet: 500);
        var first = await env.Repair("tx-repair-1", Fx.RepairMinor);

        var second = await env.Repair("tx-repair-1", Fx.RepairMinor);

        Assert.True(second.IsReplay);
        Assert.Equal(first.RepairId, second.RepairId);
        Assert.Equal(first.WalletAfter, second.WalletAfter);
        Assert.Equal(first.Revision, second.Revision);
        Assert.Equal(460, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Repair_TransactionIdReusedForDifferentRepair_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 500);
        await env.Repair("tx-repair-1", Fx.RepairMinor);

        var result = await env.Repair("tx-repair-1", Fx.RepairFull);

        Assert.Equal(RejectionReason.TransactionIdReused, result.Reason);
        Assert.Equal(460, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Repair_IdOfAnotherKind_DoesNotCollideInFingerprint()
    {
        // A repair and an item purchase with the same id string and transaction id must be told apart by kind.
        var env = await Env.CreateAsync(wallet: 500);
        env.Prices.Buy[(Fx.Station, Fx.RepairMinor)] = 1;
        await env.Repair("tx-1", Fx.RepairMinor);

        Assert.Equal(RejectionReason.TransactionIdReused, (await env.Buy("tx-1", Fx.RepairMinor, 1)).Reason);
    }

    [Fact]
    public async Task Repair_InvalidShape_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 500);

        Assert.Equal(RejectionReason.InvalidRequest, (await env.Repair(null, "")).Reason);
        Assert.Equal(RejectionReason.IdentityMismatch,
            (await env.Proc.RepairAsync(new TransactionContext("someone-else", true),
                new RepairRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, Fx.RepairMinor))).Reason);
    }

    [Fact]
    public async Task Repair_SurvivesExitAndReturn_IncludingReplayAfterRestart()
    {
        using var dir = new TempDirectory();
        var options = new PersistenceOptions();
        PlayerProfile beforeExit;
        using (var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>()))
        {
            var env = await Env.CreateAsync(wallet: 500, repo: repo, options: options);
            await env.Repair("tx-repair-1", Fx.RepairMinor);
            beforeExit = env.Snapshot;
        }
        using (var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>()))
        {
            var env = await Env.CreateAsync(createProfile: false, repo: repo, options: options);

            ProfileAssert.Equivalent(beforeExit, env.Snapshot);
            Assert.Equal(Fx.RepairMinor, env.Snapshot.AppliedTransactions[^1].RepairId);
            var replay = await env.Repair("tx-repair-1", Fx.RepairMinor);
            Assert.True(replay.IsReplay);
            Assert.Equal(Fx.RepairMinor, replay.RepairId);
            Assert.Equal(460, env.Snapshot.Wallet);
        }
    }

    [Fact]
    public async Task Repair_SaveWrittenBeforeRepairsExisted_StillLoads()
    {
        // Older files carry no RepairId on applied transactions; the field is optional in the payload.
        var env = await Env.CreateAsync(wallet: 500);
        await env.Buy(null, Fx.OreA, 1);
        var bytes = ProfileSerializer.Serialize(env.Snapshot);
        var envelope = System.Text.Json.Nodes.JsonNode.Parse(bytes)!.AsObject();
        var payload = System.Text.Json.Nodes.JsonNode.Parse(envelope["payload"]!.GetValue<string>())!.AsObject();
        foreach (var t in payload["appliedTransactions"]!.AsArray()) t!.AsObject().Remove("repairId");
        var text = payload.ToJsonString();
        envelope["payload"] = text;
        envelope["sha256"] = ProfileSerializerHash(text);

        var decoded = ProfileSerializer.Decode(System.Text.Encoding.UTF8.GetBytes(envelope.ToJsonString()), Array.Empty<IProfileMigration>());

        Assert.True(decoded.Succeeded);
        Assert.Null(decoded.Profile!.AppliedTransactions[^1].RepairId);
    }

    private static string ProfileSerializerHash(string text) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    // ---- Upgrades: one purchase path per family -------------------------------------------------------------

    // NEW fixture: six slots to match the master M2 count. The original data names five upgrade groups (see the oracle tests below).
    public static readonly TheoryData<string, long> SixFamilies = new()
    {
        { "fixture-upgrade-engine", 100 },
        { "fixture-upgrade-thruster", 110 },
        { "fixture-upgrade-power", 120 },
        { "fixture-upgrade-armor", 130 },
        { "fixture-upgrade-hyperdrive", 140 },
        { "fixture-upgrade-sixth", 150 },
    };

    [Theory]
    [MemberData(nameof(SixFamilies))]
    public async Task Upgrade_EachFamily_BuyRejectRepeatAndReplay(string upgrade, long price)
    {
        var env = await Env.CreateAsync(wallet: price + 5);
        env.Prices.Upgrade[(Fx.Station, upgrade)] = price;

        var broke = await Env.CreateAsync(wallet: price - 1, repo: new InMemoryProfileRepository(new PersistenceOptions()));
        broke.Prices.Upgrade[(Fx.Station, upgrade)] = price;
        Assert.Equal(RejectionReason.InsufficientFunds, (await broke.BuyUpgrade(null, upgrade)).Reason);
        Assert.Empty(broke.Snapshot.Upgrades);

        var bought = await env.BuyUpgrade("tx-up-1", upgrade);
        Assert.Equal(TransactionStatus.Applied, bought.Status);
        Assert.Equal(upgrade, bought.UpgradeId);
        Assert.Equal(5, env.Snapshot.Wallet);
        Assert.Contains(upgrade, env.Snapshot.Upgrades);

        Assert.Equal(RejectionReason.UpgradeAlreadyOwned, (await env.BuyUpgrade(null, upgrade)).Reason);
        var replay = await env.BuyUpgrade("tx-up-1", upgrade);
        Assert.True(replay.IsReplay);
        Assert.Equal(5, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task Upgrade_AllSixOwnedTogether_ChargeSumOnce()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        long total = 0;
        foreach (var args in SixFamilies)
        {
            var (upgrade, price) = ((string)args[0], (long)args[1]);
            env.Prices.Upgrade[(Fx.Station, upgrade)] = price;
            Assert.Equal(TransactionStatus.Applied, (await env.BuyUpgrade(null, upgrade)).Status);
            total += price;
        }

        Assert.Equal(6, env.Snapshot.Upgrades.Count);
        Assert.Equal(1000 - total, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task RepairAndUpgrade_Interleaved_KeepLedgerConsistent()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        await env.BuyUpgrade(null, Fx.UpgradeX);   // -500
        await env.Repair(null, Fx.RepairFull);     // -200
        await env.Repair(null, Fx.RepairMinor);    // -40

        Assert.Equal(260, env.Snapshot.Wallet);
        Assert.Equal(new long[] { 500, 700, 740 }.Select(s => 1000 - s), env.Snapshot.Ledger.Select(e => e.BalanceAfter));
        Assert.Equal(new[] { TransactionKind.BuyUpgrade, TransactionKind.Repair, TransactionKind.Repair }, env.Snapshot.Ledger.Select(e => e.Kind));
    }

    // ---- Original-data binding (price source only) ---------------------------------------------------------

    private static string ItmRow(int id, string name, string category, int buy)
    {
        var f = Enumerable.Repeat("0", 31).ToArray();
        f[0] = "4"; f[1] = "v57"; f[2] = id.ToString(); f[3] = name; f[4] = category; f[8] = buy.ToString();
        return string.Join(",", f);
    }

    [Fact]
    public void OriginalBinding_PricesOnlyTheFiveUpgradeCategories_AtRawBuy()
    {
        var text = string.Join("\n",
            ItmRow(1, "E", "Engine Mods", 10), ItmRow(7, "T", "Thruster Mods", 20), ItmRow(9, "P", "Power Mods", 30),
            ItmRow(4, "A", "Hull Armor", 40), ItmRow(25, "H", "HyperDrive Systems", 50),
            ItmRow(1004, "Cannon", "Cannon", 60), ItmRow(2003, "Ore", "Resources", 0), ItmRow(99, "Free", "Engine Mods", 0));
        var catalog = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(text, "ITM/synthetic.itm"));

        var table = StationPriceTable.FromOriginalItems(["st-1", "st-2"], catalog);

        foreach (var (id, price) in new[] { ("1", 10L), ("7", 20L), ("9", 30L), ("4", 40L), ("25", 50L) })
        {
            Assert.True(table.TryGetUpgradePrice("st-2", id, out var p), id);
            Assert.Equal(price, p);
        }
        Assert.False(table.TryGetUpgradePrice("st-1", "1004", out _));   // weapon is an item, not an upgrade
        Assert.False(table.TryGetUpgradePrice("st-1", "2003", out _));
        Assert.False(table.TryGetUpgradePrice("st-1", "99", out _));     // raw buy 0 is not offered
        Assert.False(table.TryGetUpgradePrice("elsewhere", "1", out _));
        Assert.Equal(5, StationPriceTable.OriginalUpgradeCategories.Count - 0 - 0);
    }

    [Fact]
    public void OriginalBinding_NeverDerivesRepairPrices_AndExplicitOnesAreChecked()
    {
        var catalog = OriginalItemCatalog.FromRecords(RawRecordReader.Parse(
            string.Join("\n", ItmRow(39, "Minor Repair", "Resources", 0)), "ITM/synthetic.itm"));
        var table = StationPriceTable.FromOriginalItems(["st-1"], catalog);
        Assert.False(table.TryGetRepairPrice("st-1", "39", out _));

        table.WithRepairPrices([("39", 15L)]);
        Assert.True(table.TryGetRepairPrice("st-1", "39", out var price));
        Assert.Equal(15, price);
        Assert.False(table.TryGetRepairPrice("st-2", "39", out _));
        Assert.Throws<ArgumentException>(() => table.WithRepairPrices([("39", 16L)]));
        Assert.Throws<ArgumentException>(() => table.WithRepairPrices([("40", -1L)]));
        Assert.Throws<ArgumentException>(() => table.WithUpgradePrices([("u", 1L), ("u", 2L)]));
    }

    [RequiresOriginalAssetsFact]
    public async Task OriginalBinding_PinnedCatalog_PricesEveryUpgradeRowAndBuysOne()
    {
        var catalog = OriginalItemCatalog.Load(OriginalAssets.Root!);
        var table = StationPriceTable.FromOriginalItems([Fx.Station], catalog);

        var expected = catalog.Items.Where(i => StationPriceTable.OriginalUpgradeCategories.Contains(i.Category)).ToList();
        // Oracle (Python audit of Rogue_mp.itm @ eb1514ea): 8+6+6+6 module rows plus 9 Hull Armor rows.
        Assert.Equal(35, expected.Count);
        foreach (var item in expected)
        {
            var id = item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(item.Buy.Value > 0, table.TryGetUpgradePrice(Fx.Station, id, out var p));
            if (item.Buy.Value > 0) Assert.Equal(item.Buy.Value, p);
        }
        // Raw repair rows 39/40/43 have buy 0 and no decoded cost (RT-F047): nothing is derived.
        foreach (var id in OriginalRepairItems.RepairItemIds)
            Assert.False(table.TryGetRepairPrice(Fx.Station, id.ToString(), out _));

        var options = new PersistenceOptions();
        var repo = new InMemoryProfileRepository(options);
        await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, 6000, Fx.Hull));
        var proc = new TransactionProcessor(repo, table, new FixtureCargoPolicy(), options);
        await proc.OpenProfileAsync(Fx.ProfileId);
        var result = await proc.BuyUpgradeAsync(new TransactionContext(Fx.ProfileId, true),
            new BuyUpgradeRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, "2")); // TurboBoosters, raw buy 5000
        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(1000, result.WalletAfter);
    }
}
