using System.Collections.Immutable;

namespace RogueTrader.Persistence.Transfers;

public enum HoldingState
{
    /// <summary>Simulated in this sector.</summary>
    Active,
    /// <summary>Frozen by an undecided transfer out of this sector.</summary>
    Detached,
    /// <summary>Held inactive for an undecided transfer into this sector.</summary>
    Reserved,
}

/// <param name="TransferId">The transfer that detached, reserved or last activated this holding.</param>
/// <param name="Spawn">Where the ship sits or will arrive; null for a ship admitted without one.</param>
public sealed record SectorHolding(string PlayerId, TransferManifest Manifest, long Epoch, HoldingState State, string? TransferId,
    SpawnPlacement? Spawn);

/// <summary>Durable state of an <see cref="InMemorySectorParticipant"/>, used to rebuild it after a simulated restart.</summary>
public sealed record SectorParticipantSnapshot(string SectorId, ImmutableSortedDictionary<string, SpawnPlacement> Entries,
    ImmutableSortedDictionary<string, SectorHolding> Holdings, ImmutableSortedDictionary<string, bool> Decided);

/// <summary>
/// Reference sector participant that follows the <see cref="ISectorParticipant"/> contract over in-memory holdings. Entries are
/// the only valid spawn points; every entry and position a caller supplies is NEW test or tooling data.
/// </summary>
public sealed class InMemorySectorParticipant : ISectorParticipant
{
    private readonly object _gate = new();
    private readonly ImmutableSortedDictionary<string, SpawnPlacement> _entries;
    private readonly Dictionary<string, SectorHolding> _holdings = new(StringComparer.Ordinal);
    // Transfer id -> true when completed, false when aborted.
    private readonly Dictionary<string, bool> _decided = new(StringComparer.Ordinal);

    public InMemorySectorParticipant(string sectorId, IEnumerable<SpawnPlacement> entries)
    {
        if (!TransferInvariants.IdOk(sectorId)) throw new ArgumentException("Sector id must be non-empty and well-formed.", nameof(sectorId));
        ArgumentNullException.ThrowIfNull(entries);
        var builder = ImmutableSortedDictionary.CreateBuilder<string, SpawnPlacement>(StringComparer.Ordinal);
        foreach (var e in entries)
        {
            if (e is null || !e.IsValid) throw new ArgumentException("Every entry must be a valid spawn.", nameof(entries));
            builder.Add(e.EntryId, e);
        }
        SectorId = sectorId;
        _entries = builder.ToImmutable();
    }

    public static InMemorySectorParticipant FromSnapshot(SectorParticipantSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var sector = new InMemorySectorParticipant(snapshot.SectorId, snapshot.Entries.Values);
        foreach (var (id, h) in snapshot.Holdings) sector._holdings[id] = h;
        foreach (var (id, done) in snapshot.Decided) sector._decided[id] = done;
        return sector;
    }

    public string SectorId { get; }

    public SectorParticipantSnapshot Snapshot()
    {
        lock (_gate)
            return new SectorParticipantSnapshot(SectorId, _entries,
                _holdings.ToImmutableSortedDictionary(StringComparer.Ordinal), _decided.ToImmutableSortedDictionary(StringComparer.Ordinal));
    }

    public IReadOnlyList<SectorHolding> Holdings
    {
        get { lock (_gate) return _holdings.Values.OrderBy(h => h.PlayerId, StringComparer.Ordinal).ToArray(); }
    }

    public SectorHolding? GetHolding(string playerId)
    {
        lock (_gate) return _holdings.TryGetValue(playerId, out var h) ? h : null;
    }

    /// <summary>Places a player's ship active in this sector, for example on first entry to the world.</summary>
    public void Admit(string playerId, TransferManifest manifest, long epoch, SpawnPlacement? spawn = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        lock (_gate)
        {
            if (_holdings.ContainsKey(playerId)) throw new InvalidOperationException($"Sector '{SectorId}' already holds '{playerId}'.");
            _holdings[playerId] = new SectorHolding(playerId, manifest, epoch, HoldingState.Active, null, spawn);
        }
    }

