using System.Numerics;

namespace RogueTrader.Simulation;

/// <summary>
/// Ship and projectile contact with the blocked tiles of a <see cref="SectorMap"/>, following the InfantryOnline emulator's bot
/// movement (<c>MovementController.updateState</c> at bb97579, RECONSTRUCTED): the ship's centre point is tested against the
/// tile it moves into; on a blocked tile each velocity component whose tile index changed is reversed, the whole velocity is
/// scaled by the hull's bounce (VEH BouncePercent / 1000) and the move is taken again from the old position. The emulator's
/// 0.7 vertical factor is an isometric screen detail of its renderer and is not applied to this top-down view (NEW).
/// </summary>
internal static class MapCollision
{
    /// <summary>
    /// Checks the move from <paramref name="previous"/> to the ship's current position and bounces it off a wall. Returns true
    /// when it hit one. A ship already standing on a blocked tile (for example placed there by a save from an older map) may move
    /// freely until it is clear, so it can never be stuck.
    /// </summary>
    internal static bool MoveShip(ShipState ship, Vector2 previous, SectorMap map, float delta)
    {
        var next = ship.Position;
        if (!map.IsBlocked(next) || map.IsBlocked(previous)) return false;
        var (fromX, fromY) = SectorMap.TileOf(previous);
        var (toX, toY) = SectorMap.TileOf(next);
        var velocity = ship.Velocity;
        if (fromX != toX) velocity.X = -velocity.X;
        if (fromY != toY) velocity.Y = -velocity.Y;
        velocity *= ship.Definition.Physics.AsteroidRestitution;
        ship.Velocity = velocity;
        var bounced = previous + velocity * delta;
        ship.Position = map.IsBlocked(bounced) ? previous : bounced;
        return true;
    }

    /// <summary>
    /// The first point of a straight move that lies on a blocked tile, as a fraction of the move, sampled every half tile and
    /// at the end. A move that starts on a blocked tile hits at once.
    /// </summary>
    internal static bool Sweep(SectorMap map, Vector2 start, Vector2 end, out float fraction)
    {
        fraction = 0;
        if (map.IsBlocked(start)) return true;
        var length = Vector2.Distance(start, end);
        var steps = Math.Max(1, (int)MathF.Ceiling(length / (SectorMap.TileSize / 2f)));
        for (var i = 1; i <= steps; i++)
        {
            var t = (float)i / steps;
            if (!map.IsBlocked(Vector2.Lerp(start, end, t))) continue;
            fraction = t;
            return true;
        }
        return false;
    }
}
