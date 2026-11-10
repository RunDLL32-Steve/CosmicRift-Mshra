using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

/// <summary>Pure C# world authority. One Tick always advances exactly 1/60 of a second.</summary>
public sealed partial class WorldSimulation
{
    public const int TickRate = 60;
    public const float TickDuration = 1f / TickRate;
    /// <summary>Ship id of the local player.</summary>
    public const string PlayerShipId = "player";
    private readonly List<ProjectileState> _projectiles = [];
    private readonly List<CollectibleState> _collectibles;
    private readonly List<TargetKill> _kills = [];
    private long _nextProjectileId;

    public GameCatalog Catalog { get; }
    public ShipState Player { get; }
    public IReadOnlyList<ShipState> Targets { get; }
    public IReadOnlyList<AsteroidState> Asteroids { get; }
    public IReadOnlyList<ProjectileState> Projectiles { get; }
    /// <summary>Piles still lying in space: configured catalog order, then mining creation order.</summary>
    public IReadOnlyList<CollectibleState> Collectibles { get; }
    public CargoHold Cargo { get; }
    public long TickNumber { get; private set; }
    public int DestroyedTargets { get; private set; }
    /// <summary>Every target destruction in the order it happened; a target appears at most once per world instance.</summary>
    public IReadOnlyList<TargetKill> Kills => _kills;
    /// <summary>How many times the target group has respawned in this world (0 for the first group).</summary>
    public int TargetWave { get; private set; }
    /// <summary>Tick at which the destroyed target group respawns; null while any target is alive or respawn is off.</summary>
    public long? TargetRespawnTick { get; private set; }
    private int TargetRespawnDelayTicks => (int)MathF.Ceiling(Catalog.Sector.TargetRespawnSeconds * TickRate);
    public int AsteroidCollisions { get; private set; }
    /// <summary>Times the player's ship bounced off a blocked map tile (see <see cref="MapCollision"/>).</summary>
    public int WallCollisions { get; private set; }
    /// <summary>The tile map this world is flown on; null for an open practice sector with no walls.</summary>
    public SectorMap? Map { get; }
    public long ShotsFired { get; private set; }
    /// <summary>Ticks on which the player held fire inside a safe zone, where weapons are offline (RT-F065).</summary>
    public long ShotsRefusedInSafeZone { get; private set; }
    /// <summary>Whether the player's ship is in a safe zone (CFG Safety=1): it cannot be damaged and cannot fire.</summary>
    public bool PlayerInSafeZone => IsSafe(Player);

    private bool IsSafe(ShipState ship) => Map?.IsSafeAt(ship.Position) == true;

