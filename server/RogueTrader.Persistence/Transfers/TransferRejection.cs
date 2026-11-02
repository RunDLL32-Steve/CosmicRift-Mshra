namespace RogueTrader.Persistence.Transfers;

public enum TransferRejection
{
    None,
    /// <summary>A malformed id or a missing field.</summary>
    InvalidRequest,
    /// <summary>The request names a player other than the authenticated one.</summary>
    IdentityMismatch,
    /// <summary>The source or destination sector is not hosted by this coordinator.</summary>
    UnknownSector,
    SameSector,
    /// <summary>The route policy refused this pair of sectors.</summary>
    RouteNotAllowed,
    /// <summary>The player has no presence record (never entered the world).</summary>
    NotRegistered,
    /// <summary>The journal says another sector owns the player.</summary>
    NotInSourceSector,
    /// <summary>A different transfer for this player is still in flight; recover it first.</summary>
    TransferInProgress,
    /// <summary>The transfer id was already used for a different request.</summary>
    TransferIdReused,
    /// <summary>The source sector refused to detach the ship (not present, busy, stale epoch or already decided).</summary>
    SourceRefused,
    /// <summary>The destination refused the arrival, including an invalid or unknown spawn entry.</summary>
    DestinationRefused,
    /// <summary>A sector participant threw.</summary>
    ParticipantFailed,
    /// <summary>Recovery found the transfer before its commit point and aborted it.</summary>
    RecoveredBeforeCommit,
    /// <summary>The journal changed underneath this coordinator (another coordinator recovered or wrote this player).</summary>
    PersistenceConflict,
    /// <summary>The journal write threw or refused the document.</summary>
    PersistenceFailed,
}
