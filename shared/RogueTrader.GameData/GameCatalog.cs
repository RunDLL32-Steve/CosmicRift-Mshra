using System.Text.Json;

namespace RogueTrader.GameData;

/// <summary>Validated external configuration shared by the client and authoritative simulation.</summary>
public sealed class GameCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    public ShipDefinition Ship { get; }
    public WeaponDefinition Weapon { get; }
    public SectorDefinition Sector { get; }

    public GameCatalog(ShipDefinition ship, WeaponDefinition weapon, SectorDefinition sector)
    {
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentNullException.ThrowIfNull(weapon);
        ArgumentNullException.ThrowIfNull(sector);
        Ship = ship;
        Weapon = weapon;
        Sector = sector;
        Validate();
    }

    public static GameCatalog FromJson(string shipJson, string weaponJson, string sectorJson) => new(
        Deserialize<ShipDefinition>(shipJson),
        Deserialize<WeaponDefinition>(weaponJson),
        Deserialize<SectorDefinition>(sectorJson));

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new JsonException($"Empty {typeof(T).Name} definition.");

    private void Validate()
    {
        RequireId(Ship.Id, "ship");
        RequireId(Weapon.Id, "weapon");
        RequireId(Sector.Id, "sector");
        ArgumentNullException.ThrowIfNull(Ship.Physics);
        ArgumentNullException.ThrowIfNull(Ship.Defense);
        ArgumentNullException.ThrowIfNull(Ship.Reactor);
        ArgumentNullException.ThrowIfNull(Sector.PlayerSpawn);
        ArgumentNullException.ThrowIfNull(Sector.Targets);
        ArgumentNullException.ThrowIfNull(Sector.Asteroids);
        Positive(Ship.Radius, "ship radius");
        Positive(Ship.Physics.Mass, "mass");
        Nonnegative(Ship.Physics.ForwardThrust, "forward thrust");
        Nonnegative(Ship.Physics.ReverseThrust, "reverse thrust");
        Nonnegative(Ship.Physics.RotationSpeed, "rotation speed");
        Positive(Ship.Physics.MaximumSpeed, "maximum speed");
        Nonnegative(Ship.Physics.AsteroidRestitution, "restitution");
        if (Ship.Physics.AsteroidRestitution > 1)
            throw new ArgumentException("Asteroid restitution must be between zero and one.");
        Positive(Ship.Defense.MaximumHull, "hull");
        Nonnegative(Ship.Defense.MaximumShield, "shield");
        Nonnegative(Ship.Reactor.MaximumEnergy, "energy");
        Nonnegative(Ship.Reactor.EnergyRegenerationPerSecond, "energy regeneration");
        Positive(Weapon.MuzzleVelocity, "muzzle velocity");
        Positive(Weapon.ProjectileRadius, "projectile radius");
        Positive(Weapon.Damage, "damage");
        Positive(Weapon.CooldownSeconds, "cooldown");
        Nonnegative(Weapon.EnergyCost, "energy cost");
        Positive(Weapon.LifetimeSeconds, "projectile lifetime");
        // Finite individual JSON values can still overflow the arithmetic used by simulation.
        FiniteDerived(Ship.Physics.ForwardThrust / Ship.Physics.Mass, "forward acceleration");
        FiniteDerived(Ship.Physics.ReverseThrust / Ship.Physics.Mass, "reverse acceleration");
        FiniteSquared(Ship.Physics.MaximumSpeed, "maximum speed");
        FiniteSquared(Ship.Radius, "ship radius");
        FiniteSquared(Weapon.ProjectileRadius, "projectile radius");
        FiniteSquared(Weapon.MuzzleVelocity + (Weapon.InheritsShipVelocity ? Ship.Physics.MaximumSpeed : 0),
            "maximum projectile speed");
        Finite(Sector.PlayerSpawn.X, Sector.PlayerSpawn.Y, Sector.PlayerSpawn.Heading,
            Sector.PlayerSpawn.VelocityX, Sector.PlayerSpawn.VelocityY);
        var identifiers = new HashSet<string>(StringComparer.Ordinal) { "player" };
        foreach (var target in Sector.Targets)
        {
            ArgumentNullException.ThrowIfNull(target);
            RequireId(target.Id, "target");
            if (!identifiers.Add(target.Id))
                throw new ArgumentException($"Duplicate entity ID: {target.Id}.");
            Positive(target.Radius, "target radius");
            FiniteSquared(target.Radius + Weapon.ProjectileRadius, "projectile/target collision radius");
            Positive(target.Hull, "target hull");
            Nonnegative(target.Shield, "target shield");
            Finite(target.X, target.Y, target.Heading);
        }
        foreach (var asteroid in Sector.Asteroids)
        {
            ArgumentNullException.ThrowIfNull(asteroid);
            RequireId(asteroid.Id, "asteroid");
            if (!identifiers.Add(asteroid.Id))
                throw new ArgumentException($"Duplicate entity ID: {asteroid.Id}.");
            Positive(asteroid.Radius, "asteroid radius");
            FiniteSquared(asteroid.Radius + MathF.Max(Ship.Radius, Weapon.ProjectileRadius),
                "asteroid collision radius");
            Finite(asteroid.X, asteroid.Y);
        }
    }

    private static void RequireId(string id, string kind)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException($"The {kind} definition requires an ID.");
    }

    private static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0)
            throw new ArgumentException($"{name} must be finite and positive.");
    }

    private static void Nonnegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0)
            throw new ArgumentException($"{name} must be finite and nonnegative.");
    }

    private static void FiniteDerived(float value, string name)
    {
        if (!float.IsFinite(value))
            throw new ArgumentException($"Derived {name} must remain finite.");
    }

    private static void FiniteSquared(float value, string name) => FiniteDerived(value * value, $"{name} squared");
    private static void Finite(params float[] values)
    {
        if (values.Any(value => !float.IsFinite(value)))
            throw new ArgumentException("Coordinates, heading, and velocity must be finite.");
    }
}

