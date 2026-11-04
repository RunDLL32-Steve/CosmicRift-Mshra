using System.Buffers.Binary;
using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

// All synthetic bytes, names and sizes in this file are NEW test fixtures, not historical values.
public sealed class BloArchiveTests
{
    private static byte[] Archive(uint version, params (byte[] Name, uint Offset, uint Length)[] rows)
    {
        var nameLength = version == 1 ? 14 : 32;
        var end = 8 + rows.Length * (nameLength + 8);
        var result = new byte[Math.Max(end, rows.Length == 0 ? end : (int)rows.Max(r => (ulong)r.Offset + r.Length))];
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(0, 4), version);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4, 4), (uint)rows.Length);
        for (var i = 0; i < rows.Length; i++)
        {
            var pos = 8 + i * (nameLength + 8);
            rows[i].Name.CopyTo(result, pos);
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(pos + nameLength, 4), rows[i].Offset);
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(pos + nameLength + 4, 4), rows[i].Length);
        }
        for (var i = end; i < result.Length; i++) result[i] = (byte)i;
        return result;
    }

    private static byte[] Name(string value) => Encoding.ASCII.GetBytes(value);

    [Theory]
    [InlineData(1u, 14)]
    [InlineData(2u, 32)]
    public void BothVersionsRetainRawNamesOrdinalAndPayload(uint version, int nameLength)
    {
        var end = 8 + 3 * (nameLength + 8);
        var raw = Enumerable.Repeat((byte)'X', nameLength).ToArray(); // no terminal NUL
        var trailing = new byte[nameLength];
        trailing[0] = (byte)'A'; trailing[2] = 0xff; // retain nonzero bytes after NUL
        var bytes = Archive(version, (raw, (uint)end, 2), (trailing, (uint)end + 2, 0), (Name("Z"), (uint)end + 4, 3));
        var archive = BloArchiveReader.Parse(bytes, "NEW/source.blo");
        Assert.False(archive.IsVerified);
        Assert.Equal(version, archive.Version);
        Assert.Equal(end, archive.DirectoryEnd);
        Assert.Equal("NEW/source.blo", archive.SourcePath);
        Assert.Equal(AssetIntegrity.Sha256Hex(bytes), archive.Sha256);
        Assert.Equal(new[] { 0, 1, 2 }, archive.Members.Select(m => m.Ordinal));
        Assert.Equal(new string('X', nameLength), archive.Members[0].Name);
        Assert.Equal("A", archive.Members[1].Name);
        Assert.Equal(trailing, archive.Members[1].RawNameField);
        foreach (var member in archive.Members)
        {
            var expected = bytes.AsSpan((int)member.Offset, (int)member.Length).ToArray();
            Assert.Equal(expected, member.CopyPayload());
            Assert.Equal(AssetIntegrity.Sha256Hex(expected), member.Sha256);
        }
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    public void DuplicateUnsafeEmptyAndNonAsciiNamesRemainRawOrdinalIdentities(uint version)
    {
        var end = (uint)(8 + 5 * ((version == 1 ? 14 : 32) + 8));
        var bytes = Archive(version, (Name("../x"), end, 1), (Name("../x"), end + 1, 1),
            (Name("C:\\x"), end + 2, 0), (Array.Empty<byte>(), end, 0), (new byte[] { 0x80 }, end, 0));
        var result = BloArchiveReader.Parse(bytes, "NEW");
        Assert.Equal(5, result.Members.Count);
        Assert.Equal(new[] { "../x", "../x", "C:\\x", "", "\u0080" }, result.Members.Select(m => m.Name));
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, result.Members.Select(m => m.Ordinal));
    }

    [Fact]
    public void SnapshotAndEveryReturnedByteArrayAreDetached()
    {
        var bytes = Archive(2, (Name("A"), 48, 2));
        var original = (byte[])bytes.Clone();
        var archive = BloArchiveReader.Parse(bytes, "NEW");
        var member = archive.Members[0];
        var hash = archive.Sha256;
        Array.Fill(bytes, (byte)0);
        Array.Fill(archive.CopyBytes(), (byte)0);
        Array.Fill(member.RawNameField, (byte)0);
        Array.Fill(member.CopyPayload(), (byte)0);
        Assert.Equal(original, archive.CopyBytes());
        Assert.Equal("A", member.Name);
        Assert.Equal((byte)'A', member.RawNameField[0]);
        Assert.Equal(original.AsSpan(48, 2).ToArray(), member.CopyPayload());
        Assert.Equal(hash, archive.Sha256);
        Assert.Equal(AssetIntegrity.Sha256Hex(member.CopyPayload()), member.Sha256);
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    public void AdjacentUnsortedGappedAndEmptySpansAreValid(uint version)
    {
        var end = (uint)(8 + 4 * ((version == 1 ? 14 : 32) + 8));
        Assert.Equal(4, BloArchiveReader.Parse(Archive(version, (Name("A"), end + 2, 2),
            (Name("B"), end, 2), (Name("C"), end + 1, 0), (Name("D"), end + 7, 0)), "NEW").Members.Count);
        Assert.Empty(BloArchiveReader.Parse(Archive(version), "NEW").Members);
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    public void MalformedHeadersDirectoriesSpansAndOverlapsFailTyped(uint version)
    {
        var nameLength = version == 1 ? 14 : 32;
        var end = (uint)(8 + 2 * (nameLength + 8));
        var valid = Archive(version, (Name("A"), end, 2), (Name("B"), end + 2, 2));
        var malformed = new List<byte[]> { Array.Empty<byte>(), new byte[7], valid[..(int)(end - 1)] };
        void Header(int position, uint value)
        {
            var bytes = (byte[])valid.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(position, 4), value);
            malformed.Add(bytes);
        }
        Header(0, 3); Header(4, uint.MaxValue);
        foreach (var offset in new[] { 0u, 7u, end - 1, (uint)valid.Length + 1, uint.MaxValue }) Header(8 + nameLength, offset);
        Header(8 + nameLength + 4, uint.MaxValue);
        Header(8 + nameLength + 4, 5); // ordinary beyond-EOF length
        Header(8 + (nameLength + 8) + nameLength, end + 1); // nonempty overlap
        foreach (var bytes in malformed)
            Assert.Throws<BloArchiveFormatException>(() => BloArchiveReader.Parse(bytes, "NEW/malformed"));
    }

    [Theory]
    [InlineData("BLO/unknown.blo")]
    [InlineData("../BLO/AutoTurrets.blo")]
    [InlineData("BLO/../BLO/AutoTurrets.blo")]
    [InlineData("blo/AutoTurrets.blo")]
    [InlineData("BLO/autoturrets.blo")]
    [InlineData("BLO\\AutoTurrets.blo")]
    [InlineData("C:/BLO/AutoTurrets.blo")]
    [InlineData("/BLO/AutoTurrets.blo")]
    public void CanonicalPathGuardRunsBeforeRootOrFilesystemAccess(string path) =>
        Assert.Throws<ArgumentException>(() => BloArchiveReader.ReadCanonical(null!, path));

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    public void ManyRepeatedLargeSpansAreRejectedBeforeMemberConstruction(uint version)
    {
        const int count = 4096; // NEW adversarial fixture; no timing assertion.
        const uint payloadLength = 1024 * 1024;
        var end = (uint)(8 + count * ((version == 1 ? 14 : 32) + 8));
        var rows = Enumerable.Repeat((Name("Repeated"), end, payloadLength), count).ToArray();
        var bytes = Archive(version, rows);
        var error = Assert.Throws<BloArchiveFormatException>(() => BloArchiveReader.Parse(bytes, "NEW/repeated-spans"));
        Assert.Contains("overlap", error.Message);
    }

    [Fact]
    public void CanonicalMissingLengthHashAndLfsFailuresAreTypedWithoutChangingOriginals()
    {
        var root = Path.Combine(Path.GetTempPath(), "NEW-blo-test-" + Guid.NewGuid().ToString("N"));
        var pin = PinnedBloArchives.All[0];
        try
        {
            Assert.Equal(AssetIntegrityFailure.Missing, Assert.Throws<AssetIntegrityException>(() =>
                BloArchiveReader.ReadCanonical(root, pin.RelativePath)).Failure);
            Directory.CreateDirectory(Path.Combine(root, "BLO"));
            var path = Path.Combine(root, pin.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            File.WriteAllBytes(path, new byte[1]);
            Assert.Equal(AssetIntegrityFailure.LengthMismatch, Assert.Throws<AssetIntegrityException>(() =>
                BloArchiveReader.ReadCanonical(root, pin.RelativePath)).Failure);
            File.WriteAllText(path, "version https://git-lfs.github.com/spec/v1\noid sha256:" + pin.Sha256 + "\nsize " + pin.Length + "\n");
            Assert.Equal(AssetIntegrityFailure.LengthMismatch, Assert.Throws<AssetIntegrityException>(() =>
                BloArchiveReader.ReadCanonical(root, pin.RelativePath)).Failure);
            File.WriteAllBytes(path, new byte[(int)pin.Length]);
            Assert.Equal(AssetIntegrityFailure.Sha256Mismatch, Assert.Throws<AssetIntegrityException>(() =>
                BloArchiveReader.ReadCanonical(root, pin.RelativePath)).Failure);
        }
        finally
        {
            // Only the specifically-created NEW temporary fixture directory is removed.
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
