using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

/// <summary>
/// One meteor shower spawner (RT-F061): an original LIO type 5 "hide" row whose f[20] places VEH 402 MeteorShower. The fields follow
/// the emulator's hide loop for vehicles (<c>ArenaLio.hideSpawn</c>/<c>hideVehicle</c> at bb97579), the same reading the mineral
/// field uses (<see cref="MineralSpawnDefinition"/>). Units are converted by whoever builds the catalog.
/// </summary>
/// <param name="Id">Stable spawner id (the LIO row's id field).</param>
/// <param name="X">Centre of the area in world units (f[4]).</param>
/// <param name="Y">Centre of the area in world units (f[5]).</param>
/// <param name="Width">Full width of the area (f[6]).</param>
/// <param name="Height">Full height of the area (f[7]).</param>
/// <param name="AttemptTicks">Wait after any attempt (f[11] AttemptDelay).</param>
/// <param name="SucceedTicks">Wait after a success (f[12] SucceedDelay); the shorter of the two applies, as for minerals.</param>
/// <param name="InitialCount">Events run on the spawner's first attempt (f[10]).</param>
/// <param name="Probability">f[13]: 0 means only the initial attempt runs.</param>
/// <param name="MinPlayers">An attempt fails with fewer players in the sector (f[14]).</param>
/// <param name="MaxPlayers">An attempt fails with more players in the sector (f[15]).</param>
/// <param name="MinPlayerDistance">An event fails while a player is within this distance of the area centre (f[16]; 0 = no check).</param>
/// <param name="MaxPlayerDistance">An event fails unless a player is within this distance of the area centre (f[17]; 0 = no check).</param>
/// <param name="MaxInArea">An event fails while this many showers stand in the area (f[18]; -1 = no limit).</param>
/// <param name="MaxInLevel">An attempt fails while this many showers stand anywhere in the sector (f[19]; -1 = no limit).</param>
/// <param name="Announcement">Sector message sent when a shower appears (f[23]; empty = none).</param>
public sealed record MeteorShowerSpawnDefinition(string Id, float X, float Y, float Width, float Height, int AttemptTicks, int SucceedTicks,
    int InitialCount = 1, int Probability = 1, int MinPlayers = 0, int MaxPlayers = int.MaxValue, float MinPlayerDistance = 0,
    float MaxPlayerDistance = 0, int MaxInArea = -1, int MaxInLevel = -1, string Announcement = "");

/// <summary>
/// What a standing meteor shower does (VEH 402 MeteorShower and the ITM 1007 Meteor it fires). It fires one meteor at the nearest
/// pilot within <see cref="FireRadius"/> every <see cref="FireIntervalTicks"/>, aimed where the pilot will be if they hold course
/// (the emulator's <c>Computer.poll</c> and <c>computeLeadFireAngle</c>), and is removed <see cref="LifetimeTicks"/> after it appears.
/// </summary>
/// <param name="VehicleId">The VEH id of the shower (402).</param>
/// <param name="FireRadius">Pilots farther than this are not fired at (VEH FireRadius).</param>
/// <param name="FireIntervalTicks">Ticks between meteors (ITM fireDelay, 10 ms units).</param>
/// <param name="LifetimeTicks">How long a shower stands before it is removed; 0 = it stands until the sector is reset.</param>
/// <param name="Meteor">The meteor projectile: id, speed, radius, life, hull damage and shield drain.</param>
public sealed record MeteorShowerDefinition(string VehicleId, float FireRadius, int FireIntervalTicks, long LifetimeTicks, WeaponDefinition Meteor);

