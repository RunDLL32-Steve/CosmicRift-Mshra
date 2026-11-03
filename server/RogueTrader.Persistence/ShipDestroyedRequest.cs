namespace RogueTrader.Persistence;

/// <summary>
/// Records that the active ship was destroyed (schema 6, Steve, Nov 3 2026): the ship respawns at the spawn with a full hull,
/// keeps its weapons and upgrades, loses the cargo in its hold, and the player pays <see cref="TransactionProcessor.DestructionPenaltyPercent"/>% of
/// the credits they hold. The transaction id names one destruction, so a retry or replay never charges twice.
/// </summary>
public sealed record ShipDestroyedRequest(string TransactionId, string ProfileId, string HullId);
