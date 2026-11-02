using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueTrader.Simulation;

/// <summary>
/// One mineral spawn area (queue M7 mining slice). Item id and the raw field values come from an original LIO type 5 row;
/// how they are read (world units, ticks, piles) is decided by whoever builds the catalog and is NEW unless stated there.
/// </summary>
/// <param name="Id">Stable spawn id (for original rows, the LIO row's id field).</param>
/// <param name="ItemId">Cargo item a pile holds.</param>
/// <param name="Quantity">Units in each new pile.</param>
/// <param name="MaxPiles">Piles from this spawn that may lie in space at once; an attempt while that many remain fails.</param>
/// <param name="X">Centre of the spawn area in world units.</param>
/// <param name="Y">Centre of the spawn area in world units.</param>
/// <param name="Width">Full width of the spawn area in world units.</param>
/// <param name="Height">Full height of the spawn area in world units.</param>
/// <param name="AttemptTicks">Ticks before a failed attempt is retried.</param>
/// <param name="SucceedTicks">Ticks after a successful spawn before the next attempt.</param>
/// <param name="ClearRadius">An attempt fails when the player's centre is within this distance of the chosen point (0 = never).</param>
/// <param name="PileRadius">Pickup radius of each pile.</param>
/// <param name="StartingPiles">Piles placed on the spawn's first attempt, capped at <paramref name="MaxPiles"/>; 0 or 1 places one.</param>
public sealed record MineralSpawnDefinition(string Id, string ItemId, int Quantity, int MaxPiles, float X, float Y,
    float Width, float Height, int AttemptTicks, int SucceedTicks, float ClearRadius, float PileRadius, int StartingPiles = 0);

/// <summary>
/// Mineral spawn areas for one sector, plus the seed of their placement. Omitted from the world, no minerals appear. The world
/// attempts every spawn on its first tick, placing its <see cref="MineralSpawnDefinition.StartingPiles"/>, so a fresh sector
/// starts stocked; afterwards each spawn places one pile per attempt on its own delays.
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
            if (s.Quantity <= 0 || s.MaxPiles <= 0 || s.AttemptTicks <= 0 || s.SucceedTicks <= 0 || s.StartingPiles < 0)
                throw new ArgumentException($"Mineral spawn '{s.Id}' needs positive quantity, pile limit and delays.", nameof(spawns));
            if (!float.IsFinite(s.X) || !float.IsFinite(s.Y) || !float.IsFinite(s.Width) || !float.IsFinite(s.Height)
                || s.Width < 0 || s.Height < 0 || !float.IsFinite(s.ClearRadius) || s.ClearRadius < 0
                || !float.IsFinite(s.PileRadius) || s.PileRadius <= 0)
                throw new ArgumentException($"Mineral spawn '{s.Id}' needs a finite area and a positive pile radius.", nameof(spawns));
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

    private const int MaxPlacementTries = 8;
    private readonly List<CargoPickup> _pickups = [];
    private readonly MineralSpawnState[] _mineralSpawns;
    private ulong _mineralRandom;

    /// <summary>Every pickup in the order it happened, mineral and other piles alike.</summary>
    public IReadOnlyList<CargoPickup> Pickups => _pickups;

    /// <summary>Mineral spawn areas of this world (empty when no mineral field was supplied).</summary>
    public IReadOnlyList<MineralSpawnDefinition> MineralSpawns => _mineralSpawns.Select(s => s.Definition).ToArray();

    internal ulong MineralRandomState => _mineralRandom;
    internal IEnumerable<(long NextAttemptTick, long Placed)> MineralSpawnTimers => _mineralSpawns.Select(s => (s.NextAttemptTick, s.Placed));

    private sealed class MineralSpawnState(MineralSpawnDefinition definition)
    {
        public MineralSpawnDefinition Definition { get; } = definition;
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

    /// <summary>
    /// Runs every spawn whose attempt is due. A due attempt picks a point in the spawn's area and places a pile there unless the
    /// spawn already has <see cref="MineralSpawnDefinition.MaxPiles"/> piles in space, the point lies inside an asteroid, or the
    /// player is within the clear radius. Success waits <see cref="MineralSpawnDefinition.SucceedTicks"/>, failure
    /// <see cref="MineralSpawnDefinition.AttemptTicks"/>.
    /// </summary>
    private void StepMineralSpawns()
    {
        foreach (var spawn in _mineralSpawns)
        {
            if (TickNumber < spawn.NextAttemptTick) continue;
            var definition = spawn.Definition;
            var prefix = $"{MineralPilePrefix}{definition.Id}:";
            var alive = _collectibles.Count(p => p.Id.StartsWith(prefix, StringComparison.Ordinal));
            // A spawn that has never attempted (timer still 0) stocks its starting piles; later attempts place one.
            var wanted = spawn.NextAttemptTick == 0 ? Math.Max(1, definition.StartingPiles) : 1;
            var placed = 0;
            while (placed < wanted && alive + placed < definition.MaxPiles && TryPlaceMineral(spawn, prefix)) placed++;
            spawn.NextAttemptTick = checked(TickNumber + (placed > 0 ? definition.SucceedTicks : definition.AttemptTicks));
        }
    }

    private bool TryPlaceMineral(MineralSpawnState spawn, string prefix)
    {
        var d = spawn.Definition;
        for (var attempt = 0; attempt < MaxPlacementTries; attempt++)
        {
            var point = new Vector2(d.X + (float)((NextMineralUnit() - 0.5) * d.Width), d.Y + (float)((NextMineralUnit() - 0.5) * d.Height));
            if (Asteroids.Any(a => Vector2.Distance(a.Position, point) < a.Radius + d.PileRadius)) continue;
            if (d.ClearRadius > 0 && !Player.IsDestroyed && Vector2.Distance(Player.Position, point) <= d.ClearRadius) return false;
            string id;
            do { id = $"{prefix}{++spawn.Placed}"; } while (!_collectibleIds.Add(id));
            _collectibles.Add(new CollectibleState(new CollectibleSpawn(id, d.ItemId, d.Quantity, point.X, point.Y, d.PileRadius)));
            return true;
        }
        return false;
    }

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
