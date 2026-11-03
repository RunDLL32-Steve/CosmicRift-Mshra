using System.Buffers.Binary;
using System.Text;

namespace RogueTrader.OriginalData;

/// <summary>Why an LVL file was refused. Every refusal is explicit; nothing is padded, clamped or guessed.</summary>
public enum OriginalLevelFailure
{
    /// <summary>Fewer bytes than the fixed 5,152-byte header.</summary>
    TruncatedHeader,
    /// <summary>The version field is not one this reader supports (only 6, the pinned Rogue.lvl, is supported).</summary>
    UnsupportedVersion,
    /// <summary>Width, height or a count is zero/negative or too large to hold.</summary>
    InvalidDimensions,
    /// <summary>The file ends inside the blob reference table.</summary>
    TruncatedBlobReferences,
    /// <summary>The file ends inside an RLE run header or value byte.</summary>
    TruncatedPlane,
    /// <summary>An RLE run has length zero (the escaped 16-bit length was 0).</summary>
    ZeroLengthRun,
    /// <summary>An RLE run writes past the end of its width × height plane.</summary>
    PlaneOverrun,
    /// <summary>The bytes after the three planes are not exactly EntityCount × 17 bytes.</summary>
    EntityTableSizeMismatch,
}

/// <summary>An LVL file could not be parsed. <see cref="Offset"/> is the byte offset where the problem was found.</summary>
public sealed class OriginalLevelFormatException : OriginalDataException
{
    public OriginalLevelFormatException(string sourceFile, OriginalLevelFailure failure, long offset, string message)
        : base($"{sourceFile} at byte {offset}: {failure}: {message}")
    {
        SourceFile = sourceFile;
        Failure = failure;
        Offset = offset;
    }

    public string SourceFile { get; }
    public OriginalLevelFailure Failure { get; }
    public long Offset { get; }
}

/// <summary>One 64-byte blob reference: a 32-byte NUL-padded BLO file name and a 32-byte NUL-padded member id, plus the raw bytes.</summary>
public sealed record OriginalLevelBlobReference(int Index, string FileName, string MemberId, byte[] RawBytes);

/// <summary>
/// One 17-byte record of the entity table that follows the three tile planes. The field layout is not decoded: the
/// emulator's reader stops before this table ("Tile data is followed with entity (object) information"), so only the
/// raw bytes are kept. The 17-byte stride is VERIFIED only in the sense that it divides the pinned tail exactly
/// (380,052 = 22,356 × 17).
/// </summary>
public sealed record OriginalLevelEntity(int Index, long Offset, byte[] RawBytes);

/// <summary>
/// Queue G6 (RT-F021/F022 data only): a parsed LVL file. Every header byte, every decoded plane byte and every entity
/// byte is kept. Field names and the plane meanings follow the InfantryOnline emulator reader
/// (Infantry-Online-Server bb97579, dotnetcore/Assets/Lvl/LvlInfo*.cs, header struct zlib-licensed by Rick/Gibbed);
/// those readings are RECONSTRUCTED for the original client, not native evidence.
/// </summary>
public sealed class OriginalLevel
{
    /// <summary>Fixed header size in bytes (VERIFIED: the header struct is 5,152 bytes and the pinned file's blob table starts there).</summary>
    public const int HeaderSize = 5152;

    /// <summary>Size of one blob reference.</summary>
    public const int BlobReferenceSize = 64;

    /// <summary>Size of one entity record (observed stride; see <see cref="OriginalLevelEntity"/>).</summary>
    public const int EntitySize = 17;

    /// <summary>The only LVL version this reader accepts. The pinned Rogue.lvl is version 6.</summary>
    public const int SupportedVersion = 6;

    /// <summary>Upper bound on width × height (16M tiles), so a corrupt header cannot request gigabytes.</summary>
    public const int MaxTiles = 16 * 1024 * 1024;

    /// <summary>
    /// LVL/Rogue.lvl at the canonical pin. The file is Git LFS in fowlmouth/cosmic-rift-assets; the same bytes
    /// (same SHA-256) are in InfantryOnline/Zone-Assets 401633b (docs/original-research/ZONE_ASSETS_COMPARISON.md).
    /// </summary>
    public static readonly PinnedAsset LvlRogue = new("LVL/Rogue.lvl", 1290994, "7034476858b86e9b11a41eaaa0d636003eb432a4b682e45d264def8b425cf02f");

    internal OriginalLevel() { }

