namespace RogueTrader.Persistence;

/// <summary>
/// Converts <see cref="Quantity"/> units of a recipe's input item into the same number of its output item at a refinery
/// (queue M7 mining slice). The recipe's items come from the price source. One-for-one and free is the original shape of every
/// pinned VEH id 400 block (Quantity -1, PlayerItemNeededQuantity -1, Cost 0; RT-F044); the caller chooses the quantity (the
/// station session orders every unit held, as the original does) and the timing.
/// </summary>
public sealed record RefineRequest(string TransactionId, string ProfileId, string StationId, string RecipeId, int Quantity);
