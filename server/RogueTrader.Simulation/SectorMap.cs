using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueTrader.Simulation;

/// <summary>
/// The tile map a sector is flown on: which 16-pixel tiles block a ship and which CFG terrain each tile belongs to. World
/// coordinates are map pixels, origin at the top-left corner of tile (0, 0), so a tile is <c>floor(x / 16), floor(y / 16)</c>
/// as in the InfantryOnline emulator's lookups. For the original Rogue map the tiles come from the pinned LVL/Rogue.lvl through
/// the G6 reader (derived file <c>data/sectors/rogue_map.json</c>); the readings are the emulator's (RECONSTRUCTED). Everything
/// outside the map blocks.
/// </summary>
public sealed class SectorMap
{
    /// <summary>Pixels per tile (the emulator's <c>x / 16</c>).</summary>
    public const int TileSize = 16;

    /// <summary>Terrain numbers run 0..15 (CFG [Terrain0]..[Terrain15]).</summary>
    public const int TerrainCount = 16;

    /// <summary>Bit of a tile byte that marks a blocked tile; the low four bits are the terrain number.</summary>
    public const byte BlockedBit = 0x10;

    /// <summary>
    /// Coordinate version of a sector saved on this map. Version 0 is the earlier practice sector, which drew the original
    /// map into 1,400 units around an assumed centre; version 1 is map pixels.
    /// </summary>
    public const int CoordinateVersion = 1;

    /// <summary>Practice-sector units per original map unit before version 1 (the old <c>MapScale</c>).</summary>
    public const float LegacyScale = 1400f / 32000f;

    /// <summary>Original map unit that the version 0 practice sector put at its origin (the old <c>MapCentre</c>).</summary>
    public const float LegacyCentre = 16000f;

    private readonly byte[] _tiles;
    private readonly long[] _prizeExpireTicks;

