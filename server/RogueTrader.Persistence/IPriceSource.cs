namespace RogueTrader.Persistence;

/// <summary>Authoritative prices; false means not traded here. Called once per transaction, inside the profile lock.</summary>
public interface IPriceSource
{
    bool TryGetBuyUnitPrice(string stationId, string itemId, out long unitPrice);
    bool TryGetSellUnitPrice(string stationId, string itemId, out long unitPrice);
    bool TryGetUpgradePrice(string stationId, string upgradeId, out long price);

    /// <summary>
    /// Credits paid for taking one installed upgrade back (ITM f[15]; 0 is a legal 0-credit sale). False means it cannot be sold
    /// back here (raw -1, or not an upgrade of this store). Not offered unless a source overrides it.
    /// </summary>
    bool TryGetUpgradeSellPrice(string stationId, string upgradeId, out long price)
    {
        price = 0;
        return false;
    }

    /// <summary>
    /// The most units of an item a player may hold (|ITM f[13]|, emulator ScriptArena.handlePlayerShop); false means no limit
    /// (raw 0). Not limited unless a source overrides it.
    /// </summary>
    bool TryGetMaxQuantity(string itemId, out int maxQuantity)
    {
        maxQuantity = 0;
        return false;
    }

    /// <summary>Price of one repair service; false means the service is not offered here. Not offered unless a source overrides it.</summary>
    bool TryGetRepairPrice(string stationId, string repairId, out long price)
    {
        price = 0;
        return false;
    }

    /// <summary>Input and output item of one refinery recipe; false means the recipe is not offered here. Not offered unless a source overrides it.</summary>
    bool TryGetRefineRecipe(string stationId, string recipeId, out string inputItemId, out string outputItemId)
    {
        inputItemId = outputItemId = "";
        return false;
    }

    /// <summary>Price of one hull at this station (schema 6); false means the hull is not sold here. Not sold unless a source overrides it.</summary>
    bool TryGetShipPrice(string stationId, string hullId, out long price)
    {
        price = 0;
        return false;
    }

    /// <summary>Hulls of which the buyer must already own one to buy this hull here; false means no requirement.</summary>
    bool TryGetShipRequirement(string stationId, string hullId, out IReadOnlyCollection<string> requiresOneOf)
    {
        requiresOneOf = [];
        return false;
    }
}
