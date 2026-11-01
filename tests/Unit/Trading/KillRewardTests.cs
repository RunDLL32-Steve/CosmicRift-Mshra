using RogueTrader.OriginalData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;
using RogueTrader.UnitTests.Persistence;

namespace RogueTrader.UnitTests.Trading;

/// <summary>
/// Queue M7 slice: a destroyed sector target pays the raw rogue.cfg [Cash] KillReward once through the idempotent
/// GrantReward core. The flat per-kill reading and the practice targets are NEW (RT-F051 stays open).
/// </summary>
public sealed class KillRewardTests
{
    private const string Profile = "m7-pilot";

    private static StationDefinition LoadStation() =>
        StationDefinition.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "stations", "trading_outpost.json")));

    private static KillRewardDefinition LoadReward() =>
        KillRewardDefinition.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "rewards", "kill_reward.json")));

    private static async Task<StationProfileSession> OpenAsync(IProfileRepository repo, KillRewardDefinition? reward = null)
    {
        var session = new StationProfileSession(repo, LoadStation(), "starter_interceptor") { KillReward = reward ?? LoadReward() };
        Assert.True((await session.OpenOrCreateAsync(Profile)).Succeeded);
        return session;
    }

    private static TargetKill PlayerKill(string target = "target_alpha") => new(target, WorldSimulation.PlayerShipId, 42);

    [Fact]
    public void ShippedReward_IsLabelledAndUsesTheRawKillReward()
    {
        var reward = LoadReward();
        Assert.Equal("ORIGINAL+NEW", reward.EvidenceClassification);
        Assert.Equal(75, reward.Credits);
        Assert.Contains("[Cash]", reward.Source);
        Assert.NotEmpty(reward.NewRules);
    }

    [RequiresOriginalAssetsFact]
    public void ShippedCreditsMatchThePinnedRogueCfg()
    {
        var cash = OriginalRewardBlock.From(OriginalCfg.LoadRogue(OriginalAssets.Root!), "Cash");
        Assert.Equal(cash.KillReward, LoadReward().Credits);
        Assert.Contains($"line {cash.Line}", LoadReward().Source);
    }

    [Theory]
    [InlineData("""{ "id": "", "credits": 5 }""")]
    [InlineData("""{ "id": "r", "credits": -1 }""")]
    [InlineData("""{ "id": "r" }""")]
    public void Definition_RejectsBadFiles(string json) => Assert.Throws<InvalidDataException>(() => KillRewardDefinition.FromJson(json));

    [Fact]
    public void World_RecordsEachDestroyedTargetOnce()
    {
        var world = new WorldSimulation(TestWorld.ExternalCatalog());
        for (var tick = 0; tick < 1200 && world.Kills.Count == 0; tick++) world.Tick(new ShipInput(0, 0, true));
        TestWorld.Tick(world, 240, new ShipInput(0, 0, true));

        var kill = Assert.Single(world.Kills);
        Assert.Equal("target_alpha", kill.TargetId);
        Assert.Equal(WorldSimulation.PlayerShipId, kill.KillerShipId);
        Assert.True(kill.Tick > 0 && kill.Tick <= world.TickNumber);
        Assert.Equal(1, world.DestroyedTargets);
    }

    [Fact]
    public async Task Kill_PaysOnce_AndAReplayIsADuplicate()
    {
        var session = await OpenAsync(new InMemoryProfileRepository());
        var start = session.Profile!.Wallet;

        var paid = await session.GrantKillRewardAsync("sector-1", PlayerKill());
        var replay = await session.GrantKillRewardAsync("sector-1", PlayerKill());

        Assert.Equal(TransactionStatus.Applied, paid!.Status);
        Assert.False(paid.IsReplay);
        Assert.Equal(75, paid.CurrencyDelta);
        Assert.True(replay!.IsReplay);
        Assert.Equal(start + 75, session.Profile!.Wallet);
        Assert.Equal(paid.Revision, session.Profile.Revision);
    }

    [Fact]
    public async Task SameTargetInANewSectorInstance_PaysAgain()
    {
        var session = await OpenAsync(new InMemoryProfileRepository());
        await session.GrantKillRewardAsync("sector-1", PlayerKill());
        var again = await session.GrantKillRewardAsync("sector-2", PlayerKill());

        Assert.False(again!.IsReplay);
        Assert.Equal(150, session.Profile!.Wallet);
    }

    [Fact]
    public async Task OtherKillerOrNoReward_GrantsNothing()
    {
        var session = await OpenAsync(new InMemoryProfileRepository());
        Assert.Null(await session.GrantKillRewardAsync("sector-1", new TargetKill("target_alpha", "target_beta", 3)));
        var unpaid = await OpenAsync(new InMemoryProfileRepository(), KillRewardDefinition.None);
        Assert.Null(await unpaid.GrantKillRewardAsync("sector-1", PlayerKill()));
        Assert.Equal(0, session.Profile!.Revision);
    }

    [Fact]
    public async Task FailedSave_LeavesNoPartialChange_AndTheRetryPaysOnce()
    {
        var repo = new FaultInjectingProfileRepository(new InMemoryProfileRepository()) { Mode = FaultMode.FailBeforeWrite };
        var session = await OpenAsync(repo);
        var before = session.Profile!;

        var failed = await session.GrantKillRewardAsync("sector-1", PlayerKill());
        Assert.Equal(RejectionReason.PersistenceFailed, failed!.Reason);
        Assert.Same(before, session.Profile);

        repo.Mode = FaultMode.None;
        Assert.Equal(RejectionReason.ProfileNotOpen, (await session.GrantKillRewardAsync("sector-1", PlayerKill()))!.Reason);
        Assert.True((await session.ReloadAsync()).Succeeded);
        var retried = await session.GrantKillRewardAsync("sector-1", PlayerKill());
        Assert.Equal(TransactionStatus.Applied, retried!.Status);
        Assert.Equal(before.Wallet + 75, session.Profile!.Wallet);
    }

    [Fact]
    public async Task LostAcknowledgement_RetryAfterReloadIsADuplicate()
    {
        var repo = new FaultInjectingProfileRepository(new InMemoryProfileRepository()) { Mode = FaultMode.ThrowAfterWrite };
        var session = await OpenAsync(repo);
        var start = session.Profile!.Wallet;

        Assert.Equal(RejectionReason.PersistenceFailed, (await session.GrantKillRewardAsync("sector-1", PlayerKill()))!.Reason);
        repo.Mode = FaultMode.None;
        Assert.True((await session.ReloadAsync()).Succeeded);
        var retried = await session.GrantKillRewardAsync("sector-1", PlayerKill());

        Assert.True(retried!.IsReplay);
        Assert.Equal(start + 75, session.Profile!.Wallet);
    }

    [Fact]
    public void KillTransactionId_IsStableAndBounded()
    {
        Assert.Equal("kill-abc-target_alpha", StationProfileSession.KillTransactionId("abc", "target_alpha"));
        var longId = StationProfileSession.KillTransactionId(new string('s', 32), new string('t', 200));
        Assert.True(longId.Length <= ProfileInvariants.MaxTransactionIdLength);
        Assert.Equal(longId, StationProfileSession.KillTransactionId(new string('s', 32), new string('t', 200)));
        Assert.NotEqual(longId, StationProfileSession.KillTransactionId(new string('s', 32), new string('u', 200)));
    }

    [Fact]
    public async Task UncommittedPickups_AreTheHoldUnitsTheProfileLacks()
    {
        var session = await OpenAsync(new InMemoryProfileRepository());
        await session.CommitPickupsAsync(new Dictionary<string, int> { ["2008"] = 4 });

        var gained = session.UncommittedPickups(new Dictionary<string, int> { ["2008"] = 6, ["2020"] = 2, ["2004"] = 0 });

        Assert.Equal(new Dictionary<string, int> { ["2008"] = 2, ["2020"] = 2 }, gained);
    }
}
