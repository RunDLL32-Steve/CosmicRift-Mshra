namespace RogueTrader.OriginalData;

/// <summary>
/// Raw view of one original upgrade-module row for RT-F048 (data only): type-13 non-armor rows (Engine Mods, Thruster Mods,
/// Power Mods; 137 fields) and the "HyperDrive Systems" rows (HyperCore type 4, hyper abilities type 17). Price, resale and
/// skill logic are the raw f[8], f[15], f[5] values; the modifier block f[42..44] is exposed for type 13 only, unnamed and
/// unscaled (observed: f[42] with boosters, f[43] with boosters/afterburners, f[44] with reaction thrusters; meaning
/// undecoded, see OriginalArmor). No stacking, exclusivity or effect is derived.
/// </summary>
public sealed class OriginalUpgrade
{
    public static readonly IReadOnlyList<string> Families = ["Engine Mods", "Thruster Mods", "Power Mods", "HyperDrive Systems"];

    private OriginalUpgrade(OriginalItem item) => Item = item;

    public OriginalItem Item { get; }
    public string Family => Item.Category;
    /// <summary>Raw f[5] skill logic text ("" when empty).</summary>
    public string Logic => Item.Record.Value(5);
    /// <summary>f[42..44] for type-13 rows; empty otherwise.</summary>
    public IReadOnlyList<OriginalInt> ModifierBlock =>
        Item.RecordType == OriginalArmor.RecordType ? Item.Record.Ints(OriginalArmor.ModifierA, 3) : [];

    /// <summary>All upgrade rows of the four families in file order.</summary>
    public static IReadOnlyList<OriginalUpgrade> All(OriginalItemCatalog catalog) =>
        catalog.Items.Where(i => Families.Contains(i.Category)).Select(i => new OriginalUpgrade(i)).ToList();
}
