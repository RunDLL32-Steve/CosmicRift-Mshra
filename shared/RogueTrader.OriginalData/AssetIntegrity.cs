using System.Security.Cryptography;

namespace RogueTrader.OriginalData;

/// <summary>Pinned identity (path, byte length, SHA-256) of one original file; values from docs/original-research/ASSET_MANIFEST.json.</summary>
public sealed record PinnedAsset(string RelativePath, long Length, string Sha256);

/// <summary>The three canonical source files at fowlmouth/cosmic-rift-assets commit <see cref="Commit"/>.</summary>
public static class PinnedAssets
{
    public const string Commit = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35";

    public static readonly PinnedAsset Rpg = new("RPG/Rogue.rpg", 2329, "44d5e6d3c78bfa445c723260086235017bd1973557103003aa8f3b71e30ae123");
    public static readonly PinnedAsset Veh = new("VEH/Rogue.veh", 41075, "9eefa85a04587867616be786fffc664f3ebc0fb1d50ba6f6fd303a7a8a988572");
    public static readonly PinnedAsset Itm = new("ITM/Rogue_mp.itm", 111614, "dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7");

    public static IReadOnlyList<PinnedAsset> All { get; } = [Rpg, Veh, Itm];
}

/// <summary>Verifies raw bytes before they are parsed. The raw bytes are hashed with no newline normalization.</summary>
public static class AssetIntegrity
{
    public static string Sha256Hex(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    /// <summary>Throws <see cref="AssetIntegrityException"/> unless the bytes match the pinned length and SHA-256.</summary>
    public static void Verify(PinnedAsset pinned, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != pinned.Length)
            throw new AssetIntegrityException(pinned.RelativePath, AssetIntegrityFailure.LengthMismatch,
                pinned.Length.ToString(), bytes.Length.ToString());
        var actual = Sha256Hex(bytes);
        if (!string.Equals(actual, pinned.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new AssetIntegrityException(pinned.RelativePath, AssetIntegrityFailure.Sha256Mismatch, pinned.Sha256, actual);
    }

    /// <summary>Reads a pinned file below <paramref name="assetsRoot"/>, verifies it and returns the verified bytes (read once, so what is checked is what is parsed).</summary>
    public static byte[] ReadVerified(string assetsRoot, PinnedAsset pinned)
    {
        var path = Path.Combine(assetsRoot, pinned.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            throw new AssetIntegrityException(pinned.RelativePath, AssetIntegrityFailure.Missing, "file present", "missing: " + path);
        var bytes = File.ReadAllBytes(path);
        Verify(pinned, bytes);
        return bytes;
    }

    /// <summary>Verifies all three pinned files.</summary>
    public static void VerifyAll(string assetsRoot)
    {
        foreach (var pinned in PinnedAssets.All) ReadVerified(assetsRoot, pinned);
    }
}
