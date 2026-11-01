namespace RogueTrader.Persistence;

/// <summary>
/// Buys one repair service (for example an original Minor/Major/Full Repair effect row) at a station. The wallet is the only
/// persisted state it changes: the caller applies the repair to the ship itself, because hull health is simulation state and
/// the original amount/unit of a repair is undecoded (RT-F047).
/// </summary>
public sealed record RepairRequest(string TransactionId, string ProfileId, string StationId, string RepairId);
