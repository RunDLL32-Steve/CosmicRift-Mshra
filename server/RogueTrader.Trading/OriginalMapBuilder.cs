using System.Globalization;
using System.Text.Json;
using RogueTrader.GameData;
using RogueTrader.OriginalData;
using RogueTrader.Simulation;

namespace RogueTrader.Trading;

/// <summary>Where a derived sector's player spawn came from: the LIO warp row and its raw position and box.</summary>
public sealed record SectorSpawnSource(int LioRow, string Name, int X, int Y, int Width, int Height, int WarpGroup, int WarpMode, int WarpModeParameter);

/// <summary>The document behind <c>data/sectors/rogue_map.json</c>; <see cref="SectorMap.FromJson"/> reads it.</summary>
public sealed record SectorMapDocument(string Id, string EvidenceClassification, string Source, IReadOnlyList<string> NewRules,
    int Width, int Height, int TileSize, SectorMapOffset LioOffset, IReadOnlyList<int> TerrainPrizeExpireSeconds, string TileEncoding,
    int BlockedTiles, IReadOnlyDictionary<string, int> TerrainTiles, string Tiles)
{
    public string ToJson() => JsonSerializer.Serialize(this, StationDefinition.Json).ReplaceLineEndings("\n") + "\n";
}

public sealed record SectorMapOffset(int X, int Y);

/// <summary>
/// Derives the full-size Rogue sector from the pinned originals (G10 F10, Steve's "continue on the next major tasks",
/// November 4, 2026): the tile map (walls and terrain) from LVL/Rogue.lvl through the G6 reader, each terrain's drop life from
/// CFG [Terrain0..15] PrizeExpire, and the player spawn from the LIO warp the CFG's initial entry group sends a player to.
/// World coordinates are map pixels (see <see cref="SectorMap"/>). The NEW parts are listed in <see cref="MapNewRules"/> and
/// in the sector's evidence note.
/// </summary>
public static class OriginalMapBuilder
{
    public const string MapId = "rogue_map";
    public const string SectorId = "rogue";

    /// <summary>
    /// Pixels the emulator subtracts from every LIO position (LVL/Rogue.lvl OffsetX = OffsetY = 3 tiles, × 16). Pinned by
    /// <c>OriginalMapTests</c> against the LVL header, so builders that do not read the LFS-only LVL can still use it.
    /// </summary>
    public const int LioOffsetPixels = 48;

    /// <summary>NEW: centre of the practice range (targets and asteroids of <c>first_contact.json</c>), in open space east of Cold Harbor.</summary>
    public const float PracticeRangeX = 4400, PracticeRangeY = 1600;

    /// <summary>NEW: the team the solo pilot is on. CFG [TeamInfo0] is Cold Harbor; nothing pinned says which team a new pilot joins.</summary>
    public const int PilotTeam = 0;

    private const int WarpType = 4;
    private const int FWarpGroup = 14, FWarpMode = 15, FWarpModeParameter = 16;
    private const int SpecificTeamMode = 2;

    public static readonly IReadOnlyList<string> MapNewRules =
    [
        "Tiles: blocked = LVL plane 2 physics (low 5 bits) is not 0, terrain = TerrainLookup[plane 0 & 0x7F] mod 16, 16 pixels per tile: the G6 reader's emulator readings (RECONSTRUCTED). Height rules (PhysicsLow/High) are not applied; every non-zero physics tile blocks.",
        "World coordinates are map pixels from the top-left tile. LIO positions are moved by the LVL OffsetX/OffsetY (3 tiles = 48 pixels) as the emulator does (Logic_Lio.Warp, ArenaLio.hideSpawn).",
        "A ship bounces off a blocked tile as the emulator's bot does (MovementController at bb97579): its centre point is tested, the velocity components whose tile index changed reverse, and the velocity is scaled by VEH BouncePercent/1000. The emulator's 0.7 vertical screen factor is not applied (NEW). Shots stop at the first blocked tile.",
        "A drop lasts the PrizeExpire of the terrain it lands on (CFG [Terrain0] 720 s in open space; [Terrain9] repair docks, [Terrain10] SpaceDock store zones and [Terrain15] refinery and trade zones 120 s), as the emulator takes the terrain under the drop.",
        "Tile bytes: bit 4 = blocked, bits 0-3 = terrain; run-length encoded as (LEB128 run length, tile byte) pairs, base 64.",
    ];

    // ---- Map ----------------------------------------------------------------------------------------------------------

    public static SectorMapDocument BuildMap(string assetsRoot)
    {
        var level = OriginalLevel.LoadRogue(assetsRoot);
        var cfg = OriginalCfg.LoadRogue(assetsRoot);
        return BuildMap(level, cfg);
    }

