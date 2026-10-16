namespace RogueTrader.OriginalData;

/// <summary>A raw item-id field of VEH row 4 beside the ITM row it names (null when the raw value is 0, the empty slot value; nothing is invented for it).</summary>
public sealed record OriginalPirateStationItemSlot(OriginalInt Raw, OriginalItem? Item);

/// <summary>A raw vehicle-id field (ChildVehicles) beside the VEH row it names (null when the raw value is 0).</summary>
public sealed record OriginalPirateStationVehicleSlot(OriginalInt Raw, OriginalVehicleRecord? Vehicle);

/// <summary>One skill id of the f[234] ownership expression and the RPG hull row whose f[1] skill id it equals (null when no hull has that id).</summary>
public sealed record OriginalPirateStationOwnershipTerm(int SkillId, OriginalHull? Hull);

/// <summary>Raw destruction-drop fields of a type-5 row, used to compare VEH 403 with the other rows NWS line 340 may refer to.</summary>
public sealed record OriginalPirateStationDropRow(int VehicleId, string Name, int Hitpoints, int DropItemId, int DropItemQuantity, int ExplodeItemId);

/// <summary>
/// Data view of RT-F060 "Distinct Pirate NPC/base" (data clause only): VEH row 4, id 403 PirateStation, as a validated type-5 (Computer) v53 record
/// with its raw combat, loadout, drop, density, sprite and ownership-logic fields, the ITM/VEH rows those fields name, the ownership expression parsed by
/// <see cref="OriginalSkillLogic"/>, and NWS lines 21, 51, 338 and 340. Field names are upstream-parser names (InfantryOnline bb97579e), not
/// original-executable evidence. Every raw value is kept exactly; nothing is scaled, defaulted or corrected.
/// NOT resolved here (RT-F060 stays open): the actor that controls the station (VEH f[44] IsControllable = 1 only says the record is flagged), which of its
/// ChildVehicles or inventory it fires, spawn and respawn (LIO row 20 "Pirate Base", actor -403, timed 48000/48000 and its warning text, is queue G6 and is not read
/// here), team membership, how the "large amount of wreckage" is produced, and the original presentation.
/// </summary>
public sealed class OriginalPirateStation
{
    public const int VehicleId = 403;
    public const int VehicleRowNumber = 4;
    public const string VehicleName = "PirateStation";
    /// <summary>NWS lines kept: 21 and 51 (lore text naming Pirates), 338 (player wreckage), 340 ("Null/Pirates drop a large amount of wreckage").</summary>
    public static readonly int[] NwsLines = [21, 51, 338, 340];
    public const int NwsWreckageLine = 340;
    public const int NwsPlayerWreckageLine = 338;
    /// <summary>Type-5 rows whose drop is shown beside 403: 401 Null Death Orb and 405 Null Kill Station (NWS 340 says "Null/Pirates").</summary>
    public static readonly int[] NullDropComparisonIds = [401, 405];
    /// <summary>Work this data view does not cover.</summary>
    public static readonly IReadOnlyList<string> RemainingWork =
    [
        "LIO/Rogue.lio row 20 (type 5 v23 id 48 'Pirate Base': raw special 6, actor -403, timed 48000/48000, warning text) is not read here; queue G6 owns the LIO reader.",
        "Which actor/controller drives the station (f[44] IsControllable = 1 is a raw flag only), which inventory item or child vehicle it fires, and its team relationship.",
        "Spawn, respawn timing and the destruction drop amount ('large amount of wreckage') in native behaviour.",
        "Original presentation (sprites, radar, alert text, sound) and a verified completed NPC feature.",
    ];

    private OriginalPirateStation(
        OriginalComputerRecord record,
        IReadOnlyList<OriginalPirateStationItemSlot> inventory,
        IReadOnlyList<OriginalPirateStationVehicleSlot> children,
        OriginalPirateStationItemSlot explode,
        OriginalPirateStationItemSlot drop,
        SkillLogicNode ownership,
        IReadOnlyList<OriginalPirateStationOwnershipTerm> terms,
        IReadOnlyList<OriginalComputerProduct> nonEmptyProducts,
        IReadOnlyList<OriginalNwsLine> nws,
        IReadOnlyList<OriginalPirateStationDropRow> nullDrops,
        IReadOnlyList<string> mismatches)
    {
        Record = record;
        Inventory = inventory;
        ChildVehicles = children;
        Explode = explode;
        Drop = drop;
        OwnershipLogic = ownership;
        OwnershipTerms = terms;
        NonEmptyProducts = nonEmptyProducts;
        Nws = nws;
        NullDropComparison = nullDrops;
        VisibleMismatches = mismatches;
    }

