using RogueTrader.Persistence;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>Result of one <see cref="PickupCommitQueue.CommitAsync"/> pass.</summary>
/// <param name="Applied">Pickups newly saved in this pass, oldest first.</param>
/// <param name="Replayed">Pickups the profile already held (a lost acknowledgement): the hold must be rebuilt from the profile.</param>
/// <param name="Refused">Pickups the core refused for good (for example a full hold); their units are gone.</param>
/// <param name="Blocked">Why the pass stopped with pickups still queued (persistence or profile trouble); null when the queue drained.</param>
public sealed record PickupCommitOutcome(IReadOnlyList<CargoPickup> Applied, IReadOnlyList<CargoPickup> Replayed,
    IReadOnlyList<(CargoPickup Pickup, RejectionReason Reason)> Refused, RejectionReason? Blocked);

/// <summary>
/// Queue M7 mining slice: every pickup a world records is granted to the profile once, through the idempotent GrantReward core,
/// with a stable id per world instance and pickup sequence (see <see cref="StationProfileSession.PickupTransactionId"/>). The
/// queue holds pickups not yet saved, so the ship's hold is always the saved inventory plus <see cref="PendingUnits"/>; a
/// refusal or a reload keeps them queued for the next pass. Godot-free so the client's flow is unit-tested.
/// </summary>
public sealed class PickupCommitQueue
{
    private readonly List<(string WorldInstanceId, CargoPickup Pickup)> _pending = [];
    private int _seen;

    public int Count => _pending.Count;

    /// <summary>Queues the pickups the world recorded since the last call.</summary>
    /// <param name="worldInstanceId">
    /// Unique per <see cref="WorldSimulation"/> instance (pickup sequences restart at 0 in every world), not per sector: a sector
    /// that keeps its identity across a reset still needs a new world id, or the second world's pickups would repeat old ids.
    /// </param>
    public void Observe(string worldInstanceId, IReadOnlyList<CargoPickup> pickups)
    {
        ArgumentException.ThrowIfNullOrEmpty(worldInstanceId);
        ArgumentNullException.ThrowIfNull(pickups);
        for (; _seen < pickups.Count; _seen++) _pending.Add((worldInstanceId, pickups[_seen]));
    }

    /// <summary>
    /// Starts observing a new world. Returns the pickups of the previous one that were still unsaved; the caller should give
    /// them a last <see cref="CommitAsync"/> first, because whatever is returned here is lost with that sector instance.
    /// </summary>
    public IReadOnlyList<CargoPickup> StartNewWorld()
    {
        var lost = _pending.Select(p => p.Pickup).ToArray();
        _pending.Clear();
        _seen = 0;
        return lost;
    }

    /// <summary>Units held in the ship but not yet saved, by item (ordinal order).</summary>
    public IReadOnlyDictionary<string, int> PendingUnits()
    {
        var units = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var (_, pickup) in _pending) units[pickup.ItemId] = checked(units.GetValueOrDefault(pickup.ItemId) + pickup.Quantity);
        return units;
    }

    /// <summary>
    /// The saved inventory plus the queued pickups: what the ship's hold holds. A queued pickup whose transaction id is already in
    /// <paramref name="savedTransactionIds"/> was saved but its acknowledgement was lost; it is already in the inventory and is not
    /// counted again (review R1-2). The next commit pass recognises it as a replay and removes it from the queue.
    /// </summary>
    public IReadOnlyDictionary<string, int> HoldContents(IReadOnlyDictionary<string, int> savedInventory, IEnumerable<string>? savedTransactionIds = null)
    {
        ArgumentNullException.ThrowIfNull(savedInventory);
        var saved = new HashSet<string>(savedTransactionIds ?? [], StringComparer.Ordinal);
        var hold = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var (item, quantity) in savedInventory) if (quantity > 0) hold[item] = quantity;
        foreach (var (world, pickup) in _pending)
            if (!saved.Contains(StationProfileSession.PickupTransactionId(world, pickup.Sequence)))
                hold[pickup.ItemId] = checked(hold.GetValueOrDefault(pickup.ItemId) + pickup.Quantity);
        return hold;
    }

    /// <summary>
    /// Grants queued pickups oldest first. A saved or already-saved pickup leaves the queue; a refusal that cannot succeed later
    /// (invalid request, full hold, overflow, id reused) drops it; anything else stops the pass and keeps it queued for a retry
    /// with the same id.
    /// </summary>
    public async Task<PickupCommitOutcome> CommitAsync(StationProfileSession session, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        var applied = new List<CargoPickup>();
        var replayed = new List<CargoPickup>();
        var refused = new List<(CargoPickup, RejectionReason)>();
        while (_pending.Count > 0)
        {
            var (instance, pickup) = _pending[0];
            var result = await session.GrantPickupAsync(instance, pickup, ct).ConfigureAwait(false);
            if (result.Status == TransactionStatus.Applied)
            {
                (result.IsReplay ? replayed : applied).Add(pickup);
                _pending.RemoveAt(0);
                continue;
            }
            if (result.Reason is RejectionReason.InvalidRequest or RejectionReason.CargoCapacityExceeded
                or RejectionReason.ArithmeticOverflow or RejectionReason.TransactionIdReused)
            {
                refused.Add((pickup, result.Reason));
                _pending.RemoveAt(0);
                continue;
            }
            return new PickupCommitOutcome(applied, replayed, refused, result.Reason);
        }
        return new PickupCommitOutcome(applied, replayed, refused, null);
    }
}
