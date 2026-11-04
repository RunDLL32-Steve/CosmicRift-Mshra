using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Text;

namespace RogueTrader.OriginalData;

/// <summary>A malformed BLO header, directory, or payload span.</summary>
public sealed class BloArchiveFormatException : OriginalDataException
{
    public BloArchiveFormatException(string sourcePath, string message)
        : base($"BLO '{sourcePath}': {message}") { }
}

/// <summary>An owned raw archive snapshot. No decompression, interpretation, or extraction is performed.</summary>
public sealed class BloArchive
{
    private readonly byte[] bytes;

    internal BloArchive(byte[] bytes, string sourcePath, uint version, int directoryEnd,
        string sha256, bool isVerified, BloMember[] members)
    {
        this.bytes = bytes;
        SourcePath = sourcePath;
        Version = version;
        DirectoryEnd = directoryEnd;
        Sha256 = sha256;
        IsVerified = isVerified;
        Members = Array.AsReadOnly(members);
    }

    public string SourcePath { get; }
    public uint Version { get; }
    public int DirectoryEnd { get; }
    public string Sha256 { get; }
    /// <summary>True only when ReadCanonical verified this snapshot against its complete canonical pin.</summary>
    public bool IsVerified { get; }
    public ReadOnlyCollection<BloMember> Members { get; }
    public byte[] CopyBytes() => (byte[])bytes.Clone();
}

/// <summary>
/// An original zero-based directory identity. RawNameField is authoritative, including bytes after NUL.
/// Duplicate, empty, and path-like diagnostic names remain distinct ordinal identities; never use Name as an extraction path.
/// </summary>
public sealed class BloMember
{
    private readonly byte[] archiveBytes;
    private readonly byte[] rawNameField;

    internal BloMember(byte[] archiveBytes, int ordinal, byte[] rawNameField, uint offset, uint length)
    {
        this.archiveBytes = archiveBytes;
        this.rawNameField = rawNameField;
        Ordinal = ordinal;
        Offset = offset;
        Length = length;
        var nul = Array.IndexOf(rawNameField, (byte)0);
        Name = Encoding.Latin1.GetString(rawNameField, 0, nul < 0 ? rawNameField.Length : nul);
        Sha256 = AssetIntegrity.Sha256Hex(archiveBytes.AsSpan((int)offset, (int)length));
    }

    public int Ordinal { get; }
    /// <summary>
    /// NEW/provisional byte-preserving Latin-1 diagnostic projection up to the first NUL (or full field).
    /// Canonical names are ASCII; historical non-ASCII encoding is unproven. No normalization is applied.
    /// </summary>
    public string Name { get; }
    public uint Offset { get; }
    public uint Length { get; }
    public string Sha256 { get; }
    public byte[] RawNameField => (byte[])rawNameField.Clone();
    public byte[] CopyPayload() => archiveBytes.AsSpan((int)Offset, (int)Length).ToArray();
}

/// <summary>Bounded, read-only BLO v1/v2 directory/member reader.</summary>
public static class BloArchiveReader
{
    /// <summary>Parses a copied caller snapshot. Always UNVERIFIED, even if its bytes happen to match an original.</summary>
    public static BloArchive Parse(ReadOnlySpan<byte> data, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);
        return ParseOwned(data.ToArray(), sourcePath, false, null);
    }

    /// <summary>
    /// Reads only an exact, ordinal canonical relative path from PinnedBloArchives.All.
    /// Unknown/rooted/traversal/case-changed paths fail before filesystem access. The actual owned bytes
    /// are length/SHA-256 verified before parsing; no arbitrary-path fallback exists.
    /// </summary>
    public static BloArchive ReadCanonical(string assetsRoot, string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        var pin = PinnedBloArchives.All.FirstOrDefault(p => string.Equals(p.RelativePath, relativePath, StringComparison.Ordinal));
        if (pin is null)
            throw new ArgumentException("An exact canonical BLO relative path is required.", nameof(relativePath));
        ArgumentException.ThrowIfNullOrEmpty(assetsRoot);
        var path = Path.Combine(assetsRoot, pin.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        byte[] bytes;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length != pin.Length)
                throw new AssetIntegrityException(relativePath, AssetIntegrityFailure.LengthMismatch,
                    pin.Length.ToString(), stream.Length.ToString());
            // Allocate only the pinned length, never a caller-controlled file length. The second
            // check covers a file changing after the initial length check, without unbounded reads.
            bytes = new byte[checked((int)pin.Length)];
            var read = 0;
            while (read < bytes.Length)
            {
                var n = stream.Read(bytes, read, bytes.Length - read);
                if (n == 0) break;
                read += n;
            }
            if (read != bytes.Length || stream.ReadByte() != -1)
                throw new AssetIntegrityException(relativePath, AssetIntegrityFailure.LengthMismatch,
                    pin.Length.ToString(), "file changed while reading");
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new AssetIntegrityException(relativePath, AssetIntegrityFailure.Missing, "file present", "missing: " + path);
        }
        AssetIntegrity.Verify(pin, bytes);
        return ParseOwned(bytes, relativePath, true, pin.Sha256);
    }

    private static BloArchive ParseOwned(byte[] bytes, string sourcePath, bool verified, string? verifiedHash)
    {
        void Fail(string message) => throw new BloArchiveFormatException(sourcePath, message);
        if (bytes.Length < 8) Fail("Truncated header.");
        var version = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0, 4));
        if (version is not (1 or 2)) Fail($"Unsupported version {version}.");
        var count = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4));
        var nameLength = version == 1 ? 14 : 32;
        var rowLength = nameLength + 8;
        var directoryEndWide = 8UL + (ulong)count * (ulong)rowLength;
        if (directoryEndWide > (ulong)bytes.Length) Fail("Truncated directory.");
        var directoryEnd = (int)directoryEndWide;
        var spans = new (int Ordinal, uint Offset, uint Length)[(int)count];
        for (var ordinal = 0; ordinal < spans.Length; ordinal++)
        {
            var row = 8 + ordinal * rowLength;
            var offset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(row + nameLength, 4));
            var length = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(row + nameLength + 4, 4));
            if ((ulong)offset < directoryEndWide || (ulong)offset + length > (ulong)bytes.Length)
                Fail($"Member {ordinal} has an out-of-bounds span or overlaps the directory.");
            spans[ordinal] = (ordinal, offset, length);
        }
        // Empty spans occupy no bytes, including when placed inside a nonempty span.
        var occupied = spans.Where(m => m.Length != 0).OrderBy(m => m.Offset).ToArray();
        for (var i = 1; i < occupied.Length; i++)
            if ((ulong)occupied[i - 1].Offset + occupied[i - 1].Length > occupied[i].Offset)
                Fail($"Members {occupied[i - 1].Ordinal} and {occupied[i].Ordinal} overlap.");
        // Construct/hash only after every span is accepted. Nonempty spans are now disjoint,
        // so the total payload bytes hashed cannot exceed this snapshot's byte length.
        var members = new BloMember[spans.Length];
        foreach (var span in spans)
        {
            var row = 8 + span.Ordinal * rowLength;
            members[span.Ordinal] = new BloMember(bytes, span.Ordinal,
                bytes.AsSpan(row, nameLength).ToArray(), span.Offset, span.Length);
        }
        return new BloArchive(bytes, sourcePath, version, directoryEnd,
            verifiedHash ?? AssetIntegrity.Sha256Hex(bytes), verified, members);
    }
}
