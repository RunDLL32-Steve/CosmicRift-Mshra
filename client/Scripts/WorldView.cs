using Godot;
using RogueTrader.Simulation;

namespace RogueTrader.Client;

/// <summary>Canonical raw ship pixels and original map art where staged; provisional range/effects remain engine independent.</summary>
public partial class WorldView : Node2D
{
    private readonly record struct Burst(Vector2 Position, Color Color, float Age, float Duration);
    private readonly List<Burst> _bursts = [];
    private readonly Dictionary<string, (float Hull, float Shield, bool Destroyed)> _previous = [];
    private WorldSimulation? _world;
    private float _time;
    public ShipInput InputState { get; set; }
    public OriginalSpriteLibrary? Originals { get; set; }
    /// <summary>The atlas bound by the most recent player draw, or null for the geometric fallback (bounded render QA).</summary>
    internal string? PlayerAtlasPath { get; private set; }
    /// <summary>Original Rogue.lvl sprites (rocks, stations, walls) and mineral prize sprites; null draws the NEW tile/marker look.</summary>
    public OriginalLevelArt? LevelArt
    {
        get => _levelArt;
        set
        {
            _levelArt = value;
            _mapTexture = _map is null ? null : TileTexture(_map, value);
        }
    }
    private OriginalLevelArt? _levelArt;
    /// <summary>Provisional station marker (queue M8). With <see cref="LevelArt"/> staged, the original station sprites stand on the map and only the name and dock ring are drawn here.</summary>
    public (System.Numerics.Vector2 Position, float DockRadius, string Name)? Station { get; set; }
    /// <summary>Provisional refinery marker (queue M7 mining slice), drawn like the station.</summary>
    public (System.Numerics.Vector2 Position, float DockRadius, string Name)? Refinery { get; set; }
    /// <summary>Trading Posts, Repair Docks and the other teams' stores, drawn like the station in their service's colour.</summary>
    public IReadOnlyList<(System.Numerics.Vector2 Position, float DockRadius, string Name, Color Color)> Outposts { get; set; } = [];

    /// <summary>Marker colour of a site's service: amber stores, teal Trading Posts, blue Repair Docks, red refineries.</summary>
    public static Color ServiceColor(string service) => service switch
    {
        RogueTrader.Trading.SiteServices.Trade => new Color("62dfd1"),
        RogueTrader.Trading.SiteServices.Repair => new Color("7fb6ff"),
        RogueTrader.Trading.SiteServices.Refine => new Color("e06a5a"),
        _ => Amber,
    };

    /// <summary>The sector's walls and terrain, drawn one texture pixel per 16-pixel tile; null for the open practice sector.</summary>
    public SectorMap? Map
    {
        get => _map;
        set
        {
            _map = value;
            _mapTexture = value is null ? null : TileTexture(value, _levelArt);
        }
    }
    /// <summary>Where the player spawns; the launch ring is drawn there.</summary>
    public System.Numerics.Vector2 Spawn { get; set; }
    private SectorMap? _map;
    private ImageTexture? _mapTexture;
    private static readonly Color Teal = new("62dfd1");
    private static readonly Color Amber = new("f4b66b");

    public void SetWorld(WorldSimulation world)
    {
        _world = world;
        _previous.Clear();
        _bursts.Clear();
        ObserveShips();
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        for (var i = _bursts.Count - 1; i >= 0; i--)
        {
            var burst = _bursts[i];
            _bursts[i] = burst with { Age = burst.Age + (float)delta };
            if (_bursts[i].Age >= burst.Duration) _bursts.RemoveAt(i);
        }
        ObserveShips();
        QueueRedraw();
    }

