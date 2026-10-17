namespace RogueTrader.Trading;

/// <summary>
/// A trading station in the world. Position and radius are in simulation world units. The dock radius is a NEW/provisional
/// value: the original proximity rule (RT-F045 trading-post radius, shop eligibility) is unresolved.
/// </summary>
public sealed record StationSite
{
    public StationSite(string stationId, float x, float y, float dockRadius)
    {
        if (string.IsNullOrEmpty(stationId))
            throw new ArgumentException("A station needs an id.", nameof(stationId));
        if (!float.IsFinite(x) || !float.IsFinite(y))
            throw new ArgumentException("Station position must be finite.", nameof(x));
        if (!float.IsFinite(dockRadius) || dockRadius <= 0)
            throw new ArgumentException("Dock radius must be finite and positive.", nameof(dockRadius));
        StationId = stationId;
        X = x;
        Y = y;
        DockRadius = dockRadius;
    }

    public string StationId { get; }
    public float X { get; }
    public float Y { get; }
    /// <summary>Largest ship-centre to station-centre distance, in world units, at which the ship counts as docked (inclusive).</summary>
    public float DockRadius { get; }
}

/// <summary>Pure proximity predicate: a ship is docked when its centre lies within the dock radius of the station.</summary>
public static class Docking
{
    public static bool IsDocked(StationSite station, float shipX, float shipY)
    {
        ArgumentNullException.ThrowIfNull(station);
        if (!float.IsFinite(shipX) || !float.IsFinite(shipY))
            return false;
        // double arithmetic: float squares of large coordinates would lose the boundary.
        var dx = (double)shipX - station.X;
        var dy = (double)shipY - station.Y;
        var r = (double)station.DockRadius;
        return dx * dx + dy * dy <= r * r;
    }
}

/// <summary>Stations by id (ordinal, unique).</summary>
public sealed class StationDirectory
{
    private readonly Dictionary<string, StationSite> _stations = new(StringComparer.Ordinal);

    public StationDirectory(IEnumerable<StationSite> stations)
    {
        ArgumentNullException.ThrowIfNull(stations);
        foreach (var station in stations)
        {
            if (station is null)
                throw new ArgumentException("Null station.", nameof(stations));
            if (!_stations.TryAdd(station.StationId, station))
                throw new ArgumentException($"Duplicate station '{station.StationId}'.", nameof(stations));
        }
    }

    public IReadOnlyCollection<StationSite> Stations => _stations.Values;

    public bool TryGet(string stationId, out StationSite station) =>
        _stations.TryGetValue(stationId ?? string.Empty, out station!);

    /// <summary>False for an unknown station id or a ship outside the dock radius.</summary>
    public bool IsDocked(string stationId, float shipX, float shipY) =>
        TryGet(stationId, out var station) && Docking.IsDocked(station, shipX, shipY);
}
