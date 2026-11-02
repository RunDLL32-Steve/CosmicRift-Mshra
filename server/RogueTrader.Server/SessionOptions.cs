namespace RogueTrader.Server;

/// <summary>Limits of an <see cref="AuthoritativeSession"/>. Both defaults are NEW/provisional; no original value is known.</summary>
/// <param name="InputQueueCapacity">Most inputs held at once, across all future ticks.</param>
/// <param name="MaxInputLeadTicks">How far past the last simulated tick an input may name (60 ticks is one second at 60 Hz).</param>
public sealed record SessionOptions(int InputQueueCapacity = 128, int MaxInputLeadTicks = 60)
{
    public static SessionOptions Default { get; } = new();

    internal void Validate()
    {
        if (InputQueueCapacity < 1) throw new ArgumentOutOfRangeException(nameof(InputQueueCapacity));
        if (MaxInputLeadTicks < 1) throw new ArgumentOutOfRangeException(nameof(MaxInputLeadTicks));
    }
}
