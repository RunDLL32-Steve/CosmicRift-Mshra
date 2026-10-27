using System.Globalization;
using RogueTrader.OriginalData;
using RogueTrader.Persistence;

namespace RogueTrader.Trading;

/// <summary>The unscaled ITM buy (f[8]) and sell (f[15]) fields of one original item; sentinels stay raw.</summary>
public sealed record RawItemPrice(string ItemId, int RawBuy, int RawSell);

/// <summary>
/// Price source for stations. Unit prices are whole currency units; the unit and scaling of the original credit fields are
/// unvalidated (RT-F045/RT-F049), so original values are used raw.
/// </summary>
public sealed class StationPriceTable : IPriceSource
{
    private readonly string[] _stationIds;
    private readonly Dictionary<(string Station, string Item), long> _buy = new();
    private readonly Dictionary<(string Station, string Item), long> _sell = new();
    private readonly Dictionary<(string Station, string Upgrade), long> _upgrade = new();
    private readonly Dictionary<(string Station, string Repair), long> _repair = new();

    /// <summary>
    /// The original ITM categories that hold installable upgrade modules (RT-F048). The catalog names five groups here, not six:
    /// the "six equipment upgrades" of master M2 is a NEW count that the original data does not confirm.
    /// </summary>
    public static readonly IReadOnlyList<string> OriginalUpgradeCategories =
        [.. OriginalUpgrade.Families, "Hull Armor"];

    /// <summary>Explicit prices (NEW fixtures or hand-authored data). Absent entries are not traded.</summary>
    public StationPriceTable(string stationId, IEnumerable<(string ItemId, long? Buy, long? Sell)> prices)
        : this(new[] { stationId }, prices) { }

    public StationPriceTable(IEnumerable<string> stationIds, IEnumerable<(string ItemId, long? Buy, long? Sell)> prices)
    {
        ArgumentNullException.ThrowIfNull(stationIds);
        ArgumentNullException.ThrowIfNull(prices);
        var stations = stationIds.ToArray();
        _stationIds = stations;
        if (stations.Any(string.IsNullOrEmpty))
            throw new ArgumentException("A station needs an id.", nameof(stationIds));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (item, buy, sell) in prices.ToArray())
        {
            if (string.IsNullOrEmpty(item))
                throw new ArgumentException("A price needs an item id.", nameof(prices));
            // A repeated item would silently overwrite (or half-merge) an earlier price: refuse it instead.
            if (!seen.Add(item))
                throw new ArgumentException($"Duplicate price entry for '{item}'.", nameof(prices));
            if (buy < 0 || sell < 0)
                throw new ArgumentException($"Negative price for '{item}'.", nameof(prices));
            foreach (var station in stations)
            {
                if (buy is { } b) _buy[(station, item)] = b;
                if (sell is { } s) _sell[(station, item)] = s;
            }
        }
    }

    /// <summary>
    /// Provisional binding of the original ITM catalog (Rogue_mp.itm): item id = decimal ITM id. An item is buyable when the raw
    /// buy field is greater than zero and sellable when the raw sell field is greater than zero. The raw sell value -1 (34 rows,
    /// all with buy 0; RT-F049) and a raw 0 are treated as "not traded" in this provisional rule only; the native meaning of -1
    /// is unvalidated and the raw values stay available through <see cref="RawPrices"/>. Making every listed item available at
    /// every station is NEW/provisional: the original station inventories are unresolved (the RT-F045 trading posts are
    /// credit-recipe stations, not shops).
    /// </summary>
    public static StationPriceTable FromOriginalItems(IEnumerable<string> stationIds, OriginalItemCatalog items)
    {
        ArgumentNullException.ThrowIfNull(items);
        static string Key(OriginalItem i) => i.Id.ToString(CultureInfo.InvariantCulture);
        var table = new StationPriceTable(stationIds, items.Items.Select(i =>
            (Key(i), i.Buy.Value > 0 ? (long?)i.Buy.Value : null, i.Sell.Value > 0 ? (long?)i.Sell.Value : null)));
        // Upgrade modules are priced at their raw buy field (f[8] > 0), id = decimal ITM id. Exclusivity, slots and effects are undecoded.
        foreach (var i in items.Items)
            if (OriginalUpgradeCategories.Contains(i.Category) && i.Buy.Value > 0)
                foreach (var station in table._stationIds)
                    table._upgrade[(station, Key(i))] = i.Buy.Value;
        table.RawPrices = items.Items.Select(i => new RawItemPrice(Key(i), i.Buy.Value, i.Sell.Value)).ToArray();
        return table;
    }

    /// <summary>Unmodified source buy/sell fields (empty for explicitly authored tables).</summary>
    public IReadOnlyList<RawItemPrice> RawPrices { get; private set; } = Array.Empty<RawItemPrice>();

    public bool TryGetBuyUnitPrice(string stationId, string itemId, out long unitPrice) =>
        _buy.TryGetValue((stationId, itemId), out unitPrice);

    public bool TryGetSellUnitPrice(string stationId, string itemId, out long unitPrice) =>
        _sell.TryGetValue((stationId, itemId), out unitPrice);

    public bool TryGetUpgradePrice(string stationId, string upgradeId, out long price) =>
        _upgrade.TryGetValue((stationId, upgradeId), out price);

    public bool TryGetRepairPrice(string stationId, string repairId, out long price) =>
        _repair.TryGetValue((stationId, repairId), out price);

    /// <summary>
    /// Adds explicit upgrade prices (NEW fixtures or hand-authored data) at every station of this table, replacing none: a repeated
    /// upgrade id is refused. Returns this table.
    /// </summary>
    public StationPriceTable WithUpgradePrices(IEnumerable<(string UpgradeId, long Price)> prices) => AddPrices(prices, _upgrade, "upgrade");

    /// <summary>
    /// Adds explicit repair prices. The original repair rows (ids 39/40/43) have buy 0 and no decoded cost (RT-F047), so no price
    /// is ever derived from original data: every repair price here is authored and NEW/provisional.
    /// </summary>
    public StationPriceTable WithRepairPrices(IEnumerable<(string RepairId, long Price)> prices) => AddPrices(prices, _repair, "repair");

    private StationPriceTable AddPrices(IEnumerable<(string Id, long Price)> prices, Dictionary<(string, string), long> target, string what)
    {
        ArgumentNullException.ThrowIfNull(prices);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, price) in prices.ToArray())
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException($"A {what} price needs an id.", nameof(prices));
            if (!seen.Add(id) || _stationIds.Any(s => target.ContainsKey((s, id))))
                throw new ArgumentException($"Duplicate {what} price for '{id}'.", nameof(prices));
            if (price < 0) throw new ArgumentException($"Negative {what} price for '{id}'.", nameof(prices));
            foreach (var station in _stationIds) target[(station, id)] = price;
        }
        return this;
    }
}
