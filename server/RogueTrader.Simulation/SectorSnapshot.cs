using System.Numerics;

namespace RogueTrader.Simulation;

/// <summary>One practice target's condition in a <see cref="SectorSnapshot"/>.</summary>
public sealed record TargetSnapshot(string Id, float Hull, float Shield, string? LastDamagedByShipId = null,
    string? DestroyedByShipId = null, string? LastDamageWeaponId = null);

/// <summary>
/// One pile still lying in space in a <see cref="SectorSnapshot"/>. <paramref name="ExpiresInTicks"/> is the time left before an
/// unpicked mineral drop vanishes; null for a pile that never expires.
/// </summary>
public sealed record PileSnapshot(string Id, string ItemId, int Quantity, float X, float Y, float Radius, long? ExpiresInTicks = null);

/// <summary>
/// One mineral spawn's timer in a <see cref="SectorSnapshot"/>. <paramref name="TicksUntilAttempt"/> is 0 only for a spawn that
/// has never attempted (it still stocks its starting piles); otherwise it is at least 1.
/// </summary>
public sealed record MineralSpawnSnapshot(string Id, long TicksUntilAttempt, long Placed);

/// <summary>An asteroid's remaining mining deposit in a <see cref="SectorSnapshot"/>.</summary>
public sealed record AsteroidSnapshot(string Id, int ResourceQuantity);

/// <summary>
/// The shared state of one sector that must outlive any one <see cref="WorldSimulation"/> (owner rule, November 2, 2026: the
/// world keeps its state for as long as the server runs, so a client relaunch, a logout or a sector reset never refills it).
/// It holds what a player could otherwise farm: the target group (condition, wave and the time left on a pending respawn), the
/// piles still in space (configured, mined and mineral piles alike), the mineral spawn timers and random state, and the mining
/// deposits. The player's ship, projectiles in flight and the per-world pickup and kill records are not part of it. Timers are
/// stored as ticks remaining, so a snapshot does not depend on the tick count of the world that produced it.
/// </summary>
public sealed record SectorSnapshot(
    string SectorId,
    int TargetWave,
    long? TargetRespawnTicksRemaining,
    IReadOnlyList<TargetSnapshot> Targets,
    IReadOnlyList<PileSnapshot> Piles,
    IReadOnlyList<MineralSpawnSnapshot> MineralSpawns,
    ulong MineralRandomState,
    IReadOnlyList<AsteroidSnapshot> Asteroids,
    long NextMiningPileId,
    long TicksUntilNextMine);

public sealed partial class WorldSimulation
{
    /// <summary>Captures the sector state that must survive this world (see <see cref="SectorSnapshot"/>).</summary>
    public SectorSnapshot CaptureSector() => new(
        Catalog.Sector.Id,
        TargetWave,
        TargetRespawnTick is { } at ? Math.Max(1, at - TickNumber) : null,
        Targets.Select(t => new TargetSnapshot(t.Id, t.Hull, t.Shield, t.LastDamagedByShipId, t.DestroyedByShipId, t.LastDamageWeaponId)).ToArray(),
        _collectibles.Select(p => new PileSnapshot(p.Id, p.ItemId, p.Quantity, p.Position.X, p.Position.Y, p.Radius,
            p.ExpiresAt is { } at ? Math.Max(1, at - MineralClock) : null)).ToArray(),
        _mineralSpawns.Select(s => new MineralSpawnSnapshot(s.Definition.Id,
            s.NextAttemptTick == 0 ? 0 : Math.Max(1, s.NextAttemptTick - MineralClock), s.Placed)).ToArray(),
        _mineralRandom,
        Asteroids.Select(a => new AsteroidSnapshot(a.Id, a.ResourceQuantity)).ToArray(),
        NextMiningPileId,
        Math.Max(0, NextMineTick - TickNumber));

    /// <summary>
    /// Puts a captured sector state into this world, replacing its target group, piles, mineral timers and deposits, so a new
    /// world continues the sector instead of starting it afresh. Entries are matched by id: a target, spawn or asteroid this
    /// world's data no longer has is ignored and one the snapshot lacks keeps its fresh state, and a pile whose item this world
    /// does not know is dropped, so a data update cannot break a saved sector. Piles that were collected stay gone. Call it
    /// before the first <see cref="Tick"/>. Returns false, changing nothing, for a snapshot of another sector.
    /// </summary>
    public bool RestoreSector(SectorSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!string.Equals(snapshot.SectorId, Catalog.Sector.Id, StringComparison.Ordinal)) return false;
        if (TickNumber != 0) throw new InvalidOperationException("A sector can only be restored into a world that has not ticked.");

