namespace RogueTrader.OriginalData;

/// <summary>
/// Data view of RT-F061 "Meteor hazard" (data clause only): VEH row 3 (id 402 "MeteorShower", a v53 type-5 Computer record) as a validated
/// <see cref="OriginalComputerRecord"/>, the single ITM item it lists in InventoryItems0 (f[50] = 1007 "Meteor", category "Child Effects"),
/// that item's raw projectile view and reference chain where the type-6/227-field layout allows it, and the pinned NWS line 188. Field
/// names on the VEH record are upstream-parser names (InfantryOnline bb97579e), NOT original-executable evidence. Nothing here decodes the
/// native trap event, the spawn area or timing (the LIO rows 18/49/52/57 are not read: queue G6), meteor movement, collision or damage; the
/// raw numbers are exposed unscaled and uninterpreted, so no hazard behaviour is implemented or validated.
/// </summary>
public sealed class OriginalMeteorShower
{
    public const int VehicleId = 402;
    public const int WeaponItemId = 1007;
    public const int NwsLine = 188;

    private OriginalMeteorShower(OriginalComputerRecord record, OriginalItem weapon, OriginalProjectile? projectile, OriginalWeaponChain? chain, OriginalNwsLine nws)
    {
        Record = record;
        Weapon = weapon;
        Projectile = projectile;
        Chain = chain;
        Nws = nws;
    }

    /// <summary>VEH row for id 402 as a guarded type-5 record.</summary>
    public OriginalComputerRecord Record { get; }
    /// <summary>The ITM row resolved from InventoryItems0 f[50] (id 1007).</summary>
    public OriginalItem Weapon { get; }
    /// <summary>Raw projectile view of <see cref="Weapon"/>; null when the row is not the type-6/227-field layout.</summary>
    public OriginalProjectile? Projectile { get; }
    /// <summary>Ammo (f[31]) and child-effect (f[142]) chain of <see cref="Weapon"/>; null when the row is not a projectile layout. For item 1007 both references are 0, so the chain is empty.</summary>
    public OriginalWeaponChain? Chain { get; }
    /// <summary>NWS/Rogue.nws line 188, verbatim.</summary>
    public OriginalNwsLine Nws { get; }

    /// <summary>InventoryItems0..5 (f[50]..f[55]) raw; only slot 0 is non-zero in the pinned row.</summary>
    public IReadOnlyList<OriginalInt> InventoryItems => Record.InventoryItems;
    /// <summary>f[74] and f[75]: Sprites0 blob name and id, raw text.</summary>
    public RawField Sprite0BlobName => Record.Field(VehComputerFields.AnimatedSprite(0) + 4 + VehComputerFields.BlobName);
    public RawField Sprite0BlobId => Record.Field(VehComputerFields.AnimatedSprite(0) + 4 + VehComputerFields.BlobId);
    /// <summary>f[94] EnergyMax, raw (upstream-parser name; units and native use not established).</summary>
    public OriginalInt EnergyMax => Record.Int(VehComputerFields.EnergyMax);
    /// <summary>f[95] EnergyRate, raw.</summary>
    public OriginalInt EnergyRate => Record.Int(VehComputerFields.EnergyRate);

    public static OriginalMeteorShower Load(string assetsRoot) =>
        Resolve(OriginalVehicleCatalog.Load(assetsRoot), OriginalItemCatalog.Load(assetsRoot), OriginalNws.LoadRogue(assetsRoot));

    public static OriginalMeteorShower Resolve(OriginalVehicleCatalog vehicles, OriginalItemCatalog items, OriginalNws nws)
    {
        var veh = vehicles.All.SingleOrDefault(v => v.Id == VehicleId)
            ?? throw new OriginalDataException($"VEH has no row with id {VehicleId}.");
        var record = new OriginalComputerRecord(veh);

        var line = nws.Range(NwsLine, NwsLine)[0];
        if (!line.Text.Contains("Meteor Showers", StringComparison.Ordinal))
            throw new OriginalDataException($"NWS line {NwsLine} does not contain 'Meteor Showers': '{line.Text}'.");

        var itemId = record.InventoryItems[0].Value;
        if (itemId != WeaponItemId)
            throw new OriginalDataException($"VEH row {record.RowNumber} f[{VehComputerFields.InventoryItemsFirst}]={itemId}, expected {WeaponItemId}.");
        OriginalItem weapon;
        try { weapon = items.ById(itemId); }
        catch (KeyNotFoundException) { throw new OriginalItemException($"VEH row {record.RowNumber} f[{VehComputerFields.InventoryItemsFirst}]={itemId}: no ITM item with that id."); }

        var isProjectile = OriginalProjectile.TryOf(weapon, out var projectile);
        var chain = isProjectile ? OriginalWeaponChain.Resolve(items, weapon) : null;
        return new OriginalMeteorShower(record, weapon, projectile, chain, line);
    }
}
