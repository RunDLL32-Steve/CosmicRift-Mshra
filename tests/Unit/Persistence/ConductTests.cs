using System.Collections.Immutable;
using RogueTrader.Persistence;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>Queue C9 bounty/reputation rules through the transaction core. Every number is NEW provisional data, not original balance.</summary>
public sealed class ConductTests
{
    private static ConductRuleBook ShippedRules() =>
        ConductRuleBook.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", "conduct", "rules.json")));

    private static Task<TransactionResult> Record(Env env, string? id, string eventId, int count = 1, bool stationAccess = false) =>
        env.Proc.RecordConductAsync(env.Ctx(stationAccess), new RecordConductRequest(id ?? Fx.NewId(), Fx.ProfileId, "fixture-source-1", eventId, count));

    private static long Standing(PlayerProfile p, string category) => p.Reputation.TryGetValue(category, out var v) ? v : 0;

    [Fact]
    public void ShippedRulesLoadWithTheMasterCategories()
    {
        var rules = ShippedRules();
        Assert.Equal(["civilian", "mercenary", "miner", "pirate", "trader"], rules.Categories.ToArray());
        Assert.Equal(-1000, rules.MinimumReputation);
        Assert.Equal(1000, rules.MaximumReputation);
        Assert.Equal(100000, rules.MaximumBounty);
        Assert.Equal(7, rules.Events.Count);
    }

    [Fact]
    public async Task RepeatedEventsAddUpAndReportWhatChanged()
    {
        var env = await Env.CreateAsync(wallet: 300, conduct: ShippedRules());

        var first = await Record(env, null, "trader-attacked");
        var second = await Record(env, null, "trader-attacked");

        Assert.Equal(TransactionStatus.Applied, second.Status);
        Assert.Equal(250, first.Conduct!.BountyDelta);
        Assert.Equal(500, second.Conduct!.BountyAfter);
        Assert.Equal(new Dictionary<string, long> { ["civilian"] = -10, ["pirate"] = 5, ["trader"] = -20 }, second.Conduct.ReputationDeltas);
        var p = env.Snapshot;
        Assert.Equal(500, p.Bounty);
        Assert.Equal(-40, Standing(p, "trader"));
        Assert.Equal(-20, Standing(p, "civilian"));
        Assert.Equal(10, Standing(p, "pirate"));
        // Conduct moves no currency and writes no wallet ledger entry.
        Assert.Equal(300, p.Wallet);
        Assert.Empty(p.Ledger);
        Assert.Equal(2, p.Revision);
    }

    [Fact]
    public async Task ACountAppliesTheEventThatManyTimes()
    {
        var once = await Env.CreateAsync(conduct: ShippedRules());
        var batched = await Env.CreateAsync(conduct: ShippedRules());
        for (var i = 0; i < 3; i++) await Record(once, null, "pirate-destroyed");
        await Record(batched, null, "pirate-destroyed", count: 3);
        Assert.Equal(once.Snapshot.Reputation.ToArray(), batched.Snapshot.Reputation.ToArray());
        Assert.Equal(30, Standing(batched.Snapshot, "mercenary"));
    }

    [Fact]
    public async Task ADuplicateTransactionIdReplaysTheFirstOutcome()
    {
        var env = await Env.CreateAsync(conduct: ShippedRules());
        var first = await Record(env, "conduct-1", "civilian-destroyed");
        var after = env.Snapshot;

        var replay = await Record(env, "conduct-1", "civilian-destroyed");
        var reused = await Record(env, "conduct-1", "cargo-sold");

        Assert.True(replay.IsReplay);
        Assert.Equal(first.Conduct, replay.Conduct);
        Assert.Equal(first.Revision, replay.Revision);
        Assert.Equal(RejectionReason.TransactionIdReused, reused.Reason);
        Assert.Same(after, env.Snapshot);
        Assert.Equal(1000, env.Snapshot.Bounty);
    }

