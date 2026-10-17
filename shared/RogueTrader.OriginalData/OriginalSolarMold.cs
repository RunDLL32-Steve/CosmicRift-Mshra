using System.Text.RegularExpressions;

namespace RogueTrader.OriginalData;

/// <summary>
/// An item id found in a raw field of a Solar Mold / Mold Spore VEH row, with the ITM row it resolves to. The field role is the upstream-parser
/// name of the slot (InfantryOnline bb97579e), not original-executable evidence; resolving the id in ITM is the data lookup only and does not
/// establish how the native game uses the slot.
/// </summary>
public sealed record OriginalMoldItemReference(string FieldRole, int FieldIndex, int ItemId, OriginalItem Item);

/// <summary>Any other VEH or ITM field whose raw text equals a mold vehicle id (the id column itself excluded). A pure lookup; a numeric coincidence is possible.</summary>
public sealed record OriginalMoldIdHit(string Source, int Row, int FieldIndex, string Text);

/// <summary>Raw spawn-limit scalars f[218]..f[228] of a type-5 row (upstream-parser names), unscaled and uninterpreted.</summary>
public sealed record OriginalMoldSpawnLimits(
    OriginalInt DensityRadius, OriginalInt DensityMaxType, OriginalInt DensityMaxActive, OriginalInt DensityMaxInactive,
    OriginalInt MaxTypeByPlayerRegardlessOfTeam,
    OriginalInt FrequencyMaxType, OriginalInt FrequencyMaxActive, OriginalInt FrequencyMaxInactive,
    OriginalInt FrequencyDensityMaxType, OriginalInt FrequencyDensityMaxActive, OriginalInt FrequencyDensityMaxInactive);

/// <summary>Raw combat-related scalars of a type-5 row (upstream-parser names), unscaled and uninterpreted.</summary>
public sealed record OriginalMoldCombatFields(
    OriginalInt Hitpoints, OriginalInt RepairRate, OriginalInt HitpointsRequiredToOperate, OriginalInt Destroyable,
    OriginalInt RelativeId, OriginalInt BarrelLength, OriginalInt PhysicalRadius, OriginalInt TriggerRadius,
    OriginalInt EnergyRate, OriginalInt EnergyMin, OriginalInt RotateSpeed, OriginalInt TrackingTime, OriginalInt TrackingRadius,
    OriginalInt TrackingWeightHigh, OriginalInt FireRadius, OriginalInt ObeyLos);

/// <summary>One of the two mold actors as a validated type-5 record with raw combat, spawn-limit, product and reference views.</summary>
public sealed class OriginalMoldActor
{
    internal OriginalMoldActor(OriginalComputerRecord record, OriginalItemCatalog items, OriginalVehicleCatalog vehicles)
    {
        Record = record;
        Combat = new OriginalMoldCombatFields(
            record.Hitpoints, record.RepairRate, record.HitpointsRequiredToOperate, record.Destroyable, record.RelativeId,
            record.Int(VehComputerFields.BarrelLength), record.PhysicalRadius, record.TriggerRadius,
            record.Int(VehComputerFields.EnergyRate), record.Int(VehComputerFields.EnergyMin),
            record.RotateSpeed, record.TrackingTime, record.TrackingRadius, record.TrackingWeightHigh, record.FireRadius, record.ObeyLos);
        SpawnLimits = new OriginalMoldSpawnLimits(
            record.DensityRadius, record.DensityMaxType, record.DensityMaxActive, record.DensityMaxInactive, record.MaxTypeByPlayerRegardlessOfTeam,
            record.FrequencyMaxType, record.FrequencyMaxActive, record.FrequencyMaxInactive,
            record.FrequencyDensityMaxType, record.FrequencyDensityMaxActive, record.FrequencyDensityMaxInactive);
        NonEmptyProducts = record.Products.Where(p => !p.IsEmpty).ToList();

        var refs = new List<OriginalMoldItemReference>();
        for (var k = 0; k < VehComputerFields.InventoryItemsCount; k++)
            Add(refs, items, $"InventoryItems{k}", record.InventoryItems[k]);
        Add(refs, items, "ExplodeItemId", record.ExplodeItemId);
        Add(refs, items, "DropItemId", record.DropItemId);
        ItemReferences = refs;

        var hits = new List<OriginalMoldIdHit>();
        foreach (var v in vehicles.All)
            foreach (var f in v.Record.Fields)
                if (f.Value == Id.ToString() && !(v.Id == Id && f.Index == VehComputerFields.IdField))
                    hits.Add(new OriginalMoldIdHit(v.Record.SourceFile, v.RowNumber, f.Index, f.Value));
        foreach (var i in items.Items)
            foreach (var f in i.Record.Fields)
                if (f.Value == Id.ToString() && f.Index != ItmFields.Id)
                    hits.Add(new OriginalMoldIdHit(i.Record.SourceFile, i.RowNumber, f.Index, f.Value));
        OtherFieldsEqualToId = hits;
    }

