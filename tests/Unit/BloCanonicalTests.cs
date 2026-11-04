using System.Globalization;
using System.Text;
using System.Text.Json;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

public sealed class BloCanonicalTests
{
    [RequiresOriginalAssetsFact]
    public void AllCanonicalArchivesMatchIndependentDirectoryOracleAndDirectSourceSlices()
    {
        using var oracle = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "BloDirectoryOracle.json")));
        var top = oracle.RootElement;
        Assert.Equal(PinnedAssets.Commit, top.GetProperty("sourceCommit").GetString());
        Assert.Equal(89, top.GetProperty("archiveCount").GetInt32());
        Assert.Equal(1262, top.GetProperty("memberCount").GetInt32());
        Assert.Equal(89, PinnedBloArchives.All.Count);
        var rows = top.GetProperty("archives").EnumerateArray().ToArray();
        Assert.Equal(PinnedBloArchives.All.Select(p => p.RelativePath), rows.Select(r => r.GetProperty("relativePath").GetString()));
        var count = 0;
        var v1Count = 0;
        var trailingNameCount = 0;
        foreach (var row in rows)
        {
            var relativePath = row.GetProperty("relativePath").GetString()!;
            var pin = Assert.Single(PinnedBloArchives.All, p => p.RelativePath == relativePath);
            Assert.Equal(row.GetProperty("length").GetInt64(), pin.Length);
            Assert.Equal(row.GetProperty("sha256").GetString(), pin.Sha256);
            var source = File.ReadAllBytes(Path.Combine(OriginalAssets.Root!, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            var archive = BloArchiveReader.ReadCanonical(OriginalAssets.Root!, relativePath);
            Assert.True(archive.IsVerified);
            Assert.Equal(relativePath, archive.SourcePath);
            Assert.Equal(source, archive.CopyBytes());
            Assert.Equal(pin.Sha256, AssetIntegrity.Sha256Hex(source));
            Assert.Equal(pin.Sha256, archive.Sha256);
            Assert.Equal(row.GetProperty("version").GetUInt32(), archive.Version);
            Assert.Equal(row.GetProperty("memberCount").GetInt32(), archive.Members.Count);
            Assert.Equal(row.GetProperty("directoryEnd").GetInt32(), archive.DirectoryEnd);
            var digestRows = new StringBuilder();
            var nameLength = archive.Version == 1 ? 14 : 32;
            if (archive.Version == 1) v1Count++;
            for (var i = 0; i < archive.Members.Count; i++)
            {
                var member = archive.Members[i];
                Assert.Equal(i, member.Ordinal);
                var raw = member.RawNameField;
                Assert.Equal(source.AsSpan(8 + i * (nameLength + 8), nameLength).ToArray(), raw);
                var nul = Array.IndexOf(raw, (byte)0);
                Assert.All(raw.Take(nul < 0 ? raw.Length : nul), b => Assert.True(b < 128));
                if (nul >= 0 && raw.Skip(nul + 1).Any(b => b != 0)) trailingNameCount++;
                Assert.True(member.Offset >= archive.DirectoryEnd);
                Assert.True((ulong)member.Offset + member.Length <= (ulong)source.Length);
                var payload = source.AsSpan((int)member.Offset, (int)member.Length).ToArray();
                Assert.Equal(payload, member.CopyPayload());
                Assert.Equal(AssetIntegrity.Sha256Hex(payload), member.Sha256);
                digestRows.Append(i.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(Convert.ToHexString(raw).ToLowerInvariant()).Append('\t')
                    .Append(member.Offset.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(member.Length.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(member.Sha256).Append('\n');
            }
            var expectedDigest = row.GetProperty("memberDirectorySha256").GetString();
            Assert.Equal(expectedDigest, AssetIntegrity.Sha256Hex(Encoding.UTF8.GetBytes(digestRows.ToString())));
            var repeated = BloArchiveReader.ReadCanonical(OriginalAssets.Root!, relativePath);
            var repeatedRows = string.Concat(repeated.Members.Select(m => string.Join("\t",
                m.Ordinal.ToString(CultureInfo.InvariantCulture), Convert.ToHexString(m.RawNameField).ToLowerInvariant(),
                m.Offset.ToString(CultureInfo.InvariantCulture), m.Length.ToString(CultureInfo.InvariantCulture), m.Sha256) + "\n"));
            Assert.True(repeated.IsVerified);
            Assert.Equal(expectedDigest, AssetIntegrity.Sha256Hex(Encoding.UTF8.GetBytes(repeatedRows)));
            count += archive.Members.Count;
        }
        Assert.Equal(1262, count);
        Assert.Equal(2, v1Count);
        Assert.Equal(3, trailingNameCount);
    }
}
