using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// Queue G10 rows W2 and W3: the original cannons' muzzleVelocity and aliveTime read in the emulator's units
/// (<see cref="OriginalShipPhysics.MuzzleVelocityPixelsPerSecond"/>, <see cref="OriginalShipPhysics.LifetimeSeconds"/>), and
/// the shots the game actually fires from the derived Asp and Plasma Cannon, measured in <see cref="WorldSimulation"/>.
/// </summary>
public sealed class G10ProjectileUnitTests
{
    private static readonly Lazy<OriginalItemCatalog> Items = new(() => OriginalItemCatalog.Load(OriginalAssets.Root!));

    private static string Data(params string[] parts) => File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "data", .. parts]));

    /// <summary>The ship a new pilot flies (the derived Asp) with its starting weapon, in an empty sector.</summary>
    private static WorldSimulation OriginalAspInOpenSpace()
    {
        var shipped = GameCatalog.FromJson(
            Data("ships", "original_asp.json"), Data("weapons", "original_plasma_cannon.json"), Data("sectors", "first_contact.json"));
        return new WorldSimulation(TestWorld.Catalog(shipped.Ship, shipped.Weapon));
    }

    private static ProjectileState FireOnce(WorldSimulation world)
    {
        var before = world.Projectiles.Count;
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(before + 1, world.Projectiles.Count);
        return world.Projectiles[^1];
    }

    internal static double ObservedShotSpeedFromRest()
    {
        var world = OriginalAspInOpenSpace();
        return FireOnce(world).Velocity.Length();
    }

    internal static double ObservedShotLifetimeSeconds(out double rangeFromRest)
    {
        var world = OriginalAspInOpenSpace();
        var shot = FireOnce(world);
        var start = shot.Position - shot.Velocity * WorldSimulation.TickDuration; // where it was fired, before its first move
        var last = shot.Position;
        var ticks = 1;
        while (world.Projectiles.Any(p => p.Id == shot.Id) && ticks < 60 * 60)
        {
            last = world.Projectiles.Single(p => p.Id == shot.Id).Position;
            world.Tick(default);
            ticks++;
        }
        rangeFromRest = Vector2.Distance(start, last);
        return ticks / (double)WorldSimulation.TickRate;
    }

    [Fact]
    public void TheAspFiresTheOriginalPlasmaCannonShot()
    {
        Assert.Equal(220, ObservedShotSpeedFromRest(), 3);
        Assert.Equal(5.5, ObservedShotLifetimeSeconds(out var range), 3);
        // 220 px/s for 5.5 s is 1210 px, about 76 map tiles of 16 px. The simulation removes a shot on its 330th tick instead
        // of moving it, so the last position it is seen at is one tick (3.7 px) short of that.
        Assert.Equal(220 * 329 / 60.0, range, 2);
    }

    [Fact]
    public void AShotAtTopSpeedAddsTheShipsVelocity()
    {
        // W4: inheritedSpeed 1000, read as per mille, adds the whole ship velocity. A shot fired straight ahead at the Asp's top
        // speed flies 325 + 220 = 545 px/s; one fired from rest by a pilot who is chased at top speed never catches up.
        var world = OriginalAspInOpenSpace();
        TestWorld.Tick(world, 60 * 5, new ShipInput(1, 0, false));
        Assert.Equal(325, world.Player.Velocity.Length(), 2);
        Assert.Equal(545, FireOnce(world).Velocity.Length(), 1);
    }

    [RequiresOriginalAssetsTheory]
    [InlineData(1004, "Plasma Cannon", 220.0, 5.5)]
    [InlineData(1036, "Bio Cannon", 220.0, 5.5)]
    [InlineData(1041, "D-Cannon", 220.0, 5.5)]
    public void StartingCannonsReadInTheEmulatorUnits(int id, string name, double pixelsPerSecond, double seconds)
    {
        var projectile = OriginalProjectile.Of(Items.Value.ById(id));
        Assert.Equal(name, projectile.Item.Name);
        Assert.Equal(pixelsPerSecond, OriginalShipPhysics.MuzzleVelocityPixelsPerSecond(projectile), 3);
        Assert.Equal(seconds, OriginalShipPhysics.LifetimeSeconds(projectile), 3);
        var weapon = OriginalWeaponBuilder.Weapon(Items.Value.ById(id));
        Assert.Equal(pixelsPerSecond, weapon.MuzzleVelocity, 3);
        Assert.Equal(seconds, weapon.LifetimeSeconds, 3);
    }

    [RequiresOriginalAssetsTheory]
    [InlineData("Mass Driver", 230.0, 5.5)]
    [InlineData("Meson Cannon", 220.0, 7.5)]
    [InlineData("Laser Cannon", 400.0, 5.5)]
    [InlineData("Plasma Bomb", 210.0, 20.0)]
    [InlineData("Meteor", 425.0, 20.0)]
    public void OtherProjectilesReadTheSameWay(string name, double pixelsPerSecond, double seconds)
    {
        var projectile = OriginalProjectile.Of(Items.Value.Items.Single(i => i.Name == name));
        Assert.Equal(pixelsPerSecond, OriginalShipPhysics.MuzzleVelocityPixelsPerSecond(projectile), 3);
        Assert.Equal(seconds, OriginalShipPhysics.LifetimeSeconds(projectile), 3);
    }

    [RequiresOriginalAssetsFact]
    public void SentinelsAndStillProjectilesAreRefusedNotRead()
    {
        // aliveTime -65 (MIRV) and -2000 (rockets) are negative sentinels with no known meaning; a mine has muzzle 0.
        var mirv = OriginalProjectile.Of(Items.Value.Items.Single(i => i.Name == "MIRV Missile Launcher"));
        Assert.True(mirv.AliveTime.Value < 0);
        Assert.Throws<OriginalDataException>(() => OriginalShipPhysics.LifetimeSeconds(mirv));
        var mine = OriginalProjectile.Of(Items.Value.Items.Single(i => i.Name == "Plasma Mine"));
        Assert.Equal(60, OriginalShipPhysics.LifetimeSeconds(mine), 3); // every space mine: 6000 = 60 s
        Assert.Throws<OriginalDataException>(() => OriginalShipPhysics.MuzzleVelocityPixelsPerSecond(mine));
    }

    [RequiresOriginalAssetsFact]
    public void ProjectileAndFlightReadingsShareOneScale()
    {
        // The emulator's lead-aim code holds ship and projectile velocities in the same thousandths of a pixel per 10 ms, so a
        // raw muzzle of 3250 must fly exactly as fast as the Asp's RollTopSpeed of 3250 (G10 F1).
        var hulls = OriginalHullCatalog.Load(OriginalAssets.Root!);
        var asp = EmulatorFlightReading.From(hulls.ByName("Asp"));
        Assert.Equal(3250 / OriginalShipPhysics.MuzzleUnitsPerPixelPerSecond, asp.TopSpeedPixelsPerSecond, 6);
    }
}
