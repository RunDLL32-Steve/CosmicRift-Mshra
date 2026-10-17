using System.Numerics;
using RogueTrader.Simulation;

namespace RogueTrader.AI;

/// <summary>
/// Initial NEW NPC pirate (plan 12.1/12.2). Pure and deterministic: its only inputs are the per-tick <see cref="AiWorldView"/>, the
/// configuration, the patrol waypoints and the seed. Its only output is a <see cref="ShipInput"/>, so it flies under exactly the
/// movement, energy and weapon rules the simulation applies to players. This is a provisional engineering AI, not a recovered
/// original controller.
/// </summary>
public sealed class PirateController : IShipController
{
    private readonly PirateAiConfig _config;
    private readonly Vector2[] _waypoints;
    private readonly DeterministicRng _rng;
    private Vector2? _home;
    private int _waypointIndex;
    private long _stateEnteredTick;
    private int _stateDurationTicks;
    private float? _lastHealth;
    private Vector2 _lastThreatPosition;
    private float _evadeJink;
    private bool _started;

    public PirateController(PirateAiConfig config, IEnumerable<Vector2>? patrolWaypoints, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Validate();
        _config = config;
        _waypoints = patrolWaypoints?.ToArray() ?? [];
        if (_waypoints.Any(point => !float.IsFinite(point.X) || !float.IsFinite(point.Y)))
            throw new ArgumentException("Patrol waypoints must be finite.", nameof(patrolWaypoints));
        _rng = new DeterministicRng(seed);
        State = PirateState.Idle;
        _stateDurationTicks = _rng.NextInt(config.IdleTicksMin, config.IdleTicksMax);
    }

    public PirateState State { get; private set; }
    public string? TargetId { get; private set; }
    public int WaypointIndex => _waypointIndex;

    /// <summary>Raised as (tick, from, to) on each transition.</summary>
    public event Action<long, PirateState, PirateState>? StateChanged;

    public ShipInput Think(AiWorldView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        var self = view.Self;
        if (!_started)
        {
            _started = true;
            _stateEnteredTick = view.Tick;
            _home = self.Position;
        }
        if (self.IsDestroyed)
        {
            Enter(PirateState.Dead, view.Tick);
            return default;
        }

        var health = self.HealthFraction;
        var damaged = _lastHealth.HasValue && health < _lastHealth.Value - 1e-4f;
        _lastHealth = health;

        var target = ResolveTarget(view);
        if (target is null)
            TargetId = null;
        UpdateState(view, target, health, damaged);
        target = TargetId is null ? null : FindShip(view, TargetId);
        return Act(view, target);
    }

    private void UpdateState(AiWorldView view, AiShipView? target, float health, bool damaged)
    {
        var self = view.Self;
        var elapsed = view.Tick - _stateEnteredTick;
        switch (State)
        {
            case PirateState.Idle:
            case PirateState.Patrol:
            case PirateState.ReturnToPatrol:
                if (TryAcquire(view, health, damaged))
                    break;
                if (State == PirateState.Idle && elapsed >= _stateDurationTicks)
                    Enter(PirateState.Patrol, view.Tick);
                else if (State == PirateState.ReturnToPatrol && DistanceTo(self.Position, CurrentWaypoint()) <= _config.WaypointArrivalRadius)
                {
                    AdvanceWaypoint();
                    Enter(PirateState.Patrol, view.Tick);
                }
                else if (State == PirateState.Patrol && DistanceTo(self.Position, CurrentWaypoint()) <= _config.WaypointArrivalRadius)
                    AdvanceWaypoint();
                break;
            case PirateState.DetectTarget:
                if (target is null || Distance(self, target.Value) > _config.LoseTargetRange)
                    LoseTarget(view);
                else if (health <= _config.FleeHealthFraction)
                    BeginEvade(view, target.Value);
                else if (elapsed >= _stateDurationTicks)
                    Enter(PirateState.Chase, view.Tick);
                break;
            case PirateState.Chase:
                if (target is null || Distance(self, target.Value) > _config.LoseTargetRange)
                    LoseTarget(view);
                else if (health <= _config.FleeHealthFraction)
                    BeginEvade(view, target.Value);
                else if (Distance(self, target.Value) <= _config.ChaseToAttackRange)
                    Enter(PirateState.Attack, view.Tick);
                break;
            case PirateState.Attack:
                if (target is null || Distance(self, target.Value) > _config.LoseTargetRange)
                    LoseTarget(view);
                else if (health <= _config.FleeHealthFraction)
                    BeginEvade(view, target.Value);
                else if (Distance(self, target.Value) > _config.AttackToChaseRange)
                    Enter(PirateState.Chase, view.Tick);
                break;
            case PirateState.Evade:
                if (target is not null)
                    _lastThreatPosition = target.Value.Position;
                if (elapsed >= _stateDurationTicks)
                {
                    TargetId = null;
                    ChooseNearestWaypoint(self.Position);
                    Enter(PirateState.ReturnToPatrol, view.Tick);
                }
                break;
        }
    }