    /// <param name="cargo">Cargo rules and collectible piles; omitted means no cargo space and no piles.</param>
    /// <param name="initialCargo">Starting contents of the player's hold (for example a persisted inventory).</param>
    /// <param name="map">Walls and terrain; omitted means open space everywhere and drops keep their spawn's own life.</param>
    public WorldSimulation(GameCatalog catalog, CargoCatalog? cargo = null,
        IEnumerable<KeyValuePair<string, int>>? initialCargo = null, MiningCatalog? mining = null, MineralFieldCatalog? minerals = null,
        SectorMap? map = null, MeteorShowerCatalog? meteors = null,
        DefenseSatelliteCatalog? satellites = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Catalog = catalog;
        Map = map;
        cargo ??= new CargoCatalog(0, []);
        Cargo = new CargoHold(cargo, initialCargo);
        _collectibles = cargo.Collectibles.Select(spawn => new CollectibleState(spawn)).ToList();
        Collectibles = _collectibles.AsReadOnly();
        var spawn = catalog.Sector.PlayerSpawn;
        Player = new ShipState(PlayerShipId, catalog.Ship, catalog.Weapon, new Vector2(spawn.X, spawn.Y), spawn.Heading)
        {
            Velocity = ShipMotion.ClampSpeed(new Vector2(spawn.VelocityX, spawn.VelocityY), catalog.Ship.Physics.MaximumSpeed)
        };
        Targets = Array.AsReadOnly(catalog.Sector.Targets.Select(target => new ShipState(
            target.Id, catalog.Ship with
            {
                Radius = target.Radius,
                Defense = new ShipDefense { MaximumHull = target.Hull, MaximumShield = target.Shield, Armor = target.Armor }
            }, catalog.Weapon, new Vector2(target.X, target.Y), target.Heading)).ToArray());
        Asteroids = Array.AsReadOnly(catalog.Sector.Asteroids.Select(asteroid => new AsteroidState(asteroid)).ToArray());
        Projectiles = _projectiles.AsReadOnly();
        foreach (var pile in _collectibles) _collectibleIds.Add(pile.Id);
        if (mining is not null) ValidateMining(mining, cargo);
        _miningEquipment = mining?.Equipment;
        _mineralSpawns = CreateMineralSpawns(minerals, cargo);
        _mineralRandom = minerals?.Seed ?? 0;
        if (meteors is not null && meteors.Spawns.Count > 0)
        {
            _shower = meteors.Shower;
            _meteorSpawns = meteors.Spawns.Select(s => new MeteorSpawnState(s)).ToArray();
            _meteorRandom = meteors.Seed;
        }
        if (satellites is not null && satellites.Spawns.Count > 0)
        {
            _satellite = satellites.Satellite;
            _satelliteSpawns = satellites.Spawns.Select(s => new SatelliteSpawnState(s)).ToArray();
            _satelliteRandom = satellites.Seed;
        }
    }

    public void Tick(ShipInput input)
    {
        TickNumber++;
        if (TargetRespawnTick is { } respawnAt && TickNumber >= respawnAt) RespawnTargets();
        StepMineralSpawns();
        StepMeteorShowers();
        StepDefenseSatellites();
        if (!Player.IsDestroyed)
        {
            var previous = Player.Position;
            MovePlayer(input);
            CollectPickups(previous);
            // Existing swept pickups precede extraction; new drops use only the current position.
            if (input.Mine && TryMine()) CollectPickups(Player.Position);
            var recharge = !Player.IsHyperEngaged || Player.Definition.Hyper!.RechargeWhileEngaged
                ? Player.Definition.Reactor.EnergyRegenerationPerSecond : 0;
            // A terrain's EnergyRate adds to the hull's recharge: Rogue's safe zones drain 200 energy a second (RT-F065).
            var terrainRate = Map?.RuleAt(Player.Position).EnergyPerSecond ?? 0;
            Player.Energy = Math.Clamp(Player.Energy + (recharge + terrainRate) * TickDuration, 0, Player.Definition.Reactor.MaximumEnergy);
            if (input.Fire)
                TryFire();
        }
        MoveProjectiles();
    }

    /// <summary>
    /// Restores player hull after an authoritative repair purchase, clamped to the hull maximum. Returns the hull actually
    /// restored. A destroyed ship is not revived (reset handles that). The amount comes from the caller because the original
    /// repair amount/unit is undecoded (RT-F047).
    /// </summary>
    public float RepairPlayerHull(float hullPoints)
    {
        if (!float.IsFinite(hullPoints) || hullPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(hullPoints), "Repair amount must be finite and non-negative.");
        if (Player.IsDestroyed) return 0;
        var before = Player.Hull;
        Player.Hull = MathF.Min(Player.Definition.Defense.MaximumHull, before + hullPoints);
        return Player.Hull - before;
    }

    /// <summary>
    /// Takes hull points from the player's ship, never below zero; the ship is destroyed at zero. Returns the hull actually
    /// removed. This direct hook ignores safe zones (acceptance checks use it at a dock); hazards that call it must check
    /// <see cref="PlayerInSafeZone"/> first, as shots do. Nothing in the practice sector damages the player yet, so this is the entry point for hazards, enemy fire and
    /// acceptance checks of the destruction rule (Steve, Nov 3 2026).
    /// </summary>
    public float DamagePlayer(float hullPoints)
    {
        if (!float.IsFinite(hullPoints) || hullPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(hullPoints), "Damage must be finite and non-negative.");
        var before = Player.Hull;
        Player.Hull = MathF.Max(0, before - hullPoints);
        return before - Player.Hull;
    }

