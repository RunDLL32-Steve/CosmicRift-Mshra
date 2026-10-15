namespace RogueTrader.Persistence;

public sealed record ProfileLoadResult(ProfileLoadStatus Status, PlayerProfile? Profile, IReadOnlyList<PersistenceDiagnostic> Diagnostics)
{
    public bool Succeeded => Status is ProfileLoadStatus.Loaded or ProfileLoadStatus.RecoveredFromBackup;
}
