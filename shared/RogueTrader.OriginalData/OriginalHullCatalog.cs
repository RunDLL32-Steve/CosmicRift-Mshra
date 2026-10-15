using System.Globalization;

namespace RogueTrader.OriginalData;

/// <summary>
/// Loads the twelve original selectable hulls from RPG/Rogue.rpg and VEH/Rogue.veh.
/// <para>
/// Hull rows: every RPG record with version token <c>v6</c> and f[2] == "1". A record that is not v6 is an error.
/// </para>
/// <para>
/// VEH matching rule: the RPG vehicle id (f[3]) is compared with VEH f[2]. The VEH record is the single row whose
/// f[0] is "2" (the Car type), whose version token is <c>v53</c> and whose f[2] equals that id. Zero or several
/// matches, a non-Car/non-v53 row with that id, a Car row too short for the typed accessors, or an RPG name different
/// from VEH f[3], throw <see cref="OriginalHullLinkException"/>. Example: Asp has RPG f[3]=2002, which is VEH row 20.
/// </para>
/// </summary>
public sealed class OriginalHullCatalog
{
    public const string SupportedRpgVersion = "v6";
    public const string SupportedVehVersion = "v53";
    private const string HullRecordType = "1";
    private const string CarRecordType = "2";

    private OriginalHullCatalog(IReadOnlyList<OriginalHull> hulls) => Hulls = hulls;

    /// <summary>Hulls in RPG file order.</summary>
    public IReadOnlyList<OriginalHull> Hulls { get; }

    public OriginalHull ByName(string name) =>
        Hulls.FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.Ordinal))
        ?? throw new KeyNotFoundException($"No original hull named '{name}'.");

    public OriginalHull BySkillId(int skillId) =>
        Hulls.FirstOrDefault(h => h.SkillId == skillId)
        ?? throw new KeyNotFoundException($"No original hull with skill id {skillId}.");

    /// <summary>
    /// Verifies the SHA-256 and byte length of RPG/Rogue.rpg, VEH/Rogue.veh and ITM/Rogue_mp.itm under
    /// <paramref name="assetsRoot"/> (throwing <see cref="AssetIntegrityException"/> on any difference), then parses the
    /// verified bytes. The files are only read, never written.
    /// </summary>
    public static OriginalHullCatalog Load(string assetsRoot)
    {
        var rpg = AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Rpg);
        var veh = AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh);
        AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Itm);
        return FromRecords(
            RawRecordReader.Parse(rpg, PinnedAssets.Rpg.RelativePath),
            RawRecordReader.Parse(veh, PinnedAssets.Veh.RelativePath));
    }

    /// <summary>Links already-parsed records (no integrity check; used by <see cref="Load"/> after verification).</summary>
    public static OriginalHullCatalog FromRecords(IReadOnlyList<RawRecord> rpgRecords, IReadOnlyList<RawRecord> vehRecords)
    {
        var hulls = new List<OriginalHull>();
        foreach (var rpg in rpgRecords)
        {
            if (rpg.VersionToken != SupportedRpgVersion)
                throw new OriginalHullLinkException($"{rpg.SourceFile} row {rpg.RowNumber}: unsupported version '{rpg.VersionToken}', expected {SupportedRpgVersion}.");
            if (rpg.Value(RpgFields.RecordType) != HullRecordType) continue;

            var vehicleId = rpg.Value(RpgFields.VehicleId);
            var matches = vehRecords.Where(v =>
                v.FieldCount > VehFields.RecordType && v.Value(VehFields.RecordType) == CarRecordType &&
                v.VersionToken == SupportedVehVersion &&
                v.FieldCount > VehFields.VehicleId && v.Value(VehFields.VehicleId) == vehicleId).ToList();
            var name = rpg.Value(RpgFields.Name);
            if (matches.Count != 1)
                throw new OriginalHullLinkException($"{rpg.SourceFile} row {rpg.RowNumber} ('{name}'): vehicle id {vehicleId} matches {matches.Count} VEH Car {SupportedVehVersion} rows, expected exactly one.");
            var veh = matches[0];
            if (veh.FieldCount <= VehFields.MaxTypedField)
                throw new OriginalHullLinkException($"{veh.SourceFile} row {veh.RowNumber}: only {veh.FieldCount} fields, expected more than {VehFields.MaxTypedField}.");
            if (veh.Value(VehFields.Name) != name)
                throw new OriginalHullLinkException($"{rpg.SourceFile} row {rpg.RowNumber}: name '{name}' differs from {veh.SourceFile} row {veh.RowNumber} name '{veh.Value(VehFields.Name)}'.");
            hulls.Add(new OriginalHull(rpg, veh));
        }

        foreach (var group in hulls.GroupBy(h => h.SkillId).Where(g => g.Count() > 1))
            throw new OriginalHullLinkException($"Skill id {group.Key.ToString(CultureInfo.InvariantCulture)} is used by more than one hull row.");
        foreach (var group in hulls.GroupBy(h => h.VehicleId).Where(g => g.Count() > 1))
            throw new OriginalHullLinkException($"Vehicle id {group.Key.ToString(CultureInfo.InvariantCulture)} is used by more than one hull row.");
        return new OriginalHullCatalog(hulls);
    }
}