        var targets = (snapshot.Targets ?? []).Where(t => t is not null).GroupBy(t => t.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var target in Targets)
        {
            if (!targets.TryGetValue(target.Id, out var saved)) continue;
            target.Hull = Finite(saved.Hull, 0, target.Definition.Defense.MaximumHull);
            target.Shield = Finite(saved.Shield, 0, target.Definition.Defense.MaximumShield);
            target.LastDamagedByShipId = saved.LastDamagedByShipId;
            target.DestroyedByShipId = target.IsDestroyed ? saved.DestroyedByShipId : null;
            target.LastDamageWeaponId = saved.LastDamageWeaponId;
        }
        TargetWave = Math.Max(0, snapshot.TargetWave);
        TargetRespawnTick = null;
        if (Targets.Count > 0 && Targets.All(t => t.IsDestroyed))
        {
            // A destroyed group always has a respawn pending; a missing or odd timer falls back to the full delay.
            if (TargetRespawnDelayTicks > 0)
                TargetRespawnTick = snapshot.TargetRespawnTicksRemaining is > 0 and var left ? Math.Min(left, TargetRespawnDelayTicks) : TargetRespawnDelayTicks;
        }

        _collectibles.Clear();
        foreach (var pile in snapshot.Piles ?? [])
        {
            if (pile is null || string.IsNullOrEmpty(pile.Id) || pile.Quantity <= 0 || !Cargo.Catalog.IsKnown(pile.ItemId ?? "")
                || !float.IsFinite(pile.X) || !float.IsFinite(pile.Y) || !float.IsFinite(pile.Radius) || pile.Radius <= 0
                || _collectibles.Any(p => p.Id == pile.Id))
                continue;
            _collectibleIds.Add(pile.Id);
            _collectibles.Add(new CollectibleState(new CollectibleSpawn(pile.Id, pile.ItemId!, pile.Quantity, pile.X, pile.Y, pile.Radius))
            {
                ExpiresAt = RestoredExpiry(pile),
            });
        }

        var spawns = (snapshot.MineralSpawns ?? []).Where(s => s is not null).GroupBy(s => s.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var spawn in _mineralSpawns)
        {
            if (!spawns.TryGetValue(spawn.Definition.Id, out var saved)) continue;
            spawn.Placed = Math.Max(0, saved.Placed);
            spawn.NextAttemptTick = saved.TicksUntilAttempt <= 0 ? 0 : MineralClock + saved.TicksUntilAttempt;
        }
        if (_mineralSpawns.Length > 0) _mineralRandom = snapshot.MineralRandomState;

        var deposits = (snapshot.Asteroids ?? []).Where(a => a is not null).GroupBy(a => a.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var asteroid in Asteroids)
            if (asteroid.ResourceItemId is not null && deposits.TryGetValue(asteroid.Id, out var saved))
                asteroid.ResourceQuantity = Math.Clamp(saved.ResourceQuantity, 0, asteroid.ResourceQuantity);
        NextMiningPileId = Math.Max(NextMiningPileId, snapshot.NextMiningPileId);
        NextMineTick = Math.Max(0, snapshot.TicksUntilNextMine);
        return true;
    }

    /// <summary>
    /// When a restored pile expires. A mineral drop saved before drops expired (no expiry in the snapshot) gets its spawn's full
    /// life from now, so a field saved under the older, much larger limits drains back to the original ones.
    /// </summary>
    private long? RestoredExpiry(PileSnapshot pile)
    {
        if (pile.ExpiresInTicks is > 0 and var left) return checked(MineralClock + left);
        if (!pile.Id.StartsWith(MineralPilePrefix, StringComparison.Ordinal)) return null;
        var rest = pile.Id[MineralPilePrefix.Length..];
        var colon = rest.LastIndexOf(':');
        var spawnId = colon < 0 ? rest : rest[..colon];
        var life = _mineralSpawns.FirstOrDefault(s => s.Definition.Id == spawnId)?.Definition.ExpireTicks ?? 0;
        return life > 0 ? checked(MineralClock + life) : null;
    }

    private static float Finite(float value, float minimum, float maximum) =>
        float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : maximum;
}
