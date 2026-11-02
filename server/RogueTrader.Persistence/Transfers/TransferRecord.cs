using System.Collections.Immutable;

namespace RogueTrader.Persistence.Transfers;

/// <summary>Requested transfer of one player's ship from the sector that owns it to another sector.</summary>
/// <param name="DestinationEntryId">Opaque entry (gate or spawn point) id the destination validates.</param>
public sealed record SectorTransferRequest(string TransferId, string PlayerId, string SourceSectorId, string DestinationSectorId,
    string DestinationEntryId);

/// <param name="AuthenticatedPlayerId">Identity established by the session/login layer.</param>
public sealed record TransferContext(string AuthenticatedPlayerId);

/// <summary>The journalled state of one transfer.</summary>
/// <param name="SourceEpoch">Ownership epoch of the source when the transfer began; the commit writes <c>SourceEpoch + 1</c>.</param>
/// <param name="Manifest">What the source detached; set from <see cref="TransferPhase.Prepared"/> on.</param>
/// <param name="Spawn">Where the destination reserved the arrival; set from <see cref="TransferPhase.Prepared"/> on.</param>
public sealed record TransferRecord(string TransferId, string PlayerId, string SourceSectorId, string DestinationSectorId,
    string DestinationEntryId, string Fingerprint, TransferPhase Phase, long SourceEpoch, TransferManifest? Manifest = null,
    SpawnPlacement? Spawn = null, TransferRejection AbortReason = TransferRejection.None);

/// <summary>
/// One durable document per player: which sector owns the player, its fencing epoch, the in-flight transfer (at most one) and
/// a bounded window of finished transfers for duplicate detection. Saved with compare-and-swap on <see cref="Revision"/>.
/// </summary>
public sealed record PlayerPresence(string PlayerId, string OwnerSectorId, long Epoch, long Revision, TransferRecord? Active,
    ImmutableList<TransferRecord> Recent)
{
    public static PlayerPresence Create(string playerId, string ownerSectorId) =>
        new(playerId, ownerSectorId, 1, 0, null, ImmutableList<TransferRecord>.Empty);
}

public enum SectorTransferStatus
{
    /// <summary>The destination owns and has activated the ship.</summary>
    Completed,
    /// <summary>The source kept the ship; <see cref="SectorTransferResult.Reason"/> says why.</summary>
    Aborted,
    /// <summary>Refused before anything was journalled or changed.</summary>
    Rejected,
    /// <summary>
    /// The outcome is journalled or decided but not finished, or the journal write failed. Nothing is lost or duplicated:
    /// retry the same request or run recovery to finish it.
    /// </summary>
    Unresolved,
}

/// <param name="OwnerSectorId">Owner according to the last journal state this coordinator read or wrote.</param>
/// <param name="Phase">Phase of the record this result describes; null for a rejection.</param>
public sealed record SectorTransferResult(string TransferId, SectorTransferStatus Status, TransferRejection Reason, bool IsReplay,
    string? OwnerSectorId, long Epoch, SpawnPlacement? Spawn, TransferPhase? Phase);
