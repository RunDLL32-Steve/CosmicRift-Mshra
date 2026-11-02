namespace RogueTrader.Persistence.Transfers;

public sealed record DetachOutcome(bool Ok, TransferManifest? Manifest, string? Reason)
{
    public static DetachOutcome Detached(TransferManifest manifest) => new(true, manifest, null);
    public static DetachOutcome Refused(string reason) => new(false, null, reason);
}

public sealed record ReserveOutcome(bool Ok, SpawnPlacement? Spawn, string? Reason)
{
    public static ReserveOutcome Reserved(SpawnPlacement spawn) => new(true, spawn, null);
    public static ReserveOutcome Refused(string reason) => new(false, null, reason);
}

/// <summary>
/// One sector's side of a transfer. The coordinator calls the source's Detach then the destination's Reserve before the
/// commit point; after the decision it calls Activate and Release (committed) or Restore and Cancel (aborted).
/// </summary>
/// <remarks>
/// Contract, which the coordinator's guarantees depend on:
/// <list type="bullet">
/// <item>Every call is idempotent per transfer id and must be durable before it returns: recovery repeats calls freely.</item>
/// <item>Detach freezes the ship (no longer simulated, cannot be damaged, traded or moved) and returns the same manifest on
/// every repeat. It refuses unless the sector holds the player active at the given epoch.</item>
/// <item>Reserve validates the entry and holds the arrival inactive. It refuses an invalid spawn.</item>
/// <item>Restore and Cancel record the transfer id as aborted even when nothing was detached or reserved yet, and a later
/// Detach or Reserve for that id must refuse. This fences a stale coordinator that is still running after recovery.</item>
/// <item>Activate and Release record the id as completed. Activate after an abort, or Restore after a completion, is a
/// contract violation and must throw rather than create or drop a copy.</item>
/// </list>
/// </remarks>
public interface ISectorParticipant
{
    string SectorId { get; }

    Task<DetachOutcome> DetachAsync(string transferId, string playerId, long epoch, CancellationToken ct = default);
    Task<ReserveOutcome> ReserveAsync(string transferId, string playerId, TransferManifest manifest, string entryId, CancellationToken ct = default);
    Task ActivateAsync(string transferId, string playerId, long epoch, CancellationToken ct = default);
    Task ReleaseAsync(string transferId, string playerId, CancellationToken ct = default);
    Task RestoreAsync(string transferId, string playerId, CancellationToken ct = default);
    Task CancelAsync(string transferId, string playerId, CancellationToken ct = default);
}
