using Godot;
using RogueTrader.GameData;
using RogueTrader.Simulation;

namespace RogueTrader.Client;

/// <summary>
/// Radar: the whole sector map in the bottom-right corner, with the player, the stations, Trading Posts, Repair Docks and refinery, the practice targets
/// and the mineral drops. Each radar pixel covers 8 x 8 tiles and shows a wall when any of them blocks.
/// ORIGINAL: the player is drawn in CFG [Radar] ColorSelf and the practice targets in ColorEnemy (data/ui/radar.json), and the frame is the
/// [Uiart] RadarBorder art (cruiart,newradar) when it is staged. RECONSTRUCTED: how the frame pieces join. NEW: the whole-map view, the
/// station/outpost/mineral marks and the frame used when the art is not staged; the original range, zoom and line-of-sight rules are unknown (G10 H3).
/// </summary>
public partial class MinimapView : Control
{
    private const int TilesPerPixel = 8;
    private const float Side = 210;
    private readonly SectorMap? _map;
    private readonly ImageTexture? _texture;
    private readonly (System.Numerics.Vector2 Position, float DockRadius, string Name)? _station;
    private readonly (System.Numerics.Vector2 Position, float DockRadius, string Name)? _refinery;
    private readonly OriginalUiArt? _art;
    private readonly Color _self;
    private readonly Color _enemy;
    private WorldSimulation? _world;
    private static readonly Color Amber = new("f4b66b");

    public MinimapView(SectorMap? map, (System.Numerics.Vector2, float, string)? station, (System.Numerics.Vector2, float, string)? refinery,
        RadarLook look, OriginalUiArt? art)
    {
        _map = map;
        _art = art;
        _self = Rgb(look.SelfRgb);
        _enemy = Rgb(look.EnemyRgb);
        _station = station;
        _refinery = refinery;
        Name = "Radar";
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = map is not null;
        if (map is null) return;
        _texture = RadarTexture(map);
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        OffsetLeft = -Side - 22;
        OffsetRight = -22;
        OffsetTop = -Side - 74;
        OffsetBottom = -74;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    /// <summary>Trading Posts, Repair Docks and the other teams' stores.</summary>
    public IReadOnlyList<(System.Numerics.Vector2 Position, float DockRadius, string Name, Color Color)> Outposts { get; init; } = [];

    public void Update(WorldSimulation world)
    {
        _world = world;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_map is null || _texture is null) return;
        var size = new Vector2(Side, Side);
        DrawRect(new Rect2(Vector2.Zero, size), new Color("0b151dd8"));
        DrawTextureRect(_texture, new Rect2(Vector2.Zero, size), false);
        if (_art is not null) DrawOriginalFrame(_art);
        else
        {
            DrawRect(new Rect2(Vector2.Zero, size), new Color("62dfd1", 0.45f), false, 1);
            DrawString(ThemeDB.FallbackFont, new Vector2(0, -5), "RADAR (NEW frame)", HorizontalAlignment.Left, -1, 11, FlightHud.Muted);
        }
        Mark(_station, Amber, 3);
        Mark(_refinery, new Color("e06a5a"), 3);
        foreach (var (position, radius, name, color) in Outposts) Mark((position, radius, name), color, 2.5f);
        if (_world is null) return;
        foreach (var pile in _world.Collectibles) DrawRect(new Rect2(ToRadar(pile.Position) - Vector2.One, Vector2.One * 2), new Color(Amber, 0.8f));
        foreach (var target in _world.Targets)
            if (!target.IsDestroyed) DrawCircle(ToRadar(target.Position), 1.5f, _enemy);
        var player = _world.Player;
        var at = ToRadar(player.Position);
        DrawLine(at, at + new Vector2(Mathf.Cos(player.Heading), Mathf.Sin(player.Heading)) * 7, _self, 1.5f, true);
        DrawCircle(at, 2.5f, _self);
    }

