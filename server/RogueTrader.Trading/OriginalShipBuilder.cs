using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using RogueTrader.GameData;
using RogueTrader.OriginalData;

namespace RogueTrader.Trading;

/// <summary>
/// Derives the twelve original player hulls (RPG rows 1-12 and their VEH Car rows) as flyable ship definitions, the ship
/// roster and the hull exchanges a station sells (ITM rows 181-192, type 16), for owned ships (Steve, Nov 3 2026).
/// Taken from the originals: name, flight (<see cref="OriginalShipPhysics"/>, G10's emulator reading), hold cap (VEH f[48]
/// StopWeight, read as a hard cap as for the Interceptor), hull points (VEH f[6], raw), the buy price (ITM f[8]) and the
/// same-race requirement (ITM f[5], RPG skill ids), radius (f[35]), energy and recharge (f[94..95]), energy as the shield, the
/// Shift boost (terrain-0 hyper fields and f[373]), dock and trade-zone flight (terrain blocks 9, 10, 15) and the starting weapon (f[129]). Every remaining assumption is listed in each
/// <see cref="ShipDefinition.NewValues"/>. Regenerate with RT_WRITE_DERIVED_SHIPS=1; never hand-edit the outputs.
/// </summary>
public static class OriginalShipBuilder
{
    /// <summary>The NEW engineering fixture older saves fly; it stays on the roster so those saves keep their ship.</summary>
    public const string LegacyStarterHullId = "starter_interceptor";
    public const string LegacyStarterDefinition = "starter_interceptor.json";
    /// <summary>The Asp: the hull of RPG row 1, which the original gives a new pilot.</summary>
    public const int StarterVehicleId = 2002;
    public const int HullExchangeRecordType = 16;

    /// <summary>
    /// Owner's rule pending (the original data gives the boost's cost per second but not whether recharge keeps running): energy
    /// stops recharging while the Shift boost is engaged, so every hull pays its full HyperEnergyCost.
    /// </summary>
    public const bool HyperRechargeWhileEngaged = false;

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string HullId(OriginalHull hull) => hull.VehicleId.ToString(CultureInfo.InvariantCulture);

