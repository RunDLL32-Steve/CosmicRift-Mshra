using System.Buffers.Binary;
using System.Text;
using RogueTrader.Protocol;

namespace RogueTrader.UnitTests;

public class ProtocolTests
{
    private static EntityState Ent(uint id, EntityKind k = EntityKind.Ship) =>
        new(id, k, 1.5f * id, -2.25f, 0.5f, -0.125f, 3.14159f, 100f, 25.5f);

    public static IEnumerable<object[]> Samples()
    {
        yield return new object[] { new PlayerInput(10, 7, 4, 1f, -0.5f, InputFlags.Fire) };
        yield return new object[] { new PlayerInput(0, 0, 0, 0f, 0f, InputFlags.None) };
        yield return new object[] { new WorldSnapshot(99, 7, Array.Empty<EntityState>()) };
        yield return new object[] { new WorldSnapshot(99, 7, new[] { Ent(1), Ent(2, EntityKind.Pickup), Ent(uint.MaxValue, EntityKind.Projectile) }) };
        yield return new object[] { new TransactionRequest(ulong.MaxValue, TransactionKind.Buy, "item-é中", 3) };
        yield return new object[] { new TransactionRequest(1, TransactionKind.Uninstall, new string('a', ProtocolLimits.MaxItemIdBytes), uint.MaxValue) };
        yield return new object[] { new TransactionResponse(42, TransactionStatus.Rejected, 17, long.MinValue) };
        yield return new object[] { new TransactionResponse(42, TransactionStatus.Accepted, 0, 1_000_000) };
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void RoundTripsAndIsCanonical(Message msg)
    {
        var bytes = MessageCodec.Encode(msg);
        Assert.Equal(ProtocolLimits.Version, bytes[0]);
        Assert.Equal((byte)msg.Type, bytes[1]);
        Assert.Equal((uint)(bytes.Length - ProtocolLimits.HeaderBytes), BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(2)));
        Assert.True(MessageCodec.TryDecode(bytes, out var back, out var err));
        Assert.Equal(ProtocolError.None, err);
        Assert.Equal(msg, back);
        Assert.Equal(bytes, MessageCodec.Encode(back!));
    }

    [Fact]
    public void WireFormatIsLittleEndianAndPinned()
    {
        var bytes = MessageCodec.Encode(new TransactionResponse(0x0102030405060708UL, TransactionStatus.Duplicate, 0x0A0B, 0x1112131415161718L));
        var expected = new byte[]
        {
            1, 4, 19, 0, 0, 0,
            0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
            2, 0x0B, 0x0A,
            0x18, 0x17, 0x16, 0x15, 0x14, 0x13, 0x12, 0x11,
        };
        Assert.Equal(expected, bytes);

        var input = MessageCodec.Encode(new PlayerInput(1, 2, 3, 1f, -1f, InputFlags.Fire));
        Assert.Equal(new byte[] { 1, 1, 21, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0x80, 0x3F, 0, 0, 0x80, 0xBF, 1 }, input);
    }

    [Fact]
    public void EveryTruncationIsRejectedWithTruncated()
    {
        foreach (var o in Samples())
        {
            var bytes = MessageCodec.Encode((Message)o[0]);
            for (var n = 0; n < bytes.Length; n++)
            {
                Assert.False(MessageCodec.TryDecode(bytes.AsSpan(0, n), out var m, out var e));
                Assert.Null(m);
                Assert.Equal(ProtocolError.Truncated, e);
            }
        }
    }

    [Fact]
    public void TrailingBytesAreRejected()
    {
        var bytes = MessageCodec.Encode(new PlayerInput(1, 2, 3, 0f, 0f, InputFlags.None));
        var longer = bytes.Concat(new byte[] { 0 }).ToArray();
        Assert.False(MessageCodec.TryDecode(longer, out _, out var e));
        Assert.Equal(ProtocolError.TrailingBytes, e);

        // Declared payload shorter than the frame body.
        var shortDecl = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(shortDecl.AsSpan(2), 20);
        Assert.False(MessageCodec.TryDecode(shortDecl, out _, out e));
        Assert.Equal(ProtocolError.TrailingBytes, e);

        // Declared payload covers the frame but the message needs fewer bytes.
        var resp = MessageCodec.Encode(new TransactionResponse(1, TransactionStatus.Accepted, 0, 0)).Concat(new byte[] { 9 }).ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(resp.AsSpan(2), 20);
        Assert.False(MessageCodec.TryDecode(resp, out _, out e));
        Assert.Equal(ProtocolError.TrailingBytes, e);
    }