    [Fact]
    public async Task ValuesClampAtTheirBoundsAndNeutralStandingIsDropped()
    {
        var env = await Env.CreateAsync(conduct: ShippedRules());

        var flood = await Record(env, null, "civilian-destroyed", count: ConductRuleBook.MaximumCount);
        Assert.Equal(100000, env.Snapshot.Bounty);
        Assert.Equal(-1000, Standing(env.Snapshot, "civilian"));
        Assert.Equal(1000, Standing(env.Snapshot, "pirate"));
        Assert.Equal(100000, flood.Conduct!.BountyDelta);

        var saturated = env.Snapshot;
        var more = await Record(env, null, "civilian-destroyed");
        Assert.Equal(TransactionStatus.Applied, more.Status);
        Assert.Same(saturated, env.Snapshot); // no change, so no save and no new revision
        Assert.Equal(saturated.Revision, more.Revision);
        Assert.Equal(0, more.Conduct!.BountyDelta);
        Assert.Empty(more.Conduct.ReputationDeltas); // every category the event touches is already at its bound

        var cleared = await Record(env, null, "bounty-cleared");
        Assert.Equal(0, env.Snapshot.Bounty);
        Assert.Equal(-100000, cleared.Conduct!.BountyDelta);
        var atZero = env.Snapshot;
        var floor = await Record(env, null, "bounty-cleared");
        Assert.Equal(0, floor.Conduct!.BountyDelta);
        Assert.Same(atZero, env.Snapshot);

        // pirate +5 +5 -10 returns to neutral and is no longer stored.
        var fresh = await Env.CreateAsync(conduct: ShippedRules());
        await Record(fresh, null, "trader-attacked");
        await Record(fresh, null, "trader-attacked");
        var back = await Record(fresh, null, "pirate-destroyed");
        Assert.False(fresh.Snapshot.Reputation.ContainsKey("pirate"));
        Assert.Equal(-10, back.Conduct!.ReputationDeltas["pirate"]);
    }

    [Fact]
    public async Task AnEventThatChangesNothingIsNotSavedOrRecorded()
    {
        var env = await Env.CreateAsync(conduct: ShippedRules());
        var before = env.Snapshot;

        var noop = await Record(env, "conduct-noop", "bounty-cleared");

        Assert.Equal(TransactionStatus.Applied, noop.Status);
        Assert.False(noop.IsReplay);
        Assert.Equal(0, noop.Conduct!.BountyDelta);
        Assert.Empty(noop.Conduct.ReputationDeltas);
        Assert.Equal(before.Revision, noop.Revision);
        Assert.Same(before, env.Snapshot);
        Assert.Empty(env.Snapshot.AppliedTransactions);
        var stored = await env.Repo.LoadAsync(Fx.ProfileId);
        ProfileAssert.Equivalent(before, stored.Profile!);

        // Chosen rule: the id is not remembered, so the same id evaluates again against the current state. A retry after a
        // bounty was raised therefore clears it; a retry while nothing changed stays a no-op.
        var again = await Record(env, "conduct-noop", "bounty-cleared");
        Assert.False(again.IsReplay);
        Assert.Same(before, env.Snapshot);
        await Record(env, null, "trader-attacked");
        var retried = await Record(env, "conduct-noop", "bounty-cleared");
        Assert.False(retried.IsReplay);
        Assert.Equal(-250, retried.Conduct!.BountyDelta);
        Assert.Equal(0, env.Snapshot.Bounty);
    }

    [Theory]
    [InlineData("no-such-event", 1, RejectionReason.UnknownConductEvent)]
    [InlineData("cargo-sold", ConductRuleBook.MaximumCount + 1, RejectionReason.UnknownConductEvent)]
    [InlineData("cargo-sold", 0, RejectionReason.InvalidRequest)]
    [InlineData("cargo-sold", -1, RejectionReason.InvalidRequest)]
    [InlineData("", 1, RejectionReason.InvalidRequest)]
    public async Task RefusedEventsChangeNothing(string eventId, int count, RejectionReason reason)
    {
        var env = await Env.CreateAsync(conduct: ShippedRules());
        var before = env.Snapshot;
        var result = await Record(env, null, eventId, count);
        Assert.Equal(reason, result.Reason);
        Assert.Same(before, env.Snapshot);
    }

    [Fact]
    public async Task WithoutRulesEveryEventIsRefusedAndOtherIdentitiesCannotRecord()
    {
        var env = await Env.CreateAsync();
        Assert.Equal(RejectionReason.UnknownConductEvent, (await Record(env, null, "cargo-sold")).Reason);

        var ruled = await Env.CreateAsync(conduct: ShippedRules());
        var foreign = await ruled.Proc.RecordConductAsync(new TransactionContext("someone-else", true),
            new RecordConductRequest(Fx.NewId(), Fx.ProfileId, "fixture-source-1", "cargo-sold"));
        Assert.Equal(RejectionReason.IdentityMismatch, foreign.Reason);
        Assert.Equal(0, ruled.Snapshot.Revision);
    }

