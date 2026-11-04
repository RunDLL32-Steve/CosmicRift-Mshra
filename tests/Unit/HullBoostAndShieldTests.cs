using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// The per-hull values that replaced the placeholders: the Shift boost (original hyper thrust), the original shield rule (a hit
/// drains the shield and does no hull damage while any shield is left) and each hull's own derived stats and starting weapon.
/// </summary>
public sealed class HullBoostAndShieldTests
{
    private static readonly ShipHyper Boost = new()
    {
        ForwardThrust = 30000, ReverseThrust = 20000, MaximumSpeed = 500, EnergyPerSecond = 60, RechargeWhileEngaged = false,
    };

    private static WorldSimulation BoostWorld(ShipHyper? hyper = null, float energy = 100, float regen = 30) =>
        new(TestWorld.Catalog(ship: new ShipDefinition
        {
            Physics = new ShipPhysics { ForwardThrust = 12000, ReverseThrust = 12000, MaximumSpeed = 360 },
            Reactor = new ShipReactor { MaximumEnergy = energy, EnergyRegenerationPerSecond = regen },
            Hyper = hyper ?? Boost,
        }, sector: new SectorDefinition { PlayerSpawn = new SpawnDefinition { Heading = 0 } }));

    [Fact]
    public void ShiftWithThrust_UsesTheBoostThrustAndPaysItsEnergyWithoutRecharging()
    {
        var world = BoostWorld();
        world.Tick(new ShipInput(1, 0, false, Hyper: true));
        Assert.True(world.Player.IsHyperEngaged);
        TestWorld.Near(new Vector2(30000 / 100f / 60, 0), world.Player.Velocity);
        Assert.Equal(100 - 1f, world.Player.Energy, 3); // 60/s for one tick, and no recharge while engaged
        // Reverse uses the boost's reverse thrust.
        var reverse = BoostWorld();
        reverse.Tick(new ShipInput(-1, 0, false, Hyper: true));
        TestWorld.Near(new Vector2(-20000 / 100f / 60, 0), reverse.Player.Velocity);
    }

    [Fact]
    public void ShiftWithoutThrust_DoesNothingAndCostsNothing()
    {
        var world = BoostWorld();
        TestWorld.Tick(world, 30, new ShipInput(0, 0, false, Hyper: true));
        Assert.False(world.Player.IsHyperEngaged);
        Assert.Equal(Vector2.Zero, world.Player.Velocity);
        Assert.Equal(100, world.Player.Energy);
    }

    [Fact]
    public void TheBoostRaisesTopSpeed_AndReleasingShiftClampsBackToTheNormalTop()
    {
        var world = BoostWorld(energy: 100_000);
        TestWorld.Tick(world, 600, new ShipInput(1, 0, false, Hyper: true));
        Assert.Equal(500, world.Player.Velocity.Length(), 2);
        world.Tick(new ShipInput(1, 0, false));
        Assert.False(world.Player.IsHyperEngaged);
        Assert.Equal(360, world.Player.Velocity.Length(), 2);
    }

    [Fact]
    public void AnEmptyReactorCannotBoost_AndEnergyNeverGoesNegative()
    {
        // 2 energy pays for exactly two ticks at 60/s; after that the ship thrusts on its normal values.
        var world = BoostWorld(energy: 2, regen: 0);
        var engaged = 0;
        for (var i = 0; i < 10; i++)
        {
            world.Tick(new ShipInput(1, 0, false, Hyper: true));
            if (world.Player.IsHyperEngaged) engaged++;
        }
        Assert.Equal(2, engaged);
        Assert.False(world.Player.IsHyperEngaged);
        Assert.Equal(0, world.Player.Energy, 4);
        Assert.True(world.Player.Velocity.X > 0);
    }

    [Fact]
    public void WithRechargeWhileEngaged_TheCostComesOutOfARunningRecharge()
    {
        var world = BoostWorld(Boost with { RechargeWhileEngaged = true }, energy: 100, regen: 30);
        world.Tick(new ShipInput(0, 0, true)); // spend 6 so the recharge has room
        var before = world.Player.Energy;
        world.Tick(new ShipInput(1, 0, false, Hyper: true));
        Assert.Equal(before - 1f + 0.5f, world.Player.Energy, 3);
    }

    [Fact]
    public void AHullWithNoBoost_IgnoresShift()
    {
        var world = new WorldSimulation(TestWorld.Catalog());
        world.Tick(new ShipInput(1, 0, false, Hyper: true));
        Assert.False(world.Player.IsHyperEngaged);
        Assert.Equal(360, new ShipDefinition().Physics.MaximumSpeed);
    }

    [Fact]
    public void OriginalShieldRule_AnyShieldLeftTakesTheDrainAndTheHullTakesNothing()
    {
        // Two shots: the first drains 266 of a 40-point shield (no hull damage), the second finds no shield and does 5 to the hull.
        var world = new WorldSimulation(TestWorld.Catalog(
            weapon: new WeaponDefinition { MuzzleVelocity = 6000, CooldownSeconds = 1f / 60, Damage = 5, ShieldDrain = 266 },
            sector: new SectorDefinition
            {
                PlayerSpawn = new SpawnDefinition { Heading = 0 },
                Targets = [new TargetDefinition { X = 100, Y = 0, Hull = 100, Shield = 40 }],
            }));
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(0, world.Targets[0].Shield);
        Assert.Equal(100, world.Targets[0].Hull);
        world.Tick(new ShipInput(0, 0, true));
        Assert.Equal(95, world.Targets[0].Hull);
    }

