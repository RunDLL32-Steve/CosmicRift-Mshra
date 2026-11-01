namespace RogueTrader.Persistence;

/// <param name="Revision">Committed revision after the save, or the current revision on refusal (-1 when unknown).</param>
public sealed record ShipConditionResult(bool Saved, RejectionReason Reason, long Revision);
