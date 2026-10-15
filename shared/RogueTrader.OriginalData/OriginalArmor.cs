namespace RogueTrader.OriginalData;

/// <summary>
/// Raw view of an original "Hull Armor" item (type 13, 137 fields) for RT-F041 (data only). f[42..44] is a modifier block
/// shared by every type-13 row (Engine Mods and Thruster Mods use it too: SuperBoosters 80,100,0; Turbo Reaction Thrusters
/// 0,0,50), so it is not armor-specific. The armors' negative values do not store the protection grades printed in their
/// help text (Ablative Shell row 136 and Duraplas row 21 carry the identical -75,-75,-20 despite different grades); where
/// the grades are stored is undecoded. The three fields are exposed unnamed, unscaled and unvalidated, with no semantic
/// decision.
/// </summary>
public sealed class OriginalArmor
{
    public const int RecordType = 13;
    public const int FieldCount = 137;
    public const string ArmorCategory = "Hull Armor";
    public const int ModifierA = 42;
    public const int ModifierB = 43;
    public const int ModifierC = 44;

    private OriginalArmor(OriginalItem item)
    {
        Item = item;
        ModifierFieldA = item.Record.Int(ModifierA);
        ModifierFieldB = item.Record.Int(ModifierB);
        ModifierFieldC = item.Record.Int(ModifierC);
    }

    public OriginalItem Item { get; }
    /// <summary>Raw f[42] of the shared type-13 modifier block (meaning undecoded).</summary>
    public OriginalInt ModifierFieldA { get; }
    /// <summary>Raw f[43] of the shared type-13 modifier block (meaning undecoded).</summary>
    public OriginalInt ModifierFieldB { get; }
    /// <summary>Raw f[44] of the shared type-13 modifier block (meaning undecoded).</summary>
    public OriginalInt ModifierFieldC { get; }
    /// <summary>The original help text, which states the protection grades in words (not stored in f[42..44]).</summary>
    public string Description => Item.Record.Value(6);

    public static bool TryOf(OriginalItem item, out OriginalArmor? view)
    {
        var ok = item.RecordType == RecordType && item.Record.FieldCount == FieldCount && item.Category == ArmorCategory;
        view = ok ? new OriginalArmor(item) : null;
        return ok;
    }

    /// <summary>Every Hull Armor row in file order.</summary>
    public static IReadOnlyList<OriginalArmor> All(OriginalItemCatalog catalog)
    {
        var list = new List<OriginalArmor>();
        foreach (var i in catalog.Items)
            if (TryOf(i, out var a)) list.Add(a!);
        return list;
    }
}

/// <summary>One (armor item id, effect item id) pair of the "Remove Armor" row, ids resolved against the catalog.</summary>
public sealed record OriginalArmorRemoval(OriginalItem Armor, OriginalItem Effect);

public static class OriginalRemoveArmor
{
    /// <summary>Item id of "Remove Armor" (type 15, Resources).</summary>
    public const int RemoveArmorItemId = 41;
    /// <summary>First field of the (armor id, effect id) pair list in the Remove Armor row (inferred from the pinned row 151: f[31]=37,f[32]=42,f[33]=38,...); pairs run until the first 0.</summary>
    public const int FirstPairField = 31;

    public static IReadOnlyList<OriginalArmorRemoval> Pairs(OriginalItemCatalog catalog)
    {
        var row = catalog.ById(RemoveArmorItemId);
        var r = row.Record;
        var list = new List<OriginalArmorRemoval>();
        for (var i = FirstPairField; i + 1 < r.FieldCount; i += 2)
        {
            var a = r.Int(i).Value;
            if (a == 0) break;
            list.Add(new OriginalArmorRemoval(catalog.ById(a), catalog.ById(r.Int(i + 1).Value)));
        }
        return list;
    }
}
