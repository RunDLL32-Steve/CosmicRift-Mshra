using System.Collections.Immutable;

namespace RogueTrader.Persistence;

/// <summary>What one conduct transaction changed, after clamping: the bounty change and each reputation category's change.</summary>
public sealed record ConductDelta(string EventId, int Count, long BountyDelta, ImmutableSortedDictionary<string, long> ReputationDeltas,
    long BountyAfter);
