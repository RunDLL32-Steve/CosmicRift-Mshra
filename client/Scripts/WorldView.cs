using Godot;
using RogueTrader.Simulation;

namespace RogueTrader.Client;

/// <summary>Canonical raw ship pixels where staged; provisional range/effects remain engine independent.</summary>
public partial class WorldView : Node2D
{
    private readonly record struct Burst(Vector2 Position, Color Color, float Age, float Duration);
    private readonly List<Burst> _bursts = [];
    private readonly Dictionary<string, (float Hull, float Shield, bool Destroyed)> _previous = [];
    private WorldSimulation? _world;
    private float _time;
    public ShipInput InputState { get; set; }
    public OriginalSpriteLibrary? Originals { get; set; }
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
        DrawOrigin();
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
        DrawArc(Vector2.Zero, 74, 0, Mathf.Tau, 60, new Color(0.18f, 0.47f, 0.5f, 0.18f), 1, true);
        DrawArc(Vector2.Zero, 80, 0, Mathf.Tau, 60, new Color(0.18f, 0.47f, 0.5f, 0.1f), 1, true);
        DrawString(ThemeDB.FallbackFont, new Vector2(-51, 107), "LAUNCH POINT", HorizontalAlignment.Left, -1, 13, new Color("426676"));
    }

    private void DrawAsteroid(Vector2 position, float radius)
    {
        var seed = unchecked((int)(position.X * 73 + position.Y * 97));
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
        if (Originals is not null)
        {
            var sprite = Originals.HullSprite(isPlayer ? "Asp" : "Hornet");
            var size = new Vector2(sprite.Width, sprite.Height);
            // Column5 is a visual preview choice; native bank/HSV/shadow semantics remain pending.
            DrawTextureRectRegion(Originals.Texture(sprite), new Rect2(position - size / 2, size),
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
