using System.Runtime.CompilerServices;
using Assets;
using InfServer;
using InfServer.Game;
using InfServer.Network;
using InfServer.Protocol;

public static class ShopChecks
{
    public static IEnumerable<CheckResult> Run(string itemPath)
    {
        // Use an unchanged canonical sellable record. Boundary-only prices/counts below are NEW fixtures.
        ItemInfo original = ItemInfo.Load(itemPath).First(item => item.sellPrice > 0);
        yield return Check("Shop.Partial", () => Sale(original, 5, -2, 3, 100 + 2 * original.sellPrice, 1));
        yield return Check("Shop.FullAndReplay", () =>
        {
            var f = new Fixture(original, 5);
            f.Arena.handlePlayerShop(f.Player, original, -5);
            AssertSale(f, 0, 100 + 5 * original.sellPrice, 1);
            f.Arena.handlePlayerShop(f.Player, original, -5);
            AssertSale(f, 0, 100 + 5 * original.sellPrice, 1);
        });
        yield return Check("Shop.Oversell", () => Sale(original, 5, -6, 5, 100, 0));
        yield return Check("Shop.Zero", () => Sale(original, 5, 0, 5, 100, 0));
        yield return Check("Shop.MinValue", () => Sale(NewItem(1), 5, int.MinValue, 5, 100, 0));
        yield return Check("Shop.Absent", () => Sale(original, 0, -1, 0, 100, 0));
        yield return Check("Shop.Unsellable", () => Sale(NewItem(-1), 5, -1, 5, 100, 0));
        yield return Check("Shop.ZeroPrice", () => Sale(NewItem(0), 5, -2, 3, 100, 1));
        yield return Check("Shop.BalanceExactMax", () =>
        {
            var f = new Fixture(NewItem(7), 5, int.MaxValue - 7);
            f.Arena.handlePlayerShop(f.Player, f.Item, -1);
            AssertSale(f, 4, int.MaxValue, 1);
        });
        yield return Check("Shop.BalanceOverflow", () =>
        {
            var f = new Fixture(NewItem(7), 5, int.MaxValue - 6);
            f.Arena.handlePlayerShop(f.Player, f.Item, -1);
            AssertSale(f, 5, int.MaxValue - 6, 0);
        });
        yield return Check("Shop.PriceOverflow", () => Sale(NewItem(50000), 50000, -50000, 50000, 100, 0));
        yield return Check("Shop.HookVeto", () =>
        {
            var f = new Fixture(original, 5);
            var hook = new SaleHook((_, _, _) => false);
            f.AddHook(hook);
            f.Arena.handlePlayerShop(f.Player, f.Item, -2);
            AssertSale(f, 5, 100, 0);
            Require(hook.Calls == 1 && hook.Quantity == 2, "Hook must receive a positive sale count once.");
        });
        yield return Check("Shop.HookAllows", () =>
        {
            var f = new Fixture(original, 5);
            var hook = new SaleHook((_, _, _) => true);
            f.AddHook(hook);
            f.Arena.handlePlayerShop(f.Player, f.Item, -2);
            AssertSale(f, 3, 100 + 2 * original.sellPrice, 1);
            Require(hook.Calls == 1 && hook.Quantity == 2, "Hook must receive the requested positive sale count.");
        });
        yield return Check("Shop.HookRemovesInventory", () =>
        {
            var f = new Fixture(original, 5);
            f.AddHook(new SaleHook((player, item, _) => { player._inventory.Remove(item.id); return true; }));
            f.Arena.handlePlayerShop(f.Player, f.Item, -2);
            AssertSale(f, 0, 100, 0);
        });
        yield return Check("Shop.HookFillsBalance", () =>
        {
            var f = new Fixture(NewItem(7), 5);
            f.AddHook(new SaleHook((player, _, _) => { player.Cash = int.MaxValue; return true; }));
            f.Arena.handlePlayerShop(f.Player, f.Item, -1);
            AssertSale(f, 5, int.MaxValue, 0);
        });
        yield return Check("Shop.InvalidSkipsHook", () =>
        {
            var f = new Fixture(original, 5);
            var hook = new SaleHook((_, _, _) => true);
            f.AddHook(hook);
            f.Arena.handlePlayerShop(f.Player, f.Item, -6);
            Require(hook.Calls == 0, "Oversell must be rejected before invoking scripts.");
        });
        yield return Check("Shop.ZeroSkipsHook", () =>
        {
            var f = new Fixture(original, 5);
            var hook = new SaleHook((_, _, _) => true);
            f.AddHook(hook);
            f.Arena.handlePlayerShop(f.Player, f.Item, 0);
            Require(hook.Calls == 0, "Empty sale must not invoke script side effects.");
            AssertSale(f, 5, 100, 0);
        });

        // RT-F396 buy-path checks: NEW fixtures; stack, price and category values are boundary-only.
        yield return Check("Buy.Valid", () => Buy(BuyItem(10), 1, 3, 100, 4, 70, 1));
        yield return Check("Buy.ExactCash", () => Buy(BuyItem(10), 1, 10, 100, 11, 0, 1));
        yield return Check("Buy.InsufficientCash", () => Buy(BuyItem(10), 0, 11, 100, 0, 100, 0));
        yield return Check("Buy.PriceOverflow", () => Buy(BuyItem(50000), 1, 50000, 100, 1, 100, 0));
        yield return Check("Buy.NegativePrice", () => Buy(BuyItem(-5), 1, 2, 100, 1, 100, 0));
        yield return Check("Buy.Unbuyable", () => Buy(BuyItem(0), 0, 1, 100, 0, 100, 0));
        yield return Check("Buy.ZeroAndNegativeRequest", () =>
        {
            Buy(BuyItem(10), 0, 0, 100, 0, 100, 0);
            Buy(BuyItem(10), 5, -6, 100, 5, 100, 0);
        });
        yield return Check("Buy.StackOverflow", () => Buy(BuyItem(1), 65535, 1, 100, 65535, 100, 0));
        yield return Check("Buy.StackReachesMax", () => Buy(BuyItem(1), 65534, 1, 100, 65535, 99, 1));
        yield return Check("Buy.MaxAllowedExceeded", () => Buy(BuyItem(1, maxAllowed: 3), 2, 2, 100, 2, 100, 0));
        yield return Check("Buy.RejectedDeliveryNotCharged", () => Buy(BuyItem(10, heldCategory: 1), 1, 2, 100, 1, 100, 0));
        yield return Check("Buy.HookVeto", () =>
        {
            var f = new Fixture(BuyItem(10), 0);
            var hook = new SaleHook((_, _, _) => false);
            f.AddHook(hook, "Shop.Buy");
            f.Arena.handlePlayerShop(f.Player, f.Item, 2);
            AssertSale(f, 0, 100, 0);
            Require(hook.Calls == 1 && hook.Quantity == 2, "Buy hook must receive the requested count once.");
        });
        yield return Check("Buy.HookAllows", () =>
        {
            var f = new Fixture(BuyItem(10), 1);
            f.AddHook(new SaleHook((_, _, _) => true), "Shop.Buy");
            f.Arena.handlePlayerShop(f.Player, f.Item, 2);
            AssertSale(f, 3, 80, 1);
        });
        yield return Check("Buy.HookDrainsCash", () =>
        {
            var f = new Fixture(BuyItem(10), 0);
            f.AddHook(new SaleHook((player, _, _) => { player.Cash = 5; return true; }), "Shop.Buy");
            f.Arena.handlePlayerShop(f.Player, f.Item, 2);
            AssertSale(f, 0, 5, 0);
        });
        yield return Check("Buy.HookFillsLimit", () =>
        {
            var f = new Fixture(BuyItem(1, maxAllowed: 3), 0);
            f.AddHook(new SaleHook((player, item, _) => { player._inventory[item.id] = new Player.InventoryItem { item = item, quantity = 3 }; return true; }), "Shop.Buy");
            f.Arena.handlePlayerShop(f.Player, f.Item, 2);
            AssertSale(f, 3, 100, 0);
        });
    }

