namespace RogueTrader.OriginalData;

/// <summary>
/// NEW/provisional reading of NWS line 295 ("taken down to 10% HP") laid next to the raw VEH values. It is a plain integer-math candidate:
/// <see cref="TenPercentFloor"/> = <c>HitpointsRaw / 10</c> (truncating division) and <see cref="TenPercentIsExact"/> says whether
/// <c>HitpointsRaw % 10 == 0</c>. NEITHER <see cref="TenPercentFloor"/> NOR <see cref="HitpointsRequiredToOperateRaw"/> is claimed to be the
/// native steal threshold; the native comparison (inclusive/exclusive, which hit-point value, rounding) is unvalidated, and the equality flag only records
/// whether the two raw numbers agree.
/// </summary>
public sealed record NewsTenPercentCandidate(
    int HitpointsRaw,
    int TenPercentFloor,
    bool TenPercentIsExact,
    int HitpointsRequiredToOperateRaw,
    bool RequiredToOperateEqualsTenPercentFloor);

/// <summary>
/// One structure named by NWS line 295, with its validated type-5 record, the raw fields that could relate to getting in, ownership and
/// destruction, and the visible mismatches against the NWS text. Nothing is corrected or defaulted; the field names are upstream-parser
/// names (InfantryOnline bb97579e), not original-executable evidence.
/// </summary>
public sealed class OriginalStealableStructure
{
    internal OriginalStealableStructure(OriginalComputerRecord record)
    {
        Record = record;
        Candidate = OriginalStealableStructures.TenPercentCandidate(record.Hitpoints.Value, record.HitpointsRequiredToOperate.Value);
        VisibleMismatches = OriginalStealableStructures.DescribeMismatches(record, Candidate);
    }

    public OriginalComputerRecord Record { get; }
    public int VehicleId => Record.Id;
    public string Name => Record.Name;
    /// <summary>f[6] Hitpoints, raw.</summary>
    public OriginalInt Hitpoints => Record.Hitpoints;
    /// <summary>f[211] HitpointsRequiredToOperate, raw (upstream-parser name; its native use is not established here).</summary>
    public OriginalInt HitpointsRequiredToOperate => Record.HitpointsRequiredToOperate;
    /// <summary>f[217] Destroyable, raw.</summary>
    public OriginalInt Destroyable => Record.Destroyable;
    /// <summary>f[66] VehicleGetInMode, raw.</summary>
    public OriginalInt VehicleGetInMode => Record.VehicleGetInMode;
    /// <summary>f[234] LogicTakeOwnership, raw text (empty for all three structures in the pinned file).</summary>
    public RawField LogicTakeOwnership => Record.LogicTakeOwnership;
    /// <summary>f[235] LogicStealOwnership, raw text (empty for all three structures in the pinned file).</summary>
    public RawField LogicStealOwnership => Record.LogicStealOwnership;
    /// <summary>f[210] RepairRate, raw.</summary>
    public OriginalInt RepairRate => Record.RepairRate;
    /// <summary>f[68] DropItemId, raw.</summary>
    public OriginalInt DropItemId => Record.DropItemId;
    /// <summary>f[69] DropItemQuantity, raw.</summary>
    public OriginalInt DropItemQuantity => Record.DropItemQuantity;
    /// <summary>NEW/provisional: the NWS "10% HP" integer candidate next to the raw HitpointsRequiredToOperate. Not the native steal threshold.</summary>
    public NewsTenPercentCandidate Candidate { get; }
    /// <summary>Human-readable notes where the raw values do not line up with NWS line 295. Informational; the values are never changed.</summary>
    public IReadOnlyList<string> VisibleMismatches { get; }
}

