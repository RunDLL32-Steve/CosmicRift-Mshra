using System.Numerics;
using RogueTrader.Persistence;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>Where a saved ship stands when it is flown again (review R1-2 on #118).</summary>
public static class ShipPlacement
{
    /// <summary>
    /// The saved position when it is in the coordinates of the sector being flown; otherwise <paramref name="spawn"/>. A ship
    /// saved in the old practice sector (<see cref="ShipCondition.LegacyPracticeSector"/>) and flown on the full-size map starts
    /// at the spawn: its position was in the old 1,400-unit coordinates and on the map would put it somewhere it never was. The
    /// decision is made per ship from its own saved version, so parked ships move whenever they are next flown, and a sector
    /// saved before a failed profile write cannot make an old position pass for a map position. Hull and heading always carry over.
    /// </summary>
    public static Vector2 Position(ShipCondition ship, SectorMap? map, Vector2 spawn)
    {
        ArgumentNullException.ThrowIfNull(ship);
        var onMap = map is not null;
        var savedOnMap = ship.CoordinateVersion >= SectorMap.CoordinateVersion;
        return onMap == savedOnMap ? new Vector2(ship.X, ship.Y) : spawn;
    }

    /// <summary>
    /// How long a saved ship has stood still at <paramref name="nowUtc"/>, in simulation ticks (the 60-second dock rule): the time
    /// since <see cref="ShipCondition.StillSinceUtc"/>, so the clock ran on while the pilot was away. A condition without the field
    /// (saved before schema 8) or one dated in the future reads as a ship that has just moved.
    /// </summary>
    public static long StillTicks(ShipCondition ship, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(ship);
        if (ship.StillSinceUtc is not { } since || since >= nowUtc) return 0;
        return (long)Math.Min(long.MaxValue / 2, Math.Floor((nowUtc - since).TotalSeconds * WorldSimulation.TickRate));
    }

    /// <summary>When a ship that has stood still for <paramref name="stillTicks"/> at <paramref name="nowUtc"/> last moved.</summary>
    public static DateTimeOffset StillSince(long stillTicks, DateTimeOffset nowUtc) =>
        nowUtc - TimeSpan.FromSeconds(Math.Max(0, stillTicks) / (double)WorldSimulation.TickRate);

    /// <summary>The coordinate version a ship saved now carries: map pixels on a map, the practice sector's otherwise.</summary>
    public static int CoordinateVersionFor(SectorMap? map) => map is null ? ShipCondition.LegacyPracticeSector : ShipCondition.MapPixels;
}
