namespace RogueTrader.Persistence;

public sealed record BuyUpgradeRequest(string TransactionId, string ProfileId, string StationId, string UpgradeId);
