using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueTrader.AI;

/// <summary>
/// External tuning for the initial NEW pirate (plan 12.1/12.2). Every value is NEW/provisional; none is a recovered original
/// controller value. Distances are world units, angles radians, durations 60 Hz ticks, speeds world units per second.
/// </summary>
public sealed record PirateAiConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public string Id { get; init; } = "initial_pirate";
    public string EvidenceClassification { get; init; } = "NEW";
    public string Team { get; init; } = "pirates";

    // Perception ranges.
    public float DetectionRange { get; init; } = 600;
    public float LoseTargetRange { get; init; } = 900;

    // Engagement band. ChaseToAttackRange < AttackToChaseRange gives hysteresis; MinimumRange < PreferredRange < ChaseToAttackRange.
    public float ChaseToAttackRange { get; init; } = 420;
    public float AttackToChaseRange { get; init; } = 520;
    public float PreferredRange { get; init; } = 280;
    public float MinimumRange { get; init; } = 140;
    public float ClosingSpeedLimit { get; init; } = 200;
    public float FireRangeFraction { get; init; } = 0.9f;
    public float FireConeAngle { get; init; } = 0.05f;

    // Steering.
    public float TurnProportionalAngle { get; init; } = 0.35f;
    public float ThrustAlignAngle { get; init; } = 0.6f;
    public float PatrolSpeed { get; init; } = 140;
    public float WaypointArrivalRadius { get; init; } = 60;

    // Timings (ticks). Each *Min/*Max pair is drawn from the seeded generator on state entry.
    public int IdleTicksMin { get; init; } = 60;
    public int IdleTicksMax { get; init; } = 180;
    public int AcquisitionTicksMin { get; init; } = 12;
    public int AcquisitionTicksMax { get; init; } = 36;
    public int EvadeTicksMin { get; init; } = 180;
    public int EvadeTicksMax { get; init; } = 360;
    public float EvadeJinkAngle { get; init; } = 0.5f;

    // Health fractions of (hull + shield) / (max hull + max shield).
    public float FleeHealthFraction { get; init; } = 0.3f;
    public float ReengageHealthFraction { get; init; } = 0.6f;

    public static PirateAiConfig FromJson(string json)
    {
        var config = JsonSerializer.Deserialize<PirateAiConfig>(json, JsonOptions)
            ?? throw new JsonException("Empty pirate AI definition.");
        config.Validate();
        return config;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Team))
            throw new ArgumentException("The pirate AI definition requires an ID and a team.");
        Positive(DetectionRange, nameof(DetectionRange));
        Positive(LoseTargetRange, nameof(LoseTargetRange));
        Positive(ChaseToAttackRange, nameof(ChaseToAttackRange));
        Positive(AttackToChaseRange, nameof(AttackToChaseRange));
        Positive(PreferredRange, nameof(PreferredRange));
        Positive(MinimumRange, nameof(MinimumRange));
        Positive(ClosingSpeedLimit, nameof(ClosingSpeedLimit));
        Positive(FireRangeFraction, nameof(FireRangeFraction));
        Positive(FireConeAngle, nameof(FireConeAngle));
        Positive(TurnProportionalAngle, nameof(TurnProportionalAngle));
        Positive(ThrustAlignAngle, nameof(ThrustAlignAngle));
        Positive(PatrolSpeed, nameof(PatrolSpeed));
        Positive(WaypointArrivalRadius, nameof(WaypointArrivalRadius));
        if (FireRangeFraction > 1)
            throw new ArgumentException("FireRangeFraction must be at most one.");
        if (LoseTargetRange < DetectionRange)
            throw new ArgumentException("LoseTargetRange must be at least DetectionRange.");
        if (!(MinimumRange < PreferredRange && PreferredRange < ChaseToAttackRange && ChaseToAttackRange < AttackToChaseRange))
            throw new ArgumentException("Ranges must satisfy MinimumRange < PreferredRange < ChaseToAttackRange < AttackToChaseRange.");
        if (AttackToChaseRange > LoseTargetRange)
            throw new ArgumentException("AttackToChaseRange must not exceed LoseTargetRange.");
        Ticks(IdleTicksMin, IdleTicksMax, "Idle");
        Ticks(AcquisitionTicksMin, AcquisitionTicksMax, "Acquisition");
        Ticks(EvadeTicksMin, EvadeTicksMax, "Evade");
        if (!float.IsFinite(EvadeJinkAngle) || EvadeJinkAngle < 0)
            throw new ArgumentException("EvadeJinkAngle must be finite and nonnegative.");
        if (!(FleeHealthFraction >= 0 && FleeHealthFraction < ReengageHealthFraction && ReengageHealthFraction <= 1))
            throw new ArgumentException("Health fractions must satisfy 0 <= Flee < Reengage <= 1.");
    }

    private static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0)
            throw new ArgumentException($"{name} must be finite and positive.");
    }

    private static void Ticks(int minimum, int maximum, string name)
    {
        if (minimum < 1 || maximum < minimum)
            throw new ArgumentException($"{name} tick range must satisfy 1 <= min <= max.");
    }
}