    private void ObserveShips()
    {
        if (_world is null) return;
        foreach (var ship in _world.Targets.Prepend(_world.Player))
        {
            if (_previous.TryGetValue(ship.Id, out var before))
            {
                if (!before.Destroyed && ship.IsDestroyed)
                    _bursts.Add(new(ToGodot(ship.Position), Amber, 0, 0.9f));
                else if (ship.Shield < before.Shield || ship.Hull < before.Hull)
                    _bursts.Add(new(ToGodot(ship.Position), ship.Shield > 0 ? Teal : Amber, 0, 0.22f));
            }
            _previous[ship.Id] = (ship.Hull, ship.Shield, ship.IsDestroyed);
        }
    }

    public override void _Draw()
    {
        if (_world is null) return;
        var player = ToGodot(_world.Player.Position);
        DrawStars(player);
        if (_mapTexture is not null && _map is not null)
            DrawTextureRect(_mapTexture, new Rect2(Vector2.Zero, new Vector2(_map.PixelWidth, _map.PixelHeight)), false);
        if (_levelArt is not null && _map is not null)
            foreach (var index in _levelArt.Visible(VisibleWorldRect())) _levelArt.Draw(this, index, _time);
        DrawOrigin();
        DrawStation(Station, Amber);
        DrawStation(Refinery, new Color("e06a5a"));
        foreach (var (position, radius, name, color) in Outposts) DrawStation((position, radius, name), color);
        foreach (var pile in _world.Collectibles)
            if (pile.Id.StartsWith(WorldSimulation.MineralPilePrefix, StringComparison.Ordinal)) DrawMineral(pile.ItemId, ToGodot(pile.Position), pile.Radius);
            else DrawPile(ToGodot(pile.Position), Mathf.Min(pile.Radius, 24));
        foreach (var rock in _world.Asteroids) DrawAsteroid(ToGodot(rock.Position), rock.Radius);
        foreach (var target in _world.Targets)
            if (!target.IsDestroyed) DrawShip(target, false);
        if (!_world.Player.IsDestroyed) DrawShip(_world.Player, true);
        foreach (var projectile in _world.Projectiles)
        {
            var position = ToGodot(projectile.Position);
            var direction = ToGodot(projectile.Velocity).Normalized();
            DrawLine(position - direction * 15, position, Teal.Darkened(0.4f), 4, true);
            DrawCircle(position, projectile.Radius + 3, new Color(Teal, 0.12f));
            DrawCircle(position, projectile.Radius, new Color("e3fff8"));
        }
        foreach (var burst in _bursts) DrawBurst(burst);
        // Rogue.lvl's gas clouds are "dynamic last" with smart translucency: drawn see-through over ships and shots.
        if (_levelArt is not null && _map is not null)
            foreach (var index in _levelArt.Visible(VisibleWorldRect(), overShips: true)) _levelArt.Draw(this, index, _time);
    }

    private void DrawStars(Vector2 player)
    {
        // Cell hashing gives an unlimited, deterministic starfield without gameplay objects.
        var viewSize = GetViewportRect().Size;
        var extent = viewSize * 0.6f + new Vector2(180, 180);
        const int cell = 100;
        for (var y = (int)Math.Floor((player.Y - extent.Y) / cell); y <= (int)Math.Ceiling((player.Y + extent.Y) / cell); y++)
        for (var x = (int)Math.Floor((player.X - extent.X) / cell); x <= (int)Math.Ceiling((player.X + extent.X) / cell); x++)
        {
            var hash = unchecked((uint)(x * 73856093 ^ y * 19349663 ^ 83492791));
            var position = new Vector2(x * cell + hash % 87, y * cell + (hash >> 8) % 89);
            var brightness = 0.28f + ((hash >> 16) % 100) / 200f;
            DrawCircle(position, hash % 11 == 0 ? 1.5f : 0.7f, new Color(0.65f, 0.8f, 0.95f, brightness));
        }
        var spacing = 400;
        for (var x = (int)Math.Floor((player.X - extent.X) / spacing); x <= (int)Math.Ceiling((player.X + extent.X) / spacing); x++)
            DrawLine(new Vector2(x * spacing, player.Y - extent.Y), new Vector2(x * spacing, player.Y + extent.Y), new Color(0.18f, 0.33f, 0.42f, 0.10f), 1);
        for (var y = (int)Math.Floor((player.Y - extent.Y) / spacing); y <= (int)Math.Ceiling((player.Y + extent.Y) / spacing); y++)
            DrawLine(new Vector2(player.X - extent.X, y * spacing), new Vector2(player.X + extent.X, y * spacing), new Color(0.18f, 0.33f, 0.42f, 0.10f), 1);
    }

