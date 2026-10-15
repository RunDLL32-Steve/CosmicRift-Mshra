namespace RogueTrader.Persistence;

public sealed record WalletLedgerEntry(long Sequence, string TransactionId, TransactionKind Kind,
    long Delta, long BalanceAfter, DateTimeOffset AtUtc);