    /// <summary>VEH row 4 as a validated type-5 v53 477-field record.</summary>
    public OriginalComputerRecord Record { get; }

    // ---- raw combat / behaviour fields (upstream-parser names) ----
    public OriginalInt Hitpoints => Record.Hitpoints;
    public OriginalInt IsControllable => Record.Int(VehComputerFields.IsControllable);
    public OriginalInt DisplayHealth => Record.DisplayHealth;
    public OriginalInt BarrelLength => Record.Int(VehComputerFields.BarrelLength);
    public OriginalInt PhysicalRadius => Record.PhysicalRadius;
    public OriginalInt TriggerRadius => Record.TriggerRadius;
    public OriginalInt RotateSpeed => Record.RotateSpeed;
    public OriginalInt TrackingTime => Record.TrackingTime;
    public OriginalInt TrackingRadius => Record.TrackingRadius;
    public OriginalInt TrackingWeightLow => Record.TrackingWeightLow;
    public OriginalInt TrackingWeightHigh => Record.TrackingWeightHigh;
    public OriginalInt FireRadius => Record.FireRadius;
    public OriginalInt RepairRate => Record.RepairRate;
    public OriginalInt HitpointsRequiredToOperate => Record.HitpointsRequiredToOperate;
    public OriginalInt ObeyLos => Record.ObeyLos;
    public OriginalInt Destroyable => Record.Destroyable;
    public OriginalInt ChainedTurretId => Record.ChainedTurretId;
    public OriginalInt DensityRadius => Record.DensityRadius;
    public OriginalInt DensityMaxType => Record.DensityMaxType;
    public OriginalInt DensityMaxActive => Record.DensityMaxActive;
    public OriginalInt DensityMaxInactive => Record.DensityMaxInactive;
    public OriginalInt MaxTypeByPlayerRegardlessOfTeam => Record.MaxTypeByPlayerRegardlessOfTeam;
    public OriginalInt FrequencyMaxType => Record.FrequencyMaxType;
    public OriginalInt FrequencyMaxActive => Record.FrequencyMaxActive;
    public OriginalInt FrequencyMaxInactive => Record.FrequencyMaxInactive;
    public OriginalInt FrequencyDensityMaxType => Record.FrequencyDensityMaxType;
    public OriginalInt FrequencyDensityMaxActive => Record.FrequencyDensityMaxActive;
    public OriginalInt FrequencyDensityMaxInactive => Record.FrequencyDensityMaxInactive;
    public OriginalInt DisplayOnFriendlyRadar => Record.Int(VehComputerFields.DisplayOnFriendlyRadar);
    public OriginalInt FriendlyRadarColor => Record.Int(VehComputerFields.FriendlyRadarColor);
    public OriginalInt DisplayOnEnemyRadar => Record.Int(VehComputerFields.DisplayOnEnemyRadar);
    public OriginalInt EnemyRadarColor => Record.Int(VehComputerFields.EnemyRadarColor);

    // ---- sprites (raw text) ----
    public RawField SpriteBaseBlobName => Record.SpriteBaseBlobName;
    public RawField SpriteBaseBlobId => Record.SpriteBaseBlobId;
    public RawField SpriteTurretBlobName => Record.SpriteTurretBlobName;
    public RawField SpriteTurretBlobId => Record.SpriteTurretBlobId;

    // ---- loadout / drop references ----
    /// <summary>InventoryItems0..5 (f[50..55]) with their ITM rows.</summary>
    public IReadOnlyList<OriginalPirateStationItemSlot> Inventory { get; }
    /// <summary>ChildVehicles0..7 (f[56..63]) with their VEH rows (all 0 in the pinned file).</summary>
    public IReadOnlyList<OriginalPirateStationVehicleSlot> ChildVehicles { get; }
    /// <summary>f[64] ExplodeItemId with its ITM row.</summary>
    public OriginalPirateStationItemSlot Explode { get; }
    /// <summary>f[68] DropItemId with its ITM row; <see cref="DropItemQuantity"/> is f[69].</summary>
    public OriginalPirateStationItemSlot Drop { get; }
    public OriginalInt DropItemQuantity => Record.DropItemQuantity;

