using System.Numerics;

namespace RogueTrader.AI;

/// <summary>Read-only snapshot of one ship as an AI controller may observe it. Contains no mutable simulation state.</summary>
public readonly record struct AiShipView(
    string Id,
    string Team,
    Vector2 Position,
    Vector2 Velocity,
    float Heading,
    float Radius,
    float Hull,
    float MaximumHull,
    float Shield,
    float MaximumShield,
    float Energy,
    bool IsDestroyed,
    string? LastDamagedByShipId)
{
    public float HealthFraction
    {
        get
        {
            var maximum = MaximumHull + MaximumShield;
            return maximum <= 0 ? 0 : Math.Clamp((Hull + Shield) / maximum, 0, 1);
        }
    }
}

/// <summary>Flight and weapon limits of the controlled ship; the same limits the simulation applies to players.</summary>
public readonly record struct AiShipCapabilities(
    float MaximumSpeed,
    float RotationSpeed,
    float ForwardAcceleration,
    float ReverseAcceleration,
    float MuzzleVelocity,
    float ProjectileLifetimeSeconds,
    float WeaponEnergyCost,
    bool ProjectileInheritsShipVelocity)
{
    public float WeaponRange => MuzzleVelocity * ProjectileLifetimeSeconds;
}

/// <summary>Obstacle the AI may observe (asteroids). Reserved for future avoidance; the initial pirate does not steer around them.</summary>
public readonly record struct AiObstacleView(string Id, Vector2 Position, float Radius);

/// <summary>Everything one controller sees on one tick. Controllers never receive the authoritative world object.</summary>
public sealed record AiWorldView(
    long Tick,
    AiShipView Self,
    AiShipCapabilities Capabilities,
    IReadOnlyList<AiShipView> Others,
    IReadOnlyList<AiObstacleView> Obstacles);
