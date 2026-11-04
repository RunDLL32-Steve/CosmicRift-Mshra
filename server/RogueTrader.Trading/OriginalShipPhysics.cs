using RogueTrader.GameData;
using RogueTrader.OriginalData;

namespace RogueTrader.Trading;

/// <summary>
/// Queue G10: the flight model of an original hull as the simulation's <see cref="ShipPhysics"/>, read through the
/// InfantryOnline emulator's unit chain (<see cref="EmulatorFlightReading"/>, terrain 0 = open space; RECONSTRUCTED). World units
/// are original pixels. Taken from the hull: top speed (RollTopSpeed), forward and reverse acceleration (RollThrust; BackwardThrust
/// −1 means "same as forward", owner decision 2026-11-02), turn rate (RollRotate) and the share of speed kept after a bounce
/// (BouncePercent). The simulation has no friction in open space, which matches RollFriction 0. NEW: the mass of 100 is only a
/// scale for thrust (acceleration = thrust / mass); the original hull data has no mass in this sense.
/// </summary>
public static class OriginalShipPhysics
{
    /// <summary>NEW scale factor: thrust is stored as acceleration × this mass.</summary>
    public const float Mass = 100;

    public static ShipPhysics From(OriginalHull hull)
    {
        var reading = EmulatorFlightReading.From(hull);
        return new ShipPhysics
        {
            Mass = Mass,
            ForwardThrust = (float)(reading.ForwardAccelerationPixelsPerSecondSquared * Mass),
            ReverseThrust = (float)(reading.ReverseAccelerationPixelsPerSecondSquared * Mass),
            StrafeThrust = (float)(reading.StrafeAccelerationPixelsPerSecondSquared * Mass),
            RotationSpeed = (float)(reading.TurnDegreesPerSecond * Math.PI / 180),
            MaximumSpeed = (float)reading.TopSpeedPixelsPerSecond,
            AsteroidRestitution = (float)reading.WallBounceRetained,
        };
    }

    /// <summary>Every original hull's physics, keyed by hull name (the twelve selectable hulls).</summary>
    public static IReadOnlyDictionary<string, ShipPhysics> All(OriginalHullCatalog hulls)
    {
        ArgumentNullException.ThrowIfNull(hulls);
        return hulls.Hulls.ToDictionary(h => h.Name, From);
    }

    /// <summary>
    /// Seconds between shots for a projectile's fireDelay, read as 10 ms units (the emulator's <c>fireDelay × 10</c> ms in
    /// <c>Vehicle.Computer.cs</c>; RECONSTRUCTED, one live path).
    /// </summary>
    public static float FireIntervalSeconds(OriginalProjectile projectile)
    {
        ArgumentNullException.ThrowIfNull(projectile);
        return projectile.FireDelay.Value * 10 / 1000f;
    }
}
