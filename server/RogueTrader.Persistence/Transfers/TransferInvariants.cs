namespace RogueTrader.Persistence.Transfers;

/// <summary>Checks a presence document before every journal write; the in-memory journal refuses violations as Invalid.</summary>
public static class TransferInvariants
{
    public static IReadOnlyList<string> Validate(PlayerPresence p, TransferOptions o)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentNullException.ThrowIfNull(o);
        var errors = new List<string>();
        if (!ProfileIdRules.IsValid(p.PlayerId)) errors.Add("PlayerId does not match ^[A-Za-z0-9_-]{1,64}$.");
        if (!IdOk(p.OwnerSectorId)) errors.Add("OwnerSectorId is empty or malformed.");
        if (p.Epoch < 1) errors.Add($"Epoch {p.Epoch} must be >= 1.");
        if (p.Revision < 0) errors.Add($"Revision {p.Revision} is negative.");
        if (p.Recent is null) { errors.Add("Recent is null."); return errors; }
        if (p.Recent.Count > o.RecentWindow) errors.Add($"Recent has {p.Recent.Count} entries; window is {o.RecentWindow}.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in p.Recent)
        {
            if (r is null) { errors.Add("Recent contains a null record."); continue; }
            CheckRecord(r, p, errors);
            if (!r.Phase.IsTerminal()) errors.Add($"Recent transfer '{r.TransferId}' is not terminal ({r.Phase}).");
            if (!seen.Add(r.TransferId)) errors.Add($"Transfer id '{r.TransferId}' appears more than once.");
        }

        if (p.Active is { } a)
        {
            CheckRecord(a, p, errors);
            if (a.Phase.IsTerminal()) errors.Add($"Active transfer '{a.TransferId}' is terminal ({a.Phase}).");
            if (!seen.Add(a.TransferId)) errors.Add($"Active transfer id '{a.TransferId}' is also in Recent.");
            // Ownership must match the decision: before the commit point and while aborting the source still owns the player
            // at the epoch the transfer began; once committed the destination owns it one epoch later.
            if (a.Phase == TransferPhase.Committed)
            {
                if (!string.Equals(p.OwnerSectorId, a.DestinationSectorId, StringComparison.Ordinal) || p.Epoch != a.SourceEpoch + 1)
                    errors.Add($"Committed transfer '{a.TransferId}' requires owner '{a.DestinationSectorId}' at epoch {a.SourceEpoch + 1}.");
            }
            else if (!string.Equals(p.OwnerSectorId, a.SourceSectorId, StringComparison.Ordinal) || p.Epoch != a.SourceEpoch)
                errors.Add($"Undecided or aborting transfer '{a.TransferId}' requires owner '{a.SourceSectorId}' at epoch {a.SourceEpoch}.");
            if (a.Phase is TransferPhase.Prepared or TransferPhase.Committed && (a.Manifest is null || a.Spawn is null))
                errors.Add($"Transfer '{a.TransferId}' at {a.Phase} lacks its manifest or spawn.");
        }
        return errors;
    }

    private static void CheckRecord(TransferRecord r, PlayerPresence p, List<string> errors)
    {
        if (!TransferIdOk(r.TransferId)) errors.Add("A transfer id is empty, too long or malformed.");
        if (!string.Equals(r.PlayerId, p.PlayerId, StringComparison.Ordinal)) errors.Add($"Transfer '{r.TransferId}' belongs to '{r.PlayerId}'.");
        if (!IdOk(r.SourceSectorId) || !IdOk(r.DestinationSectorId) || !IdOk(r.DestinationEntryId) || string.IsNullOrEmpty(r.Fingerprint))
            errors.Add($"Transfer '{r.TransferId}' is incomplete.");
        if (string.Equals(r.SourceSectorId, r.DestinationSectorId, StringComparison.Ordinal)) errors.Add($"Transfer '{r.TransferId}' has the same source and destination.");
        if (r.SourceEpoch < 1) errors.Add($"Transfer '{r.TransferId}' has epoch {r.SourceEpoch}.");
        if (r.Spawn is { IsValid: false }) errors.Add($"Transfer '{r.TransferId}' has an invalid spawn.");
    }

    internal static bool IdOk(string? id) => !string.IsNullOrEmpty(id) && ProfileInvariants.IsWellFormedUtf16(id);

    internal static bool TransferIdOk(string? id) => IdOk(id) && id!.Length <= ProfileInvariants.MaxTransactionIdLength;
}