    public required string SourceFile { get; init; }
    public required int Version { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int EntityCount { get; init; }
    public required int FloorCount { get; init; }
    public required int ObjectCount { get; init; }

    /// <summary>512 raw uint32 values at header offset 24 (emulator name MinimapPalette).</summary>
    public required IReadOnlyList<uint> MinimapPalette { get; init; }

    /// <summary>Raw int32 at header offset 2072 (emulator name OffsetX; meaning not established).</summary>
    public required int OffsetX { get; init; }

    /// <summary>Raw int32 at header offset 2076 (emulator name OffsetY; meaning not established).</summary>
    public required int OffsetY { get; init; }

    /// <summary>128 raw int32 values at header offset 2080 (emulator name TerrainLookup), not reduced modulo 16.</summary>
    public required IReadOnlyList<int> TerrainLookupRaw { get; init; }

    /// <summary>32 raw int16 values at header offset 2592 (emulator name PhysicsLow).</summary>
    public required IReadOnlyList<short> PhysicsLow { get; init; }

    /// <summary>32 raw int16 values at header offset 2656 (emulator name PhysicsHigh).</summary>
    public required IReadOnlyList<short> PhysicsHigh { get; init; }

    /// <summary>Four raw uint32 values at header offset 2720 (emulator names LightColorWhite/Red/Green/Blue).</summary>
    public required IReadOnlyList<uint> LightColors { get; init; }

    /// <summary>The 5,152 header bytes exactly as read, including the 2,416 trailing bytes the emulator calls padding.</summary>
    public required byte[] HeaderBytes { get; init; }

    /// <summary>The first <see cref="ObjectCount"/> blob references (emulator order: objects, then floors).</summary>
    public required IReadOnlyList<OriginalLevelBlobReference> ObjectBlobs { get; init; }

    /// <summary>The <see cref="FloorCount"/> blob references after the object references.</summary>
    public required IReadOnlyList<OriginalLevelBlobReference> FloorBlobs { get; init; }

    /// <summary>First decoded plane, width × height bytes, row-major. Emulator: low 7 bits index the terrain lookup; bit 7 unknown.</summary>
    public required byte[] Plane0 { get; init; }

    /// <summary>Second decoded plane, width × height bytes. Emulator name Unknown1; meaning not established.</summary>
    public required byte[] Plane1 { get; init; }

    /// <summary>Third decoded plane, width × height bytes. Emulator: low 5 bits physics (collision), high 3 bits vision.</summary>
    public required byte[] Plane2 { get; init; }

    /// <summary>Byte offset (in the file) where each plane's RLE stream starts and where the entity table starts.</summary>
    public required IReadOnlyList<long> PlaneOffsets { get; init; }

    public required long EntityTableOffset { get; init; }

    public required IReadOnlyList<OriginalLevelEntity> Entities { get; init; }

    public int TileCount => Width * Height;

    /// <summary>The collision/terrain grid view over the decoded planes.</summary>
    public OriginalCollisionGrid CollisionGrid => _grid ??= new OriginalCollisionGrid(this);
    private OriginalCollisionGrid? _grid;

    /// <summary>Reads, hash-verifies and parses <see cref="LvlRogue"/> below <paramref name="assetsRoot"/>.</summary>
    public static OriginalLevel LoadRogue(string assetsRoot) =>
        Parse(AssetIntegrity.ReadVerified(assetsRoot, LvlRogue), LvlRogue.RelativePath);

    /// <summary>
    /// Parses LVL bytes. Layout (version 6): 5,152-byte header; (ObjectCount + FloorCount) × 64-byte blob references;
    /// three RLE planes of width × height bytes each; EntityCount × 17-byte entity records; end of file. RLE: one length
    /// byte, or 0 followed by a big-endian 16-bit length, then one value byte. Deterministic: the same bytes always give
    /// the same result, and any deviation from this layout throws <see cref="OriginalLevelFormatException"/>.
    /// </summary>
    public static OriginalLevel Parse(ReadOnlySpan<byte> bytes, string sourceFile)
    {
        ArgumentNullException.ThrowIfNull(sourceFile);
        if (bytes.Length < HeaderSize)
            throw Fail(sourceFile, OriginalLevelFailure.TruncatedHeader, bytes.Length, $"file has {bytes.Length} bytes, header needs {HeaderSize}.");

        var header = bytes[..HeaderSize];

        var version = I32(header, 0);
        if (version != SupportedVersion)
            throw Fail(sourceFile, OriginalLevelFailure.UnsupportedVersion, 0, $"version {version}; only version {SupportedVersion} is supported.");

        int width = I32(header, 4), height = I32(header, 8), entityCount = I32(header, 12), floorCount = I32(header, 16), objectCount = I32(header, 20);
        if (width <= 0 || height <= 0 || (long)width * height > MaxTiles)
            throw Fail(sourceFile, OriginalLevelFailure.InvalidDimensions, 4, $"width {width} × height {height} is outside 1..{MaxTiles} tiles.");
        if (entityCount < 0 || floorCount < 0 || objectCount < 0)
            throw Fail(sourceFile, OriginalLevelFailure.InvalidDimensions, 12, $"negative count (entities {entityCount}, floors {floorCount}, objects {objectCount}).");

        var palette = new uint[512];
        for (var i = 0; i < palette.Length; i++) palette[i] = U32(header, 24 + 4 * i);
        var terrain = new int[128];
        for (var i = 0; i < terrain.Length; i++) terrain[i] = I32(header, 2080 + 4 * i);
        var low = new short[32];
        var high = new short[32];
        for (var i = 0; i < 32; i++)
        {
            low[i] = I16(header, 2592 + 2 * i);
            high[i] = I16(header, 2656 + 2 * i);
        }
        var lights = new uint[4];
        for (var i = 0; i < 4; i++) lights[i] = U32(header, 2720 + 4 * i);

        long offset = HeaderSize;
        long blobBytes = (long)(objectCount + (long)floorCount) * BlobReferenceSize;
        if (offset + blobBytes > bytes.Length)
            throw Fail(sourceFile, OriginalLevelFailure.TruncatedBlobReferences, bytes.Length,
                $"{objectCount} object + {floorCount} floor references need {blobBytes} bytes after the header.");
        var objects = ReadBlobs(bytes, ref offset, objectCount);
        var floors = ReadBlobs(bytes, ref offset, floorCount);

        var tiles = width * height;
        var planeOffsets = new long[3];
        var planes = new byte[3][];
        for (var p = 0; p < 3; p++)
        {
            planeOffsets[p] = offset;
            planes[p] = DecodePlane(bytes, ref offset, tiles, sourceFile);
        }

        var tail = bytes.Length - offset;
        if (tail != (long)entityCount * EntitySize)
            throw Fail(sourceFile, OriginalLevelFailure.EntityTableSizeMismatch, offset,
                $"{tail} bytes follow the planes; {entityCount} entities × {EntitySize} = {(long)entityCount * EntitySize}.");
        var entityOffset = offset;
        var entities = new OriginalLevelEntity[entityCount];
        for (var i = 0; i < entityCount; i++)
        {
            entities[i] = new OriginalLevelEntity(i, offset, bytes.Slice((int)offset, EntitySize).ToArray());
            offset += EntitySize;
        }

        return new OriginalLevel
        {
            SourceFile = sourceFile,
            Version = version,
            Width = width,
            Height = height,
            EntityCount = entityCount,
            FloorCount = floorCount,
            ObjectCount = objectCount,
            MinimapPalette = palette,
            OffsetX = I32(header, 2072),
            OffsetY = I32(header, 2076),
            TerrainLookupRaw = terrain,
            PhysicsLow = low,
            PhysicsHigh = high,
            LightColors = lights,
            HeaderBytes = bytes[..HeaderSize].ToArray(),
            ObjectBlobs = objects,
            FloorBlobs = floors,
            Plane0 = planes[0],
            Plane1 = planes[1],
            Plane2 = planes[2],
            PlaneOffsets = planeOffsets,
            EntityTableOffset = entityOffset,
            Entities = entities,
        };
    }

    private static int I32(ReadOnlySpan<byte> b, int o) => BinaryPrimitives.ReadInt32LittleEndian(b.Slice(o, 4));
    private static uint U32(ReadOnlySpan<byte> b, int o) => BinaryPrimitives.ReadUInt32LittleEndian(b.Slice(o, 4));
    private static short I16(ReadOnlySpan<byte> b, int o) => BinaryPrimitives.ReadInt16LittleEndian(b.Slice(o, 2));

    private static OriginalLevelBlobReference[] ReadBlobs(ReadOnlySpan<byte> bytes, ref long offset, int count)
    {
        var result = new OriginalLevelBlobReference[count];
        for (var i = 0; i < count; i++)
        {
            var raw = bytes.Slice((int)offset, BlobReferenceSize);
            result[i] = new OriginalLevelBlobReference(i, CString(raw[..32]), CString(raw[32..]), raw.ToArray());
            offset += BlobReferenceSize;
        }
        return result;
    }

    private static string CString(ReadOnlySpan<byte> field)
    {
        var end = field.IndexOf((byte)0);
        return Encoding.Latin1.GetString(end < 0 ? field : field[..end]);
    }

    private static byte[] DecodePlane(ReadOnlySpan<byte> bytes, ref long offset, int length, string sourceFile)
    {
        var plane = new byte[length];
        var written = 0;
        while (written < length)
        {
            var runStart = offset;
            if (offset >= bytes.Length)
                throw Fail(sourceFile, OriginalLevelFailure.TruncatedPlane, offset, $"plane ends after {written} of {length} bytes.");
            int run = bytes[(int)offset++];
            if (run == 0)
            {
                if (offset + 2 > bytes.Length)
                    throw Fail(sourceFile, OriginalLevelFailure.TruncatedPlane, offset, "file ends inside a 16-bit run length.");
                run = (bytes[(int)offset] << 8) | bytes[(int)offset + 1];
                offset += 2;
                if (run == 0)
                    throw Fail(sourceFile, OriginalLevelFailure.ZeroLengthRun, runStart, "escaped run length is 0.");
            }
            if (offset >= bytes.Length)
                throw Fail(sourceFile, OriginalLevelFailure.TruncatedPlane, offset, "file ends before a run's value byte.");
            var value = bytes[(int)offset++];
            if (written + run > length)
                throw Fail(sourceFile, OriginalLevelFailure.PlaneOverrun, runStart,
                    $"run of {run} at plane position {written} passes the plane end {length}.");
            plane.AsSpan(written, run).Fill(value);
            written += run;
        }
        return plane;
    }

    private static OriginalLevelFormatException Fail(string file, OriginalLevelFailure failure, long offset, string message) =>
        new(file, failure, offset, message);
}

/// <summary>
/// Queue G6 collision grid: a read-only per-tile view of <see cref="OriginalLevel"/>. Readings follow the emulator
/// (RECONSTRUCTED): physics = Plane2 &amp; 0x1F (0 is clear, anything else blocks), vision = Plane2 &gt;&gt; 5,
/// terrain = TerrainLookupRaw[Plane0 &amp; 0x7F] mod 16, tile size 16 pixels. No movement, bounce or line-of-sight
/// rule is implemented here; height pass-under/over rules (PhysicsLow/High) are exposed raw only.
/// </summary>
public sealed class OriginalCollisionGrid
{
    /// <summary>Pixels per tile in the emulator's lookups (<c>x / 16</c>). RECONSTRUCTED.</summary>
    public const int TileSizePixels = 16;

