using System.Numerics;
using RogueTrader.Protocol;
using RogueTrader.Simulation;

namespace RogueTrader.Server;

/// <summary>
/// Headless authoritative tick loop for one controlled ship, with a bounded deterministic input queue and no transport (queue C6).
/// The client only submits intent (<see cref="PlayerInput"/>); the server decides which input runs on which tick and alone
/// advances the <see cref="WorldSimulation"/>, so client motion is never accepted as state. Every rule here is NEW/provisional:
/// <list type="bullet">
/// <item>An input names the server tick it is for (<see cref="PlayerInput.ClientTick"/>, numbered like
/// <see cref="WorldSimulation.TickNumber"/>, first tick 1). Only ticks after the last simulated one, up to
/// <see cref="SessionOptions.MaxInputLeadTicks"/> ahead, are accepted.</item>
/// <item>When several inputs name one tick, the highest <see cref="PlayerInput.Sequence"/> runs and the rest are superseded, so
/// the outcome does not depend on the order in which the inputs arrived.</item>
/// <item>A tick with no input runs the neutral input (no thrust, turn or fire); the server never repeats or invents intent.</item>
/// <item>A rejected input changes nothing: not the world, the queue or the last processed sequence.</item>
/// </list>
/// Queue C8 adds boundary validation, also NEW/provisional and pending approval:
/// <list type="bullet">
/// <item>Thrust and turn outside the documented [-1, 1] range of <see cref="ShipInput"/> are rejected
/// (<see cref="InputVerdict.OutOfRange"/>) instead of being clamped by the simulation.</item>
/// <item>An input acknowledging a snapshot tick later than the last simulated tick is rejected
/// (<see cref="InputVerdict.FutureSnapshot"/>), since snapshots only describe ticks that ran.</item>
/// <item>Every submission, whatever its verdict, spends one unit of a per-sender allowance that refills by
/// <see cref="SessionOptions.InputsPerTick"/> per simulated tick up to <see cref="SessionOptions.InputBurst"/>. With none left the
/// input is <see cref="InputVerdict.RateLimited"/>.</item>
/// <item>A motion audit checks every tick: the controlled ship may move at most its definition's maximum speed times the tick
/// duration (plus <see cref="SessionOptions.DisplacementTolerance"/>), and may not move between ticks except through
/// <see cref="RestorePlayer"/>. Findings are recorded in <see cref="MotionViolations"/> and counted, never enforced, because no
/// enforcement policy is approved. The client cannot send a position, so this guards the server's own state.</item>
/// </list>
/// Not thread-safe: one caller submits and advances.
/// </summary>
public sealed class AuthoritativeSession
{
    private readonly SessionOptions _options;
    private readonly FixedStepRunner _runner;
    // Ordered by (tick, sequence); the queue never holds an entry for a tick that has already run.
    private readonly SortedDictionary<(long Tick, uint Sequence), PlayerInput> _queue = new();
    private readonly HashSet<uint> _queuedSequences = [];
    // Recently processed sequences, so a late copy of a processed input reads as Duplicate rather than Stale.
    private readonly HashSet<uint> _recentSequences = [];
    private readonly Queue<uint> _recentOrder = new();
    private readonly int _recentCapacity;
    private bool _hasProcessed;
    // Submission allowance (C8), refilled lazily from simulated ticks.
    private long _allowance;
    private long _allowanceTick;
    // Motion audit (C8): where the ship ended the last audited tick, and where it started the tick now running.
    private readonly List<MotionViolation> _violations = [];
    private Vector2 _settledPosition;
    private Vector2 _tickStart;
    private float _tickLimit;
    private bool _tickPending;

    /// <summary>The most motion audit findings kept; older ones drop off (the counters keep totals).</summary>
    public const int MotionViolationCapacity = 64;

    public AuthoritativeSession(WorldSimulation world, SessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        _options = options ?? SessionOptions.Default;
        _options.Validate();
        _runner = new FixedStepRunner(world);
        _recentCapacity = _options.InputQueueCapacity * 2;
        _allowance = _options.InputBurst;
        _allowanceTick = world.TickNumber;
        _settledPosition = world.Player.Position;
    }

    public WorldSimulation World => _runner.World;
    /// <summary>The last simulated tick.</summary>
    public long ServerTick => World.TickNumber;
    /// <summary>Highest sequence that has run or been superseded; 0 before any input is processed. Acknowledged in snapshots.</summary>
    public uint LastProcessedInputSequence { get; private set; }
    public int QueuedInputCount => _queue.Count;
    public InputCounters Counters { get; } = new();
    /// <summary>The latest motion audit findings, oldest first, at most <see cref="MotionViolationCapacity"/>.</summary>
    public IReadOnlyList<MotionViolation> MotionViolations => _violations;

    /// <summary>Checks one input and queues it for its tick, or rejects it without changing anything.</summary>
    public InputVerdict Submit(PlayerInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var verdict = Classify(input);
        Counters.Count(verdict);
        if (verdict == InputVerdict.Accepted)
        {
            _queue.Add((input.ClientTick, input.Sequence), input);
            _queuedSequences.Add(input.Sequence);
        }
        return verdict;
    }

