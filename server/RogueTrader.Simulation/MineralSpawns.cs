using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueTrader.Simulation;

/// <summary>
/// One mineral spawn area (queue M7 mining slice), an original LIO type 5 "hide" row. The fields follow the hide semantics of the
/// InfantryOnline emulator (<c>LioInfo.Hide.cs</c>, <c>ArenaLio.hideSpawn</c>/<c>hideItem</c> at bb97579; research note
/// docs/original-research/ZONE_ASSETS_COMPARISON.md section 4, followed on Steve's call, November 3, 2026). Unit conversion
/// (world units, ticks) is done by whoever builds the catalog.
/// </summary>
/// <param name="Id">Stable spawn id (for original rows, the LIO row's id field).</param>
/// <param name="ItemId">Cargo item a drop holds (f[20] HideId).</param>
/// <param name="Quantity">Units in each drop (f[21] HideQuantity).</param>
/// <param name="X">Centre of the spawn area in world units (f[4]).</param>
/// <param name="Y">Centre of the spawn area in world units (f[5]).</param>
/// <param name="Width">Full width of the spawn area in world units (f[6]).</param>
/// <param name="Height">Full height of the spawn area in world units (f[7]).</param>
/// <param name="AttemptTicks">Wait after any attempt before the next one (f[11] AttemptDelay).</param>
/// <param name="SucceedTicks">Wait after a successful attempt (f[12] SucceedDelay); the next attempt comes after whichever of the two delays is shorter, as in the emulator.</param>
/// <param name="PileRadius">Pickup radius of each drop.</param>
/// <param name="InitialCount">Spawn events run when the sector starts (f[10]); 0 makes the first attempt an ordinary one.</param>
/// <param name="MaxInArea">An event fails while the spawn's own rectangle already holds this many units of the item (f[18]; -1 = no limit).</param>
/// <param name="MaxInLevel">An attempt fails while the whole sector holds this many units of the item, over every pile of it (f[19]; -1 = no limit).</param>
/// <param name="ClumpRadius">Drops of one event land in a box of this size around a random anchor in the area (f[26]; 0 = anywhere in the area).</param>
/// <param name="ClumpQuantity">Drops placed per event (f[27]; 0 counts as 1).</param>
/// <param name="MinPlayers">An attempt fails while fewer players than this are in the sector (f[14]).</param>
/// <param name="MaxPlayers">An attempt fails while more players than this are in the sector (f[15]).</param>
/// <param name="MinPlayerDistance">An event fails while a player is within this distance of the spawn centre (f[16]; 0 = no check).</param>
/// <param name="MaxPlayerDistance">An event fails unless a player is within this distance of the spawn centre (f[17]; 0 = no check).</param>
/// <param name="ExpireTicks">A drop vanishes this long after it lands (terrain PrizeExpire; 0 = never).</param>
/// <param name="Probability">f[13]: 0 means the spawn runs only its initial events.</param>
public sealed record MineralSpawnDefinition(string Id, string ItemId, int Quantity, float X, float Y, float Width, float Height,
    int AttemptTicks, int SucceedTicks, float PileRadius, int InitialCount = 1, int MaxInArea = -1, int MaxInLevel = -1,
    float ClumpRadius = 0, int ClumpQuantity = 1, int MinPlayers = 0, int MaxPlayers = int.MaxValue, float MinPlayerDistance = 0,
    float MaxPlayerDistance = 0, long ExpireTicks = 0, int Probability = 1);

