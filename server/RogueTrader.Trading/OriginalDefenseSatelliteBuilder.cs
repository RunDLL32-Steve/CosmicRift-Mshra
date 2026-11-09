using System.Globalization;
using System.Text.Json;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>Raw evidence for one derived defense satellite spawner: the LIO row and every integer field it was read from.</summary>
public sealed record DefenseSatelliteSpawnSource(int LioRow, string Name, IReadOnlyDictionary<string, int> Raw);

/// <summary>A derived defense satellite spawner: the simulation values plus where they came from.</summary>
public sealed record DerivedDefenseSatelliteSpawn(string Id, string Name, int Team, string TeamName, float X, float Y, float Width, float Height,
    int AttemptTicks, int SucceedTicks, int InitialCount, int Probability, int MinPlayers, int MaxPlayers, float MinPlayerDistance,
    float MaxPlayerDistance, int MaxInArea, int MaxInLevel, DefenseSatelliteSpawnSource Source);

/// <summary>Raw VEH 410 and ITM 1038 values behind the derived satellite.</summary>
public sealed record DefenseSatelliteSource(int VehRow, int ItmRow, IReadOnlyDictionary<string, int> Raw);

/// <summary>The satellite part of the document: <see cref="DefenseSatelliteDefinition"/> plus its raw evidence.</summary>
public sealed record DerivedDefenseSatellite(string VehicleId, string Name, float MaximumHull, float Radius, float TrackingRadius, float FireRadius,
    int FireIntervalTicks, int ClipSize, int ReloadTicks, int RepairIntervalTicks, float RepairAmount, float HullToOperate, bool ObeyLos,
    WeaponDefinition Cannon, DefenseSatelliteSource Source);

/// <summary>The document behind <c>data/defenses/defense_satellites.json</c>; <see cref="DefenseSatelliteCatalog.FromJson"/> reads it.</summary>
public sealed record DefenseSatelliteDocument(string Id, string EvidenceClassification, string Source, IReadOnlyList<string> NewRules,
    ulong Seed, DerivedDefenseSatellite Satellite, IReadOnlyList<DerivedDefenseSatelliteSpawn> Spawns)
{
    public string ToJson() => JsonSerializer.Serialize(this, StationDefinition.Json).ReplaceLineEndings("\n") + "\n";
}

/// <summary>
/// Derives the station defense satellites of the Rogue map (RT-F278 GunSat, RT-F351..RT-F362) from the pinned originals: the twelve
/// LIO/Rogue.lio hide rows that place VEH 410 GunSat for a team, the GunSat's VEH record and the ITM 1038 PirateStationCannon it fires.
/// Hide field meanings and the spawn loop follow the InfantryOnline emulator as the meteor showers do
/// (<see cref="OriginalMeteorShowerBuilder"/>); the turret follows its <c>Computer.poll</c> and <c>isValidTarget</c>. The NEW readings
/// are listed in <see cref="NewRules"/>.
/// </summary>
public static class OriginalDefenseSatelliteBuilder
{
    public const string DocumentId = "original_defense_satellites";
    /// <summary>NEW placement seed; the client derives a fresh one per sector instance.</summary>
    public const ulong Seed = 0x52545F4445465341UL; // "RT_DEFSA"
    /// <summary>The cannon whose provisional speed, size and life scale the satellite's shot (the player's Plasma Cannon, ITM 1004).</summary>
    public const int ReferenceCannonId = OriginalMeteorShowerBuilder.ReferenceCannonId;
    /// <summary>The emulator's turret timers count in 10 ms units (fireDelay and reloadDelayNormal x 10, RepairRate x 10).</summary>
    public const float TicksPerTenMilliseconds = WorldSimulation.TickRate / 100f;

    private const int FX = 4, FY = 5, FWidth = 6, FHeight = 7, FInitialCount = 10, FAttemptDelay = 11, FSucceedDelay = 12,
        FProbability = 13, FMinPlayers = 14, FMaxPlayers = 15, FMinPlayerDistance = 16, FMaxPlayerDistance = 17, FMaxTypeInArea = 18,
        FMaxTypeInLevel = 19;

