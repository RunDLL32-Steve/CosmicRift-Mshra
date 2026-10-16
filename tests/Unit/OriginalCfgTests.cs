using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// Synthetic parser tests (always run) plus oracle tests against the pinned CFG/rogue.cfg (eb1514ea, independent Python audit:
/// 144 unique sections, 16 Terrain blocks, 3051 CRLF lines). Asset tests are Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalCfgTests
{
    private static readonly Lazy<OriginalCfg> Rogue = new(() => OriginalCfg.LoadRogue(OriginalAssets.Root!));

    [Fact]
    public void ParsesSectionsKeysAndKeepsDuplicatesAndRawValues()
    {
        var cfg = OriginalCfg.Parse("[A]\r\nx=1\r\nx=2\r\ny= a=b \r\n\r\n[B]\r\nz=\r\n", "t.cfg");
        Assert.Equal(new[] { "A", "B" }, cfg.Sections.Select(s => s.Name));
        var a = cfg.Section("a");
        Assert.Equal(new[] { "1", "2", " a=b " }, a.Entries.Select(e => e.Value));
        Assert.Equal(new[] { 2, 3, 4 }, a.Entries.Select(e => e.LineNumber));
        Assert.Equal("", cfg.Section("B").Entries[0].Value);
        Assert.Equal(1, a.Int("x"));
    }

    [Fact]
    public void RejectsContentOutsideASectionAndLinesWithoutEquals()
    {
        Assert.Throws<OriginalDataException>(() => OriginalCfg.Parse("x=1\r\n", "t.cfg"));
        Assert.Throws<OriginalDataException>(() => OriginalCfg.Parse("[A]\r\njunk\r\n", "t.cfg"));
        Assert.Throws<OriginalDataException>(() => OriginalCfg.Parse("[A\r\n", "t.cfg"));
        Assert.Throws<OriginalDataException>(() => OriginalCfg.Parse("[A]x\r\n", "t.cfg"));
        var cfg = OriginalCfg.Parse("[A]\r\nx=zz\r\n[A]\r\n", "t.cfg");
        Assert.Throws<OriginalDataException>(() => cfg.Section("A"));
        Assert.Throws<OriginalDataException>(() => OriginalCfg.Parse("[A]\r\nx=zz\r\n", "t.cfg").Section("A").Int("x"));
    }

    [RequiresOriginalAssetsFact]
    public void RogueCfgLoadsAllSectionsAndKnownValues()
    {
        var cfg = Rogue.Value;
        Assert.Equal(144, cfg.Sections.Count);
        Assert.Equal(16, cfg.Sections.Count(s => s.Name.StartsWith("Terrain", StringComparison.Ordinal)));
        Assert.Equal(63, cfg.Section("Arena").Find("PointBlankDamageMultiplier")!.LineNumber);
        Assert.Equal(500, cfg.Section("Arena").Int("PointBlankDamageMultiplier"));
        Assert.Equal(400, cfg.Section("Vehicle").Int("PhysicsDelay"));
        Assert.Equal(1700000, cfg.Section("Soul").Int("EnergyDefaultMax"));
        Assert.Equal(250, cfg.Section("Soul").Int("EnergyDefaultRate"));
        Assert.Equal(2, cfg.Section("Soul").Int("EnergyShieldMode"));
        Assert.Equal("Rogue.lvl", cfg.Section("Level").Find("LvlFile")!.Value);
        Assert.Equal("Rogue.lio", cfg.Section("Level").Find("LioFile")!.Value);
    }

    [RequiresOriginalAssetsFact]
    public void TerrainAccessFlagsMatchTheAudit()
    {
        var t = OriginalTerrainAccess.All(Rogue.Value);
        Assert.Equal(16, t.Count);
        Assert.Equal(248, t[0].Line);
        Assert.Equal(new[] { 0, 9, 15 }, t.Where(x => x.StoreEnabled == 0).Select(x => x.Terrain));
        Assert.Equal(new[] { 0, 9, 10, 15 }, t.Where(x => x.SkillEnabled == 0).Select(x => x.Terrain));
        Assert.Equal(new[] { 0, 9, 15 }, t.Where(x => x.TeamChangeEnabled == 0).Select(x => x.Terrain));
        Assert.All(t, x =>
        {
            Assert.InRange(x.StoreEnabled, 0, 1);
            Assert.InRange(x.SkillEnabled, 0, 1);
            Assert.InRange(x.TeamChangeEnabled, 0, 1);
        });
    }

    [Fact]
    public void CfgPinMatchesAssetManifest()
    {
        var root = OriginalAssets.RepoRoot();
        Assert.NotNull(root);
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root!, "docs", "original-research", "ASSET_MANIFEST.json")));
        var entry = doc.RootElement.GetProperty("assets").EnumerateArray()
            .Single(a => a.GetProperty("originalPath").GetString() == OriginalCfg.Rogue.RelativePath);
        Assert.Equal(OriginalCfg.Rogue.Sha256, entry.GetProperty("originalSha256").GetString());
        Assert.Equal(OriginalCfg.Rogue.Length, entry.GetProperty("originalSizeBytes").GetInt64());
    }

    [RequiresOriginalAssetsFact]
    public void ModifiedBytesAreRejectedByTheIntegrityGuard()
    {
        var bytes = AssetIntegrity.ReadVerified(OriginalAssets.Root!, OriginalCfg.Rogue).ToArray();
        bytes[0] ^= 1;
        Assert.Throws<AssetIntegrityException>(() => AssetIntegrity.Verify(OriginalCfg.Rogue, bytes));
    }
}
