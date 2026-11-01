namespace RogueTrader.Persistence;

public enum RejectionReason
{
    None, InvalidRequest, IdentityMismatch, ProfileNotOpen, TransactionIdReused, StationAccessDenied,
    NotForSale, InvalidPrice, InsufficientInventory, InsufficientFunds, UpgradeAlreadyOwned, CargoCapacityExceeded,
    ArithmeticOverflow, PersistenceConflict, PersistenceFailed,
    /// <summary>Refused by the station session before the core: the ship is at full hull or destroyed (queue M8 review R1-1).</summary>
    NothingToRepair,
}
