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

    /// <summary>
    /// G10 W2: a projectile's muzzleVelocity in pixels per second. The emulator's lead-aim code passes
    /// <c>muzzleVelocity / 1000</c> to <c>computeLeadFireAngle</c> as "pixels per tick" (<c>Vehicle.Computer.cs:202</c>,
    /// <c>Bot/WeaponController.cs:197/205</c>), whose own comment reads "Muzzle velocity = thousandths of pixels per tick"
    /// (<c>Math.cs:229</c>; the integer division there drops the fraction, which the game does not copy), and it puts the target's velocity in the same unit (<c>Math.cs:216-218</c>) with a tick of 10 ms
    /// (<c>Math.cs:210</c>). That is the unit that gives the Asp its 325 px/s (G10 F1), so the projectile reading and the
    /// flight reading are one scale: raw / 1000 px per 10 ms = raw / 10 px/s. The Plasma Cannon's 2200 is 220 px/s, before the
    /// shooter's own velocity is added (W4). RECONSTRUCTED: three call sites and a comment, no emulator code moves a projectile.
    /// </summary>
    public static float MuzzleVelocityPixelsPerSecond(OriginalProjectile projectile)
    {
        ArgumentNullException.ThrowIfNull(projectile);
        if (projectile.MuzzleVelocity.Value <= 0)
            throw new OriginalDataException($"{projectile.Item.Name}: muzzleVelocity {projectile.MuzzleVelocity} is not a moving projectile.");
        return projectile.MuzzleVelocity.Value / MuzzleUnitsPerPixelPerSecond;
    }

    /// <summary>Raw muzzleVelocity units per px/s: thousandths of a pixel per 10 ms tick.</summary>
    public const float MuzzleUnitsPerPixelPerSecond = 10;

    /// <summary>
    /// G10 W3: a projectile's aliveTime in seconds, read in the same 10 ms ticks as fireDelay (W1) and the lead-aim tick
    /// (<c>Math.cs:210</c>, milliseconds / 10). The rest of the same file is consistent with that reading: every space mine lives
    /// 6000 (60 s), the cannons 550 (5.5 s), bombs and missile children 2000 (20 s). Negative values (aliveTime -65, -2000 and
    /// so on) are sentinels whose meaning is open, so they are refused rather than read. RECONSTRUCTED: no emulator code times a
    /// projectile out.
    /// </summary>
    public static float LifetimeSeconds(OriginalProjectile projectile)
    {
        ArgumentNullException.ThrowIfNull(projectile);
        if (projectile.AliveTime.Value <= 0)
            throw new OriginalDataException($"{projectile.Item.Name}: aliveTime {projectile.AliveTime} is a sentinel whose meaning is open, not a lifetime.");
        return projectile.AliveTime.Value * 10 / 1000f;
    }
}