    static ItemInfo BuyItem(int buyPrice, int maxAllowed = 0, int heldCategory = 0) => new() { id = 60001, name = "NEW buy boundary fixture", buyPrice = buyPrice, sellPrice = -1, maxAllowed = maxAllowed, heldCategoryType = heldCategory };
    static void Buy(ItemInfo item, ushort owned, int request, int cash, int remaining, int expectedCash, int packets)
    {
        var f = new Fixture(item, owned, cash);
        f.Arena.handlePlayerShop(f.Player, item, request);
        AssertSale(f, remaining, expectedCash, packets);
    }
    static ItemInfo NewItem(int sellPrice) => new() { id = 60000, name = "NEW sale boundary fixture", sellPrice = sellPrice };
    static CheckResult Check(string name, Action action)
    {
        try { action(); return new(name, true, "Actual handler/state and serialized inventory packet matched."); }
        catch (Exception exception) { return new(name, false, $"{exception.GetType().Name}: {exception.Message}"); }
    }
    static void Sale(ItemInfo item, ushort owned, int request, int remaining, int cash, int packets)
    {
        var f = new Fixture(item, owned);
        f.Arena.handlePlayerShop(f.Player, item, request);
        AssertSale(f, remaining, cash, packets);
    }
    static void AssertSale(Fixture f, int remaining, int cash, int packets)
    {
        Require(f.Player.getInventoryAmount(f.Item.id) == remaining, $"Inventory expected {remaining}, actual {f.Player.getInventoryAmount(f.Item.id)}.");
        Require(f.Player.Cash == cash, $"Cash expected {cash}, actual {f.Player.Cash}.");
        var queue = f.Player._client._streams[0].reliableQueue;
        Require(queue.Count == packets, $"Expected {packets} packets, actual {queue.Count}.");
        if (packets != 0)
        {
            var packet = queue.Last().packet as SC_Inventory ?? throw new InvalidDataException("Expected actual SC_Inventory.");
            Require(packet.cash == cash && BitConverter.ToInt32(packet.Data, 1) == cash, "Outgoing serialized cash must reflect committed balance.");
            Require(BitConverter.ToInt16(packet.Data, 7) == (remaining == 0 ? 0 : 1), "Serialized item count mismatch.");
            if (remaining != 0) Require(BitConverter.ToUInt16(packet.Data, 11) == remaining, "Serialized remaining quantity mismatch.");
        }
    }
    static void Require(bool condition, string detail) { if (!condition) throw new InvalidDataException(detail); }
    sealed class Fixture
    {
        public ScriptArena Arena { get; } = (ScriptArena)RuntimeHelpers.GetUninitializedObject(typeof(ScriptArena));
        public Player Player { get; } = new();
        public ItemInfo Item { get; }
        public Fixture(ItemInfo item, ushort owned, int cash = 100)
        {
            Item = item;
            Arena.eventInit(false);
            Player.WipeStats();
            Player._inventory = new();
            Player._skills = new();
            Player._client = new Client(false) { _C2S_UDPSize = 4096 };
            Player.Cash = cash;
            if (owned != 0) Player._inventory.Add(item.id, new Player.InventoryItem { item = item, quantity = owned });
        }
        public void AddHook(SaleHook hook, string name = "Shop.Sell") => Arena.events[name] += new CEventHandler(hook, nameof(SaleHook.OnSell));
    }
}

public sealed class SaleHook(Func<Player, ItemInfo, int, bool> handler)
{
    public int Calls { get; private set; }
    public int Quantity { get; private set; }
    public bool OnSell(Player player, ItemInfo item, int count) { Calls++; Quantity = count; return handler(player, item, count); }
}
