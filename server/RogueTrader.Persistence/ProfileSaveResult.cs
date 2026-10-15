namespace RogueTrader.Persistence;

/// <param name="StoredRevision">Revision now stored on success; otherwise the repository's tracked revision, or -1 if none is tracked.</param>
public sealed record ProfileSaveResult(ProfileSaveStatus Status, long StoredRevision, IReadOnlyList<PersistenceDiagnostic> Diagnostics);
