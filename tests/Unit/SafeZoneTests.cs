using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests;

/// <summary>
/// Safe zones (RT-F065): CFG [TerrainN] Safety, DeleteLiveWeapons and EnergyRate applied by the simulation. The synthetic map is a
/// NEW fixture; the Rogue checks read the derived <c>data/sectors/rogue_map.json</c>.
/// </summary>
public sealed class SafeZoneTests
{
    private static readonly int[] Expire = [720, .. Enumerable.Repeat(120, 15)];

    /// <summary>Rogue's three safe terrains: 9 repair dock (shots fly on), 10 SpaceDock store zone and 15 trade zone (shots vanish).</summary>
    private static TerrainRule[] RogueLikeRules(bool storeZoneDeletesShots = true)
    {
        var rules = Enumerable.Repeat(TerrainRule.Open, SectorMap.TerrainCount).ToArray();
        rules[9] = new TerrainRule(Safety: true, DeleteLiveWeapons: false, EnergyRate: -2000);
        rules[10] = new TerrainRule(Safety: true, DeleteLiveWeapons: storeZoneDeletesShots, EnergyRate: -2000);
        rules[15] = new TerrainRule(Safety: true, DeleteLiveWeapons: true, EnergyRate: -2000);
        return rules;
    }

    /// <summary>A 40 x 40 open map: terrain 9 in tile columns 0..9 (x 0..159), terrain 10 in columns 20..29 (x 320..479).</summary>
    private static SectorMap Fixture(TerrainRule[]? rules)
    {
        var tiles = new byte[40 * 40];
        for (var y = 0; y < 40; y++)
        for (var x = 0; x < 40; x++)
            tiles[y * 40 + x] = (byte)(x < 10 ? 9 : x is >= 20 and < 30 ? 10 : 0);
        return new SectorMap(40, 40, tiles, Expire, terrainRules: rules);
    }

    private static WorldSimulation World(SectorMap map, float x, float heading, TargetDefinition[]? targets = null, ShipDefinition? ship = null) =>
        new(TestWorld.Catalog(ship: ship, sector: new SectorDefinition
        {
            PlayerSpawn = new SpawnDefinition { X = x, Y = 320, Heading = heading },
            Targets = targets ?? [],
        }), map: map);

    [Fact]
    public void A_ShipInASafeZone_CannotFire()
    {
        var map = Fixture(RogueLikeRules());
        var docked = World(map, 80, 0);
        Assert.True(docked.PlayerInSafeZone);
        var energy = docked.Player.Energy;
        docked.Tick(new ShipInput(0, 0, true));
        Assert.Equal(0, docked.ShotsFired);
        Assert.Equal(1, docked.ShotsRefusedInSafeZone);
        Assert.Empty(docked.Projectiles);
        Assert.True(docked.Player.Energy < energy, "The refused shot costs nothing, but the zone drains energy.");

        var open = World(map, 240, 0);
        Assert.False(open.PlayerInSafeZone);
        open.Tick(new ShipInput(0, 0, true));
        Assert.Equal(1, open.ShotsFired);
        Assert.Equal(0, open.ShotsRefusedInSafeZone);
    }

    [Fact]
    public void A_ShipInASafeZone_CannotBeHit_AndTheShotFliesOn()
    {
        var target = new TargetDefinition { X = 100, Y = 320, Radius = 10 };
        var safe = World(Fixture(RogueLikeRules()), 300, MathF.PI, [target]);
        safe.Tick(new ShipInput(0, 0, true));
        TestWorld.Tick(safe, 20);
        Assert.Equal(1, safe.ShotsFired);
        Assert.Equal(40, safe.Targets[0].Shield);
        Assert.Equal(100, safe.Targets[0].Hull);
        Assert.Single(safe.Projectiles); // terrain 9 does not delete shots: it passed the target

        var open = World(Fixture(null), 300, MathF.PI, [target]);
        open.Tick(new ShipInput(0, 0, true));
        TestWorld.Tick(open, 20);
        Assert.True(open.Targets[0].Shield < 40, "Without the safe-zone rule the same shot hits.");
        Assert.Empty(open.Projectiles);
    }

