namespace RogueTrader.Server;

/// <summary>
/// Limits of an <see cref="AuthoritativeSession"/>. Every default is NEW/provisional, pending approval: no original value is
/// known for any of them.
/// </summary>
/// <param name="InputQueueCapacity">Most inputs held at once, across all future ticks.</param>
/// <param name="MaxInputLeadTicks">How far past the last simulated tick an input may name (60 ticks is one second at 60 Hz).</param>
/// <param name="InputsPerTick">
/// Submissions the rate limit allows per simulated tick (queue C8). The default 4 is one input per tick plus up to three
/// redundant resends, so a client may resend recent inputs against packet loss.
/// </param>
/// <param name="InputBurst">
/// Most submissions the rate limit allows before any tick runs, and the cap its allowance refills to (queue C8). The default
/// 120 is two seconds of inputs at 60 Hz, enough for a client to fill the whole lead window after a stall.
/// </param>
/// <param name="DisplacementTolerance">
/// World units a ship may move in one tick beyond its documented maximum speed times the tick duration before the motion
/// audit flags it (queue C8). The default 0.01 covers float rounding and the 0.001-unit asteroid contact separation.
/// </param>
public sealed record SessionOptions(
    int InputQueueCapacity = 128,
    int MaxInputLeadTicks = 60,
    int InputsPerTick = 4,
    int InputBurst = 120,
    float DisplacementTolerance = 0.01f)
{
    public static SessionOptions Default { get; } = new();

    internal void Validate()
    {
        if (InputQueueCapacity < 1) throw new ArgumentOutOfRangeException(nameof(InputQueueCapacity));
        if (MaxInputLeadTicks < 1) throw new ArgumentOutOfRangeException(nameof(MaxInputLeadTicks));
        if (InputsPerTick < 1) throw new ArgumentOutOfRangeException(nameof(InputsPerTick));
        if (InputBurst < 1) throw new ArgumentOutOfRangeException(nameof(InputBurst));
        if (!float.IsFinite(DisplacementTolerance) || DisplacementTolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(DisplacementTolerance));
    }
}
