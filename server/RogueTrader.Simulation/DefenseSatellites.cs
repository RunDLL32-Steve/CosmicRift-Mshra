using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

/// <summary>
/// One defense satellite spawner: an original LIO type 5 "hide" row that places VEH 410 GunSat for a team (RT-F351..RT-F362). The
/// fields follow the emulator's hide loop for vehicles (<c>ArenaLio.hideSpawn</c>/<c>hideVehicle</c> at bb97579), the same reading
/// the meteor showers use (<see cref="MeteorShowerSpawnDefinition"/>). Units are converted by whoever builds the catalog.
/// </summary>
/// <param name="Id">Stable spawner id (the LIO row's id field).</param>
/// <param name="Name">The LIO row's name, for example <c>ColdHarbourDefSatA</c>.</param>
/// <param name="Team">The team the satellite belongs to (f[25] AssignFrequency); it never fires at that team's pilots.</param>
/// <param name="X">Centre of the area in world units (f[4]).</param>
/// <param name="Y">Centre of the area in world units (f[5]).</param>
/// <param name="Width">Full width of the area (f[6]).</param>
/// <param name="Height">Full height of the area (f[7]).</param>
/// <param name="AttemptTicks">Wait after any attempt (f[11] AttemptDelay).</param>
/// <param name="SucceedTicks">Wait after a success (f[12] SucceedDelay); the shorter of the two applies, as for minerals and showers.</param>
/// <param name="InitialCount">Events run on the spawner's first attempt (f[10]).</param>
/// <param name="Probability">f[13]: 0 means only the initial attempt runs.</param>
/// <param name="MinPlayers">An attempt fails with fewer players in the sector (f[14]).</param>
/// <param name="MaxPlayers">An attempt fails with more players in the sector (f[15]).</param>
/// <param name="MinPlayerDistance">An event fails while a player is within this distance of the area centre (f[16]; 0 = no check).</param>
/// <param name="MaxPlayerDistance">An event fails unless a player is within this distance of the area centre (f[17]; 0 = no check).</param>
/// <param name="MaxInArea">An event fails while this many satellites stand in the area (f[18]; -1 = no limit).</param>
/// <param name="MaxInLevel">An attempt fails while this many satellites stand anywhere in the sector (f[19]; -1 = no limit).</param>
public sealed record DefenseSatelliteSpawnDefinition(string Id, string Name, int Team, float X, float Y, float Width, float Height,
    int AttemptTicks, int SucceedTicks, int InitialCount = 1, int Probability = 1, int MinPlayers = 0, int MaxPlayers = int.MaxValue,
    float MinPlayerDistance = 0, float MaxPlayerDistance = 0, int MaxInArea = -1, int MaxInLevel = -1);

/// <summary>
/// What a standing defense satellite is and does (VEH 410 GunSat and the ITM 1038 PirateStationCannon it fires), read as the
/// emulator's <c>Computer.poll</c> does: while its hull is at least <see cref="HullToOperate"/> it repairs and fires at the nearest
/// enemy pilot within both <see cref="TrackingRadius"/> and <see cref="FireRadius"/>, one shot every <see cref="FireIntervalTicks"/>,
/// with a reload of <see cref="ReloadTicks"/> after every <see cref="ClipSize"/> shots, aimed where the pilot will be if they hold
/// course. The player's shots damage it; at zero hull it is destroyed and its spawner places a new one later.
/// </summary>
/// <param name="VehicleId">The VEH id of the satellite (410).</param>
/// <param name="Name">The VEH name ("GunSat").</param>
/// <param name="MaximumHull">VEH f[6] Hitpoints.</param>
/// <param name="Radius">VEH f[35] PhysicalRadius: the size the player's shots hit.</param>
/// <param name="TrackingRadius">VEH f[206] TrackingRadius.</param>
/// <param name="FireRadius">VEH f[209] FireRadius.</param>
/// <param name="FireIntervalTicks">Ticks between shots (ITM fireDelay, 10 ms units).</param>
/// <param name="ClipSize">Shots before a reload (ITM ammoCapacity); 0 = no reload.</param>
/// <param name="ReloadTicks">Reload after a clip (ITM reloadDelayNormal, 10 ms units).</param>
/// <param name="RepairIntervalTicks">Ticks between repairs (VEH f[210] RepairRate x 10 ms); 0 = no repair.</param>
/// <param name="RepairAmount">Hull repaired each interval (RepairRate / 100).</param>
/// <param name="HullToOperate">Below this hull it neither fires nor repairs (VEH f[211] HitpointsRequiredToOperate).</param>
/// <param name="ObeyLos">VEH f[214]: walls between the satellite and a pilot hide the pilot from it.</param>
/// <param name="Cannon">The projectile it fires: id, speed, radius, life, hull damage and shield drain.</param>
public sealed record DefenseSatelliteDefinition(string VehicleId, string Name, float MaximumHull, float Radius, float TrackingRadius,
    float FireRadius, int FireIntervalTicks, int ClipSize, int ReloadTicks, int RepairIntervalTicks, float RepairAmount, float HullToOperate,
    bool ObeyLos, WeaponDefinition Cannon);