    /// <summary>
    /// RECONSTRUCTED layout of the eight newradar pieces around the map square: piece 0 (the RADAR plate) starts the title bar, piece 1 fills
    /// it, piece 2 caps it; piece 5 runs down both sides, piece 6 along the bottom, and piece 3 (the pipe) sits on the lower-right corner.
    /// Pieces 4 and 7 are staged but not placed, because nothing shows where they belong.
    /// </summary>
    private void DrawOriginalFrame(OriginalUiArt art)
    {
        var tex = art.RadarBorderTexture;
        void Piece(int index, Vector2 at) => DrawTextureRectRegion(tex, new Rect2(at, art.PieceSize(index)), art.PieceRegion(index));
        var side = art.PieceSize(5);
        var bottom = art.PieceSize(6);
        var plate = art.PieceSize(0);
        var fill = art.PieceSize(1);
        var cap = art.PieceSize(2);
        float left = -side.X, right = Side + side.X, top = -plate.Y;
        for (var y = 0f; y < Side; y += side.Y)
        {
            var h = Math.Min(side.Y, Side - y);
            DrawTextureRectRegion(tex, new Rect2(left, y, side.X, h), new Rect2(art.PieceRegion(5).Position, new Vector2(side.X, h)));
            DrawTextureRectRegion(tex, new Rect2(Side, y, side.X, h), new Rect2(art.PieceRegion(5).Position, new Vector2(side.X, h)));
        }
        for (var x = left; x < right; x += bottom.X)
        {
            var w = Math.Min(bottom.X, right - x);
            DrawTextureRectRegion(tex, new Rect2(x, Side, w, bottom.Y), new Rect2(art.PieceRegion(6).Position, new Vector2(w, bottom.Y)));
        }
        for (var x = left + plate.X; x < right - cap.X; x += fill.X)
        {
            var w = Math.Min(fill.X, right - cap.X - x);
            DrawTextureRectRegion(tex, new Rect2(x, top, w, fill.Y), new Rect2(art.PieceRegion(1).Position, new Vector2(w, fill.Y)));
        }
        Piece(2, new Vector2(right - cap.X, top + (plate.Y - cap.Y) / 2));
        Piece(0, new Vector2(left, top));
        var pipe = art.PieceSize(3);
        Piece(3, new Vector2(right, Side + bottom.Y) - pipe);
    }

    private static Color Rgb(int rgb) => new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

    private void Mark((System.Numerics.Vector2 Position, float DockRadius, string Name)? site, Color color, float radius)
    {
        if (site is not { } s) return;
        var at = ToRadar(s.Position);
        DrawRect(new Rect2(at - Vector2.One * radius, Vector2.One * radius * 2), color);
    }

    private Vector2 ToRadar(System.Numerics.Vector2 world) =>
        new(Mathf.Clamp(world.X / _map!.PixelWidth, 0, 1) * Side, Mathf.Clamp(world.Y / _map.PixelHeight, 0, 1) * Side);

    private static ImageTexture RadarTexture(SectorMap map)
    {
        int width = (map.Width + TilesPerPixel - 1) / TilesPerPixel, height = (map.Height + TilesPerPixel - 1) / TilesPerPixel;
        var bytes = new byte[width * height * 4];
        for (var py = 0; py < height; py++)
        for (var px = 0; px < width; px++)
        {
            byte shown = 0;
            for (var ty = py * TilesPerPixel; ty < Math.Min(map.Height, (py + 1) * TilesPerPixel) && (shown & SectorMap.BlockedBit) == 0; ty++)
            for (var tx = px * TilesPerPixel; tx < Math.Min(map.Width, (px + 1) * TilesPerPixel); tx++)
            {
                var tile = map.TileAt(tx, ty);
                if ((tile & SectorMap.BlockedBit) != 0) { shown = tile; break; }
                if ((tile & 0x0F) != 0) shown = tile;
            }
            var color = (shown & SectorMap.BlockedBit) != 0 ? new Color("5d7889") : WorldView.TileColor(shown) * new Color(1, 1, 1, 4);
            WorldView.ToRgba(color).CopyTo(bytes, (py * width + px) * 4);
        }
        return ImageTexture.CreateFromImage(Image.CreateFromData(width, height, false, Image.Format.Rgba8, bytes));
    }
}