/// <summary>
/// Data view of RT-F058 "Stealable damaged structures" (data clause only): VEH rows 8, 12 and 13 (ids 408 Micro Repair Dock, 412 Micro Refinery,
/// 413 Rogue Trader Outpost) as validated type-5 records beside the pinned NWS line 295. No steal condition, access check, owner transfer, inventory
/// or production hand-over is implemented or validated: the pinned rows leave the upstream-named ownership logic strings empty and give
/// HitpointsRequiredToOperate = 2% of Hitpoints, so where the "10% HP" rule lives is still open.
/// </summary>
public sealed class OriginalStealableStructures
{
    public const int NwsLine = 295;
    public static readonly int[] VehicleIds = [408, 412, 413];

    private OriginalStealableStructures(IReadOnlyList<OriginalStealableStructure> structures, OriginalNwsLine nws)
    {
        Structures = structures;
        Nws = nws;
    }

    /// <summary>VEH ids 408, 412, 413 in that order.</summary>
    public IReadOnlyList<OriginalStealableStructure> Structures { get; }
    /// <summary>NWS/Rogue.nws line 295, verbatim.</summary>
    public OriginalNwsLine Nws { get; }

    public OriginalStealableStructure ById(int vehicleId) =>
        Structures.FirstOrDefault(s => s.VehicleId == vehicleId) ?? throw new KeyNotFoundException($"No stealable structure with VEH id {vehicleId}.");

    public static OriginalStealableStructures Load(string assetsRoot) =>
        Resolve(OriginalVehicleCatalog.Load(assetsRoot), OriginalNws.LoadRogue(assetsRoot));

    public static OriginalStealableStructures Resolve(OriginalVehicleCatalog vehicles, OriginalNws nws)
    {
        var line = nws.Range(NwsLine, NwsLine)[0];
        if (!line.Text.Contains("can be stolen", StringComparison.Ordinal))
            throw new OriginalDataException($"NWS line {NwsLine} does not contain 'can be stolen': '{line.Text}'.");
        var list = new List<OriginalStealableStructure>();
        foreach (var id in VehicleIds)
        {
            var veh = vehicles.All.SingleOrDefault(v => v.Id == id)
                ?? throw new OriginalDataException($"VEH has no row with id {id}.");
            list.Add(new OriginalStealableStructure(new OriginalComputerRecord(veh)));
        }
        return new OriginalStealableStructures(list, line);
    }

    /// <summary>
    /// Pure NEW/provisional helper: 10% of <paramref name="hitpoints"/> by truncating integer division, placed next to the raw
    /// <paramref name="hitpointsRequiredToOperate"/>. Throws <see cref="OriginalDataException"/> for negative hit points.
    /// </summary>
    public static NewsTenPercentCandidate TenPercentCandidate(int hitpoints, int hitpointsRequiredToOperate)
    {
        if (hitpoints < 0) throw new OriginalDataException($"Hitpoints {hitpoints} is negative.");
        var floor = hitpoints / 10;
        return new NewsTenPercentCandidate(hitpoints, floor, hitpoints % 10 == 0, hitpointsRequiredToOperate, hitpointsRequiredToOperate == floor);
    }

    internal static IReadOnlyList<string> DescribeMismatches(OriginalComputerRecord r, NewsTenPercentCandidate c)
    {
        var notes = new List<string>();
        if (!c.RequiredToOperateEqualsTenPercentFloor)
            notes.Add($"f[{VehComputerFields.HitpointsRequiredToOperate}] HitpointsRequiredToOperate={c.HitpointsRequiredToOperateRaw} is not 10% of f[{VehComputerFields.Hitpoints}] Hitpoints={c.HitpointsRaw} (integer 10% = {c.TenPercentFloor}).");
        if (r.LogicStealOwnership.Value.Length == 0)
            notes.Add($"f[{VehComputerFields.LogicStealOwnership}] LogicStealOwnership is empty.");
        if (r.LogicTakeOwnership.Value.Length == 0)
            notes.Add($"f[{VehComputerFields.LogicTakeOwnership}] LogicTakeOwnership is empty.");
        return notes;
    }
}
