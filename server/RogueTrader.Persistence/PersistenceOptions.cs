namespace RogueTrader.Persistence;

public sealed record PersistenceOptions
{
    /// <summary>NEW engineering limit, not game balance.</summary>
    public int IdempotencyWindow { get; init; } = 1024;

    /// <summary>NEW engineering limit, not game balance.</summary>
    public int LedgerRetention { get; init; } = 1024;
}
