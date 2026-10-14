using System.Numerics;

namespace RogueTrader.Simulation;

internal static class CircleCollision
{
    private const float Separation = 0.001f;

    // Segment/circle intersection includes the body's radius. Hits between ticks cannot tunnel.
    internal static bool Sweep(Vector2 start, Vector2 end, Vector2 center, float radius, out float fraction)
    {
        fraction = 0;
        var offset = start - center;
        var movement = end - start;
        var c = Vector2.Dot(offset, offset) - radius * radius;
        if (c <= 0)
            return true;
        var a = Vector2.Dot(movement, movement);
        var b = Vector2.Dot(offset, movement);
        if (a <= 1e-12f || b >= 0)
            return false;
        var discriminant = b * b - a * c;
        if (discriminant < 0)
            return false;
        fraction = (-b - MathF.Sqrt(discriminant)) / a;
        return fraction is >= 0 and <= 1;
    }

    internal static int MoveShip(ShipState ship, IReadOnlyList<AsteroidState> asteroids, float delta)
    {
        var collisions = 0;
        // Recover an overlapping spawn deterministically, including the coincident-center case.
        for (var pass = 0; pass < 4; pass++)
        {
            var adjusted = false;
            foreach (var asteroid in asteroids)
            {
                var offset = ship.Position - asteroid.Position;
                var distance = offset.Length();
                var radius = ship.Radius + asteroid.Radius;
                if (distance >= radius)
                    continue;
                var normal = distance > 1e-6f ? offset / distance : Vector2.UnitX;
                ship.Position = asteroid.Position + normal * (radius + Separation);
                if (Reflect(ship, normal))
                    collisions++;
                adjusted = true;
            }
            if (!adjusted)
                break;
        }

        var remaining = delta;
        for (var impact = 0; impact < 4 && remaining > 1e-7f; impact++)
        {
            var end = ship.Position + ship.Velocity * remaining;
            AsteroidState? hit = null;
            var earliest = float.PositiveInfinity;
            foreach (var asteroid in asteroids)
            {
                if (Sweep(ship.Position, end, asteroid.Position, ship.Radius + asteroid.Radius, out var fraction)
                    && fraction < earliest)
                {
                    hit = asteroid;
                    earliest = fraction;
                }
            }
            if (hit is null)
            {
                ship.Position = end;
                break;
            }
            ship.Position = Vector2.Lerp(ship.Position, end, earliest);
            var offset = ship.Position - hit.Position;
            var normal = offset.LengthSquared() > 1e-12f ? Vector2.Normalize(offset) : Vector2.UnitX;
            ship.Position = hit.Position + normal * (ship.Radius + hit.Radius + Separation);
            if (Reflect(ship, normal))
                collisions++;
            remaining *= 1 - earliest;
        }
        return collisions;
    }

    private static bool Reflect(ShipState ship, Vector2 normal)
    {
        var inwardSpeed = Vector2.Dot(ship.Velocity, normal);
        if (inwardSpeed >= 0)
            return false;
        ship.Velocity -= normal * ((1 + ship.Definition.Physics.AsteroidRestitution) * inwardSpeed);
        return true;
    }
}
