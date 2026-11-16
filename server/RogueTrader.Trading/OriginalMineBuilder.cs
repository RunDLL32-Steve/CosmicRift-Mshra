using System.Globalization;
using RogueTrader.GameData;
using RogueTrader.OriginalData;

namespace RogueTrader.Trading;

/// <summary>A space mine the home stores sell (RT-F038), with the rules of the mine it lays.</summary>
/// <param name="Id">The ITM id, which is also the cargo item each mine uses up (the mine is its own ammunition, f[31]).</param>
/// <param name="MaxCarried">Most of these one hold carries.</param>
public sealed record StationMunition(string Id, string Name, long UnitWeight, long Buy, long Sell, int MaxCarried, MineDefinition Mine);

/// <summary>
/// Derives the Plasma Mine (ITM/Rogue_mp.itm row 13, id 102, "Space Mines") as a <see cref="MineDefinition"/> and a store item.
/// Field names are the InfantryOnline emulator's <c>ItmInfo.Projectile.cs</c> (bb97579e) names for the same indexes; the energy
/// and damage scales are <see cref="OriginalEnergyScale"/>; time fields are read in the 10 ms units the emulator uses for fireDelay.
/// </summary>
public static class OriginalMineBuilder
{
    /// <summary>ITM id of the Plasma Mine.</summary>
    public const int PlasmaMineId = 102;
    public const string SpaceMinesCategory = "Space Mines";

    /// <summary>f[13], the item's maxAllowed (-10 on every mine row).</summary>
    public const int MaxAllowedField = 13;
    /// <summary>f[31] useAmmoID: the item each shot uses up (the mine's own id).</summary>
    public const int UseAmmoIdField = 31;

    /// <summary>The per-damage-type blast radius fields, each followed by its inner, outer and mode fields.</summary>
    private static readonly (int Radius, string Field)[] HullDamageRadii = [(151, "kinetic"), (155, "explosive"), (159, "electronic")];
    private const int EnergyDrainRadius = 171;
    private const int MaxLivePerTeamField = 132;

    /// <summary>The Plasma Mine of <paramref name="items"/>.</summary>
    public static StationMunition PlasmaMine(OriginalItemCatalog items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var item = items.Items.SingleOrDefault(i => i.Id == PlasmaMineId && i.Category == SpaceMinesCategory)
            ?? throw new OriginalDataException($"ITM id {PlasmaMineId} is not one '{SpaceMinesCategory}' item.");
        return Munition(item);
    }

