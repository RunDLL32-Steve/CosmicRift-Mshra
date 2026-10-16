namespace RogueTrader.OriginalData;

/// <summary>
/// Resolves the item-reference chain of one projectile-layout ITM row: ITM f[31] (the ammo item id; 0 = none; mines and
/// defensive weapons reference themselves) and f[142] explodeItem (child effect). Evidence: the documented MIRV chain
/// (row 6 id 1047 -> ammo 2002 -> child 3008, FEATURE_CATALOG RT-F040) and an audit of the pinned Rogue_mp.itm in which every
/// non-zero f[31] and f[142] of the 92 type-6/227-field rows resolves to an existing item id. The f[31] name is inferred
/// from that pattern (the field is not named in the pinned notes); reference semantics beyond resolution are not decided.
/// </summary>
public static class WeaponChainFields
{
    public const int AmmoItem = 31;
}

/// <summary>
/// One sub-projectile entry of a type-8 multi-item row (for example 3008 "MIRV Missile-Blast"). Layout inferred from the pinned
/// Rogue_mp.itm: entries start at f[97] with stride 6 (<c>id, 0, 0, 0, offset, 0</c>); 133 fields = 6 entries, 157 = 10, 127 = 5.
/// <see cref="RawOffset"/> (entry field 4; MIRV: -5,-10,-15,5,10,15) is kept raw: its meaning (angle/spread unit) is undecided.
/// </summary>
public sealed record OriginalMultiItemEntry(int ItemId, int RawOffset, OriginalItem? Item);

/// <summary>A resolved reference chain. <see cref="Children"/> follows f[142] transitively (type-6 rows only) and stops at a row without a projectile layout, at 0, or at a repeated id. Type-8 targets (every f[142] target in the pinned file) are listed via <see cref="ChildEntries"/>.</summary>
public sealed class OriginalWeaponChain
{
    public const int MultiItemType = 8;
    public const int MultiItemFirstEntryField = 97;
    public const int MultiItemEntryStride = 6;
    public const int MultiItemOffsetInEntry = 4;

    private OriginalWeaponChain(OriginalItem launcher, OriginalItem? ammo, bool ammoIsSelf, IReadOnlyList<OriginalItem> children, IReadOnlyList<OriginalMultiItemEntry> childEntries)
    {
        Launcher = launcher; Ammo = ammo; AmmoIsSelf = ammoIsSelf; Children = children; ChildEntries = childEntries;
    }

    public OriginalItem Launcher { get; }
    /// <summary>The ammo item, or null when f[31] is 0.</summary>
    public OriginalItem? Ammo { get; }
    /// <summary>True when f[31] equals the launcher's own id (mines, defensive weapons).</summary>
    public bool AmmoIsSelf { get; }
    /// <summary>Child effect items in chain order (first = the launcher's f[142]).</summary>
    public IReadOnlyList<OriginalItem> Children { get; }
    /// <summary>Sub-projectile entries of the last child when it is a type-8 multi-item row (else empty); each id resolved against the catalog.</summary>
    public IReadOnlyList<OriginalMultiItemEntry> ChildEntries { get; }

    /// <summary>Resolves the chain of <paramref name="launcher"/>. Throws <see cref="OriginalItemException"/> for a non-projectile row or a non-zero reference with no matching item.</summary>
    public static OriginalWeaponChain Resolve(OriginalItemCatalog catalog, OriginalItem launcher)
    {
        var view = OriginalProjectile.Of(launcher);
        var ammoId = launcher.Record.Int(WeaponChainFields.AmmoItem).Value;
        OriginalItem? ammo = ammoId == 0 ? null : Lookup(catalog, launcher, WeaponChainFields.AmmoItem, ammoId);

        var children = new List<OriginalItem>();
        var seen = new HashSet<int> { launcher.Id };
        var current = view;
        while (current.ExplodeItem.Value != 0)
        {
            var child = Lookup(catalog, current.Item, ItmProjectileFields.ExplodeItem, current.ExplodeItem.Value);
            if (!seen.Add(child.Id)) break;
            children.Add(child);
            if (!OriginalProjectile.TryOf(child, out var next)) break;
            current = next!;
        }
        var entries = children.Count > 0 ? MultiItemEntries(catalog, children[^1]) : [];
        return new OriginalWeaponChain(launcher, ammo, ammoId == launcher.Id, children, entries);
    }

    /// <summary>Reads the stride-6 entry list of a type-8 row; empty for any other type. Throws when an entry id is unresolved.</summary>
    public static IReadOnlyList<OriginalMultiItemEntry> MultiItemEntries(OriginalItemCatalog catalog, OriginalItem item)
    {
        var list = new List<OriginalMultiItemEntry>();
        if (item.RecordType != MultiItemType) return list;
        for (var f = MultiItemFirstEntryField; f + MultiItemOffsetInEntry < item.Record.FieldCount; f += MultiItemEntryStride)
        {
            var id = item.Record.Int(f).Value;
            if (id == 0) continue;
            list.Add(new OriginalMultiItemEntry(id, item.Record.Int(f + MultiItemOffsetInEntry).Value, Lookup(catalog, item, f, id)));
        }
        return list;
    }

    private static OriginalItem Lookup(OriginalItemCatalog catalog, OriginalItem from, int field, int id)
    {
        try { return catalog.ById(id); }
        catch (KeyNotFoundException) { throw new OriginalItemException($"{from.Record.SourceFile} row {from.RowNumber} f[{field}]={id}: no item with that id."); }
    }
}
