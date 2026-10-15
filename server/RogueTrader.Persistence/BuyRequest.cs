namespace RogueTrader.Persistence;

public sealed record BuyRequest(string TransactionId, string ProfileId, string StationId, string ItemId, int Quantity);
