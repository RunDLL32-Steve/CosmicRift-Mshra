using System.Text.Json;

namespace RogueTrader.Trading;

/// <summary>
/// Credits paid when the player destroys a sector target (queue M7 slice). <c>data/rewards/kill_reward.json</c> carries the
/// raw rogue.cfg <c>[Cash] KillReward</c> value; reading it as a flat per-kill credit amount, and every other rule, is
/// NEW/provisional and listed in <see cref="NewRules"/> (RT-F051 stays open).
/// </summary>
public sealed record KillRewardDefinition(string Id, long Credits)
{
    public string EvidenceClassification { get; init; } = "NEW";
    public string Source { get; init; } = "";
    public IReadOnlyList<string> NewRules { get; init; } = [];

    /// <summary>No credits for a kill (used when no reward file is loaded).</summary>
    public static KillRewardDefinition None { get; } = new("none", 0);

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static KillRewardDefinition FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<Document>(json, Options) ?? throw new InvalidDataException("Kill reward definition is empty.");
        if (string.IsNullOrWhiteSpace(document.Id)) throw new InvalidDataException("Kill reward definition needs an id.");
        if (document.Credits is not { } credits || credits < 0) throw new InvalidDataException("Kill reward credits must be zero or more.");
        return new KillRewardDefinition(document.Id, credits)
        {
            EvidenceClassification = document.EvidenceClassification ?? "NEW",
            Source = document.Source ?? "",
            NewRules = document.NewRules ?? [],
        };
    }

    private sealed record Document(string? Id, long? Credits, string? EvidenceClassification, string? Source, string[]? NewRules);
}
