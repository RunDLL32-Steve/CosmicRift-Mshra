using System.Text.Json;
using Godot;

namespace RogueTrader.Client;

/// <summary>
/// The original Rogue map art staged by scripts/original-assets/stage_level_art.py: every sprite Rogue.lvl places
/// (asteroids, space stations, walls, gas clouds, map border) and the mineral prize sprites named by Rogue_mp.itm.
/// Originals are never read here; only the derived, git-ignored client/OriginalContent/level/ folder.
/// Entity fields follow the Gibbed LVL format source; the top-left anchor, the animation unit (milliseconds per frame),
/// the strength of smart translucency and the Value brightness scale are RECONSTRUCTED; see the staging script.
/// </summary>
public sealed class OriginalLevelArt
{
    public sealed class Sprite
    {
        public int Index { get; set; }
        public string ItemId { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string Archive { get; set; } = "";
        public string Member { get; set; } = "";
        public string Png { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public int FrameCount { get; set; }
        public int AnimationTime { get; set; }
    }

    private sealed class Document
    {
        public int SchemaVersion { get; set; }
        public string CanonicalCommit { get; set; } = "";
        public List<Sprite> Objects { get; set; } = [];
        public List<Sprite> Minerals { get; set; } = [];
        public int EntityStride { get; set; }
        public int[] Entities { get; set; } = [];
    }

    /// <summary>
    /// One placed sprite: cell top-left in map pixels, object index, starting frame, milliseconds per frame (0 = still), the raw
    /// TimeDelta, LVL draw order (0-5; 5 = dynamic last, drawn over ships), translucency (0 opaque, 1 = 50%, 2 = 33%, 3 = 25%),
    /// the smart translucency flag, and the entity's signed Value and Saturation, Hue, light and palette shift.
    /// </summary>
    public readonly record struct Entity(int X, int Y, int Object, int Frame, int AnimationTime, int TimeDelta, int DrawOrder, int Translucency,
        bool SmartTranslucency, int Value, int Saturation, int Hue, int Light, int PaletteShift)
    {
        /// <summary>True for sprites the original draws after ships (Rogue.lvl: the translucent gas clouds).</summary>
        public bool OverShips => DrawOrder == DynamicLast;

        /// <summary>
        /// Opacity the sprite is drawn with. Smart translucency (every gas cloud except gasCloud4sm_) has no strength in the format
        /// source; 50%, the first translucency step, is RECONSTRUCTED.
        /// </summary>
        public float Opacity => Translucency switch
        {
            1 => 0.5f,
            2 => 0.33f,
            3 => 0.25f,
            _ => SmartTranslucency ? SmartOpacity : 1f,
        };

        /// <summary>
        /// Colour the sprite is drawn with: its opacity, and its Value as a brightness change in percent (Rogue.lvl: 927 gas clouds
        /// carry -10). The percent scale is RECONSTRUCTED. Hue, Saturation and light are kept but not applied; their scale is unknown.
        /// </summary>
        public Color Modulate
        {
            get
            {
                var brightness = Math.Clamp(1f + Value / 100f, 0f, 1f);
                return new Color(brightness, brightness, brightness, Opacity);
            }
        }
    }

    public const int DynamicLast = 5;
    public const float SmartOpacity = 0.5f;

    private const string CanonicalCommit = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35";
    private const string ResourceRoot = "res://OriginalContent/level/";
    /// <summary>Bucket size for culling; larger than the biggest sprite (288-pixel station) so a sprite is found from its neighbours.</summary>
    private const int Bucket = 512;
    private const int SchemaVersion = 3;
    private const int Stride = 14;
    /// <summary>Frame time for simulation-placed rocks: 250 ms, the time Rogue.lvl gives most of its rocks (8,839 of 13,790).</summary>
    private const int PracticeRockFrameMs = 250;

    private readonly Document _document;
    private readonly Dictionary<long, List<int>> _buckets = [];
    private readonly Dictionary<string, Sprite> _minerals;
    private readonly Texture2D?[] _objectTextures;
    private readonly Dictionary<string, Texture2D> _mineralTextures = [];
    public IReadOnlyList<Sprite> Objects => _document.Objects;
    public Entity[] Entities { get; }

    private OriginalLevelArt(Document document)
    {
        if (document.SchemaVersion != SchemaVersion || document.CanonicalCommit != CanonicalCommit || document.EntityStride != Stride ||
            document.Entities.Length % Stride != 0 || document.Objects.Count == 0)
            throw new InvalidOperationException("Original level art schema or canonical commit mismatch; re-run stage_level_art.py.");
        foreach (var sprite in document.Objects.Concat(document.Minerals))
            if (sprite.Width <= 0 || sprite.Height <= 0 || sprite.RowCount <= 0 || sprite.ColumnCount <= 0)
                throw new InvalidOperationException($"Invalid original sprite layout: {sprite.Archive}::{sprite.Member}");
        _document = document;
        _objectTextures = new Texture2D?[document.Objects.Count];
        _minerals = document.Minerals.ToDictionary(m => m.ItemId, StringComparer.Ordinal);
        Entities = new Entity[document.Entities.Length / Stride];
        for (var i = 0; i < Entities.Length; i++)
        {
            var v = document.Entities.AsSpan(i * Stride, Stride);
            if ((uint)v[2] >= (uint)document.Objects.Count) throw new InvalidOperationException($"Entity {i} names an unknown object.");
            if ((uint)v[3] > 0x7F || (uint)v[4] > 0x3FF || (uint)v[5] > 0x1FFF || (uint)v[6] > 7 || (uint)v[7] > 3 || (uint)v[8] > 1 ||
                v[9] is < -128 or > 127 || v[10] is < -128 or > 127 || (uint)v[11] > 0x7F || (uint)v[12] > 0x7F || (uint)v[13] > 0x3F)
                throw new InvalidOperationException($"Entity {i} has invalid draw fields.");
            Entities[i] = new Entity(v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8] == 1, v[9], v[10], v[11], v[12], v[13]);
            var key = BucketKey(Math.DivRem(v[0], Bucket).Quotient, Math.DivRem(v[1], Bucket).Quotient);
            if (!_buckets.TryGetValue(key, out var list)) _buckets[key] = list = [];
            list.Add(i);
        }
    }

