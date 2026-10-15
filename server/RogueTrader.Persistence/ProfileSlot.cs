namespace RogueTrader.Persistence;

/// <summary>Per-profile serialization point: the lock plus the last committed state.</summary>
internal sealed class ProfileSlot
{
    public SemaphoreSlim Lock { get; } = new(1, 1);
    public volatile PlayerProfile? Committed;
    public volatile bool NeedsReload;
}