    // ---- ownership logic ----
    /// <summary>f[234] LogicTakeOwnership, raw text.</summary>
    public RawField LogicTakeOwnershipRaw => Record.LogicTakeOwnership;
    /// <summary>f[235] LogicStealOwnership, raw text (empty for 403).</summary>
    public RawField LogicStealOwnershipRaw => Record.LogicStealOwnership;
    /// <summary>f[234] parsed by <see cref="OriginalSkillLogic.Parse"/>; its <c>ToString()</c> reproduces the raw text.</summary>
    public SkillLogicNode OwnershipLogic { get; }
    /// <summary>The skill ids of <see cref="OwnershipLogic"/> in written order, each beside the RPG hull row with that f[1] skill id.</summary>
    public IReadOnlyList<OriginalPirateStationOwnershipTerm> OwnershipTerms { get; }
    /// <summary>True when the written ids are exactly the twelve RPG hull skill ids (a set-equality fact; what the expression gates natively is not established).</summary>
    public bool OwnershipTermsAreExactlyTheTwelveHullSkillIds { get; init; }

    // ---- products, text, comparison, notes ----
    /// <summary>Product blocks (f[253 + 14k]) with any non-default field; empty for 403 in the pinned file.</summary>
    public IReadOnlyList<OriginalComputerProduct> NonEmptyProducts { get; }
    /// <summary>NWS/Rogue.nws lines 21, 51, 338 and 340, verbatim.</summary>
    public IReadOnlyList<OriginalNwsLine> Nws { get; }
    public OriginalNwsLine NwsWreckage => Nws.Single(l => l.LineNumber == NwsWreckageLine);
    /// <summary>Raw drop/explode fields of VEH 401 and 405 (the "Null" structures) in that order.</summary>
    public IReadOnlyList<OriginalPirateStationDropRow> NullDropComparison { get; }
    /// <summary>Human-readable notes where raw values do not line up with the NWS text. Informational; values are never changed.</summary>
    public IReadOnlyList<string> VisibleMismatches { get; }

    public static OriginalPirateStation Load(string assetsRoot) =>
        Resolve(OriginalVehicleCatalog.Load(assetsRoot), OriginalItemCatalog.Load(assetsRoot), OriginalHullCatalog.Load(assetsRoot), OriginalNws.LoadRogue(assetsRoot));

    public static OriginalPirateStation Resolve(OriginalVehicleCatalog vehicles, OriginalItemCatalog items, OriginalHullCatalog hulls, OriginalNws nws)
    {
        var veh = vehicles.All.SingleOrDefault(v => v.Id == VehicleId)
            ?? throw new OriginalDataException($"VEH has no row with id {VehicleId}.");
        var r = new OriginalComputerRecord(veh);
        if (r.RowNumber != VehicleRowNumber)
            throw new OriginalDataException($"VEH id {VehicleId} is row {r.RowNumber}, expected row {VehicleRowNumber}.");
        if (r.Name != VehicleName)
            throw new OriginalDataException($"VEH id {VehicleId} is named '{r.Name}', expected '{VehicleName}'.");

        var inventory = r.InventoryItems.Select(i => ItemSlot(items, i, r)).ToList();
        var children = r.ChildVehicles.Select(c => new OriginalPirateStationVehicleSlot(c, c.Value == 0 ? null
            : vehicles.All.SingleOrDefault(v => v.Id == c.Value) ?? throw new OriginalDataException($"{r.Record.SourceFile} row {r.RowNumber} f[{c.FieldIndex}]={c.Value} names no VEH row."))).ToList();
        var explode = ItemSlot(items, r.ExplodeItemId, r);
        var drop = ItemSlot(items, r.DropItemId, r);

        var text = r.LogicTakeOwnership.Value;
        var logic = OriginalSkillLogic.Parse(text);
        if (logic.ToString() != text)
            throw new OriginalDataException($"f[{VehComputerFields.LogicTakeOwnership}] '{text}' does not round-trip through the skill-logic parser ('{logic}').");
        var terms = new List<OriginalPirateStationOwnershipTerm>();
        if (logic is SkillLogicChain chain)
            foreach (var t in chain.Terms)
            {
                if (t is not SkillLogicSkill s) throw new OriginalDataException($"f[{VehComputerFields.LogicTakeOwnership}] term '{t}' is not a plain skill id.");
                terms.Add(new OriginalPirateStationOwnershipTerm(s.SkillId, hulls.Hulls.FirstOrDefault(h => h.SkillId == s.SkillId)));
            }
        else if (logic is SkillLogicSkill single)
            terms.Add(new OriginalPirateStationOwnershipTerm(single.SkillId, hulls.Hulls.FirstOrDefault(h => h.SkillId == single.SkillId)));
        var exact = terms.Count == hulls.Hulls.Count
            && terms.Select(t => t.SkillId).OrderBy(x => x).SequenceEqual(hulls.Hulls.Select(h => h.SkillId).OrderBy(x => x))
            && terms.All(t => t.Hull != null);

        var nonEmpty = r.Products.Where(p => !p.IsEmpty).ToList();
        var lines = NwsLines.Select(n => nws.Range(n, n)[0]).ToList();
        if (!lines.Single(l => l.LineNumber == NwsWreckageLine).Text.Contains("Pirates drop a large amount of wreckage", StringComparison.Ordinal))
            throw new OriginalDataException($"NWS line {NwsWreckageLine} does not contain 'Pirates drop a large amount of wreckage'.");

        var nullDrops = NullDropComparisonIds.Select(id =>
        {
            var v = vehicles.All.SingleOrDefault(x => x.Id == id) ?? throw new OriginalDataException($"VEH has no row with id {id}.");
            var c = new OriginalComputerRecord(v);
            return new OriginalPirateStationDropRow(c.Id, c.Name, c.Hitpoints.Value, c.DropItemId.Value, c.DropItemQuantity.Value, c.ExplodeItemId.Value);
        }).ToList();

        return new OriginalPirateStation(r, inventory, children, explode, drop, logic, terms, nonEmpty, lines, nullDrops, DescribeMismatches(r, drop, nullDrops, nonEmpty))
        {
            OwnershipTermsAreExactlyTheTwelveHullSkillIds = exact,
        };
    }

