namespace RogueTrader.Persistence;

public static class ProfileInvariants
{
    public const int MaxTransactionIdLength = 128;

    /// <summary>Empty list == valid. Used on load (violation =&gt; Corrupt) and before every save (violation =&gt; Invalid).</summary>
    public static IReadOnlyList<string> Validate(PlayerProfile p, PersistenceOptions o)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentNullException.ThrowIfNull(o);
        var errors = new List<string>();
        if (p.SchemaVersion != PlayerProfile.CurrentSchemaVersion)
            errors.Add($"SchemaVersion {p.SchemaVersion} is not the current version {PlayerProfile.CurrentSchemaVersion}.");
        if (!ProfileIdRules.IsValid(p.ProfileId)) errors.Add("ProfileId does not match ^[A-Za-z0-9_-]{1,64}$.");
        if (p.Revision < 0) errors.Add($"Revision {p.Revision} is negative.");
        if (p.Wallet < 0) errors.Add($"Wallet {p.Wallet} is negative.");
        if (p.ShipHullId is { Length: 0 }) errors.Add("ShipHullId is empty (use null for none).");
        if (p.ShipHullId is not null && !IsWellFormedUtf16(p.ShipHullId)) errors.Add(Malformed("ShipHullId"));

        if (p.Inventory is null) errors.Add("Inventory is null.");
        else
        {
            if (!ReferenceEquals(p.Inventory.KeyComparer, StringComparer.Ordinal)) errors.Add("Inventory does not use the ordinal comparer.");
            foreach (var (key, qty) in p.Inventory)
            {
                if (string.IsNullOrEmpty(key)) errors.Add("Inventory contains an empty item id.");
                else if (!IsWellFormedUtf16(key)) errors.Add(Malformed("An inventory item id"));
                if (qty <= 0) errors.Add($"Inventory quantity for '{key}' is {qty}; must be > 0.");
            }
        }

        if (p.Upgrades is null) errors.Add("Upgrades is null.");
        else
        {
            if (!ReferenceEquals(p.Upgrades.KeyComparer, StringComparer.Ordinal)) errors.Add("Upgrades does not use the ordinal comparer.");
            foreach (var u in p.Upgrades)
            {
                if (string.IsNullOrEmpty(u)) errors.Add("Upgrades contains an empty id.");
                else if (!IsWellFormedUtf16(u)) errors.Add(Malformed("An upgrade id"));
            }
        }

        if (p.AppliedTransactions is null) errors.Add("AppliedTransactions is null.");
        else
        {
            if (p.AppliedTransactions.Count > o.IdempotencyWindow)
                errors.Add($"AppliedTransactions has {p.AppliedTransactions.Count} entries; window is {o.IdempotencyWindow}.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var t in p.AppliedTransactions)
            {
                if (t is null || string.IsNullOrEmpty(t.TransactionId)) { errors.Add("An applied transaction has an empty id."); continue; }
                if (t.TransactionId.Length > MaxTransactionIdLength) errors.Add($"Transaction id '{Trim(t.TransactionId)}' exceeds {MaxTransactionIdLength} characters.");
                if (!IsWellFormedUtf16(t.TransactionId)) errors.Add(Malformed("A transaction id"));
                if (!seen.Add(t.TransactionId)) errors.Add($"Transaction id '{Trim(t.TransactionId)}' appears more than once.");
                if (t.ItemDeltas is null || string.IsNullOrEmpty(t.RequestFingerprint)) errors.Add($"Applied transaction '{Trim(t.TransactionId)}' is incomplete.");
                else if (t.ItemDeltas.Keys.Any(k => k is null || !IsWellFormedUtf16(k)) || (t.UpgradeId is not null && !IsWellFormedUtf16(t.UpgradeId)))
                    errors.Add(Malformed($"An id in applied transaction '{Trim(t.TransactionId)}'"));
            }
        }

        if (p.NextLedgerSequence < 1) errors.Add("NextLedgerSequence must be >= 1.");
        if (p.Ledger is null) errors.Add("Ledger is null.");
        else
        {
            if (p.Ledger.Count > o.LedgerRetention)
                errors.Add($"Ledger has {p.Ledger.Count} entries; retention is {o.LedgerRetention}.");
            long previous = 0;
            foreach (var e in p.Ledger)
            {
                if (e is null) { errors.Add("Ledger contains a null entry."); continue; }
                if (e.TransactionId is not null && !IsWellFormedUtf16(e.TransactionId)) errors.Add(Malformed($"Ledger entry {e.Sequence} transaction id"));
                if (e.Sequence <= previous) errors.Add($"Ledger sequence {e.Sequence} is not strictly increasing.");
                if (e.Sequence >= p.NextLedgerSequence) errors.Add($"Ledger sequence {e.Sequence} is not below NextLedgerSequence {p.NextLedgerSequence}.");
                previous = e.Sequence;
            }
        }
        return errors;
    }

    private static string Trim(string s) => s.Length <= 40 ? s : s[..40] + "...";

    private static string Malformed(string what) => $"{what} is not well-formed UTF-16 (unpaired surrogate); it would not survive a save verbatim.";

    /// <summary>
    /// False when the string has an unpaired surrogate. Such strings are replaced with U+FFFD by UTF-8/JSON encoding,
    /// so they cannot be stored verbatim (a transaction id would no longer match after a restart).
    /// </summary>
    internal static bool IsWellFormedUtf16(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= s.Length || !char.IsLowSurrogate(s[i + 1])) return false;
                i++;
            }
            else if (char.IsLowSurrogate(c)) return false;
        }
        return true;
    }
}
