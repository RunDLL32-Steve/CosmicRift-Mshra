using System.Globalization;

namespace RogueTrader.OriginalData;

/// <summary>
/// RT-F029 (data clauses only): the raw, uninterpreted collision-related fields of one hull's VEH Car row.
/// <see cref="PhysicalRadiusCandidate"/> is VEH f[35]; the name is a CANDIDATE taken from FEATURE_CATALOG.md rt-f029
/// ("Asp f[35]=21 physical radius"), not an established meaning. <see cref="BouncePercent"/> is f[362]
/// (FIELD_INTERPRETATION.md: "Native collision response and scale", still open). No unit or scale is implied.
/// </summary>
public sealed record OriginalHullCollisionFields(string Name, int VehRow, OriginalInt PhysicalRadiusCandidate, OriginalInt BouncePercent);

/// <summary>One CFG key read as-is: the raw entry (line number, text) plus its plain-integer value; no unit or scale.</summary>
public sealed record OriginalCfgInt(CfgEntry Entry, int Value);

/// <summary>Raw <c>RepelVehicle</c> and <c>RepelWeapons</c> entries of one <c>[TerrainN]</c> block with their line numbers; semantics unvalidated.</summary>
public sealed record OriginalTerrainRepel(int Terrain, int SectionLine, OriginalCfgInt RepelVehicle, OriginalCfgInt RepelWeapons);

/// <summary>
/// Raw collision-related data for RT-F029 from VEH/Rogue.veh and CFG/rogue.cfg. Nothing is interpreted: no unit, scaling or
/// timestep is implied. In particular <see cref="PhysicsDelay"/> (<c>[Vehicle] PhysicsDelay</c>) is NOT established to be the
/// simulation timestep, and <see cref="PointBlankDamageMultiplier"/> has no established scale. Missing keys or non-integer
/// values throw <see cref="OriginalDataException"/>; nothing is defaulted. The rogue.cfg values are the Rogue arena's;
/// other CFG files differ (PointBlankDamageMultiplier is 500 only in rogue.cfg, see the cloud branch log).
/// </summary>
public sealed class OriginalCollisionFields
{
    public const string PhysicalRadiusCandidateNote = "VEH f[35]: candidate name from FEATURE_CATALOG.md rt-f029, meaning unvalidated";
    public const int PhysicalRadiusCandidateField = 35;

    private OriginalCollisionFields(IReadOnlyList<OriginalHullCollisionFields> hulls, OriginalCfgInt pointBlank,
        OriginalCfgInt physicsDelay, IReadOnlyList<OriginalTerrainRepel> terrains)
    {
        Hulls = hulls;
        PointBlankDamageMultiplier = pointBlank;
        PhysicsDelay = physicsDelay;
        Terrains = terrains;
    }

    /// <summary>The twelve hulls in RPG file order.</summary>
    public IReadOnlyList<OriginalHullCollisionFields> Hulls { get; }

    /// <summary><c>[Arena] PointBlankDamageMultiplier</c> (rogue.cfg line 63 = 500); scale unvalidated.</summary>
    public OriginalCfgInt PointBlankDamageMultiplier { get; }

    /// <summary><c>[Vehicle] PhysicsDelay</c> (rogue.cfg line 125 = 400); unit and relation to the 60 Hz simulation unvalidated.</summary>
    public OriginalCfgInt PhysicsDelay { get; }

    /// <summary>[Terrain0..15] RepelVehicle/RepelWeapons in terrain order.</summary>
    public IReadOnlyList<OriginalTerrainRepel> Terrains { get; }

    public static OriginalCollisionFields Load(string assetsRoot) =>
        Resolve(OriginalHullCatalog.Load(assetsRoot), OriginalCfg.LoadRogue(assetsRoot));

    public static OriginalCollisionFields Resolve(OriginalHullCatalog hulls, OriginalCfg cfg)
    {
        var rows = hulls.Hulls
            .Select(h => new OriginalHullCollisionFields(h.Name, h.VehRowNumber,
                h.VehRecord.Int(PhysicalRadiusCandidateField), h.BouncePercent))
            .ToList();
        var terrains = new List<OriginalTerrainRepel>();
        for (var t = 0; t < OriginalTerrainAccess.TerrainCount; t++)
        {
            var s = cfg.Section($"Terrain{t}");
            terrains.Add(new OriginalTerrainRepel(t, s.LineNumber, Require(s, "RepelVehicle"), Require(s, "RepelWeapons")));
        }
        return new OriginalCollisionFields(rows, Require(cfg.Section("Arena"), "PointBlankDamageMultiplier"),
            Require(cfg.Section("Vehicle"), "PhysicsDelay"), terrains);
    }

    private static OriginalCfgInt Require(CfgSection section, string key)
    {
        var e = section.Find(key) ?? throw new OriginalDataException($"CFG [{section.Name}] has no key '{key}'.");
        if (!int.TryParse(e.Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v))
            throw new OriginalDataException($"CFG [{section.Name}] line {e.LineNumber}: '{e.Key}={e.Value}' is not a plain integer.");
        return new OriginalCfgInt(e, v);
    }
}
