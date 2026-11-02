using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace RogueTrader.Persistence.Transfers;

/// <summary>
/// Server-authoritative, recoverable transfer of one player's ship between two sectors (queue C10, NEW engineering design;
/// original transition semantics stay open). Ownership lives in the <see cref="ITransferJournal"/>: the journal names the one
/// sector that owns the player and a fencing epoch. A transfer is a prepare/commit/recover state machine whose single commit
/// point is the journal write that flips ownership; participants are only told the decision after it is durable.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Before the commit point recovery aborts (source restores, destination cancels); after it recovery rolls forward
/// (destination activates, source releases). Every participant call is idempotent, so any step may be repeated.</item>
/// <item>Per player, one in-process lock serializes calls; the journal compare-and-swap fences a second or restarted
/// coordinator. A coordinator whose write conflicts stops and reports <see cref="SectorTransferStatus.Unresolved"/>.</item>
/// <item>Transfer ids are idempotency keys: a retry of the same request resumes or replays it; a different request under a
/// used id is refused.</item>
/// <item>The coordinator never touches a <see cref="PlayerProfile"/>: wallet and inventory stay in the transaction core.</item>
/// </list>
/// </remarks>
public sealed class SectorTransferCoordinator
{
    private readonly ITransferJournal _journal;
    private readonly IReadOnlyDictionary<string, ISectorParticipant> _sectors;
    private readonly Func<string, string, bool> _routeAllowed;
    private readonly TransferOptions _options;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    /// <param name="routeAllowed">Optional (source, destination) policy; every route is allowed when null. Original routes are undecoded.</param>
    public SectorTransferCoordinator(ITransferJournal journal, IEnumerable<ISectorParticipant> sectors, TransferOptions? options = null,
        Func<string, string, bool>? routeAllowed = null)
    {
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        ArgumentNullException.ThrowIfNull(sectors);
        var map = new Dictionary<string, ISectorParticipant>(StringComparer.Ordinal);
        foreach (var s in sectors)
        {
            if (s is null || !TransferInvariants.IdOk(s.SectorId)) throw new ArgumentException("Every sector needs a well-formed id.", nameof(sectors));
            if (!map.TryAdd(s.SectorId, s)) throw new ArgumentException($"Sector '{s.SectorId}' is listed twice.", nameof(sectors));
        }
        _sectors = map;
        _options = options ?? new TransferOptions();
        if (_options.RecentWindow < 1) throw new ArgumentOutOfRangeException(nameof(options), "RecentWindow must be >= 1.");
        _routeAllowed = routeAllowed ?? ((_, _) => true);
    }

    /// <summary>Creates the presence document that makes <paramref name="sectorId"/> the player's owner at epoch 1.</summary>
    public async Task<JournalWriteStatus> RegisterAsync(string playerId, string sectorId, CancellationToken ct = default)
    {
        if (!ProfileIdRules.IsValid(playerId) || !_sectors.ContainsKey(sectorId ?? string.Empty)) return JournalWriteStatus.Invalid;
        return await _journal.CreateAsync(PlayerPresence.Create(playerId, sectorId!), ct).ConfigureAwait(false);
    }