    [Fact]
    public void HeaderValidation()
    {
        var good = MessageCodec.Encode(new PlayerInput(1, 2, 3, 0f, 0f, InputFlags.None));

        var badVersion = (byte[])good.Clone(); badVersion[0] = 2;
        Assert.False(MessageCodec.TryDecode(badVersion, out _, out var e));
        Assert.Equal(ProtocolError.UnsupportedVersion, e);

        foreach (byte t in new byte[] { 0, 5, 255 })
        {
            var badType = (byte[])good.Clone(); badType[1] = t;
            Assert.False(MessageCodec.TryDecode(badType, out _, out e));
            Assert.Equal(ProtocolError.UnknownMessageType, e);
        }
    }

    [Theory]
    [InlineData(ProtocolLimits.MaxPayloadBytes + 1)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)] // 0xFFFFFFFF
    public void OversizedLengthIsRejectedWithoutAllocating(int declared)
    {
        var frame = new byte[ProtocolLimits.HeaderBytes];
        frame[0] = ProtocolLimits.Version; frame[1] = (byte)MessageType.WorldSnapshot;
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(2), unchecked((uint)declared));
        var before = GC.GetAllocatedBytesForCurrentThread();
        var ok = MessageCodec.TryDecode(frame, out var m, out var e);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.False(ok);
        Assert.Null(m);
        Assert.Equal(ProtocolError.PayloadTooLarge, e);
        Assert.True(allocated < 1024, $"allocated {allocated}");
    }

    [Fact]
    public void ValidLengthButMissingPayloadIsTruncated()
    {
        var frame = new byte[ProtocolLimits.HeaderBytes];
        frame[0] = ProtocolLimits.Version; frame[1] = (byte)MessageType.PlayerInput;
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(2), ProtocolLimits.MaxPayloadBytes);
        Assert.False(MessageCodec.TryDecode(frame, out _, out var e));
        Assert.Equal(ProtocolError.Truncated, e);
    }

    private static byte[] SnapshotFrame(ushort claimedCount, int actualEntityBytes)
    {
        var payload = new byte[10 + actualEntityBytes];
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(8), claimedCount);
        var frame = new byte[ProtocolLimits.HeaderBytes + payload.Length];
        frame[0] = ProtocolLimits.Version; frame[1] = (byte)MessageType.WorldSnapshot;
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(2), (uint)payload.Length);
        payload.CopyTo(frame, ProtocolLimits.HeaderBytes);
        return frame;
    }

    [Fact]
    public void HostileSnapshotCountDoesNotDriveAllocation()
    {
        var tooMany = SnapshotFrame(ushort.MaxValue, 0);
        var claimsMaxButEmpty = SnapshotFrame(ProtocolLimits.MaxSnapshotEntities, 0);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.False(MessageCodec.TryDecode(tooMany, out _, out var e1));
        Assert.False(MessageCodec.TryDecode(claimsMaxButEmpty, out _, out var e2));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(ProtocolError.TooManyEntities, e1);
        Assert.Equal(ProtocolError.Truncated, e2);
        Assert.True(allocated < 1024, $"allocated {allocated}");
    }

    [Fact]
    public void SnapshotAtMaximumEntitiesFitsPayloadLimit()
    {
        var entities = Enumerable.Range(0, ProtocolLimits.MaxSnapshotEntities).Select(i => Ent((uint)i)).ToArray();
        var bytes = MessageCodec.Encode(new WorldSnapshot(1, 1, entities));
        Assert.True(bytes.Length <= ProtocolLimits.MaxFrameBytes);
        Assert.Equal(new WorldSnapshot(1, 1, entities), MessageCodec.Decode(bytes));

        Assert.False(MessageCodec.TryEncode(new WorldSnapshot(1, 1, new EntityState[ProtocolLimits.MaxSnapshotEntities + 1]),
            new byte[ProtocolLimits.MaxFrameBytes], out var n, out var e));
        Assert.Equal(0, n);
        Assert.Equal(ProtocolError.TooManyEntities, e);
    }

    [Fact]
    public void NonFiniteFloatsRejectedOnBothSides()
    {
        foreach (var bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Assert.False(MessageCodec.TryEncode(new PlayerInput(0, 0, 0, bad, 0f, InputFlags.None), new byte[64], out _, out var e));
            Assert.Equal(ProtocolError.NonFiniteValue, e);

            var frame = MessageCodec.Encode(new PlayerInput(0, 0, 0, 0f, 0f, InputFlags.None));
            BinaryPrimitives.WriteSingleLittleEndian(frame.AsSpan(18), bad);
            Assert.False(MessageCodec.TryDecode(frame, out _, out e));
            Assert.Equal(ProtocolError.NonFiniteValue, e);
        }
    }

    [Fact]
    public void ReservedFlagsAndBadEnumsRejected()
    {
        var input = MessageCodec.Encode(new PlayerInput(0, 0, 0, 0f, 0f, InputFlags.None));
        input[^1] = 0x02;
        Assert.False(MessageCodec.TryDecode(input, out _, out var e));
        Assert.Equal(ProtocolError.InvalidFlags, e);
        Assert.False(MessageCodec.TryEncode(new PlayerInput(0, 0, 0, 0f, 0f, (InputFlags)0x80), new byte[64], out _, out e));
        Assert.Equal(ProtocolError.InvalidFlags, e);

        var resp = MessageCodec.Encode(new TransactionResponse(1, TransactionStatus.Accepted, 0, 0));
        resp[ProtocolLimits.HeaderBytes + 8] = 77;
        Assert.False(MessageCodec.TryDecode(resp, out _, out e));
        Assert.Equal(ProtocolError.InvalidEnumValue, e);

        var snap = MessageCodec.Encode(new WorldSnapshot(1, 1, new[] { Ent(1) }));
        snap[ProtocolLimits.HeaderBytes + 10 + 4] = 200;
        Assert.False(MessageCodec.TryDecode(snap, out _, out e));
        Assert.Equal(ProtocolError.InvalidEnumValue, e);

        Assert.False(MessageCodec.TryEncode(new TransactionRequest(1, (TransactionKind)99, "x", 1), new byte[64], out _, out e));
        Assert.Equal(ProtocolError.InvalidEnumValue, e);
    }

    [Fact]
    public void StringRulesEnforced()
    {
        Assert.False(MessageCodec.TryEncode(new TransactionRequest(1, TransactionKind.Buy, "", 1), new byte[256], out _, out var e));
        Assert.Equal(ProtocolError.InvalidString, e);
        Assert.False(MessageCodec.TryEncode(new TransactionRequest(1, TransactionKind.Buy, new string('a', ProtocolLimits.MaxItemIdBytes + 1), 1), new byte[256], out _, out e));
        Assert.Equal(ProtocolError.StringTooLong, e);
        Assert.False(MessageCodec.TryEncode(new TransactionRequest(1, TransactionKind.Buy, new string('中', 30), 1), new byte[256], out _, out e));
        Assert.Equal(ProtocolError.StringTooLong, e);
        Assert.False(MessageCodec.TryEncode(new TransactionRequest(1, TransactionKind.Buy, "bad\uD800", 1), new byte[256], out _, out e));
        Assert.Equal(ProtocolError.InvalidString, e);

        var good = MessageCodec.Encode(new TransactionRequest(1, TransactionKind.Buy, "ab", 1));
        var lenIdx = ProtocolLimits.HeaderBytes + 9;

        var zero = (byte[])good.Clone(); zero[lenIdx] = 0;
        Assert.False(MessageCodec.TryDecode(zero, out _, out e));
        Assert.Equal(ProtocolError.InvalidString, e);

        var tooLong = (byte[])good.Clone(); tooLong[lenIdx] = 200;
        Assert.False(MessageCodec.TryDecode(tooLong, out _, out e));
        Assert.Equal(ProtocolError.StringTooLong, e);

        var invalidUtf8 = (byte[])good.Clone(); invalidUtf8[lenIdx + 1] = 0xFF;
        Assert.False(MessageCodec.TryDecode(invalidUtf8, out _, out e));
        Assert.Equal(ProtocolError.InvalidString, e);
    }

    [Fact]
    public void EncodeIntoSmallBufferReportsDefinedError()
    {
        var msg = new WorldSnapshot(1, 1, new[] { Ent(1) });
        var need = MessageCodec.Encode(msg).Length;
        for (var n = 0; n < need; n++)
        {
            Assert.False(MessageCodec.TryEncode(msg, new byte[n], out var written, out var e));
            Assert.Equal(0, written);
            Assert.Equal(ProtocolError.InvalidArgument, e);
        }
        Assert.True(MessageCodec.TryEncode(msg, new byte[need + 10], out var w, out _));
        Assert.Equal(need, w);
        Assert.False(MessageCodec.TryEncode(null, new byte[64], out _, out var e2));
        Assert.Equal(ProtocolError.InvalidArgument, e2);
    }

    [Fact]
    public void ThrowingWrappersOnlyThrowProtocolException()
    {
        Assert.Equal(ProtocolError.Truncated, Assert.Throws<ProtocolException>(() => MessageCodec.Decode(new byte[2])).Error);
        Assert.Equal(ProtocolError.InvalidArgument, Assert.Throws<ProtocolException>(() => MessageCodec.Encode(null!)).Error);
        Assert.Equal(ProtocolError.TooManyEntities, Assert.Throws<ProtocolException>(() =>
            MessageCodec.Encode(new WorldSnapshot(0, 0, new EntityState[ProtocolLimits.MaxSnapshotEntities + 1]))).Error);
    }

    [Fact]
    public void FuzzRandomBytesNeverThrowAndAcceptedFramesAreCanonical()
    {
        var rng = new Random(0xC5);
        var seeds = Samples().Select(o => MessageCodec.Encode((Message)o[0])).ToArray();
        var accepted = 0;
        for (var i = 0; i < 20000; i++)
        {
            byte[] frame;
            switch (i % 3)
            {
                case 0: // pure noise
                    frame = new byte[rng.Next(0, 80)];
                    rng.NextBytes(frame);
                    break;
                case 1: // noise behind a plausible header with a correct length
                {
                    var payload = new byte[rng.Next(0, 120)];
                    rng.NextBytes(payload);
                    frame = new byte[ProtocolLimits.HeaderBytes + payload.Length];
                    frame[0] = ProtocolLimits.Version; frame[1] = (byte)rng.Next(1, 5);
                    BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(2), (uint)payload.Length);
                    payload.CopyTo(frame, ProtocolLimits.HeaderBytes);
                    break;
                }
                default: // mutate a valid frame
                {
                    frame = (byte[])seeds[rng.Next(seeds.Length)].Clone();
                    for (var k = rng.Next(1, 4); k > 0; k--) frame[rng.Next(frame.Length)] = (byte)rng.Next(256);
                    if (rng.Next(4) == 0) Array.Resize(ref frame, rng.Next(0, frame.Length + 8));
                    break;
                }
            }

            var ok = MessageCodec.TryDecode(frame, out var m, out var e);
            if (ok)
            {
                accepted++;
                Assert.Equal(ProtocolError.None, e);
                Assert.Equal(frame, MessageCodec.Encode(m!));
            }
            else
            {
                Assert.Null(m);
                Assert.NotEqual(ProtocolError.None, e);
            }
        }
        Assert.True(accepted > 0);
    }
}