    /// <summary>Detection or damage while not engaged. A weakened pirate runs instead of fighting.</summary>
    private bool TryAcquire(AiWorldView view, float health, bool damaged)
    {
        var self = view.Self;
        AiShipView? candidate = null;
        if (damaged && self.LastDamagedByShipId is { } attackerId
            && FindShip(view, attackerId) is { IsDestroyed: false } attacker
            && attacker.Team != self.Team && Distance(self, attacker) <= _config.LoseTargetRange)
            candidate = attacker;
        candidate ??= NearestHostile(view, _config.DetectionRange);
        if (candidate is null)
            return false;
        if (health < _config.ReengageHealthFraction)
        {
            // Too weak to fight: run only when provoked or critical, otherwise leave the contact alone.
            if (!damaged && health > _config.FleeHealthFraction)
                return false;
            BeginEvade(view, candidate.Value);
            return true;
        }
        TargetId = candidate.Value.Id;
        _stateDurationTicks = _rng.NextInt(_config.AcquisitionTicksMin, _config.AcquisitionTicksMax);
        Enter(PirateState.DetectTarget, view.Tick);
        return true;
    }

    private void LoseTarget(AiWorldView view)
    {
        TargetId = null;
        ChooseNearestWaypoint(view.Self.Position);
        Enter(PirateState.ReturnToPatrol, view.Tick);
    }

    private void BeginEvade(AiWorldView view, AiShipView threat)
    {
        _lastThreatPosition = threat.Position;
        TargetId = threat.Id;
        _stateDurationTicks = _rng.NextInt(_config.EvadeTicksMin, _config.EvadeTicksMax);
        _evadeJink = _rng.NextBool() ? _config.EvadeJinkAngle : -_config.EvadeJinkAngle;
        Enter(PirateState.Evade, view.Tick);
    }

    private void Enter(PirateState next, long tick)
    {
        if (State == next)
            return;
        var previous = State;
        State = next;
        _stateEnteredTick = tick;
        if (next == PirateState.Idle)
            _stateDurationTicks = _rng.NextInt(_config.IdleTicksMin, _config.IdleTicksMax);
        StateChanged?.Invoke(tick, previous, next);
    }

    private ShipInput Act(AiWorldView view, AiShipView? target)
    {
        var self = view.Self;
        switch (State)
        {
            case PirateState.Idle:
                return default;
            case PirateState.Patrol:
            case PirateState.ReturnToPatrol:
                return CruiseTo(view, CurrentWaypoint());
            case PirateState.DetectTarget:
                // Lock on: turn to face the contact without thrusting or firing while the reaction delay runs.
                return target is null
                    ? default
                    : new ShipInput(0, Turn(self, Intercept.HeadingTo(target.Value.Position - self.Position)), false);
            case PirateState.Chase:
                return target is null ? default : Chase(view, target.Value);
            case PirateState.Attack:
                return target is null ? default : Attack(view, target.Value);
            case PirateState.Evade:
            {
                var away = self.Position - _lastThreatPosition;
                var heading = away.LengthSquared() < 1e-6f ? self.Heading : Intercept.HeadingTo(away) + _evadeJink;
                var error = Intercept.AngleDifference(self.Heading, heading);
                var thrust = MathF.Abs(error) <= _config.ThrustAlignAngle ? 1f : 0f;
                return new ShipInput(thrust, TurnFromError(error), false);
            }
            default:
                return default;
        }
    }

    /// <summary>
    /// Arrive-style steering with only forward thrust and rotation: aim the velocity change at the desired velocity (toward the point,
    /// capped at patrol speed and slowed so the ship can stop with half its forward acceleration). Corrects drift instead of
    /// assuming the ship has drag.
    /// </summary>
    private ShipInput CruiseTo(AiWorldView view, Vector2 point)
    {
        var self = view.Self;
        var offset = point - self.Position;
        var distance = offset.Length();
        var brake = MathF.Max(1f, view.Capabilities.ForwardAcceleration * 0.5f);
        var desiredSpeed = MathF.Min(_config.PatrolSpeed, MathF.Sqrt(2 * brake * MathF.Max(0, distance - _config.WaypointArrivalRadius * 0.5f)));
        var desired = distance > 1e-4f ? offset / distance * desiredSpeed : Vector2.Zero;
        var change = desired - self.Velocity;
        if (change.LengthSquared() < 25f)
        {
            // Velocity already matches the plan: hold the bearing to the point and coast.
            var hold = Intercept.AngleDifference(self.Heading, Intercept.HeadingTo(offset));
            return new ShipInput(0, distance > 1e-4f ? TurnFromError(hold) : 0, false);
        }
        var error = Intercept.AngleDifference(self.Heading, Intercept.HeadingTo(change));
        var thrust = MathF.Abs(error) <= _config.ThrustAlignAngle ? 1f : 0f;
        return new ShipInput(thrust, TurnFromError(error), false);
    }