/// <summary>
/// Mineral spawn areas for one sector, plus the seed of their placement. Omitted from the world, no minerals appear. On its
/// first tick the world runs every spawn's initial events; afterwards each spawn attempts on its own delays (see
/// <see cref="MineralSpawnDefinition"/>).
/// </summary>
public sealed class MineralFieldCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // The derived file also carries raw evidence fields; only the members below are read.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };

    public IReadOnlyList<MineralSpawnDefinition> Spawns { get; }
    public ulong Seed { get; }

    /// <summary>
    /// This field reseeded for one world: <see cref="Seed"/> XOR <paramref name="worldSalt"/>. Callers pass a value that is
    /// stable across processes (for example GUID bytes, never <c>string.GetHashCode</c>, which .NET randomizes per process),
    /// so a logged world id or salt replays the same placement.
    /// </summary>
    public MineralFieldCatalog ForWorld(ulong worldSalt) => new(Spawns, Seed ^ worldSalt);

    public MineralFieldCatalog(IEnumerable<MineralSpawnDefinition> spawns, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(spawns);
        Spawns = Array.AsReadOnly(spawns.ToArray());
        Seed = seed;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in Spawns)
        {
            if (s is null || string.IsNullOrWhiteSpace(s.Id) || !ids.Add(s.Id) || string.IsNullOrWhiteSpace(s.ItemId))
                throw new ArgumentException("Mineral spawns need unique ids and an item id.", nameof(spawns));
            if (s.Quantity <= 0 || s.AttemptTicks <= 0 || s.SucceedTicks <= 0 || s.InitialCount < 0 || s.MaxInArea < -1
                || s.MaxInLevel < -1 || s.ClumpQuantity < 0 || s.MinPlayers < 0 || s.MaxPlayers < 0 || s.ExpireTicks < 0 || s.Probability < 0)
                throw new ArgumentException($"Mineral spawn '{s.Id}' needs a positive quantity and delays and no negative limits.", nameof(spawns));
            if (!float.IsFinite(s.X) || !float.IsFinite(s.Y) || !float.IsFinite(s.Width) || !float.IsFinite(s.Height)
                || s.Width < 0 || s.Height < 0 || !float.IsFinite(s.PileRadius) || s.PileRadius <= 0
                || !float.IsFinite(s.ClumpRadius) || s.ClumpRadius < 0 || !float.IsFinite(s.MinPlayerDistance) || s.MinPlayerDistance < 0
                || !float.IsFinite(s.MaxPlayerDistance) || s.MaxPlayerDistance < 0)
                throw new ArgumentException($"Mineral spawn '{s.Id}' needs a finite area, distances and a positive pile radius.", nameof(spawns));
        }
    }

    /// <summary>Document shape: <c>{ "seed": n, "spawns": [ MineralSpawnDefinition... ] }</c>; other members are ignored.</summary>
    public static MineralFieldCatalog FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<Document>(json, JsonOptions) ?? throw new JsonException("Empty mineral field definition.");
        return new MineralFieldCatalog(document.Spawns ?? throw new JsonException("Missing spawns."), document.Seed);
    }

    private sealed class Document
    {
        public ulong Seed { get; set; }
        public List<MineralSpawnDefinition>? Spawns { get; set; }
    }
}

/// <summary>One pickup: <paramref name="Quantity"/> units of a pile entered the player's hold. <paramref name="Sequence"/> counts pickups per world from 0.</summary>
public sealed record CargoPickup(int Sequence, string PileId, string ItemId, int Quantity, long Tick)
{
    /// <summary>True for a pile placed by a mineral spawn.</summary>
    public bool IsMineral => PileId.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal);
}

public sealed partial class WorldSimulation
{
    /// <summary>Id prefix of piles placed by mineral spawns: <c>mineral:&lt;spawn id&gt;:&lt;n&gt;</c>.</summary>
    public const string MineralPilePrefix = "mineral:";

    private readonly List<CargoPickup> _pickups = [];
    private readonly MineralSpawnState[] _mineralSpawns;
    private ulong _mineralRandom;
    // Ticks that passed without ticking (docked, paused, inspecting, or the game closed): mineral timers run on real time,
    // so they count these too (owner rule, November 2, 2026). The mineral clock is TickNumber plus this.
    private long _mineralIdleTicks;
    private long MineralClock => TickNumber + _mineralIdleTicks;

    /// <summary>Every pickup in the order it happened, mineral and other piles alike.</summary>
    public IReadOnlyList<CargoPickup> Pickups => _pickups;

    /// <summary>Mineral spawn areas of this world (empty when no mineral field was supplied).</summary>
    public IReadOnlyList<MineralSpawnDefinition> MineralSpawns => _mineralSpawns.Select(s => s.Definition).ToArray();

    internal ulong MineralRandomState => _mineralRandom;
    internal IEnumerable<(long NextAttemptTick, long Placed)> MineralSpawnTimers => _mineralSpawns.Select(s => (s.NextAttemptTick, s.Placed));

    /// <summary>
    /// Players counted by a spawn's <see cref="MineralSpawnDefinition.MinPlayers"/>/<see cref="MineralSpawnDefinition.MaxPlayers"/>
    /// check. The world simulates one player; a multiplayer server passes its sector's count here.
    /// </summary>
    public int PlayersInSector { get; set; } = 1;

