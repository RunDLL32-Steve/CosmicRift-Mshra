namespace RogueTrader.OriginalData;

/// <summary>
/// Raw starting-loadout string of a hull (VEH f[129], for example <c>addinv=Plasma Cannon</c>) for RT-F035 (data only).
/// The string is kept verbatim; only the leading <c>addinv=</c> verb is split off and the item is resolved by exact name.
/// The event grammar (RT-F067), event order, and inventory/energy/velocity policy remain unvalidated.
/// </summary>
public sealed record OriginalStartingLoadout(OriginalHull Hull, string Raw, string Verb, string ItemName, OriginalItem? Item)
{
    public const int EventField = 129;
    public const string AddInventoryVerb = "addinv";

    public static IReadOnlyList<OriginalStartingLoadout> All(OriginalHullCatalog hulls, OriginalItemCatalog items)
    {
        var list = new List<OriginalStartingLoadout>();
        foreach (var h in hulls.Hulls)
        {
            var raw = h.VehRecord.Value(EventField);
            var eq = raw.IndexOf('=');
            var verb = eq < 0 ? raw : raw[..eq];
            var name = eq < 0 ? "" : raw[(eq + 1)..];
            var matches = items.Items.Where(i => i.Name == name).ToList();
            list.Add(new OriginalStartingLoadout(h, raw, verb, name, matches.Count == 1 ? matches[0] : null));
        }
        return list;
    }
}

/// <summary>
/// One type-16 "Ship Hulls-*" ITM row (same-race hull exchange) for RT-F035 (data only). f[5] and f[31] hold a skill logic
/// string, f[32] the target hull's RPG skill id; buy/sell are the raw f[8]/f[15]. No exchange flow is implemented.
/// </summary>
public sealed class OriginalHullExchange
{
    public const int RecordType = 16;
    public const int LogicCopyField = 31;
    public const int TargetSkillField = 32;

    private OriginalHullExchange(OriginalItem item) => Item = item;

    public OriginalItem Item { get; }
    /// <summary>Raw f[5] skill logic string.</summary>
    public string Logic => Item.Record.Value(5);
    /// <summary>Raw f[31] skill logic string (equals f[5] for 11 of 12 rows; Zag'Gir differs only in term order).</summary>
    public string LogicCopy => Item.Record.Value(LogicCopyField);
    /// <summary>Raw f[32]: equals the RPG skill id of the hull named by the item.</summary>
    public OriginalInt TargetSkill => Item.Record.Int(TargetSkillField);

    public static IReadOnlyList<OriginalHullExchange> All(OriginalItemCatalog catalog) =>
        catalog.Items.Where(i => i.RecordType == RecordType).Select(i => new OriginalHullExchange(i)).ToList();
}
