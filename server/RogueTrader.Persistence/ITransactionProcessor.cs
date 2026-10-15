namespace RogueTrader.Persistence;

public interface ITransactionProcessor
{
    /// <summary>(Re)loads the profile from the repository; required before transactions and after a persistence failure.</summary>
    Task<ProfileLoadResult> OpenProfileAsync(string profileId, CancellationToken ct = default);

    /// <summary>Last committed state, or null if never opened.</summary>
    PlayerProfile? GetSnapshot(string profileId);

    Task<TransactionResult> BuyAsync(TransactionContext ctx, BuyRequest r, CancellationToken ct = default);
    Task<TransactionResult> SellAsync(TransactionContext ctx, SellRequest r, CancellationToken ct = default);
    Task<TransactionResult> BuyUpgradeAsync(TransactionContext ctx, BuyUpgradeRequest r, CancellationToken ct = default);
    Task<TransactionResult> GrantRewardAsync(TransactionContext ctx, GrantRewardRequest r, CancellationToken ct = default);
}
