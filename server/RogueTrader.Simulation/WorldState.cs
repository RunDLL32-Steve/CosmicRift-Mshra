using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

public sealed class ShipState
{
    public string Id { get; }
    public ShipDefinition Definition { get; }
    public WeaponDefinition Weapon { get; }
    public Vector2 Position { get; internal set; }
    public Vector2 Velocity { get; internal set; }
    public float Heading { get; internal set; }
    public float Radius => Definition.Radius;
    public float Hull { get; internal set; }
    public float Shield { get; internal set; }
    public float Energy { get; internal set; }
    public bool IsDestroyed => Hull <= 0;
    public string? LastDamagedByShipId { get; internal set; }
    public string? DestroyedByShipId { get; internal set; }
    public string? LastDamageWeaponId { get; internal set; }
    internal long NextFireTick { get; set; }

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
    public int AgeTicks { get; internal set; }
    public int LifetimeTicks { get; }

    internal ProjectileState(long id, ShipState owner, Vector2 position, Vector2 velocity)
    {
        Id = id;
        OwnerShipId = owner.Id;
        WeaponId = owner.Weapon.Id;
        Position = position;
        Velocity = velocity;
        Radius = owner.Weapon.ProjectileRadius;
        Damage = owner.Weapon.Damage;
        LifetimeTicks = Math.Max(1, (int)MathF.Ceiling(owner.Weapon.LifetimeSeconds * WorldSimulation.TickRate));
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

    internal CollectibleState(CollectibleSpawn spawn)
    {
        Id = spawn.Id;
        ItemId = spawn.ItemId;
        Quantity = spawn.Quantity;
        Position = new Vector2(spawn.X, spawn.Y);
        Radius = spawn.Radius;
    }
}

