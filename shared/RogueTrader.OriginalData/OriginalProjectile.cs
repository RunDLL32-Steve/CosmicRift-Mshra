namespace RogueTrader.OriginalData;

/// <summary>
/// Field indexes of the projectile-related ITM v57 fields, named by the active projectile loader as recorded in
/// docs/original-research/FIELD_INTERPRETATION.md ("Projectile fields and negative values"). Values are raw and unscaled;
/// time and velocity units, the negative inheritance sentinel and the negative lifetime trigger are unvalidated.
/// </summary>
public static class ItmProjectileFields
{
    public const int FireDelay = 54;
    public const int InheritedSpeed = 94;
    public const int MuzzleVelocity = 101;
    public const int AliveTime = 110;
    public const int MaxLivePerPlayer = 131;
    public const int AntiRecharge = 135;
    public const int AntiFire = 136;
    public const int AntiThrust = 137;
    public const int AntiRotate = 138;
    public const int ExplodeItem = 142;
    /// <summary>Highest index read; a row must have more fields than this.</summary>
    public const int MaxTypedField = ExplodeItem;
    /// <summary>Record type (f[0]) of the projectile layout. Audit of the pinned Rogue_mp.itm: all 92 rows with type 6 and 227 fields carry integers at every field above; the type-8 rows with 109..193 fields do not (for example f[94]="None" on row 42), so they are not given a view.</summary>
    public const int ProjectileRecordType = 6;
    public const int ProjectileFieldCount = 227;
}

/// <summary>
/// Raw projectile view of one <see cref="OriginalItem"/> (RT-F036 inheritance, RT-F037 delay/life/muzzle, plus the
/// RT-F038/F039/F040 fields in the same record). Which items are projectile launchers is not decided here: the view can
/// be taken for any row that has the fields. Every property returns the raw integer with its source field index;
/// negatives such as inheritedSpeed -550 or aliveTime -65 are preserved and flagged, never interpreted.
/// </summary>
public sealed class OriginalProjectile
{
    private OriginalProjectile(OriginalItem item)
    {
        Item = item;
        var r = item.Record;
        FireDelay = r.Int(ItmProjectileFields.FireDelay);
        InheritedSpeed = r.Int(ItmProjectileFields.InheritedSpeed);
        MuzzleVelocity = r.Int(ItmProjectileFields.MuzzleVelocity);
        AliveTime = r.Int(ItmProjectileFields.AliveTime);
        MaxLivePerPlayer = r.Int(ItmProjectileFields.MaxLivePerPlayer);
        AntiRecharge = r.Int(ItmProjectileFields.AntiRecharge);
        AntiFire = r.Int(ItmProjectileFields.AntiFire);
        AntiThrust = r.Int(ItmProjectileFields.AntiThrust);
        AntiRotate = r.Int(ItmProjectileFields.AntiRotate);
        ExplodeItem = r.Int(ItmProjectileFields.ExplodeItem);
    }

    public OriginalItem Item { get; }
    public OriginalInt FireDelay { get; }
    public OriginalInt InheritedSpeed { get; }
    public OriginalInt MuzzleVelocity { get; }
    public OriginalInt AliveTime { get; }
    public OriginalInt MaxLivePerPlayer { get; }
    public OriginalInt AntiRecharge { get; }
    public OriginalInt AntiFire { get; }
    public OriginalInt AntiThrust { get; }
    public OriginalInt AntiRotate { get; }
    /// <summary>Raw item id of the child effect (0 = none per the documented rows); not resolved here.</summary>
    public OriginalInt ExplodeItem { get; }

    /// <summary>True for rows with the type-6, 227-field projectile layout (see <see cref="ItmProjectileFields.ProjectileRecordType"/>). Other rows keep only their raw record.</summary>
    public static bool TryOf(OriginalItem item, out OriginalProjectile? view)
    {
        var ok = item.RecordType == ItmProjectileFields.ProjectileRecordType && item.Record.FieldCount == ItmProjectileFields.ProjectileFieldCount;
        view = ok ? new OriginalProjectile(item) : null;
        return ok;
    }

    /// <summary>Returns the view, or throws <see cref="OriginalItemException"/> when the row is not the projectile layout.</summary>
    public static OriginalProjectile Of(OriginalItem item)
    {
        if (!TryOf(item, out var view))
            throw new OriginalItemException($"{item.Record.SourceFile} row {item.RowNumber}: type {item.RecordType} with {item.Record.FieldCount} fields is not the type-{ItmProjectileFields.ProjectileRecordType}/{ItmProjectileFields.ProjectileFieldCount}-field projectile layout.");
        return view!;
    }
}