/// <summary>The defense satellite spawners of one sector, the satellite they place, and the placement seed.</summary>
public sealed class DefenseSatelliteCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };

    public DefenseSatelliteDefinition Satellite { get; }
    public IReadOnlyList<DefenseSatelliteSpawnDefinition> Spawns { get; }
    public ulong Seed { get; }

    /// <summary>This catalog reseeded for one world (see <see cref="MineralFieldCatalog.ForWorld"/>).</summary>
    public DefenseSatelliteCatalog ForWorld(ulong worldSalt) => new(Satellite, Spawns, Seed ^ worldSalt);

    public DefenseSatelliteCatalog(DefenseSatelliteDefinition satellite, IEnumerable<DefenseSatelliteSpawnDefinition> spawns, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(satellite);
        ArgumentNullException.ThrowIfNull(spawns);
        var c = satellite.Cannon ?? throw new ArgumentException("A defense satellite needs a cannon.", nameof(satellite));
        if (string.IsNullOrWhiteSpace(satellite.VehicleId) || !Positive(satellite.MaximumHull) || !Positive(satellite.Radius)
            || !Positive(satellite.TrackingRadius) || !Positive(satellite.FireRadius) || satellite.FireIntervalTicks <= 0
            || satellite.ClipSize < 0 || satellite.ReloadTicks < 0 || satellite.RepairIntervalTicks < 0
            || !float.IsFinite(satellite.RepairAmount) || satellite.RepairAmount < 0
            || !float.IsFinite(satellite.HullToOperate) || satellite.HullToOperate < 0
            || string.IsNullOrWhiteSpace(c.Id) || !Positive(c.MuzzleVelocity) || !Positive(c.ProjectileRadius) || !Positive(c.LifetimeSeconds)
            || !float.IsFinite(c.Damage) || c.Damage < 0 || c.ShieldDrain is { } drain && (!float.IsFinite(drain) || drain < 0))
            throw new ArgumentException("A defense satellite needs an id, a positive hull, size, ranges and fire interval, no negative reload or repair, and a cannon with positive speed, size and life.", nameof(satellite));
        Satellite = satellite;
        Spawns = Array.AsReadOnly(spawns.ToArray());
        Seed = seed;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in Spawns)
        {
            if (s is null || string.IsNullOrWhiteSpace(s.Id) || !ids.Add(s.Id))
                throw new ArgumentException("Defense satellite spawners need unique ids.", nameof(spawns));
            if (s.AttemptTicks <= 0 || s.SucceedTicks <= 0 || s.InitialCount < 0 || s.Probability < 0 || s.MinPlayers < 0 || s.MaxPlayers < 0
                || s.MaxInArea < -1 || s.MaxInLevel < -1 || s.Team < 0)
                throw new ArgumentException($"Defense satellite spawner '{s.Id}' needs positive delays, a team and no negative limits.", nameof(spawns));
            if (!float.IsFinite(s.X) || !float.IsFinite(s.Y) || !float.IsFinite(s.Width) || !float.IsFinite(s.Height) || s.Width < 0 || s.Height < 0
                || !float.IsFinite(s.MinPlayerDistance) || s.MinPlayerDistance < 0 || !float.IsFinite(s.MaxPlayerDistance) || s.MaxPlayerDistance < 0)
                throw new ArgumentException($"Defense satellite spawner '{s.Id}' needs a finite area and distances.", nameof(spawns));
        }
    }

    private static bool Positive(float value) => float.IsFinite(value) && value > 0;

    /// <summary>Document shape: <c>{ "seed": n, "satellite": DefenseSatelliteDefinition, "spawns": [ ... ] }</c>; other members are ignored.</summary>
    public static DefenseSatelliteCatalog FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<Document>(json, JsonOptions) ?? throw new JsonException("Empty defense satellite definition.");
        return new DefenseSatelliteCatalog(document.Satellite ?? throw new JsonException("Missing satellite."),
            document.Spawns ?? throw new JsonException("Missing spawns."), document.Seed);
    }

    private sealed class Document
    {
        public ulong Seed { get; set; }
        public DefenseSatelliteDefinition? Satellite { get; set; }
        public List<DefenseSatelliteSpawnDefinition>? Spawns { get; set; }
    }
}

