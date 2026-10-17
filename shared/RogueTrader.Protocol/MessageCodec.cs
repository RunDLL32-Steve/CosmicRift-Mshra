using System.Buffers.Binary;

namespace RogueTrader.Protocol;

/// <summary>
/// Hand-written binary codec (no reflection). Decoding is total: any input yields either a message or a
/// <see cref="ProtocolError"/>; it never throws and never allocates based on an unchecked length field.
/// </summary>
public static class MessageCodec
{
    // ---------------------------------------------------------------- decode

    /// <summary>Decode one complete frame. On failure <paramref name="message"/> is null and <paramref name="error"/> is not None.</summary>
    public static bool TryDecode(ReadOnlySpan<byte> frame, out Message? message, out ProtocolError error)
    {
        message = null;
        error = Decode(frame, out var m);
        if (error != ProtocolError.None) return false;
        message = m;
        return true;
    }

    /// <summary>Throwing wrapper; only <see cref="ProtocolException"/> is ever raised.</summary>
    public static Message Decode(ReadOnlySpan<byte> frame)
    {
        if (!TryDecode(frame, out var m, out var e)) throw new ProtocolException(e);
        return m!;
    }

    private static ProtocolError Decode(ReadOnlySpan<byte> frame, out Message? message)
    {
        message = null;
        if (frame.Length < ProtocolLimits.HeaderBytes) return ProtocolError.Truncated;
        if (frame[0] != ProtocolLimits.Version) return ProtocolError.UnsupportedVersion;
        var type = (MessageType)frame[1];
        if (!Enum.IsDefined(type)) return ProtocolError.UnknownMessageType;
        var declared = BinaryPrimitives.ReadUInt32LittleEndian(frame[2..]);
        if (declared > ProtocolLimits.MaxPayloadBytes) return ProtocolError.PayloadTooLarge;
        var actual = frame.Length - ProtocolLimits.HeaderBytes;
        if (declared > (uint)actual) return ProtocolError.Truncated;
        if (declared < (uint)actual) return ProtocolError.TrailingBytes;

        var r = new WireReader(frame[ProtocolLimits.HeaderBytes..]);
        Message? m = type switch
        {
            MessageType.PlayerInput => ReadInput(ref r),
            MessageType.WorldSnapshot => ReadSnapshot(ref r),
            MessageType.TransactionRequest => ReadRequest(ref r),
            MessageType.TransactionResponse => ReadResponse(ref r),
            _ => null,
        };
        if (!r.Ok) return r.Error;
        if (r.Remaining != 0) return ProtocolError.TrailingBytes;
        message = m;
        return ProtocolError.None;
    }

    private static Message? ReadInput(ref WireReader r)
    {
        var clientTick = r.U32();
        var seq = r.U32();
        var snap = r.U32();
        var thrust = r.F32();
        var turn = r.F32();
        var flags = r.U8();
        if (!r.Ok) return null;
        if ((flags & ~(byte)InputFlags.Fire) != 0) { r.Fail(ProtocolError.InvalidFlags); return null; }
        return new PlayerInput(clientTick, seq, snap, thrust, turn, (InputFlags)flags);
    }

    private static Message? ReadSnapshot(ref WireReader r)
    {
        var tick = r.U32();
        var ack = r.U32();
        var count = r.U16();
        if (!r.Ok) return null;
        if (count > ProtocolLimits.MaxSnapshotEntities) { r.Fail(ProtocolError.TooManyEntities); return null; }
        // Validate the claimed count against the bytes actually present before allocating.
        if ((long)count * ProtocolLimits.EntityBytes > r.Remaining) { r.Fail(ProtocolError.Truncated); return null; }
        var entities = new EntityState[count];
        for (var i = 0; i < count; i++)
        {
            var id = r.U32();
            var kind = r.U8();
            var x = r.F32(); var y = r.F32(); var vx = r.F32(); var vy = r.F32();
            var rot = r.F32(); var hull = r.F32(); var shield = r.F32();
            if (!r.Ok) return null;
            if (!Enum.IsDefined((EntityKind)kind)) { r.Fail(ProtocolError.InvalidEnumValue); return null; }
            entities[i] = new EntityState(id, (EntityKind)kind, x, y, vx, vy, rot, hull, shield);
        }
        return new WorldSnapshot(tick, ack, entities);
    }

    private static Message? ReadRequest(ref WireReader r)
    {
        var id = r.U64();
        var kind = r.U8();
        var item = r.String(ProtocolLimits.MaxItemIdBytes);
        var qty = r.U32();
        if (!r.Ok) return null;
        if (!Enum.IsDefined((TransactionKind)kind)) { r.Fail(ProtocolError.InvalidEnumValue); return null; }
        return new TransactionRequest(id, (TransactionKind)kind, item, qty);
    }

