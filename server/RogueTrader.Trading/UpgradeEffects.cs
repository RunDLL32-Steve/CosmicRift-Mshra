using RogueTrader.GameData;

namespace RogueTrader.Trading;

/// <summary>
/// Applies owned upgrades to a ship definition (NEW/provisional; RT-F048 effects are undecoded). Upgrades sharing a held
/// category (raw ITM f[20]) occupy one slot, so only the highest-priced owned one per category is active. Fractions add to 1
/// and multiply the base value; the result never drops below 10% of the base, and hull bonuses add points.
/// </summary>
public static class UpgradeEffects
{
    private const float Floor = 0.1f;

    public static IReadOnlyList<StationUpgrade> Active(StationDefinition station, IEnumerable<string> owned)
    {
        ArgumentNullException.ThrowIfNull(station);
        ArgumentNullException.ThrowIfNull(owned);
        var set = owned.ToHashSet(StringComparer.Ordinal);
        return station.Upgrades.Where(u => set.Contains(u.Id))
            .GroupBy(u => u.HeldCategory == 0 ? "id:" + u.Id : "cat:" + u.HeldCategory)
            .Select(g => g.OrderByDescending(u => u.Price).ThenBy(u => u.Id, StringComparer.Ordinal).First())
            .OrderBy(u => u.Id, StringComparer.Ordinal)
            .ToList();
    }

    public static ShipDefinition Apply(ShipDefinition ship, StationDefinition station, IEnumerable<string> owned)
    {
        ArgumentNullException.ThrowIfNull(ship);
        var active = Active(station, owned);
        if (active.Count == 0) return ship;
        float Sum(Func<UpgradeEffect, float> pick) => active.Sum(u => pick(u.Effect));
        static float Scale(float value, float fraction) => value * MathF.Max(Floor, 1 + fraction);
        ShipPhysics Physics(ShipPhysics physics) => physics with
        {
            ForwardThrust = Scale(physics.ForwardThrust, Sum(e => e.ForwardThrust)),
            // Engine upgrades raise reverse thrust too: the original reverse value is "same as forward" (BackwardThrust -1, owner
            // decision 2026-11-02), so it follows the upgraded forward thrust.
            ReverseThrust = Scale(physics.ReverseThrust, Sum(e => e.ForwardThrust)),
            MaximumSpeed = Scale(physics.MaximumSpeed, Sum(e => e.MaximumSpeed)),
            RotationSpeed = Scale(physics.RotationSpeed, Sum(e => e.RotationSpeed)),
        };
        // The Shift boost scales with the same thrust and speed fractions, so an upgraded ship never boosts slower or weaker than
        // it flies (review R1-2 on #120). Which upgrades the original applied to hyper only (AfterBurners) is RT-F048, still NEW.
        ShipHyper? Hyper(ShipHyper? hyper) => hyper is null ? null : hyper with
        {
            ForwardThrust = Scale(hyper.ForwardThrust, Sum(e => e.ForwardThrust)),
            ReverseThrust = Scale(hyper.ReverseThrust, Sum(e => e.ForwardThrust)),
            MaximumSpeed = Scale(hyper.MaximumSpeed, Sum(e => e.MaximumSpeed)),
        };
        return ship with
        {
            Physics = Physics(ship.Physics),
            Hyper = Hyper(ship.Hyper),
            // Dock and trade-zone flight scales with the same fractions (NEW, like every upgrade effect), so an upgrade helps
            // everywhere and a dock never lets an upgraded ship fly faster than it does in open space.
            Terrains = ship.Terrains?.Select(t => t with { Physics = Physics(t.Physics), Hyper = Hyper(t.Hyper) }).ToList(),
            Reactor = ship.Reactor with
            {
                MaximumEnergy = Scale(ship.Reactor.MaximumEnergy, Sum(e => e.MaximumEnergy)),
                EnergyRegenerationPerSecond = Scale(ship.Reactor.EnergyRegenerationPerSecond, Sum(e => e.EnergyRegeneration)),
            },
            Defense = ship.Defense with { MaximumHull = ship.Defense.MaximumHull + Sum(e => e.MaximumHull) },
        };
    }
}
