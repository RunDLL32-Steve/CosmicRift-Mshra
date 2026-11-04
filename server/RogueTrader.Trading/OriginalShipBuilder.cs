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
/// same-race requirement (ITM f[5], RPG skill ids). Everything else is a placeholder listed in each
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

    // ---- Placeholders: no decoded original value yet (copied from the NEW Interceptor so every hull handles alike) -----
    private const float PlaceholderRadius = 14;
    private const float PlaceholderShield = 50;
    private const float PlaceholderEnergy = 100;
    private const float PlaceholderEnergyRegeneration = 20;

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

    public static ShipDefinition Ship(OriginalHull hull)
    {
        ArgumentNullException.ThrowIfNull(hull);
        var rows = $"RPG/Rogue.rpg row {hull.RpgRowNumber} (skill {hull.SkillId}), VEH/Rogue.veh row {hull.VehRowNumber} (vehicle {hull.VehicleId})";
        return new ShipDefinition
        {
            Id = HullId(hull),
            Name = hull.Name,
            EvidenceClassification = "ORIGINAL+NEW",
            Source = $"Derived by OriginalShipBuilder from {rows}, category '{hull.Category}'. Flight: OriginalShipPhysics (G10 emulator reading, mass 100 is a NEW scale). Hull points: VEH f[6]={hull.HullPoints.Value}, raw.",
            NewValues =
            [
                $"Radius {PlaceholderRadius} (VEH f[{OriginalCollisionFields.PhysicalRadiusCandidateField}]={hull.VehRecord.Value(OriginalCollisionFields.PhysicalRadiusCandidateField)} is only a candidate radius; its meaning is unvalidated).",
                $"Shield {PlaceholderShield} (no original shield field identified).",
                $"Energy {PlaceholderEnergy}, regeneration {PlaceholderEnergyRegeneration}/s (raw VEH f[94..96] = {hull.EnergyMax.Value}/{hull.EnergyRate.Value}/{hull.EnergyMin.Value}; units and timing undecoded).",
                $"Starting weapon: every hull fires the Plasma Cannon for now (original VEH f[129] '{hull.VehRecord.Value(129)}').",
                "Hull points used raw (f[6]); the scale against NEW weapon damage and repair amounts is unvalidated.",
            ],
            Radius = PlaceholderRadius,
            Physics = OriginalShipPhysics.From(hull),
            Defense = new ShipDefense { MaximumHull = hull.HullPoints.Value, MaximumShield = PlaceholderShield },
            Reactor = new ShipReactor { MaximumEnergy = PlaceholderEnergy, EnergyRegenerationPerSecond = PlaceholderEnergyRegeneration },
            Cargo = new ShipCargo
            {
                Capacity = hull.StopWeight.Value,
                Source = $"ORIGINAL: VEH/Rogue.veh {hull.Name} (vehicle {hull.VehicleId}) f[48] StopWeight; NEW: read as the hold's hard weight cap.",
            },
        };
    }

    /// <summary>The Asp is the starter; the Interceptor stays listed (last) so saves that own it keep flying it.</summary>
    public static ShipRoster Roster(OriginalHullCatalog hulls)
    {
        ArgumentNullException.ThrowIfNull(hulls);
        return new ShipRoster
        {
            StarterHullId = StarterVehicleId.ToString(CultureInfo.InvariantCulture),
            Ships = hulls.Hulls.Select(h => new ShipRosterEntry(HullId(h), FileName(h)))
                .Append(new ShipRosterEntry(LegacyStarterHullId, LegacyStarterDefinition)).ToList(),
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
