namespace RogueTrader.Persistence;

/// <summary>In-memory repository with the same compare-and-swap rules as the file repository. For tests and tooling.</summary>
public sealed class InMemoryProfileRepository(PersistenceOptions? options = null) : IProfileRepository
{
    private static readonly IReadOnlyList<PersistenceDiagnostic> NoDiagnostics = Array.Empty<PersistenceDiagnostic>();

    private readonly PersistenceOptions _options = options ?? new PersistenceOptions();
    private readonly ProfileCoordinator _coordinator = new();
    private readonly Dictionary<string, PlayerProfile> _store = new(StringComparer.Ordinal);

    public async Task<ProfileLoadResult> LoadAsync(string profileId, CancellationToken ct = default)
    {
        ProfileIdRules.ThrowIfInvalid(profileId, nameof(profileId));
        using (await _coordinator.LockAsync(profileId, ct).ConfigureAwait(false))
        {
            if (!_store.TryGetValue(profileId, out var profile))
                return new ProfileLoadResult(ProfileLoadStatus.NotFound, null, NoDiagnostics);
            _coordinator.SetTracked(profileId, profile.Revision);
            return new ProfileLoadResult(ProfileLoadStatus.Loaded, profile, NoDiagnostics);
        }
    }

    public async Task<ProfileSaveResult> CreateAsync(PlayerProfile initial, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(initial);
        if (!ProfileIdRules.IsValid(initial.ProfileId)) return Invalid("ProfileId is invalid.");
        using (await _coordinator.LockAsync(initial.ProfileId, ct).ConfigureAwait(false))
        {
            if (initial.Revision != 0) return Invalid("A new profile must have Revision 0.");
            var violations = ProfileInvariants.Validate(initial, _options);
            if (violations.Count > 0) return Invalid(string.Join("; ", violations));
            if (_store.ContainsKey(initial.ProfileId))
                return new ProfileSaveResult(ProfileSaveStatus.AlreadyExists, _coordinator.TrackedOrMinusOne(initial.ProfileId), NoDiagnostics);
            _store[initial.ProfileId] = initial;
            _coordinator.SetTracked(initial.ProfileId, 0);
            return new ProfileSaveResult(ProfileSaveStatus.Saved, 0, NoDiagnostics);
        }
    }

    public async Task<ProfileSaveResult> SaveAsync(PlayerProfile next, long expectedRevision, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(next);
        if (!ProfileIdRules.IsValid(next.ProfileId)) return Invalid("ProfileId is invalid.");
        using (await _coordinator.LockAsync(next.ProfileId, ct).ConfigureAwait(false))
        {
            if (!_coordinator.CasHolds(next.ProfileId, expectedRevision, next.Revision))
                return new ProfileSaveResult(ProfileSaveStatus.RevisionConflict, _coordinator.TrackedOrMinusOne(next.ProfileId), NoDiagnostics);
            var violations = ProfileInvariants.Validate(next, _options);
            if (violations.Count > 0) return Invalid(string.Join("; ", violations));
            _store[next.ProfileId] = next;
            _coordinator.SetTracked(next.ProfileId, next.Revision);
            return new ProfileSaveResult(ProfileSaveStatus.Saved, next.Revision, NoDiagnostics);
        }
    }

    private static ProfileSaveResult Invalid(string message) =>
        new(ProfileSaveStatus.Invalid, -1, new[] { new PersistenceDiagnostic(DiagnosticCode.InvariantViolation, message, null) });
}
