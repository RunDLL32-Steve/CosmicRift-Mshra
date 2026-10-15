using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

public sealed class AssetIntegrityTests : IDisposable
{
    private readonly string _temp = Path.Combine(Path.GetTempPath(), "rt-integrity-" + Guid.NewGuid().ToString("N"));

    public AssetIntegrityTests() => Directory.CreateDirectory(_temp);

    public void Dispose()
    {
        try { Directory.Delete(_temp, true); } catch (IOException) { }
    }

    private string CopyRealAssets()
    {
        var root = Path.Combine(_temp, "copy");
        foreach (var asset in PinnedAssets.All)
        {
            var target = Path.Combine(root, asset.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(OriginalAssets.Root!, asset.RelativePath), target);
        }
        return root;
    }

    [Fact]
    public void MissingFileThrowsTypedException()
    {
        var ex = Assert.Throws<AssetIntegrityException>(() => OriginalHullCatalog.Load(Path.Combine(_temp, "nothing")));
        Assert.Equal(AssetIntegrityFailure.Missing, ex.Failure);
    }

    [Fact]
    public void WrongContentThrowsBeforeAnythingIsParsed()
    {
        var root = Path.Combine(_temp, "fake");
        Directory.CreateDirectory(Path.Combine(root, "RPG"));
        File.WriteAllText(Path.Combine(root, "RPG", "Rogue.rpg"), "v6,1,1,1,1\r\n");
        var ex = Assert.Throws<AssetIntegrityException>(() => OriginalHullCatalog.Load(root));
        Assert.Equal("RPG/Rogue.rpg", ex.RelativePath);
        Assert.Equal(AssetIntegrityFailure.LengthMismatch, ex.Failure);
    }

    [Fact]
    public void VerifyDetectsSameLengthTamperingBySha256()
    {
        var bytes = new byte[PinnedAssets.Rpg.Length];
        var ex = Assert.Throws<AssetIntegrityException>(() => AssetIntegrity.Verify(PinnedAssets.Rpg, bytes));
        Assert.Equal(AssetIntegrityFailure.Sha256Mismatch, ex.Failure);
        Assert.Equal(PinnedAssets.Rpg.Sha256, ex.Expected);
    }

    [RequiresOriginalAssetsFact]
    public void UntouchedCopyVerifiesAndLoads()
    {
        var root = CopyRealAssets();
        AssetIntegrity.VerifyAll(root);
        Assert.Equal(12, OriginalHullCatalog.Load(root).Hulls.Count);
    }

    [RequiresOriginalAssetsTheory]
    [InlineData("RPG/Rogue.rpg")]
    [InlineData("VEH/Rogue.veh")]
    [InlineData("ITM/Rogue_mp.itm")]
    public void OneChangedByteInATamperedCopyThrowsSha256Mismatch(string relativePath)
    {
        var root = CopyRealAssets();
        var path = Path.Combine(root, relativePath);
        var bytes = File.ReadAllBytes(path);
        bytes[bytes.Length / 2] ^= 0x01;
        File.WriteAllBytes(path, bytes);

        var ex = Assert.Throws<AssetIntegrityException>(() => OriginalHullCatalog.Load(root));
        Assert.Equal(relativePath, ex.RelativePath);
        Assert.Equal(AssetIntegrityFailure.Sha256Mismatch, ex.Failure);
    }

    [RequiresOriginalAssetsFact]
    public void AppendedByteInATamperedCopyThrowsLengthMismatch()
    {
        var root = CopyRealAssets();
        File.AppendAllText(Path.Combine(root, "VEH", "Rogue.veh"), "\r\n");
        var ex = Assert.Throws<AssetIntegrityException>(() => OriginalHullCatalog.Load(root));
        Assert.Equal(AssetIntegrityFailure.LengthMismatch, ex.Failure);
    }

    [RequiresOriginalAssetsFact]
    public void NewlineNormalizedCopyIsRejected()
    {
        var root = CopyRealAssets();
        var path = Path.Combine(root, "RPG", "Rogue.rpg");
        File.WriteAllText(path, File.ReadAllText(path).Replace("\r\n", "\n"));
        Assert.Throws<AssetIntegrityException>(() => OriginalHullCatalog.Load(root));
    }
}
