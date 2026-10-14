using RogueTrader.GameData;
using RogueTrader.Simulation;
using Vector2 = System.Numerics.Vector2;

namespace RogueTrader.Client;

/// <summary>Runtime acceptance checks driven through the same fixed-tick world used by gameplay.</summary>
public sealed class ClientSmokeTest(GameCatalog catalog)
{
    public WorldSimulation World { get; private set; } = new(catalog);
    public bool IsComplete { get; private set; }
    private int _phase;
    private int _phaseTick;
    private Vector2 _velocity;
    private Vector2 _position;
    private float _heading;
    private AsteroidState? _collisionTarget;
    public Action<string>? Report { get; set; }

    public void Step()
    {
        if (IsComplete) return;
        _phaseTick++;
        switch (_phase)
        {
            case 0:
                World.Tick(new ShipInput(0, 0, true));
                if (World.DestroyedTargets > 0)
                {
                    Require(World.ShotsFired > 0, "The client must fire real projectiles.");
                    Advance("Projectile firing, shield and hull damage, and target destruction");
                    _position = World.Player.Position;
                }
                else Require(_phaseTick < 360, "The initial firing lane did not destroy a target within 360 ticks.");
                break;
            case 1:
                World.Tick(new ShipInput(1, 0, false));
                if (_phaseTick >= 90)
                {
                    Require(World.Player.Velocity.Length() > 10 && Vector2.Distance(_position, World.Player.Position) > 10,
                        "Forward thrust must accelerate and move the ship.");
                    _velocity = World.Player.Velocity;
                    Advance("Forward thrust and flight");
                }
                break;
            case 2:
                World.Tick(default);
                if (_phaseTick >= 60)
                {
                    Require(Vector2.Distance(_velocity, World.Player.Velocity) < 0.01f, "Momentum must persist after thrust release.");
                    Advance("Persistent momentum through 60 coast ticks");
                }
                break;
            case 3:
                World.Tick(new ShipInput(-1, 0, false));
                if (_phaseTick >= 180)
                {
                    Require(Vector2.Dot(_velocity, World.Player.Velocity) < 0, "Reverse thrust must reverse velocity along the current heading.");
                    _velocity = World.Player.Velocity;
                    _heading = World.Player.Heading;
                    Advance("Reverse thrust and braking");
                }
                break;
            case 4:
                World.Tick(new ShipInput(0, 1, false));
                if (_phaseTick >= 30)
                {
                    Require(Math.Abs(World.Player.Heading - _heading) > 0.5f, "Rotation must change the heading.");
                    Require(Vector2.Distance(_velocity, World.Player.Velocity) < 0.01f, "Rotation alone must preserve velocity.");
                    Advance("Rotation independent from movement direction");
                    World = new WorldSimulation(catalog);
                    _collisionTarget = World.Asteroids.OrderBy(rock => Vector2.DistanceSquared(rock.Position, World.Player.Position)).FirstOrDefault();
                    Require(_collisionTarget is not null, "The playable sector must contain asteroids.");
                }
                break;
            case 5:
                var toRock = _collisionTarget!.Position - World.Player.Position;
                var desired = MathF.Atan2(toRock.Y, toRock.X);
                var error = MathF.IEEERemainder(desired - World.Player.Heading, MathF.Tau);
                var turn = Math.Clamp(error / (catalog.Ship.Physics.RotationSpeed * WorldSimulation.TickDuration), -1f, 1f);
                World.Tick(new ShipInput(Math.Abs(error) < 0.06f ? 1 : 0, turn, false));
                if (World.AsteroidCollisions > 0)
                {
                    var separation = Vector2.Distance(World.Player.Position, _collisionTarget.Position);
                    Require(separation >= World.Player.Radius + _collisionTarget.Radius - 0.02f, "The ship must remain outside the asteroid after impact.");
                    Advance("Asteroid approach and collision response");
                    IsComplete = true;
                    Report?.Invoke("CLIENT_SMOKE_PASS: flight, drift, reverse, rotation, target destruction, and asteroid collision verified.");
                }
                else Require(_phaseTick < 900, "The scripted ship did not reach an asteroid within 900 ticks.");
                break;
        }
    }

    private void Advance(string description)
    {
        Report?.Invoke($"SMOKE PASS: {description}");
        _phase++;
        _phaseTick = 0;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