    public static OriginalLevelArt? LoadIfAvailable()
    {
        var path = ResourceRoot + "level.json";
        if (!Godot.FileAccess.FileExists(path)) return null;
        try
        {
            var document = JsonSerializer.Deserialize<Document>(Godot.FileAccess.GetFileAsString(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("Invalid original level art JSON.");
            // Every atlas is loaded here, so a partly staged or unimported folder falls back to the NEW look
            // instead of failing inside _Draw.
            var art = new OriginalLevelArt(document);
            art.VerifyPackagedResources();
            return art;
        }
        catch (Exception exception) when (exception is InvalidOperationException or JsonException)
        {
            GD.PushWarning($"Original map art not used: {exception.Message}");
            return null;
        }
    }

    /// <summary>
    /// Indices of the entities whose cell can overlap <paramref name="view"/>, in file (draw) order: those drawn under ships,
    /// or with <paramref name="overShips"/> those the original draws after them.
    /// </summary>
    public List<int> Visible(Rect2 view, bool overShips = false)
    {
        var result = new List<int>();
        var x0 = (int)Math.Floor(view.Position.X / Bucket) - 1;
        var y0 = (int)Math.Floor(view.Position.Y / Bucket) - 1;
        var x1 = (int)Math.Floor(view.End.X / Bucket);
        var y1 = (int)Math.Floor(view.End.Y / Bucket);
        for (var by = y0; by <= y1; by++)
        for (var bx = x0; bx <= x1; bx++)
        {
            if (!_buckets.TryGetValue(BucketKey(bx, by), out var list)) continue;
            foreach (var index in list)
            {
                var e = Entities[index];
                if (e.OverShips != overShips) continue;
                var sprite = _document.Objects[e.Object];
                if (view.Intersects(new Rect2(e.X, e.Y, sprite.Width, sprite.Height))) result.Add(index);
            }
        }
        result.Sort();
        return result;
    }

    /// <summary>Draws one placed sprite at its original position, animated through its frames.</summary>
    public void Draw(CanvasItem canvas, int index, float seconds)
    {
        var e = Entities[index];
        var sprite = _document.Objects[e.Object];
        var texture = _objectTextures[e.Object] ??= LoadTexture(sprite);
        // The entity's own cycle decides whether it moves: safe-zone markings and the map border carry 0 and stay still.
        var frame = (e.Frame + FrameAt(sprite.FrameCount, e.AnimationTime, 0, seconds)) % sprite.FrameCount;
        canvas.DrawTextureRectRegion(texture, new Rect2(e.X, e.Y, sprite.Width, sprite.Height), FrameRect(sprite, frame), e.Modulate);
    }

    /// <summary>Draws a mineral's original prize sprite centred on <paramref name="centre"/>; false when the item has none staged.</summary>
    public bool DrawMineral(CanvasItem canvas, string itemId, Vector2 centre, float seconds)
    {
        if (!_minerals.TryGetValue(itemId, out var sprite)) return false;
        if (!_mineralTextures.TryGetValue(itemId, out var texture)) _mineralTextures[itemId] = texture = LoadTexture(sprite);
        var size = new Vector2(sprite.Width, sprite.Height);
        var frame = FrameAt(sprite.FrameCount, sprite.AnimationTime, 0, seconds);
        canvas.DrawTextureRectRegion(texture, new Rect2(centre - size / 2, size), FrameRect(sprite, frame));
        return true;
    }

    /// <summary>The original rock (O_Asteroids RockA..RockI) whose cell best fits a practice asteroid of this radius.</summary>
    public int? RockFor(float radius, int seed)
    {
        var rocks = _document.Objects.Where(o => o.Archive == "BLO/O_Asteroids.blo").ToArray();
        if (rocks.Length == 0) return null;
        var best = rocks.Min(r => Math.Abs(r.Width - radius * 2));
        var fits = rocks.Where(r => Math.Abs(r.Width - radius * 2) == best).ToArray();
        return fits[(int)((uint)seed % (uint)fits.Length)].Index;
    }

    /// <summary>Draws object <paramref name="objectIndex"/> centred on <paramref name="centre"/> (for simulation-placed rocks).</summary>
    public void DrawObjectCentred(CanvasItem canvas, int objectIndex, Vector2 centre, float seconds, int phase)
    {
        var sprite = _document.Objects[objectIndex];
        var texture = _objectTextures[objectIndex] ??= LoadTexture(sprite);
        var size = new Vector2(sprite.Width, sprite.Height);
        canvas.DrawTextureRectRegion(texture, new Rect2(centre - size / 2, size),
            FrameRect(sprite, FrameAt(sprite.FrameCount, PracticeRockFrameMs, phase, seconds)));
    }

    /// <summary>Tiles (16 px) that some placed sprite's cell overlaps, row-major; the same test stage_level_art.py reports.</summary>
    public bool[] CoverageMask(int width, int height)
    {
        var covered = new bool[width * height];
        foreach (var e in Entities)
        {
            var sprite = _document.Objects[e.Object];
            int x0 = Math.Max(0, e.X / 16), y0 = Math.Max(0, e.Y / 16);
            int x1 = Math.Min(width - 1, (e.X + sprite.Width - 1) / 16), y1 = Math.Min(height - 1, (e.Y + sprite.Height - 1) / 16);
            for (var y = y0; y <= y1; y++)
                Array.Fill(covered, true, y * width + x0, Math.Max(0, x1 - x0 + 1));
        }
        return covered;
    }

    /// <summary>Loads every staged atlas once and checks its dimensions.</summary>
    public void VerifyPackagedResources()
    {
        for (var i = 0; i < _objectTextures.Length; i++) _objectTextures[i] ??= LoadTexture(_document.Objects[i]);
        foreach (var mineral in _document.Minerals) _mineralTextures.TryAdd(mineral.ItemId, LoadTexture(mineral));
        GD.Print($"ORIGINAL_LEVEL_ART_PASS: {_objectTextures.Length} objects; {_document.Minerals.Count} mineral sprites; {Entities.Length} placed sprites.");
    }

    /// <summary>
    /// Frame shown at <paramref name="seconds"/> when each frame lasts <paramref name="frameMilliseconds"/> (entity and CFS
    /// AnimationTime, RECONSTRUCTED as milliseconds per frame); 0 holds the first frame. <paramref name="phasePercent"/> offsets
    /// the start by a share of the cycle (only simulation-placed rocks use it; Rogue.lvl has no per-entity phase).
    /// </summary>
    public static int FrameAt(int frameCount, int frameMilliseconds, int phasePercent, float seconds)
    {
        if (frameCount <= 1 || frameMilliseconds <= 0) return 0;
        var cycles = seconds * 1000f / ((float)frameMilliseconds * frameCount) + phasePercent / 100f;
        var frame = (int)Math.Floor((cycles - Math.Floor(cycles)) * frameCount);
        return Math.Clamp(frame, 0, frameCount - 1);
    }

    private static Rect2 FrameRect(Sprite sprite, int frame) =>
        new((frame % sprite.ColumnCount) * sprite.Width, (frame / sprite.ColumnCount % sprite.RowCount) * sprite.Height, sprite.Width, sprite.Height);

    private static long BucketKey(int x, int y) => ((long)x << 32) ^ (uint)y;

    private static Texture2D LoadTexture(Sprite sprite)
    {
        var relative = sprite.Png;
        if (string.IsNullOrWhiteSpace(relative) || relative.Contains('\\') || relative.Contains(':') || relative.StartsWith('/') ||
            relative.Split('/').Any(part => part is "" or "." or ".."))
            throw new InvalidOperationException($"Unsafe original resource path: {relative}");
        var texture = GD.Load<Texture2D>(ResourceRoot + relative)
            ?? throw new InvalidOperationException($"Missing original texture: {relative}. Run stage_level_art.py and import.");
        if (texture.GetWidth() != sprite.Width * sprite.ColumnCount || texture.GetHeight() != sprite.Height * sprite.RowCount)
            throw new InvalidOperationException($"Original atlas dimensions changed: {relative}.");
        return texture;
    }
}
