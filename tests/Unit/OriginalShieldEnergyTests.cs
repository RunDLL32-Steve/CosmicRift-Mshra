using System.Text;
using RogueTrader.OriginalData;

namespace RogueTrader.UnitTests;

/// <summary>
/// RT-F031 raw shield/energy/hull fields. Oracle: scripts/audit-shield-energy.py output (SHIELD_ENERGY_OK) against eb1514ea.
/// Asset tests are Skipped, never passed, without the assets.
/// </summary>
public sealed class OriginalShieldEnergyTests
{
    private static readonly Lazy<OriginalShieldEnergy> Data = new(() => OriginalShieldEnergy.Load(OriginalAssets.Root!));

    [Fact]
    public void MissingKeysFailLoudly()
    {
        var cfg = OriginalCfg.Parse("[Soul]\r\nEnergyShieldMode=2\r\n", "t.cfg");
        Assert.Throws<OriginalDataException>(() => cfg.Section("Soul").Int("EnergyDefaultStart"));
        Assert.Throws<OriginalDataException>(() => cfg.Section("Terrain0"));
    }

    [RequiresOriginalAssetsFact]
    public void SoulKeysMatchTheAudit()
    {
        var d = Data.Value;
        Assert.Equal(new OriginalCfgInt(100, "EnergyShieldMode", 2), d.EnergyShieldMode);
        Assert.Equal(new OriginalCfgInt(108, "EnergyDefaultStart", -1), d.EnergyDefaultStart);
        Assert.True(d.EnergyDefaultStart.IsNegativeSentinel);
        Assert.Equal(new OriginalCfgInt(97, "EnergyDefaultMax", 1700000), d.EnergyDefaultMax);
        Assert.Equal(new OriginalCfgInt(98, "EnergyDefaultRate", 250), d.EnergyDefaultRate);
    }

    [RequiresOriginalAssetsFact]
    public void TerrainRatesMatchTheAudit()
    {
        var t = Data.Value.Terrains;
        Assert.Equal(16, t.Count);
        Assert.Equal(new[] { 9, 10, 15 }, t.Where(x => x.EnergyRate.Value != 0).Select(x => x.Terrain).ToArray());
        Assert.All(t.Where(x => x.EnergyRate.Value != 0), x => Assert.Equal(-2000, x.EnergyRate.Value));
        Assert.All(t, x => Assert.Equal(0, x.HealthRate.Value));
        Assert.All(t, x => Assert.Equal(0, x.RepairRate.Value));
        Assert.Equal(256, t[0].EnergyRate.Line);
        Assert.Equal(257, t[0].HealthRate.Line);
        Assert.Equal(258, t[0].RepairRate.Line);
        Assert.Equal(589, t[9].EnergyRate.Line);
        Assert.Equal(626, t[10].EnergyRate.Line);
        Assert.Equal(811, t[15].EnergyRate.Line);
        Assert.Equal(813, t[15].RepairRate.Line);
        for (var i = 0; i < 16; i++) Assert.Equal(i, t[i].Terrain);
    }

    [RequiresOriginalAssetsFact]
    public void HullFieldsMatchTheAudit()
    {
        var h = Data.Value.Hulls;
        Assert.Equal(12, h.Count);
        Assert.Equal(
            new[] { "Asp", "Excelsior", "Ravager", "Hornet", "Skithist", "Mastakith", "Tarthist", "Kragath" },
            h.Take(8).Select(x => x.Name).ToArray());
        foreach (var x in h)
        {
            Assert.Equal(new OriginalInt(6, 50, false), x.HullPoints);
            Assert.Equal(new OriginalInt(94, 1700, false), x.EnergyMax);
            Assert.Equal(new OriginalInt(95, 1050, false), x.EnergyRate);
            Assert.Equal(new OriginalInt(96, 1700, false), x.EnergyMin);
        }
        Assert.Equal(20, h.Single(x => x.Name == "Asp").VehRow);
    }

    [RequiresOriginalAssetsFact]
    public void EnergyShieldModeIsTwoInAll31CfgFilesAndNwsRowsAreVerbatim()
    {
        var root = OriginalAssets.Root!;
        var files = Directory.GetFiles(Path.Combine(root, "CFG"), "*.cfg");
        Assert.Equal(31, files.Length);
        foreach (var f in files)
        {
            var cfg = OriginalCfg.Parse(File.ReadAllText(f, Encoding.Latin1), Path.GetFileName(f));
            var e = cfg.Section("Soul").Find("EnergyShieldMode");
            Assert.NotNull(e);
            Assert.Equal("2", e!.Value);
            Assert.Equal(100, e.LineNumber);
        }

        var nws = File.ReadAllText(Path.Combine(root, "NWS", "Rogue.nws"), Encoding.Latin1).Split('\n');
        Assert.StartsWith("Your ship in ~BRogue Trader~B also differs", nws[38]);
        Assert.StartsWith("has Shield energy, it will take no damage from weapons", nws[39]);
        Assert.StartsWith("your Hull Points are at 0, you will blow up", nws[40]);
        Assert.StartsWith("All weapons do Shield Drain Damage.", nws[130]);
        Assert.StartsWith("All weapons will do one of 3 types of Hull Point Damage", nws[132]);
    }
}