    public SectorMap(int width, int height, byte[] tiles, IReadOnlyList<int> terrainPrizeExpireSeconds, int lioOffsetX = 0, int lioOffsetY = 0)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(terrainPrizeExpireSeconds);
        if (width <= 0 || height <= 0 || (long)width * height != tiles.Length)
            throw new ArgumentException($"A {width} x {height} map needs {(long)width * height} tiles, got {tiles.Length}.", nameof(tiles));
        if (terrainPrizeExpireSeconds.Count != TerrainCount || terrainPrizeExpireSeconds.Any(s => s < 0))
            throw new ArgumentException($"A map needs {TerrainCount} non-negative PrizeExpire values.", nameof(terrainPrizeExpireSeconds));
        foreach (var tile in tiles)
            if ((tile & ~(BlockedBit | 0x0F)) != 0)
                throw new ArgumentException("A tile byte holds only the blocked bit and a terrain number.", nameof(tiles));
        Width = width;
        Height = height;
        _tiles = tiles;
        TerrainPrizeExpireSeconds = terrainPrizeExpireSeconds.ToArray();
        _prizeExpireTicks = TerrainPrizeExpireSeconds.Select(s => (long)s * WorldSimulation.TickRate).ToArray();
        LioOffset = new Vector2(lioOffsetX, lioOffsetY);
    }

    /// <summary>Width in tiles.</summary>
    public int Width { get; }

    /// <summary>Height in tiles.</summary>
    public int Height { get; }

    public float PixelWidth => Width * TileSize;
    public float PixelHeight => Height * TileSize;

    /// <summary>CFG [TerrainN] PrizeExpire seconds, indexed by terrain number.</summary>
    public IReadOnlyList<int> TerrainPrizeExpireSeconds { get; }

    /// <summary>
    /// Pixels the emulator subtracts from an LIO position to get a map position (LVL OffsetX/OffsetY × 16, as in
    /// <c>Logic_Lio.Warp</c> and <c>ArenaLio.hideSpawn</c>).
    /// </summary>
    public Vector2 LioOffset { get; }

    public bool InBounds(int tileX, int tileY) => (uint)tileX < (uint)Width && (uint)tileY < (uint)Height;

    /// <summary>The tile byte (blocked bit and terrain) of a tile; a tile outside the map is blocked open space.</summary>
    public byte TileAt(int tileX, int tileY) => InBounds(tileX, tileY) ? _tiles[tileY * Width + tileX] : BlockedBit;

    public bool IsBlockedTile(int tileX, int tileY) => (TileAt(tileX, tileY) & BlockedBit) != 0;

    public static (int TileX, int TileY) TileOf(Vector2 position) =>
        ((int)MathF.Floor(position.X / TileSize), (int)MathF.Floor(position.Y / TileSize));

    /// <summary>True on a blocked tile and anywhere outside the map (or at a non-finite position).</summary>
    public bool IsBlocked(Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) return true;
        var (x, y) = TileOf(position);
        return IsBlockedTile(x, y);
    }

    /// <summary>
    /// The open point nearest to <paramref name="centre"/> inside the box of half size <paramref name="halfWidth"/> by
    /// <paramref name="halfHeight"/>: the centre itself when it is open, otherwise the centre of the nearest open tile whose
    /// centre lies in the box (ties go to the lower row, then the lower column, so the answer never changes). Null when the box
    /// holds no open tile. A deterministic stand-in for the emulator's random open point in a warp box. With
    /// <paramref name="terrain"/>, only tiles of that terrain count.
    /// </summary>
    public Vector2? NearestOpen(Vector2 centre, float halfWidth, float halfHeight, int? terrain = null)
    {
        if (!IsBlocked(centre) && (terrain is null || TerrainAt(centre) == terrain)) return centre;
        Vector2? best = null;
        var bestDistance = float.MaxValue;
        var (x0, y0) = TileOf(centre - new Vector2(halfWidth, halfHeight));
        var (x1, y1) = TileOf(centre + new Vector2(halfWidth, halfHeight));
        for (var y = Math.Max(0, y0); y <= Math.Min(Height - 1, y1); y++)
        for (var x = Math.Max(0, x0); x <= Math.Min(Width - 1, x1); x++)
        {
            if (IsBlockedTile(x, y) || (terrain is { } wanted && (TileAt(x, y) & 0x0F) != wanted)) continue;
            var point = new Vector2(x * TileSize + TileSize / 2f, y * TileSize + TileSize / 2f);
            if (MathF.Abs(point.X - centre.X) > halfWidth || MathF.Abs(point.Y - centre.Y) > halfHeight) continue;
            var distance = Vector2.DistanceSquared(point, centre);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = point;
            }
        }
        return best;
    }

    /// <summary>The terrain number under a position; 0 (open space) outside the map.</summary>
    public int TerrainAt(Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y)) return 0;
        var (x, y) = TileOf(position);
        return InBounds(x, y) ? _tiles[y * Width + x] & 0x0F : 0;
    }

    /// <summary>How long a drop lying at <paramref name="position"/> lasts: the PrizeExpire of the terrain under it, in ticks.</summary>
    public long PrizeExpireTicksAt(Vector2 position) => _prizeExpireTicks[TerrainAt(position)];

    /// <summary>Longest drop life of any terrain, in ticks.</summary>
    public long LongestPrizeExpireTicks => _prizeExpireTicks.Max();

    /// <summary>Converts an LIO position (original map units) to a map position.</summary>
    public Vector2 FromLio(float x, float y) => new Vector2(x, y) - LioOffset;

    /// <summary>
    /// Converts a position saved by the version 0 practice sector, which drew LIO positions at <see cref="LegacyScale"/> around
    /// <see cref="LegacyCentre"/>, back to the map position it stood for.
    /// </summary>
    public Vector2 FromLegacyPracticeSector(Vector2 position) => FromLio(position.X / LegacyScale + LegacyCentre, position.Y / LegacyScale + LegacyCentre);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };

    /// <summary>
    /// Reads the derived map document (<c>data/sectors/rogue_map.json</c>): <c>width</c>, <c>height</c>, <c>tileSize</c> (must
    /// be 16), <c>lioOffset</c>, <c>terrainPrizeExpireSeconds</c> and <c>tiles</c>, the run-length encoded tile bytes in base 64
    /// (see <see cref="EncodeTiles"/>). Other members are evidence and are ignored.
    /// </summary>
    public static SectorMap FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<Document>(json, JsonOptions) ?? throw new JsonException("Empty sector map.");
        if (document.TileSize != TileSize) throw new JsonException($"Sector map tile size must be {TileSize}, got {document.TileSize}.");
        if (document.Tiles is null || document.TerrainPrizeExpireSeconds is null) throw new JsonException("Sector map needs tiles and terrain expiry.");
        var tiles = DecodeTiles(Convert.FromBase64String(document.Tiles), checked(document.Width * document.Height));
        return new SectorMap(document.Width, document.Height, tiles, document.TerrainPrizeExpireSeconds,
            document.LioOffset?.X ?? 0, document.LioOffset?.Y ?? 0);
    }

    /// <summary>
    /// Run-length encodes tile bytes: for each run, its length as an unsigned LEB128 varint, then the tile byte. Deterministic,
    /// so the derived file is stable.
    /// </summary>
    public static byte[] EncodeTiles(ReadOnlySpan<byte> tiles)
    {
        var output = new List<byte>(tiles.Length / 16);
        for (var i = 0; i < tiles.Length;)
        {
            var value = tiles[i];
            var run = 1;
            while (i + run < tiles.Length && tiles[i + run] == value) run++;
            var n = (uint)run;
            while (n >= 0x80)
            {
                output.Add((byte)(n | 0x80));
                n >>= 7;
            }
            output.Add((byte)n);
            output.Add(value);
            i += run;
        }
        return output.ToArray();
    }

    /// <summary>Inverse of <see cref="EncodeTiles"/>; throws on a zero-length run, an overrun or a short stream.</summary>
    public static byte[] DecodeTiles(ReadOnlySpan<byte> encoded, int length)
    {
        var tiles = new byte[length];
        var written = 0;
        var offset = 0;
        while (offset < encoded.Length)
        {
            uint run = 0;
            var shift = 0;
            while (true)
            {
                if (offset >= encoded.Length || shift > 28) throw new JsonException("Sector map tiles end inside a run length.");
                var b = encoded[offset++];
                run |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
            }
            if (offset >= encoded.Length) throw new JsonException("Sector map tiles end before a run's tile byte.");
            var value = encoded[offset++];
            if (run == 0 || written + run > (uint)length) throw new JsonException("Sector map tile run is empty or passes the map end.");
            tiles.AsSpan(written, (int)run).Fill(value);
            written += (int)run;
        }
        if (written != length) throw new JsonException($"Sector map tiles cover {written} of {length} tiles.");
        return tiles;
    }

    private sealed class Document
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileSize { get; set; }
        public Offset? LioOffset { get; set; }
        public int[]? TerrainPrizeExpireSeconds { get; set; }
        public string? Tiles { get; set; }
    }

    private sealed class Offset
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
