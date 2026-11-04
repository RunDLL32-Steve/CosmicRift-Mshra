using System.Collections.Immutable;

namespace RogueTrader.Persistence;

/// <summary>Immutable persisted player state. Ids are opaque strings compared ordinally; currency is <c>long</c>.</summary>
public sealed record PlayerProfile
{
    public const int CurrentSchemaVersion = 7;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string ProfileId { get; init; }
    public long Revision { get; init; }
    public long Wallet { get; init; }
    public ImmutableSortedDictionary<string, int> Inventory { get; init; } = ImmutableSortedDictionary.Create<string, int>(StringComparer.Ordinal);
    public ImmutableSortedSet<string> Upgrades { get; init; } = ImmutableSortedSet.Create<string>(StringComparer.Ordinal);
    /// <summary>The hull the player flies now (the active owned ship); null when the profile owns no ship.</summary>
    public string? ShipHullId { get; init; }
    /// <summary>Schema 3: last saved condition of the active ship; null until the client first saves one (the ship then starts at spawn).</summary>
    public ShipCondition? Ship { get; init; }
    /// <summary>
    /// Schema 6: every hull the player owns, at most one of each (Steve, Nov 3 2026). The value is the condition a ship was
    /// parked with when the player switched away from it (null: it starts at the spawn with a full hull). The active ship's
    /// own entry is always null, because its condition is <see cref="Ship"/>.
    /// </summary>
    public ImmutableSortedDictionary<string, ShipCondition?> OwnedShips { get; init; } = ImmutableSortedDictionary.Create<string, ShipCondition?>(StringComparer.Ordinal);
    /// <summary>Schema 5 (queue C9): the bounty on this player; 0 when none.</summary>
    public long Bounty { get; init; }
    /// <summary>Schema 5 (queue C9): non-neutral standing per reputation category; a category at 0 is absent.</summary>
    public ImmutableSortedDictionary<string, long> Reputation { get; init; } = ImmutableSortedDictionary.Create<string, long>(StringComparer.Ordinal);
    public ImmutableList<AppliedTransaction> AppliedTransactions { get; init; } = ImmutableList<AppliedTransaction>.Empty;
    public ImmutableList<WalletLedgerEntry> Ledger { get; init; } = ImmutableList<WalletLedgerEntry>.Empty;
    public long NextLedgerSequence { get; init; } = 1;

    public static PlayerProfile CreateNew(string profileId, long initialWallet, string? shipHullId) => new()
    {
        ProfileId = profileId,
        Wallet = initialWallet,
        ShipHullId = shipHullId,
        OwnedShips = shipHullId is null
            ? ImmutableSortedDictionary.Create<string, ShipCondition?>(StringComparer.Ordinal)
            : ImmutableSortedDictionary.Create<string, ShipCondition?>(StringComparer.Ordinal).Add(shipHullId, null),
    };
}
