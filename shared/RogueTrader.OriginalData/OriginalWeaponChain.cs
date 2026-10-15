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

/// <summary>A resolved reference chain. <see cref="Children"/> follows f[142] transitively (type-6 rows only) and stops at a row without a projectile layout, at 0, or at a repeated id.</summary>
public sealed class OriginalWeaponChain
{
    private OriginalWeaponChain(OriginalItem launcher, OriginalItem? ammo, bool ammoIsSelf, IReadOnlyList<OriginalItem> children)
    {
        Launcher = launcher; Ammo = ammo; AmmoIsSelf = ammoIsSelf; Children = children;
    }

    public OriginalItem Launcher { get; }
    /// <summary>The ammo item, or null when f[31] is 0.</summary>
    public OriginalItem? Ammo { get; }
    /// <summary>True when f[31] equals the launcher's own id (mines, defensive weapons).</summary>
    public bool AmmoIsSelf { get; }
    /// <summary>Child effect items in chain order (first = the launcher's f[142]).</summary>
    public IReadOnlyList<OriginalItem> Children { get; }

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
        return new OriginalWeaponChain(launcher, ammo, ammoId == launcher.Id, children);
    }

    private static OriginalItem Lookup(OriginalItemCatalog catalog, OriginalItem from, int field, int id) =>
        catalog.Items.FirstOrDefault(i => i.Id == id)
        ?? throw new OriginalItemException($"{from.Record.SourceFile} row {from.RowNumber} f[{field}]={id}: no item with that id.");
}
