namespace RogueTrader.OriginalData;

/// <summary>
/// An ITM id found in a VEH field, resolved against ITM/Rogue_mp.itm by id only. <see cref="Item"/> is null when the catalog has no row with
/// <see cref="RawId"/> (for example the raw 0 of an unused inventory slot, or a value that is not an item id at all); such ids stay visible and
/// are never guessed. Which VEH fields are item ids comes from the upstream-parser field names (InfantryOnline bb97579e), not executable evidence.
/// </summary>
public sealed record OriginalItemRef(string Role, int FieldIndex, int RawId, OriginalItem? Item)
{
    public bool Resolved => Item != null;
    public string? ItemName => Item?.Name;
    public int? ItemRecordType => Item?.RecordType;
    public string? ItemCategory => Item?.Category;
}

/// <summary>
/// One Null NPC row (VEH type 5): the validated raw record plus its item-id fields resolved against the ITM catalog. Nothing is scaled,
/// defaulted or interpreted; actor logic (targeting, firing, spawning), faction and drop behaviour are native/server and are not claimed.
/// </summary>
public sealed class OriginalNullNpc
{
    internal OriginalNullNpc(OriginalComputerRecord record, OriginalItemCatalog items)
    {
        Record = record;
        Inventory = Enumerable.Range(0, VehComputerFields.InventoryItemsCount)
            .Select(k => Ref(items, $"InventoryItems{k}", VehComputerFields.InventoryItem(k)))
            .ToList();
        Explode = Ref(items, "ExplodeItemId", VehComputerFields.ExplodeItemId);
        Drop = Ref(items, "DropItemId", VehComputerFields.DropItemId);
    }

    private OriginalItemRef Ref(OriginalItemCatalog items, string role, int field)
    {
        var raw = Record.Int(field).Value;
        return new OriginalItemRef(role, field, raw, items.Items.FirstOrDefault(i => i.Id == raw));
    }

    public OriginalComputerRecord Record { get; }
    public int VehicleId => Record.Id;
    public string Name => Record.Name;
    /// <summary>1-based physical line of the row in VEH/Rogue.veh.</summary>
    public int RowNumber => Record.RowNumber;
    /// <summary>f[6] Hitpoints, raw.</summary>
    public OriginalInt Hitpoints => Record.Hitpoints;
    /// <summary>f[50]..f[55] InventoryItems0..5, each resolved by ITM id (unused slots are raw 0 and stay unresolved).</summary>
    public IReadOnlyList<OriginalItemRef> Inventory { get; }
    /// <summary>f[64] ExplodeItemId.</summary>
    public OriginalItemRef Explode { get; }
    /// <summary>f[68] DropItemId.</summary>
    public OriginalItemRef Drop { get; }
    /// <summary>f[69] DropItemQuantity, raw.</summary>
    public OriginalInt DropItemQuantity => Record.DropItemQuantity;
    public OriginalInt TrackingRadius => Record.TrackingRadius;
    public OriginalInt TrackingTime => Record.TrackingTime;
    public OriginalInt FireRadius => Record.FireRadius;
    public OriginalInt RotateSpeed => Record.RotateSpeed;
    public OriginalInt ObeyLos => Record.ObeyLos;
    public OriginalInt RepairRate => Record.RepairRate;
    public OriginalInt Destroyable => Record.Destroyable;
    public OriginalInt HitpointsRequiredToOperate => Record.HitpointsRequiredToOperate;
    public OriginalInt RelativeId => Record.RelativeId;
    public OriginalInt DensityMaxType => Record.DensityMaxType;
    public OriginalInt DensityMaxActive => Record.DensityMaxActive;
    public OriginalInt DensityMaxInactive => Record.DensityMaxInactive;
    public OriginalInt FrequencyMaxType => Record.FrequencyMaxType;
    public OriginalInt FrequencyMaxActive => Record.FrequencyMaxActive;
    public OriginalInt FrequencyMaxInactive => Record.FrequencyMaxInactive;
    public RawField LogicTakeOwnership => Record.LogicTakeOwnership;
    public RawField LogicStealOwnership => Record.LogicStealOwnership;
    public RawField SpriteBaseBlobName => Record.SpriteBaseBlobName;
    public RawField SpriteBaseBlobId => Record.SpriteBaseBlobId;
    public RawField SpriteTurretBlobName => Record.SpriteTurretBlobName;
    public RawField SpriteTurretBlobId => Record.SpriteTurretBlobId;
}

