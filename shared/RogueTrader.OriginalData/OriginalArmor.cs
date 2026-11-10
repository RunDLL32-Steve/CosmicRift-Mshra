namespace RogueTrader.OriginalData;

/// <summary>
/// Raw view of an original "Hull Armor" item (type 13, 137 fields) for RT-F041. Type 13 is the InfantryOnline emulator's
/// utility item (<c>Docs/Itm/Utility.txt</c>, <c>ItmInfo.UtilityItem.cs</c> at bb97579). Its f[42..44] are Vehicle Top Speed,
/// Vehicle Hyper Top Speed and Vehicle Thrust, a block shared with Engine Mods and Thruster Mods (SuperBoosters 80,100,0; Turbo
/// Reaction Thrusters 0,0,50): for armour they are negative, a speed penalty. The protection grades the help text prints are
/// f[52..63], six (Ignore, Percent) pairs for Kinetic, Explosive, Electronic, Psionic, Bypass and Energy damage. The help text's
/// "Energy" damage is the emulator's Electronic pair; the emulator's own Energy pair (f[62..63]) is the shield drain and is 0
/// in every armour, as are Psionic and Bypass.
/// </summary>
public sealed class OriginalArmor
{
    public const int RecordType = 13;
    public const int FieldCount = 137;
    public const string ArmorCategory = "Hull Armor";
    public const int ModifierA = 42;
    public const int ModifierB = 43;
    public const int ModifierC = 44;
    /// <summary>f[52]: Kinetic Ignore; f[53] Kinetic Percent, then Explosive (54, 55) and Electronic (56, 57).</summary>
    public const int KineticIgnoreField = 52;
    public const int ExplosiveIgnoreField = 54;
    public const int ElectronicIgnoreField = 56;
    /// <summary>f[58..63]: Psionic, Bypass and Energy (shield drain) pairs, all 0 in the pinned armours.</summary>
    public const int PsionicIgnoreField = 58;

    private OriginalArmor(OriginalItem item)
    {
        Item = item;
        ModifierFieldA = item.Record.Int(ModifierA);
        ModifierFieldB = item.Record.Int(ModifierB);
        ModifierFieldC = item.Record.Int(ModifierC);
        Kinetic = Pair(item, KineticIgnoreField);
        Explosive = Pair(item, ExplosiveIgnoreField);
        Electronic = Pair(item, ElectronicIgnoreField);
    }

    private static OriginalArmorPair Pair(OriginalItem item, int ignoreField) =>
        new(item.Record.Int(ignoreField), item.Record.Int(ignoreField + 1));

    public OriginalItem Item { get; }
    /// <summary>Raw f[42] of the shared type-13 modifier block (meaning undecoded).</summary>
    public OriginalInt ModifierFieldA { get; }
    /// <summary>Raw f[43] of the shared type-13 modifier block (meaning undecoded).</summary>
    public OriginalInt ModifierFieldB { get; }
    /// <summary>Raw f[44] of the shared type-13 modifier block (meaning undecoded).</summary>
    public OriginalInt ModifierFieldC { get; }
    /// <summary>Raw f[52..53] Kinetic Ignore and Percent.</summary>
    public OriginalArmorPair Kinetic { get; }
    /// <summary>Raw f[54..55] Explosive Ignore and Percent.</summary>
    public OriginalArmorPair Explosive { get; }
    /// <summary>Raw f[56..57] Electronic Ignore and Percent: the "Energy" damage of the help text and NWS row 133.</summary>
    public OriginalArmorPair Electronic { get; }
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

/// <summary>
/// One raw (Ignore, Percent) protection pair of an armour item. Ignore uses the thousandths of ITM hull damage (1000 = one hull
/// point); Percent is read as per mille (tenths of a percent), the scale the emulator divides vehicle SelfReduction by
/// (<c>Vehicle.cs</c>, <c>/ 1000.0d</c>) and the only one under which chaosadvanced.itm's 990 stays below 100%.
/// </summary>
public sealed record OriginalArmorPair(OriginalInt Ignore, OriginalInt Percent)
{
    public const float IgnoreUnitsPerHullPoint = 1000f;
    public const float PercentUnitsPerWhole = 1000f;
    public float IgnoreHullPoints => Ignore.Value / IgnoreUnitsPerHullPoint;
    public float ReductionFraction => Percent.Value / PercentUnitsPerWhole;
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
