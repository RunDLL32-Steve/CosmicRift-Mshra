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

    /// <summary>The coordinate version a ship saved now carries: map pixels on a map, the practice sector's otherwise.</summary>
    public static int CoordinateVersionFor(SectorMap? map) => map is null ? ShipCondition.LegacyPracticeSector : ShipCondition.MapPixels;
}