    [Fact]
    public async Task StandingSurvivesSaveAndReopenAndReplaysAfterRestart()
    {
        using var dir = new TempDirectory();
        var options = new PersistenceOptions();
        PlayerProfile saved;
        TransactionResult first;
        using (var repo = FileProfileRepository.Open(dir.Path, options, ProfileSchemaMigrations.BuiltIn))
        {
            var env = await Env.CreateAsync(wallet: 50, repo: repo, options: options, conduct: ShippedRules());
            first = await Record(env, "conduct-persist", "trader-attacked", count: 2);
            await Record(env, null, "ore-mined", count: 5);
            Assert.Equal(TransactionStatus.Applied, (await env.Buy(null, Fx.OreA, 1)).Status);
            saved = env.Snapshot;
        }
        using (var repo = FileProfileRepository.Open(dir.Path, options, ProfileSchemaMigrations.BuiltIn))
        {
            var env = await Env.CreateAsync(createProfile: false, repo: repo, options: options, conduct: ShippedRules());
            ProfileAssert.Equivalent(saved, env.Snapshot);
            Assert.Equal(500, env.Snapshot.Bounty);
            Assert.Equal(5, Standing(env.Snapshot, "miner"));
            Assert.Single(env.Snapshot.Ledger); // only the purchase

            var replay = await Record(env, "conduct-persist", "trader-attacked", count: 2);
            Assert.True(replay.IsReplay);
            Assert.Equal(first.Conduct!.BountyAfter, replay.Conduct!.BountyAfter);
            Assert.Equal(first.Conduct.ReputationDeltas.ToArray(), replay.Conduct.ReputationDeltas.ToArray());
            Assert.Equal(saved.Revision, env.Snapshot.Revision);
        }
    }

    [Fact]
    public async Task ASchemaFourFileOpensWithNoBountyAndNeutralStanding()
    {
        var env = await Env.CreateAsync(wallet: 500);
        await env.Buy(null, Fx.OreA, 1);
        var envelope = System.Text.Json.Nodes.JsonNode.Parse(ProfileSerializer.Serialize(env.Snapshot))!.AsObject();
        var payload = System.Text.Json.Nodes.JsonNode.Parse(envelope["payload"]!.GetValue<string>())!.AsObject();
        payload.Remove("bounty");
        payload.Remove("reputation");
        foreach (var t in payload["appliedTransactions"]!.AsArray()) t!.AsObject().Remove("conduct");
        var text = payload.ToJsonString();
        envelope["payload"] = text;
        envelope["sha256"] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
        envelope["schemaVersion"] = 4;

        var decoded = ProfileSerializer.Decode(System.Text.Encoding.UTF8.GetBytes(envelope.ToJsonString()), Array.Empty<IProfileMigration>());

        Assert.True(decoded.Succeeded);
        Assert.Equal(5, decoded.Profile!.SchemaVersion);
        Assert.Equal(0, decoded.Profile.Bounty);
        Assert.Empty(decoded.Profile.Reputation);
        Assert.Contains(decoded.Diagnostics, d => d.Code == DiagnosticCode.MigrationApplied);
    }

    [Fact]
    public void InvariantsRejectANegativeBountyAndStoredNeutralStanding()
    {
        var good = PlayerProfile.CreateNew(Fx.ProfileId, 0, null);
        Assert.Empty(ProfileInvariants.Validate(good, new PersistenceOptions()));
        Assert.NotEmpty(ProfileInvariants.Validate(good with { Bounty = -1 }, new PersistenceOptions()));
        Assert.NotEmpty(ProfileInvariants.Validate(good with
        {
            Reputation = ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, [KeyValuePair.Create("trader", 0L)]),
        }, new PersistenceOptions()));
    }

    [Theory]
    [InlineData("""{"version":"v","categories":["a"],"minimumReputation":-1,"maximumReputation":1,"maximumBounty":1,"events":[{"id":"e","bounty":1}],"decayPerDay":1}""")]
    [InlineData("""{"version":"v","categories":["a"],"minimumReputation":-1,"maximumReputation":1,"maximumBounty":1,"events":[{"id":"e","reputation":{"b":1}}]}""")]
    [InlineData("""{"version":"v","categories":["a"],"minimumReputation":-1,"maximumReputation":1,"maximumBounty":1,"events":[{"id":"e","reputation":{"a":0}}]}""")]
    [InlineData("""{"version":"v","categories":["a"],"minimumReputation":-1,"maximumReputation":1,"maximumBounty":1,"events":[{"id":"e","bounty":1},{"id":"e","bounty":2}]}""")]
    [InlineData("""{"version":"v","categories":["a","a"],"minimumReputation":-1,"maximumReputation":1,"maximumBounty":1,"events":[{"id":"e","bounty":1}]}""")]
    [InlineData("""{"version":"v","categories":["a"],"minimumReputation":1,"maximumReputation":5,"maximumBounty":1,"events":[{"id":"e","bounty":1}]}""")]
    [InlineData("""{"version":"v","categories":["a"],"minimumReputation":-1,"maximumReputation":1,"maximumBounty":1,"events":[{"id":"e"}]}""")]
    public void MalformedRuleFilesAreRefused(string json) =>
        Assert.Throws<FormatException>(() => ConductRuleBook.FromJson(json));
}
