namespace RogueTrader.Persistence;

/// <param name="AuthenticatedProfileId">Identity established by the caller (session/login layer).</param>
/// <param name="StationAccessVerified">Set by the caller after checking docking/proximity; the persistence layer only enforces that it is true.</param>
public sealed record TransactionContext(string AuthenticatedProfileId, bool StationAccessVerified);
