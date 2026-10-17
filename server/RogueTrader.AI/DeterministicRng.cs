namespace RogueTrader.AI;

/// <summary>SplitMix64 generator: a fixed, documented algorithm so seeded AI runs reproduce on every platform and runtime.</summary>
public sealed class DeterministicRng
{
    private ulong _state;

    public DeterministicRng(ulong seed) => _state = seed;

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Uniform integer in [minInclusive, maxInclusive].</summary>
    public int NextInt(int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxInclusive));
        var span = (ulong)((long)maxInclusive - minInclusive + 1);
        return (int)(minInclusive + (long)(NextUInt64() % span));
    }

    public bool NextBool() => (NextUInt64() & 1) == 1;
}