/// <summary>A defense satellite standing in the sector.</summary>
public sealed class DefenseSatelliteState
{
    public string Id { get; }
    public string SpawnId { get; }
    public int Team { get; }
    public Vector2 Position { get; }
    public float Hull { get; internal set; }
    /// <summary>Direction the satellite last aimed (radians, as ship headings); it starts facing up.</summary>
    public float Heading { get; internal set; } = -MathF.PI / 2;
    internal long NextFireAt { get; set; }
    internal int ShotsLeft { get; set; }
    /// <summary>Clock ticks counted toward the next repair.</summary>
    internal long RepairTicks { get; set; }

    internal DefenseSatelliteState(string id, string spawnId, int team, Vector2 position, float hull)
    {
        Id = id;
        SpawnId = spawnId;
        Team = team;
        Position = position;
        Hull = hull;
    }
}

/// <summary>A defense satellite destroyed by a shot: which one, whose it was, where, on which tick, and who fired the shot.</summary>
public sealed record DefenseSatelliteDestroyed(int Sequence, string SatelliteId, string SpawnId, int Team, float X, float Y, long Tick, string ByShipId);

public sealed partial class WorldSimulation
{
    /// <summary>Id prefix of standing satellites (<c>defense-sat:&lt;spawner id&gt;:&lt;n&gt;</c>), also the owner id of their shots.</summary>
    public const string DefenseSatellitePrefix = "defense-sat:";

    private readonly SatelliteSpawnState[] _satelliteSpawns = [];
    private readonly List<DefenseSatelliteState> _satellites = [];
    private readonly List<DefenseSatelliteDestroyed> _satelliteKills = [];
    private readonly DefenseSatelliteDefinition? _satellite;
    private ulong _satelliteRandom;
    // Satellite timers run on real time like the mineral and meteor timers (owner rule, November 2, 2026).
    private long _satelliteIdleTicks;
    private long SatelliteClock => TickNumber + _satelliteIdleTicks;

    /// <summary>
    /// The player's team (the <see cref="TeamDefinition.Index"/> the pilot joined); satellites of this team never fire at the player
    /// and the player's shots pass through them. Null means the player is on no team, so every satellite is hostile.
    /// </summary>
    public int? PlayerTeam { get; set; }

    /// <summary>Defense satellites standing in the sector, in the order they appeared.</summary>
    public IReadOnlyList<DefenseSatelliteState> DefenseSatellites => _satellites;
    /// <summary>Every satellite destroyed in this world, in order (the client shows each once).</summary>
    public IReadOnlyList<DefenseSatelliteDestroyed> DefenseSatelliteKills => _satelliteKills;
    /// <summary>The satellite this world places, or null when it has no defense satellite spawners.</summary>
    public DefenseSatelliteDefinition? DefenseSatellite => _satellite;
    /// <summary>Defense satellite spawners of this world.</summary>
    public IReadOnlyList<DefenseSatelliteSpawnDefinition> DefenseSatelliteSpawns => _satelliteSpawns.Select(s => s.Definition).ToArray();
    /// <summary>Satellite shots that hit the player in this world.</summary>
    public int SatelliteHits { get; private set; }

    private sealed class SatelliteSpawnState(DefenseSatelliteSpawnDefinition definition)
    {
        public DefenseSatelliteSpawnDefinition Definition { get; } = definition;
        /// <summary>Satellite clock tick of the next attempt; 0 until the spawner's initial attempt has run.</summary>
        public long NextAttemptTick { get; set; }
        public long Placed { get; set; }
    }

    private bool IsFriendly(DefenseSatelliteState satellite) => PlayerTeam is { } team && team == satellite.Team;

