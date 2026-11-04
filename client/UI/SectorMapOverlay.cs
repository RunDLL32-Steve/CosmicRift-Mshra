using Godot;
using RogueTrader.Simulation;

namespace RogueTrader.Client;

/// <summary>
/// Full-screen sector map opened with M (NEW: whether Rogue Trader had a full map screen is unknown, like the radar, G10 H3).
/// It shows the walls and zones of the original Rogue map, the stations, Trading Posts, Repair Docks and refinery, and the
/// player's ship live as it flies. It is a display only: it reads the same world the radar reads and shows nothing the radar
/// does not, and flight keeps running underneath it (the world never pauses for the map in a multiplayer game).
/// Zoom with the mouse wheel or +/- (PageUp/PageDown), drag to pan, double-click or Home to centre on the ship again.
/// </summary>
public partial class SectorMapOverlay : Control
{
    /// <summary>Each zoom step multiplies the scale by this factor.</summary>
    public const float ZoomStep = 1.25f;
    /// <summary>Closest zoom: this many screen pixels per 16-pixel tile.</summary>
    public const float MaxScreenPixelsPerTile = 12;
    private const float Margin = 56;
    private readonly SectorMap? _map;
    private readonly (System.Numerics.Vector2 Position, float DockRadius, string Name)? _station;
    private readonly (System.Numerics.Vector2 Position, float DockRadius, string Name)? _refinery;
    private ImageTexture? _texture;
    private WorldSimulation? _world;
    private float _zoom = 1;
    private System.Numerics.Vector2 _centre;
    private bool _follow = true;
    private bool _dragging;
    private static readonly Color Amber = new("f4b66b");

    public SectorMapOverlay(SectorMap? map, (System.Numerics.Vector2, float, string)? station, (System.Numerics.Vector2, float, string)? refinery)
    {
        _map = map;
        _station = station;
        _refinery = refinery;
        Name = "SectorMap";
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        TextureFilter = TextureFilterEnum.Nearest;
    }

    /// <summary>Trading Posts, Repair Docks and the other teams' stores.</summary>
    public IReadOnlyList<(System.Numerics.Vector2 Position, float DockRadius, string Name, Color Color)> Outposts { get; init; } = [];

    /// <summary>False in the open practice sector, which has no map to show.</summary>
    public bool Available => _map is not null;
    /// <summary>Zoom relative to the whole map fitting the screen (1 = whole map).</summary>
    public float Zoom => _zoom;
    /// <summary>True while the view follows the player's ship.</summary>
    public bool FollowsShip => _follow;

    /// <summary>Opens the map on the whole sector, following the ship. Returns false when the sector has no map.</summary>
    public bool Open()
    {
        if (_map is null) return false;
        _texture ??= MapTexture(_map);
        _follow = true;
        _dragging = false;
        Visible = true;
        QueueRedraw();
        return true;
    }

    public void Close()
    {
        _dragging = false;
        Visible = false;
    }

    public void Update(WorldSimulation world)
    {
        _world = world;
        if (Visible) QueueRedraw();
    }

    /// <summary>Zooms by whole steps (positive = closer), keeping the map point under <paramref name="anchor"/> (screen) still.</summary>
    public void ZoomBy(int steps, Vector2? anchor = null)
    {
        if (_map is null || steps == 0) return;
        var oldScale = ViewScale();
        var maxZoom = Mathf.Max(1, MaxScreenPixelsPerTile / SectorMap.TileSize / FitScale());
        var zoom = Mathf.Clamp(_zoom * Mathf.Pow(ZoomStep, steps), 1, maxZoom);
        if (Mathf.IsEqualApprox(zoom, _zoom)) return;
        if (anchor is { } at && !_follow)
        {
            // Keep the point under the cursor in place; while following, the ship stays centred instead.
            var world = ScreenToWorld(at, oldScale);
            _zoom = zoom;
            var scale = ViewScale();
            _centre = world - new System.Numerics.Vector2(at.X - Size.X / 2, at.Y - Size.Y / 2) / scale;
        }
        else _zoom = zoom;
        if (Mathf.IsEqualApprox(_zoom, 1)) _follow = true;
        QueueRedraw();
    }

