namespace RogueTrader.OriginalData;

/// <summary>
/// Resolves the item-reference chain of one projectile-layout ITM row: ITM f[31] (the ammo item id; 0 = none; mines and
/// defensive weapons reference themselves) and f[142] explodeItem (child effect). Evidence: the documented MIRV chain
/// (row 6 id 1047 -> ammo 2002 -> child 3008, FEATURE_CATALOG RT-F040) and an audit of the pinned Rogue_mp.itm in which every
/// non-zero f[31] and f[142] of the 92 type-6/227-field rows resolves to an existing item id. The upstream InfantryOnline parser
/// (ItmInfo.Projectile.cs / ItmInfo.MultiUse.cs at bb97579e) names f[31] useAmmoID, f[32] ammoUsedPerShot and f[33]
/// ammoCapacity for both type 6 and type 8; that is parser naming, not original-executable evidence, and reference
/// semantics beyond resolution are not decided.
/// </summary>
public static class WeaponChainFields
{
    public const int AmmoItem = 31;
    /// <summary>Upstream ammoUsedPerShot (raw; 1 for single launchers, 2 for the Dual type-8 rows in the pinned file).</summary>
    public const int AmmoUsedPerShot = 32;
    /// <summary>Upstream ammoCapacity (raw; non-zero only for 1035 Heavy Mass Driver 5, 1077 Rocket Pod 3, 3006 5, 3017 3).</summary>
    public const int AmmoCapacity = 33;
}

/// <summary>
/// One child entry of a type-8 (upstream "MultiUse") row, for example 3008 "MIRV Missile-Blast". Entries start at f[97] with
/// stride 6; every type-8 row of the pinned Rogue_mp.itm has 97 + 6k fields (109..193). The six raw ints are kept in
/// <see cref="RawFields"/>. Their names come from the upstream InfantryOnline parser (ItmInfo.MultiUse.cs at bb97579e:
/// <c>id, deltaX, deltaY, deltaZ, deltaAngle, multiLinkMode</c>), which is not original-executable evidence: units of the
/// deltas and of <see cref="DeltaAngle"/> are not decided (G5 audit, docs/original-research/G5_WEAPON_VALUE_AUDIT.md).
/// </summary>
public sealed record OriginalMultiItemEntry(int ItemId, IReadOnlyList<int> RawFields, OriginalItem? Item)
{
    /// <summary>Raw entry field 1 (upstream deltaX). Pinned rows use +-5, +-10, +-20 (for example 3006 Dual Light Mass Drivers 20/-20).</summary>
    public int DeltaX => RawFields[1];
    /// <summary>Raw entry field 2 (upstream deltaY). Non-zero only in 3002 BaseStationCannon (15) and 3018 Wake Caster (-25).</summary>
    public int DeltaY => RawFields[2];
    /// <summary>Raw entry field 3 (upstream deltaZ). Zero in every pinned entry.</summary>
    public int DeltaZ => RawFields[3];
    /// <summary>Raw entry field 4 (upstream deltaAngle; MIRV -5,-10,-15,5,10,15; Wake Caster 135/225). Unit undecided.</summary>
    public int DeltaAngle => RawFields[4];
    /// <summary>Raw entry field 5 (upstream multiLinkMode, enum None=0/Disappear=1/Explode=2 in that parser). Zero in every pinned entry.</summary>
    public int MultiLinkMode => RawFields[5];
    /// <summary>Same value as <see cref="DeltaAngle"/>; kept for the RT-F040 callers that named it before the field was identified.</summary>
    public int RawOffset => DeltaAngle;
}

/// <summary>A resolved reference chain. <see cref="Children"/> follows f[142] transitively (type-6 rows only) and stops at a row without a projectile layout, at 0, or at a repeated id. Type-8 targets (every f[142] target in the pinned file) are listed via <see cref="ChildEntries"/>.</summary>
public sealed class OriginalWeaponChain
{
    public const int MultiItemType = 8;
    public const int MultiItemFirstEntryField = 97;
    public const int MultiItemEntryStride = 6;
    /// <summary>Entry field 4, upstream name deltaAngle (formerly described as an undecided "offset").</summary>
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

    /// <summary>Reads the stride-6 entry list of a type-8 row (all six raw ints per entry); empty for any other type. Throws when an entry id is unresolved or a non-zero entry is cut short.</summary>
    public static IReadOnlyList<OriginalMultiItemEntry> MultiItemEntries(OriginalItemCatalog catalog, OriginalItem item)
    {
        var list = new List<OriginalMultiItemEntry>();
        if (item.RecordType != MultiItemType) return list;
        for (var f = MultiItemFirstEntryField; f < item.Record.FieldCount; f += MultiItemEntryStride)
        {
            var id = item.Record.Int(f).Value;
            if (id == 0) continue;
            if (f + MultiItemEntryStride > item.Record.FieldCount)
                throw new OriginalItemException($"{item.Record.SourceFile} row {item.RowNumber}: entry at f[{f}] has {item.Record.FieldCount - f} of {MultiItemEntryStride} fields.");
            var raw = new int[MultiItemEntryStride];
            for (var k = 0; k < MultiItemEntryStride; k++) raw[k] = item.Record.Int(f + k).Value;
            list.Add(new OriginalMultiItemEntry(id, raw, Lookup(catalog, item, f, id)));
        }
        return list;
    }

    private static OriginalItem Lookup(OriginalItemCatalog catalog, OriginalItem from, int field, int id)
    {
        try { return catalog.ById(id); }
        catch (KeyNotFoundException) { throw new OriginalItemException($"{from.Record.SourceFile} row {from.RowNumber} f[{field}]={id}: no item with that id."); }
    }
}
