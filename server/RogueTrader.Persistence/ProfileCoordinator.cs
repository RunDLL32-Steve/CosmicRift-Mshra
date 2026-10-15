using System.Collections.Concurrent;

namespace RogueTrader.Persistence;

/// <summary>Per-profile lock plus the tracked revision used for compare-and-swap, shared by the repository implementations.</summary>
/// <remarks>
/// <paramref name="lockComparer"/> decides which ids share a lock. The file repository passes
/// <see cref="StringComparer.OrdinalIgnoreCase"/> because ids differing only in case map to the same files on a
/// case-insensitive file system; tracked revisions always stay ordinal.
/// </remarks>
internal sealed class ProfileCoordinator(StringComparer? lockComparer = null) : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(lockComparer ?? StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _tracked = new(StringComparer.Ordinal);

    public async Task<Releaser> LockAsync(string profileId, CancellationToken ct)
    {
        var gate = _locks.GetOrAdd(profileId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        return new Releaser(gate);
    }

    public bool TryGetTracked(string profileId, out long revision) => _tracked.TryGetValue(profileId, out revision);

    public void SetTracked(string profileId, long revision) => _tracked[profileId] = revision;

    /// <summary>True only when a revision is tracked, equals <paramref name="expected"/>, and next is exactly one ahead.</summary>
    public bool CasHolds(string profileId, long expected, long nextRevision) =>
        _tracked.TryGetValue(profileId, out var tracked) && tracked == expected && nextRevision == expected + 1;

    public long TrackedOrMinusOne(string profileId) => _tracked.TryGetValue(profileId, out var r) ? r : -1;

    public void Dispose()
    {
        foreach (var gate in _locks.Values) gate.Dispose();
    }

    public readonly struct Releaser(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }
}
