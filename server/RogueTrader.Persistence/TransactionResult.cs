namespace RogueTrader.Persistence;

/// <param name="Revision">Revision of the committed profile after the transaction (or current revision for a rejection; -1 when the profile state is unknown).</param>
public sealed record TransactionResult(string TransactionId, TransactionStatus Status, RejectionReason Reason, bool IsReplay,
    long CurrencyDelta, IReadOnlyDictionary<string, int> ItemDeltas, string? UpgradeId, long WalletAfter, long Revision);
