namespace RogueTrader.Server;

/// <summary>What the motion audit of an <see cref="AuthoritativeSession"/> found (queue C8, NEW/provisional).</summary>
public enum MotionViolationKind
{
    /// <summary>The controlled ship moved further in one tick than its maximum speed allows.</summary>
    SpeedExceeded = 0,
    /// <summary>The controlled ship's position changed between ticks without going through the session (a teleport).</summary>
    MovedOutsideTick = 1,
}

/// <summary>
/// One flagged movement of the controlled ship. The audit only reports: it does not undo the move or punish anyone, because no
/// approved enforcement policy exists.
/// </summary>
/// <param name="Tick">The tick whose result was flagged; for <see cref="MotionViolationKind.MovedOutsideTick"/>, the last tick
/// simulated before the move was seen.</param>
/// <param name="Distance">How far the ship moved, in world units.</param>
/// <param name="Limit">The most it was allowed to move, in world units (0 between ticks).</param>
public sealed record MotionViolation(long Tick, MotionViolationKind Kind, float Distance, float Limit);