    /// <summary>File name in data/ships for one hull, for example <c>original_rip_pir.json</c>.</summary>
    public static string FileName(OriginalHull hull) =>
        "original_" + new string(hull.Name.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_').ToArray()) + ".json";

    public static ShipDefinition Ship(OriginalHull hull, OriginalItemCatalog items)
    {
        ArgumentNullException.ThrowIfNull(hull);
        ArgumentNullException.ThrowIfNull(items);
        var rows = $"RPG/Rogue.rpg row {hull.RpgRowNumber} (skill {hull.SkillId}), VEH/Rogue.veh row {hull.VehRowNumber} (vehicle {hull.VehicleId})";
        var weapon = OriginalWeaponBuilder.StartingWeapon(hull, items);
        var flight = EmulatorFlightReading.From(hull);
        return new ShipDefinition
        {
            Id = HullId(hull),
            Name = hull.Name,
            EvidenceClassification = "ORIGINAL+NEW",
            Source = $"Derived by OriginalShipBuilder from {rows}, category '{hull.Category}'. Flight: OriginalShipPhysics (G10 emulator reading, mass 100 is a NEW scale). "
                + $"Hull points: VEH f[6]={hull.HullPoints.Value}, raw. Radius: VEH f[{VehFields.PhysicalRadius}] PhysicalRadius={hull.PhysicalRadius.Value} px. "
                + $"Energy: VEH f[94] EnergyMax={hull.EnergyMax.Value}, f[95] EnergyRate={hull.EnergyRate.Value} (tenths per second, OriginalEnergyScale); f[96] EnergyStart={hull.EnergyMin.Value} equals the maximum. "
                + "Shield: the energy itself (CFG EnergyShieldMode=2, NWS/Rogue.nws rows 39-41 and 131). "
                + $"Starting weapon: VEH f[{OriginalStartingLoadout.EventField}] '{hull.VehRecord.Value(OriginalStartingLoadout.EventField)}' (ITM id {weapon.Id}).",
            NewValues =
            [
                "Hull points used raw (f[6]); weapon hull damage reads ITM thousandths, so a Plasma Cannon hit takes 5 of the Asp's 50 (unvalidated in play).",
                $"Energy recharge {OriginalEnergyScale.PerSecond(hull.EnergyRate.Value)}/s and boost cost {OriginalEnergyScale.PerSecond(hull.HyperEnergyCost.Value)}/s read f[95] and f[373] as tenths per second (the recharge items' 'kj/s' wording); "
                    + "recharge pausing while the boost is engaged is the owner's rule, not original data.",
                "Boost thrust and top speed (HyperThrust, HyperTopSpeed, BackwardHyperThrust -1 = HyperThrust) use the RollThrust/RollTopSpeed units; no emulator code reads them. Releasing Shift clamps the ship back to its normal top speed.",
                "Dock and trade-zone flight (terrains 9, 10, 15) uses each terrain's own VEH block in the open-space units; the emulator's bot looks the block up but keeps flying terrain 0, so no emulator path applies it. "
                    + "Friction is the bot's formula at the 60 Hz step. Terrains whose block is all zero (1-8, 11-14, unused by the Rogue map) fly the open-space values.",
            ],
            Radius = hull.PhysicalRadius.Value,
            Physics = OriginalShipPhysics.From(hull),
            Hyper = Hyper(flight),
            Terrains = Terrains(hull),
            WeaponId = OriginalWeaponBuilder.WeaponId(weapon),
            Defense = new ShipDefense { MaximumHull = hull.HullPoints.Value, MaximumShield = 0, EnergyShield = true },
            Reactor = new ShipReactor
            {
                MaximumEnergy = hull.EnergyMax.Value,
                EnergyRegenerationPerSecond = OriginalEnergyScale.PerSecond(hull.EnergyRate.Value),
            },
            Cargo = new ShipCargo
            {
                Capacity = hull.StopWeight.Value,
                Source = $"ORIGINAL: VEH/Rogue.veh {hull.Name} (vehicle {hull.VehicleId}) f[48] StopWeight; NEW: read as the hold's hard weight cap.",
            },
        };
    }

    // The boost one terrain block gives, or null when its HyperTopSpeed is 0 (the boost does not work there).
    private static ShipHyper? Hyper(EmulatorFlightReading flight)
    {
        var hull = flight.Hull;
        var block = flight.Block;
        if (block.HyperTopSpeed.Value <= 0) return null;
        return new ShipHyper
        {
            ForwardThrust = (float)(flight.HyperForwardAccelerationPixelsPerSecondSquared * OriginalShipPhysics.Mass),
            ReverseThrust = (float)(flight.HyperReverseAccelerationPixelsPerSecondSquared * OriginalShipPhysics.Mass),
            MaximumSpeed = (float)flight.HyperTopSpeedPixelsPerSecond,
            EnergyPerSecond = OriginalEnergyScale.PerSecond(hull.HyperEnergyCost.Value),
            RechargeWhileEngaged = HyperRechargeWhileEngaged,
            Source = $"ORIGINAL: VEH/Rogue.veh {hull.Name} terrain {block.TerrainIndex} HyperThrust f[{block.HyperThrust.FieldIndex}]={block.HyperThrust.Value}, "
                + $"BackwardHyperThrust f[{block.BackwardHyperThrust.FieldIndex}]={block.BackwardHyperThrust.Value}, HyperTopSpeed f[{block.HyperTopSpeed.FieldIndex}]={block.HyperTopSpeed.Value}, "
                + $"HyperEnergyCost f[{VehFields.HyperEnergyCost}]={hull.HyperEnergyCost.Value}.",
        };
    }

    /// <summary>
    /// F12: the hull's flight in every terrain other than open space whose VEH block holds data (RollAllowed..HyperThrust not all
    /// zero; for Rogue that is the Repair Docks 9, the SpaceDocks 10 and the Trading Posts / refinery zone 15). Values go through the
    /// same emulator unit chain as terrain 0. RollAllowed (1 in these terrains, 0 in open space) is read by no emulator code and is
    /// not applied.
    /// </summary>
    public static IReadOnlyList<ShipTerrainFlight> Terrains(OriginalHull hull)
    {
        ArgumentNullException.ThrowIfNull(hull);
        return OriginalFlightProfile.From(hull).ActiveTerrainIndexes.Where(t => t != 0).Select(t =>
        {
            var flight = EmulatorFlightReading.From(hull, t);
            var b = flight.Block;
            return new ShipTerrainFlight
            {
                Terrain = t,
                Physics = OriginalShipPhysics.From(hull, t),
                FrictionPerTenMilliseconds = (float)flight.FrictionLossPerTenMilliseconds,
                Hyper = Hyper(flight),
                Source = $"ORIGINAL: VEH/Rogue.veh {hull.Name} terrain {t} f[{b.FirstFieldIndex}..{b.FirstFieldIndex + VehFields.TerrainBlockSize - 1}]: "
                    + $"RollFriction {b.RollFriction.Value}, RollThrust {b.RollThrust.Value}, RollRotate {b.RollRotate.Value}, RollTopSpeed {b.RollTopSpeed.Value}, "
                    + $"HyperTopSpeed {b.HyperTopSpeed.Value}, BackwardThrust {b.BackwardThrust.Value} (-1 = RollThrust); RECONSTRUCTED units (G10 F12).",
            };
        }).ToList();
    }

    /// <summary>The Asp is the starter; the Interceptor stays listed (last) so saves that own it keep flying it.</summary>
    public static ShipRoster Roster(OriginalHullCatalog hulls, OriginalItemCatalog items)
    {
        ArgumentNullException.ThrowIfNull(hulls);
        ArgumentNullException.ThrowIfNull(items);
        return new ShipRoster
        {
            StarterHullId = StarterVehicleId.ToString(CultureInfo.InvariantCulture),
            Ships = hulls.Hulls.Select(h => new ShipRosterEntry(HullId(h), FileName(h)))
                .Append(new ShipRosterEntry(LegacyStarterHullId, LegacyStarterDefinition)).ToList(),
            Weapons = OriginalWeaponBuilder.StartingWeapons(hulls, items)
                .Select(w => new WeaponRosterEntry(OriginalWeaponBuilder.WeaponId(w), OriginalWeaponBuilder.FileName(w)))
                .Append(new WeaponRosterEntry(ShipRoster.DefaultWeaponId, "plasma_cannon.json")).ToList(),
        };
    }

    /// <summary>
    /// The twelve type-16 hull exchanges as hulls a station sells, at the raw ITM buy price, requiring one of the same-race
    /// hulls ITM f[5] names (RPG skill ids). The Interceptor flies with the Asp's values, so it counts as a Terran hull (NEW).
    /// </summary>
    public static IReadOnlyList<StationShip> StationShips(OriginalHullCatalog hulls, OriginalItemCatalog items)
    {
        ArgumentNullException.ThrowIfNull(hulls);
        ArgumentNullException.ThrowIfNull(items);
        var bySkill = hulls.Hulls.ToDictionary(h => h.SkillId);
        var asp = StarterVehicleId.ToString(CultureInfo.InvariantCulture);
        return items.Items.Where(i => i.RecordType == HullExchangeRecordType && i.Category.StartsWith("Ship Hulls", StringComparison.Ordinal))
            .Select(i =>
            {
                var hull = hulls.ByName(i.Name);
                var requires = i.Record.Value(5).Split('|').Select(s => HullId(bySkill[int.Parse(s, CultureInfo.InvariantCulture)])).ToList();
                if (hull.Category == hulls.Hulls.Single(h => HullId(h) == asp).Category) requires.Add(LegacyStarterHullId);
                return new StationShip(HullId(hull), hull.Name, i.Buy.Value,
                    $"ITM/Rogue_mp.itm row {i.RowNumber} (type 16, id {i.Id}) f[8]={i.Buy.Value}, f[5]=\"{i.Record.Value(5)}\"")
                { RequiresOneOf = requires };
            }).ToList();
    }

    public static string ToJson(ShipDefinition ship) => JsonSerializer.Serialize(ship, Json).ReplaceLineEndings("\n") + "\n";
    public static string ToJson(ShipRoster roster) => JsonSerializer.Serialize(roster, Json).ReplaceLineEndings("\n") + "\n";
}