    private void DrawOrigin()
    {
        var spawn = ToGodot(Spawn);
        DrawArc(spawn, 74, 0, Mathf.Tau, 60, new Color(0.18f, 0.47f, 0.5f, 0.18f), 1, true);
        DrawArc(spawn, 80, 0, Mathf.Tau, 60, new Color(0.18f, 0.47f, 0.5f, 0.1f), 1, true);
        DrawString(ThemeDB.FallbackFont, spawn + new Vector2(-51, 107), "LAUNCH POINT", HorizontalAlignment.Left, -1, 13, new Color("426676"));
    }

    /// <summary>The world rectangle the camera currently shows.</summary>
    private Rect2 VisibleWorldRect() => GetCanvasTransform().AffineInverse() * GetViewportRect();

    /// <summary>Wall and terrain colours: blocked tiles are rock; store, trade and repair zones get a faint tint (NEW look).</summary>
    internal static Color TileColor(byte tile) =>
        (tile & SectorMap.BlockedBit) != 0 ? new Color("3b5161")
        : (tile & 0x0F) switch
        {
            10 => new Color(0.38f, 0.87f, 0.82f, 0.10f), // SpaceDock store zone
            15 => new Color(0.96f, 0.71f, 0.42f, 0.10f), // refinery / trade zone
            9 => new Color(0.88f, 0.42f, 0.35f, 0.10f),  // repair dock
            0 => new Color(0, 0, 0, 0),
            _ => new Color(0.6f, 0.6f, 0.9f, 0.08f),
        };

    /// <summary>
    /// One pixel per tile (NEW look). With the original map art staged, its sprites draw the walls, so only the blocked
    /// tiles no sprite cell covers keep the NEW wall colour (otherwise they would be invisible collision), and the zone
    /// tints are left out.
    /// </summary>
    private static ImageTexture TileTexture(SectorMap map, OriginalLevelArt? art)
    {
        var covered = art?.CoverageMask(map.Width, map.Height);
        var clear = new byte[4];
        var bytes = new byte[map.Width * map.Height * 4];
        var palette = new byte[256][];
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
        {
            var tile = map.TileAt(x, y);
            var rgba = covered is null ? palette[tile] ??= ToRgba(TileColor(tile))
                : (tile & SectorMap.BlockedBit) == 0 || covered[y * map.Width + x] ? clear
                : palette[tile] ??= ToRgba(TileColor(tile));
            rgba.CopyTo(bytes, (y * map.Width + x) * 4);
        }
        return ImageTexture.CreateFromImage(Image.CreateFromData(map.Width, map.Height, false, Image.Format.Rgba8, bytes));
    }

    internal static byte[] ToRgba(Color c) => [(byte)c.R8, (byte)c.G8, (byte)c.B8, (byte)c.A8];