    private ShipInput Chase(AiWorldView view, AiShipView target)
    {
        var self = view.Self;
        var aim = AimPoint(view, target);
        var error = Intercept.AngleDifference(self.Heading, Intercept.HeadingTo(aim - self.Position));
        var thrust = MathF.Abs(error) <= _config.ThrustAlignAngle ? 1f : 0f;
        return new ShipInput(thrust, TurnFromError(error), false);
    }

    private ShipInput Attack(AiWorldView view, AiShipView target)
    {
        var self = view.Self;
        var caps = view.Capabilities;
        var aim = AimPoint(view, target);
        var error = Intercept.AngleDifference(self.Heading, Intercept.HeadingTo(aim - self.Position));
        var offset = target.Position - self.Position;
        var range = offset.Length();
        var line = range > 1e-4f ? offset / range : Vector2.UnitX;
        var closingSpeed = -Vector2.Dot(target.Velocity - self.Velocity, line);
        var alignedForThrust = MathF.Abs(Intercept.AngleDifference(self.Heading, Intercept.HeadingTo(offset))) <= _config.ThrustAlignAngle;

        // Range management happens only while the nose points at the target, so thrust sign means closer/farther.
        float thrust = 0;
        if (alignedForThrust)
        {
            if (range < _config.MinimumRange || closingSpeed > _config.ClosingSpeedLimit)
                thrust = -1;
            else if (range > _config.PreferredRange)
                thrust = 1;
        }

        var fire = MathF.Abs(error) <= _config.FireConeAngle
            && self.Energy >= caps.WeaponEnergyCost
            && (aim - self.Position).Length() <= caps.WeaponRange * _config.FireRangeFraction;
        return new ShipInput(thrust, TurnFromError(error), fire);
    }

    private Vector2 AimPoint(AiWorldView view, AiShipView target)
    {
        var self = view.Self;
        var caps = view.Capabilities;
        var relative = target.Position - self.Position;
        var relativeVelocity = caps.ProjectileInheritsShipVelocity ? target.Velocity - self.Velocity : target.Velocity;
        if (Intercept.TrySolve(relative, relativeVelocity, caps.MuzzleVelocity, out var time) && time <= caps.ProjectileLifetimeSeconds)
            return target.Position + relativeVelocity * time;
        return target.Position;
    }

    private float Turn(AiShipView self, float desiredHeading) =>
        TurnFromError(Intercept.AngleDifference(self.Heading, desiredHeading));

    private float TurnFromError(float error) => Math.Clamp(error / _config.TurnProportionalAngle, -1f, 1f);

    private AiShipView? ResolveTarget(AiWorldView view)
    {
        if (TargetId is null)
            return null;
        var found = FindShip(view, TargetId);
        return found is { IsDestroyed: false } ship && ship.Team != view.Self.Team ? ship : null;
    }

    private static AiShipView? FindShip(AiWorldView view, string id)
    {
        foreach (var other in view.Others)
            if (string.Equals(other.Id, id, StringComparison.Ordinal))
                return other;
        return null;
    }

    private static AiShipView? NearestHostile(AiWorldView view, float range)
    {
        AiShipView? best = null;
        var bestDistance = float.PositiveInfinity;
        foreach (var other in view.Others)
        {
            if (other.IsDestroyed || other.Team == view.Self.Team)
                continue;
            var distance = Distance(view.Self, other);
            if (distance > range)
                continue;
            if (distance < bestDistance
                || (distance == bestDistance && string.CompareOrdinal(other.Id, best!.Value.Id) < 0))
            {
                best = other;
                bestDistance = distance;
            }
        }
        return best;
    }

    private Vector2 CurrentWaypoint() =>
        _waypoints.Length == 0 ? _home ?? Vector2.Zero : _waypoints[_waypointIndex % _waypoints.Length];

    private void AdvanceWaypoint()
    {
        if (_waypoints.Length > 0)
            _waypointIndex = (_waypointIndex + 1) % _waypoints.Length;
    }

    private void ChooseNearestWaypoint(Vector2 from)
    {
        var bestDistance = float.PositiveInfinity;
        for (var index = 0; index < _waypoints.Length; index++)
        {
            var distance = Vector2.DistanceSquared(from, _waypoints[index]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                _waypointIndex = index;
            }
        }
    }

    private static float Distance(AiShipView a, AiShipView b) => Vector2.Distance(a.Position, b.Position);
    private static float DistanceTo(Vector2 a, Vector2 b) => Vector2.Distance(a, b);
}
