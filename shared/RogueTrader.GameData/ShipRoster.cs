using System.Text.Json;

namespace RogueTrader.GameData;

/// <summary>One flyable hull: the profile hull id and the ship definition file (relative to <c>data/ships/</c>) that flies it.</summary>
public sealed record ShipRosterEntry(string HullId, string Definition);

/// <summary>One weapon a roster hull starts with: the weapon id and its definition file (relative to <c>data/weapons/</c>).</summary>
public sealed record WeaponRosterEntry(string WeaponId, string Definition);

/// <summary>
/// The hulls the client can fly (owned ships, Steve, Nov 3 2026), read from <c>data/ships/roster.json</c>. A player owns at most
/// one of each; <see cref="StarterHullId"/> is the hull a new profile starts with. A hull is listed here only once its ship
/// definition exists, so an owned hull without one is shown in the picker but cannot be flown yet.
/// </summary>
public sealed class ShipRoster
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public string StarterHullId { get; init; } = "";
    public IReadOnlyList<ShipRosterEntry> Ships { get; init; } = [];
    /// <summary>
    /// The weapon definitions the hulls start with. A ship definition names its weapon by <see cref="ShipDefinition.WeaponId"/>;
    /// one with none flies <see cref="DefaultWeaponId"/>.
    /// </summary>
    public IReadOnlyList<WeaponRosterEntry> Weapons { get; init; } = [];

    /// <summary>The NEW Plasma cannon fixture, flown by a ship definition that names no weapon (the legacy Interceptor).</summary>
    public const string DefaultWeaponId = "plasma_cannon";

    /// <summary>The weapon definition file (in data/weapons) of the weapon <paramref name="ship"/> starts with.</summary>
    public string WeaponDefinitionFor(ShipDefinition ship)
    {
        ArgumentNullException.ThrowIfNull(ship);
        var id = ship.WeaponId ?? DefaultWeaponId;
        return Weapons.FirstOrDefault(w => w.WeaponId == id)?.Definition
            ?? throw new ArgumentException($"Ship '{ship.Id}' starts with weapon '{id}', which the roster does not list.");
    }

    public static ShipRoster FromJson(string json)
    {
        var roster = JsonSerializer.Deserialize<ShipRoster>(json, JsonOptions) ?? throw new JsonException("Empty ship roster.");
        roster.Validate();
        return roster;
    }

    public ShipRosterEntry? Find(string? hullId) => hullId is null ? null : Ships.FirstOrDefault(s => s.HullId == hullId);

    private void Validate()
    {
        if (Ships is null || Ships.Count == 0) throw new ArgumentException("The ship roster lists no ships.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ship in Ships)
        {
            if (ship is null || string.IsNullOrEmpty(ship.HullId) || string.IsNullOrEmpty(ship.Definition))
                throw new ArgumentException("A roster ship needs a hull id and a definition file.");
            if (!seen.Add(ship.HullId)) throw new ArgumentException($"Hull '{ship.HullId}' is listed twice: a player owns one of each.");
            if (ship.Definition.Contains('/') || ship.Definition.Contains('\\') || ship.Definition.Contains("..", StringComparison.Ordinal)
                || !ship.Definition.EndsWith(".json", StringComparison.Ordinal))
                throw new ArgumentException($"Definition '{ship.Definition}' must be a .json file name in data/ships.");
        }
        if (!seen.Contains(StarterHullId)) throw new ArgumentException($"The starter hull '{StarterHullId}' is not on the roster.");
        if (Weapons is null) throw new ArgumentException("The roster's weapon list is missing.");
        var weapons = new HashSet<string>(StringComparer.Ordinal);
        foreach (var weapon in Weapons)
        {
            if (weapon is null || string.IsNullOrEmpty(weapon.WeaponId) || string.IsNullOrEmpty(weapon.Definition))
                throw new ArgumentException("A roster weapon needs a weapon id and a definition file.");
            if (!weapons.Add(weapon.WeaponId)) throw new ArgumentException($"Weapon '{weapon.WeaponId}' is listed twice.");
            if (weapon.Definition.Contains('/') || weapon.Definition.Contains('\\') || weapon.Definition.Contains("..", StringComparison.Ordinal)
                || !weapon.Definition.EndsWith(".json", StringComparison.Ordinal))
                throw new ArgumentException($"Definition '{weapon.Definition}' must be a .json file name in data/weapons.");
        }
    }
}