    /// <summary>Runs due spawners in catalog order, then lets every satellite repair and fire.</summary>
    private void StepDefenseSatellites()
    {
        if (_satellite is null) return;
        var now = SatelliteClock;
        foreach (var spawn in _satelliteSpawns)
            if (spawn.NextAttemptTick == 0 || now >= spawn.NextAttemptTick)
                AttemptDefenseSatellite(spawn, now);
        foreach (var satellite in _satellites)
        {
            if (satellite.Hull < _satellite.HullToOperate) continue; // too damaged to operate: no repair, no fire
            Repair(satellite, 1);
            if (now >= satellite.NextFireAt) FireSatellite(satellite, now);
        }
    }

    /// <summary>
    /// Lets the spawners and repairs run through <paramref name="ticks"/> in which the world did not tick (docked, paused or closed):
    /// each attempt happens at its own tick. Nothing fires while the world is not ticking.
    /// </summary>
    private void ElapseDefenseSatellites(long ticks)
    {
        if (_satellite is not null && _satelliteSpawns.Length > 0)
        {
            var end = checked(SatelliteClock + ticks);
            var first = SatelliteClock + 1;
            foreach (var spawn in _satelliteSpawns)
            {
                while ((spawn.NextAttemptTick == 0 ? first : spawn.NextAttemptTick) <= end)
                {
                    var at = spawn.NextAttemptTick == 0 ? first : spawn.NextAttemptTick;
                    AttemptDefenseSatellite(spawn, at);
                    if (!CanPlaceMore(spawn.Definition))
                    {
                        // Nothing is removed while the world is not ticking, so every later attempt in the gap would fail the same way.
                        var delay = spawn.Definition.AttemptTicks;
                        if (spawn.NextAttemptTick <= end) spawn.NextAttemptTick += ((end - spawn.NextAttemptTick) / delay + 1) * delay;
                        break;
                    }
                }
            }
            foreach (var satellite in _satellites)
                if (satellite.Hull >= _satellite.HullToOperate) Repair(satellite, ticks);
        }
        _satelliteIdleTicks = checked(_satelliteIdleTicks + ticks);
    }

    private void Repair(DefenseSatelliteState satellite, long ticks)
    {
        var d = _satellite!;
        if (d.RepairIntervalTicks <= 0 || d.RepairAmount <= 0) return;
        if (satellite.Hull >= d.MaximumHull)
        {
            satellite.RepairTicks = 0;
            return;
        }
        satellite.RepairTicks += ticks;
        var repairs = satellite.RepairTicks / d.RepairIntervalTicks;
        if (repairs == 0) return;
        satellite.RepairTicks %= d.RepairIntervalTicks;
        satellite.Hull = MathF.Min(d.MaximumHull, satellite.Hull + (float)(repairs * (double)d.RepairAmount));
    }

    private bool CanPlaceMore(DefenseSatelliteSpawnDefinition d)
    {
        if (d.MaxInLevel != -1 && OperatingCount >= d.MaxInLevel) return false;
        return d.MaxInArea == -1 || CountInArea(d) < d.MaxInArea;
    }

    /// <summary>
    /// True while the satellite has at least <see cref="DefenseSatelliteDefinition.HullToOperate"/> hull. NEW (Steve, November 10, 2026,
    /// pending his ruling): a disabled satellite does not count toward a spawner's limits, and the spawner's next attempt replaces it, so
    /// stopping just short of a kill cannot leave a station undefended for longer than a kill would. The emulator counts it and, since it
    /// never repairs below that hull, it would block its own respawn for as long as the server runs.
    /// </summary>
    private bool IsOperating(DefenseSatelliteState satellite) => satellite.Hull >= _satellite!.HullToOperate;

    private int OperatingCount => _satellites.Count(IsOperating);

    private static bool InArea(DefenseSatelliteSpawnDefinition d, DefenseSatelliteState s) =>
        MathF.Abs(s.Position.X - d.X) <= d.Width / 2 && MathF.Abs(s.Position.Y - d.Y) <= d.Height / 2;

    private int CountInArea(DefenseSatelliteSpawnDefinition d) => _satellites.Count(s => InArea(d, s) && IsOperating(s));

