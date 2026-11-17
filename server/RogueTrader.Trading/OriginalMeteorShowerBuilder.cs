using System.Globalization;
using System.Text.Json;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>Raw evidence for one derived meteor shower spawner: the LIO row and every integer field it was read from.</summary>
public sealed record MeteorShowerSpawnSource(int LioRow, string Name, IReadOnlyDictionary<string, int> Raw);

/// <summary>A derived meteor shower spawner: the simulation values plus where they came from.</summary>
public sealed record DerivedMeteorShowerSpawn(string Id, float X, float Y, float Width, float Height, int AttemptTicks, int SucceedTicks,
    int InitialCount, int Probability, int MinPlayers, int MaxPlayers, float MinPlayerDistance, float MaxPlayerDistance, int MaxInArea,
    int MaxInLevel, string Announcement, MeteorShowerSpawnSource Source);

/// <summary>Raw VEH 402 and ITM 1007 values behind the derived shower.</summary>
public sealed record MeteorShowerSource(int VehRow, int ItmRow, IReadOnlyDictionary<string, int> Raw);

/// <summary>The shower part of the document: <see cref="MeteorShowerDefinition"/> plus its raw evidence.</summary>
public sealed record DerivedMeteorShower(string VehicleId, string Name, float FireRadius, int FireIntervalTicks, long LifetimeTicks,
    WeaponDefinition Meteor, MeteorShowerSource Source);

/// <summary>The document behind <c>data/hazards/meteor_showers.json</c>; <see cref="MeteorShowerCatalog.FromJson"/> reads it.</summary>
public sealed record MeteorShowerDocument(string Id, string EvidenceClassification, string Source, IReadOnlyList<string> NewRules,
    ulong Seed, DerivedMeteorShower Shower, IReadOnlyList<DerivedMeteorShowerSpawn> Spawns)
{
    public string ToJson() => JsonSerializer.Serialize(this, StationDefinition.Json).ReplaceLineEndings("\n") + "\n";
}

/// <summary>
/// Derives the meteor showers of the Rogue map (RT-F061) from the pinned originals: the four LIO/Rogue.lio hide rows that place
/// VEH 402 MeteorShower (rows 18, 49, 52, 57), the shower's VEH record and the ITM 1007 Meteor it fires. Hide field meanings and the
/// spawn loop follow the InfantryOnline emulator as the mineral field does (<see cref="OriginalMineralFieldBuilder"/>); the turret
/// behaviour follows its <c>Computer.poll</c> (fire at the nearest pilot within FireRadius every fireDelay, lead-aimed). The NEW
/// readings are listed in <see cref="NewRules"/>.
/// </summary>
public static class OriginalMeteorShowerBuilder
{
    public const string DocumentId = "original_meteor_showers";
    /// <summary>NEW placement seed; the client derives a fresh one per sector instance.</summary>
    public const ulong Seed = 0x52545F4D45544F52UL; // "RT_METOR"
    /// <summary>The cannon whose provisional hit radius scales the meteor's (the player's Plasma Cannon, ITM 1004).</summary>
    public const int ReferenceCannonId = 1004;
    /// <summary>
    /// NEW: VEH f[212] RemoveGlobalTimer (5 for the shower, 0 on every other computer) read as seconds. The unit is not documented and
    /// the emulator never reads the field; Steve's call is pending (5 seconds recommended).
    /// </summary>
    public const float RemoveGlobalTimerSecondsPerUnit = 1;

    private const int FX = 4, FY = 5, FWidth = 6, FHeight = 7, FInitialCount = 10, FAttemptDelay = 11, FSucceedDelay = 12,
        FProbability = 13, FMinPlayers = 14, FMaxPlayers = 15, FMinPlayerDistance = 16, FMaxPlayerDistance = 17, FMaxTypeInArea = 18,
        FMaxTypeInLevel = 19;

