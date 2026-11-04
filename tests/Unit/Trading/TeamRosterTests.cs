using RogueTrader.GameData;
using RogueTrader.Trading;

namespace RogueTrader.UnitTests.Trading;

/// <summary>Team balance (Steve, Nov 4 2026): the first pilot joins Cold Harbor, then pilots alternate so teams stay even.</summary>
public sealed class TeamRosterTests
{
    private static IReadOnlyList<TeamDefinition> Teams() => TestWorld.RogueCatalog().Sector.Teams;

    [Fact]
    public void FirstPilot_JoinsColdHarbor_ThenPilotsTakeTurns()
    {
        var roster = new TeamRoster(Teams());
        var joined = Enumerable.Range(0, 9).Select(i => roster.Join($"pilot{i}")!.Name).ToArray();
        Assert.Equal(new[] { "Cold Harbor", "Northern Cross", "Cassandra", "Yankee Station", "Cold Harbor", "Northern Cross", "Cassandra", "Yankee Station", "Cold Harbor" }, joined);
        Assert.Equal((3, 2, 2, 2), (roster.CountOf(0), roster.CountOf(1), roster.CountOf(2), roster.CountOf(3)));
    }

    [Fact]
    public void Rejoining_KeepsTheTeam_AndLeavingFreesThePlace()
    {
        var roster = new TeamRoster(Teams());
        foreach (var pilot in new[] { "a", "b", "c", "d" }) roster.Join(pilot);
        Assert.Equal(1, roster.Join("b")!.Index); // already in: same team, counted once
        Assert.Equal(1, roster.CountOf(1));
        Assert.True(roster.Leave("c"));
        Assert.False(roster.Leave("c"));
        Assert.Null(roster.TeamOf("c"));
        Assert.Equal(2, roster.Join("e")!.Index); // Cassandra is now the emptiest
    }

    [Fact]
    public void FullTeams_TakeNoMorePilots()
    {
        var teams = Teams();
        var roster = new TeamRoster(teams);
        var capacity = teams.Sum(t => t.MaxPlayers); // CFG MaxPlayers=10 each
        for (var i = 0; i < capacity; i++) Assert.NotNull(roster.Join($"p{i}"));
        Assert.Null(roster.Join("one-too-many"));
        Assert.All(teams, t => Assert.Equal(t.MaxPlayers, roster.CountOf(t.Index)));
        roster.Leave("p5");
        Assert.Equal(1, roster.Join("late")!.Index); // p5 was on team 1 (5 mod 4)
    }

    [Fact]
    public void ASectorWithoutTeams_TakesNobody()
    {
        Assert.Null(new TeamRoster([]).Join("solo"));
        Assert.Throws<ArgumentException>(() => new TeamRoster([new TeamDefinition { Index = 1 }, new TeamDefinition { Index = 1 }]));
    }
}
