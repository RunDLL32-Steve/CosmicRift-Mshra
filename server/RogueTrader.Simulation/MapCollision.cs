using System.Numerics;

namespace RogueTrader.Simulation;

/// <summary>
/// Ship and projectile contact with the blocked tiles of a <see cref="SectorMap"/>, following the InfantryOnline emulator's bot
/// movement (<c>MovementController.updateState</c> at bb97579, RECONSTRUCTED): the ship's centre point is tested against the
/// tile it moves into; on a blocked tile each velocity component whose tile index changed is reversed, the whole velocity is
/// scaled by the hull's bounce (VEH BouncePercent / 1000) and the move is taken again from the old position. The emulator's
/// 0.7 vertical factor is an isometric screen detail of its renderer and is not applied to this top-down view (NEW).
/// </summary>
public static class MapCollision
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
    /// Where a straight move first enters a blocked tile, as a fraction of the move (review R1-3). The move walks the tile grid
    /// exactly (Amanatides and Woo), so no blocked tile it crosses is ever skipped, however short the cut through it. A move
    /// that passes exactly through a tile corner counts as entering every tile that meets there, so a shot cannot slip
    /// diagonally between two blocked tiles. A move that starts on a blocked tile, or ends at a non-finite point, hits at once.
    /// </summary>
    public static bool Sweep(SectorMap map, Vector2 start, Vector2 end, out float fraction)
    {
        ArgumentNullException.ThrowIfNull(map);
        fraction = 0;
        if (map.IsBlocked(start) || !float.IsFinite(end.X) || !float.IsFinite(end.Y)) return true;
        const float size = SectorMap.TileSize;
        var (x, y) = SectorMap.TileOf(start);
        var d = end - start;
        int stepX = Math.Sign(d.X), stepY = Math.Sign(d.Y);
        var tMaxX = stepX == 0 ? float.PositiveInfinity : ((stepX > 0 ? (x + 1) * size : x * size) - start.X) / d.X;
        var tMaxY = stepY == 0 ? float.PositiveInfinity : ((stepY > 0 ? (y + 1) * size : y * size) - start.Y) / d.Y;
        var tDeltaX = stepX == 0 ? float.PositiveInfinity : size / MathF.Abs(d.X);
        var tDeltaY = stepY == 0 ? float.PositiveInfinity : size / MathF.Abs(d.Y);
        while (true)
        {
            var t = MathF.Min(tMaxX, tMaxY);
            if (!(t <= 1)) return false; // the move ends inside the current (open) tile
            bool blocked;
            if (tMaxX < tMaxY)
            {
                x += stepX;
                tMaxX += tDeltaX;
                blocked = map.IsBlockedTile(x, y);
            }
            else if (tMaxY < tMaxX)
            {
                y += stepY;
                tMaxY += tDeltaY;
                blocked = map.IsBlockedTile(x, y);
            }
            else
            {
                // Exactly through a corner: the two side tiles and the diagonal one all meet the move here.
                blocked = map.IsBlockedTile(x + stepX, y) || map.IsBlockedTile(x, y + stepY) || map.IsBlockedTile(x + stepX, y + stepY);
                x += stepX;
                y += stepY;
                tMaxX += tDeltaX;
                tMaxY += tDeltaY;
            }
            if (blocked)
            {
                fraction = Math.Clamp(t, 0, 1);
                return true;
            }
        }
    }
}
