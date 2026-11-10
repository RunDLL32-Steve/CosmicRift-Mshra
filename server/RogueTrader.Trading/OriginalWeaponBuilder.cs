using System.Globalization;
using System.Text.Json;
using RogueTrader.GameData;
using RogueTrader.OriginalData;

namespace RogueTrader.Trading;

/// <summary>
/// The energy and damage scales of the original data, as the simulation reads them. Each one is tied to a pinned source line:
/// <list type="bullet">
/// <item>VEH f[94] EnergyMax and f[96] EnergyStart are whole energy units (1700, shown in game).</item>
/// <item>
/// ITM and CFG energy amounts are thousandths of a unit: CFG <c>TeamSwitchMinEnergy=500000</c> (rogue.cfg line 83) is the
/// "at least 500 units of energy" of NWS/Rogue.nws row 290, and the emulator divides that key by 1000 too
/// (<c>Commands.cs</c>, <c>teamSwitchMinEnergy / 1000</c>). So a Plasma Cannon shot (f[36] 60000) costs 60 energy, and its drain
/// (f[172] 266000) and hull damage (f[160] 5000) are 266 and 5.
/// </item>
/// <item>
/// VEH f[95] EnergyRate and f[373] HyperEnergyCost are tenths of a unit per second: the recharge items of the same engine's
/// item files say so in words, "Recharge I/II/III ... by 7/14/21 kj/s" for a recharge-rate field (f[34]) of 70/140/210
/// (ITM/chaosadvanced.itm rows 280, 271, 301). Reading the hull's EnergyRate and HyperEnergyCost in that unit is an assumption:
/// the Asp recharges 105 a second and its boost costs 110 a second.
/// </item>
/// </list>
/// </summary>
public static class OriginalEnergyScale
{
    public const float ItemUnitsPerEnergy = 1000;
    public const float RateUnitsPerEnergyPerSecond = 10;

    public static float ItemEnergy(int raw) => raw / ItemUnitsPerEnergy;
    public static float PerSecond(int raw) => raw / RateUnitsPerEnergyPerSecond;
}

/// <summary>
/// Field indexes of the damage part of a type-6 projectile row, named by the InfantryOnline emulator's
/// <c>Docs/Itm/Projectile.txt</c> (1-based elements 37, 161 and 173). Every weapon in Rogue_mp.itm carries one energy drain and
/// exactly one of the kinetic, explosive or electronic hull damages, which is NWS/Rogue.nws rows 131-133: "All weapons do Shield
/// Drain Damage" and "one of 3 types of Hull Point Damage: Kinetic, Explosive or Energy" (the emulator's "electronic").
/// </summary>
public static class ItmDamageFields
{
    /// <summary>f[36]: energy cost of one shot in terrain 0 (open space).</summary>
    public const int EnergyCostTerrain0 = 36;
    public const int KineticDamageInner = 152;
    public const int ExplosiveDamageInner = 156;
    public const int ElectronicDamageInner = 160;
    /// <summary>f[172]: the shield (energy) drain of one hit.</summary>
    public const int EnergyDamageInner = 172;
}

/// <summary>
/// Derives the starting weapons of the twelve original hulls (VEH f[129] <c>addinv=…</c>: the Plasma Cannon for Terran hulls, the
/// Bio Cannon for Skithzar and the D-Cannon for Grazi) as weapon definitions. Taken from the originals: name, ITM id, energy cost
/// per shot, fire delay, shield drain and hull damage. Regenerate with RT_WRITE_DERIVED_SHIPS=1; never hand-edit the outputs.
/// </summary>
public static class OriginalWeaponBuilder
{
    /// <summary>NEW values kept from the Plasma cannon fixture until G10 rows W2/W3 decide the projectile units.</summary>
    private static readonly WeaponDefinition Fixture = new();

    /// <summary>The weapon id a hull starts with: the ITM id of its <c>addinv</c> item.</summary>
    public static string WeaponId(OriginalItem item) => item.Id.ToString(CultureInfo.InvariantCulture);

