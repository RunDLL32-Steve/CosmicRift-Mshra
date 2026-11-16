namespace RogueTrader.Protocol;

public enum MessageType : byte
{
    PlayerInput = 1,
    WorldSnapshot = 2,
    TransactionRequest = 3,
    TransactionResponse = 4,
}

/// <summary>Base of all wire messages. The client is never trusted: no message carries prices, damage or granted items.</summary>
public abstract record Message
{
    public abstract MessageType Type { get; }
}

/// <summary>Input flag bits (NEW/provisional); all other bits are reserved and rejected.</summary>
[Flags]
public enum InputFlags : byte
{
    None = 0,
    Fire = 1,
    /// <summary>Shift boost (hyper thrust) held; it engages only while thrusting with a tick's energy (ShipMotion).</summary>
    Hyper = 2,
}

/// <summary>Client intent for one client tick. Thrust/Turn nominal range is [-1,1]; range policing is a server validation concern (TASK_QUEUE C8).</summary>
public sealed record PlayerInput(uint ClientTick, uint Sequence, uint LastSnapshotTick, float Thrust, float Turn, InputFlags Flags) : Message
{
    public override MessageType Type => MessageType.PlayerInput;
}

public enum EntityKind : byte
{
    Ship = 0,
    Asteroid = 1,
    Projectile = 2,
    Pickup = 3,
}

/// <summary>One replicated entity in NEW/provisional world units (position, velocity per second, rotation radians).</summary>
/// <param name="Energy">Reactor energy, which pays for the Shift boost (and, on an energy-shield hull, is also its shield).
/// Client prediction reads it to know whether the boost can engage; 0 means the boost is predicted off.</param>
public readonly record struct EntityState(
    uint EntityId, EntityKind Kind, float X, float Y, float VelX, float VelY, float Rotation, float Hull, float Shield,
    float Energy = 0);

/// <summary>Authoritative state at <see cref="ServerTick"/>; acknowledges inputs up to <see cref="LastProcessedInputSequence"/> for reconciliation.</summary>
public sealed record WorldSnapshot(uint ServerTick, uint LastProcessedInputSequence, EntityState[] Entities) : Message
{
    public override MessageType Type => MessageType.WorldSnapshot;

    public bool Equals(WorldSnapshot? other) =>
        other is not null && ServerTick == other.ServerTick &&
        LastProcessedInputSequence == other.LastProcessedInputSequence &&
        Entities.AsSpan().SequenceEqual(other.Entities);

    public override int GetHashCode() => HashCode.Combine(ServerTick, LastProcessedInputSequence, Entities.Length);
}

public enum TransactionKind : byte
{
    Buy = 0,
    Sell = 1,
    Install = 2,
    Uninstall = 3,
}

/// <summary>Request only names an item and quantity; the server decides price, legality and outcome. RequestId is an idempotency key.</summary>
public sealed record TransactionRequest(ulong RequestId, TransactionKind Kind, string ItemId, uint Quantity) : Message
{
    public override MessageType Type => MessageType.TransactionRequest;
}

public enum TransactionStatus : byte
{
    Accepted = 0,
    Rejected = 1,
    Duplicate = 2,
}

/// <summary>Server verdict. ReasonCode is an opaque NEW/provisional code (0 when accepted); Balance is the authoritative balance after the request.</summary>
public sealed record TransactionResponse(ulong RequestId, TransactionStatus Status, ushort ReasonCode, long Balance) : Message
{
    public override MessageType Type => MessageType.TransactionResponse;
}
