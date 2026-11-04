using System.Numerics;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests;

/// <summary>
/// The full-size Rogue sector (G10 F10): the derived tile map <c>data/sectors/rogue_map.json</c> and sector
/// <c>data/sectors/rogue.json</c> must equal what <see cref="OriginalMapBuilder"/> derives from the pinned originals. Set
/// RT_WRITE_DERIVED_MAP=1 to rewrite both. The map oracle needs the Git LFS content of LVL/Rogue.lvl; the shipped-data checks
/// run everywhere.
/// </summary>
public sealed class OriginalMapTests
{
    private static string Data(params string[] parts) => File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "data", .. parts]));

    private static string Committed(params string[] parts) =>
        Path.Combine([OriginalAssets.RepoRoot() ?? throw new InvalidOperationException("Repository root not found."), "data", .. parts]);

    private static readonly Lazy<SectorMap> Map = new(() => SectorMap.FromJson(Data("sectors", "rogue_map.json")));

    private static SectorDefinition Sector() =>
        GameCatalog.FromJson(Data("ships", "starter_interceptor.json"), Data("weapons", "plasma_cannon.json"), Data("sectors", "rogue.json")).Sector;

    private static SectorDefinition Practice() =>
        GameCatalog.FromJson(Data("ships", "starter_interceptor.json"), Data("weapons", "plasma_cannon.json"), Data("sectors", "first_contact.json")).Sector;

    // ---- Oracles ------------------------------------------------------------------------------------------------------

    [RequiresOriginalLevelFact]
    public void CommittedMap_MatchesPinnedLevel()
    {
        var derived = OriginalMapBuilder.BuildMap(OriginalAssets.Root!).ToJson();
        if (Environment.GetEnvironmentVariable("RT_WRITE_DERIVED_MAP") == "1") File.WriteAllText(Committed("sectors", "rogue_map.json"), derived);
        Assert.Equal(derived, File.ReadAllText(Committed("sectors", "rogue_map.json")).ReplaceLineEndings("\n"));
    }

    [RequiresOriginalLevelFact]
    public void CommittedMap_ReadsEveryTileAsTheG6GridDoes()
    {
        var grid = OriginalLevel.LoadRogue(OriginalAssets.Root!).CollisionGrid;
        var map = Map.Value;
        Assert.Equal((grid.Width, grid.Height), (map.Width, map.Height));
        for (var y = 0; y < grid.Height; y++)
        for (var x = 0; x < grid.Width; x++)
        {
            var tile = map.TileAt(x, y);
            if (((tile & SectorMap.BlockedBit) != 0) != grid.IsBlocked(x, y) || (tile & 0x0F) != grid.TerrainAt(x, y))
                Assert.Fail($"Tile ({x}, {y}) differs from the G6 grid.");
        }
    }

    [RequiresOriginalLevelFact]
    public void LioOffset_IsTheLevelHeaderOffset()
    {
        var level = OriginalLevel.LoadRogue(OriginalAssets.Root!);
        Assert.Equal((3, 3), (level.OffsetX, level.OffsetY));
        Assert.Equal(OriginalMapBuilder.LioOffsetPixels, level.OffsetX * SectorMap.TileSize);
    }

    [RequiresOriginalAssetsFact]
    public void CommittedSector_MatchesPinnedOriginals()
    {
        var (sector, spawn) = OriginalMapBuilder.BuildSector(OriginalAssets.Root!, Practice());
        var derived = OriginalMapBuilder.SectorToJson(sector);
        if (Environment.GetEnvironmentVariable("RT_WRITE_DERIVED_MAP") == "1") File.WriteAllText(Committed("sectors", "rogue.json"), derived);
        Assert.Equal(derived, File.ReadAllText(Committed("sectors", "rogue.json")).ReplaceLineEndings("\n"));
        // CFG [WarpGroup] InitialEntry=0; LIO row 6 is the group 0 warp for team 0 (SpecificTeam mode 2).
        Assert.Equal((6, "Cold Harbor SpawnA", 1936, 1776, 300, 250, 0, 2, 0),
            (spawn.LioRow, spawn.Name, spawn.X, spawn.Y, spawn.Width, spawn.Height, spawn.WarpGroup, spawn.WarpMode, spawn.WarpModeParameter));
    }

    // ---- Shipped data -------------------------------------------------------------------------------------------------

    [Fact]
    public void ShippedMap_IsTheFullOriginalSizeWithItsTerrains()
    {
        var map = Map.Value;
        Assert.Equal((1999, 1998), (map.Width, map.Height));
        Assert.Equal((31984f, 31968f), (map.PixelWidth, map.PixelHeight));
        Assert.Equal(new Vector2(48, 48), map.LioOffset);
        // CFG [Terrain0] PrizeExpire=720, [Terrain1..15]=120.
        Assert.Equal([720, .. Enumerable.Repeat(120, 15)], map.TerrainPrizeExpireSeconds);
        var document = Data("sectors", "rogue_map.json");
        Assert.Contains("\"blockedTiles\": 116065", document); // the G6 count
        Assert.Contains("\"evidenceClassification\": \"ORIGINAL+RECONSTRUCTED+NEW\"", document);
        // The border tiles themselves are open (the emulator clamps positions to the map instead); everything outside blocks.
        Assert.False(map.IsBlockedTile(0, 1000) || map.IsBlockedTile(1000, 0));
        Assert.True(map.IsBlocked(new Vector2(-1, 500)) && map.IsBlocked(new Vector2(500, 40000)) && map.IsBlocked(new Vector2(31984, 10)));
    }

    [Fact]
    public void ShippedMap_PutsTheOriginalPlacesOnTheirTerrains()
    {
        var map = Map.Value;
        Assert.Equal(10, map.TerrainAt(map.FromLio(1936, 1776)));  // Cold Harbor SpawnA: SpaceDock store zone
        Assert.Equal(15, map.TerrainAt(map.FromLio(10752, 624)));  // RefineryA: refinery/trade zone
        Assert.Equal(15, map.TerrainAt(map.FromLio(4448, 20848))); // TradingPostA
        Assert.Equal(9, map.TerrainAt(map.FromLio(1808, 3024)));   // RepairDockA
        Assert.Equal(0, map.TerrainAt(map.FromLio(19280, 10160))); // Global Tellerium SpawnA: open space
        Assert.Equal(120L * WorldSimulation.TickRate, map.PrizeExpireTicksAt(map.FromLio(10752, 624)));
        Assert.Equal(720L * WorldSimulation.TickRate, map.PrizeExpireTicksAt(map.FromLio(19280, 10160)));
    }

    [Fact]
    public void ShippedSector_PlacesEverythingOnOpenTiles()
    {
        var map = Map.Value;
        var sector = Sector();
        var station = StationDefinition.FromJson(Data("stations", "trading_outpost.json"));
        Assert.Equal("rogue", sector.Id);
        Assert.Equal("rogue_map.json", sector.MapFile);
        Assert.Equal(new Vector2(1888, 1728), new Vector2(sector.PlayerSpawn.X, sector.PlayerSpawn.Y));
        Assert.False(map.IsBlocked(new Vector2(sector.PlayerSpawn.X, sector.PlayerSpawn.Y)));
        foreach (var target in sector.Targets) AssertOpenCircle(map, new Vector2(target.X, target.Y), target.Radius + 20, target.Id);
        foreach (var rock in sector.Asteroids) AssertOpenCircle(map, new Vector2(rock.X, rock.Y), rock.Radius + 20, rock.Id);
        AssertOpenCircle(map, new Vector2(station.X, station.Y), 20, "station");
        Assert.Equal(10, map.TerrainAt(new Vector2(station.X, station.Y))); // the Cold Harbor store zone
        AssertOpenCircle(map, new Vector2(station.Refinery!.X, station.Refinery.Y), 20, "refinery");
        foreach (var pile in station.Collectibles) AssertOpenCircle(map, new Vector2(pile.X, pile.Y), pile.Radius, pile.Id);
        // The practice range keeps its shape: the same offsets from its centre as first_contact.json from its spawn.
        var practice = Practice();
        for (var i = 0; i < practice.Targets.Count; i++)
            Assert.Equal(new Vector2(practice.Targets[i].X, practice.Targets[i].Y) + new Vector2(OriginalMapBuilder.PracticeRangeX, OriginalMapBuilder.PracticeRangeY),
                new Vector2(sector.Targets[i].X, sector.Targets[i].Y));
    }

    [Fact]
    public void ShippedSector_IsTheOriginalSize_CrossingTakesAboutNinetySeconds()
    {
        // G10 F10: 1999 tiles x 16 px at the Asp's 325 px/s.
        var map = Map.Value;
        var ship = GameCatalog.FromJson(Data("ships", "starter_interceptor.json"), Data("weapons", "plasma_cannon.json"), Data("sectors", "rogue.json")).Ship;
        Assert.InRange(map.PixelWidth / ship.Physics.MaximumSpeed, 98, 99);
    }

    private static void AssertOpenCircle(SectorMap map, Vector2 centre, float radius, string what)
    {
        for (var a = 0; a < 36; a++)
        for (var r = 0f; r <= radius; r += Math.Max(1, radius / 4))
        {
            var p = centre + new Vector2(MathF.Cos(a * MathF.Tau / 36), MathF.Sin(a * MathF.Tau / 36)) * r;
            Assert.False(map.IsBlocked(p), $"{what} at {centre} touches a blocked tile at {p}.");
        }
    }
}