    private static void Add(List<OriginalMoldItemReference> refs, OriginalItemCatalog items, string role, OriginalInt value)
    {
        if (value.Value > 0) refs.Add(new OriginalMoldItemReference(role, value.FieldIndex, value.Value, items.ById(value.Value)));
    }

    public OriginalComputerRecord Record { get; }
    public int Id => Record.Id;
    public string Name => Record.Name;
    public RawField Description => Record.Description;
    public OriginalMoldCombatFields Combat { get; }
    public OriginalMoldSpawnLimits SpawnLimits { get; }
    /// <summary>f[234] LogicTakeOwnership, raw text.</summary>
    public RawField LogicTakeOwnership => Record.LogicTakeOwnership;
    /// <summary>f[235] LogicStealOwnership, raw text.</summary>
    public RawField LogicStealOwnership => Record.LogicStealOwnership;
    /// <summary>f[237] SpriteBase BlobName / f[238] BlobId, raw text.</summary>
    public (RawField Name, RawField Id) SpriteBase => (Record.SpriteBaseBlobName, Record.SpriteBaseBlobId);
    /// <summary>f[245] SpriteTurret BlobName / f[246] BlobId, raw text.</summary>
    public (RawField Name, RawField Id) SpriteTurret => (Record.SpriteTurretBlobName, Record.SpriteTurretBlobId);
    /// <summary>The product blocks that are not the all-default block. Empty for both mold rows in the pinned file (they build nothing through this block).</summary>
    public IReadOnlyList<OriginalComputerProduct> NonEmptyProducts { get; }
    /// <summary>Every positive item id in InventoryItems0..5, ExplodeItemId and DropItemId, resolved in ITM. Zero and negative values are not references.</summary>
    public IReadOnlyList<OriginalMoldItemReference> ItemReferences { get; }
    /// <summary>All other VEH/ITM fields whose raw text equals this actor's id (empty in the pinned file for both).</summary>
    public IReadOnlyList<OriginalMoldIdHit> OtherFieldsEqualToId { get; }
}

/// <summary>An NWS line with the dated heading ("********M/D/YY") it sits under, both verbatim.</summary>
public sealed record OriginalDatedNwsLine(OriginalNwsLine Heading, OriginalNwsLine Line);

/// <summary>
/// Data view of RT-F062 "Solar Mold/spore ecology" (data clause only): VEH 409 Solar Mold and 414 Mold Spore as validated type-5 records, the
/// four NWS lines about them with their dated headings, and the mismatches that are visible in the pinned data. Spawn behaviour of the
/// mold (LIO rows 80/81, special 20, warnings), the native use of every field, the spore spawn/combat/drop behaviour and hazard or resource
/// interactions are NOT implemented or validated; nothing here is a simulation. Field names are upstream-parser names (InfantryOnline bb97579e).
/// </summary>
public sealed class OriginalSolarMold
{
    public const int SolarMoldId = 409;
    public const int MoldSporeId = 414;
    public const int NwsMoreThanOneMold = 278;
    public const int NwsSporesDropPrizes = 336;
    public const int NwsSpriteHitpoints = 337;
    public const int NwsFixedSpores = 367;

    private static readonly Regex HeadingPattern = new(@"^\*+\s*(\d{1,2}/\d{1,2}/\d{2})\s*$", RegexOptions.CultureInvariant);

    private OriginalSolarMold(OriginalMoldActor mold, OriginalMoldActor spore, IReadOnlyList<OriginalDatedNwsLine> nws)
    {
        SolarMold = mold;
        MoldSpore = spore;
        Nws = nws;
        VisibleMismatches = DescribeMismatches(mold, spore, nws);
    }

    public OriginalMoldActor SolarMold { get; }
    public OriginalMoldActor MoldSpore { get; }
    /// <summary>NWS lines 278, 336, 337 and 367 in that order, each with its dated heading, verbatim.</summary>
    public IReadOnlyList<OriginalDatedNwsLine> Nws { get; }
    /// <summary>Notes where the pinned values do not line up with the NWS text or the catalog wording. Informational; no value is changed.</summary>
    public IReadOnlyList<string> VisibleMismatches { get; }

    public OriginalDatedNwsLine NwsLine(int lineNumber) =>
        Nws.FirstOrDefault(n => n.Line.LineNumber == lineNumber) ?? throw new KeyNotFoundException($"NWS line {lineNumber} is not part of this view.");

    public static OriginalSolarMold Load(string assetsRoot) =>
        Resolve(OriginalVehicleCatalog.Load(assetsRoot), OriginalItemCatalog.Load(assetsRoot), OriginalNws.LoadRogue(assetsRoot));

