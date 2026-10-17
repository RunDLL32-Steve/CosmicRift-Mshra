namespace RogueTrader.Protocol;

/// <summary>
/// Wire constants. Frame layout (all integers little-endian, floats IEEE-754 binary32 little-endian):
/// [version u8][type u8][payload length u32][payload...]. The frame must be exactly header + payload.
/// Every size limit below is a NEW/provisional value, not derived from original Rogue data.
/// World units are the NEW/provisional simulation units; they are not original game units.
/// </summary>
public static class ProtocolLimits
{
    /// <summary>Explicit wire version byte. Bump on any incompatible layout change.</summary>
    public const byte Version = 1;
    public const int HeaderBytes = 6;
    /// <summary>NEW/provisional: largest accepted payload (checked before any allocation).</summary>
    public const int MaxPayloadBytes = 65535;
    /// <summary>NEW/provisional: most entities in one snapshot (512 * 33 B = 16,896 B).</summary>
    public const int MaxSnapshotEntities = 512;
    /// <summary>NEW/provisional: longest item id in UTF-8 bytes.</summary>
    public const int MaxItemIdBytes = 64;
    public const int MaxFrameBytes = HeaderBytes + MaxPayloadBytes;
    /// <summary>id u32 + kind u8 + 7 floats.</summary>
    internal const int EntityBytes = 33;
}
