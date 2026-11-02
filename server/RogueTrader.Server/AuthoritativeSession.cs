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
/// Not thread-safe: one caller submits and advances. Range/rate/teleport policing is queue C8 and is not done here; the
/// simulation still clamps thrust and turn to [-1, 1].
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

    public AuthoritativeSession(WorldSimulation world, SessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        _options = options ?? SessionOptions.Default;
        _options.Validate();
        _runner = new FixedStepRunner(world);
        _recentCapacity = _options.InputQueueCapacity * 2;
    }

    public WorldSimulation World => _runner.World;
    /// <summary>The last simulated tick.</summary>
    public long ServerTick => World.TickNumber;
    /// <summary>Highest sequence that has run or been superseded; 0 before any input is processed. Acknowledged in snapshots.</summary>
    public uint LastProcessedInputSequence { get; private set; }
    public int QueuedInputCount => _queue.Count;
    public InputCounters Counters { get; } = new();

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
    public int Advance(double elapsedSeconds) => _runner.Advance(elapsedSeconds, TakeInput);

    /// <summary>Simulates exactly one tick.</summary>
    public void Step() => World.Tick(TakeInput(World.TickNumber + 1));

    private InputVerdict Classify(PlayerInput input)
    {
        if (input.ClientTick == 0 || !float.IsFinite(input.Thrust) || !float.IsFinite(input.Turn) ||
            (input.Flags & ~InputFlags.Fire) != 0)
            return InputVerdict.Invalid;
        if (_queuedSequences.Contains(input.Sequence) || _recentSequences.Contains(input.Sequence))
            return InputVerdict.Duplicate;
        if (input.ClientTick <= ServerTick) return InputVerdict.Late;
        if (_hasProcessed && input.Sequence <= LastProcessedInputSequence) return InputVerdict.Stale;
        if (input.ClientTick > ServerTick + _options.MaxInputLeadTicks) return InputVerdict.TooFarAhead;
        if (_queue.Count >= _options.InputQueueCapacity) return InputVerdict.QueueFull;
        return InputVerdict.Accepted;
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
