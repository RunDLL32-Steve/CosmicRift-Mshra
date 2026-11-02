namespace RogueTrader.Server;

/// <summary>What the server did with one submitted <see cref="RogueTrader.Protocol.PlayerInput"/> (NEW/provisional rules, queue C6).</summary>
public enum InputVerdict
{
    /// <summary>Queued for the tick it names.</summary>
    Accepted = 0,
    /// <summary>The sequence is already queued or was recently processed; nothing changes.</summary>
    Duplicate = 1,
    /// <summary>The input names a tick the server has already simulated.</summary>
    Late = 2,
    /// <summary>The sequence is at or below the last processed sequence (an older input that arrived after a newer one ran).</summary>
    Stale = 3,
    /// <summary>The input names a tick further ahead than <see cref="SessionOptions.MaxInputLeadTicks"/>.</summary>
    TooFarAhead = 4,
    /// <summary>The bounded input queue is full.</summary>
    QueueFull = 5,
    /// <summary>The input is malformed: tick 0, non-finite thrust or turn, or reserved flag bits.</summary>
    Invalid = 6,
}