    [Fact]
    public void AnEnergyShieldedHullShowsItsEnergyAsItsShield_AndCannotAlsoHaveAShieldPool()
    {
        var ship = new ShipDefinition { Defense = new ShipDefense { MaximumShield = 0, EnergyShield = true } };
        var world = new WorldSimulation(TestWorld.Catalog(ship: ship));
        Assert.Equal(world.Player.Energy, world.Player.ShieldLeft);
        Assert.Throws<ArgumentException>(() => TestWorld.Catalog(ship: ship with { Defense = new ShipDefense { MaximumShield = 10, EnergyShield = true } }));
        Assert.Throws<ArgumentException>(() => TestWorld.Catalog(ship: ship with { WeaponId = "1041" }));
    }

    [Fact]
    public void ReplayRecordsTheBoost()
    {
        var world = BoostWorld();
        var recorder = new RogueTrader.Simulation.Replay.SimulationRecorder(world);
        recorder.Tick(new ShipInput(1, 0, false, Hyper: true));
        var copy = RogueTrader.Simulation.Replay.SimulationRecording.FromBytes(recorder.Finish().ToBytes());
        Assert.True(copy.Inputs[0].Hyper);
    }

    // ---- Derived originals ----

    private static OriginalHullCatalog Hulls => OriginalHullCatalog.Load(OriginalAssets.Root!);
    private static OriginalItemCatalog Items => OriginalItemCatalog.Load(OriginalAssets.Root!);

    [RequiresOriginalAssetsFact]
    public void EveryHullHasItsOriginalRadiusEnergyAndEnergyShield()
    {
        foreach (var hull in Hulls.Hulls)
        {
            var ship = OriginalShipBuilder.Ship(hull, Items);
            Assert.Equal(21, ship.Radius);
            Assert.Equal(1700, ship.Reactor.MaximumEnergy);
            Assert.Equal(105, ship.Reactor.EnergyRegenerationPerSecond);
            Assert.True(ship.Defense.EnergyShield);
            Assert.Equal(0, ship.Defense.MaximumShield);
        }
    }

    [RequiresOriginalAssetsFact]
    public void EachRaceStartsWithItsOwnCannon()
    {
        var expected = new Dictionary<string, string>
        {
            ["Ship Hull - Terran"] = "Plasma Cannon", ["Ship Hull - Skithzar"] = "Bio Cannon", ["Ship Hull - Grazi"] = "D-Cannon",
        };
        foreach (var hull in Hulls.Hulls)
        {
            var weapon = OriginalWeaponBuilder.StartingWeapon(hull, Items);
            Assert.Equal(expected[hull.Category], weapon.Name);
            Assert.Equal(OriginalWeaponBuilder.WeaponId(weapon), OriginalShipBuilder.Ship(hull, Items).WeaponId);
        }
        // The three cannons share their combat numbers; they differ in name, race, price, colour and sound.
        Assert.All(OriginalWeaponBuilder.StartingWeapons(Hulls, Items).Select(OriginalWeaponBuilder.Weapon), w =>
        {
            Assert.Equal(60, w.EnergyCost);
            Assert.Equal(0.25f, w.CooldownSeconds);
            Assert.Equal(266, w.ShieldDrain);
            Assert.Equal(5, w.Damage);
        });
    }

    [RequiresOriginalAssetsFact]
    public void EachHullHasItsOwnBoost()
    {
        Assert.All(Hulls.Hulls, h => Assert.False(OriginalShipBuilder.Ship(h, Items).Hyper!.RechargeWhileEngaged));
        (float Speed, float Cost, float Thrust) Of(string name)
        {
            var h = OriginalShipBuilder.Ship(Hulls.ByName(name), Items).Hyper!;
            return (h.MaximumSpeed, h.EnergyPerSecond, h.ForwardThrust);
        }
        Assert.Equal((350f, 110f, 18750f), Of("Asp"));        // HyperTopSpeed 3500, HyperEnergyCost 1100, HyperThrust 50
        Assert.Equal((375f, 120f, 22500f), Of("Mastakith"));  // 3750, 1200, 60
        Assert.Equal((360f, 99f, 18750f), Of("Skithist"));    // 3600, 990, 50
        Assert.Equal((380f, 110f, 18750f), Of("Excelsior"));  // 3800
        Assert.Equal((350f, 110f, 20620f), Of("Hornet"));     // HyperThrust 55 (integer thrust factor: 2062)
        // The boost is always faster than the hull's normal top speed.
        Assert.All(Hulls.Hulls, h =>
        {
            var ship = OriginalShipBuilder.Ship(h, Items);
            Assert.True(ship.Hyper!.MaximumSpeed > ship.Physics.MaximumSpeed, h.Name);
        });
    }

    [RequiresOriginalAssetsFact]
    public void ShippedHullsLoadWithTheirOwnWeaponFiles()
    {
        var data = Path.Combine(AppContext.BaseDirectory, "data");
        var roster = ShipRoster.FromJson(File.ReadAllText(Path.Combine(data, "ships", "roster.json")));
        var sector = File.ReadAllText(Path.Combine(data, "sectors", "rogue.json"));
        var flown = new HashSet<string>();
        foreach (var entry in roster.Ships)
        {
            var shipJson = File.ReadAllText(Path.Combine(data, "ships", entry.Definition));
            var weaponFile = roster.WeaponDefinitionFor(GameCatalog.ShipFromJson(shipJson));
            var catalog = GameCatalog.FromJson(shipJson, File.ReadAllText(Path.Combine(data, "weapons", weaponFile)), sector);
            flown.Add(catalog.Weapon.Name);
        }
        Assert.Equal(new[] { "Bio Cannon", "D-Cannon", "Plasma Cannon", "Plasma cannon" }, flown.Order(StringComparer.Ordinal));
    }
}