    public static readonly IReadOnlyList<string> NewRules =
    [
        "Spawners: LIO type 5 v23 rows whose f[20] is -402 place VEH 402 MeteorShower, as the emulator reads a negative f[20] (ArenaLio.hideSpawn/hideVehicle at bb97579). f[10] InitialCount, f[11]/f[12] Attempt/SucceedDelay in tenths of a second, f[13] Probability, f[14]/f[15] Min/MaxPlayers, f[16]/f[17] Min/MaxPlayerDistance measured from the area centre (99999 = no check), f[18] MaxTypeInArea and f[19] MaxTypeInLevel counted in showers (every shower counts, whichever row placed it), f[23] the sector announcement. The emulator is a reconstruction, not the native server (RECONSTRUCTED).",
        "Positions f[4]/f[5] are the area centre and f[6]/f[7] its full size in map pixels, moved by the LVL offset (48 px) like every LIO position; a point on a blocked tile is retried up to 10 times. f[24] RelativeId 0 and f[25] AssignFrequency 9999 are kept raw: the shower belongs to no team and fires at any pilot.",
        "Firing (emulator Computer.poll): one meteor every ITM fireDelay (35, 10 ms units = 0.35 s) at the nearest pilot within VEH FireRadius, aimed where the pilot will be if they hold course (computeLeadFireAngle, without its 256-step angle and 0.7 isometric Y scale). The shower is not destroyable (VEH Destroyable 0) and does not block shots; it does not track TrackingRadius/TrackingTime, energy or LOS separately (its ObeyLos is 0).",
        "NEW lifetime: VEH f[212] RemoveGlobalTimer=5 is read as 5 seconds. The emulator never reads it (a shower would stand for ever); its unit is undocumented. Steve's call is pending.",
        "NEW: VEH f[213] RandomizeAim=5 is kept raw and not applied (unit unknown; the emulator ignores it).",
        "Meteor damage from ITM 1007 through OriginalWeaponBuilder: Kinetic hull damage f[152] 15000 = 15 and shield drain f[172] 750000 = 750 (thousandths). Armour reduces the hull damage by its Kinetic protection like any other Kinetic hit (RT-F041).",
        "Meteor speed and life use the emulator's projectile units like the player's cannons (G10 W2/W3, RECONSTRUCTED): muzzleVelocity 4250 thousandths of a pixel per 10 ms = 425 px/s, aliveTime 2000 x 10 ms = 20 s. NEW: its hit radius is its proximity radius 56 scaled by the factor that gives the player's Plasma Cannon (ITM 1004) its provisional radius (3 for 4).",
        "Meteors hit only the player's ship (and stop on walls and asteroids); they do not hit practice targets, so a meteor never takes a kill from the player.",
    ];

    public static MeteorShowerDocument Build(string assetsRoot)
    {
        var shower = OriginalMeteorShower.Load(assetsRoot);
        var rows = OriginalMeteorShowerSpawns.Load(assetsRoot);
        var items = OriginalItemCatalog.Load(assetsRoot);
        return Build(rows, shower, items.ById(ReferenceCannonId));
    }

