using System.Collections.Immutable;

namespace RogueTrader.Persistence.Transfers;

/// <summary>
/// What a sector hands over for one player: counted holdings under opaque ids (ship, carried cargo, carried currency, as the
/// sector chooses) plus an opaque state string (for example an encoded ship condition). The coordinator never interprets it;
/// it only checks that the destination received exactly what the source detached.
/// </summary>
public sealed class TransferManifest : IEquatable<TransferManifest>
{
    public TransferManifest(IEnumerable<KeyValuePair<string, long>> holdings, string? state = null)
    {
        ArgumentNullException.ThrowIfNull(holdings);
        var builder = ImmutableSortedDictionary.CreateBuilder<string, long>(StringComparer.Ordinal);
        foreach (var (id, count) in holdings)
        {
            if (string.IsNullOrEmpty(id) || !ProfileInvariants.IsWellFormedUtf16(id)) throw new ArgumentException("Holding ids must be non-empty, well-formed strings.", nameof(holdings));
            if (count <= 0) throw new ArgumentException($"Holding '{id}' must have a positive count.", nameof(holdings));
            if (builder.ContainsKey(id)) throw new ArgumentException($"Holding '{id}' appears twice.", nameof(holdings));
            builder.Add(id, count);
        }
        if (state is not null && !ProfileInvariants.IsWellFormedUtf16(state)) throw new ArgumentException("State must be well-formed.", nameof(state));
        Holdings = builder.ToImmutable();
        State = state;
    }

    public ImmutableSortedDictionary<string, long> Holdings { get; }
    public string? State { get; }

    public bool Equals(TransferManifest? other) =>
        other is not null && string.Equals(State, other.State, StringComparison.Ordinal) && Holdings.Count == other.Holdings.Count
        && Holdings.All(p => other.Holdings.TryGetValue(p.Key, out var v) && v == p.Value);

    public override bool Equals(object? obj) => Equals(obj as TransferManifest);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(State, StringComparer.Ordinal);
        foreach (var (id, count) in Holdings)
        {
            hash.Add(id, StringComparer.Ordinal);
            hash.Add(count);
        }
        return hash.ToHashCode();
    }
}

/// <summary>Where the destination will place the arriving ship. Coordinates are destination world units.</summary>
public sealed record SpawnPlacement(string EntryId, double X, double Y)
{
    public bool IsValid => !string.IsNullOrEmpty(EntryId) && double.IsFinite(X) && double.IsFinite(Y);
}