/// <summary>The meteor shower spawners of one sector, the shower they place, and the placement seed.</summary>
public sealed class MeteorShowerCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };

    public MeteorShowerDefinition Shower { get; }
    public IReadOnlyList<MeteorShowerSpawnDefinition> Spawns { get; }
    public ulong Seed { get; }

    /// <summary>This catalog reseeded for one world (see <see cref="MineralFieldCatalog.ForWorld"/>).</summary>
    public MeteorShowerCatalog ForWorld(ulong worldSalt) => new(Shower, Spawns, Seed ^ worldSalt);

    public MeteorShowerCatalog(MeteorShowerDefinition shower, IEnumerable<MeteorShowerSpawnDefinition> spawns, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(shower);
        ArgumentNullException.ThrowIfNull(spawns);
        var m = shower.Meteor ?? throw new ArgumentException("A meteor shower needs a meteor.", nameof(shower));
        if (string.IsNullOrWhiteSpace(shower.VehicleId) || !Positive(shower.FireRadius) || shower.FireIntervalTicks <= 0 || shower.LifetimeTicks < 0
            || string.IsNullOrWhiteSpace(m.Id) || !Positive(m.MuzzleVelocity) || !Positive(m.ProjectileRadius) || !Positive(m.LifetimeSeconds)
            || !float.IsFinite(m.Damage) || m.Damage < 0 || m.ShieldDrain is { } drain && (!float.IsFinite(drain) || drain < 0))
            throw new ArgumentException("A meteor shower needs an id, a positive fire radius, interval, meteor speed, size and life, and finite damage.", nameof(shower));
        Shower = shower;
        Spawns = Array.AsReadOnly(spawns.ToArray());
        Seed = seed;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in Spawns)
        {
            if (s is null || string.IsNullOrWhiteSpace(s.Id) || !ids.Add(s.Id))
                throw new ArgumentException("Meteor shower spawners need unique ids.", nameof(spawns));
            if (s.AttemptTicks <= 0 || s.SucceedTicks <= 0 || s.InitialCount < 0 || s.Probability < 0 || s.MinPlayers < 0 || s.MaxPlayers < 0
                || s.MaxInArea < -1 || s.MaxInLevel < -1)
                throw new ArgumentException($"Meteor shower spawner '{s.Id}' needs positive delays and no negative limits.", nameof(spawns));
            if (!float.IsFinite(s.X) || !float.IsFinite(s.Y) || !float.IsFinite(s.Width) || !float.IsFinite(s.Height) || s.Width < 0 || s.Height < 0
                || !float.IsFinite(s.MinPlayerDistance) || s.MinPlayerDistance < 0 || !float.IsFinite(s.MaxPlayerDistance) || s.MaxPlayerDistance < 0)
                throw new ArgumentException($"Meteor shower spawner '{s.Id}' needs a finite area and distances.", nameof(spawns));
        }
    }

    private static bool Positive(float value) => float.IsFinite(value) && value > 0;

    /// <summary>Document shape: <c>{ "seed": n, "shower": MeteorShowerDefinition, "spawns": [ ... ] }</c>; other members are ignored.</summary>
    public static MeteorShowerCatalog FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<Document>(json, JsonOptions) ?? throw new JsonException("Empty meteor shower definition.");
        return new MeteorShowerCatalog(document.Shower ?? throw new JsonException("Missing shower."),
            document.Spawns ?? throw new JsonException("Missing spawns."), document.Seed);
    }

    private sealed class Document
    {
        public ulong Seed { get; set; }
        public MeteorShowerDefinition? Shower { get; set; }
        public List<MeteorShowerSpawnDefinition>? Spawns { get; set; }
    }
}

/// <summary>A meteor shower standing in the sector.</summary>
public sealed class MeteorShowerState
{
    public string Id { get; }
    public string SpawnId { get; }
    public Vector2 Position { get; }
    /// <summary>Meteor clock tick at which the shower is removed; null when it stands until the sector is reset.</summary>
    public long? RemoveAt { get; internal set; }
    internal long NextFireAt { get; set; }

    internal MeteorShowerState(string id, string spawnId, Vector2 position)
    {
        Id = id;
        SpawnId = spawnId;
        Position = position;
    }
}