    public static readonly IReadOnlyList<string> NewRules =
    [
        "Spawners: LIO type 5 v23 rows whose f[20] is -410 place VEH 410 GunSat, as the emulator reads a negative f[20] (ArenaLio.hideSpawn/hideVehicle at bb97579). f[10] InitialCount, f[11]/f[12] Attempt/SucceedDelay in tenths of a second (6000 = 600 s), f[13] Probability, f[14]/f[15] Min/MaxPlayers, f[16]/f[17] Min/MaxPlayerDistance (99999 = no check), f[18] MaxTypeInArea (1: a satellite comes back only when its own area is empty), f[19] MaxTypeInLevel, f[25] AssignFrequency = the team the satellite belongs to. The emulator is a reconstruction, not the native server (RECONSTRUCTED).",
        "Positions f[4]/f[5] are the area centre and f[6]/f[7] its full size in map pixels, moved by the LVL offset (48 px) like every LIO position; a point on a blocked tile is retried up to 10 times.",
        "Turret (emulator Computer.poll and isValidTarget): it fires only at a pilot of another team who is alive, within VEH TrackingRadius and FireRadius (both 2500), and not hidden behind a wall; one shot every ITM fireDelay (25 x 10 ms), and after ammoCapacity shots (5) it reloads for reloadDelayNormal (150 x 10 ms). Shots are aimed where the pilot will be if they hold course (computeLeadFireAngle, without its 256-step angle and 0.7 isometric Y scale). TrackingWeightLow/High (0 to 10,000,000) is not checked: every player hull weighs 10,000 (VEH f[7]), inside it. AngleStart/AngleLength are 0: no angle limit. RotateSpeed and TrackingTime are kept raw; the emulator turns the turret at once.",
        "NEW line of sight: VEH ObeyLos=1 hides a pilot behind walls. The emulator walks LVL vision tiles; the map here only has blocked tiles, so a wall between the satellite and the pilot stands in for them.",
        "Repair: while its hull is at least HitpointsRequiredToOperate (5) it repairs RepairRate / 100 hull every RepairRate x 10 ms (0.5 every 0.5 s for the GunSat), as Computer.poll reads the field; below 5 hull it neither repairs nor fires. NEW: the emulator ties the repair step to its update-packet timer; here it is a steady interval. Repairs keep running in real time while the world does not tick, like every sector timer (owner rule, November 2, 2026).",
        "Damage: the player's shots take their hull damage off the satellite (it has no shield and no energy: VEH EnergyMax 0, ComputerEnergyMax 0). The player's shots pass through satellites of the player's own team. Armour and damage types are not applied (RT-F041). Ships fly through satellites (no ship collision yet).",
        "Destruction: at 0 hull the satellite is removed and its spawner places a new one on a later attempt. NEW: no credits or bounty are paid for it, and its VEH ExplodeItemId (3004) and DropItemId (2008 Wreckage x1) are not applied yet; the drop is left to the RT-F043 drop work.",
        "NEW shot speed, life and size: the projectile units are open (G10 W2/W3), so the cannon's raw muzzle velocity, alive time and proximity radius are scaled by the same factors that turn the player's Plasma Cannon (ITM 1004) raw values into its provisional ones, as the meteor showers do.",
    ];

    public static DefenseSatelliteDocument Build(string assetsRoot)
    {
        var satellites = OriginalDefenseSatellites.Load(assetsRoot);
        var items = OriginalItemCatalog.Load(assetsRoot);
        var cfg = OriginalCfg.LoadRogue(assetsRoot);
        var hulls = OriginalHullCatalog.Load(assetsRoot);
        return Build(satellites, items.ById(ReferenceCannonId), cfg, hulls);
    }

