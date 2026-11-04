using System.Numerics;
using System.Text.Json;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.UnitTests;

/// <summary>
/// Walls and terrain in the simulation (<see cref="SectorMap"/>, <see cref="MapCollision"/>). Synthetic maps are NEW fixtures;
/// the Rogue checks use the derived <c>data/sectors/rogue_map.json</c>. The default test ship starts at (0, 0) heading up.
/// </summary>
public sealed class SectorMapTests
{
    private const string Ore = "fixture-ore";
    private static readonly int[] Expire = [720, .. Enumerable.Repeat(120, 15)];

    /// <summary>A 40 x 40 open map with a wall column at tile x = 30 and terrain 10 in the 4 x 4 tiles from (0, 0).</summary>
    private static SectorMap Fixture()
    {
        var tiles = new byte[40 * 40];
        for (var y = 0; y < 40; y++) tiles[y * 40 + 30] = SectorMap.BlockedBit;
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++) tiles[y * 40 + x] = 10;
        return new SectorMap(40, 40, tiles, Expire);
    }

    private static GameCatalog Catalog(float x = 320, float y = 320, float heading = 0) =>
        TestWorld.Catalog(sector: new SectorDefinition { PlayerSpawn = new SpawnDefinition { X = x, Y = y, Heading = heading } });

    private static WorldSimulation World(SectorMap map, float x = 320, float y = 320, float heading = 0, MineralFieldCatalog? minerals = null) =>
        new(Catalog(x, y, heading), new CargoCatalog(1000, [new(Ore, 1)]), minerals: minerals, map: map);

    // ---- Tile data -----------------------------------------------------------------------------------------------------

    [Fact]
    public void Tiles_RoundTripThroughTheRunLengthEncoding()
    {
        var tiles = new byte[1000];
        for (var i = 0; i < tiles.Length; i++) tiles[i] = (byte)(i < 300 ? 0 : i < 301 ? 0x1F : i % 7 == 0 ? 9 : 0x10);
        var encoded = SectorMap.EncodeTiles(tiles);
        Assert.Equal(tiles, SectorMap.DecodeTiles(encoded, tiles.Length));
        Assert.Equal(new byte[] { 0xAC, 0x02, 0 }, encoded[..3]); // 300 as LEB128, then the tile byte
        Assert.Throws<JsonException>(() => SectorMap.DecodeTiles(encoded, tiles.Length - 1));
        Assert.Throws<JsonException>(() => SectorMap.DecodeTiles(encoded, tiles.Length + 1));
        Assert.Throws<JsonException>(() => SectorMap.DecodeTiles([0, 1], 4)); // empty run
        Assert.Throws<JsonException>(() => SectorMap.DecodeTiles([0x80], 4)); // ends inside a length
    }

    [Fact]
    public void Map_RejectsBadShapes()
    {
        Assert.Throws<ArgumentException>(() => new SectorMap(2, 2, new byte[3], Expire));
        Assert.Throws<ArgumentException>(() => new SectorMap(2, 2, new byte[4], [720]));
        Assert.Throws<ArgumentException>(() => new SectorMap(1, 1, [0x20], Expire));
    }

    [Fact]
    public void Lookups_UseSixteenPixelTiles_AndEverythingOutsideBlocks()
    {
        var map = Fixture();
        Assert.True(map.IsBlocked(new Vector2(480, 5)) && map.IsBlocked(new Vector2(495.9f, 600)));
        Assert.False(map.IsBlocked(new Vector2(479.9f, 5)) || map.IsBlocked(new Vector2(496, 5)));
        Assert.True(map.IsBlocked(new Vector2(-0.1f, 5)) && map.IsBlocked(new Vector2(5, 640)) && map.IsBlocked(new Vector2(float.NaN, 5)));
        Assert.Equal((10, 0, 0), (map.TerrainAt(new Vector2(63, 63)), map.TerrainAt(new Vector2(64, 10)), map.TerrainAt(new Vector2(-50, -50))));
        Assert.Equal((120L * 60, 720L * 60), (map.PrizeExpireTicksAt(new Vector2(1, 1)), map.PrizeExpireTicksAt(new Vector2(300, 300))));
    }

    [Fact]
    public void LegacyPositions_MapBackToTheOriginalPlaceTheyStoodFor()
    {
        var map = new SectorMap(1, 1, [0], Expire, 48, 48);
        // The old practice sector drew LIO (x, y) at ((x - 16000) * 1400 / 32000, ...); RefineryA was LIO (10752, 624).
        var old = new Vector2((10752 - 16000) * SectorMap.LegacyScale, (624 - 16000) * SectorMap.LegacyScale);
        var back = map.FromLegacyPracticeSector(old);
        Assert.Equal(10752 - 48, back.X, 2);
        Assert.Equal(624 - 48, back.Y, 2);
    }

    // ---- Ship and shots --------------------------------------------------------------------------------------------------

    [Fact]
    public void Ship_BouncesOffAWall_LikeTheEmulatorBot()
    {
        var world = World(Fixture()); // heading +x towards the wall at x = 480
        var restitution = world.Player.Definition.Physics.AsteroidRestitution;
        Vector2 before = default;
        for (var tick = 0; tick < 600 && world.WallCollisions == 0; tick++)
        {
            before = world.Player.Velocity;
            world.Tick(new ShipInput(1, 0, false));
            Assert.False(world.Map!.IsBlocked(world.Player.Position), $"The ship entered a wall at {world.Player.Position}.");
        }
        Assert.Equal(1, world.WallCollisions);
        // Thrust is applied first, then the bounce reverses x (its tile index changed) and scales the whole velocity.
        var thrust = world.Player.Definition.Physics.ForwardThrust / world.Player.Definition.Physics.Mass * WorldSimulation.TickDuration;
        var expected = new Vector2(-(before.X + thrust), before.Y) * restitution;
        TestWorld.Near(expected, world.Player.Velocity, 0.01f);
        Assert.True(world.Player.Position.X < 480);
    }

    [Fact]
    public void Ship_CannotLeaveTheMap()
    {
        var world = World(Fixture(), x: 100, y: 20, heading: -MathF.PI / 2); // up, towards y = 0
        TestWorld.Tick(world, 600, new ShipInput(1, 0, false));
        Assert.True(world.WallCollisions > 0);
        Assert.InRange(world.Player.Position.Y, 0, 640);
    }

    [Fact]
    public void Shots_StopAtTheFirstWall()
    {
        var world = World(Fixture(), x: 400);
        world.Tick(new ShipInput(0, 0, true));
        Assert.Single(world.Projectiles);
        TestWorld.Tick(world, 30);
        Assert.Empty(world.Projectiles); // the 80-pixel gap to the wall is crossed in well under 30 ticks
    }

    [Fact]
    public void RestoringOntoAWall_KeepsTheSpawnButStillRestoresTheHull()
    {
        var world = World(Fixture());
        Assert.True(world.RestorePlayer(new Vector2(485, 100), 1, 7));
        Assert.Equal((new Vector2(320, 320), 7f), (world.Player.Position, world.Player.Hull));
        Assert.True(world.RestorePlayer(new Vector2(200, 100), 1, 7));
        Assert.Equal(new Vector2(200, 100), world.Player.Position);
    }

    // ---- Drops ------------------------------------------------------------------------------------------------------------

    [Fact]
    public void MineralDrops_NeverLandOnAWall_AndLastTheirTerrainsPrizeExpire()
    {
        // NEW fixture: one area spanning the wall column and the terrain-10 corner, 40 drops per event.
        var spawn = new MineralSpawnDefinition("s", Ore, 1, 320, 320, 640, 640, 60, 60, 80, MaxInArea: -1, ClumpQuantity: 40, ExpireTicks: 720 * 60);
        var world = World(Fixture(), x: 600, y: 600, minerals: new MineralFieldCatalog([spawn], 7));
        for (var tick = 0; tick < 600; tick++)
        {
            world.Tick(default);
            Assert.All(world.Collectibles, p => Assert.False(world.Map!.IsBlocked(p.Position), $"{p.Id} landed on a wall at {p.Position}."));
        }
        var probe = World(Fixture(), x: 600, y: 600, minerals: new MineralFieldCatalog([spawn], 7));
        probe.Tick(default);
        Assert.NotEmpty(probe.Collectibles);
        foreach (var pile in probe.Collectibles)
            Assert.Equal(1 + probe.Map!.PrizeExpireTicksAt(pile.Position), pile.ExpiresAt);
    }

    [Fact]
    public void WreckDrops_LastTheirTerrainsPrizeExpire()
    {
        var world = World(Fixture());
        var dock = Assert.Single(world.DropCargo("w:1", new Vector2(20, 20), new Dictionary<string, int> { [Ore] = 2 }));
        var space = Assert.Single(world.DropCargo("w:2", new Vector2(200, 600), new Dictionary<string, int> { [Ore] = 2 }));
        Assert.Equal((120L * 60, 720L * 60), (dock.ExpiresAt, space.ExpiresAt));
    }

    // ---- Sector saves -----------------------------------------------------------------------------------------------------

    [Fact]
    public void ASectorSavedOnAMap_IsMarkedWithTheMapCoordinates()
    {
        Assert.Equal(SectorMap.CoordinateVersion, World(Fixture()).CaptureSector().MapVersion);
        Assert.Equal(0, new WorldSimulation(Catalog()).CaptureSector().MapVersion);
    }

    [Fact]
    public void ALegacySectorSave_IsMovedOntoTheMap_WithoutRefillingOrLosingAPile()
    {
        var map = new SectorMap(40, 40, new byte[1600], Expire, 48, 48);
        var configured = new CollectibleSpawn("pile_config", Ore, 5, 300, 300, 14);
        var cargo = new CargoCatalog(1000, [new(Ore, 1)], [configured]);
        var world = new WorldSimulation(Catalog(), cargo, map: map);
        // Saved by the old 1,400-unit practice sector: a configured pile, a mineral drop and a wreck drop at its old places.
        var legacyMineral = new Vector2((300 + 48 - 16000) * SectorMap.LegacyScale, (200 + 48 - 16000) * SectorMap.LegacyScale);
        var snapshot = world.CaptureSector() with
        {
            MapVersion = 0,
            Piles =
            [
                new PileSnapshot("pile_config", Ore, 2, 0, -70, 14),
                new PileSnapshot("mineral:9:1", Ore, 1, legacyMineral.X, legacyMineral.Y, 80 * SectorMap.LegacyScale, 600),
                new PileSnapshot("wreck:w:1:" + Ore, Ore, 3, legacyMineral.X, legacyMineral.Y, 3.5f, 900),
            ],
        };
        var next = new WorldSimulation(Catalog(), cargo, map: map);
        Assert.True(next.RestoreSector(snapshot));
        var piles = next.Collectibles.ToDictionary(p => p.Id);
        Assert.Equal(3, piles.Count);
        Assert.Equal((new Vector2(300, 300), 2, 14f), (piles["pile_config"].Position, piles["pile_config"].Quantity, piles["pile_config"].Radius));
        TestWorld.Near(new Vector2(300, 200), piles["mineral:9:1"].Position, 0.05f);
        Assert.Equal(80, piles["mineral:9:1"].Radius, 3);
        Assert.Equal(600, piles["mineral:9:1"].ExpiresAt);
        Assert.Equal(80, piles["wreck:w:1:" + Ore].Radius, 3);

        // A save already in map coordinates is restored as it is.
        var again = new WorldSimulation(Catalog(), cargo, map: map);
        Assert.True(again.RestoreSector(next.CaptureSector()));
        Assert.Equal(next.Collectibles.Select(p => (p.Id, p.Position)), again.Collectibles.Select(p => (p.Id, p.Position)));
    }

    // ---- The Rogue map ----------------------------------------------------------------------------------------------------

    [Fact]
    public void Rogue_FlyingStraightOutOfTheColdHarborBay_BouncesOffItsWalls()
    {
        var world = new WorldSimulation(TestWorld.RogueCatalog(), map: TestWorld.RogueMap);
        Assert.Equal(new Vector2(1888, 1728), world.Player.Position);
        for (var tick = 0; tick < 20 * WorldSimulation.TickRate; tick++)
        {
            world.Tick(new ShipInput(1, 0, false));
            Assert.False(TestWorld.RogueMap.IsBlocked(world.Player.Position), $"Tick {tick}: the ship entered a wall at {world.Player.Position}.");
        }
        Assert.True(world.WallCollisions > 0, "Twenty seconds of thrust from the dock must reach one of its walls.");
    }

    [Fact]
    public void Rogue_PracticeRangeStillWorks_FromItsOwnCentre()
    {
        var catalog = TestWorld.RogueCatalog();
        var range = new GameCatalog(catalog.Ship, catalog.Weapon,
            catalog.Sector with { PlayerSpawn = catalog.Sector.PlayerSpawn with { X = 4400, Y = 1600 } });
        var world = new WorldSimulation(range, map: TestWorld.RogueMap);
        TestWorld.Tick(world, 360, new ShipInput(0, 0, true));
        Assert.True(world.DestroyedTargets > 0, "The firing lane from the range centre must still reach target_alpha.");
    }
}
