namespace RogueTrader.Persistence.Transfers;

public enum JournalWriteStatus { Saved, RevisionConflict, AlreadyExists, Invalid }

/// <summary>
/// Durable per-player presence documents. Every write is a compare-and-swap on the document revision, so a second or stale
/// coordinator cannot overwrite a decision it did not read. A write that returns has either replaced the document or not.
/// </summary>
public interface ITransferJournal
{
    Task<PlayerPresence?> LoadAsync(string playerId, CancellationToken ct = default);

    /// <summary>Revision must be 0; fails with AlreadyExists if the player has a document.</summary>
    Task<JournalWriteStatus> CreateAsync(PlayerPresence initial, CancellationToken ct = default);

    /// <summary>Succeeds only when the stored revision equals <paramref name="expectedRevision"/> and <c>next.Revision == expectedRevision + 1</c>.</summary>
    Task<JournalWriteStatus> SaveAsync(PlayerPresence next, long expectedRevision, CancellationToken ct = default);

    /// <summary>Players whose document has an in-flight transfer, for recovery after a restart.</summary>
    Task<IReadOnlyList<string>> ListInFlightAsync(CancellationToken ct = default);
}
