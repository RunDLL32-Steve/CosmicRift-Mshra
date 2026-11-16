namespace RogueTrader.Persistence;

/// <summary>
/// Takes <see cref="Quantity"/> units of <see cref="ItemId"/> out of the hold because the ship used them in space (a laid space
/// mine, RT-F038). <see cref="SourceId"/> names the use for the ledger. Like a pickup grant it needs no station, and a stable
/// transaction id makes a retried or replayed use count once.
/// </summary>
public sealed record UseItemsRequest(string TransactionId, string ProfileId, string SourceId, string ItemId, int Quantity);
