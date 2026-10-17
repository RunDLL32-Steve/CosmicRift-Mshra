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
    private readonly Dictionary<(string Station, string Item), long> _buy = new();
    private readonly Dictionary<(string Station, string Item), long> _sell = new();

    /// <summary>Explicit prices (NEW fixtures or hand-authored data). Absent entries are not traded.</summary>
    public StationPriceTable(string stationId, IEnumerable<(string ItemId, long? Buy, long? Sell)> prices)
        : this(new[] { stationId }, prices) { }

    public StationPriceTable(IEnumerable<string> stationIds, IEnumerable<(string ItemId, long? Buy, long? Sell)> prices)
    {
        ArgumentNullException.ThrowIfNull(stationIds);
        ArgumentNullException.ThrowIfNull(prices);
        var stations = stationIds.ToArray();
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
        table.RawPrices = items.Items.Select(i => new RawItemPrice(Key(i), i.Buy.Value, i.Sell.Value)).ToArray();
        return table;
    }

    /// <summary>Unmodified source buy/sell fields (empty for explicitly authored tables).</summary>
    public IReadOnlyList<RawItemPrice> RawPrices { get; private set; } = Array.Empty<RawItemPrice>();

    public bool TryGetBuyUnitPrice(string stationId, string itemId, out long unitPrice) =>
        _buy.TryGetValue((stationId, itemId), out unitPrice);

    public bool TryGetSellUnitPrice(string stationId, string itemId, out long unitPrice) =>
        _sell.TryGetValue((stationId, itemId), out unitPrice);

    /// <summary>Upgrades are not priced here (queue item M5).</summary>
    public bool TryGetUpgradePrice(string stationId, string upgradeId, out long price)
    {
        price = 0;
        return false;
    }
}
