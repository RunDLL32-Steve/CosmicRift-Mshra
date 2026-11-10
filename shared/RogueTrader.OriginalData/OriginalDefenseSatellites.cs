namespace RogueTrader.OriginalData;

/// <summary>
/// One station defense satellite row of LIO/Rogue.lio (RT-F351..RT-F362, data only): a type 5 v23 "hide" record whose f[20] is -410,
/// the negative id of VEH 410 "GunSat" (the emulator reads a negative f[20] as a vehicle to place, <c>ArenaLio.hideSpawn</c> at
/// bb97579). Every integer field is kept raw by index; f[25] is the AssignFrequency, the team the satellite belongs to.
/// </summary>
public sealed record OriginalDefenseSatelliteSpawn(int Row, int Id, string Name, IReadOnlyList<OriginalInt> Raw)
{
    /// <summary>Raw integer field <paramref name="index"/> (f[4..22] and f[24..27]).</summary>
    public int F(int index) => Raw.Single(r => r.FieldIndex == index).Value;
}

/// <summary>
/// Data view of the station defense satellites (RT-F278 GunSat, RT-F168 PirateStationCannon, RT-F351..RT-F362): VEH id 410 as a
/// validated <see cref="OriginalComputerRecord"/>, the ITM item it lists in InventoryItems0 (f[50] = 1038 "PirateStationCannon") with
/// its raw projectile view, and the twelve LIO hide rows that place it. Field names are upstream-parser names (InfantryOnline
/// bb97579e), not original-executable evidence; nothing is scaled here.
/// </summary>
public sealed class OriginalDefenseSatellites
{
    public const int VehicleId = 410;
    public const int WeaponItemId = 1038;
    public const int ActorField = 20;
    /// <summary>f[25] AssignFrequency of a hide row: the team the placed vehicle belongs to.</summary>
    public const int AssignFrequencyField = 25;
    /// <summary>f[20] of a defense satellite row: minus <see cref="VehicleId"/>.</summary>
    public const int ActorValue = -VehicleId;

    private OriginalDefenseSatellites(OriginalComputerRecord record, OriginalItem weapon, OriginalProjectile projectile,
        IReadOnlyList<OriginalDefenseSatelliteSpawn> spawns)
    {
        Record = record;
        Weapon = weapon;
        Projectile = projectile;
        Spawns = spawns;
    }

    /// <summary>VEH row for id 410 as a guarded type-5 record.</summary>
    public OriginalComputerRecord Record { get; }
    /// <summary>The ITM row resolved from InventoryItems0 f[50] (id 1038).</summary>
    public OriginalItem Weapon { get; }
    /// <summary>Raw projectile view of <see cref="Weapon"/>.</summary>
    public OriginalProjectile Projectile { get; }
    /// <summary>The LIO hide rows that place VEH 410, in file order.</summary>
    public IReadOnlyList<OriginalDefenseSatelliteSpawn> Spawns { get; }

    /// <summary>ITM f[58] reloadDelayNormal of <see cref="Weapon"/> (the emulator's turret waits this x 10 ms after a clip), raw.</summary>
    public OriginalInt ReloadDelayNormal => Weapon.Record.Int(ReloadDelayNormalField);
    public const int ReloadDelayNormalField = 58;

    public static OriginalDefenseSatellites Load(string assetsRoot)
    {
        var lio = AssetIntegrity.ReadVerified(assetsRoot, OriginalSafeZones.LioRogue);
        return Resolve(OriginalVehicleCatalog.Load(assetsRoot), OriginalItemCatalog.Load(assetsRoot),
            RawRecordReader.Parse(lio, OriginalSafeZones.LioRogue.RelativePath));
    }

    public static OriginalDefenseSatellites Resolve(OriginalVehicleCatalog vehicles, OriginalItemCatalog items, IReadOnlyList<RawRecord> lio)
    {
        ArgumentNullException.ThrowIfNull(vehicles);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(lio);
        var veh = vehicles.All.SingleOrDefault(v => v.Id == VehicleId)
            ?? throw new OriginalDataException($"VEH has no row with id {VehicleId}.");
        var record = new OriginalComputerRecord(veh);
        var itemId = record.InventoryItems[0].Value;
        if (itemId != WeaponItemId)
            throw new OriginalDataException($"VEH row {record.RowNumber} f[{VehComputerFields.InventoryItemsFirst}]={itemId}, expected {WeaponItemId}.");
        OriginalItem weapon;
        try { weapon = items.ById(itemId); }
        catch (KeyNotFoundException) { throw new OriginalItemException($"VEH row {record.RowNumber} f[{VehComputerFields.InventoryItemsFirst}]={itemId}: no ITM item with that id."); }
        if (!OriginalProjectile.TryOf(weapon, out var projectile) || projectile is null)
            throw new OriginalDataException($"ITM {weapon.Id} '{weapon.Name}' is not a projectile record.");

        var spawns = new List<OriginalDefenseSatelliteSpawn>();
        foreach (var r in lio)
        {
            if (r.FieldCount == 0 || r.Value(0) != "5") continue;
            if (r.FieldCount != OriginalMineralSpawns.FieldCount || r.VersionToken != "v23")
                throw new OriginalDataException($"LIO row {r.RowNumber}: type 5 record has {r.FieldCount} fields ({r.VersionToken}), expected {OriginalMineralSpawns.FieldCount} (v23).");
            if (r.Int(ActorField).Value != ActorValue) continue;
            spawns.Add(new OriginalDefenseSatelliteSpawn(r.RowNumber, r.Int(2).Value, r.Value(3).Trim('"'),
                OriginalMineralSpawns.RawFieldIndexes.Select(r.Int).ToArray()));
        }
        return new OriginalDefenseSatellites(record, weapon, projectile, spawns);
    }
}