    private readonly OriginalLevel _level;

    internal OriginalCollisionGrid(OriginalLevel level) => _level = level;

    public int Width => _level.Width;
    public int Height => _level.Height;
    public int PixelWidth => Width * TileSizePixels;
    public int PixelHeight => Height * TileSizePixels;

    public bool InBounds(int tileX, int tileY) => (uint)tileX < (uint)Width && (uint)tileY < (uint)Height;

    /// <summary>Raw physics index 0..31 of a tile.</summary>
    public int PhysicsAt(int tileX, int tileY) => _level.Plane2[Index(tileX, tileY)] & 0x1F;

    /// <summary>Raw vision value 0..7 of a tile.</summary>
    public int VisionAt(int tileX, int tileY) => _level.Plane2[Index(tileX, tileY)] >> 5;

    /// <summary>True when the tile's physics index is not 0 (the emulator's <c>Tile.Blocked</c>).</summary>
    public bool IsBlocked(int tileX, int tileY) => PhysicsAt(tileX, tileY) != 0;

    /// <summary>The CFG terrain number of a tile: TerrainLookupRaw[Plane0 &amp; 0x7F] mod 16 (the emulator applies the modulo on load).</summary>
    public int TerrainAt(int tileX, int tileY)
    {
        var raw = _level.TerrainLookupRaw[_level.Plane0[Index(tileX, tileY)] & 0x7F];
        return ((raw % 16) + 16) % 16;
    }

    /// <summary>The tile under a pixel position; throws when the pixel is outside the map.</summary>
    public (int TileX, int TileY) TileAtPixel(int pixelX, int pixelY)
    {
        if (pixelX < 0 || pixelY < 0 || pixelX >= PixelWidth || pixelY >= PixelHeight)
            throw new ArgumentOutOfRangeException(nameof(pixelX), $"pixel ({pixelX}, {pixelY}) is outside 0..{PixelWidth - 1} × 0..{PixelHeight - 1}.");
        return (pixelX / TileSizePixels, pixelY / TileSizePixels);
    }

    /// <summary>Number of tiles whose physics index is not 0.</summary>
    public int BlockedTileCount()
    {
        var count = 0;
        foreach (var b in _level.Plane2) if ((b & 0x1F) != 0) count++;
        return count;
    }

    private int Index(int tileX, int tileY)
    {
        if (!InBounds(tileX, tileY))
            throw new ArgumentOutOfRangeException(nameof(tileX), $"tile ({tileX}, {tileY}) is outside {Width} × {Height}.");
        return tileY * Width + tileX;
    }
}