    public static OriginalSolarMold Resolve(OriginalVehicleCatalog vehicles, OriginalItemCatalog items, OriginalNws nws)
    {
        OriginalMoldActor Actor(int id) => new(new OriginalComputerRecord(vehicles.ById(id)), items, vehicles);
        var mold = Actor(SolarMoldId);
        var spore = Actor(MoldSporeId);
        var lines = new List<OriginalDatedNwsLine>();
        foreach (var (number, needle) in new[]
        {
            (NwsMoreThanOneMold, "Solar Mold will now spawn"),
            (NwsSporesDropPrizes, "Mold Spores drop prizes"),
            (NwsSpriteHitpoints, "Mold Sprites have 35 HP"),
            (NwsFixedSpores, "Fixed Mold Spores"),
        })
        {
            var line = nws.Range(number, number)[0];
            if (!line.Text.Contains(needle, StringComparison.Ordinal))
                throw new OriginalDataException($"NWS line {number} does not contain '{needle}': '{line.Text}'.");
            lines.Add(new OriginalDatedNwsLine(FindHeading(nws, number), line));
        }
        return new OriginalSolarMold(mold, spore, lines);
    }

    private static OriginalNwsLine FindHeading(OriginalNws nws, int lineNumber)
    {
        for (var n = lineNumber; n >= 1; n--)
        {
            var l = nws.Lines[n - 1];
            if (HeadingPattern.IsMatch(l.Text)) return l;
        }
        throw new OriginalDataException($"NWS line {lineNumber} has no dated heading above it.");
    }

    private static IReadOnlyList<string> DescribeMismatches(OriginalMoldActor mold, OriginalMoldActor spore, IReadOnlyList<OriginalDatedNwsLine> nws)
    {
        var notes = new List<string>();
        var hp = nws.Single(n => n.Line.LineNumber == NwsSpriteHitpoints);
        if (spore.Combat.Hitpoints.Value != 35)
            notes.Add($"NWS line {hp.Line.LineNumber} ({hp.Heading.Text}) says 'Mold Sprites have 35 HP now' but VEH {spore.Id} {spore.Name} f[{VehComputerFields.Hitpoints}] Hitpoints={spore.Combat.Hitpoints.Value}; no VEH row is named Sprite, the link between 'sprites' and this row is unproven.");
        var prizes = nws.Single(n => n.Line.LineNumber == NwsSporesDropPrizes);
        notes.Add($"NWS line {prizes.Line.LineNumber} says Mold Spores drop prizes; VEH {spore.Id} f[{VehComputerFields.DropItemId}] DropItemId={spore.Record.DropItemId.Value} x{spore.Record.DropItemQuantity.Value} resolves in ITM to '{spore.ItemReferences.Single(r => r.FieldRole == "DropItemId").Item.Name}' (VEH id {spore.Record.DropItemId.Value} is a different row in VEH); whether that is the prize is unproven.");
        var fix = nws.Single(n => n.Line.LineNumber == NwsFixedSpores);
        notes.Add($"NWS line {fix.Line.LineNumber} sits under heading '{fix.Heading.Text}' (2002 if read as M/D/YY), outside the 2003 window the catalog row names.");
        var many = nws.Single(n => n.Line.LineNumber == NwsMoreThanOneMold);
        if (mold.SpawnLimits.DensityMaxType.Value == 0 && mold.SpawnLimits.FrequencyMaxType.Value == 0)
            notes.Add($"NWS line {many.Line.LineNumber} ({many.Heading.Text}) says more than 1 Solar Mold spawns with spores/sprites around them, but VEH {mold.Id} has Density/Frequency spawn-limit fields f[{VehComputerFields.DensityRadius}]..f[{VehComputerFields.FrequencyDensityMaxInactive}] DensityMaxType, FrequencyMaxType, FrequencyDensityMaxType all 0 (the limits sit on VEH {spore.Id}: {spore.SpawnLimits.DensityMaxType.Value}/{spore.SpawnLimits.FrequencyMaxType.Value}/{spore.SpawnLimits.FrequencyDensityMaxType.Value}); mold count, spawn time and radius live in LIO, which is not read here.");
        if (mold.NonEmptyProducts.Count == 0)
            notes.Add($"VEH {mold.Id} has no non-empty product block and no item or vehicle reference to {spore.Id}; how it 'releases spores' is not expressed in its VEH row.");
        if (mold.Combat.RelativeId.Value != 0 && spore.Combat.RelativeId.Value != mold.Combat.RelativeId.Value)
            notes.Add($"f[{VehComputerFields.RelativeId}] RelativeId is {mold.Combat.RelativeId.Value} on VEH {mold.Id} but {spore.Combat.RelativeId.Value} on VEH {spore.Id}; the catalog's 'faction 20' cannot be read from the spore row and the meaning of RelativeId is unvalidated.");
        return notes;
    }
}
