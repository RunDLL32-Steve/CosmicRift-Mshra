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
    Task<TransactionResult> RepairAsync(TransactionContext ctx, RepairRequest r, CancellationToken ct = default);
    Task<TransactionResult> RefineAsync(TransactionContext ctx, RefineRequest r, CancellationToken ct = default);
    Task<TransactionResult> GrantRewardAsync(TransactionContext ctx, GrantRewardRequest r, CancellationToken ct = default);

    /// <summary>Applies one conduct event to bounty and reputation through the conduct rules (queue C9); no station needed.</summary>
    Task<TransactionResult> RecordConductAsync(TransactionContext ctx, RecordConductRequest r, CancellationToken ct = default);

    /// <summary>
    /// Saves the ship's hull and position (last write wins). Not a currency transaction: no ledger or idempotency entry, but the
    /// same per-profile lock, compare-and-swap save and acknowledge-after-save rule apply.
    /// </summary>
    Task<ShipConditionResult> SaveShipConditionAsync(TransactionContext ctx, string profileId, ShipCondition ship, CancellationToken ct = default);

    /// <summary>Buys a hull the player does not own yet (one of each); the player keeps flying the current ship.</summary>
    Task<TransactionResult> BuyShipAsync(TransactionContext ctx, BuyShipRequest r, CancellationToken ct = default);

    /// <summary>Charges the destruction penalty once per destruction id and puts the active ship back at the spawn with a full hull.</summary>
    Task<TransactionResult> RecordShipDestroyedAsync(TransactionContext ctx, ShipDestroyedRequest r, CancellationToken ct = default);

    /// <summary>
    /// Makes an owned hull the active ship. The ship left behind is parked with its last saved condition and the chosen ship
    /// comes back exactly as it was parked, so switching never repairs a hull. Like ship saves, no ledger or idempotency entry.
    /// </summary>
    Task<ShipConditionResult> SelectShipAsync(TransactionContext ctx, string profileId, string hullId, CancellationToken ct = default);
}
