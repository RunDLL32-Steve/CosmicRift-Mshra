namespace RogueTrader.Persistence;

/// <summary>Buys one hull at a station (schema 6). The player owns at most one of each hull; the price comes from the station.</summary>
public sealed record BuyShipRequest(string TransactionId, string ProfileId, string StationId, string HullId);
