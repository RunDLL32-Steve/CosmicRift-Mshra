namespace RogueTrader.Persistence;

/// <summary>
/// Saved ship condition (schema 3, queue M8 follow-up): hull and where the ship was left, in simulation world units and
/// radians. Which fields the original game retained across sessions is unrecoverable from the pinned package (RT-F068), so
/// retaining these is a NEW/provisional engineering choice. Velocity is not saved: a reloaded ship starts at rest.
/// <para>
/// Schema 7 (review R1-2): <see cref="CoordinateVersion"/> says which coordinates X and Y are in, per ship, so each owned ship
/// is moved onto the full-size map on its own, whenever it is next flown: <see cref="LegacyPracticeSector"/> for every
/// condition saved before schema 7 (the 1,400-unit practice sector), <see cref="MapPixels"/> for one saved on the map.
/// </para>
/// </summary>
public sealed record ShipCondition(float Hull, float X, float Y, float Heading, int CoordinateVersion = ShipCondition.MapPixels)
{
    /// <summary>Positions in the old 1,400-unit practice sector (every condition saved before schema 7).</summary>
    public const int LegacyPracticeSector = 0;

    /// <summary>Positions in pixels of the full-size Rogue map (the simulation's <c>SectorMap.CoordinateVersion</c>).</summary>
    public const int MapPixels = 1;

    public bool IsValid => float.IsFinite(Hull) && Hull >= 0 && float.IsFinite(X) && float.IsFinite(Y) && float.IsFinite(Heading)
        && CoordinateVersion is LegacyPracticeSector or MapPixels;
}
