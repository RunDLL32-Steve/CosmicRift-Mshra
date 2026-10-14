using System.Text.Json;
using Godot;

namespace RogueTrader.Client;

/// <summary>Read-only derived views of canonical archives; never interprets gameplay values.</summary>
public sealed class OriginalSpriteLibrary
{
    public sealed class Sprite
    {
        public string AssetKey { get; set; } = "";
        public string ObjectName { get; set; } = "";
        public string ArchiveName { get; set; } = "";
        public string SourceSha256 { get; set; } = "";
        public string AtlasPng { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public int FrameCount { get; set; }
        public int BlitMode { get; set; }
        public int ShadowCount { get; set; }
    }

    public sealed class Hull
    {
        public string VehicleId { get; set; } = "";
        public string Name { get; set; } = "";
        public string AssetKey { get; set; } = "";
    }

    public sealed class MapInfo
    {
        public string SourcePath { get; set; } = "";
        public string SourceSha256 { get; set; } = "";
        public string Png { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
    }

    private sealed class Manifest
    {
        public int SchemaVersion { get; set; }
        public string CanonicalCommit { get; set; } = "";
        public List<Sprite> Sprites { get; set; } = [];
        public List<Hull> Ships { get; set; } = [];
        public MapInfo? Map { get; set; }
    }

    private const string CanonicalCommit = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35";
    private const string ResourceRoot = "res://OriginalContent/";
    private readonly Dictionary<string, Texture2D> _textures = [];
    private readonly Dictionary<string, Sprite> _byKey;
    public IReadOnlyList<Sprite> Sprites { get; }
    public IReadOnlyList<Hull> Hulls { get; }
    public MapInfo Map { get; }

    private OriginalSpriteLibrary(Manifest manifest)
    {
        if (manifest.SchemaVersion != 1 || manifest.CanonicalCommit != CanonicalCommit)
            throw new InvalidOperationException("Original content manifest schema or canonical commit mismatch.");
        if (manifest.Ships.Count != 12 || manifest.Sprites.Count == 0 || manifest.Map is null)
            throw new InvalidOperationException("Original content manifest requires 12 hulls, decoded sprites and Rogue.bmp.");
        _byKey = new(StringComparer.Ordinal);
        foreach (var sprite in manifest.Sprites)
        {
            if (sprite.Width <= 0 || sprite.Height <= 0 || sprite.RowCount <= 0 || sprite.ColumnCount <= 0 ||
                (long)sprite.RowCount * sprite.ColumnCount != sprite.FrameCount)
                throw new InvalidOperationException($"Invalid original sprite layout: {sprite.AssetKey}");
            SafeResourcePath(sprite.AtlasPng);
            if (sprite.SourceSha256.Length != 64)
                throw new InvalidOperationException($"Missing original provenance: {sprite.AssetKey}");
            if (!_byKey.TryAdd(sprite.AssetKey, sprite)) throw new InvalidOperationException($"Duplicate original asset: {sprite.AssetKey}");
        }
        foreach (var hull in manifest.Ships)
            if (!_byKey.ContainsKey(hull.AssetKey)) throw new InvalidOperationException($"Unresolved hull sprite: {hull.Name}");
        if (manifest.Ships.Select(hull => hull.VehicleId).Distinct().Count() != 12)
            throw new InvalidOperationException("Duplicate original vehicle identity.");
        Sprites = manifest.Sprites.OrderBy(sprite => sprite.ArchiveName).ThenBy(sprite => sprite.ObjectName).ToArray();
        Hulls = manifest.Ships.OrderBy(hull => hull.Name).ToArray();
        Map = manifest.Map;
        SafeResourcePath(Map.Png);
        if (Map.Width <= 0 || Map.Height <= 0 || Map.SourceSha256.Length != 64)
            throw new InvalidOperationException("Missing original map dimensions/provenance.");
    }

    public static OriginalSpriteLibrary? LoadIfAvailable()
    {
        var path = ResourceRoot + "runtime.json";
        if (!Godot.FileAccess.FileExists(path)) return null;
        var manifest = JsonSerializer.Deserialize<Manifest>(Godot.FileAccess.GetFileAsString(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Invalid original content JSON.");
        return new OriginalSpriteLibrary(manifest);
    }

    public Sprite Find(string assetKey) => _byKey[assetKey];
    public Sprite HullSprite(string hullName) => Find(Hulls.Single(hull => hull.Name == hullName).AssetKey);
    public Texture2D Texture(Sprite sprite) => LoadTexture(sprite.AtlasPng, sprite.Width * sprite.ColumnCount, sprite.Height * sprite.RowCount);
    public Texture2D MapTexture() => LoadTexture(Map.Png, Map.Width, Map.Height);

    public Rect2 Frame(Sprite sprite, int row, int column)
    {
        if (row < 0 || row >= sprite.RowCount || column < 0 || column >= sprite.ColumnCount)
            throw new ArgumentOutOfRangeException(nameof(row), "Original sprite frame is outside its archive layout.");
        return new Rect2(column * sprite.Width, row * sprite.Height, sprite.Width, sprite.Height);
    }

    public static int DirectionRow(float heading, int rows = 64)
    {
        // Observed rows: 0 up, 16 right, 32 down, 48 left; simulation zero points right.
        var turns = (heading + Mathf.Pi / 2) / Mathf.Tau;
        return ((int)MathF.Round(turns * rows, MidpointRounding.AwayFromZero) % rows + rows) % rows;
    }

    public void VerifyPackagedResources()
    {
        foreach (var sprite in Sprites) Texture(sprite);
        MapTexture();
        foreach (var hull in Hulls)
        {
            var sprite = Find(hull.AssetKey);
            if (sprite.RowCount != 64 || sprite.ColumnCount != 11)
                throw new InvalidOperationException($"Unexpected original hull direction/state layout: {hull.Name}");
            Frame(sprite, 0, 0);
            Frame(sprite, 63, 10);
        }
        GD.Print($"ORIGINAL_RESOURCES_PASS: {Hulls.Count} hull identities; {Sprites.Count} atlases; {Map.Width}x{Map.Height} Rogue.bmp; unchanged canonical commit.");
    }

    private Texture2D LoadTexture(string relativePath, int width, int height)
    {
        if (_textures.TryGetValue(relativePath, out var texture)) return texture;
        texture = GD.Load<Texture2D>(SafeResourcePath(relativePath))
            ?? throw new InvalidOperationException($"Missing original texture: {relativePath}. Stage and import original resources.");
        if (texture.GetWidth() != width || texture.GetHeight() != height)
            throw new InvalidOperationException($"Original atlas dimensions changed: {relativePath}.");
        _textures.Add(relativePath, texture);
        return texture;
    }

    private static string SafeResourcePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains('\\') || relativePath.Contains(':') ||
            relativePath.StartsWith('/') || relativePath.Split('/').Any(part => part is "" or "." or ".."))
            throw new InvalidOperationException($"Unsafe original resource path: {relativePath}");
        return ResourceRoot + relativePath;
    }
}
