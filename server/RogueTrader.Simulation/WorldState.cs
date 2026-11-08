using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

public sealed class ShipState
{
    public string Id { get; }
    public ShipDefinition Definition { get; internal set; }
    public WeaponDefinition Weapon { get; }
    public Vector2 Position { get; internal set; }
    public Vector2 Velocity { get; internal set; }
    public float Heading { get; internal set; }
    public float Radius => Definition.Radius;
    public float Hull { get; internal set; }
    public float Shield { get; internal set; }
    public float Energy { get; internal set; }
    public bool IsDestroyed => Hull <= 0;
    /// <summary>True when the last motion step thrust with the hull's Shift boost (and paid its energy).</summary>
    public bool IsHyperEngaged { get; internal set; }
    /// <summary>
    /// What protects the hull: energy for a ship whose energy is its shield (<see cref="ShipDefense.EnergyShield"/>), otherwise the
    /// separate shield pool.
    /// </summary>
    public float ShieldLeft => Definition.Defense.EnergyShield ? Energy : Shield;
    public string? LastDamagedByShipId { get; internal set; }
    public string? DestroyedByShipId { get; internal set; }
    public string? LastDamageWeaponId { get; internal set; }
    internal long NextFireTick { get; set; }

    /// <summary>Back to full hull, shield and energy with no damage history (target group respawn).</summary>
    internal void Revive(Vector2 position, float heading)
    {
        Position = position;
        Heading = heading;
        Velocity = Vector2.Zero;
        Hull = Definition.Defense.MaximumHull;
        Shield = Definition.Defense.MaximumShield;
        Energy = Definition.Reactor.MaximumEnergy;
        LastDamagedByShipId = DestroyedByShipId = LastDamageWeaponId = null;
    }

    internal ShipState(string id, ShipDefinition definition, WeaponDefinition weapon, Vector2 position, float heading)
    {
        Id = id;
        Definition = definition;
        Weapon = weapon;
        Position = position;
        Heading = heading;
        Hull = definition.Defense.MaximumHull;
        Shield = definition.Defense.MaximumShield;
        Energy = definition.Reactor.MaximumEnergy;
    }
}

/// <summary>One target destruction: which target, which ship fired the destroying projectile, and the tick it happened on.</summary>
/// <param name="Wave">Which respawn of the target group the target belonged to (0 for the first group).</param>
public sealed record TargetKill(string TargetId, string KillerShipId, long Tick, int Wave = 0);

public sealed class AsteroidState
{
    public string Id { get; }
    public Vector2 Position { get; }
    public float Radius { get; }

    public string? ResourceItemId { get; internal set; }
    public int ResourceQuantity { get; internal set; }

    internal AsteroidState(AsteroidDefinition definition)
    {
        Id = definition.Id;
        Position = new Vector2(definition.X, definition.Y);
        Radius = definition.Radius;
    }
}

public sealed class ProjectileState
{
    public long Id { get; }
    public string OwnerShipId { get; }
    public string WeaponId { get; }
    public Vector2 Position { get; internal set; }
    public Vector2 Velocity { get; }
    public float Radius { get; }
    public float Damage { get; }
    /// <summary>The weapon's shield drain per hit; null for a weapon under the NEW point-for-point absorption rule.</summary>
    public float? ShieldDrain { get; }
    public int AgeTicks { get; internal set; }
    public int LifetimeTicks { get; }

    internal ProjectileState(long id, string ownerId, WeaponDefinition weapon, Vector2 position, Vector2 velocity)
    {
        Id = id;
        OwnerShipId = ownerId;
        WeaponId = weapon.Id;
        Position = position;
        Velocity = velocity;
        Radius = weapon.ProjectileRadius;
        Damage = weapon.Damage;
        ShieldDrain = weapon.ShieldDrain;
        LifetimeTicks = Math.Max(1, (int)MathF.Ceiling(weapon.LifetimeSeconds * WorldSimulation.TickRate));
    }
}

/// <summary>A pile of one item in space. Quantity falls as the player collects it; the pile is removed at zero.</summary>
public sealed class CollectibleState
{
    public string Id { get; }
    public string ItemId { get; }
    public int Quantity { get; internal set; }
    public Vector2 Position { get; }
    public float Radius { get; }
    /// <summary>Mineral clock tick at which an unpicked drop vanishes; null for a pile that never expires.</summary>
    public long? ExpiresAt { get; internal set; }

    internal CollectibleState(CollectibleSpawn spawn)
    {
        Id = spawn.Id;
        ItemId = spawn.ItemId;
        Quantity = spawn.Quantity;
        Position = new Vector2(spawn.X, spawn.Y);
        Radius = spawn.Radius;
    }
}

