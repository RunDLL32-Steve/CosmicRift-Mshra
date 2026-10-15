namespace RogueTrader.Persistence;

public enum DiagnosticCode
{
    Truncated, MalformedJson, ChecksumMismatch, WrongFormat, InvariantViolation, NewerVersion,
    MigrationApplied, MigrationMissing, OrphanTempQuarantined, PrimaryQuarantined, BackupQuarantined,
    RecoveredFromBackup, IoFailure,
}