    /// <summary>One attempt (the emulator's <c>hideSpawn</c> for a vehicle): timer first, then the player and sector-wide limits.</summary>
    private void AttemptDefenseSatellite(SatelliteSpawnState spawn, long now)
    {
        var d = spawn.Definition;
        var initial = spawn.NextAttemptTick == 0;
        spawn.NextAttemptTick = checked(now + d.AttemptTicks);
        if (!initial && d.Probability == 0) return;
        if (PlayersInSector > d.MaxPlayers || PlayersInSector < d.MinPlayers) return;
        // Every satellite in the sector counts, whichever spawner placed it (the emulator counts vehicles of the same type).
        if (d.MaxInLevel != -1 && OperatingCount >= d.MaxInLevel) return;
        var events = initial && d.InitialCount > 0 ? d.InitialCount : 1;
        var succeeded = false;
        for (var e = 0; e < events; e++) succeeded |= PlaceDefenseSatellite(spawn, now);
        if (succeeded) spawn.NextAttemptTick = checked(now + Math.Min(d.AttemptTicks, d.SucceedTicks));
    }

    /// <summary>One event (the emulator's <c>hideVehicle</c>): distance and area checks, then up to 10 tries for an open point.</summary>
    private bool PlaceDefenseSatellite(SatelliteSpawnState spawn, long now)
    {
        var d = spawn.Definition;
        var centre = new Vector2(d.X, d.Y);
        if (d.MinPlayerDistance > 0 && !Player.IsDestroyed && Vector2.Distance(Player.Position, centre) <= d.MinPlayerDistance) return false;
        if (d.MaxPlayerDistance > 0 && (Player.IsDestroyed || Vector2.Distance(Player.Position, centre) > d.MaxPlayerDistance)) return false;
        if (d.MaxInArea != -1 && CountInArea(d) >= d.MaxInArea) return false;
        for (var tries = 0; tries < DropTries; tries++)
        {
            var point = new Vector2(d.X + (float)((NextSatelliteUnit() - 0.5) * d.Width), d.Y + (float)((NextSatelliteUnit() - 0.5) * d.Height));
            if (Map is not null && Map.IsBlocked(point)) continue;
            // The new satellite replaces any disabled one in its area (see IsOperating).
            _satellites.RemoveAll(s => InArea(d, s) && !IsOperating(s));
            _satellites.Add(new DefenseSatelliteState($"{DefenseSatellitePrefix}{d.Id}:{++spawn.Placed}", d.Id, d.Team, point, _satellite!.MaximumHull)
            {
                NextFireAt = now,
                ShotsLeft = _satellite.ClipSize,
            });
            return true;
        }
        return false;
    }

    /// <summary>
    /// The emulator's target check for the one pilot this world holds: alive, not on the satellite's team, within the tracking and
    /// firing radius, and (when the satellite obeys line of sight) not behind a wall.
    /// </summary>
    private bool CanTarget(DefenseSatelliteState satellite)
    {
        var d = _satellite!;
        if (Player.IsDestroyed || IsFriendly(satellite)) return false;
        var range = MathF.Min(d.TrackingRadius, d.FireRadius);
        if (Vector2.DistanceSquared(Player.Position, satellite.Position) > range * range) return false;
        return !d.ObeyLos || Map is null || !MapCollision.Sweep(Map, satellite.Position, Player.Position, out _);
    }

    /// <summary>One shot at the player when they are a valid target, aimed to meet them if they hold their course.</summary>
    private void FireSatellite(DefenseSatelliteState satellite, long now)
    {
        if (!CanTarget(satellite)) return;
        var d = _satellite!;
        var cannon = d.Cannon;
        var direction = LeadDirection(Player.Position - satellite.Position, Player.Velocity, cannon.MuzzleVelocity);
        satellite.Heading = MathF.Atan2(direction.Y, direction.X);
        _projectiles.Add(new ProjectileState(++_nextProjectileId, satellite.Id, cannon, satellite.Position, direction * cannon.MuzzleVelocity));
        satellite.NextFireAt = now + d.FireIntervalTicks;
        if (d.ClipSize > 0 && --satellite.ShotsLeft <= 0)
        {
            satellite.ShotsLeft = d.ClipSize;
            satellite.NextFireAt = Math.Max(satellite.NextFireAt, now + d.ReloadTicks);
        }
    }