/// <summary>One shower appearing: which spawner placed it, where, on which tick, and the sector message it sent.</summary>
public sealed record MeteorShowerAppeared(int Sequence, string ShowerId, string SpawnId, float X, float Y, long Tick, string Announcement);

public sealed partial class WorldSimulation
{
    /// <summary>Id prefix of standing showers (<c>meteor-shower:&lt;spawner id&gt;:&lt;n&gt;</c>), also the owner id of their meteors.</summary>
    public const string MeteorShowerPrefix = "meteor-shower:";

    private readonly MeteorSpawnState[] _meteorSpawns = [];
    private readonly List<MeteorShowerState> _showers = [];
    private readonly List<MeteorShowerAppeared> _showerEvents = [];
    private readonly MeteorShowerDefinition? _shower;
    private ulong _meteorRandom;
    // Meteor timers run on real time like the mineral timers (owner rule, November 2, 2026): ticks that pass without ticking count too.
    private long _meteorIdleTicks;
    private long MeteorClock => TickNumber + _meteorIdleTicks;

    /// <summary>Meteor showers standing in the sector, in the order they appeared.</summary>
    public IReadOnlyList<MeteorShowerState> MeteorShowers => _showers;
    /// <summary>Every shower that appeared in this world, in order (the client shows each announcement once).</summary>
    public IReadOnlyList<MeteorShowerAppeared> MeteorShowerEvents => _showerEvents;
    /// <summary>The shower this world places, or null when it has no meteor shower spawners.</summary>
    public MeteorShowerDefinition? MeteorShower => _shower;
    /// <summary>Meteor shower spawners of this world.</summary>
    public IReadOnlyList<MeteorShowerSpawnDefinition> MeteorShowerSpawns => _meteorSpawns.Select(s => s.Definition).ToArray();
    /// <summary>Meteors that hit the player in this world.</summary>
    public int MeteorHits { get; private set; }

    private sealed class MeteorSpawnState(MeteorShowerSpawnDefinition definition)
    {
        public MeteorShowerSpawnDefinition Definition { get; } = definition;
        /// <summary>Meteor clock tick of the next attempt; 0 until the spawner's initial attempt has run.</summary>
        public long NextAttemptTick { get; set; }
        public long Placed { get; set; }
    }

    /// <summary>Removes showers whose time is up, runs due spawners in catalog order, then lets every shower fire.</summary>
    private void StepMeteorShowers()
    {
        if (_shower is null) return;
        RunMeteorEvents(MeteorClock, MeteorClock);
        if (Player.IsDestroyed || PlayersInSector <= 0) return;
        foreach (var shower in _showers)
            if (MeteorClock >= shower.NextFireAt) FireMeteor(shower);
    }

    private void RunMeteorEvents(long now, long first)
    {
        _showers.RemoveAll(s => s.RemoveAt is { } at && at <= now);
        foreach (var spawn in _meteorSpawns)
            if (spawn.NextAttemptTick == 0 ? now >= first : now >= spawn.NextAttemptTick)
                AttemptMeteorShower(spawn, now);
    }

