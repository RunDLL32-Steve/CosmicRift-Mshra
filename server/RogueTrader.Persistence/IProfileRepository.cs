namespace RogueTrader.Persistence;

public interface IProfileRepository
{
    Task<ProfileLoadResult> LoadAsync(string profileId, CancellationToken ct = default);

    /// <summary>Revision must be 0; fails with AlreadyExists if any generation exists.</summary>
    Task<ProfileSaveResult> CreateAsync(PlayerProfile initial, CancellationToken ct = default);

    /// <summary>
    /// Compare-and-swap save: succeeds only if this repository's tracked revision for the profile equals
    /// <paramref name="expectedRevision"/> and <c>next.Revision == expectedRevision + 1</c>.
    /// </summary>
    Task<ProfileSaveResult> SaveAsync(PlayerProfile next, long expectedRevision, CancellationToken ct = default);
}