    private static OriginalPirateStationItemSlot ItemSlot(OriginalItemCatalog items, OriginalInt raw, OriginalComputerRecord r)
    {
        if (raw.Value == 0) return new OriginalPirateStationItemSlot(raw, null);
        var item = items.Items.FirstOrDefault(i => i.Id == raw.Value)
            ?? throw new OriginalDataException($"{r.Record.SourceFile} row {r.RowNumber} f[{raw.FieldIndex}]={raw.Value} names no ITM row.");
        return new OriginalPirateStationItemSlot(raw, item);
    }

    public static IReadOnlyList<string> DescribeMismatches(
        OriginalComputerRecord r, OriginalPirateStationItemSlot drop, IReadOnlyList<OriginalPirateStationDropRow> nullDrops, IReadOnlyList<OriginalComputerProduct> nonEmptyProducts)
    {
        var notes = new List<string>();
        var qty = r.DropItemQuantity.Value;
        if (qty <= 1)
            notes.Add($"NWS line {NwsWreckageLine} says Pirates drop 'a large amount of wreckage' but f[{VehComputerFields.DropItemId}] DropItemId={r.DropItemId.Value} ({drop.Item?.Name}) has f[{VehComputerFields.DropItemQuantity}] DropItemQuantity={qty}; the multiple-wreckage amount is not in these two fields.");
        if (nullDrops.All(d => d.DropItemId == r.DropItemId.Value && d.DropItemQuantity == qty))
            notes.Add($"VEH {string.Join(" and ", nullDrops.Select(d => $"{d.VehicleId} {d.Name}"))} carry the same drop {r.DropItemId.Value} x{qty}, so these fields do not set Pirates apart from the other 'Null' structures.");
        if (r.LogicStealOwnership.Value.Length == 0)
            notes.Add($"f[{VehComputerFields.LogicStealOwnership}] LogicStealOwnership is empty (LogicTakeOwnership f[{VehComputerFields.LogicTakeOwnership}] is not).");
        if (nonEmptyProducts.Count == 0)
            notes.Add("All 16 product blocks are default: the station sells and produces nothing in the VEH row.");
        if (r.ChildVehicles.All(c => c.Value == 0))
            notes.Add($"ChildVehicles f[{VehComputerFields.ChildVehiclesFirst}..{VehComputerFields.ChildVehiclesFirst + VehComputerFields.ChildVehiclesCount - 1}] are all 0: no child ships are named by the VEH row.");
        return notes;
    }
}
