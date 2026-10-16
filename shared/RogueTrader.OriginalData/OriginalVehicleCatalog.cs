namespace RogueTrader.OriginalData;

/// <summary>
/// One row of VEH/Rogue.veh (version <c>v53</c>) kept completely raw. Only the four fields that existing code and
/// docs already name are typed: record type f[0] (<see cref="VehFields.RecordType"/>), version token (f[1]), vehicle id
/// f[2] (<see cref="VehFields.VehicleId"/>) and name f[3] (<see cref="VehFields.Name"/>). Every other field is
/// available only by index through <see cref="Record"/>. Field indexes documented for Car rows (hull points, energy,
/// terrain blocks ...) are NOT assumed to apply to the other record types.
/// </summary>
public sealed class OriginalVehicleRecord
{
    internal OriginalVehicleRecord(RawRecord record, int recordType, string versionToken, int id, string name)
    {
        Record = record;
        RecordType = recordType;
        VersionToken = versionToken;
        Id = id;
        Name = name;
    }

    public RawRecord Record { get; }
    /// <summary>1-based physical line number in VEH/Rogue.veh.</summary>
    public int RowNumber => Record.RowNumber;
    /// <summary>f[0], raw (2 = the twelve hull Car rows, 4 = Spectator Mode, 5 = the seventeen computer rows).</summary>
    public int RecordType { get; }
    /// <summary>The <c>v53</c> version token (f[1]).</summary>
    public string VersionToken { get; }
    /// <summary>f[2].</summary>
    public int Id { get; }
    /// <summary>f[3] (unquoted).</summary>
    public string Name { get; }
    public int FieldCount => Record.FieldCount;

    public bool IsCar => RecordType == OriginalVehicleCatalog.CarRecordType;
    public bool IsComputer => RecordType == OriginalVehicleCatalog.ComputerRecordType;
    public bool IsSpectator => RecordType == OriginalVehicleCatalog.SpectatorRecordType;

    /// <summary>A raw field by index (padding removed, quotes stripped; nothing scaled).</summary>
    public RawField Field(int index) => Record.Field(index);
    public OriginalInt Int(int index) => Record.Int(index);
}

/// <summary>
/// Raw loader for all rows of VEH/Rogue.veh (queue G7; the twelve Car rows are also consumed by
/// <see cref="OriginalHullCatalog"/>, whose code is not changed). Rules: every non-blank row must carry version
/// token <c>v53</c> and at least four fields; f[0] and f[2] must be integers; ids must be unique; otherwise
/// <see cref="OriginalVehicleException"/> (or <see cref="OriginalDataFormatException"/> for a malformed integer) is
/// thrown. Pinned file: 30 rows, rows 1-17 type 5 (17 computers; Solar Mold has 478 fields, the other sixteen 477),
/// rows 18-29 type 2 (the 12 hulls, 454 fields), row 30 type 4 Spectator Mode (202 fields). The meaning of the
/// record-type values is inferred from row names and the hull loader only; it is not established by documentation.
/// </summary>
public sealed class OriginalVehicleCatalog
{
    public const string SupportedVersion = OriginalHullCatalog.SupportedVehVersion;
    /// <summary>f[0] value of the twelve hull rows (the hull loader's Car type).</summary>
    public const int CarRecordType = 2;
    public const int SpectatorRecordType = 4;
    public const int ComputerRecordType = 5;

    private OriginalVehicleCatalog(IReadOnlyList<OriginalVehicleRecord> all) => All = all;

    /// <summary>Every VEH row in file order.</summary>
    public IReadOnlyList<OriginalVehicleRecord> All { get; }

    /// <summary>The type-2 rows (the hull Car rows) in file order.</summary>
    public IEnumerable<OriginalVehicleRecord> CarRows => All.Where(v => v.IsCar);

    /// <summary>The type-5 rows in file order.</summary>
    public IEnumerable<OriginalVehicleRecord> Computers => All.Where(v => v.IsComputer);

    /// <summary>Every row that is not a type-2 hull row (computers plus Spectator Mode), in file order.</summary>
    public IEnumerable<OriginalVehicleRecord> NonHullRows => All.Where(v => !v.IsCar);

    public OriginalVehicleRecord ById(int id) =>
        All.FirstOrDefault(v => v.Id == id) ?? throw new KeyNotFoundException($"No original VEH row with id {id}.");

    public OriginalVehicleRecord ByName(string name) =>
        All.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.Ordinal))
        ?? throw new KeyNotFoundException($"No original VEH row named '{name}'.");

    /// <summary>Verifies VEH/Rogue.veh against <see cref="PinnedAssets.Veh"/> (hash and length), then parses the verified bytes.</summary>
    public static OriginalVehicleCatalog Load(string assetsRoot) =>
        FromRecords(RawRecordReader.Parse(AssetIntegrity.ReadVerified(assetsRoot, PinnedAssets.Veh), PinnedAssets.Veh.RelativePath));

    public static OriginalVehicleCatalog FromRecords(IReadOnlyList<RawRecord> vehRecords)
    {
        var list = new List<OriginalVehicleRecord>();
        var seen = new Dictionary<int, int>();
        foreach (var r in vehRecords)
        {
            if (r.VersionToken != SupportedVersion)
                throw new OriginalVehicleException($"{r.SourceFile} row {r.RowNumber}: unsupported version '{r.VersionToken}', expected {SupportedVersion}.");
            if (r.FieldCount <= VehFields.Name)
                throw new OriginalVehicleException($"{r.SourceFile} row {r.RowNumber}: only {r.FieldCount} fields, expected more than {VehFields.Name}.");
            var type = r.Int(VehFields.RecordType).Value;
            var id = r.Int(VehFields.VehicleId).Value;
            if (seen.TryGetValue(id, out var first))
                throw new OriginalVehicleException($"{r.SourceFile} row {r.RowNumber}: id {id} already used by row {first}.");
            seen[id] = r.RowNumber;
            list.Add(new OriginalVehicleRecord(r, type, r.VersionToken!, id, r.Value(VehFields.Name)));
        }
        return new OriginalVehicleCatalog(list);
    }
}

public sealed class OriginalVehicleException(string message) : OriginalDataException(message);
