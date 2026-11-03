namespace RogueTrader.OriginalData;

/// <summary>
/// Field indexes of the projectile-related ITM v57 fields, named by the active projectile loader as recorded in
/// docs/original-research/FIELD_INTERPRETATION.md ("Projectile fields and negative values"). Values are raw and unscaled;
/// time and velocity units, the negative inheritance sentinel and the negative lifetime trigger are unvalidated.
/// </summary>
public static class ItmProjectileFields
{
    /// <summary>Upstream ammoUsedPerShot (G5 audit). Same index as <see cref="WeaponChainFields.AmmoUsedPerShot"/>.</summary>
    public const int AmmoUsedPerShot = WeaponChainFields.AmmoUsedPerShot;
    /// <summary>Upstream ammoCapacity (G5 audit).</summary>
    public const int AmmoCapacity = WeaponChainFields.AmmoCapacity;
    public const int FireDelay = 54;
    public const int InheritedSpeed = 94;
    public const int MuzzleVelocity = 101;
    /// <summary>Upstream bounceCount (G5 audit; X-Laser carries -1).</summary>
    public const int BounceCount = 104;
    /// <summary>Upstream proximityRadius (G5 audit; the only radius-like trigger field of the projectile record).</summary>
    public const int ProximityRadius = 108;
    public const int AliveTime = 110;
    /// <summary>Upstream inactiveTime (G5 audit; MIRV 65 against aliveTime -65).</summary>
    public const int InactiveTime = 115;
    /// <summary>Upstream damageMode (G5 audit; 0 or 2 in the pinned file).</summary>
    public const int DamageMode = 116;
    public const int MaxLivePerPlayer = 131;
    public const int AntiRecharge = 135;
    public const int AntiFire = 136;
    public const int AntiThrust = 137;
    public const int AntiRotate = 138;
    /// <summary>Upstream preventPointBlank (G5 audit; 0 or -1 in the pinned file).</summary>
    public const int PreventPointBlank = 141;
    public const int ExplodeItem = 142;
    /// <summary>Highest index read; a row must have more fields than this.</summary>
    public const int MaxTypedField = ExplodeItem;
    /// <summary>Record type (f[0]) of the projectile layout. Audit of the pinned Rogue_mp.itm: all 92 rows with type 6 and 227 fields carry integers at every field above; the type-8 rows with 109..193 fields do not (for example f[94]="None" on row 42), so they are not given a view.</summary>
    public const int ProjectileRecordType = 6;
    public const int ProjectileFieldCount = 227;
}

/// <summary>
/// Raw projectile view of one <see cref="OriginalItem"/> (RT-F036 inheritance, RT-F037 delay/life/muzzle, plus the
/// RT-F038/F039/F040 fields in the same record, and the G5-audited ammo, bounce, proximity, inactive, damage-mode and
/// point-blank fields). Which items are projectile launchers is not decided here: the view can
/// be taken for any row that has the fields. Every property returns the raw integer with its source field index;
/// negatives such as inheritedSpeed -550 or aliveTime -65 are preserved and flagged, never interpreted.
/// </summary>
public sealed class OriginalProjectile
{
    private OriginalProjectile(OriginalItem item)
    {
        Item = item;
        var r = item.Record;
        AmmoUsedPerShot = r.Int(ItmProjectileFields.AmmoUsedPerShot);
        AmmoCapacity = r.Int(ItmProjectileFields.AmmoCapacity);
        FireDelay = r.Int(ItmProjectileFields.FireDelay);
        InheritedSpeed = r.Int(ItmProjectileFields.InheritedSpeed);
        MuzzleVelocity = r.Int(ItmProjectileFields.MuzzleVelocity);
        BounceCount = r.Int(ItmProjectileFields.BounceCount);
        ProximityRadius = r.Int(ItmProjectileFields.ProximityRadius);
        AliveTime = r.Int(ItmProjectileFields.AliveTime);
        InactiveTime = r.Int(ItmProjectileFields.InactiveTime);
        DamageMode = r.Int(ItmProjectileFields.DamageMode);
        MaxLivePerPlayer = r.Int(ItmProjectileFields.MaxLivePerPlayer);
        AntiRecharge = r.Int(ItmProjectileFields.AntiRecharge);
        AntiFire = r.Int(ItmProjectileFields.AntiFire);
        AntiThrust = r.Int(ItmProjectileFields.AntiThrust);
        AntiRotate = r.Int(ItmProjectileFields.AntiRotate);
        PreventPointBlank = r.Int(ItmProjectileFields.PreventPointBlank);
        ExplodeItem = r.Int(ItmProjectileFields.ExplodeItem);
    }

    public OriginalItem Item { get; }
    public OriginalInt AmmoUsedPerShot { get; }
    public OriginalInt AmmoCapacity { get; }
    public OriginalInt FireDelay { get; }
    public OriginalInt InheritedSpeed { get; }
    public OriginalInt MuzzleVelocity { get; }
    /// <summary>Raw bounce count; -1 (X-Laser) is preserved as a sentinel, not read as unlimited.</summary>
    public OriginalInt BounceCount { get; }
    /// <summary>Raw proximity radius; unit and shape (circle or otherwise) undecided.</summary>
    public OriginalInt ProximityRadius { get; }
    public OriginalInt AliveTime { get; }
    public OriginalInt InactiveTime { get; }
    public OriginalInt DamageMode { get; }
    public OriginalInt MaxLivePerPlayer { get; }
    public OriginalInt AntiRecharge { get; }
    public OriginalInt AntiFire { get; }
    public OriginalInt AntiThrust { get; }
    public OriginalInt AntiRotate { get; }
    public OriginalInt PreventPointBlank { get; }
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
