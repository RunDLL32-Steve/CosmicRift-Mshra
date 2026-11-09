using System.Text.Json;
using Godot;

namespace RogueTrader.Client;

/// <summary>
/// The original radar frame staged by scripts/original-assets/stage_ui_art.py: CFG/rogue.cfg [Uiart] RadarBorder = cruiart,newradar,
/// eight pieces in one column of 256 x 256 cells. Originals are never read here; only the derived, git-ignored client/OriginalContent/ui/ folder.
/// </summary>
public sealed class OriginalUiArt
{
    private const string CanonicalCommit = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35";
    private const string ResourceRoot = "res://OriginalContent/ui/";
    public const int RadarPieceCount = 8;

    public sealed class Piece
    {
        public int Frame { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public sealed class Border
    {
        public string Png { get; set; } = "";
        public int CellWidth { get; set; }
        public int CellHeight { get; set; }
        public List<Piece> Pieces { get; set; } = [];
    }

    private sealed class Document
    {
        public int SchemaVersion { get; set; }
        public string CanonicalCommit { get; set; } = "";
        public Border? RadarBorder { get; set; }
    }

    private readonly Border _border;

    private OriginalUiArt(Border border, Texture2D texture)
    {
        _border = border;
        RadarBorderTexture = texture;
    }

    public Texture2D RadarBorderTexture { get; }

    /// <summary>Size in pixels of radar frame piece <paramref name="index"/> (its crop inside the cell).</summary>
    public Vector2 PieceSize(int index) => new(_border.Pieces[index].Width, _border.Pieces[index].Height);

    /// <summary>Atlas region of radar frame piece <paramref name="index"/>: the CFS crop rectangle inside its row's cell.</summary>
    public Rect2 PieceRegion(int index)
    {
        var p = _border.Pieces[index];
        return new Rect2(p.X, p.Frame * _border.CellHeight + p.Y, p.Width, p.Height);
    }

    public static OriginalUiArt? LoadIfAvailable()
    {
        var path = ResourceRoot + "ui.json";
        if (!Godot.FileAccess.FileExists(path)) return null;
        try
        {
            var document = JsonSerializer.Deserialize<Document>(Godot.FileAccess.GetFileAsString(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Invalid original UI art JSON.");
            if (document.SchemaVersion != 1 || document.CanonicalCommit != CanonicalCommit)
                throw new InvalidOperationException("Original UI art schema or canonical commit mismatch.");
            var border = document.RadarBorder ?? throw new InvalidOperationException("Original UI art has no radar frame.");
            if (border.Pieces.Count != RadarPieceCount || border.CellWidth <= 0 || border.CellHeight <= 0 ||
                border.Pieces.Select((p, i) => p.Frame == i && p.Width > 0 && p.Height > 0 && p.X >= 0 && p.Y >= 0 &&
                    p.X + p.Width <= border.CellWidth && p.Y + p.Height <= border.CellHeight).Any(ok => !ok))
                throw new InvalidOperationException("Original radar frame pieces are not the eight expected crops.");
            var relative = border.Png;
            if (string.IsNullOrWhiteSpace(relative) || relative.Contains('\\') || relative.Contains(':') || relative.StartsWith('/') ||
                relative.Split('/').Any(part => part is "" or "." or ".."))
                throw new InvalidOperationException($"Unsafe original resource path: {relative}");
            var texture = GD.Load<Texture2D>(ResourceRoot + relative)
                ?? throw new InvalidOperationException($"Missing original texture: {relative}. Run stage_ui_art.py and import.");
            if (texture.GetWidth() != border.CellWidth || texture.GetHeight() != border.CellHeight * RadarPieceCount)
                throw new InvalidOperationException($"Original atlas dimensions changed: {relative}.");
            GD.Print($"ORIGINAL_UI_ART_PASS: radar frame {border.Pieces.Count} pieces.");
            return new OriginalUiArt(border, texture);
        }
        catch (Exception exception) when (exception is InvalidOperationException or JsonException)
        {
            GD.PushWarning($"Original UI art not used: {exception.Message}");
            return null;
        }
    }
}
