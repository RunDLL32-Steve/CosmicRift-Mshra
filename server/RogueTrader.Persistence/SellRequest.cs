namespace RogueTrader.Persistence;

public sealed record SellRequest(string TransactionId, string ProfileId, string StationId, string ItemId, int Quantity);