    public static StationMunition Munition(OriginalItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var projectile = OriginalProjectile.Of(item);
        var r = item.Record;
        if (r.Int(UseAmmoIdField).Value != item.Id || projectile.AmmoUsedPerShot.Value != 1)
            throw new OriginalDataException($"{item.Name} (ITM row {item.RowNumber}) is not a mine that uses one of itself per drop.");
        if (projectile.MuzzleVelocity.Value != 0 || projectile.InheritedSpeed.Value != 0)
            throw new OriginalDataException($"{item.Name} (ITM row {item.RowNumber}) moves when laid; only still mines are supported.");
        if (projectile.ExplodeItem.Value != 0)
            throw new OriginalDataException($"{item.Name} (ITM row {item.RowNumber}) spawns a child effect (RT-F040), which mines do not support yet.");
        var hull = HullDamageRadii.Where(d => r.Int(d.Radius + 1).Value != 0).ToList();
        if (hull.Count != 1)
            throw new OriginalDataException($"{item.Name} (ITM row {item.RowNumber}) carries {hull.Count} hull damage types, not one.");
        var (hullRadiusField, fieldName) = hull[0];
        var damageType = OriginalWeaponBuilder.DamageTypeOf(hullRadiusField + 1);
        var hullRadius = r.Int(hullRadiusField).Value;
        var drainRadius = r.Int(EnergyDrainRadius).Value;
        if (hullRadius <= 0 || drainRadius != hullRadius)
            throw new OriginalDataException($"{item.Name} (ITM row {item.RowNumber}) has drain radius {drainRadius} and hull damage radius {hullRadius}; one shared blast radius is expected.");
        var maxAllowed = r.Int(MaxAllowedField).Value;
        var id = item.Id.ToString(CultureInfo.InvariantCulture);
        static float Time(int raw) => raw * 10 / 1000f;
        var mine = new MineDefinition
        {
            Id = id,
            Name = item.Name,
            ItemId = id,
            ProximityRadius = projectile.ProximityRadius.Value,
            BlastRadius = hullRadius,
            ShieldDrainInner = OriginalEnergyScale.ItemEnergy(r.Int(EnergyDrainRadius + 1).Value),
            ShieldDrainOuter = OriginalEnergyScale.ItemEnergy(r.Int(EnergyDrainRadius + 2).Value),
            DamageInner = OriginalEnergyScale.ItemEnergy(r.Int(hullRadiusField + 1).Value),
            DamageOuter = OriginalEnergyScale.ItemEnergy(r.Int(hullRadiusField + 2).Value),
            DamageType = damageType,
            ArmSeconds = Time(projectile.InactiveTime.Value),
            LifetimeSeconds = Time(projectile.AliveTime.Value),
            DropIntervalSeconds = OriginalShipPhysics.FireIntervalSeconds(projectile),
            MaxLivePerPlayer = projectile.MaxLivePerPlayer.Value,
            Source = $"Derived by OriginalMineBuilder from ITM/Rogue_mp.itm row {item.RowNumber} (id {item.Id}, '{item.Category}'): "
                + $"f[{ItmProjectileFields.ProximityRadius}] proximityRadius {projectile.ProximityRadius.Value}, "
                + $"f[{hullRadiusField}..{hullRadiusField + 2}] {fieldName} ({damageType}) damage radius/inner/outer {hullRadius}/{r.Int(hullRadiusField + 1).Value}/{r.Int(hullRadiusField + 2).Value}, "
                + $"f[{EnergyDrainRadius}..{EnergyDrainRadius + 2}] energy drain radius/inner/outer {drainRadius}/{r.Int(EnergyDrainRadius + 1).Value}/{r.Int(EnergyDrainRadius + 2).Value}, "
                + $"f[{ItmProjectileFields.InactiveTime}] inactiveTime {projectile.InactiveTime.Value}, f[{ItmProjectileFields.AliveTime}] aliveTime {projectile.AliveTime.Value}, "
                + $"f[{ItmProjectileFields.FireDelay}] fireDelay {projectile.FireDelay.Value}, f[{ItmProjectileFields.MaxLivePerPlayer}] maxLivePerPlayer {projectile.MaxLivePerPlayer.Value}; "
                + "energy amounts in thousandths (OriginalEnergyScale), times in 10 ms units.",
            NewValues =
            [
                $"Lifetime and arm time read f[{ItmProjectileFields.AliveTime}] aliveTime and f[{ItmProjectileFields.InactiveTime}] inactiveTime in the 10 ms units of fireDelay (G10 W3 reading; the unit is not proven).",
                "Radii are read as map pixels; the mine goes off when a ship's edge comes within the proximity radius, and its blast reaches ships whose centre is inside the blast radius.",
                "Drain and damage fall linearly from inner at the centre to outer at the blast radius (the raw damage mode fields are kept unread).",
                "A mine never goes off for, or hurts, the pilot who laid it; a mine whose life runs out vanishes without a blast.",
                $"Armour reduces the hull damage by its damage type as it does for cannon hits (RT-F041); f[{MaxLivePerTeamField}] maxLivePerTeam {r.Int(MaxLivePerTeamField).Value} is not applied (the sector has one pilot).",
            ],
        };
        mine.Validate();
        return new StationMunition(id, item.Name, item.Mass.Value, item.Buy.Value, item.Sell.Value, Math.Abs(maxAllowed), mine);
    }

    /// <summary>NEW-value notes for the station definition about how mines are sold and carried.</summary>
    public static string StationNote(StationMunition munition) =>
        $"{munition.Name}: sold at every home store for its raw buy f[8]={munition.Buy} and bought back for its raw sell f[15]={munition.Sell} "
        + $"(NEW: where the original sold mines is unknown); a hold carries at most {munition.MaxCarried}, the magnitude of f[{MaxAllowedField}] maxAllowed "
        + $"-{munition.MaxCarried} (NEW: the sign's meaning is open); every hull may carry it (f[5] '!%1' skill logic undecoded); mines are bought "
        + "items, so they stay with the pilot when the ship is destroyed (Steve, Nov 3 2026).";
}
