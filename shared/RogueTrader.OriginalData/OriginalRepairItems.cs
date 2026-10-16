namespace RogueTrader.OriginalData;

/// <summary>
/// Raw view of the original "Resources"-category type-1 effect rows (53 fields) with a non-zero f[33]: exactly the three
/// repair rows Minor/Major/Full Repair (ids 39/40/43; f[33] = 5/10/50). The credit-effect rows (ids 17/18/19/44) are a
/// different field (f[31] = 80/55/300/20, sell -1) and are not part of this view. f[33] is exposed unscaled and
/// unnamed beyond an effect amount: whether it is hit points, a percentage or a fraction, how it is applied and what it
/// costs at a dock are undecoded (RT-F047 data only).
/// </summary>
public sealed class OriginalEffectAmountRow
{
    public const int RecordType = 1;
    public const int FieldCount = 53;
    public const int AmountField = 33;

    private OriginalEffectAmountRow(OriginalItem item) => Item = item;

    public OriginalItem Item { get; }
    /// <summary>Raw f[33] (unscaled, meaning undecoded).</summary>
    public OriginalInt Amount => Item.Record.Int(AmountField);

    public static bool TryOf(OriginalItem item, out OriginalEffectAmountRow? view)
    {
        var ok = item.RecordType == RecordType && item.Record.FieldCount == FieldCount
            && item.Category == OriginalItemCatalog.ResourceCategory && item.Record.Int(AmountField).Value != 0;
        view = ok ? new OriginalEffectAmountRow(item) : null;
        return ok;
    }

    /// <summary>Every type-1 effect row with a non-zero f[33], in file order.</summary>
    public static IReadOnlyList<OriginalEffectAmountRow> All(OriginalItemCatalog catalog)
    {
        var list = new List<OriginalEffectAmountRow>();
        foreach (var i in catalog.Items)
            if (TryOf(i, out var r)) list.Add(r!);
        return list;
    }
}

/// <summary>The three original repair effect rows (Minor, Major, Full Repair).</summary>
public static class OriginalRepairItems
{
    public static readonly int[] RepairItemIds = [39, 40, 43];

    public static IReadOnlyList<OriginalEffectAmountRow> Resolve(OriginalItemCatalog catalog) =>
        OriginalEffectAmountRow.All(catalog).Where(r => RepairItemIds.Contains(r.Item.Id)).ToList();
}
