namespace RogueTrader.OriginalData;

/// <summary>Field indices of the item (ITM) record layout that are confirmed by the catalog; every other field stays raw.</summary>
public static class ItmFields
{
    public const int RecordType = 0;
    public const int Id = 2;
    public const int Name = 3;
    public const int Category = 4;
    /// <summary>Catalog "mass" field (raw units, unscaled).</summary>
    public const int Mass = 7;
    /// <summary>Catalog "buy" field (raw, unscaled).</summary>
    public const int Buy = 8;
    /// <summary>Catalog "sell" field (raw; -1 is a negative sentinel whose native meaning is unvalidated).</summary>
    public const int Sell = 15;
    public const int MinFieldCount = 31;
}

/// <summary>
/// One original item row. The full <see cref="Record"/> is kept; the typed accessors cover only the fields the
/// catalog confirms (type, id, name, category, mass, buy, sell). Nothing is scaled or defaulted.
/// </summary>
public sealed class OriginalItem
{
    internal OriginalItem(RawRecord record) => Record = record;

    public RawRecord Record { get; }
    public int RowNumber => Record.RowNumber;
    public int RecordType => Record.Int(ItmFields.RecordType).Value;
    public int Id => Record.Int(ItmFields.Id).Value;
    public string Name => Record.Value(ItmFields.Name);
    public string Category => Record.Value(ItmFields.Category);
    public OriginalInt Mass => Record.Int(ItmFields.Mass);
    public OriginalInt Buy => Record.Int(ItmFields.Buy);
    public OriginalInt Sell => Record.Int(ItmFields.Sell);
}
