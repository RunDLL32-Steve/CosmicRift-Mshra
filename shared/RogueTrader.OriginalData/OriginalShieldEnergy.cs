namespace RogueTrader.OriginalData;

/// <summary>A raw CFG integer with its source line (no unit, scale or default applied).</summary>
public readonly record struct OriginalCfgRawInt(int Line, string Key, int Value)
{
    public bool IsNegativeSentinel => Value < 0;
}

/// <summary>Raw <c>[TerrainN]</c> EnergyRate, HealthRate and RepairRate (RT-F031 data only; semantics and units unresolved).</summary>
public sealed record OriginalTerrainEnergyRates(int Terrain, OriginalCfgRawInt EnergyRate, OriginalCfgRawInt HealthRate, OriginalCfgRawInt RepairRate);

/// <summary>Raw per-hull VEH HullPoints f[6] and EnergyMax/EnergyRate/EnergyMin f[94..96] with row number and field indices.</summary>
public sealed record OriginalHullShieldEnergy(string Name, int VehRow, OriginalInt HullPoints, OriginalInt EnergyMax, OriginalInt EnergyRate, OriginalInt EnergyMin);

/// <summary>
/// RT-F031 raw, uninterpreted view of the shield / energy / hull fields for the Rogue arena: CFG <c>[Soul]</c>
/// EnergyShieldMode and EnergyDefaultStart (plus EnergyDefaultMax/Rate), the sixteen terrain EnergyRate/HealthRate/RepairRate triples
/// and the twelve hulls' HullPoints and energy fields. No unit, hit order, overflow, recharge or death rule is implied; those stay open.
/// Missing or non-integer keys throw <see cref="OriginalDataException"/>.
/// </summary>
public sealed class OriginalShieldEnergy
{
    private OriginalShieldEnergy(
        OriginalCfgRawInt shieldMode, OriginalCfgRawInt defaultStart, OriginalCfgRawInt defaultMax, OriginalCfgRawInt defaultRate,
        IReadOnlyList<OriginalTerrainEnergyRates> terrains, IReadOnlyList<OriginalHullShieldEnergy> hulls)
    {
        EnergyShieldMode = shieldMode;
        EnergyDefaultStart = defaultStart;
        EnergyDefaultMax = defaultMax;
        EnergyDefaultRate = defaultRate;
        Terrains = terrains;
        Hulls = hulls;
    }

    public OriginalCfgRawInt EnergyShieldMode { get; }
    public OriginalCfgRawInt EnergyDefaultStart { get; }
    public OriginalCfgRawInt EnergyDefaultMax { get; }
    public OriginalCfgRawInt EnergyDefaultRate { get; }
    public IReadOnlyList<OriginalTerrainEnergyRates> Terrains { get; }
    public IReadOnlyList<OriginalHullShieldEnergy> Hulls { get; }

    public static OriginalShieldEnergy Load(string assetsRoot) =>
        From(OriginalCfg.LoadRogue(assetsRoot), OriginalHullCatalog.Load(assetsRoot));

    public static OriginalShieldEnergy From(OriginalCfg cfg, OriginalHullCatalog hulls)
    {
        var soul = cfg.Section("Soul");
        var terrains = new List<OriginalTerrainEnergyRates>();
        for (var t = 0; t < OriginalTerrainAccess.TerrainCount; t++)
        {
            var s = cfg.Section($"Terrain{t}");
            terrains.Add(new OriginalTerrainEnergyRates(t, Read(s, "EnergyRate"), Read(s, "HealthRate"), Read(s, "RepairRate")));
        }
        var rows = hulls.Hulls
            .Select(h => new OriginalHullShieldEnergy(h.Name, h.VehRowNumber, h.HullPoints, h.EnergyMax, h.EnergyRate, h.EnergyMin))
            .ToList();
        return new OriginalShieldEnergy(
            Read(soul, "EnergyShieldMode"), Read(soul, "EnergyDefaultStart"), Read(soul, "EnergyDefaultMax"), Read(soul, "EnergyDefaultRate"),
            terrains, rows);
    }

    private static OriginalCfgRawInt Read(CfgSection section, string key)
    {
        var entry = section.Find(key) ?? throw new OriginalDataException($"CFG [{section.Name}] has no key '{key}'.");
        return new OriginalCfgRawInt(entry.LineNumber, entry.Key, section.Int(key));
    }
}
