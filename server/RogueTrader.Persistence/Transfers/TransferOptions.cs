namespace RogueTrader.Persistence.Transfers;

public sealed record TransferOptions
{
    /// <summary>Finished transfers kept per player for duplicate detection. NEW engineering limit, not game balance.</summary>
    public int RecentWindow { get; init; } = 64;
}
