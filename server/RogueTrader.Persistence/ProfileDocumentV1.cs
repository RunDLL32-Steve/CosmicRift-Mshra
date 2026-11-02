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
    /// <summary>Schema 3. Absent in schema 1/2 payloads (no saved ship condition).</summary>
    public ShipConditionDto? Ship { get; set; }
    /// <summary>Schema 5. Absent in older payloads (no bounty).</summary>
    public long Bounty { get; set; }
    /// <summary>Schema 5. Absent in older payloads (neutral standing).</summary>
    public Dictionary<string, long>? Reputation { get; set; }

    internal sealed class ConductDto
    {
        [JsonRequired] public string EventId { get; set; } = "";
        [JsonRequired] public int Count { get; set; }
        [JsonRequired] public long BountyDelta { get; set; }
        [JsonRequired] public Dictionary<string, long> ReputationDeltas { get; set; } = new(StringComparer.Ordinal);
        [JsonRequired] public long BountyAfter { get; set; }
    }

    internal sealed class ShipConditionDto
    {
        [JsonRequired] public float Hull { get; set; }
        [JsonRequired] public float X { get; set; }
        [JsonRequired] public float Y { get; set; }
        [JsonRequired] public float Heading { get; set; }
    }

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
        /// <summary>Schema 2. Absent in schema 1 payloads, which <see cref="ProfileSchemaMigrations.V1ToV2"/> carries forward unchanged.</summary>
        public string? RepairId { get; set; }
        /// <summary>Schema 5. Present only on <c>RecordConduct</c> transactions.</summary>
        public ConductDto? Conduct { get; set; }
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
            RepairId = t.RepairId,
            Conduct = t.Conduct is { } c ? new ConductDto
            {
                EventId = c.EventId,
                Count = c.Count,
                BountyDelta = c.BountyDelta,
                ReputationDeltas = new Dictionary<string, long>(c.ReputationDeltas, StringComparer.Ordinal),
                BountyAfter = c.BountyAfter,
            } : null,
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
        Ship = p.Ship is { } s ? new ShipConditionDto { Hull = s.Hull, X = s.X, Y = s.Y, Heading = s.Heading } : null,
        Bounty = p.Bounty,
        Reputation = p.Reputation.Count > 0 ? new Dictionary<string, long>(p.Reputation, StringComparer.Ordinal) : null,
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
        foreach (var key in Reputation?.Keys ?? Enumerable.Empty<string>())
            if (key is null) { error = "Reputation contains a null key."; return null; }

        var applied = ImmutableList.CreateBuilder<AppliedTransaction>();
        foreach (var t in AppliedTransactions)
        {
            if (t is null || t.ItemDeltas is null || t.Conduct is { EventId: null } or { ReputationDeltas: null } || !Enum.TryParse<TransactionKind>(t.Kind, ignoreCase: false, out var kind) || !Enum.IsDefined(kind))
            { error = "An applied transaction is malformed."; return null; }
            applied.Add(new AppliedTransaction(t.TransactionId, kind, t.RequestFingerprint, t.CurrencyDelta,
                ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, t.ItemDeltas), t.UpgradeId,
                t.WalletAfter, t.Revision, t.AppliedAtUtc, t.RepairId,
                t.Conduct is { } c ? new ConductDelta(c.EventId, c.Count, c.BountyDelta,
                    ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, c.ReputationDeltas), c.BountyAfter) : null));
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
            Ship = Ship is { } s ? new ShipCondition(s.Hull, s.X, s.Y, s.Heading) : null,
            Bounty = Bounty,
            Reputation = ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, Reputation ?? []),
        };
    }
}