    /// <summary>The first enemy satellite the player's shot meets on this tick's path before <paramref name="hitFraction"/>.</summary>
    private DefenseSatelliteState? SweepSatellites(ProjectileState projectile, Vector2 end, ref float hitFraction)
    {
        if (_satellite is null || projectile.OwnerShipId != Player.Id) return null;
        DefenseSatelliteState? hit = null;
        foreach (var satellite in _satellites)
        {
            if (IsFriendly(satellite)) continue;
            if (CircleCollision.Sweep(projectile.Position, end, satellite.Position, projectile.Radius + _satellite.Radius, out var fraction)
                && fraction < hitFraction)
            {
                hitFraction = fraction;
                hit = satellite;
            }
        }
        return hit;
    }

    /// <summary>A shot's hull damage; the satellite has no shield or energy to drain. At zero hull it is destroyed.</summary>
    private void DamageSatellite(DefenseSatelliteState satellite, ProjectileState projectile)
    {
        satellite.Hull = MathF.Max(0, satellite.Hull - projectile.Damage);
        if (satellite.Hull > 0) return;
        _satellites.Remove(satellite);
        _satelliteKills.Add(new DefenseSatelliteDestroyed(_satelliteKills.Count, satellite.Id, satellite.SpawnId, satellite.Team,
            satellite.Position.X, satellite.Position.Y, TickNumber, projectile.OwnerShipId));
    }

    /// <summary>SplitMix64 in [0, 1), its own stream so satellite placement never shifts the mineral field or the meteors.</summary>
    private double NextSatelliteUnit()
    {
        var z = _satelliteRandom += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (z >> 11) * (1.0 / (1UL << 53));
    }

    private void CaptureSatellites(out IReadOnlyList<SatelliteSpawnSnapshot> spawns, out IReadOnlyList<DefenseSatelliteSnapshot> satellites)
    {
        spawns = _satelliteSpawns.Select(s => new SatelliteSpawnSnapshot(s.Definition.Id,
            s.NextAttemptTick == 0 ? 0 : Math.Max(1, s.NextAttemptTick - SatelliteClock), s.Placed)).ToArray();
        satellites = _satellites.Select(s => new DefenseSatelliteSnapshot(s.Id, s.SpawnId, s.Position.X, s.Position.Y, s.Hull)).ToArray();
    }

    /// <summary>
    /// Puts saved spawner timers and standing satellites back. A satellite keeps its saved hull (never above the maximum), so a
    /// relaunch never repairs a damaged one or brings back a destroyed one early; its team comes from its spawner.
    /// </summary>
    private void RestoreSatellites(SectorSnapshot snapshot)
    {
        if (_satellite is null) return;
        var saved = (snapshot.SatelliteSpawns ?? []).Where(s => s is not null).GroupBy(s => s.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var spawn in _satelliteSpawns)
        {
            if (!saved.TryGetValue(spawn.Definition.Id, out var s)) continue;
            spawn.Placed = Math.Max(0, s.Placed);
            spawn.NextAttemptTick = s.TicksUntilAttempt <= 0 ? 0 : SatelliteClock + s.TicksUntilAttempt;
        }
        var spawns = _satelliteSpawns.ToDictionary(s => s.Definition.Id, s => s.Definition, StringComparer.Ordinal);
        _satellites.Clear();
        foreach (var s in snapshot.DefenseSatellites ?? [])
        {
            if (s is null || string.IsNullOrEmpty(s.Id) || !spawns.TryGetValue(s.SpawnId ?? "", out var spawn) || !float.IsFinite(s.X)
                || !float.IsFinite(s.Y) || !float.IsFinite(s.Hull) || s.Hull <= 0 || _satellites.Any(x => x.Id == s.Id))
                continue;
            _satellites.Add(new DefenseSatelliteState(s.Id, spawn.Id, spawn.Team, new Vector2(s.X, s.Y), MathF.Min(s.Hull, _satellite.MaximumHull))
            {
                NextFireAt = SatelliteClock,
                ShotsLeft = _satellite.ClipSize,
            });
        }
        if (saved.Count > 0) _satelliteRandom = snapshot.SatelliteRandomState;
    }
}

/// <summary>One defense satellite spawner's timer in a <see cref="SectorSnapshot"/> (as <see cref="MeteorSpawnSnapshot"/>).</summary>
public sealed record SatelliteSpawnSnapshot(string Id, long TicksUntilAttempt, long Placed);

/// <summary>A standing defense satellite in a <see cref="SectorSnapshot"/> with the hull it had.</summary>
public sealed record DefenseSatelliteSnapshot(string Id, string? SpawnId, float X, float Y, float Hull);
