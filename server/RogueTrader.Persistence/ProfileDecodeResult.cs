namespace RogueTrader.Persistence;

/// <summary>Outcome of <see cref="ProfileSerializer.Decode"/>. <see cref="Failure"/> is the first failing diagnostic code (null on success).</summary>
public sealed record ProfileDecodeResult(PlayerProfile? Profile, DiagnosticCode? Failure, IReadOnlyList<PersistenceDiagnostic> Diagnostics)
{
    public bool Succeeded => Profile is not null;
}
