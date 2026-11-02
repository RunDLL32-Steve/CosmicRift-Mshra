namespace RogueTrader.Persistence.Transfers;

/// <summary>
/// Phases of one sector transfer (queue C10, NEW engineering design). The commit point is the single journal write that moves
/// a record from <see cref="Prepared"/> to <see cref="Committed"/> and, in the same write, hands ownership to the destination.
/// </summary>
public enum TransferPhase
{
    /// <summary>Journalled intent. The source may have detached the ship and the destination may hold a reservation.</summary>
    Preparing,
    /// <summary>The source detached the ship and the destination reserved a validated spawn. Still before the commit point.</summary>
    Prepared,
    /// <summary>Decided: the destination owns the player. Recovery only rolls forward.</summary>
    Committed,
    /// <summary>Decided: the source keeps the player. Recovery only finishes the compensation.</summary>
    Aborting,
    /// <summary>Terminal: the destination activated the ship and the source released its frozen copy.</summary>
    Completed,
    /// <summary>Terminal: the source restored the ship and the destination dropped its reservation.</summary>
    Aborted,
}

public static class TransferPhaseExtensions
{
    public static bool IsTerminal(this TransferPhase phase) => phase is TransferPhase.Completed or TransferPhase.Aborted;

    /// <summary>True before the commit point, where recovery aborts.</summary>
    public static bool IsBeforeCommit(this TransferPhase phase) => phase is TransferPhase.Preparing or TransferPhase.Prepared;
}