    [Fact]
    public void A_ShotFlyingIntoADeleteLiveWeaponsZone_Disappears()
    {
        var target = new TargetDefinition { X = 560, Y = 320, Radius = 10 };
        var deleting = World(Fixture(RogueLikeRules()), 250, 0, [target]);
        deleting.Tick(new ShipInput(0, 0, true));
        Assert.Single(deleting.Projectiles);
        TestWorld.Tick(deleting, 8); // 700 px/s: about 82 px further, past the zone edge at x = 320
        Assert.Empty(deleting.Projectiles);
        TestWorld.Tick(deleting, 60);
        Assert.Equal(40, deleting.Targets[0].Shield);

        // The same zone without DeleteLiveWeapons lets the shot cross it and hit the target beyond.
        var crossing = World(Fixture(RogueLikeRules(storeZoneDeletesShots: false)), 250, 0, [target]);
        crossing.Tick(new ShipInput(0, 0, true));
        TestWorld.Tick(crossing, 60);
        Assert.True(crossing.Targets[0].Shield < 40);
    }

    [Fact]
    public void A_SafeZone_AddsItsEnergyRate_ToTheHullsRecharge_NeverBelowZero()
    {
        var ship = new ShipDefinition { Reactor = new ShipReactor { MaximumEnergy = 1700, EnergyRegenerationPerSecond = 105 } };
        var map = Fixture(RogueLikeRules());
        var docked = World(map, 80, 0, ship: ship);
        Assert.Equal(1700, docked.Player.Energy);
        TestWorld.Tick(docked, WorldSimulation.TickRate);
        Assert.Equal(1700 - 95, docked.Player.Energy, 1); // 105 recharge - 200 drain per second
        TestWorld.Tick(docked, 20 * WorldSimulation.TickRate);
        Assert.Equal(0, docked.Player.Energy);

        var open = World(map, 240, 0, ship: ship);
        TestWorld.Tick(open, WorldSimulation.TickRate);
        Assert.Equal(1700, open.Player.Energy);
    }

    [Fact]
    public void A_MapWithoutRules_IsOpenSpace_AndBadRuleListsAreRefused()
    {
        var map = Fixture(null);
        Assert.All(map.TerrainRules, rule => Assert.Equal(TerrainRule.Open, rule));
        Assert.False(map.IsSafeAt(new Vector2(80, 320)));
        Assert.Throws<ArgumentException>(() => new SectorMap(1, 1, [0], Expire, terrainRules: new TerrainRule[15]));
        Assert.Throws<ArgumentException>(() => new SectorMap(1, 1, [0], Expire, terrainRules: new TerrainRule[16]));
    }

    [Fact]
    public void TheRogueMap_CarriesRogueCfgSafeZones()
    {
        var map = TestWorld.RogueMap;
        for (var terrain = 0; terrain < SectorMap.TerrainCount; terrain++)
        {
            var rule = map.TerrainRules[terrain];
            var safe = terrain is 9 or 10 or 15;
            Assert.Equal(safe, rule.Safety);
            Assert.Equal(terrain is 10 or 15, rule.DeleteLiveWeapons);
            Assert.Equal(safe ? -2000 : 0, rule.EnergyRate);
        }
        // Every team spawns in its SpaceDock store zone, so a new pilot starts safe.
        var sector = TestWorld.RogueCatalog().Sector;
        Assert.All(sector.Teams, team => Assert.True(map.IsSafeAt(new Vector2(team.Spawn.X, team.Spawn.Y))));
        // The NEW practice range stays in open space, so it can still be fought over.
        Assert.False(map.IsSafeAt(new Vector2(RogueTrader.Trading.OriginalMapBuilder.PracticeRangeX, RogueTrader.Trading.OriginalMapBuilder.PracticeRangeY)));
        Assert.All(sector.Targets, target => Assert.False(map.IsSafeAt(new Vector2(target.X, target.Y))));
    }
}