    /// <summary>
    /// Places the player as a saved ship condition describes, at rest. Hull is clamped to the hull maximum. A saved hull of zero
    /// (a ship saved while destroyed) is not restored: the caller keeps the fresh spawn instead (NEW/provisional rule). On a map,
    /// a saved position on a wall or outside the map keeps the ship where it is (the spawn) but still restores its heading and
    /// hull, so a stale position can never repair the ship for free.
    /// </summary>
    public bool RestorePlayer(Vector2 position, float heading, float hull)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(heading) || !float.IsFinite(hull) || hull <= 0)
            return false;
        if (Map is null || !Map.IsBlocked(position)) Player.Position = position;
        Player.Heading = MathF.IEEERemainder(heading, MathF.Tau);
        Player.Velocity = Vector2.Zero;
        Player.Hull = MathF.Min(Player.Definition.Defense.MaximumHull, hull);
        return true;
    }

    /// <summary>
    /// Swaps the player's ship statistics in place (for example after an upgrade is installed or a profile reload): position,
    /// velocity, heading and world state are untouched; hull, shield and energy keep their values clamped to the new maxima;
    /// a destroyed ship stays destroyed. The radius must not change, so collision contacts stay valid.
    /// </summary>
    public void ApplyPlayerDefinition(ShipDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Radius != Player.Definition.Radius)
            throw new ArgumentException("A replacement ship definition must keep the same radius.", nameof(definition));
        Player.Definition = definition;
        Player.Hull = MathF.Min(Player.Hull, definition.Defense.MaximumHull);
        Player.Shield = MathF.Min(Player.Shield, definition.Defense.MaximumShield);
        Player.Energy = MathF.Min(Player.Energy, definition.Reactor.MaximumEnergy);
    }

    // NEW practice-range rule: the whole group comes back together, at its configured positions, as the next wave.
    private void RespawnTargets()
    {
        for (var i = 0; i < Targets.Count; i++)
        {
            var definition = Catalog.Sector.Targets[i];
            Targets[i].Revive(new Vector2(definition.X, definition.Y), definition.Heading);
        }
        TargetWave++;
        TargetRespawnTick = null;
    }

    /// <summary>
    /// Counts time that passes while the world is not ticking (the player is docked, paused or in the inspector, or the game
    /// was closed) toward a pending target-group respawn and toward the mineral spawn timers, so both run on real elapsed time
    /// (owner rules, November 1 and 2, 2026). Mineral spawns place the piles that fall due in that time; nothing else moves
    /// and <see cref="TickNumber"/> does not change. The group respawns at once if its delay has run out.
    /// </summary>
    public void ElapseWithoutTicking(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (ticks == 0) return;
        ElapseMineralSpawns(ticks);
        ElapseMeteorShowers(ticks);
        ElapseDefenseSatellites(ticks);
        if (TargetRespawnTick is not { } respawnAt) return;
        TargetRespawnTick = respawnAt - ticks;
        if (TargetRespawnTick <= TickNumber) RespawnTargets();
    }

    /// <summary>
    /// Carries the target group's state (hull, shield, destroyed state, wave and the time left on a pending respawn) over from
    /// another world of the same sector, so rebuilding the world, for example on a sector reset, cannot bring destroyed targets
    /// back early. Kill records are not copied; they belong to the world that produced them.
    /// </summary>
    public void CarryTargetsFrom(WorldSimulation previous)
    {
        ArgumentNullException.ThrowIfNull(previous);
        if (previous.Catalog.Sector.Id != Catalog.Sector.Id || previous.Targets.Count != Targets.Count)
            throw new ArgumentException("Targets can only be carried between worlds of the same sector.", nameof(previous));
        for (var i = 0; i < Targets.Count; i++)
        {
            var from = previous.Targets[i];
            var to = Targets[i];
            if (from.Id != to.Id) throw new ArgumentException("Targets can only be carried between worlds of the same sector.", nameof(previous));
            to.Hull = from.Hull;
            to.Shield = from.Shield;
            to.LastDamagedByShipId = from.LastDamagedByShipId;
            to.DestroyedByShipId = from.DestroyedByShipId;
            to.LastDamageWeaponId = from.LastDamageWeaponId;
        }
        TargetWave = previous.TargetWave;
        TargetRespawnTick = previous.TargetRespawnTick is { } at ? TickNumber + Math.Max(1, at - previous.TickNumber) : null;
    }

    private void MovePlayer(ShipInput input)
    {
        var previous = Player.Position;
        AsteroidCollisions += ShipMotion.Step(Player, Asteroids, input);
        // Walls are server-side only for now: the shared C7 motion path (and so client prediction) does not know the map yet.
        if (Map is not null && MapCollision.MoveShip(Player, previous, Map, TickDuration)) WallCollisions++;
    }

    // Piles are visited in catalog order and the swept ship path catches piles passed between ticks. A pile the hold cannot
    // fully take keeps its remainder, so units on the ground plus units in the hold never change.
    private void CollectPickups(Vector2 previous)
    {
        for (var index = 0; index < _collectibles.Count;)
        {
            var pile = _collectibles[index];
            if (CircleCollision.Sweep(previous, Player.Position, pile.Position, Player.Radius + pile.Radius, out _))
            {
                var accepted = Cargo.Add(pile.ItemId, pile.Quantity).Accepted;
                pile.Quantity -= accepted;
                RecordPickup(pile, accepted);
            }
            if (pile.Quantity == 0)
                _collectibles.RemoveAt(index);
            else
                index++;
        }
    }

    private void TryFire()
    {
        var weapon = Player.Weapon;
        if (TickNumber < Player.NextFireTick || Player.Energy < weapon.EnergyCost)
            return;
        if (IsSafe(Player))
        {
            ShotsRefusedInSafeZone++;
            return;
        }
        Player.Energy -= weapon.EnergyCost;
        Player.NextFireTick = TickNumber + Math.Max(1, (int)MathF.Ceiling(weapon.CooldownSeconds * TickRate));
        var direction = Direction(Player.Heading);
        var position = Player.Position + direction * (Player.Radius + weapon.ProjectileRadius + 1);
        var velocity = direction * weapon.MuzzleVelocity;
        if (weapon.InheritsShipVelocity)
            velocity += Player.Velocity;
        _projectiles.Add(new ProjectileState(++_nextProjectileId, Player.Id, weapon, position, velocity));
        ShotsFired++;
    }

    private void MoveProjectiles()
    {
        for (var index = 0; index < _projectiles.Count;)
        {
            var projectile = _projectiles[index];
            var end = projectile.Position + projectile.Velocity * TickDuration;
            var hitFraction = float.PositiveInfinity;
            ShipState? hitShip = null;
            DefenseSatelliteState? hitSatellite = null;
            if (Map is not null && MapCollision.Sweep(Map, projectile.Position, end, out var wall)) hitFraction = wall;
            foreach (var asteroid in Asteroids)
            {
                if (CircleCollision.Sweep(projectile.Position, end, asteroid.Position,
                        projectile.Radius + asteroid.Radius, out var fraction) && fraction < hitFraction)
                    hitFraction = fraction;
            }
            // A ship in a safe zone cannot be hit: the shot flies on through it (RT-F065).
            foreach (var target in Targets)
            {
                // Practice targets are hit by the player's shots only, so a meteor never takes a kill from the player.
                if (!target.IsDestroyed && projectile.OwnerShipId == Player.Id && !IsSafe(target)
                    && CircleCollision.Sweep(projectile.Position, end, target.Position,
                        projectile.Radius + target.Radius, out var fraction) && fraction < hitFraction)
                {
                    hitFraction = fraction;
                    hitShip = target;
                }
            }
            // The player's shots hit enemy defense satellites.
            if (SweepSatellites(projectile, end, ref hitFraction) is { } satellite)
            {
                hitSatellite = satellite;
                hitShip = null;
            }
            // Meteors, satellite shots (and any shot the player did not fire) hit the player's ship.
            if (!Player.IsDestroyed && projectile.OwnerShipId != Player.Id && !IsSafe(Player)
                && CircleCollision.Sweep(projectile.Position, end, Player.Position, projectile.Radius + Player.Radius, out var playerHit)
                && playerHit < hitFraction)
            {
                hitFraction = playerHit;
                hitShip = Player;
                hitSatellite = null;
            }
            projectile.AgeTicks++;
            if (float.IsFinite(hitFraction))
            {
                projectile.Position = Vector2.Lerp(projectile.Position, end, hitFraction);
                if (hitShip is not null)
                    ApplyDamage(hitShip, projectile);
                else if (hitSatellite is not null)
                    DamageSatellite(hitSatellite, projectile);
                _projectiles.RemoveAt(index);
            }
            else if (projectile.AgeTicks >= projectile.LifetimeTicks || EntersDeletingTerrain(projectile.Position, end))
                _projectiles.RemoveAt(index);
            else
            {
                projectile.Position = end;
                index++;
            }
        }
    }

    // CFG DeleteLiveWeapons=1 (SpaceDock store zones and refinery/trade zones): a shot that reaches such a tile on this tick
    // disappears. The step is sampled every half tile so a fast shot cannot skip a tile (RT-F065).
    private bool EntersDeletingTerrain(Vector2 start, Vector2 end)
    {
        if (Map is null) return false;
        var steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(start, end) / (SectorMap.TileSize / 2f)));
        for (var step = 1; step <= steps; step++)
            if (Map.RuleAt(Vector2.Lerp(start, end, step / (float)steps)).DeleteLiveWeapons)
                return true;
        return false;
    }

    private void ApplyDamage(ShipState target, ProjectileState projectile)
    {
        // Armour stops part of the hull damage by type (RT-F041); the shield drain is untouched (every armour's energy-drain
        // protection, ITM f[62..63], is 0).
        var hullDamage = ShipArmor.HullDamage(projectile.Damage, projectile.DamageType, target.Definition.Defense.Armor);
        if (projectile.ShieldDrain is { } drain)
        {
            // For a ship whose energy is its shield, the shield is energy.
            var (shield, hull) = ShieldRule.Hit(target.ShieldLeft, target.Hull, drain, hullDamage);
            if (target.Definition.Defense.EnergyShield) target.Energy = shield;
            else target.Shield = shield;
            target.Hull = hull;
        }
        else
        {
            var absorbed = MathF.Min(target.Shield, projectile.Damage);
            target.Shield -= absorbed;
            target.Hull = MathF.Max(0, target.Hull - ShipArmor.HullDamage(projectile.Damage - absorbed, projectile.DamageType, target.Definition.Defense.Armor));
        }
        target.LastDamagedByShipId = projectile.OwnerShipId;
        target.LastDamageWeaponId = projectile.WeaponId;
        if (ReferenceEquals(target, Player))
        {
            // The client records the player's destruction (CommitDestruction); it is not a target kill.
            if (projectile.OwnerShipId.StartsWith(DefenseSatellitePrefix, StringComparison.Ordinal)) SatelliteHits++;
            else MeteorHits++;
            return;
        }
        if (target.IsDestroyed)
        {
            target.DestroyedByShipId = projectile.OwnerShipId;
            DestroyedTargets++;
            _kills.Add(new TargetKill(target.Id, projectile.OwnerShipId, TickNumber, TargetWave));
            if (TargetRespawnDelayTicks > 0 && Targets.All(t => t.IsDestroyed)) TargetRespawnTick = TickNumber + TargetRespawnDelayTicks;
        }
    }

    private static Vector2 Direction(float heading) => new(MathF.Cos(heading), MathF.Sin(heading));
}


