using System.Numerics;
using RogueTrader.GameData;

namespace RogueTrader.Simulation;

/// <summary>Pure C# world authority. One Tick always advances exactly 1/60 of a second.</summary>
public sealed class WorldSimulation
{
    public const int TickRate = 60;
    public const float TickDuration = 1f / TickRate;
    private readonly List<ProjectileState> _projectiles = [];
    private readonly List<CollectibleState> _collectibles;
    private long _nextProjectileId;

    public GameCatalog Catalog { get; }
    public ShipState Player { get; }
    public IReadOnlyList<ShipState> Targets { get; }
    public IReadOnlyList<AsteroidState> Asteroids { get; }
    public IReadOnlyList<ProjectileState> Projectiles { get; }
    /// <summary>Piles still lying in space, in catalog order.</summary>
    public IReadOnlyList<CollectibleState> Collectibles { get; }
    public CargoHold Cargo { get; }
    public long TickNumber { get; private set; }
    public int DestroyedTargets { get; private set; }
    public int AsteroidCollisions { get; private set; }
    public long ShotsFired { get; private set; }

    /// <param name="cargo">Cargo rules and collectible piles; omitted means no cargo space and no piles.</param>
    /// <param name="initialCargo">Starting contents of the player's hold (for example a persisted inventory).</param>
    public WorldSimulation(GameCatalog catalog, CargoCatalog? cargo = null,
        IEnumerable<KeyValuePair<string, int>>? initialCargo = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Catalog = catalog;
        cargo ??= new CargoCatalog(0, []);
        Cargo = new CargoHold(cargo, initialCargo);
        _collectibles = cargo.Collectibles.Select(spawn => new CollectibleState(spawn)).ToList();
        Collectibles = _collectibles.AsReadOnly();
        var spawn = catalog.Sector.PlayerSpawn;
        Player = new ShipState("player", catalog.Ship, catalog.Weapon, new Vector2(spawn.X, spawn.Y), spawn.Heading)
        {
            Velocity = ClampSpeed(new Vector2(spawn.VelocityX, spawn.VelocityY), catalog.Ship.Physics.MaximumSpeed)
        };
        Targets = Array.AsReadOnly(catalog.Sector.Targets.Select(target => new ShipState(
            target.Id, catalog.Ship with
            {
                Radius = target.Radius,
                Defense = new ShipDefense { MaximumHull = target.Hull, MaximumShield = target.Shield }
            }, catalog.Weapon, new Vector2(target.X, target.Y), target.Heading)).ToArray());
        Asteroids = Array.AsReadOnly(catalog.Sector.Asteroids.Select(asteroid => new AsteroidState(asteroid)).ToArray());
        Projectiles = _projectiles.AsReadOnly();
    }

    public void Tick(ShipInput input)
    {
        TickNumber++;
        if (!Player.IsDestroyed)
        {
            var previous = Player.Position;
            MovePlayer(input);
            CollectPickups(previous);
            Player.Energy = MathF.Min(Player.Definition.Reactor.MaximumEnergy,
                Player.Energy + Player.Definition.Reactor.EnergyRegenerationPerSecond * TickDuration);
            if (input.Fire)
                TryFire();
        }
        MoveProjectiles();
    }

    private void MovePlayer(ShipInput input)
    {
        var turn = float.IsFinite(input.Turn) ? Math.Clamp(input.Turn, -1, 1) : 0;
        var thrust = float.IsFinite(input.Thrust) ? Math.Clamp(input.Thrust, -1, 1) : 0;
        var physics = Player.Definition.Physics;
        Player.Heading = MathF.IEEERemainder(Player.Heading + turn * physics.RotationSpeed * TickDuration, MathF.Tau);
        var force = thrust >= 0 ? thrust * physics.ForwardThrust : thrust * physics.ReverseThrust;
        Player.Velocity = ClampSpeed(Player.Velocity + Direction(Player.Heading) * (force / physics.Mass * TickDuration),
            physics.MaximumSpeed);
        AsteroidCollisions += CircleCollision.MoveShip(Player, Asteroids, TickDuration);
    }

    // Piles are visited in catalog order and the swept ship path catches piles passed between ticks. A pile the hold cannot
    // fully take keeps its remainder, so units on the ground plus units in the hold never change.
    private void CollectPickups(Vector2 previous)
    {
        for (var index = 0; index < _collectibles.Count;)
        {
            var pile = _collectibles[index];
            if (CircleCollision.Sweep(previous, Player.Position, pile.Position, Player.Radius + pile.Radius, out _))
                pile.Quantity -= Cargo.Add(pile.ItemId, pile.Quantity).Accepted;
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
        Player.Energy -= weapon.EnergyCost;
        Player.NextFireTick = TickNumber + Math.Max(1, (int)MathF.Ceiling(weapon.CooldownSeconds * TickRate));
        var direction = Direction(Player.Heading);
        var position = Player.Position + direction * (Player.Radius + weapon.ProjectileRadius + 1);
        var velocity = direction * weapon.MuzzleVelocity;
        if (weapon.InheritsShipVelocity)
            velocity += Player.Velocity;
        _projectiles.Add(new ProjectileState(++_nextProjectileId, Player, position, velocity));
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
            foreach (var asteroid in Asteroids)
            {
                if (CircleCollision.Sweep(projectile.Position, end, asteroid.Position,
                        projectile.Radius + asteroid.Radius, out var fraction) && fraction < hitFraction)
                    hitFraction = fraction;
            }
            foreach (var target in Targets)
            {
                if (!target.IsDestroyed && target.Id != projectile.OwnerShipId
                    && CircleCollision.Sweep(projectile.Position, end, target.Position,
                        projectile.Radius + target.Radius, out var fraction) && fraction < hitFraction)
                {
                    hitFraction = fraction;
                    hitShip = target;
                }
            }
            projectile.AgeTicks++;
            if (float.IsFinite(hitFraction))
            {
                projectile.Position = Vector2.Lerp(projectile.Position, end, hitFraction);
                if (hitShip is not null)
                    ApplyDamage(hitShip, projectile);
                _projectiles.RemoveAt(index);
            }
            else if (projectile.AgeTicks >= projectile.LifetimeTicks)
                _projectiles.RemoveAt(index);
            else
            {
                projectile.Position = end;
                index++;
            }
        }
    }

    private void ApplyDamage(ShipState target, ProjectileState projectile)
    {
        var absorbed = MathF.Min(target.Shield, projectile.Damage);
        target.Shield -= absorbed;
        target.Hull = MathF.Max(0, target.Hull - (projectile.Damage - absorbed));
        target.LastDamagedByShipId = projectile.OwnerShipId;
        target.LastDamageWeaponId = projectile.WeaponId;
        if (target.IsDestroyed)
        {
            target.DestroyedByShipId = projectile.OwnerShipId;
            DestroyedTargets++;
        }
    }

    private static Vector2 Direction(float heading) => new(MathF.Cos(heading), MathF.Sin(heading));

    private static Vector2 ClampSpeed(Vector2 velocity, float maximumSpeed) =>
        velocity.LengthSquared() > maximumSpeed * maximumSpeed
            ? Vector2.Normalize(velocity) * maximumSpeed
            : velocity;
}