    /// <summary>Tries per event to find a drop point outside every asteroid (the emulator's 10, with 5 back per drop placed).</summary>
    private const int DropTries = 10;

    private sealed class MineralSpawnState(MineralSpawnDefinition definition)
    {
        public MineralSpawnDefinition Definition { get; } = definition;
        /// <summary>Mineral clock tick of the next attempt; 0 until the spawn's initial events have run.</summary>
        public long NextAttemptTick { get; set; }
        public long Placed { get; set; }
    }

    private static MineralSpawnState[] CreateMineralSpawns(MineralFieldCatalog? minerals, CargoCatalog cargo)
    {
        if (minerals is null) return [];
        foreach (var spawn in minerals.Spawns)
            if (!cargo.IsKnown(spawn.ItemId))
                throw new ArgumentException($"Mineral spawn '{spawn.Id}' names unknown cargo item '{spawn.ItemId}'.", nameof(minerals));
        return minerals.Spawns.Select(s => new MineralSpawnState(s)).ToArray();
    }

    private void RecordPickup(CollectibleState pile, int accepted)
    {
        if (accepted > 0) _pickups.Add(new CargoPickup(_pickups.Count, pile.Id, pile.ItemId, accepted, TickNumber));
    }

    /// <summary>Removes expired drops, then runs every spawn whose attempt is due, in catalog order.</summary>
    private void StepMineralSpawns() => RunMineralEvents(MineralClock, MineralClock);

    /// <summary>
    /// Expiry first, then the due spawns in catalog order: the same order whether the world is ticking or catching up.
    /// <paramref name="first"/> is the tick on which a spawn whose initial events have not run yet runs them.
    /// </summary>
    private void RunMineralEvents(long now, long first)
    {
        if (_mineralSpawns.Length == 0) return;
        _collectibles.RemoveAll(p => p.ExpiresAt is { } at && at <= now);
        foreach (var spawn in _mineralSpawns)
            if (spawn.NextAttemptTick == 0 ? now >= first : now >= spawn.NextAttemptTick)
                AttemptMineral(spawn, now);
    }

    /// <summary>
    /// Lets mineral spawns and drop expiry run through <paramref name="ticks"/> of time in which the world did not tick: every
    /// expiry and attempt that falls due happens at its own tick, in the order ticking would run them, so the result is the
    /// same as flying for that time (the player stays where they are). A gap longer than <see cref="LongGapTicks"/> is caught up
    /// as its last part only: every drop placed before that would have expired by its end, so only the field's timing state
    /// carries over. That shortcut needs every spawn to expire its drops; without expiry the whole gap is run.
    /// </summary>
    private void ElapseMineralSpawns(long ticks)
    {
        if (_mineralSpawns.Length == 0)
        {
            _mineralIdleTicks = checked(_mineralIdleTicks + ticks);
            return;
        }
        var start = MineralClock;
        var end = checked(start + ticks);
        var first = start + 1; // a spawn still waiting for its initial events runs them on the first tick that would have run
        var longGap = LongGapTicks;
        if (longGap > 0 && ticks > longGap)
        {
            var resume = end - longGap;
            _collectibles.RemoveAll(p => p.ExpiresAt is { } at && at <= resume);
            foreach (var spawn in _mineralSpawns)
                if (spawn.NextAttemptTick != 0) spawn.NextAttemptTick = Math.Max(spawn.NextAttemptTick, resume);
            first = Math.Max(first, resume);
        }
        while (true)
        {
            var at = long.MaxValue;
            foreach (var spawn in _mineralSpawns)
                at = Math.Min(at, spawn.NextAttemptTick == 0 ? first : spawn.NextAttemptTick);
            foreach (var pile in _collectibles)
                if (pile.ExpiresAt is { } expires) at = Math.Min(at, expires);
            if (at > end) break;
            RunMineralEvents(Math.Max(at, start + 1), first);
        }
        _mineralIdleTicks = checked(_mineralIdleTicks + ticks);
    }

    /// <summary>Longest drop life plus twice the longest delay; 0 when some spawn never expires its drops.</summary>
    private long LongGapTicks => _mineralSpawns.All(s => s.Definition.ExpireTicks > 0)
        ? _mineralSpawns.Max(s => s.Definition.ExpireTicks) + 2L * _mineralSpawns.Max(s => Math.Max(s.Definition.AttemptTicks, s.Definition.SucceedTicks))
        : 0;

