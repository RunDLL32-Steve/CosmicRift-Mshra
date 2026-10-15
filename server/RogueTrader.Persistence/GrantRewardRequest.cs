namespace RogueTrader.Persistence;

/// <summary>Mined cargo collection and NPC rewards both use this request.</summary>
public sealed record GrantRewardRequest(string TransactionId, string ProfileId, string SourceId, long Currency,
    IReadOnlyDictionary<string, int> Items);
