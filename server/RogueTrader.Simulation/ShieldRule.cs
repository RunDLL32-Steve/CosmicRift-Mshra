namespace RogueTrader.Simulation;

/// <summary>The original shield rule for a weapon that drains shields (NWS/Rogue.nws rows 39-41 and 131).</summary>
public static class ShieldRule
{
    /// <summary>
    /// One hit on a ship with <paramref name="shield"/> left (its energy, for a ship whose energy is its shield). A shield that can
    /// take the hit's whole <paramref name="drain"/> takes it and the hull takes nothing ("as long as you have any Shield Energy you
    /// will take no Hull Point Damage"). A hit that finds less shield than its drain empties the shield and also does its hull
    /// <paramref name="damage"/> ("when your Shield Energy is at 0, you will take Hull Damage"). Reading the emptying hit as one
    /// that reaches the hull is review R1-1 on #120: with a shield that recharges between shots, hull damage only at exactly 0
    /// would let a lone attacker never reach the hull. Neither value goes below 0.
    /// </summary>
    public static (float Shield, float Hull) Hit(float shield, float hull, float drain, float damage)
    {
        var shieldAfter = MathF.Max(0, shield - drain);
        var hullAfter = shield < drain ? MathF.Max(0, hull - damage) : hull;
        return (shieldAfter, hullAfter);
    }
}
