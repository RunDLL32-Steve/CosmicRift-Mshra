using System.Text;

namespace RogueTrader.Simulation.Replay;

/// <summary>
/// Order-sensitive 64-bit FNV-1a digest of every authoritative field of a <see cref="WorldSimulation"/>.
/// Floats are hashed by bit pattern, so any single-ulp divergence changes the digest. Not cryptographic.
/// Equality is promised on the same runtime/hardware only, matching the simulation's own replay guarantee.
/// </summary>
public static class WorldStateHasher
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Hash(WorldSimulation world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var hash = Offset;
        hash = Add(hash, world.TickNumber);
        hash = Add(hash, world.DestroyedTargets);
        hash = Add(hash, world.AsteroidCollisions);
        hash = Add(hash, world.ShotsFired);
        hash = AddShip(hash, world.Player);
        hash = Add(hash, world.NextMineTick);
        hash = Add(hash, world.NextMiningPileId);
        hash = Add(hash, world.Asteroids.Count);
        foreach (var asteroid in world.Asteroids)
        {
            hash = Add(hash, asteroid.Id);
            hash = Add(hash, asteroid.ResourceItemId);
            hash = Add(hash, asteroid.ResourceQuantity);
        }
        hash = Add(hash, world.Targets.Count);
        foreach (var target in world.Targets)
            hash = AddShip(hash, target);
        hash = Add(hash, world.Projectiles.Count);
        foreach (var projectile in world.Projectiles)
        {
            hash = Add(hash, projectile.Id);
            hash = Add(hash, projectile.OwnerShipId);
            hash = Add(hash, projectile.WeaponId);
            hash = Add(hash, projectile.Position.X);
            hash = Add(hash, projectile.Position.Y);
            hash = Add(hash, projectile.Velocity.X);
            hash = Add(hash, projectile.Velocity.Y);
            hash = Add(hash, projectile.Radius);
            hash = Add(hash, projectile.Damage);
            hash = Add(hash, projectile.AgeTicks);
            hash = Add(hash, projectile.LifetimeTicks);
        }
        // Cargo contents and the piles still in space are authoritative since queue M2; Items is in ordinal id order.
        hash = Add(hash, world.Cargo.TotalWeight);
        hash = Add(hash, world.Cargo.Items.Count);
        foreach (var (itemId, quantity) in world.Cargo.Items)
        {
            hash = Add(hash, itemId);
            hash = Add(hash, quantity);
        }
        hash = Add(hash, world.Collectibles.Count);
        foreach (var pile in world.Collectibles)
        {
            hash = Add(hash, pile.Id);
            hash = Add(hash, pile.ItemId);
            hash = Add(hash, pile.Quantity);
            hash = Add(hash, pile.Position.X);
            hash = Add(hash, pile.Position.Y);
            hash = Add(hash, pile.Radius);
        }
        return hash;
    }

    private static ulong AddShip(ulong hash, ShipState ship)
    {
        hash = Add(hash, ship.Id);
        hash = Add(hash, ship.Position.X);
        hash = Add(hash, ship.Position.Y);
        hash = Add(hash, ship.Velocity.X);
        hash = Add(hash, ship.Velocity.Y);
        hash = Add(hash, ship.Heading);
        hash = Add(hash, ship.Hull);
        hash = Add(hash, ship.Shield);
        hash = Add(hash, ship.Energy);
        hash = Add(hash, ship.NextFireTick);
        hash = Add(hash, ship.LastDamagedByShipId);
        hash = Add(hash, ship.DestroyedByShipId);
        return Add(hash, ship.LastDamageWeaponId);
    }

    private static ulong Add(ulong hash, float value) => Add(hash, BitConverter.SingleToInt32Bits(value));

    private static ulong Add(ulong hash, int value) => Add(hash, (long)value);

    private static ulong Add(ulong hash, long value)
    {
        var bits = unchecked((ulong)value);
        for (var shift = 0; shift < 64; shift += 8)
            hash = (hash ^ ((bits >> shift) & 0xFF)) * Prime;
        return hash;
    }

    // Null is distinguished from the empty string by a length of -1.
    private static ulong Add(ulong hash, string? value)
    {
        if (value is null)
            return Add(hash, -1L);
        var bytes = Encoding.UTF8.GetBytes(value);
        hash = Add(hash, bytes.Length);
        foreach (var b in bytes)
            hash = (hash ^ b) * Prime;
        return hash;
    }
}

