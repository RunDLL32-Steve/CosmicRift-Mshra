namespace RogueTrader.Server;

/// <summary>Running totals of what an <see cref="AuthoritativeSession"/> did with inputs and ticks.</summary>
public sealed class InputCounters
{
    private readonly long[] _verdicts = new long[Enum.GetValues<InputVerdict>().Length];

    /// <summary>Submissions that received <paramref name="verdict"/>.</summary>
    public long this[InputVerdict verdict] => _verdicts[(int)verdict];
    /// <summary>Accepted inputs that ran as their tick's input.</summary>
    public long Applied { get; private set; }
    /// <summary>Accepted inputs dropped because a higher sequence named the same tick.</summary>
    public long Superseded { get; private set; }
    /// <summary>Ticks simulated with no input queued for them (the neutral input ran).</summary>
    public long MissingInputTicks { get; private set; }

    internal void Count(InputVerdict verdict) => _verdicts[(int)verdict]++;
    internal void CountApplied() => Applied++;
    internal void CountSuperseded(int count) => Superseded += count;
    internal void CountMissing() => MissingInputTicks++;
}
