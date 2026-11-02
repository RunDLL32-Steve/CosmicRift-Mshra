namespace RogueTrader.Persistence;

/// <summary>
/// Converts <see cref="Quantity"/> units of a recipe's input item into the same number of its output item at a refinery
/// (queue M7 mining slice). The recipe's items come from the price source; the one-for-one, free conversion is NEW/provisional
/// because the original refinery quantities, costs and timing are undecoded (RT-F044).
/// </summary>
public sealed record RefineRequest(string TransactionId, string ProfileId, string StationId, string RecipeId, int Quantity);
