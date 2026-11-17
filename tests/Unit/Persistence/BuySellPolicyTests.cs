using RogueTrader.OriginalData;
using RogueTrader.Persistence;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>
/// RT-F049 slice: original buy / resale / holding-cap rules (emulator ScriptArena.handlePlayerShop, ITM f[8], f[13], f[15]) through the
/// transaction core. Fixture ids and prices are NEW test data; tests that read the pinned ITM say so.
/// </summary>
public sealed class BuySellPolicyTests
{
    // ---- Buying -----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Buy_ItemWithoutBuyPrice_IsNotForSale()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        var before = env.Snapshot;

        var result = await env.Buy(null, "fixture-not-buyable", 1);

        Assert.Equal(RejectionReason.NotForSale, result.Reason);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task Buy_PastMaxAllowed_IsRefusedWithoutPartialFill()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        env.Prices.MaxQuantity[Fx.OreA] = 5; // NEW fixture cap: at most 5 held
        Assert.Equal(TransactionStatus.Applied, (await env.Buy(null, Fx.OreA, 3)).Status);
        var before = env.Snapshot;

        var over = await env.Buy(null, Fx.OreA, 3); // 3 + 3 > 5: the emulator refuses the whole purchase, it does not clamp

        Assert.Equal(RejectionReason.MaxAllowedExceeded, over.Reason);
        Assert.Same(before, env.Snapshot);
        Assert.Equal(TransactionStatus.Applied, (await env.Buy(null, Fx.OreA, 2)).Status); // exactly the cap is fine
        Assert.Equal(5, env.Snapshot.Inventory[Fx.OreA]);
        Assert.Equal(RejectionReason.MaxAllowedExceeded, (await env.Buy(null, Fx.OreA, 1)).Reason);
    }

    [Fact]
    public async Task Buy_UncappedItem_IsNotLimited()
    {
        var env = await Env.CreateAsync(wallet: 100_000);
        Assert.Equal(TransactionStatus.Applied, (await env.Buy(null, Fx.OddItem, 50)).Status);
    }

    [Fact]
    public async Task Buy_CapIsCheckedBeforeFunds()
    {
        var env = await Env.CreateAsync(wallet: 1);
        env.Prices.MaxQuantity[Fx.OreA] = 5;
        Assert.Equal(RejectionReason.MaxAllowedExceeded, (await env.Buy(null, Fx.OreA, 6)).Reason);
    }

    // ---- Selling an installed upgrade ---------------------------------------------------------------------------

    [Fact]
    public async Task SellUpgrade_PaysResalePriceAndRemovesTheUpgrade()
    {
        var env = await Env.CreateAsync(wallet: 600);
        await env.BuyUpgrade(null, Fx.UpgradeX); // 500
        var bought = env.Snapshot;
        Assert.Equal(100, bought.Wallet);

        var sold = await env.SellUpgrade(null, Fx.UpgradeX);

        Assert.Equal(TransactionStatus.Applied, sold.Status);
        Assert.Equal(350, sold.CurrencyDelta);
        Assert.Equal(Fx.UpgradeX, sold.UpgradeId);
        Assert.Equal(450, sold.WalletAfter);
        Assert.Equal(450, env.Snapshot.Wallet);
        Assert.DoesNotContain(Fx.UpgradeX, env.Snapshot.Upgrades);
        Assert.Equal(bought.Revision + 1, env.Snapshot.Revision);
        Assert.Equal(TransactionKind.SellUpgrade, env.Snapshot.Ledger[^1].Kind);
        Assert.Equal(350, env.Snapshot.Ledger[^1].Delta);
    }

    [Fact]
    public async Task SellUpgrade_OnlyRemovesTheSoldOne()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        await env.BuyUpgrade(null, Fx.UpgradeY);

        await env.SellUpgrade(null, Fx.UpgradeX);

        Assert.Equal([Fx.UpgradeY], env.Snapshot.Upgrades.ToArray());
    }

    [Fact]
    public async Task SellUpgrade_ZeroResale_IsALegal0CreditSale()
    {
        var env = await Env.CreateAsync(wallet: 300);
        await env.BuyUpgrade(null, Fx.UpgradeY);

        var sold = await env.SellUpgrade(null, Fx.UpgradeY);

        Assert.Equal(TransactionStatus.Applied, sold.Status);
        Assert.Equal(0, sold.CurrencyDelta);
        Assert.Equal(0, env.Snapshot.Wallet);
        Assert.Empty(env.Snapshot.Upgrades);
    }

    [Fact]
    public async Task SellUpgrade_NegativeSentinel_CannotBeSold()
    {
        var env = await Env.CreateAsync(wallet: 600);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        env.Prices.UpgradeSell.Remove((Fx.Station, Fx.UpgradeX)); // raw f[15] -1 is simply absent from the table
        var before = env.Snapshot;

        var result = await env.SellUpgrade(null, Fx.UpgradeX);

        Assert.Equal(RejectionReason.UpgradeNotSellable, result.Reason);
        Assert.Same(before, env.Snapshot);
        Assert.Contains(Fx.UpgradeX, env.Snapshot.Upgrades);
    }

    [Fact]
    public async Task SellUpgrade_NotOwned_IsRefused()
    {
        var env = await Env.CreateAsync(wallet: 600);
        var before = env.Snapshot;

        Assert.Equal(RejectionReason.UpgradeNotOwned, (await env.SellUpgrade(null, Fx.UpgradeX)).Reason);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task SellUpgrade_NeedsADockedShip()
    {
        var env = await Env.CreateAsync(wallet: 600);
        await env.BuyUpgrade(null, Fx.UpgradeX);

        var result = await env.SellUpgrade(null, Fx.UpgradeX, stationAccess: false);

        Assert.Equal(RejectionReason.StationAccessDenied, result.Reason);
        Assert.Contains(Fx.UpgradeX, env.Snapshot.Upgrades);
    }

    [Fact]
    public async Task SellUpgrade_ReplayWithSameId_ReturnsOriginalResultAndPaysOnce()
    {
        var env = await Env.CreateAsync(wallet: 600);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        var id = Fx.NewId();

        var first = await env.SellUpgrade(id, Fx.UpgradeX);
        var afterFirst = env.Snapshot;
        var replay = await env.SellUpgrade(id, Fx.UpgradeX);

        Assert.True(replay.IsReplay);
        Assert.Equal(first.CurrencyDelta, replay.CurrencyDelta);
        Assert.Equal(first.WalletAfter, replay.WalletAfter);
        Assert.Equal(first.Revision, replay.Revision);
        Assert.Same(afterFirst, env.Snapshot);
        Assert.Equal(450, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task SellUpgrade_ReplayAfterRebuy_DoesNotSellTheNewCopy()
    {
        var env = await Env.CreateAsync(wallet: 2000);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        var id = Fx.NewId();
        await env.SellUpgrade(id, Fx.UpgradeX);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        var wallet = env.Snapshot.Wallet;

        var replay = await env.SellUpgrade(id, Fx.UpgradeX);

        Assert.True(replay.IsReplay);
        Assert.Contains(Fx.UpgradeX, env.Snapshot.Upgrades);
        Assert.Equal(wallet, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task SellUpgrade_SecondSaleWithNewId_IsRefused_NoDoublePayment()
    {
        var env = await Env.CreateAsync(wallet: 600);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        await env.SellUpgrade(null, Fx.UpgradeX);
        var after = env.Snapshot;

        var again = await env.SellUpgrade(null, Fx.UpgradeX);

        Assert.Equal(RejectionReason.UpgradeNotOwned, again.Reason);
        Assert.Same(after, env.Snapshot);
        Assert.Equal(450, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task SellUpgrade_ConcurrentSales_PayExactlyOnce()
    {
        var env = await Env.CreateAsync(wallet: 600);
        await env.BuyUpgrade(null, Fx.UpgradeX);

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => env.SellUpgrade(null, Fx.UpgradeX)));

        Assert.Equal(1, results.Count(r => r.Status == TransactionStatus.Applied));
        Assert.Equal(450, env.Snapshot.Wallet);
    }

    [Fact]
    public async Task SellUpgrade_ReusedIdForADifferentUpgrade_IsRejected()
    {
        var env = await Env.CreateAsync(wallet: 1000);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        await env.BuyUpgrade(null, Fx.UpgradeY);
        var id = Fx.NewId();
        await env.SellUpgrade(id, Fx.UpgradeX);

        Assert.Equal(RejectionReason.TransactionIdReused, (await env.SellUpgrade(id, Fx.UpgradeY)).Reason);
        Assert.Contains(Fx.UpgradeY, env.Snapshot.Upgrades);
    }

    [Fact]
    public async Task BuyThenSell_NeverGainsCredits_AndSurvivesSaveReload()
    {
        var env = await Env.CreateAsync(wallet: 600);
        await env.BuyUpgrade(null, Fx.UpgradeX);
        await env.SellUpgrade(null, Fx.UpgradeX);
        Assert.True(env.Snapshot.Wallet < 600);

        var reopened = await env.Proc.OpenProfileAsync(Fx.ProfileId);
        Assert.True(reopened.Succeeded);
        Assert.Equal(450, reopened.Profile!.Wallet);
        Assert.Empty(reopened.Profile.Upgrades);
        Assert.Equal(TransactionKind.SellUpgrade, reopened.Profile.AppliedTransactions[^1].Kind);
    }

    // ---- Original data -----------------------------------------------------------------------------------------

    private static string Row(int id, string name, string category, int buy, int sell, int max = 0, int drop = 0)
    {
        var f = Enumerable.Repeat("0", 31).ToArray();
        f[0] = "13"; f[1] = "v57"; f[2] = id.ToString(); f[3] = name; f[4] = category;
        f[8] = buy.ToString(); f[10] = drop.ToString(); f[13] = max.ToString(); f[15] = sell.ToString();
        return string.Join(",", f);
    }

    [Fact]
    public void OriginalBinding_UpgradeResaleFollowsRawF15_AndMinusOneIsNotSellable()
    {
        var text = string.Join("\n",
            Row(1, "SuperBoosters", "Engine Mods", 10000, 7500, -1), Row(4, "Armor", "Hull Armor", 5000, 0, -1),
            Row(2, "Locked", "Engine Mods", 5000, -1, -1), Row(1004, "Cannon", "Cannon", 2000, 1500, -1));
        var table = StationPriceTable.FromOriginalItems(["st-1", "st-2"], OriginalItemCatalog.FromRecords(RawRecordReader.Parse(text, "ITM/synthetic.itm")));

        Assert.True(table.TryGetUpgradeSellPrice("st-2", "1", out var sell));
        Assert.Equal(7500, sell);
        Assert.True(table.TryGetUpgradeSellPrice("st-1", "4", out var zero));
        Assert.Equal(0, zero);
        Assert.False(table.TryGetUpgradeSellPrice("st-1", "2", out _));
        Assert.False(table.TryGetUpgradeSellPrice("st-1", "1004", out _)); // a weapon is cargo, not an installed upgrade
        Assert.False(table.TryGetUpgradeSellPrice("elsewhere", "1", out _));
        Assert.True(table.TryGetMaxQuantity("1", out var max));
        Assert.Equal(1, max);
    }

    [Fact]
    public async Task OriginalBinding_SuperBoostersBuyAt10000SellBackAt7500()
    {
        var text = Row(1, "SuperBoosters", "Engine Mods", 10000, 7500, -1);
        var table = StationPriceTable.FromOriginalItems([Fx.Station], OriginalItemCatalog.FromRecords(RawRecordReader.Parse(text, "ITM/synthetic.itm")));
        var options = new PersistenceOptions();
        var repo = new InMemoryProfileRepository(options);
        await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, 10000, Fx.Hull));
        var proc = new TransactionProcessor(repo, table, new FixtureCargoPolicy(), options);
        await proc.OpenProfileAsync(Fx.ProfileId);
        var ctx = new TransactionContext(Fx.ProfileId, true);

        await proc.BuyUpgradeAsync(ctx, new BuyUpgradeRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, "1"));
        var sold = await proc.SellUpgradeAsync(ctx, new SellUpgradeRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, "1"));

        Assert.Equal(7500, sold.CurrencyDelta);
        Assert.Equal(7500, proc.GetSnapshot(Fx.ProfileId)!.Wallet);
    }

    [Fact]
    public void OriginalBinding_AmmoHoldingCapIsTheAbsoluteValue()
    {
        var text = string.Join("\n", Row(2014, "Pellets", "Ammo-Projectile", 2, 2, -250), Row(2003, "Ore", "Resources", 0, 0));
        var table = StationPriceTable.FromOriginalItems([Fx.Station], OriginalItemCatalog.FromRecords(RawRecordReader.Parse(text, "ITM/synthetic.itm")));

        Assert.True(table.TryGetMaxQuantity("2014", out var max));
        Assert.Equal(250, max);
        Assert.False(table.TryGetMaxQuantity("2003", out _)); // raw 0 is unlimited
    }

    [RequiresOriginalAssetsFact]
    public void PinnedItm_EveryNonNegativeUpgradeResaleIsRawF15_AndMinusOneIsAbsent()
    {
        var catalog = OriginalItemCatalog.Load(OriginalAssets.Root!);
        var table = StationPriceTable.FromOriginalItems([Fx.Station], catalog);
        foreach (var item in catalog.Items.Where(i => StationPriceTable.OriginalUpgradeCategories.Contains(i.Category)))
        {
            var sellable = table.TryGetUpgradeSellPrice(Fx.Station, item.Id.ToString(), out var price);
            Assert.Equal(item.Sell.Value >= 0, sellable);
            if (sellable) Assert.Equal(item.Sell.Value, price);
        }
        // SuperBoosters (id 1): buy 10000, resale 7500 (RT-F049 example); every armour row resells for 0.
        Assert.True(table.TryGetUpgradeSellPrice(Fx.Station, "1", out var boosters));
        Assert.Equal(7500, boosters);
        Assert.True(table.TryGetUpgradeSellPrice(Fx.Station, "4", out var armour));
        Assert.Equal(0, armour);
    }
}
