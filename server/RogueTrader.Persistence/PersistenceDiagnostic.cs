namespace RogueTrader.Persistence;

public sealed record PersistenceDiagnostic(DiagnosticCode Code, string Message, string? FilePath);