    /// <summary>
    /// Lets the spawners and shower removal run through <paramref name="ticks"/> in which the world did not tick (docked, paused or
    /// closed): each attempt and removal happens at its own tick, in the order ticking would run them. Nothing fires while the world
    /// is not ticking; a shower that is still standing afterwards fires on the next tick.
    /// </summary>
    private void ElapseMeteorShowers(long ticks)
    {
        // Meteors in flight age through time spent away (docked, paused or closed) like every other timer here, so one saved
        // mid-flight cannot hit long after its 20 s life; they do not move while the world is not ticking.
        _projectiles.RemoveAll(p => p.OwnerShipId.StartsWith(MeteorShowerPrefix, StringComparison.Ordinal)
            && ticks >= p.LifetimeTicks - p.AgeTicks);
        foreach (var p in _projectiles)
            if (p.OwnerShipId.StartsWith(MeteorShowerPrefix, StringComparison.Ordinal)) p.AgeTicks += (int)ticks;
        if (_shower is null || _meteorSpawns.Length == 0)
        {
            _meteorIdleTicks = checked(_meteorIdleTicks + ticks);
            return;
        }
        var start = MeteorClock;
        var end = checked(start + ticks);
        var first = start + 1;
        // Only the last stretch of a long gap can leave a shower standing or a spawner mid-delay; skip to it.
        var longGap = 2L * _meteorSpawns.Max(s => Math.Max(s.Definition.AttemptTicks, s.Definition.SucceedTicks)) + _shower.LifetimeTicks;
        if (_shower.LifetimeTicks > 0 && ticks > longGap)
        {
            var resume = end - longGap;
            _showers.RemoveAll(s => s.RemoveAt is { } at && at <= resume);
            foreach (var spawn in _meteorSpawns)
                if (spawn.NextAttemptTick != 0) spawn.NextAttemptTick = Math.Max(spawn.NextAttemptTick, resume);
            first = Math.Max(first, resume);
        }
        while (true)
        {
            var at = long.MaxValue;
            foreach (var spawn in _meteorSpawns)
                at = Math.Min(at, spawn.NextAttemptTick == 0 ? first : spawn.NextAttemptTick);
            foreach (var shower in _showers)
                if (shower.RemoveAt is { } removeAt) at = Math.Min(at, removeAt);
            if (at > end) break;
            RunMeteorEvents(Math.Max(at, start + 1), first);
        }
        _meteorIdleTicks = checked(_meteorIdleTicks + ticks);
    }

    /// <summary>One attempt (the emulator's <c>hideSpawn</c> for a vehicle): timer first, then the player and sector-wide limits.</summary>
    private void AttemptMeteorShower(MeteorSpawnState spawn, long now)
    {
        var d = spawn.Definition;
        var initial = spawn.NextAttemptTick == 0;
        spawn.NextAttemptTick = checked(now + d.AttemptTicks);
        if (!initial && d.Probability == 0) return;
        if (PlayersInSector > d.MaxPlayers || PlayersInSector < d.MinPlayers) return;
        // Every shower in the sector counts, whichever spawner placed it (the emulator counts vehicles of the same type).
        if (d.MaxInLevel != -1 && _showers.Count >= d.MaxInLevel) return;
        var events = initial && d.InitialCount > 0 ? d.InitialCount : 1;
        var succeeded = false;
        for (var e = 0; e < events; e++) succeeded |= PlaceMeteorShower(spawn, now);
        if (succeeded) spawn.NextAttemptTick = checked(now + Math.Min(d.AttemptTicks, d.SucceedTicks));
    }

    /// <summary>One event (the emulator's <c>hideVehicle</c>): distance and area checks, then up to 10 tries for an open point.</summary>
    private bool PlaceMeteorShower(MeteorSpawnState spawn, long now)
    {
        var d = spawn.Definition;
        var centre = new Vector2(d.X, d.Y);
        if (d.MinPlayerDistance > 0 && !Player.IsDestroyed && Vector2.Distance(Player.Position, centre) <= d.MinPlayerDistance) return false;
        if (d.MaxPlayerDistance > 0 && (Player.IsDestroyed || Vector2.Distance(Player.Position, centre) > d.MaxPlayerDistance)) return false;
        if (d.MaxInArea != -1
            && _showers.Count(s => MathF.Abs(s.Position.X - d.X) <= d.Width / 2 && MathF.Abs(s.Position.Y - d.Y) <= d.Height / 2) >= d.MaxInArea)
            return false;
        for (var tries = 0; tries < DropTries; tries++)
        {
            var point = new Vector2(d.X + (float)((NextMeteorUnit() - 0.5) * d.Width), d.Y + (float)((NextMeteorUnit() - 0.5) * d.Height));
            if (Map is not null && Map.IsBlocked(point)) continue;
            var shower = new MeteorShowerState($"{MeteorShowerPrefix}{d.Id}:{++spawn.Placed}", d.Id, point)
            {
                RemoveAt = _shower!.LifetimeTicks > 0 ? checked(now + _shower.LifetimeTicks) : null,
                NextFireAt = now,
            };
            _showers.Add(shower);
            _showerEvents.Add(new MeteorShowerAppeared(_showerEvents.Count, shower.Id, d.Id, point.X, point.Y, TickNumber, d.Announcement));
            return true;
        }
        return false;
    }

