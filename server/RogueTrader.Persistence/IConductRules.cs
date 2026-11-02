using System.Collections.Immutable;

namespace RogueTrader.Persistence;

/// <summary>Bounty and reputation rules the transaction processor applies to <see cref="RecordConductRequest"/> (queue C9).</summary>
public interface IConductRules
{
    /// <summary>
    /// The state after <paramref name="eventId"/> happens <paramref name="count"/> times, or false for an event the rules do not
    /// define. Must be pure: the same inputs always give the same result.
    /// </summary>
    bool TryApply(string eventId, int count, long bounty, ImmutableSortedDictionary<string, long> reputation,
        out long bountyAfter, out ImmutableSortedDictionary<string, long> reputationAfter);
}
