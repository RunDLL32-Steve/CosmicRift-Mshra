using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace RogueTrader.Persistence;

/// <summary>Version 1 payload DTO (plain mutable collections for System.Text.Json), mapped explicitly to/from <see cref="PlayerProfile"/>.</summary>
internal sealed class ProfileDocumentV1
{
    [JsonRequired] public string ProfileId { get; set; } = "";
    [JsonRequired] public long Revision { get; set; }
    [JsonRequired] public long Wallet { get; set; }
    [JsonRequired] public Dictionary<string, int> Inventory { get; set; } = new(StringComparer.Ordinal);
    [JsonRequired] public List<string> Upgrades { get; set; } = new();
    [JsonRequired] public string? ShipHullId { get; set; }
    [JsonRequired] public List<AppliedTransactionDto> AppliedTransactions { get; set; } = new();
    [JsonRequired] public List<LedgerEntryDto> Ledger { get; set; } = new();
    [JsonRequired] public long NextLedgerSequence { get; set; }

    internal sealed class AppliedTransactionDto
    {
        [JsonRequired] public string TransactionId { get; set; } = "";
        [JsonRequired] public string Kind { get; set; } = "";
        [JsonRequired] public string RequestFingerprint { get; set; } = "";
        [JsonRequired] public long CurrencyDelta { get; set; }
        [JsonRequired] public Dictionary<string, int> ItemDeltas { get; set; } = new(StringComparer.Ordinal);
        [JsonRequired] public string? UpgradeId { get; set; }
        [JsonRequired] public long WalletAfter { get; set; }
        [JsonRequired] public long Revision { get; set; }
        [JsonRequired] public DateTimeOffset AppliedAtUtc { get; set; }
    }

    internal sealed class LedgerEntryDto
    {
        [JsonRequired] public long Sequence { get; set; }
        [JsonRequired] public string TransactionId { get; set; } = "";
        [JsonRequired] public string Kind { get; set; } = "";
        [JsonRequired] public long Delta { get; set; }
        [JsonRequired] public long BalanceAfter { get; set; }
        [JsonRequired] public DateTimeOffset AtUtc { get; set; }
    }

    public static ProfileDocumentV1 From(PlayerProfile p) => new()
    {
        ProfileId = p.ProfileId,
        Revision = p.Revision,
        Wallet = p.Wallet,
        Inventory = new Dictionary<string, int>(p.Inventory, StringComparer.Ordinal),
        Upgrades = p.Upgrades.ToList(),
        ShipHullId = p.ShipHullId,
        AppliedTransactions = p.AppliedTransactions.Select(t => new AppliedTransactionDto
        {
            TransactionId = t.TransactionId,
            Kind = t.Kind.ToString(),
            RequestFingerprint = t.RequestFingerprint,
            CurrencyDelta = t.CurrencyDelta,
            ItemDeltas = new Dictionary<string, int>(t.ItemDeltas, StringComparer.Ordinal),
            UpgradeId = t.UpgradeId,
            WalletAfter = t.WalletAfter,
            Revision = t.Revision,
            AppliedAtUtc = t.AppliedAtUtc,
        }).ToList(),
        Ledger = p.Ledger.Select(e => new LedgerEntryDto
        {
            Sequence = e.Sequence,
            TransactionId = e.TransactionId,
            Kind = e.Kind.ToString(),
            Delta = e.Delta,
            BalanceAfter = e.BalanceAfter,
            AtUtc = e.AtUtc,
        }).ToList(),
        NextLedgerSequence = p.NextLedgerSequence,
    };

    /// <summary>Maps to the immutable model; returns null and an error message when the document is structurally unmappable.</summary>
    public PlayerProfile? ToProfile(out string? error)
    {
        error = null;
        if (Inventory is null || Upgrades is null || AppliedTransactions is null || Ledger is null)
        { error = "A required collection is null."; return null; }

        var upgrades = ImmutableSortedSet.CreateRange(StringComparer.Ordinal, Upgrades);
        if (upgrades.Count != Upgrades.Count) { error = "Upgrades contains duplicates or null entries."; return null; }
        foreach (var key in Inventory.Keys)
            if (key is null) { error = "Inventory contains a null key."; return null; }

        var applied = ImmutableList.CreateBuilder<AppliedTransaction>();
        foreach (var t in AppliedTransactions)
        {
            if (t is null || t.ItemDeltas is null || !Enum.TryParse<TransactionKind>(t.Kind, ignoreCase: false, out var kind) || !Enum.IsDefined(kind))
            { error = "An applied transaction is malformed."; return null; }
            applied.Add(new AppliedTransaction(t.TransactionId, kind, t.RequestFingerprint, t.CurrencyDelta,
                ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, t.ItemDeltas), t.UpgradeId,
                t.WalletAfter, t.Revision, t.AppliedAtUtc));
        }

        var ledger = ImmutableList.CreateBuilder<WalletLedgerEntry>();
        foreach (var e in Ledger)
        {
            if (e is null || !Enum.TryParse<TransactionKind>(e.Kind, ignoreCase: false, out var kind) || !Enum.IsDefined(kind))
            { error = "A ledger entry is malformed."; return null; }
            ledger.Add(new WalletLedgerEntry(e.Sequence, e.TransactionId, kind, e.Delta, e.BalanceAfter, e.AtUtc));
        }

        return new PlayerProfile
        {
            SchemaVersion = PlayerProfile.CurrentSchemaVersion,
            ProfileId = ProfileId,
            Revision = Revision,
            Wallet = Wallet,
            Inventory = ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, Inventory),
            Upgrades = upgrades,
            ShipHullId = ShipHullId,
            AppliedTransactions = applied.ToImmutable(),
            Ledger = ledger.ToImmutable(),
            NextLedgerSequence = NextLedgerSequence,
        };
    }
}
