namespace RogueTrader.OriginalData;

/// <summary>
/// Queue G10 (flight slice): one hull's raw VEH flight block read through the InfantryOnline emulator's unit chain
/// (bb97579, <c>dotnetcore/ZoneServer/Game/Arena/Bot/MovementController.cs</c> and <c>Protocol/Helpers/Math.cs</c>). The
/// emulator is a reconstruction written against the native client, not the native client, so every derived number here is
/// RECONSTRUCTED; the raw inputs stay available through <see cref="Block"/>. No code was copied. Nothing in the game uses
/// this reading yet; it is the "expected" column of docs/original-research/G10_VALIDATION.md.
/// </summary>
/// <remarks>
/// Unit chain (FLIGHT_FIELD_EVIDENCE.md section 1): position in pixels, 16 per LVL tile; velocity in thousandths of a pixel
/// per 10 ms, so 1 raw unit is 0.1 px/s; yaw 240 units per turn (1.5 degrees each). Thrust is <c>raw × 4800 / 128</c> velocity
/// units per second with C# integer division, rotation is <c>raw / 10000</c> yaw units per millisecond, and friction scales
/// velocity by <c>1000 − raw × 800 / 256 / 10 × (ms / 10)</c> per mille on each update. BackwardThrust −1 is read as "same as
/// RollThrust" (owner decision, November 2, 2026); the emulator bot multiplies the −1 literally instead.
/// </remarks>
public sealed class EmulatorFlightReading
{
    /// <summary>Velocity units per pixel per second: one raw unit is a thousandth of a pixel per 10 ms.</summary>
    public const double VelocityUnitsPerPixelPerSecond = 10;
    /// <summary>The bot's thrust factor numerator and denominator (integer arithmetic).</summary>
    public const int ThrustNumerator = 4800, ThrustDenominator = 128;
    /// <summary>Degrees per yaw unit (240 per revolution).</summary>
    public const double DegreesPerYawUnit = 1.5;

    private EmulatorFlightReading(OriginalHull hull, TerrainSpeedBlock block)
    {
        Hull = hull;
        Block = block;
        ReverseUsesForwardThrust = block.BackwardThrust.Value == -1;
        var reverseRaw = ReverseUsesForwardThrust ? block.RollThrust.Value : block.BackwardThrust.Value;
        TopSpeedPixelsPerSecond = block.RollTopSpeed.Value / VelocityUnitsPerPixelPerSecond;
        ForwardAccelerationPixelsPerSecondSquared = ThrustVelocityUnitsPerSecond(block.RollThrust.Value) / VelocityUnitsPerPixelPerSecond;
        ReverseAccelerationPixelsPerSecondSquared = ThrustVelocityUnitsPerSecond(reverseRaw) / VelocityUnitsPerPixelPerSecond;
        StrafeAccelerationPixelsPerSecondSquared = ThrustVelocityUnitsPerSecond(block.StrafeThrust.Value) / VelocityUnitsPerPixelPerSecond;
        TurnDegreesPerSecond = block.RollRotate.Value / 10000.0 * 1000 * DegreesPerYawUnit;
        HyperTopSpeedPixelsPerSecond = block.HyperTopSpeed.Value / VelocityUnitsPerPixelPerSecond;
        WallBounceRetained = hull.BouncePercent.Value / 1000.0;
    }

    /// <summary>Reads terrain block <paramref name="terrainIndex"/> (0 is open space) of <paramref name="hull"/>.</summary>
    public static EmulatorFlightReading From(OriginalHull hull, int terrainIndex = 0)
    {
        ArgumentNullException.ThrowIfNull(hull);
        ArgumentOutOfRangeException.ThrowIfNegative(terrainIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(terrainIndex, hull.TerrainSpeedBlocks.Count);
        return new EmulatorFlightReading(hull, hull.TerrainSpeedBlocks[terrainIndex]);
    }

    /// <summary>The bot's integer thrust conversion: <c>raw × 4800 / 128</c> velocity units per second (45 → 1687).</summary>
    public static int ThrustVelocityUnitsPerSecond(int raw) => raw * ThrustNumerator / ThrustDenominator;

    public OriginalHull Hull { get; }
    /// <summary>The raw block this reading came from.</summary>
    public TerrainSpeedBlock Block { get; }
    /// <summary>RollTopSpeed / 10: the bot clamps the velocity length to it.</summary>
    public double TopSpeedPixelsPerSecond { get; }
    /// <summary>RollThrust through the bot's thrust factor.</summary>
    public double ForwardAccelerationPixelsPerSecondSquared { get; }
    /// <summary>True when BackwardThrust is the −1 sentinel and the reverse value is taken from RollThrust.</summary>
    public bool ReverseUsesForwardThrust { get; }
    public double ReverseAccelerationPixelsPerSecondSquared { get; }
    /// <summary>StrafeThrust through the same factor; the bot applies it sideways.</summary>
    public double StrafeAccelerationPixelsPerSecondSquared { get; }
    /// <summary>RollRotate / 10000 yaw units per ms, at 1.5 degrees per yaw unit.</summary>
    public double TurnDegreesPerSecond { get; }
    /// <summary>HyperTopSpeed in the same velocity unit. No emulator code reads this field, so the unit is an assumption.</summary>
    public double HyperTopSpeedPixelsPerSecond { get; }
    /// <summary>BouncePercent / 1000: the share of speed the bot keeps after hitting a blocked tile.</summary>
    public double WallBounceRetained { get; }

    /// <summary>Seconds from rest to <see cref="TopSpeedPixelsPerSecond"/> under forward thrust, with no friction.</summary>
    public double SecondsToTopSpeed => TopSpeedPixelsPerSecond / ForwardAccelerationPixelsPerSecondSquared;

    /// <summary>Seconds for one full turn.</summary>
    public double SecondsPerRevolution => 360 / TurnDegreesPerSecond;

    /// <summary>
    /// Share of speed left after one second of coasting when the bot updates every <paramref name="stepMilliseconds"/>. The
    /// bot's friction depends on its update step, so this is not a time-invariant law; 1 whenever RollFriction is 0.
    /// </summary>
    public double FrictionRetainedPerSecond(double stepMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stepMilliseconds);
        var perStep = (1000 - Block.RollFriction.Value * 800.0 / 256 / 10 * (stepMilliseconds / 10)) / 1000;
        return Math.Pow(perStep, 1000 / stepMilliseconds);
    }
}