    public static MeteorShowerDocument Build(IReadOnlyList<OriginalMeteorShowerSpawn> rows, OriginalMeteorShower shower, OriginalItem referenceCannon)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(shower);
        ArgumentNullException.ThrowIfNull(referenceCannon);
        var projectile = shower.Projectile ?? throw new OriginalDataException($"ITM {shower.Weapon.Id} is not a projectile.");
        var reference = OriginalProjectile.Of(referenceCannon);
        var provisional = OriginalWeaponBuilder.Weapon(referenceCannon);
        // Speed and life come out of OriginalWeaponBuilder in the emulator's units (G10 W2/W3, #133); only the radius is scaled.
        var meteor = OriginalWeaponBuilder.Weapon(shower.Weapon) with
        {
            ProjectileRadius = Scale(projectile.ProximityRadius.Value, reference.ProximityRadius.Value, provisional.ProjectileRadius),
            InheritsShipVelocity = false,
            EnergyCost = 0,
        };
        meteor = meteor with
        {
            NewValues =
            [
                $"Muzzle velocity {meteor.MuzzleVelocity} px/s = raw {projectile.MuzzleVelocity.Value} thousandths of a pixel per 10 ms, and lifetime {meteor.LifetimeSeconds} s = raw {projectile.AliveTime.Value} x 10 ms, in the emulator's units (RECONSTRUCTED, G10 W2/W3).",
                $"Radius {meteor.ProjectileRadius} = raw proximity {projectile.ProximityRadius.Value} x {provisional.ProjectileRadius}/{reference.ProximityRadius.Value} (Plasma Cannon scale; meaning open).",
                "A meteor does not inherit the shower's velocity (the shower does not move) and costs the shower no energy (the emulator's turret never pays).",
            ],
        };
        var record = shower.Record;
        var removeTimer = record.RemoveGlobalTimer.Value;
        var derivedShower = new DerivedMeteorShower(
            OriginalMeteorShower.VehicleId.ToString(CultureInfo.InvariantCulture), record.Vehicle.Name, record.FireRadius.Value,
            Math.Max(1, (int)MathF.Round(OriginalShipPhysics.FireIntervalSeconds(projectile) * WorldSimulation.TickRate)),
            (long)MathF.Round(Math.Max(0, removeTimer) * RemoveGlobalTimerSecondsPerUnit * WorldSimulation.TickRate),
            meteor,
            new MeteorShowerSource(record.RowNumber, shower.Weapon.RowNumber, new Dictionary<string, int>
            {
                ["vehTrackingRadius"] = record.TrackingRadius.Value,
                ["vehFireRadius"] = record.FireRadius.Value,
                ["vehRemoveGlobalTimer"] = removeTimer,
                ["vehRandomizeAim"] = record.RandomizeAim.Value,
                ["vehDestroyable"] = record.Int(VehComputerFields.Destroyable).Value,
                ["itmFireDelay"] = projectile.FireDelay.Value,
                ["itmMuzzleVelocity"] = projectile.MuzzleVelocity.Value,
                ["itmAliveTime"] = projectile.AliveTime.Value,
                ["itmProximityRadius"] = projectile.ProximityRadius.Value,
            }));

        var spawns = rows.Select(row =>
        {
            var maxDistance = row.F(FMaxPlayerDistance);
            return new DerivedMeteorShowerSpawn(row.Id.ToString(CultureInfo.InvariantCulture),
                OriginalMapBuilder.World(row.F(FX)), OriginalMapBuilder.World(row.F(FY)), row.F(FWidth), row.F(FHeight),
                Ticks(row.F(FAttemptDelay)), Ticks(row.F(FSucceedDelay)), row.F(FInitialCount), row.F(FProbability),
                row.F(FMinPlayers), row.F(FMaxPlayers), row.F(FMinPlayerDistance),
                maxDistance >= OriginalMineralFieldBuilder.NoMaxPlayerDistance ? 0 : maxDistance,
                row.F(FMaxTypeInArea), row.F(FMaxTypeInLevel), row.Announcement,
                new MeteorShowerSpawnSource(row.Row, row.Name, row.Raw.ToDictionary(r => $"f{r.FieldIndex}", r => r.Value)));
        }).ToArray();

        return new MeteorShowerDocument(DocumentId, "ORIGINAL+RECONSTRUCTED+NEW",
            $"Derived by OriginalMeteorShowerBuilder from pinned {OriginalSafeZones.LioRogue.RelativePath} (type 5 v23 rows with f[20] = {OriginalMeteorShowerSpawns.ActorValue}), {PinnedAssets.Veh.RelativePath} row {record.RowNumber} (id {OriginalMeteorShower.VehicleId}) and {PinnedAssets.Itm.RelativePath} row {shower.Weapon.RowNumber} (id {shower.Weapon.Id}) scaled against row {referenceCannon.RowNumber} (id {referenceCannon.Id}); field meanings from the InfantryOnline emulator at bb97579; regenerate, never hand-edit.",
            NewRules, Seed, derivedShower, spawns);
    }

    private static float Scale(int raw, int referenceRaw, float referenceValue) =>
        referenceRaw > 0 ? MathF.Round(raw * referenceValue / referenceRaw, 3) : throw new OriginalDataException("The reference cannon has no raw value to scale by.");

    private static int Ticks(int raw) => Math.Max(1, (int)MathF.Round(raw * OriginalMineralFieldBuilder.TicksPerRawDelayUnit));
}