    /// <summary>Centres the view on the ship again and follows it.</summary>
    public void CentreOnShip()
    {
        _follow = true;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent input)
    {
        switch (input)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp } wheel:
                ZoomBy(1, wheel.Position);
                AcceptEvent();
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown } wheel:
                ZoomBy(-1, wheel.Position);
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right or MouseButton.Middle } button:
                if (button.DoubleClick) CentreOnShip();
                _dragging = button.Pressed && !button.DoubleClick;
                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragging && _map is not null:
                if (_follow) _centre = ViewCentre();
                _follow = false;
                _centre -= new System.Numerics.Vector2(motion.Relative.X, motion.Relative.Y) / ViewScale();
                QueueRedraw();
                AcceptEvent();
                break;
        }
    }

    public override void _Draw()
    {
        var shade = new Color(0.015f, 0.03f, 0.05f, 0.98f);
        DrawRect(new Rect2(Vector2.Zero, Size), shade);
        if (_map is null || _texture is null) return;
        var scale = ViewScale();
        var origin = Origin(scale);
        var mapSize = new Vector2(_map.PixelWidth, _map.PixelHeight) * scale;
        DrawRect(new Rect2(origin, mapSize), new Color("0b151d"));
        DrawTextureRect(_texture, new Rect2(origin, mapSize), false);
        DrawRect(new Rect2(origin, mapSize), new Color(FlightHud.Teal, 0.45f), false, 1);

        var font = ThemeDB.FallbackFont;
        var labels = scale * SectorMap.TileSize >= 0.9f; // names once the map is zoomed in enough to keep them apart
        Site(_station, Amber, 5, labels, origin, scale);
        Site(_refinery, new Color("e06a5a"), 4, labels, origin, scale);
        foreach (var (position, radius, name, color) in Outposts) Site((position, radius, name), color, 4, labels, origin, scale);
        if (_world is not null)
        {
            var dot = Mathf.Clamp(scale * 24, 1.5f, 4);
            foreach (var pile in _world.Collectibles)
                DrawRect(new Rect2(ToScreen(pile.Position, origin, scale) - Vector2.One * dot / 2, Vector2.One * dot), new Color(Amber, 0.85f));
            foreach (var target in _world.Targets)
                if (!target.IsDestroyed) DrawCircle(ToScreen(target.Position, origin, scale), dot * 0.75f + 0.5f, new Color("d4aa83"));
            var player = _world.Player;
            var at = ToScreen(player.Position, origin, scale);
            var facing = new Vector2(Mathf.Cos(player.Heading), Mathf.Sin(player.Heading));
            var side = new Vector2(-facing.Y, facing.X);
            DrawArc(at, 13, 0, Mathf.Tau, 32, new Color(FlightHud.Teal, 0.55f), 1.5f, true);
            DrawColoredPolygon([at + facing * 11, at - facing * 7 + side * 6, at - facing * 4, at - facing * 7 - side * 6], new Color("e3fff8"));
            // Title and footer bands stay readable over a zoomed-in map.
            DrawRect(new Rect2(0, 0, Size.X, Margin), shade);
            DrawRect(new Rect2(0, Size.Y - Margin + 12, Size.X, Margin), shade);
            var (tileX, tileY) = SectorMap.TileOf(player.Position);
            DrawString(font, new Vector2(Margin, Size.Y - Margin + 30),
                $"SHIP  TILE {tileX}, {tileY}   ·   ZOOM {_zoom:0.0}x{(_follow ? "   ·   FOLLOWING SHIP" : "")}",
                HorizontalAlignment.Left, -1, 13, FlightHud.TextColor);
        }
        DrawString(font, new Vector2(Margin, Margin - 22), "SECTOR MAP (NEW)", HorizontalAlignment.Left, -1, 20, FlightHud.TextColor);
        DrawString(font, new Vector2(Margin, Margin - 6), "Rogue  ·  live position", HorizontalAlignment.Left, -1, 12, FlightHud.Muted);
        DrawString(font, new Vector2(Margin, Size.Y - Margin + 48),
            $"{FlightInput.Display(FlightInput.MapAction)} or Esc close   ·   Wheel or + / - zoom   ·   Drag to pan   ·   Double-click or Home centres on ship",
            HorizontalAlignment.Left, -1, 12, FlightHud.Muted);
        Legend(font);
    }

    private void Legend(Font font)
    {
        var x = Size.X - Margin - 300;
        var y = Margin - 22f;
        (Color Color, string Text)[] rows =
        [
            (Amber, "Team stores"), (WorldView.ServiceColor(RogueTrader.Trading.SiteServices.Trade), "Trading Posts"),
            (WorldView.ServiceColor(RogueTrader.Trading.SiteServices.Repair), "Repair Docks"), (new Color("e06a5a"), "Refinery"),
        ];
        for (var i = 0; i < rows.Length; i++)
        {
            var at = new Vector2(x + i % 2 * 150, y + i / 2 * 16);
            DrawRect(new Rect2(at + new Vector2(0, -8), new Vector2(8, 8)), rows[i].Color);
            DrawString(font, at + new Vector2(13, 0), rows[i].Text, HorizontalAlignment.Left, -1, 12, FlightHud.Muted);
        }
    }

    private void Site((System.Numerics.Vector2 Position, float DockRadius, string Name)? site, Color color, float half, bool label, Vector2 origin, float scale)
    {
        if (site is not { } s) return;
        var at = ToScreen(s.Position, origin, scale);
        var ring = s.DockRadius * scale;
        if (ring > half + 2) DrawArc(at, ring, 0, Mathf.Tau, 40, new Color(color, 0.5f), 1, true);
        DrawRect(new Rect2(at - Vector2.One * half, Vector2.One * half * 2), color);
        if (label) DrawString(ThemeDB.FallbackFont, at + new Vector2(half + 4, 4), s.Name.ToUpperInvariant(), HorizontalAlignment.Left, -1, 11, color);
    }

    /// <summary>Screen pixels per world unit when the whole map fits inside the margins.</summary>
    private float FitScale() =>
        _map is null ? 1 : Mathf.Max(0.0001f, Mathf.Min((Size.X - Margin * 2) / _map.PixelWidth, (Size.Y - Margin * 2 - 30) / _map.PixelHeight));

    private float ViewScale() => FitScale() * _zoom;

    private System.Numerics.Vector2 ViewCentre()
    {
        var map = _map!;
        var mapCentre = new System.Numerics.Vector2(map.PixelWidth / 2, map.PixelHeight / 2);
        if (_zoom <= 1.0001f) return mapCentre;
        var wanted = _follow && _world is not null ? _world.Player.Position : _centre;
        // Never scroll the map off the screen: the centre stays inside the map.
        return new(Mathf.Clamp(wanted.X, 0, map.PixelWidth), Mathf.Clamp(wanted.Y, 0, map.PixelHeight));
    }

    private Vector2 Origin(float scale)
    {
        var centre = ViewCentre();
        if (!_follow) _centre = centre;
        return new Vector2(Size.X / 2, Size.Y / 2 - 15) - new Vector2(centre.X, centre.Y) * scale;
    }

    private static Vector2 ToScreen(System.Numerics.Vector2 world, Vector2 origin, float scale) => origin + new Vector2(world.X, world.Y) * scale;

    private System.Numerics.Vector2 ScreenToWorld(Vector2 screen, float scale)
    {
        var origin = Origin(scale);
        return new((screen.X - origin.X) / scale, (screen.Y - origin.Y) / scale);
    }

    /// <summary>One texture pixel per tile: walls, and the store, trade and repair zones tinted more strongly than in flight.</summary>
    private static ImageTexture MapTexture(SectorMap map)
    {
        var bytes = new byte[map.Width * map.Height * 4];
        var palette = new byte[256][];
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
        {
            var tile = map.TileAt(x, y);
            var rgba = palette[tile] ??= WorldView.ToRgba((tile & SectorMap.BlockedBit) != 0 ? new Color("5d7889") : WorldView.TileColor(tile) * new Color(1, 1, 1, 4));
            rgba.CopyTo(bytes, (y * map.Width + x) * 4);
        }
        return ImageTexture.CreateFromImage(Image.CreateFromData(map.Width, map.Height, false, Image.Format.Rgba8, bytes));
    }
}