    public static DefenseSatelliteDocument Build(OriginalDefenseSatellites satellites, OriginalItem referenceCannon, OriginalCfg cfg,
        OriginalHullCatalog hulls)
    {
        ArgumentNullException.ThrowIfNull(satellites);
        ArgumentNullException.ThrowIfNull(referenceCannon);
        ArgumentNullException.ThrowIfNull(cfg);
        ArgumentNullException.ThrowIfNull(hulls);
        var record = satellites.Record;
        var projectile = satellites.Projectile;
        if (record.Destroyable.Value != 1)
            throw new OriginalDataException($"VEH {record.Id} Destroyable={record.Destroyable.Value}; the builder reads a destroyable satellite.");
        if (record.AngleStart.Value != 0 || record.AngleLength.Value != 0)
            throw new OriginalDataException($"VEH {record.Id} has an angle limit ({record.AngleStart.Value}, {record.AngleLength.Value}); the builder reads none.");
        if (record.ComputerEnergyMax.Value != 0)
            throw new OriginalDataException($"VEH {record.Id} ComputerEnergyMax={record.ComputerEnergyMax.Value}; the builder reads no energy gate.");
        var low = record.TrackingWeightLow.Value;
        var high = record.TrackingWeightHigh.Value;
        if (hulls.Hulls.Any(h => h.Weight.Value < low || h.Weight.Value > high))
            throw new OriginalDataException($"A player hull weighs outside VEH {record.Id} TrackingWeight {low}..{high}; the builder does not check weight.");

        var reference = OriginalProjectile.Of(referenceCannon);
        var provisional = OriginalWeaponBuilder.Weapon(referenceCannon);
        var cannon = OriginalWeaponBuilder.Weapon(satellites.Weapon) with
        {
            MuzzleVelocity = Scale(projectile.MuzzleVelocity.Value, reference.MuzzleVelocity.Value, provisional.MuzzleVelocity),
            LifetimeSeconds = Scale(projectile.AliveTime.Value, reference.AliveTime.Value, provisional.LifetimeSeconds),
            ProjectileRadius = Scale(projectile.ProximityRadius.Value, reference.ProximityRadius.Value, provisional.ProjectileRadius),
            InheritsShipVelocity = false,
            EnergyCost = 0,
        };
        cannon = cannon with
        {
            NewValues =
            [
                $"Muzzle velocity {cannon.MuzzleVelocity} px/s = raw {projectile.MuzzleVelocity.Value} x {provisional.MuzzleVelocity}/{reference.MuzzleVelocity.Value} (Plasma Cannon scale; unit open, G10 W2).",
                $"Lifetime {cannon.LifetimeSeconds} s = raw {projectile.AliveTime.Value} x {provisional.LifetimeSeconds}/{reference.AliveTime.Value} (Plasma Cannon scale; unit open, G10 W3).",
                $"Radius {cannon.ProjectileRadius} = raw proximity {projectile.ProximityRadius.Value} x {provisional.ProjectileRadius}/{reference.ProximityRadius.Value} (Plasma Cannon scale; meaning open).",
                "A shot does not inherit the satellite's velocity (it does not move) and costs it no energy (the emulator's turret never pays). It stops on walls and asteroids; its BounceCount is not applied.",
                "Hull damage ignores the damage type and armour (RT-F041).",
            ],
        };

        var repair = record.RepairRate.Value;
        var derived = new DerivedDefenseSatellite(
            OriginalDefenseSatellites.VehicleId.ToString(CultureInfo.InvariantCulture), record.Name, record.Hitpoints.Value, record.PhysicalRadius.Value,
            record.TrackingRadius.Value, record.FireRadius.Value,
            TenMs(projectile.FireDelay.Value), Math.Max(0, projectile.AmmoCapacity.Value), Math.Max(0, (int)MathF.Round(satellites.ReloadDelayNormal.Value * TicksPerTenMilliseconds)),
            repair > 0 ? TenMs(repair) : 0, repair > 0 ? repair / 100f : 0, record.HitpointsRequiredToOperate.Value, record.ObeyLos.Value != 0,
            cannon,
            new DefenseSatelliteSource(record.RowNumber, satellites.Weapon.RowNumber, new Dictionary<string, int>
            {
                ["vehHitpoints"] = record.Hitpoints.Value,
                ["vehPhysicalRadius"] = record.PhysicalRadius.Value,
                ["vehRotateSpeed"] = record.RotateSpeed.Value,
                ["vehAngleStart"] = record.AngleStart.Value,
                ["vehAngleLength"] = record.AngleLength.Value,
                ["vehTrackingTime"] = record.TrackingTime.Value,
                ["vehTrackingRadius"] = record.TrackingRadius.Value,
                ["vehTrackingWeightLow"] = low,
                ["vehTrackingWeightHigh"] = high,
                ["vehFireRadius"] = record.FireRadius.Value,
                ["vehRepairRate"] = repair,
                ["vehHitpointsRequiredToOperate"] = record.HitpointsRequiredToOperate.Value,
                ["vehObeyLos"] = record.ObeyLos.Value,
                ["vehComputerEnergyMax"] = record.ComputerEnergyMax.Value,
                ["vehDestroyable"] = record.Destroyable.Value,
                ["vehExplodeItemId"] = record.ExplodeItemId.Value,
                ["vehDropItemId"] = record.DropItemId.Value,
                ["vehDropItemQuantity"] = record.DropItemQuantity.Value,
                ["itmFireDelay"] = projectile.FireDelay.Value,
                ["itmAmmoUsedPerShot"] = projectile.AmmoUsedPerShot.Value,
                ["itmAmmoCapacity"] = projectile.AmmoCapacity.Value,
                ["itmReloadDelayNormal"] = satellites.ReloadDelayNormal.Value,
                ["itmMuzzleVelocity"] = projectile.MuzzleVelocity.Value,
                ["itmAliveTime"] = projectile.AliveTime.Value,
                ["itmProximityRadius"] = projectile.ProximityRadius.Value,
                ["itmBounceCount"] = projectile.BounceCount.Value,
            }));

        var spawns = satellites.Spawns.Select(row =>
        {
            var team = row.F(OriginalDefenseSatellites.AssignFrequencyField);
            if (team < 0 || team >= OriginalTeamInfo.TeamCount)
                throw new OriginalDataException($"LIO row {row.Row} '{row.Name}' f[{OriginalDefenseSatellites.AssignFrequencyField}]={team} is not a CFG team.");
            var maxDistance = row.F(FMaxPlayerDistance);
            return new DerivedDefenseSatelliteSpawn(row.Id.ToString(CultureInfo.InvariantCulture), row.Name, team, OriginalTeamInfo.Read(cfg, team).Name,
                OriginalMapBuilder.World(row.F(FX)), OriginalMapBuilder.World(row.F(FY)), row.F(FWidth), row.F(FHeight),
                Ticks(row.F(FAttemptDelay)), Ticks(row.F(FSucceedDelay)), row.F(FInitialCount), row.F(FProbability),
                row.F(FMinPlayers), row.F(FMaxPlayers), row.F(FMinPlayerDistance),
                maxDistance >= OriginalMineralFieldBuilder.NoMaxPlayerDistance ? 0 : maxDistance,
                row.F(FMaxTypeInArea), row.F(FMaxTypeInLevel),
                new DefenseSatelliteSpawnSource(row.Row, row.Name, row.Raw.ToDictionary(r => $"f{r.FieldIndex}", r => r.Value)));
        }).ToArray();

        return new DefenseSatelliteDocument(DocumentId, "ORIGINAL+RECONSTRUCTED+NEW",
            $"Derived by OriginalDefenseSatelliteBuilder from pinned {OriginalSafeZones.LioRogue.RelativePath} (type 5 v23 rows with f[20] = {OriginalDefenseSatellites.ActorValue}), {PinnedAssets.Veh.RelativePath} row {record.RowNumber} (id {OriginalDefenseSatellites.VehicleId}) and {PinnedAssets.Itm.RelativePath} row {satellites.Weapon.RowNumber} (id {satellites.Weapon.Id}) scaled against row {referenceCannon.RowNumber} (id {referenceCannon.Id}); team names from CFG [TeamInfoN]; field meanings from the InfantryOnline emulator at bb97579; regenerate, never hand-edit.",
            NewRules, Seed, derived, spawns);
    }

    private static float Scale(int raw, int referenceRaw, float referenceValue) =>
        referenceRaw > 0 ? MathF.Round(raw * referenceValue / referenceRaw, 3) : throw new OriginalDataException("The reference cannon has no raw value to scale by.");

    private static int Ticks(int raw) => Math.Max(1, (int)MathF.Round(raw * OriginalMineralFieldBuilder.TicksPerRawDelayUnit));

    private static int TenMs(int raw) => Math.Max(1, (int)MathF.Round(raw * TicksPerTenMilliseconds));
}