    /// <summary>One meteor at the player when they are within range, aimed to meet them if they hold their course.</summary>
    private void FireMeteor(MeteorShowerState shower)
    {
        var meteor = _shower!.Meteor;
        var offset = Player.Position - shower.Position;
        if (offset.LengthSquared() > _shower.FireRadius * _shower.FireRadius) return;
        shower.NextFireAt = MeteorClock + _shower.FireIntervalTicks;
        var direction = LeadDirection(offset, Player.Velocity, meteor.MuzzleVelocity);
        _projectiles.Add(new ProjectileState(++_nextProjectileId, shower.Id, meteor, shower.Position, direction * meteor.MuzzleVelocity));
    }

    /// <summary>
    /// Direction that meets a target at <paramref name="offset"/> moving at <paramref name="velocity"/> with a shot of
    /// <paramref name="speed"/>: the smallest positive root of the intercept quadratic, or straight at the target when there is none
    /// (the emulator's <c>computeLeadFireAngle</c>, without its 256-step angle and isometric Y scale).
    /// </summary>
    public static Vector2 LeadDirection(Vector2 offset, Vector2 velocity, float speed)
    {
        if (offset == Vector2.Zero) return Vector2.UnitX;
        double a = Vector2.Dot(velocity, velocity) - (double)speed * speed;
        double b = 2.0 * Vector2.Dot(offset, velocity);
        double c = Vector2.Dot(offset, offset);
        double t = -1;
        if (Math.Abs(a) < 1e-9)
            t = b < 0 ? -c / b : -1;
        else
        {
            var disc = b * b - 4 * a * c;
            if (disc >= 0)
            {
                var root = Math.Sqrt(disc);
                var t0 = (-b + root) / (2 * a);
                var t1 = (-b - root) / (2 * a);
                t = t0 > 0 && t1 > 0 ? Math.Min(t0, t1) : Math.Max(t0, t1);
            }
        }
        var aim = t > 0 ? offset + velocity * (float)t : offset;
        return Vector2.Normalize(aim);
    }

    /// <summary>SplitMix64 in [0, 1), its own stream so meteor placement never shifts the mineral field.</summary>
    private double NextMeteorUnit()
    {
        var z = _meteorRandom += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (z >> 11) * (1.0 / (1UL << 53));
    }

    private void CaptureMeteors(out IReadOnlyList<MeteorSpawnSnapshot> spawns, out IReadOnlyList<MeteorShowerSnapshot> showers,
        out IReadOnlyList<MeteorSnapshot> inFlight)
    {
        inFlight = _projectiles.Where(p => p.OwnerShipId.StartsWith(MeteorShowerPrefix, StringComparison.Ordinal))
            .Select(p => new MeteorSnapshot(p.OwnerShipId, p.Position.X, p.Position.Y, p.Velocity.X, p.Velocity.Y, p.AgeTicks)).ToArray();
        spawns = _meteorSpawns.Select(s => new MeteorSpawnSnapshot(s.Definition.Id,
            s.NextAttemptTick == 0 ? 0 : Math.Max(1, s.NextAttemptTick - MeteorClock), s.Placed)).ToArray();
        showers = _showers.Select(s => new MeteorShowerSnapshot(s.Id, s.SpawnId, s.Position.X, s.Position.Y,
            s.RemoveAt is { } at ? Math.Max(1, at - MeteorClock) : null)).ToArray();
    }

