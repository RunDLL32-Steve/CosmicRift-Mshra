using System.Collections.Immutable;

namespace RogueTrader.Persistence;

/// <summary>Idempotency-window entry: the committed outcome of one transaction id.</summary>
public sealed record AppliedTransaction(string TransactionId, TransactionKind Kind, string RequestFingerprint,
    long CurrencyDelta, ImmutableSortedDictionary<string, int> ItemDeltas, string? UpgradeId,
    long WalletAfter, long Revision, DateTimeOffset AppliedAtUtc);
