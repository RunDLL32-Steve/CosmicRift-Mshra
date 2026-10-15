namespace RogueTrader.Persistence;

/// <summary>Capacity, weight and stacking rules live with the original data. Called only when any quantity increases.</summary>
public interface ICargoPolicy
{
    bool Fits(PlayerProfile current, IReadOnlyDictionary<string, int> proposedInventory);
}