    /// <summary>Advances by real elapsed time in whole fixed 1/60 s ticks; returns the number of ticks simulated.</summary>
    public int Advance(double elapsedSeconds)
    {
        var ticks = _runner.Advance(elapsedSeconds, BeginTick);
        EndTick();
        return ticks;
    }

    /// <summary>Simulates exactly one tick.</summary>
    public void Step()
    {
        World.Tick(BeginTick(World.TickNumber + 1));
        EndTick();
    }

    /// <summary>
    /// The sanctioned way to place the controlled ship (for example from a saved profile): restores it through
    /// <see cref="WorldSimulation.RestorePlayer"/> and moves the motion audit's reference point with it, so it is not flagged.
    /// </summary>
    public bool RestorePlayer(Vector2 position, float heading, float hull)
    {
        AuditBetweenTicks();
        if (!World.RestorePlayer(position, heading, hull)) return false;
        _settledPosition = World.Player.Position;
        return true;
    }

    // Runs right before World.Tick: audits the gap since the last tick, notes the start of this one, then picks its input.
    private ShipInput BeginTick(long tick)
    {
        EndTick();
        AuditBetweenTicks();
        _tickStart = World.Player.Position;
        _tickLimit = World.Player.Definition.Physics.MaximumSpeed * WorldSimulation.TickDuration + _options.DisplacementTolerance;
        _tickPending = true;
        return TakeInput(tick);
    }

    private void EndTick()
    {
        if (!_tickPending) return;
        _tickPending = false;
        var distance = Vector2.Distance(_tickStart, World.Player.Position);
        if (distance > _tickLimit) Flag(new MotionViolation(ServerTick, MotionViolationKind.SpeedExceeded, distance, _tickLimit));
        _settledPosition = World.Player.Position;
    }

    private void AuditBetweenTicks()
    {
        var position = World.Player.Position;
        if (position == _settledPosition) return;
        Flag(new MotionViolation(ServerTick, MotionViolationKind.MovedOutsideTick, Vector2.Distance(_settledPosition, position), 0));
        _settledPosition = position;
    }

    private void Flag(MotionViolation violation)
    {
        Counters.Count(violation.Kind);
        if (_violations.Count == MotionViolationCapacity) _violations.RemoveAt(0);
        _violations.Add(violation);
    }

    private InputVerdict Classify(PlayerInput input)
    {
        if (!SpendAllowance()) return InputVerdict.RateLimited;
        if (input.ClientTick == 0 || !float.IsFinite(input.Thrust) || !float.IsFinite(input.Turn) ||
            (input.Flags & ~InputFlags.Fire) != 0)
            return InputVerdict.Invalid;
        if (Math.Abs(input.Thrust) > 1 || Math.Abs(input.Turn) > 1) return InputVerdict.OutOfRange;
        if (input.LastSnapshotTick > ServerTick) return InputVerdict.FutureSnapshot;
        if (_queuedSequences.Contains(input.Sequence) || _recentSequences.Contains(input.Sequence))
            return InputVerdict.Duplicate;
        if (input.ClientTick <= ServerTick) return InputVerdict.Late;
        if (_hasProcessed && input.Sequence <= LastProcessedInputSequence) return InputVerdict.Stale;
        if (input.ClientTick > ServerTick + _options.MaxInputLeadTicks) return InputVerdict.TooFarAhead;
        if (_queue.Count >= _options.InputQueueCapacity) return InputVerdict.QueueFull;
        return InputVerdict.Accepted;
    }

    private bool SpendAllowance()
    {
        var elapsed = ServerTick - _allowanceTick;
        if (elapsed > 0)
        {
            _allowance = Math.Min(_options.InputBurst, _allowance + Math.Min(elapsed, _options.InputBurst) * (long)_options.InputsPerTick);
            _allowanceTick = ServerTick;
        }
        if (_allowance == 0) return false;
        _allowance--;
        return true;
    }

    private ShipInput TakeInput(long tick)
    {
        PlayerInput? chosen = null;
        var superseded = 0;
        // Entries for this tick are the smallest keys; within the tick the highest sequence wins.
        while (_queue.Count > 0)
        {
            var (key, input) = _queue.First();
            if (key.Tick != tick) break;
            _queue.Remove(key);
            _queuedSequences.Remove(input.Sequence);
            Remember(input.Sequence);
            if (chosen is not null) superseded++;
            chosen = input;
        }
        Counters.CountSuperseded(superseded);
        if (chosen is null)
        {
            Counters.CountMissing();
            return default;
        }
        Counters.CountApplied();
        LastProcessedInputSequence = _hasProcessed ? Math.Max(LastProcessedInputSequence, chosen.Sequence) : chosen.Sequence;
        _hasProcessed = true;
        return new ShipInput(chosen.Thrust, chosen.Turn, chosen.Flags.HasFlag(InputFlags.Fire));
    }

    private void Remember(uint sequence)
    {
        if (!_recentSequences.Add(sequence)) return;
        _recentOrder.Enqueue(sequence);
        if (_recentOrder.Count > _recentCapacity) _recentSequences.Remove(_recentOrder.Dequeue());
    }
}