    public async Task<SectorTransferResult> TransferAsync(TransferContext ctx, SectorTransferRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(request);
        var q = request;
        // Shape, identity and route checks happen before any lock or write.
        if (!TransferInvariants.TransferIdOk(q.TransferId) || !ProfileIdRules.IsValid(q.PlayerId) || !TransferInvariants.IdOk(q.SourceSectorId)
            || !TransferInvariants.IdOk(q.DestinationSectorId) || !TransferInvariants.IdOk(q.DestinationEntryId))
            return Rejected(q.TransferId, TransferRejection.InvalidRequest, null);
        if (!string.Equals(q.PlayerId, ctx.AuthenticatedPlayerId, StringComparison.Ordinal)) return Rejected(q.TransferId, TransferRejection.IdentityMismatch, null);
        if (!_sectors.ContainsKey(q.SourceSectorId) || !_sectors.ContainsKey(q.DestinationSectorId)) return Rejected(q.TransferId, TransferRejection.UnknownSector, null);
        if (string.Equals(q.SourceSectorId, q.DestinationSectorId, StringComparison.Ordinal)) return Rejected(q.TransferId, TransferRejection.SameSector, null);
        if (!_routeAllowed(q.SourceSectorId, q.DestinationSectorId)) return Rejected(q.TransferId, TransferRejection.RouteNotAllowed, null);

        var fingerprint = Fingerprint(q);
        var gate = _locks.GetOrAdd(q.PlayerId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            PlayerPresence? p;
            try { p = await _journal.LoadAsync(q.PlayerId, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception) { return Unresolved(q.TransferId, TransferRejection.PersistenceFailed, null, null); }
            if (p is null) return Rejected(q.TransferId, TransferRejection.NotRegistered, null);

            // Duplicate lookup precedes state checks, so a retry gets the original outcome.
            if (p.Active is { } active && active.TransferId == q.TransferId)
                return active.Fingerprint == fingerprint ? await DriveAsync(p).ConfigureAwait(false) : Rejected(q.TransferId, TransferRejection.TransferIdReused, p);
            foreach (var done in p.Recent)
                if (done.TransferId == q.TransferId)
                    return done.Fingerprint == fingerprint ? Describe(p, done) with { IsReplay = true } : Rejected(q.TransferId, TransferRejection.TransferIdReused, p);

            if (p.Active is not null) return Rejected(q.TransferId, TransferRejection.TransferInProgress, p);
            if (!string.Equals(p.OwnerSectorId, q.SourceSectorId, StringComparison.Ordinal)) return Rejected(q.TransferId, TransferRejection.NotInSourceSector, p);

            var record = new TransferRecord(q.TransferId, q.PlayerId, q.SourceSectorId, q.DestinationSectorId, q.DestinationEntryId,
                fingerprint, TransferPhase.Preparing, p.Epoch);
            var (began, failure) = await WriteAsync(p, p with { Active = record }).ConfigureAwait(false);
            if (began is null) return Unresolved(q.TransferId, failure, p, null);
            return await DriveAsync(began).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    /// <summary>Finishes the player's in-flight transfer, if any: aborts it before the commit point, rolls it forward after.</summary>
    public async Task<SectorTransferResult?> RecoverAsync(string playerId, CancellationToken ct = default)
    {
        if (!ProfileIdRules.IsValid(playerId)) return null;
        var gate = _locks.GetOrAdd(playerId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            PlayerPresence? p;
            try { p = await _journal.LoadAsync(playerId, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception) { return Unresolved(string.Empty, TransferRejection.PersistenceFailed, null, null); }
            if (p?.Active is not { } active) return null;
            if (active.Phase.IsBeforeCommit()) return await AbortAsync(p, active, TransferRejection.RecoveredBeforeCommit).ConfigureAwait(false);
            return await DriveAsync(p).ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    /// <summary>Recovers every player the journal lists with an in-flight transfer, for example after a restart.</summary>
    public async Task<IReadOnlyList<SectorTransferResult>> RecoverAllAsync(CancellationToken ct = default)
    {
        var results = new List<SectorTransferResult>();
        foreach (var playerId in await _journal.ListInFlightAsync(ct).ConfigureAwait(false))
            if (await RecoverAsync(playerId, ct).ConfigureAwait(false) is { } r) results.Add(r);
        return results;
    }

    // ---- State machine -------------------------------------------------------------------------------------

    /// <summary>Advances the active record of <paramref name="p"/> until it is terminal or a step fails. Caller holds the lock.</summary>
    private async Task<SectorTransferResult> DriveAsync(PlayerPresence p)
    {
        while (p.Active is { } r)
        {
            var source = _sectors[r.SourceSectorId];
            var destination = _sectors[r.DestinationSectorId];
            switch (r.Phase)
            {
                case TransferPhase.Preparing:
                {
                    DetachOutcome detached;
                    try { detached = await source.DetachAsync(r.TransferId, r.PlayerId, r.SourceEpoch, CancellationToken.None).ConfigureAwait(false); }
                    catch (Exception) { return await AbortAsync(p, r, TransferRejection.ParticipantFailed).ConfigureAwait(false); }
                    if (!detached.Ok || detached.Manifest is null) return await AbortAsync(p, r, TransferRejection.SourceRefused).ConfigureAwait(false);

                    ReserveOutcome reserved;
                    try { reserved = await destination.ReserveAsync(r.TransferId, r.PlayerId, detached.Manifest, r.DestinationEntryId, CancellationToken.None).ConfigureAwait(false); }
                    catch (Exception) { return await AbortAsync(p, r, TransferRejection.ParticipantFailed).ConfigureAwait(false); }
                    if (!reserved.Ok || reserved.Spawn is not { IsValid: true }) return await AbortAsync(p, r, TransferRejection.DestinationRefused).ConfigureAwait(false);

                    var prepared = r with { Phase = TransferPhase.Prepared, Manifest = detached.Manifest, Spawn = reserved.Spawn };
                    var (next, failure) = await WriteAsync(p, p with { Active = prepared }).ConfigureAwait(false);
                    if (next is null) return Unresolved(r.TransferId, failure, p, r.Phase);
                    p = next;
                    break;
                }
                case TransferPhase.Prepared:
                {
                    // The commit point: one compare-and-swap write moves ownership and the decision together.
                    var committed = r with { Phase = TransferPhase.Committed };
                    var (next, failure) = await WriteAsync(p, p with { OwnerSectorId = r.DestinationSectorId, Epoch = r.SourceEpoch + 1, Active = committed })
                        .ConfigureAwait(false);
                    if (next is null) return Unresolved(r.TransferId, failure, p, r.Phase);
                    p = next;
                    break;
                }
                case TransferPhase.Committed:
                {
                    try
                    {
                        await destination.ActivateAsync(r.TransferId, r.PlayerId, p.Epoch, CancellationToken.None).ConfigureAwait(false);
                        await source.ReleaseAsync(r.TransferId, r.PlayerId, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception) { return Unresolved(r.TransferId, TransferRejection.ParticipantFailed, p, r.Phase); }
                    var (next, failure) = await FinishAsync(p, r with { Phase = TransferPhase.Completed }).ConfigureAwait(false);
                    if (next is null) return Unresolved(r.TransferId, failure, p, r.Phase);
                    p = next;
                    break;
                }
                case TransferPhase.Aborting:
                {
                    try
                    {
                        // Cancel first so the destination is fenced before the source ship becomes active again.
                        await destination.CancelAsync(r.TransferId, r.PlayerId, CancellationToken.None).ConfigureAwait(false);
                        await source.RestoreAsync(r.TransferId, r.PlayerId, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception) { return Unresolved(r.TransferId, TransferRejection.ParticipantFailed, p, r.Phase); }
                    var (next, failure) = await FinishAsync(p, r with { Phase = TransferPhase.Aborted }).ConfigureAwait(false);
                    if (next is null) return Unresolved(r.TransferId, failure, p, r.Phase);
                    p = next;
                    break;
                }
                default:
                    throw new InvalidOperationException($"Active transfer '{r.TransferId}' is in terminal phase {r.Phase}.");
            }
        }
        return Describe(p, p.Recent[^1]);
    }

    /// <summary>Journals the abort decision (only legal before the commit point), then compensates.</summary>
    private async Task<SectorTransferResult> AbortAsync(PlayerPresence p, TransferRecord r, TransferRejection reason)
    {
        var (next, failure) = await WriteAsync(p, p with { Active = r with { Phase = TransferPhase.Aborting, AbortReason = reason } }).ConfigureAwait(false);
        if (next is null) return Unresolved(r.TransferId, failure, p, r.Phase);
        return await DriveAsync(next).ConfigureAwait(false);
    }

    private Task<(PlayerPresence? Next, TransferRejection Failure)> FinishAsync(PlayerPresence p, TransferRecord terminal)
    {
        var recent = p.Recent.Add(terminal);
        if (recent.Count > _options.RecentWindow) recent = recent.RemoveRange(0, recent.Count - _options.RecentWindow);
        return WriteAsync(p, p with { Active = null, Recent = recent });
    }

    private async Task<(PlayerPresence? Next, TransferRejection Failure)> WriteAsync(PlayerPresence current, PlayerPresence next)
    {
        next = next with { Revision = current.Revision + 1 };
        JournalWriteStatus status;
        try { status = await _journal.SaveAsync(next, current.Revision, CancellationToken.None).ConfigureAwait(false); }
        catch (Exception) { return (null, TransferRejection.PersistenceFailed); }
        return status switch
        {
            JournalWriteStatus.Saved => (next, TransferRejection.None),
            JournalWriteStatus.RevisionConflict => (null, TransferRejection.PersistenceConflict),
            _ => (null, TransferRejection.PersistenceFailed),
        };
    }

    // ---- Results -------------------------------------------------------------------------------------------

    private static SectorTransferResult Describe(PlayerPresence p, TransferRecord r) => r.Phase switch
    {
        TransferPhase.Completed => new(r.TransferId, SectorTransferStatus.Completed, TransferRejection.None, false, r.DestinationSectorId,
            r.SourceEpoch + 1, r.Spawn, r.Phase),
        TransferPhase.Aborted => new(r.TransferId, SectorTransferStatus.Aborted, r.AbortReason, false, r.SourceSectorId, r.SourceEpoch, null, r.Phase),
        _ => new(r.TransferId, SectorTransferStatus.Unresolved, TransferRejection.None, false, p.OwnerSectorId, p.Epoch, null, r.Phase),
    };

    private static SectorTransferResult Rejected(string? transferId, TransferRejection reason, PlayerPresence? p) =>
        new(transferId ?? string.Empty, SectorTransferStatus.Rejected, reason, false, p?.OwnerSectorId, p?.Epoch ?? 0, null, null);

    private static SectorTransferResult Unresolved(string transferId, TransferRejection reason, PlayerPresence? p, TransferPhase? phase) =>
        new(transferId, SectorTransferStatus.Unresolved, reason, false, p?.OwnerSectorId, p?.Epoch ?? 0, null, phase);

    /// <summary>Lowercase hex SHA-256 over the length-prefixed request fields.</summary>
    private static string Fingerprint(SectorTransferRequest q)
    {
        static string L(string s) => s.Length.ToString(CultureInfo.InvariantCulture) + ":" + s;
        var sb = new StringBuilder("SectorTransfer|");
        sb.Append(L(q.PlayerId)).Append('|').Append(L(q.SourceSectorId)).Append('|').Append(L(q.DestinationSectorId)).Append('|').Append(L(q.DestinationEntryId));
        return ProfileSerializer.Sha256Hex(sb.ToString());
    }
}