    /// <summary>
    /// One attempt (the emulator's <c>hideSpawn</c>). Its timer restarts first, so a failed attempt waits
    /// <see cref="MineralSpawnDefinition.AttemptTicks"/>. It fails when only initial events are allowed, when the sector holds
    /// too few or too many players, or when the sector already holds <see cref="MineralSpawnDefinition.MaxInLevel"/> units of the
    /// item. Otherwise it runs its events (the initial count the first time, then one), and a success waits the shorter delay.
    /// </summary>
    private void AttemptMineral(MineralSpawnState spawn, long now)
    {
        var d = spawn.Definition;
        var initial = spawn.NextAttemptTick == 0;
        spawn.NextAttemptTick = checked(now + d.AttemptTicks);
        if (!initial && d.Probability == 0) return;
        if (PlayersInSector > d.MaxPlayers || PlayersInSector < d.MinPlayers) return;
        // Same-item drops as they lie before this attempt (the emulator filters the list once per attempt).
        var same = _collectibles.Where(p => p.ItemId == d.ItemId).ToArray();
        if (d.MaxInLevel != -1 && same.Sum(p => (long)p.Quantity) >= d.MaxInLevel) return;
        var events = initial && d.InitialCount > 0 ? d.InitialCount : 1;
        var succeeded = false;
        for (var e = 0; e < events; e++) succeeded |= HideEvent(spawn, same, now);
        if (succeeded) spawn.NextAttemptTick = checked(now + Math.Min(d.AttemptTicks, d.SucceedTicks));
    }

    /// <summary>One spawn event (the emulator's <c>hideItem</c>): area and player-distance checks, then one clump of drops.</summary>
    private bool HideEvent(MineralSpawnState spawn, IReadOnlyList<CollectibleState> same, long now)
    {
        var d = spawn.Definition;
        var centre = new Vector2(d.X, d.Y);
        if (d.MinPlayerDistance > 0 && !Player.IsDestroyed && Vector2.Distance(Player.Position, centre) <= d.MinPlayerDistance) return false;
        if (d.MaxPlayerDistance > 0 && (Player.IsDestroyed || Vector2.Distance(Player.Position, centre) > d.MaxPlayerDistance)) return false;
        if (d.MaxInArea != -1)
        {
            long inArea = 0;
            foreach (var pile in same)
                if (MathF.Abs(pile.Position.X - d.X) <= d.Width / 2 && MathF.Abs(pile.Position.Y - d.Y) <= d.Height / 2) inArea += pile.Quantity;
            if (inArea >= d.MaxInArea) return false;
        }
        var anchor = d.ClumpRadius > 0 ? RandomIn(centre, d.Width, d.Height) : centre;
        var left = d.ClumpQuantity == 0 ? 1 : d.ClumpQuantity;
        var placedAny = false;
        var prefix = $"{MineralPilePrefix}{d.Id}:";
        for (var tries = 0; tries < DropTries; tries++)
        {
            var point = d.ClumpRadius > 0 ? RandomIn(anchor, d.ClumpRadius, d.ClumpRadius) : RandomIn(centre, d.Width, d.Height);
            if (Asteroids.Any(a => Vector2.Distance(a.Position, point) < a.Radius + d.PileRadius)) continue;
            string id;
            do { id = $"{prefix}{++spawn.Placed}"; } while (!_collectibleIds.Add(id));
            _collectibles.Add(new CollectibleState(new CollectibleSpawn(id, d.ItemId, d.Quantity, point.X, point.Y, d.PileRadius))
            {
                ExpiresAt = d.ExpireTicks > 0 ? checked(now + d.ExpireTicks) : null,
            });
            placedAny = true;
            if (--left > 0)
            {
                tries -= 5;
                continue;
            }
            break;
        }
        return placedAny;
    }

    private Vector2 RandomIn(Vector2 centre, float width, float height) =>
        new(centre.X + (float)((NextMineralUnit() - 0.5) * width), centre.Y + (float)((NextMineralUnit() - 0.5) * height));

    /// <summary>SplitMix64 in [0, 1): placement is reproducible from the catalog seed on any runtime.</summary>
    private double NextMineralUnit()
    {
        var z = _mineralRandom += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (z >> 11) * (1.0 / (1UL << 53));
    }
}
