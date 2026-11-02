namespace RogueTrader.Persistence.Transfers;

/// <summary>
/// In-memory journal with the compare-and-swap and invariant rules a durable store must follow. It outlives the coordinators
/// built on it, which is how tests model a coordinator restart.
/// </summary>
public sealed class InMemoryTransferJournal(TransferOptions? options = null) : ITransferJournal
{
    private readonly TransferOptions _options = options ?? new TransferOptions();
    private readonly Dictionary<string, PlayerPresence> _store = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public Task<PlayerPresence?> LoadAsync(string playerId, CancellationToken ct = default)
    {
        lock (_gate) return Task.FromResult(_store.TryGetValue(playerId, out var p) ? p : null);
    }

    public Task<JournalWriteStatus> CreateAsync(PlayerPresence initial, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (initial.Revision != 0 || TransferInvariants.Validate(initial, _options).Count > 0) return Task.FromResult(JournalWriteStatus.Invalid);
        lock (_gate)
        {
            if (_store.ContainsKey(initial.PlayerId)) return Task.FromResult(JournalWriteStatus.AlreadyExists);
            _store[initial.PlayerId] = initial;
            return Task.FromResult(JournalWriteStatus.Saved);
        }
    }

    public Task<JournalWriteStatus> SaveAsync(PlayerPresence next, long expectedRevision, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(next);
        lock (_gate)
        {
            if (!_store.TryGetValue(next.PlayerId, out var stored) || stored.Revision != expectedRevision || next.Revision != expectedRevision + 1)
                return Task.FromResult(JournalWriteStatus.RevisionConflict);
            if (TransferInvariants.Validate(next, _options).Count > 0) return Task.FromResult(JournalWriteStatus.Invalid);
            _store[next.PlayerId] = next;
            return Task.FromResult(JournalWriteStatus.Saved);
        }
    }

    public Task<IReadOnlyList<string>> ListInFlightAsync(CancellationToken ct = default)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<string>>(_store.Values.Where(p => p.Active is not null).Select(p => p.PlayerId)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }
}
