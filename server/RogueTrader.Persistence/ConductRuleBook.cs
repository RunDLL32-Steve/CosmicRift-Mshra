using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueTrader.Persistence;

/// <summary>
/// Data-driven bounty and reputation rules (queue C9). The contract and every number in a rule file are NEW/provisional: the
/// original bounty mathematics (RT-F051) are not recovered, so nothing here is original behaviour.
/// <list type="bullet">
/// <item>Each event adds a fixed bounty change and fixed per-category reputation changes once per occurrence.</item>
/// <item>Bounty is clamped to [0, <see cref="MaximumBounty"/>]; each reputation category to [<see cref="MinimumReputation"/>,
/// <see cref="MaximumReputation"/>]. Clamping happens after all occurrences, so a large count saturates at the bound.</item>
/// <item>There is no decay, reset or time-based rule: a rule file cannot express one, and unknown fields are refused.</item>
/// <item>A category whose standing returns to 0 is dropped, so profiles store only non-neutral standing.</item>
/// </list>
/// </summary>
public sealed class ConductRuleBook : IConductRules
{
    public const int MaximumCount = 1000;

    private readonly ImmutableSortedDictionary<string, ConductEventRule> _events;

    private ConductRuleBook(string version, ImmutableSortedSet<string> categories, long minimum, long maximum, long maximumBounty,
        ImmutableSortedDictionary<string, ConductEventRule> events)
    {
        Version = version;
        Categories = categories;
        MinimumReputation = minimum;
        MaximumReputation = maximum;
        MaximumBounty = maximumBounty;
        _events = events;
    }

    public string Version { get; }
    public ImmutableSortedSet<string> Categories { get; }
    public long MinimumReputation { get; }
    public long MaximumReputation { get; }
    public long MaximumBounty { get; }
    public IReadOnlyDictionary<string, ConductEventRule> Events => _events;

    public static ConductRuleBook FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        Document? doc;
        try
        {
            doc = JsonSerializer.Deserialize<Document>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            });
        }
        catch (JsonException error)
        {
            throw new FormatException($"Conduct rules are not valid: {error.Message}", error);
        }
        if (doc is null) throw new FormatException("Conduct rules are empty.");
        if (string.IsNullOrWhiteSpace(doc.Version)) throw new FormatException("Conduct rules need a version.");
        if (doc.Categories is null || doc.Categories.Count == 0) throw new FormatException("Conduct rules need at least one category.");
        var categories = ImmutableSortedSet.CreateRange(StringComparer.Ordinal, doc.Categories);
        if (categories.Count != doc.Categories.Count || categories.Any(c => !IdOk(c)))
            throw new FormatException("Conduct categories must be unique, non-empty ids.");
        if (doc.MinimumReputation > 0 || doc.MaximumReputation < 0 || doc.MinimumReputation == doc.MaximumReputation)
            throw new FormatException("The reputation range must contain 0 and must not be empty.");
        if (doc.MaximumBounty < 0) throw new FormatException("maximumBounty must be >= 0.");
        if (doc.Events is null || doc.Events.Count == 0) throw new FormatException("Conduct rules need at least one event.");

        var events = ImmutableSortedDictionary.CreateBuilder<string, ConductEventRule>(StringComparer.Ordinal);
        foreach (var e in doc.Events)
        {
            if (e is null || !IdOk(e.Id)) throw new FormatException("Every conduct event needs a non-empty id.");
            if (events.ContainsKey(e.Id)) throw new FormatException($"Conduct event '{e.Id}' appears more than once.");
            var reputation = ImmutableSortedDictionary.CreateBuilder<string, long>(StringComparer.Ordinal);
            foreach (var (category, change) in e.Reputation ?? [])
            {
                if (!categories.Contains(category)) throw new FormatException($"Conduct event '{e.Id}' names unknown category '{category}'.");
                if (change == 0) throw new FormatException($"Conduct event '{e.Id}' gives category '{category}' a change of 0.");
                reputation[category] = change;
            }
            if (e.Bounty == 0 && reputation.Count == 0) throw new FormatException($"Conduct event '{e.Id}' changes nothing.");
            events[e.Id] = new ConductEventRule(e.Id, e.Bounty, reputation.ToImmutable());
        }
        return new ConductRuleBook(doc.Version, categories, doc.MinimumReputation, doc.MaximumReputation, doc.MaximumBounty,
            events.ToImmutable());
    }

    public bool TryApply(string eventId, int count, long bounty, ImmutableSortedDictionary<string, long> reputation,
        out long bountyAfter, out ImmutableSortedDictionary<string, long> reputationAfter)
    {
        ArgumentNullException.ThrowIfNull(reputation);
        bountyAfter = bounty;
        reputationAfter = reputation;
        if (eventId is null || !_events.TryGetValue(eventId, out var rule) || count < 1 || count > MaximumCount) return false;

        // count <= 1000, so change * count fits unless a change is near long.MaxValue / 1000; Int128 keeps the clamp exact anyway.
        bountyAfter = Clamp((Int128)bounty + (Int128)rule.Bounty * count, 0, MaximumBounty);
        var next = reputation.ToBuilder();
        foreach (var (category, change) in rule.Reputation)
        {
            next.TryGetValue(category, out var held);
            var value = Clamp((Int128)held + (Int128)change * count, MinimumReputation, MaximumReputation);
            if (value == 0) next.Remove(category);
            else next[category] = value;
        }
        reputationAfter = next.ToImmutable();
        return true;
    }

    private static long Clamp(Int128 value, long minimum, long maximum) =>
        value < minimum ? minimum : value > maximum ? maximum : (long)value;

    private static bool IdOk(string? id) => !string.IsNullOrWhiteSpace(id) && ProfileInvariants.IsWellFormedUtf16(id);

    private sealed class Document
    {
        [JsonRequired] public string Version { get; set; } = "";
        [JsonRequired] public List<string> Categories { get; set; } = [];
        [JsonRequired] public long MinimumReputation { get; set; }
        [JsonRequired] public long MaximumReputation { get; set; }
        [JsonRequired] public long MaximumBounty { get; set; }
        [JsonRequired] public List<EventDocument> Events { get; set; } = [];
        /// <summary>Free text for the NEW/provisional notes; ignored by the rules.</summary>
        public string? Note { get; set; }
    }

    private sealed class EventDocument
    {
        [JsonRequired] public string Id { get; set; } = "";
        public long Bounty { get; set; }
        public Dictionary<string, long>? Reputation { get; set; }
        public string? Note { get; set; }
    }
}

/// <summary>One event's per-occurrence changes.</summary>
public sealed record ConductEventRule(string Id, long Bounty, ImmutableSortedDictionary<string, long> Reputation);