    /// <summary>File name in data/weapons, for example <c>original_d_cannon.json</c>.</summary>
    public static string FileName(OriginalItem item) =>
        "original_" + new string(item.Name.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_').ToArray()) + ".json";

    /// <summary>The distinct starting weapons of the twelve hulls, in first-use order.</summary>
    public static IReadOnlyList<OriginalItem> StartingWeapons(OriginalHullCatalog hulls, OriginalItemCatalog items) =>
        OriginalStartingLoadout.All(hulls, items).Select(l => StartingWeapon(l)).DistinctBy(i => i.Id).ToList();

    /// <summary>The weapon <paramref name="hull"/> starts with.</summary>
    public static OriginalItem StartingWeapon(OriginalHull hull, OriginalItemCatalog items)
    {
        ArgumentNullException.ThrowIfNull(hull);
        ArgumentNullException.ThrowIfNull(items);
        var raw = hull.VehRecord.Value(OriginalStartingLoadout.EventField);
        var eq = raw.IndexOf('=');
        var verb = eq < 0 ? raw : raw[..eq];
        var name = eq < 0 ? "" : raw[(eq + 1)..];
        var matches = items.Items.Where(i => i.Name == name).ToList();
        return StartingWeapon(new OriginalStartingLoadout(hull, raw, verb, name, matches.Count == 1 ? matches[0] : null));
    }

    private static OriginalItem StartingWeapon(OriginalStartingLoadout loadout) =>
        loadout.Verb == OriginalStartingLoadout.AddInventoryVerb && loadout.Item is { } item
            ? item
            : throw new OriginalDataException($"{loadout.Hull.Name}: VEH f[{OriginalStartingLoadout.EventField}] '{loadout.Raw}' does not name one item to add.");

    public static WeaponDefinition Weapon(OriginalItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var projectile = OriginalProjectile.Of(item);
        var r = item.Record;
        var hullDamages = new[] { ItmDamageFields.KineticDamageInner, ItmDamageFields.ExplosiveDamageInner, ItmDamageFields.ElectronicDamageInner }
            .Select(f => (Field: f, Raw: r.Int(f).Value)).Where(d => d.Raw != 0).ToList();
        if (hullDamages.Count != 1)
            throw new OriginalDataException($"{item.Name} (ITM row {item.RowNumber}) carries {hullDamages.Count} hull damage types, not one.");
        var (hullField, hullRaw) = hullDamages[0];
        var energyCost = r.Int(ItmDamageFields.EnergyCostTerrain0).Value;
        var drain = r.Int(ItmDamageFields.EnergyDamageInner).Value;
        return new WeaponDefinition
        {
            Id = WeaponId(item),
            Name = item.Name,
            EvidenceClassification = "ORIGINAL+NEW",
            MuzzleVelocity = Fixture.MuzzleVelocity,
            ProjectileRadius = Fixture.ProjectileRadius,
            Damage = OriginalEnergyScale.ItemEnergy(hullRaw),
            ShieldDrain = OriginalEnergyScale.ItemEnergy(drain),
            DamageType = DamageTypeOf(hullField),
            CooldownSeconds = OriginalShipPhysics.FireIntervalSeconds(projectile),
            EnergyCost = OriginalEnergyScale.ItemEnergy(energyCost),
            LifetimeSeconds = Fixture.LifetimeSeconds,
            InheritsShipVelocity = Fixture.InheritsShipVelocity,
            Source = $"Derived by OriginalWeaponBuilder from ITM/Rogue_mp.itm row {item.RowNumber} (id {item.Id}, '{item.Category}'): "
                + $"f[{ItmDamageFields.EnergyCostTerrain0}] energy cost {energyCost}, f[{ItmProjectileFields.FireDelay}] fireDelay {projectile.FireDelay.Value} (10 ms units), "
                + $"f[{ItmDamageFields.EnergyDamageInner}] drain {drain}, f[{hullField}] {DamageTypeOf(hullField)} hull damage {hullRaw}; energy amounts in thousandths (OriginalEnergyScale).",
            NewValues =
            [
                $"Muzzle velocity {Fixture.MuzzleVelocity} px/s (f[{ItmProjectileFields.MuzzleVelocity}]={projectile.MuzzleVelocity.Value}; unit open, G10 W2).",
                $"Lifetime {Fixture.LifetimeSeconds} s (f[{ItmProjectileFields.AliveTime}]={projectile.AliveTime.Value}; unit open, G10 W3).",
                $"Projectile radius {Fixture.ProjectileRadius} (f[{ItmProjectileFields.ProximityRadius}] proximityRadius={projectile.ProximityRadius.Value}; meaning open).",
                $"Inherits the ship's whole velocity (f[{ItmProjectileFields.InheritedSpeed}]={projectile.InheritedSpeed.Value}; read as per mille, unvalidated).",
                "Drain and hull damage use the inner value at the point of impact (the radius and outer value are not applied).",
            ],
        };
    }

    /// <summary>The hull damage type a damage field carries (NWS row 133: Kinetic, Explosive or Energy; the emulator's Electronic is Energy).</summary>
    public static DamageType DamageTypeOf(int hullDamageField) => hullDamageField switch
    {
        ItmDamageFields.KineticDamageInner => DamageType.Kinetic,
        ItmDamageFields.ExplosiveDamageInner => DamageType.Explosive,
        ItmDamageFields.ElectronicDamageInner => DamageType.Energy,
        _ => throw new ArgumentOutOfRangeException(nameof(hullDamageField), hullDamageField, "Not a hull damage field."),
    };

    public static string ToJson(WeaponDefinition weapon) => JsonSerializer.Serialize(weapon, OriginalShipBuilder.Json).ReplaceLineEndings("\n") + "\n";
}
