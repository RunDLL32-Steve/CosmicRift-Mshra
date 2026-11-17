namespace RogueTrader.Persistence;

/// <summary>Sells an installed upgrade (or armour) back at a store for its original resale price (RT-F049).</summary>
public sealed record SellUpgradeRequest(string TransactionId, string ProfileId, string StationId, string UpgradeId);
