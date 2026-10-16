namespace RogueTrader.OriginalData;

/// <summary>
/// Raw disable ("anti") durations of one projectile row: f[135] recharge, f[136] fire, f[137] thrust, f[138] rotate, as
/// named by the active projectile loader (FIELD_INTERPRETATION.md "Projectile fields and negative values"). Values are
/// unscaled integers; the time unit and recovery behavior are unvalidated (RT-F039 stays open).
/// </summary>
public sealed record OriginalDisableEffect(OriginalItem Item, int Recharge, int Fire, int Thrust, int Rotate)
{
    /// <summary>True when at least one of the four durations is non-zero.</summary>
    public bool IsDisabling => Recharge != 0 || Fire != 0 || Thrust != 0 || Rotate != 0;
}

public static class OriginalDisableEffects
{
    /// <summary>
    /// Every type-6/227-field row with a non-zero value in f[135..138], in file order. In the pinned Rogue_mp.itm this is
    /// 10 rows, including Meson/Tachyon bombs, DeathOrb/DeathRay rows (f[138]=50) that the catalog text does not mention.
    /// Rows whose description mentions a shield or other effect but carry all-zero fields (Blackout Bomb row 155) are
    /// deliberately absent: their effect is stored elsewhere and is not decoded here.
    /// </summary>
    public static IReadOnlyList<OriginalDisableEffect> Find(OriginalItemCatalog catalog)
    {
        var list = new List<OriginalDisableEffect>();
        foreach (var item in catalog.Items)
        {
            if (!OriginalProjectile.TryOf(item, out var p)) continue;
            var e = new OriginalDisableEffect(item, p!.AntiRecharge.Value, p.AntiFire.Value, p.AntiThrust.Value, p.AntiRotate.Value);
            if (e.IsDisabling) list.Add(e);
        }
        return list;
    }
}
