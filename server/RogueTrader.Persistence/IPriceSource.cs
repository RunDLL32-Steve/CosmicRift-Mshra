namespace RogueTrader.Persistence;

/// <summary>Authoritative prices; false means not traded here. Called once per transaction, inside the profile lock.</summary>
public interface IPriceSource
{
    bool TryGetBuyUnitPrice(string stationId, string itemId, out long unitPrice);
    bool TryGetSellUnitPrice(string stationId, string itemId, out long unitPrice);
    bool TryGetUpgradePrice(string stationId, string upgradeId, out long price);

    /// <summary>Price of one repair service; false means the service is not offered here. Not offered unless a source overrides it.</summary>
    bool TryGetRepairPrice(string stationId, string repairId, out long price)
    {
        price = 0;
        return false;
    }
}
