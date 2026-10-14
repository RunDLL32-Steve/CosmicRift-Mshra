using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests;

public sealed class CombatTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ProjectileInheritsVelocityOnlyWhenConfigured(bool inherits)
    {
        var world = new WorldSimulation(TestWorld.Catalog(
            weapon: new WeaponDefinition { InheritsShipVelocity = inherits },
            sector: new SectorDefinition { PlayerSpawn = new SpawnDefinition { VelocityX = 45 } }));
        world.Tick(new ShipInput(0, 0, true));
        var projectile = Assert.Single(world.Projectiles);
        TestWorld.Near(new Vector2(inherits ? 45 : 0, -700), projectile.Velocity);
        Assert.Equal(world.Player.Id, projectile.OwnerShipId);
        Assert.Equal(world.Catalog.Weapon.Id, projectile.WeaponId);
    }

    [Fact]
    public void WeaponCannotFireBeforeCooldownAndConsumesEnergyOncePerShot()
    {
        var world = new WorldSimulation(TestWorld.Catalog(ship: new ShipDefinition
        {
            Reactor = new ShipReactor { EnergyRegenerationPerSecond = 0 }
        }));
        TestWorld.Tick(world, 12, new ShipInput(0, 0, true));
        Assert.Equal(1, world.ShotsFired);
        Assert.Equal(94, world.Player.Energy);
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(2, world.ShotsFired);
        Assert.Equal(88, world.Player.Energy);
    }

    [Fact]
    public void InsufficientEnergyBlocksFireAndNeverMakesEnergyNegative()
    {
        var world = new WorldSimulation(TestWorld.Catalog(ship: new ShipDefinition
        {
            Reactor = new ShipReactor { MaximumEnergy = 18, EnergyRegenerationPerSecond = 0 }
        }));
        TestWorld.Tick(world, 120, new ShipInput(0, 0, true));
        Assert.Equal(3, world.ShotsFired);
        Assert.Equal(0, world.Player.Energy);
    }

    [Fact]
    public void EnergyRegeneratesToMaximum()
    {
        var world = new WorldSimulation(TestWorld.Catalog());
        world.Tick(new ShipInput(0, 0, true));
        Assert.True(world.Player.Energy < 100);
        TestWorld.Tick(world, 60);
        Assert.Equal(100, world.Player.Energy);
    }

    [Fact]
    public void ProjectileExpiresAtConfiguredLifetime()
    {
        var world = new WorldSimulation(TestWorld.Catalog(weapon: new WeaponDefinition { LifetimeSeconds = 0.05f }));
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(1, Assert.Single(world.Projectiles).AgeTicks);
        world.Tick(default);
        Assert.Equal(2, Assert.Single(world.Projectiles).AgeTicks);
        world.Tick(default);
        Assert.Empty(world.Projectiles);
    }

    [Fact]
    public void ShieldsAbsorbDamageBeforeHullAndExcessDamageCarriesThrough()
    {
        var world = AimedWorld(new WeaponDefinition { MuzzleVelocity = 6000, CooldownSeconds = 1f / 60 });
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(12, world.Targets[0].Shield);
        Assert.Equal(100, world.Targets[0].Hull);
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(0, world.Targets[0].Shield);
        Assert.Equal(84, world.Targets[0].Hull);
        Assert.Equal("player", world.Targets[0].LastDamagedByShipId);
        Assert.Equal("plasma_cannon", world.Targets[0].LastDamageWeaponId);
    }

    [Fact]
    public void DestructionIsAttributedAndCountedOnceWhileTargetStaysStationary()
    {
        var world = AimedWorld(new WeaponDefinition { MuzzleVelocity = 6000, Damage = 200 });
        var position = world.Targets[0].Position;
        world.Tick(new ShipInput(0, 0, true));
        Assert.True(world.Targets[0].IsDestroyed);
        Assert.Equal(0, world.Targets[0].Hull);
        Assert.Equal(0, world.Targets[0].Shield);
        Assert.Equal("player", world.Targets[0].DestroyedByShipId);
        Assert.Equal(1, world.DestroyedTargets);
        TestWorld.Tick(world, 60, new ShipInput(0, 0, true));
        Assert.Equal(1, world.DestroyedTargets);
        Assert.Equal(position, world.Targets[0].Position);
        Assert.Equal(Vector2.Zero, world.Targets[0].Velocity);
        Assert.Equal(100, world.Player.Hull);
        Assert.Equal(50, world.Player.Shield);
    }

    [Fact]
    public void SweptProjectileHitsSmallTargetBetweenTickPositions()
    {
        var world = AimedWorld(new WeaponDefinition { MuzzleVelocity = 12000, ProjectileRadius = 1, Damage = 200 },
            [new TargetDefinition { X = 80, Y = 0, Radius = 2 }]);
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(1, world.DestroyedTargets);
        Assert.Empty(world.Projectiles);
    }

    [Fact]
    public void SweptCollisionDamagesNearestTargetRatherThanListOrder()
    {
        var world = AimedWorld(new WeaponDefinition { MuzzleVelocity = 12000, Damage = 200 },
            [new TargetDefinition { Id = "far", X = 150, Y = 0, Radius = 5 },
             new TargetDefinition { Id = "near", X = 70, Y = 0, Radius = 5 }]);
        world.Tick(new ShipInput(0, 0, true));
        Assert.False(world.Targets[0].IsDestroyed);
        Assert.True(world.Targets[1].IsDestroyed);
        Assert.Equal(1, world.DestroyedTargets);
    }

    [Fact]
    public void AsteroidBlocksProjectileBeforeTarget()
    {
        var world = AimedWorld(new WeaponDefinition { MuzzleVelocity = 12000, Damage = 200 },
            asteroids: [new AsteroidDefinition { X = 50, Radius = 10 }]);
        world.Tick(new ShipInput(0, 0, true));
        Assert.False(world.Targets[0].IsDestroyed);
        Assert.Equal(40, world.Targets[0].Shield);
        Assert.Empty(world.Projectiles);
    }

    [Fact]
    public void ForwardTargetInExternalSectorCanBeDestroyedWithStartingWeapon()
    {
        var world = new WorldSimulation(TestWorld.ExternalCatalog());
        TestWorld.Tick(world, 120, new ShipInput(0, 0, true));
        Assert.True(world.Targets[0].IsDestroyed);
        Assert.Equal(1, world.DestroyedTargets);
        Assert.Equal("player", world.Targets[0].DestroyedByShipId);
    }

    private static WorldSimulation AimedWorld(WeaponDefinition weapon,
        IReadOnlyList<TargetDefinition>? targets = null, IReadOnlyList<AsteroidDefinition>? asteroids = null) =>
        new(TestWorld.Catalog(weapon: weapon, sector: new SectorDefinition
        {
            PlayerSpawn = new SpawnDefinition { Heading = 0 },
            Targets = targets ?? [new TargetDefinition { X = 100, Y = 0 }],
            Asteroids = asteroids ?? []
        }));
}