    public static SectorMapDocument BuildMap(OriginalLevel level, OriginalCfg cfg)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(cfg);
        var grid = level.CollisionGrid;
        if (OriginalCollisionGrid.TileSizePixels != SectorMap.TileSize)
            throw new OriginalDataException($"The G6 tile size {OriginalCollisionGrid.TileSizePixels} is not the simulation's {SectorMap.TileSize}.");
        if (level.OffsetX * SectorMap.TileSize != LioOffsetPixels || level.OffsetY * SectorMap.TileSize != LioOffsetPixels)
            throw new OriginalDataException($"LVL offset ({level.OffsetX}, {level.OffsetY}) tiles is not the pinned {LioOffsetPixels} pixels.");
        var tiles = new byte[grid.Width * grid.Height];
        var blocked = 0;
        var terrainTiles = new SortedDictionary<int, int>();
        for (var y = 0; y < grid.Height; y++)
        for (var x = 0; x < grid.Width; x++)
        {
            var terrain = grid.TerrainAt(x, y);
            var isBlocked = grid.IsBlocked(x, y);
            tiles[y * grid.Width + x] = (byte)((isBlocked ? SectorMap.BlockedBit : 0) | terrain);
            if (isBlocked) blocked++;
            terrainTiles[terrain] = terrainTiles.GetValueOrDefault(terrain) + 1;
        }
        var expire = new int[SectorMap.TerrainCount];
        var lines = new List<string>();
        for (var t = 0; t < expire.Length; t++)
        {
            var section = cfg.Section($"Terrain{t}");
            var entry = section.Find("PrizeExpire") ?? throw new OriginalDataException($"CFG [Terrain{t}] has no PrizeExpire.");
            expire[t] = section.Int("PrizeExpire");
            lines.Add(entry.LineNumber.ToString(CultureInfo.InvariantCulture));
        }
        return new SectorMapDocument(MapId, "ORIGINAL+RECONSTRUCTED+NEW",
            $"Derived by OriginalMapBuilder from pinned {OriginalLevel.LvlRogue.RelativePath} (version {level.Version}, {grid.Width} x {grid.Height} tiles, offset {level.OffsetX}, {level.OffsetY}) through the G6 reader, and {OriginalCfg.Rogue.RelativePath} [Terrain0..15] PrizeExpire (lines {string.Join(", ", lines)}); regenerate, never hand-edit.",
            MapNewRules, grid.Width, grid.Height, SectorMap.TileSize, new SectorMapOffset(level.OffsetX * SectorMap.TileSize, level.OffsetY * SectorMap.TileSize),
            expire, "rle-leb128-base64", blocked,
            terrainTiles.ToDictionary(p => p.Key.ToString(CultureInfo.InvariantCulture), p => p.Value),
            Convert.ToBase64String(SectorMap.EncodeTiles(tiles)));
    }

    // ---- Sector -------------------------------------------------------------------------------------------------------

    /// <summary>An LIO position as a map position (the emulator's offset; see <see cref="LioOffsetPixels"/>).</summary>
    public static float World(int lio) => lio - LioOffsetPixels;

    /// <summary>
    /// The full-size sector: the player spawns at the centre of the warp the CFG's [WarpGroup] InitialEntry group offers the
    /// pilot's team (Cold Harbor SpawnA for team 0), and the NEW practice range of <paramref name="practice"/> is moved, unchanged
    /// in shape, to <see cref="PracticeRangeX"/>, <see cref="PracticeRangeY"/>.
    /// </summary>
    public static (SectorDefinition Sector, SectorSpawnSource Spawn) BuildSector(string assetsRoot, SectorDefinition practice)
    {
        var cfg = OriginalCfg.LoadRogue(assetsRoot);
        var lio = RawRecordReader.Parse(AssetIntegrity.ReadVerified(assetsRoot, OriginalSafeZones.LioRogue), OriginalSafeZones.LioRogue.RelativePath);
        return BuildSector(lio, cfg.Section("WarpGroup").Int("InitialEntry"), practice);
    }

    public static (SectorDefinition Sector, SectorSpawnSource Spawn) BuildSector(IReadOnlyList<RawRecord> lio, int initialEntryGroup, SectorDefinition practice)
    {
        ArgumentNullException.ThrowIfNull(lio);
        ArgumentNullException.ThrowIfNull(practice);
        var warp = lio.First(r => r.FieldCount > FWarpModeParameter && r.Value(0) == WarpType.ToString(CultureInfo.InvariantCulture)
            && r.Int(FWarpGroup).Value == initialEntryGroup && r.Int(FWarpMode).Value == SpecificTeamMode && r.Int(FWarpModeParameter).Value == PilotTeam);
        var source = new SectorSpawnSource(warp.RowNumber, warp.Value(3).Trim('"'), warp.Int(4).Value, warp.Int(5).Value, warp.Int(6).Value, warp.Int(7).Value,
            warp.Int(FWarpGroup).Value, warp.Int(FWarpMode).Value, warp.Int(FWarpModeParameter).Value);
        float dx = PracticeRangeX - practice.PlayerSpawn.X, dy = PracticeRangeY - practice.PlayerSpawn.Y;
        var sector = practice with
        {
            Id = SectorId,
            Name = "Rogue",
            EvidenceClassification = "ORIGINAL+NEW",
            MapFile = $"{MapId}.json",
            PlayerSpawn = practice.PlayerSpawn with { X = World(source.X), Y = World(source.Y) },
            Targets = practice.Targets.Select(t => t with { X = t.X + dx, Y = t.Y + dy }).ToArray(),
            Asteroids = practice.Asteroids.Select(a => a with { X = a.X + dx, Y = a.Y + dy }).ToArray(),
            Evidence =
            [
                $"Map: {MapId}.json, the pinned LVL/Rogue.lvl at map-pixel scale (1 world unit = 1 original pixel).",
                $"Player spawn: centre of LIO row {source.LioRow} '{source.Name}' ({source.X}, {source.Y}, box {source.Width} x {source.Height}) minus the LVL offset {LioOffsetPixels}; the warp of CFG [WarpGroup] InitialEntry={initialEntryGroup} for team {PilotTeam} (TeamInfo0 Cold Harbor). NEW: the pilot's team, and the centre instead of the emulator's random point in the box.",
                $"NEW: the practice targets and asteroids of first_contact.json, moved unchanged to ({PracticeRangeX}, {PracticeRangeY}) in open space east of Cold Harbor.",
            ],
        };
        return (sector, source);
    }

    public static string SectorToJson(SectorDefinition sector) =>
        JsonSerializer.Serialize(sector, StationDefinition.Json).ReplaceLineEndings("\n") + "\n";
}