    public Task<DetachOutcome> DetachAsync(string transferId, string playerId, long epoch, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_decided.ContainsKey(transferId)) return Task.FromResult(DetachOutcome.Refused("transfer already decided"));
            if (!_holdings.TryGetValue(playerId, out var h)) return Task.FromResult(DetachOutcome.Refused("player not in sector"));
            if (h.State == HoldingState.Detached && h.TransferId == transferId) return Task.FromResult(DetachOutcome.Detached(h.Manifest));
            if (h.State != HoldingState.Active) return Task.FromResult(DetachOutcome.Refused($"ship is {h.State}"));
            if (h.Epoch != epoch) return Task.FromResult(DetachOutcome.Refused($"stale epoch {epoch}; sector holds {h.Epoch}"));
            _holdings[playerId] = h with { State = HoldingState.Detached, TransferId = transferId };
            return Task.FromResult(DetachOutcome.Detached(h.Manifest));
        }
    }

    public Task<ReserveOutcome> ReserveAsync(string transferId, string playerId, TransferManifest manifest, string entryId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        lock (_gate)
        {
            if (_decided.ContainsKey(transferId)) return Task.FromResult(ReserveOutcome.Refused("transfer already decided"));
            if (entryId is null || !_entries.TryGetValue(entryId, out var spawn)) return Task.FromResult(ReserveOutcome.Refused($"unknown entry '{entryId}'"));
            if (_holdings.TryGetValue(playerId, out var h))
            {
                if (h.State == HoldingState.Reserved && h.TransferId == transferId)
                    return Task.FromResult(h.Manifest.Equals(manifest) && h.Spawn == spawn
                        ? ReserveOutcome.Reserved(spawn)
                        : ReserveOutcome.Refused("reservation differs from the retried request"));
                return Task.FromResult(ReserveOutcome.Refused($"player already present ({h.State})"));
            }
            // Epoch 0 marks an inactive reservation; Activate sets the committed epoch.
            _holdings[playerId] = new SectorHolding(playerId, manifest, 0, HoldingState.Reserved, transferId, spawn);
            return Task.FromResult(ReserveOutcome.Reserved(spawn));
        }
    }

    public Task ActivateAsync(string transferId, string playerId, long epoch, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_decided.TryGetValue(transferId, out var done) && !done) throw Violation("activate", transferId, "aborted");
            _decided[transferId] = true;
            if (_holdings.TryGetValue(playerId, out var h) && h.TransferId == transferId)
            {
                if (h.State == HoldingState.Reserved) _holdings[playerId] = h with { State = HoldingState.Active, Epoch = epoch };
                else if (h.State != HoldingState.Active || h.Epoch != epoch) throw Violation("activate", transferId, h.State.ToString());
                return Task.CompletedTask;
            }
            throw Violation("activate", transferId, "not reserved here");
        }
    }

    public Task ReleaseAsync(string transferId, string playerId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_decided.TryGetValue(transferId, out var done) && !done) throw Violation("release", transferId, "aborted");
            _decided[transferId] = true;
            if (_holdings.TryGetValue(playerId, out var h) && h.State == HoldingState.Detached && h.TransferId == transferId)
                _holdings.Remove(playerId);
            return Task.CompletedTask;
        }
    }

    public Task RestoreAsync(string transferId, string playerId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_decided.TryGetValue(transferId, out var done) && done) throw Violation("restore", transferId, "completed");
            _decided[transferId] = false;
            if (_holdings.TryGetValue(playerId, out var h) && h.State == HoldingState.Detached && h.TransferId == transferId)
                _holdings[playerId] = h with { State = HoldingState.Active };
            return Task.CompletedTask;
        }
    }

    public Task CancelAsync(string transferId, string playerId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (_decided.TryGetValue(transferId, out var done) && done) throw Violation("cancel", transferId, "completed");
            _decided[transferId] = false;
            if (_holdings.TryGetValue(playerId, out var h) && h.State == HoldingState.Reserved && h.TransferId == transferId)
                _holdings.Remove(playerId);
            return Task.CompletedTask;
        }
    }

    private InvalidOperationException Violation(string op, string transferId, string state) =>
        new($"Sector '{SectorId}' cannot {op} transfer '{transferId}': it is {state}.");
}
