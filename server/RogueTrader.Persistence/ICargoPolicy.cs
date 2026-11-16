namespace RogueTrader.Persistence;

/// <summary>Capacity, weight and stacking rules live with the original data. Called only when any quantity increases.</summary>
public interface ICargoPolicy
{
    bool Fits(PlayerProfile current, IReadOnlyDictionary<string, int> proposedInventory);

    /// <summary>
    /// True for an item that stays with the pilot when the ship is destroyed (a bought item such as a space mine, Steve, Nov 3
    /// 2026: "all purchased items remain"); everything else in the hold is lost with the ship.
    /// </summary>
    bool KeptOnDestruction(string itemId) => false;
}
