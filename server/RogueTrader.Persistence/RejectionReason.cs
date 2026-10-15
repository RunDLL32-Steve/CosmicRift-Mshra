namespace RogueTrader.Persistence;

public enum RejectionReason
{
    None, InvalidRequest, IdentityMismatch, ProfileNotOpen, TransactionIdReused, StationAccessDenied,
    NotForSale, InvalidPrice, InsufficientInventory, InsufficientFunds, UpgradeAlreadyOwned, CargoCapacityExceeded,
    ArithmeticOverflow, PersistenceConflict, PersistenceFailed,
}
