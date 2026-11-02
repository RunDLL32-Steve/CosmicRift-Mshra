namespace RogueTrader.Persistence;

/// <summary>
/// One conduct event (queue C9), for example a destroyed target, applied <paramref name="Count"/> times through the conduct rules.
/// The caller names only the event; the rules alone decide how bounty and reputation change.
/// </summary>
/// <param name="SourceId">What produced the event (an NPC, a sector, a station), recorded in the fingerprint.</param>
public sealed record RecordConductRequest(string TransactionId, string ProfileId, string SourceId, string EventId, int Count = 1);
