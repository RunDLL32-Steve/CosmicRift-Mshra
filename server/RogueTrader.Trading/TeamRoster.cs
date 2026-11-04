using RogueTrader.GameData;

namespace RogueTrader.Trading;

/// <summary>
/// Puts each pilot who joins the sector on a team (Steve, November 4, 2026): the first pilot joins the first team (Cold Harbor),
/// and after that each pilot joins the team with the fewest pilots in the sector, ties going to the team listed first, so the
/// teams take turns and stay even. A team at its CFG MaxPlayers takes no more pilots. A pilot who is already in keeps the team,
/// and a pilot who leaves frees the place. The original did not balance teams (CFG [Arena] ForceEvenTeams=0), so the rule is NEW.
/// Not thread-safe: the server owns one roster per sector and calls it from its tick thread.
/// </summary>
public sealed class TeamRoster
{
    private readonly IReadOnlyList<TeamDefinition> _teams;
    private readonly Dictionary<string, TeamDefinition> _pilots = new(StringComparer.Ordinal);

    public TeamRoster(IReadOnlyList<TeamDefinition> teams)
    {
        ArgumentNullException.ThrowIfNull(teams);
        if (teams.Any(t => t is null)) throw new ArgumentException("Null team.", nameof(teams));
        if (teams.Select(t => t.Index).Distinct().Count() != teams.Count) throw new ArgumentException("Two teams share one index.", nameof(teams));
        _teams = teams.ToArray();
    }

    public IReadOnlyList<TeamDefinition> Teams => _teams;

    /// <summary>Pilots on a team now.</summary>
    public int CountOf(int team) => _pilots.Values.Count(t => t.Index == team);

    /// <summary>The team a pilot in the sector is on; null when the pilot is not in.</summary>
    public TeamDefinition? TeamOf(string pilotId) => _pilots.GetValueOrDefault(pilotId ?? "");

    /// <summary>
    /// Adds a pilot and returns their team: the same team when they are already in; otherwise the open team with the fewest
    /// pilots, the earlier one on a tie. Null when every team is full (or the sector has no teams).
    /// </summary>
    public TeamDefinition? Join(string pilotId)
    {
        ArgumentException.ThrowIfNullOrEmpty(pilotId);
        if (_pilots.TryGetValue(pilotId, out var current)) return current;
        TeamDefinition? best = null;
        var bestCount = int.MaxValue;
        foreach (var team in _teams)
        {
            var count = CountOf(team.Index);
            if (team.MaxPlayers > 0 && count >= team.MaxPlayers) continue;
            if (count < bestCount)
            {
                best = team;
                bestCount = count;
            }
        }
        if (best is not null) _pilots[pilotId] = best;
        return best;
    }

    /// <summary>Removes a pilot; false when they were not in.</summary>
    public bool Leave(string pilotId) => _pilots.Remove(pilotId ?? "");
}