    private void RestoreMeteors(SectorSnapshot snapshot)
    {
        if (_shower is null) return;
        var saved = (snapshot.MeteorSpawns ?? []).Where(s => s is not null).GroupBy(s => s.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var spawn in _meteorSpawns)
        {
            if (!saved.TryGetValue(spawn.Definition.Id, out var s)) continue;
            spawn.Placed = Math.Max(0, s.Placed);
            spawn.NextAttemptTick = s.TicksUntilAttempt <= 0 ? 0 : MeteorClock + s.TicksUntilAttempt;
        }
        var spawnIds = _meteorSpawns.Select(s => s.Definition.Id).ToHashSet(StringComparer.Ordinal);
        _showers.Clear();
        foreach (var s in snapshot.MeteorShowers ?? [])
        {
            if (s is null || string.IsNullOrEmpty(s.Id) || !spawnIds.Contains(s.SpawnId ?? "") || !float.IsFinite(s.X) || !float.IsFinite(s.Y)
                || _showers.Any(x => x.Id == s.Id))
                continue;
            // A shower saved without a time limit gets the current one, so a change of rule cannot leave one standing for ever.
            long? removeAt = s.RemoveInTicks is > 0 and var left
                ? MeteorClock + (_shower.LifetimeTicks > 0 ? Math.Min(left, _shower.LifetimeTicks) : left)
                : _shower.LifetimeTicks > 0 ? MeteorClock + _shower.LifetimeTicks : null;
            _showers.Add(new MeteorShowerState(s.Id, s.SpawnId!, new Vector2(s.X, s.Y)) { RemoveAt = removeAt, NextFireAt = MeteorClock });
        }
        if (saved.Count > 0) _meteorRandom = snapshot.MeteorRandomState;
        // Meteors already on their way keep coming after a relaunch (review #130 minor: a relaunch must not dodge a hit). Each one
        // keeps its position, velocity and age, so it lands or expires exactly when it would have.
        var meteor = _shower.Meteor;
        var lifetime = Math.Max(1, (int)MathF.Ceiling(meteor.LifetimeSeconds * TickRate));
        foreach (var m in snapshot.MeteorsInFlight ?? [])
        {
            if (m is null || string.IsNullOrEmpty(m.OwnerId) || !m.OwnerId.StartsWith(MeteorShowerPrefix, StringComparison.Ordinal)
                || !float.IsFinite(m.X) || !float.IsFinite(m.Y) || !float.IsFinite(m.VelocityX) || !float.IsFinite(m.VelocityY)
                || m.AgeTicks < 0 || m.AgeTicks >= lifetime)
                continue;
            var velocity = new Vector2(m.VelocityX, m.VelocityY);
            // A saved speed above the meteor's own is not trusted: it is put back to the meteor's speed in the same direction.
            if (velocity.Length() > meteor.MuzzleVelocity * 1.001f) velocity = Vector2.Normalize(velocity) * meteor.MuzzleVelocity;
            _projectiles.Add(new ProjectileState(++_nextProjectileId, m.OwnerId, meteor, new Vector2(m.X, m.Y), velocity) { AgeTicks = m.AgeTicks });
        }
    }
}

/// <summary>One meteor shower spawner's timer in a <see cref="SectorSnapshot"/> (as <see cref="MineralSpawnSnapshot"/>).</summary>
public sealed record MeteorSpawnSnapshot(string Id, long TicksUntilAttempt, long Placed);

/// <summary>A meteor in flight in a <see cref="SectorSnapshot"/>: the shower that fired it, where it is, its velocity and its age in ticks.</summary>
public sealed record MeteorSnapshot(string OwnerId, float X, float Y, float VelocityX, float VelocityY, int AgeTicks);

/// <summary>A standing meteor shower in a <see cref="SectorSnapshot"/>; <paramref name="RemoveInTicks"/> is null when it has no time limit.</summary>
public sealed record MeteorShowerSnapshot(string Id, string? SpawnId, float X, float Y, long? RemoveInTicks);