    private static Message? ReadResponse(ref WireReader r)
    {
        var id = r.U64();
        var status = r.U8();
        var reason = r.U16();
        var balance = r.I64();
        if (!r.Ok) return null;
        if (!Enum.IsDefined((TransactionStatus)status)) { r.Fail(ProtocolError.InvalidEnumValue); return null; }
        return new TransactionResponse(id, (TransactionStatus)status, reason, balance);
    }

    // ---------------------------------------------------------------- encode

    /// <summary>Encode into <paramref name="destination"/>. Returns false with a defined error for invalid messages or a too-small buffer.</summary>
    public static bool TryEncode(Message? message, Span<byte> destination, out int written, out ProtocolError error)
    {
        written = 0;
        if (message is null) { error = ProtocolError.InvalidArgument; return false; }
        if (destination.Length < ProtocolLimits.HeaderBytes) { error = ProtocolError.InvalidArgument; return false; }

        var w = new WireWriter(destination[ProtocolLimits.HeaderBytes..]);
        switch (message)
        {
            case PlayerInput m:
                w.U32(m.ClientTick); w.U32(m.Sequence); w.U32(m.LastSnapshotTick);
                w.F32(m.Thrust); w.F32(m.Turn);
                if (((byte)m.Flags & ~(byte)InputFlags.Fire) != 0) w.Fail(ProtocolError.InvalidFlags);
                w.U8((byte)m.Flags);
                break;
            case WorldSnapshot m:
                if (m.Entities is null) { w.Fail(ProtocolError.InvalidArgument); break; }
                if (m.Entities.Length > ProtocolLimits.MaxSnapshotEntities) { w.Fail(ProtocolError.TooManyEntities); break; }
                w.U32(m.ServerTick); w.U32(m.LastProcessedInputSequence); w.U16((ushort)m.Entities.Length);
                foreach (var e in m.Entities)
                {
                    if (!Enum.IsDefined(e.Kind)) { w.Fail(ProtocolError.InvalidEnumValue); break; }
                    w.U32(e.EntityId); w.U8((byte)e.Kind);
                    w.F32(e.X); w.F32(e.Y); w.F32(e.VelX); w.F32(e.VelY);
                    w.F32(e.Rotation); w.F32(e.Hull); w.F32(e.Shield);
                }
                break;
            case TransactionRequest m:
                if (!Enum.IsDefined(m.Kind)) { w.Fail(ProtocolError.InvalidEnumValue); break; }
                w.U64(m.RequestId); w.U8((byte)m.Kind);
                w.String(m.ItemId, ProtocolLimits.MaxItemIdBytes);
                w.U32(m.Quantity);
                break;
            case TransactionResponse m:
                if (!Enum.IsDefined(m.Status)) { w.Fail(ProtocolError.InvalidEnumValue); break; }
                w.U64(m.RequestId); w.U8((byte)m.Status); w.U16(m.ReasonCode); w.I64(m.Balance);
                break;
            default:
                w.Fail(ProtocolError.UnknownMessageType);
                break;
        }
        if (!w.Ok) { error = w.Error; return false; }

        destination[0] = ProtocolLimits.Version;
        destination[1] = (byte)message.Type;
        BinaryPrimitives.WriteUInt32LittleEndian(destination[2..], (uint)w.Length);
        written = ProtocolLimits.HeaderBytes + w.Length;
        error = ProtocolError.None;
        return true;
    }

    /// <summary>Throwing wrapper returning an exact-size frame; only <see cref="ProtocolException"/> is ever raised.</summary>
    public static byte[] Encode(Message message)
    {
        if (message is null) throw new ProtocolException(ProtocolError.InvalidArgument);
        var buffer = new byte[MaxEncodedSize(message)];
        if (!TryEncode(message, buffer, out var n, out var e)) throw new ProtocolException(e);
        return buffer.AsSpan(0, n).ToArray();
    }

    private static int MaxEncodedSize(Message m) => ProtocolLimits.HeaderBytes + m switch
    {
        PlayerInput => 21,
        WorldSnapshot s => 10 + ProtocolLimits.EntityBytes * Math.Min(s.Entities?.Length ?? 0, ProtocolLimits.MaxSnapshotEntities),
        TransactionRequest => 8 + 1 + 1 + ProtocolLimits.MaxItemIdBytes + 4,
        TransactionResponse => 19,
        _ => 0,
    };
}