    private void DrawAsteroid(Vector2 position, float radius)
    {
        var seed = unchecked((int)(position.X * 73 + position.Y * 97));
        if (_levelArt?.RockFor(radius, seed) is { } rock)
        {
            _levelArt.DrawObjectCentred(this, rock, position, _time, (int)((uint)seed % 100));
            return;
        }
        var points = new Vector2[13];
        for (var i = 0; i < 12; i++)
        {
            var angle = i * Mathf.Tau / 12;
            var roughness = 0.82f + Math.Abs((seed ^ i * 193) % 30) / 100f;
            points[i] = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * roughness;
        }
        points[12] = points[0];
        DrawColoredPolygon(points[..12], new Color("293b49"));
        DrawPolyline(points, new Color("667986"), 1.7f, true);
        DrawCircle(position - new Vector2(radius * 0.22f, radius * 0.2f), radius * 0.25f, new Color("1c2c38"));
        DrawArc(position - new Vector2(radius * 0.22f, radius * 0.2f), radius * 0.25f, 0.4f, 3.6f, 20, new Color("435766"), 1, true);
        DrawCircle(position + new Vector2(radius * 0.3f, radius * 0.24f), radius * 0.12f, new Color("1d2c38"));
    }

    private void DrawShip(ShipState ship, bool isPlayer)
    {
        var position = ToGodot(ship.Position);
        var color = isPlayer ? Teal : Amber;
        if (ship.Shield > 0)
            DrawArc(position, ship.Radius + 6, 0, Mathf.Tau, 40, new Color(color, 0.16f + 0.09f * ship.Shield / ship.Definition.Defense.MaximumShield), 1, true);
        if (isPlayer && ship.Velocity.LengthSquared() > 1)
        {
            var velocity = ToGodot(ship.Velocity);
            DrawLine(position, position + velocity * 0.3f, new Color(Teal, 0.25f), 1, true);
            DrawCircle(position + velocity * 0.3f, 2, new Color(Teal, 0.5f));
        }
        // Practice targets still use the provisional Hornet preview; their simulation definitions inherit the player hull.
        var sprite = isPlayer ? Originals?.HullSpriteForVehicle(ship.Definition.Id) : Originals?.HullSprite("Hornet");
        if (isPlayer) PlayerAtlasPath = null;
        if (Originals is not null && sprite is not null)
        {
            var size = new Vector2(sprite.Width, sprite.Height);
            var texture = Originals.Texture(sprite);
            if (isPlayer) PlayerAtlasPath = texture.ResourcePath;
            // Column5 is a visual preview choice; native bank/HSV/shadow semantics remain pending.
            DrawTextureRectRegion(texture, new Rect2(position - size / 2, size),
                Originals.Frame(sprite, OriginalSpriteLibrary.DirectionRow(ship.Heading), 5));
        }
        else
        {
        DrawSetTransform(position, ship.Heading);
        var size = ship.Radius / 18;
        DrawSetTransform(position, ship.Heading, new Vector2(size, size));
        if (isPlayer && InputState.Thrust != 0)
        {
            var direction = InputState.Thrust > 0 ? -1 : 1;
            var flicker = 20 + 6 * Mathf.Sin(_time * 41);
            Vector2[] flame = [new(direction * 14, -5), new(direction * (14 + flicker), 0), new(direction * 14, 5)];
            DrawColoredPolygon(flame, new Color(Teal, 0.4f));
            DrawLine(new Vector2(direction * 15, 0), new Vector2(direction * (20 + flicker * 0.65f), 0), new Color("cefff6"), 3, true);
        }
        Vector2[] wings = [new(14, 0), new(-17, -15), new(-10, 0), new(-17, 15)];
        Vector2[] body = [new(24, 0), new(-14, -9), new(-9, 0), new(-14, 9)];
        DrawColoredPolygon(wings, new Color("374e5b"));
        DrawPolyline([.. wings, wings[0]], color.Darkened(0.35f), 1.4f, true);
        DrawColoredPolygon(body, isPlayer ? new Color("c3dee2") : new Color("d4aa83"));
        DrawPolyline([.. body, body[0]], color, 1.5f, true);
        DrawColoredPolygon([new(12, 0), new(1, -4), new(-3, 0), new(1, 4)], new Color("0b2838"));
        DrawLine(new Vector2(-8, -10), new Vector2(-15, -12), color, 2, true);
        DrawLine(new Vector2(-8, 10), new Vector2(-15, 12), color, 2, true);
        DrawSetTransform(Vector2.Zero);
        }
        if (!isPlayer)
        {
            DrawString(ThemeDB.FallbackFont, position + new Vector2(-45, -38), "PRACTICE TARGET", HorizontalAlignment.Left, -1, 12, Amber);
            DrawLine(position + new Vector2(-28, 34), position + new Vector2(28, 34), new Color("243242"), 3);
            DrawLine(position + new Vector2(-28, 34), position + new Vector2(-28 + 56 * ship.Hull / ship.Definition.Defense.MaximumHull, 34), Amber, 3);
        }
    }