/// <summary>
/// Result of the RT-F059 "Null Wreckage" audit. Null Wreckage is claimed by NWS line 268 as a unique item; this records what the pinned ITM and
/// VEH data actually contain: the ITM rows whose name matches "Null Wreckage" exactly or case-insensitively (or mentions "null" at all), the item
/// the two Null NPC rows drop, and which other computer rows drop the same item. Nothing is corrected or assumed.
/// </summary>
public sealed class OriginalNullWreckageAudit
{
    internal OriginalNullWreckageAudit(IReadOnlyList<OriginalItem> exact, IReadOnlyList<OriginalItem> ignoreCase, IReadOnlyList<OriginalItem> namesContainingNull,
        OriginalItem? ordinaryWreckage, IReadOnlyList<OriginalItemRef> npcDrops, IReadOnlyList<int> otherComputerRowsWithSameDrop, IReadOnlyList<string> mismatches)
    {
        ExactMatches = exact;
        CaseInsensitiveMatches = ignoreCase;
        ItemNamesContainingNull = namesContainingNull;
        OrdinaryWreckage = ordinaryWreckage;
        NpcDrops = npcDrops;
        OtherComputerRowsWithSameDrop = otherComputerRowsWithSameDrop;
        VisibleMismatches = mismatches;
    }

    /// <summary>ITM rows whose name equals "Null Wreckage" (ordinal).</summary>
    public IReadOnlyList<OriginalItem> ExactMatches { get; }
    /// <summary>ITM rows whose name equals "Null Wreckage" ignoring case (CFG Hold1 spells it "null wreckage").</summary>
    public IReadOnlyList<OriginalItem> CaseInsensitiveMatches { get; }
    /// <summary>ITM rows whose name contains "null" in any case.</summary>
    public IReadOnlyList<OriginalItem> ItemNamesContainingNull { get; }
    /// <summary>The ITM row named exactly "Wreckage" (null if absent).</summary>
    public OriginalItem? OrdinaryWreckage { get; }
    /// <summary>The DropItemId reference of each Null NPC row, in id order (401, 405).</summary>
    public IReadOnlyList<OriginalItemRef> NpcDrops { get; }
    /// <summary>VEH type-5 ids other than the Null NPCs whose DropItemId equals the Null NPCs' drop id (context: the drop is not unique to the Null).</summary>
    public IReadOnlyList<int> OtherComputerRowsWithSameDrop { get; }
    public IReadOnlyList<string> VisibleMismatches { get; }
}

/// <summary>
/// Data view of RT-F059 "Distinct Null NPCs" (data clause only): VEH ids 401 Null Death Orb (row 2) and 405 Null Kill Station (row 6) as validated
/// type-5 records with their item-id fields resolved, the pinned NWS lines 190, 191, 268, 340 and 367, and the Null Wreckage audit. LIO rows 19
/// ("DeathOrbSpawn", -401) and 23 ("KillStationSpawn", -405) carry the timed-spawn and warning strings but are NOT read here (the LIO reader is
/// a separate queue item). No actor AI, firing, targeting, faction, spawn timing, warning or drop behaviour is implemented or validated.
/// </summary>
public sealed class OriginalNullNpcs
{
    public static readonly int[] VehicleIds = [401, 405];
    public static readonly int[] NwsLines = [190, 191, 268, 340, 367];
    public const string NullWreckageName = "Null Wreckage";
    public const string OrdinaryWreckageName = "Wreckage";

    private OriginalNullNpcs(IReadOnlyList<OriginalNullNpc> npcs, IReadOnlyList<OriginalNwsLine> nws, OriginalNullWreckageAudit audit)
    {
        Npcs = npcs;
        Nws = nws;
        WreckageAudit = audit;
    }

    /// <summary>VEH ids 401 and 405 in that order.</summary>
    public IReadOnlyList<OriginalNullNpc> Npcs { get; }
    /// <summary>NWS/Rogue.nws lines 190, 191, 268, 340, 367 verbatim, in that order.</summary>
    public IReadOnlyList<OriginalNwsLine> Nws { get; }
    public OriginalNullWreckageAudit WreckageAudit { get; }

    public OriginalNullNpc ById(int vehicleId) =>
        Npcs.FirstOrDefault(n => n.VehicleId == vehicleId) ?? throw new KeyNotFoundException($"No Null NPC with VEH id {vehicleId}.");

    public OriginalNwsLine NwsLine(int lineNumber) =>
        Nws.FirstOrDefault(l => l.LineNumber == lineNumber) ?? throw new KeyNotFoundException($"NWS line {lineNumber} is not part of this view.");

