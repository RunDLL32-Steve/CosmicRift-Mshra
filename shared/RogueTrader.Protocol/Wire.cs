using System.Buffers.Binary;
using System.Text;

namespace RogueTrader.Protocol;

internal ref struct WireReader
{
    internal static readonly UTF8Encoding Strict = new(false, true);

    private readonly ReadOnlySpan<byte> _data;
    private int _pos;
    public ProtocolError Error { get; private set; }

    public WireReader(ReadOnlySpan<byte> data) { _data = data; _pos = 0; Error = ProtocolError.None; }

    public readonly int Remaining => _data.Length - _pos;
    public readonly bool Ok => Error == ProtocolError.None;

    public void Fail(ProtocolError e) { if (Error == ProtocolError.None) Error = e; }

    private bool Need(int n)
    {
        if (!Ok) return false;
        if (n < 0 || Remaining < n) { Fail(ProtocolError.Truncated); return false; }
        return true;
    }

    public byte U8() { if (!Need(1)) return 0; return _data[_pos++]; }
    public ushort U16() { if (!Need(2)) return 0; var v = BinaryPrimitives.ReadUInt16LittleEndian(_data[_pos..]); _pos += 2; return v; }
    public uint U32() { if (!Need(4)) return 0; var v = BinaryPrimitives.ReadUInt32LittleEndian(_data[_pos..]); _pos += 4; return v; }
    public ulong U64() { if (!Need(8)) return 0; var v = BinaryPrimitives.ReadUInt64LittleEndian(_data[_pos..]); _pos += 8; return v; }
    public long I64() { if (!Need(8)) return 0; var v = BinaryPrimitives.ReadInt64LittleEndian(_data[_pos..]); _pos += 8; return v; }

    public float F32()
    {
        if (!Need(4)) return 0;
        var v = BinaryPrimitives.ReadSingleLittleEndian(_data[_pos..]); _pos += 4;
        if (!float.IsFinite(v)) { Fail(ProtocolError.NonFiniteValue); return 0; }
        return v;
    }

    /// <summary>Length-prefixed (u8) strict UTF-8, non-empty, at most <paramref name="maxBytes"/> bytes.</summary>
    public string String(int maxBytes)
    {
        var len = U8();
        if (!Ok) return string.Empty;
        if (len == 0) { Fail(ProtocolError.InvalidString); return string.Empty; }
        if (len > maxBytes) { Fail(ProtocolError.StringTooLong); return string.Empty; }
        if (!Need(len)) return string.Empty;
        try
        {
            var s = Strict.GetString(_data.Slice(_pos, len));
            _pos += len;
            return s;
        }
        catch (DecoderFallbackException)
        {
            Fail(ProtocolError.InvalidString);
            return string.Empty;
        }
    }
}

internal ref struct WireWriter
{
    private readonly Span<byte> _dest;
    private int _pos;
    public ProtocolError Error { get; private set; }

    public WireWriter(Span<byte> dest) { _dest = dest; _pos = 0; Error = ProtocolError.None; }

    public readonly int Length => _pos;
    public readonly bool Ok => Error == ProtocolError.None;
    public void Fail(ProtocolError e) { if (Error == ProtocolError.None) Error = e; }

    private bool Need(int n)
    {
        if (!Ok) return false;
        if (_dest.Length - _pos < n) { Fail(ProtocolError.InvalidArgument); return false; }
        return true;
    }

    public void U8(byte v) { if (Need(1)) _dest[_pos++] = v; }
    public void U16(ushort v) { if (Need(2)) { BinaryPrimitives.WriteUInt16LittleEndian(_dest[_pos..], v); _pos += 2; } }
    public void U32(uint v) { if (Need(4)) { BinaryPrimitives.WriteUInt32LittleEndian(_dest[_pos..], v); _pos += 4; } }
    public void U64(ulong v) { if (Need(8)) { BinaryPrimitives.WriteUInt64LittleEndian(_dest[_pos..], v); _pos += 8; } }
    public void I64(long v) { if (Need(8)) { BinaryPrimitives.WriteInt64LittleEndian(_dest[_pos..], v); _pos += 8; } }

    public void F32(float v)
    {
        if (!float.IsFinite(v)) { Fail(ProtocolError.NonFiniteValue); return; }
        if (Need(4)) { BinaryPrimitives.WriteSingleLittleEndian(_dest[_pos..], v); _pos += 4; }
    }

    public void String(string? s, int maxBytes)
    {
        if (string.IsNullOrEmpty(s)) { Fail(ProtocolError.InvalidString); return; }
        // Each UTF-16 unit needs at least one UTF-8 byte, so this bound rejects before encoding.
        if (s.Length > maxBytes) { Fail(ProtocolError.StringTooLong); return; }
        byte[] bytes;
        try { bytes = WireReader.Strict.GetBytes(s); }
        catch (EncoderFallbackException) { Fail(ProtocolError.InvalidString); return; }
        if (bytes.Length > maxBytes) { Fail(ProtocolError.StringTooLong); return; }
        U8((byte)bytes.Length);
        if (Need(bytes.Length)) { bytes.CopyTo(_dest[_pos..]); _pos += bytes.Length; }
    }
}
