using System.Collections.Immutable;

namespace RogueTrader.Persistence;

/// <summary>Immutable persisted player state. Ids are opaque strings compared ordinally; currency is <c>long</c>.</summary>
public sealed record PlayerProfile
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string ProfileId { get; init; }
    public long Revision { get; init; }
    public long Wallet { get; init; }
    public ImmutableSortedDictionary<string, int> Inventory { get; init; } = ImmutableSortedDictionary.Create<string, int>(StringComparer.Ordinal);
    public ImmutableSortedSet<string> Upgrades { get; init; } = ImmutableSortedSet.Create<string>(StringComparer.Ordinal);
    public string? ShipHullId { get; init; }
    public ImmutableList<AppliedTransaction> AppliedTransactions { get; init; } = ImmutableList<AppliedTransaction>.Empty;
    public ImmutableList<WalletLedgerEntry> Ledger { get; init; } = ImmutableList<WalletLedgerEntry>.Empty;
    public long NextLedgerSequence { get; init; } = 1;

    public static PlayerProfile CreateNew(string profileId, long initialWallet, string? shipHullId) => new()
    {
        ProfileId = profileId,
        Wallet = initialWallet,
        ShipHullId = shipHullId,
    };
}