    private void DrawStation((System.Numerics.Vector2 Position, float DockRadius, string Name)? site, Color color)
    {
        if (site is not { } station) return;
        var centre = ToGodot(station.Position);
        DrawArc(centre, station.DockRadius, 0, Mathf.Tau, 90, new Color(color, 0.10f), 1, true);
        if (_levelArt is not null && _map is not null)
        {
            // The original station sprites are already on the map; only the NEW dock ring and name are added.
            DrawString(ThemeDB.FallbackFont, centre + new Vector2(-40, 46), station.Name.ToUpperInvariant(), HorizontalAlignment.Left, -1, 12, color.Darkened(0.25f));
            return;
        }
        DrawCircle(centre, 26, new Color("1a2a36"));
        DrawArc(centre, 26, 0, Mathf.Tau, 40, color.Darkened(0.2f), 2, true);
        DrawRect(new Rect2(centre - new Vector2(9, 9), new Vector2(18, 18)), new Color(color, 0.55f));
        DrawString(ThemeDB.FallbackFont, centre + new Vector2(-40, 46), station.Name.ToUpperInvariant(), HorizontalAlignment.Left, -1, 12, color.Darkened(0.25f));
    }

    /// <summary>Mineral piles: the item's original prize sprite when staged, else a small marker; the 80-pixel pickup distance (CFG ItemPickupDistance) would hide a whole clump.</summary>
    private void DrawMineral(string itemId, Vector2 position, float radius)
    {
        if (_levelArt is not null && _levelArt.DrawMineral(this, itemId, position, _time)) return;
        var pulse = 0.5f + 0.5f * Mathf.Sin(_time * 2);
        DrawCircle(position, 12, new Color(Amber, 0.07f + 0.05f * pulse));
        DrawArc(position, 7, 0, Mathf.Tau, 16, new Color(Amber, 0.7f), 1.5f, true);
        DrawRect(new Rect2(position - new Vector2(3, 3), new Vector2(6, 6)), new Color("ffe2b8"));
    }

    private void DrawPile(Vector2 position, float radius)
    {
        var pulse = 0.5f + 0.5f * Mathf.Sin(_time * 3);
        DrawCircle(position, radius, new Color(Teal, 0.08f + 0.06f * pulse));
        DrawArc(position, radius, 0, Mathf.Tau, 24, new Color(Teal, 0.6f), 1.5f, true);
        DrawRect(new Rect2(position - new Vector2(4, 4), new Vector2(8, 8)), new Color("e3fff8"));
    }

    private void DrawBurst(Burst burst)
    {
        var progress = burst.Age / burst.Duration;
        var radius = 8 + progress * (burst.Duration > 0.5f ? 100 : 32);
        var color = new Color(burst.Color, 1 - progress);
        DrawArc(burst.Position, radius, 0, Mathf.Tau, 40, color, 2, true);
        for (var i = 0; i < 12; i++)
        {
            var direction = Vector2.FromAngle(i * Mathf.Tau / 12 + 0.31f);
            DrawLine(burst.Position + direction * radius * 0.65f, burst.Position + direction * radius, color, 2, true);
        }
    }

    public static Vector2 ToGodot(System.Numerics.Vector2 value) => new(value.X, value.Y);
}
