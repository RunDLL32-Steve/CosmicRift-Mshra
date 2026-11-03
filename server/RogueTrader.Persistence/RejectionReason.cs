namespace RogueTrader.Persistence;

public enum RejectionReason
{
    None, InvalidRequest, IdentityMismatch, ProfileNotOpen, TransactionIdReused, StationAccessDenied,
    NotForSale, InvalidPrice, InsufficientInventory, InsufficientFunds, UpgradeAlreadyOwned, CargoCapacityExceeded,
    ArithmeticOverflow, PersistenceConflict, PersistenceFailed,
    /// <summary>Refused by the station session before the core: the ship is at full hull or destroyed (queue M8 review R1-1).</summary>
    NothingToRepair,
    /// <summary>Refused by the station session: the upgrade has no implemented effect yet (review R2-1).</summary>
    UpgradeHasNoEffect,
    /// <summary>Refused by the station session: an owned upgrade already holds the slot and would stay active (review R2-1).</summary>
    UpgradeSlotHeld,
    /// <summary>The conduct rules do not define the event, the count is out of range, or no rules are configured (queue C9).</summary>
    UnknownConductEvent,
    /// <summary>The conduct event would change nothing (every value already at its bound), so nothing is saved or recorded (queue C9).</summary>
    NoConductChange,
    /// <summary>The player already owns this hull; a player owns at most one of each (Steve, Nov 3 2026).</summary>
    ShipAlreadyOwned,
    /// <summary>The player does not own this hull, so it cannot be selected.</summary>
    ShipNotOwned,
    /// <summary>The destroyed hull is not the ship the player is flying, so no penalty is taken.</summary>
    ShipNotActive,
}