    public static OriginalNullNpcs Load(string assetsRoot) =>
        Resolve(OriginalVehicleCatalog.Load(assetsRoot), OriginalItemCatalog.Load(assetsRoot), OriginalNws.LoadRogue(assetsRoot));

    public static OriginalNullNpcs Resolve(OriginalVehicleCatalog vehicles, OriginalItemCatalog items, OriginalNws nws)
    {
        var lines = NwsLines.Select(n => nws.Range(n, n)[0]).ToList();
        Expect(lines, 190, "Null Death Orbs");
        Expect(lines, 191, "Null Kill Stations");
        Expect(lines, 268, "Null Wreckage");
        Expect(lines, 340, "Null/Pirates");
        Expect(lines, 367, "Killsat/Deathorb");

        var npcs = new List<OriginalNullNpc>();
        foreach (var id in VehicleIds)
        {
            var veh = vehicles.All.SingleOrDefault(v => v.Id == id) ?? throw new OriginalDataException($"VEH has no row with id {id}.");
            npcs.Add(new OriginalNullNpc(new OriginalComputerRecord(veh), items));
        }
        return new OriginalNullNpcs(npcs, lines, Audit(vehicles, items, npcs));
    }

    private static void Expect(IReadOnlyList<OriginalNwsLine> lines, int number, string needle)
    {
        var line = lines.Single(l => l.LineNumber == number);
        if (!line.Text.Contains(needle, StringComparison.Ordinal))
            throw new OriginalDataException($"NWS line {number} does not contain '{needle}': '{line.Text}'.");
    }

    internal static OriginalNullWreckageAudit Audit(OriginalVehicleCatalog vehicles, OriginalItemCatalog items, IReadOnlyList<OriginalNullNpc> npcs)
    {
        var exact = items.Items.Where(i => string.Equals(i.Name, NullWreckageName, StringComparison.Ordinal)).ToList();
        var ignoreCase = items.Items.Where(i => string.Equals(i.Name, NullWreckageName, StringComparison.OrdinalIgnoreCase)).ToList();
        var nullNamed = items.Items.Where(i => i.Name.Contains("null", StringComparison.OrdinalIgnoreCase)).ToList();
        var ordinary = items.Items.FirstOrDefault(i => string.Equals(i.Name, OrdinaryWreckageName, StringComparison.Ordinal));
        var drops = npcs.Select(n => n.Drop).ToList();
        var dropIds = drops.Select(d => d.RawId).Distinct().ToList();
        var others = vehicles.Computers
            .Where(v => !VehicleIds.Contains(v.Id) && dropIds.Contains(new OriginalComputerRecord(v).DropItemId.Value))
            .Select(v => v.Id).ToList();
        return new OriginalNullWreckageAudit(exact, ignoreCase, nullNamed, ordinary, drops, others, DescribeMismatches(npcs, exact, ignoreCase, ordinary, others));
    }

    internal static IReadOnlyList<string> DescribeMismatches(IReadOnlyList<OriginalNullNpc> npcs, IReadOnlyList<OriginalItem> exact,
        IReadOnlyList<OriginalItem> ignoreCase, OriginalItem? ordinary, IReadOnlyList<int> otherRowsWithSameDrop)
    {
        var notes = new List<string>();
        if (exact.Count == 0)
            notes.Add($"NWS 268 names a unique '{NullWreckageName}' item but no ITM row is named exactly '{NullWreckageName}'" +
                      (ignoreCase.Count == 0 ? " (nor case-insensitively)." : $" (case-insensitive matches: {string.Join(", ", ignoreCase.Select(i => i.Id))})."));
        foreach (var n in npcs)
        {
            var d = n.Drop;
            if (!d.Resolved)
                notes.Add($"{n.Name} ({n.VehicleId}) f[{d.FieldIndex}] DropItemId={d.RawId} resolves to no ITM row.");
            else if (ordinary != null && d.RawId == ordinary.Id)
                notes.Add($"{n.Name} ({n.VehicleId}) f[{d.FieldIndex}] DropItemId={d.RawId} is ordinary '{d.ItemName}' x{n.DropItemQuantity.Value}, not a distinct '{NullWreckageName}'.");
            else
                notes.Add($"{n.Name} ({n.VehicleId}) f[{d.FieldIndex}] DropItemId={d.RawId} resolves to '{d.ItemName}' (ITM type {d.ItemRecordType}).");
        }
        if (otherRowsWithSameDrop.Count > 0)
            notes.Add($"The same DropItemId is also set on VEH ids {string.Join(", ", otherRowsWithSameDrop)}, so the drop is not unique to the Null NPCs.");
        return notes;
    }
}
