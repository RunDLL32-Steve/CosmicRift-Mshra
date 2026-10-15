using RogueTrader.Persistence;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>Master plan 20.2: simultaneous transactions cannot double-spend funds. Fixture numbers are NEW provisional.</summary>
public sealed class TransactionConcurrencyTests
{
    private const int Parallel = 64;

    [Fact]
    public async Task ConcurrentBuys_OnlyAffordableOnesApply()
    {
        const int affordable = 20; // NEW fixture: funds for exactly 20 purchases at price 10
        var env = await Env.CreateAsync(wallet: affordable * 10);

        var results = await Task.WhenAll(Enumerable.Range(0, Parallel).Select(_ => Task.Run(() => env.Buy(null, Fx.OreA, 1))));

        Assert.Equal(affordable, results.Count(r => r.Status == TransactionStatus.Applied));
        Assert.Equal(Parallel - affordable, results.Count(r => r.Reason == RejectionReason.InsufficientFunds));
        Assert.All(results, r => Assert.True(r.WalletAfter >= 0));
        Assert.Equal(0, env.Snapshot.Wallet);
        Assert.Equal(affordable, env.Snapshot.Inventory[Fx.OreA]);
        Assert.Equal(affordable, env.Snapshot.Revision);
        Assert.Empty(ProfileInvariants.Validate(env.Snapshot, env.Options));

        var stored = (await env.Repo.LoadAsync(Fx.ProfileId)).Profile!;
        ProfileAssert.Equivalent(env.Snapshot, stored);
    }

    [Fact]
    public async Task ConcurrentBuys_AgainstFileRepository_OnlyAffordableOnesApply()
    {
        using var dir = new TempDirectory();
        var options = new PersistenceOptions();
        using var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>());
        var env = await Env.CreateAsync(wallet: 100, repo: repo, options: options); // NEW fixture: 10 purchases at price 10

        var results = await Task.WhenAll(Enumerable.Range(0, Parallel).Select(_ => Task.Run(() => env.Buy(null, Fx.OreA, 1))));

        Assert.Equal(10, results.Count(r => r.Status == TransactionStatus.Applied));
        Assert.Equal(0, env.Snapshot.Wallet);
        var stored = (await repo.LoadAsync(Fx.ProfileId)).Profile!;
        ProfileAssert.Equivalent(env.Snapshot, stored);
    }

    [Fact]
    public async Task ConcurrentDuplicateRewards_ApplyExactlyOnce()
    {
        var env = await Env.CreateAsync(wallet: 0);

        var results = await Task.WhenAll(Enumerable.Range(0, Parallel)
            .Select(_ => Task.Run(() => env.Reward("fixture-tx-reward-1", 50, (Fx.OreA, 2)))));

        Assert.Equal(1, results.Count(r => !r.IsReplay));
        Assert.Equal(Parallel - 1, results.Count(r => r.IsReplay));
        Assert.All(results, r => Assert.Equal(TransactionStatus.Applied, r.Status));
        Assert.All(results, r => Assert.Equal(50, r.WalletAfter));
        Assert.Equal(50, env.Snapshot.Wallet);
        Assert.Equal(2, env.Snapshot.Inventory[Fx.OreA]);
        Assert.Equal(1, env.Snapshot.Revision);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Repository_ConcurrentSavesSameExpectedRevision_OnlyOneSucceeds(bool useFiles)
    {
        using var dir = new TempDirectory();
        var options = new PersistenceOptions();
        IProfileRepository repo = useFiles
            ? FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>())
            : new InMemoryProfileRepository(options);
        try
        {
            var start = PlayerProfile.CreateNew(Fx.ProfileId, 100, Fx.Hull);
            Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(start)).Status);

            var saves = Enumerable.Range(1, 32).Select(i =>
                Task.Run(() => repo.SaveAsync(start with { Revision = 1, Wallet = 100 + i }, 0)));
            var results = await Task.WhenAll(saves);

            Assert.Equal(1, results.Count(r => r.Status == ProfileSaveStatus.Saved));
            Assert.Equal(31, results.Count(r => r.Status == ProfileSaveStatus.RevisionConflict));
            var stored = (await repo.LoadAsync(Fx.ProfileId)).Profile!;
            Assert.Equal(1, stored.Revision);
            Assert.InRange(stored.Wallet, 101, 132);
        }
        finally { (repo as IDisposable)?.Dispose(); }
    }

    [Fact]
    public async Task TwoProcessorsSharingRepository_CannotDoubleSpend()
    {
        // Each purchase alone is affordable; both together are not. The repository CAS must reject the stale writer.
        var shared = new InMemoryProfileRepository();
        var a = await Env.CreateAsync(wallet: 100, repo: shared);
        var b = await Env.CreateAsync(repo: shared, createProfile: false);

        var results = await Task.WhenAll(
            Task.Run(() => a.Buy(null, Fx.OreA, 7)),   // costs 70 (NEW fixture price 10)
            Task.Run(() => b.Buy(null, Fx.OreA, 7)));

        Assert.Equal(1, results.Count(r => r.Status == TransactionStatus.Applied));
        var rejected = Assert.Single(results, r => r.Status == TransactionStatus.Rejected);
        Assert.Equal(RejectionReason.PersistenceConflict, rejected.Reason);

        var stored = (await shared.LoadAsync(Fx.ProfileId)).Profile!;
        Assert.Equal(30, stored.Wallet);
        Assert.Equal(7, stored.Inventory[Fx.OreA]);
        Assert.Equal(1, stored.Revision);
    }

    [Fact]
    public async Task TwoProcessorsSharingFileRepository_CannotDoubleSpend()
    {
        using var dir = new TempDirectory();
        var options = new PersistenceOptions();
        using var repo = FileProfileRepository.Open(dir.Path, options, Array.Empty<IProfileMigration>());
        var a = await Env.CreateAsync(wallet: 100, repo: repo, options: options);
        var b = await Env.CreateAsync(repo: repo, options: options, createProfile: false);

        var results = await Task.WhenAll(
            Task.Run(() => a.Buy(null, Fx.OreA, 7)),
            Task.Run(() => b.Buy(null, Fx.OreA, 7)));

        Assert.Equal(1, results.Count(r => r.Status == TransactionStatus.Applied));
        Assert.Equal(RejectionReason.PersistenceConflict, Assert.Single(results, r => r.Status == TransactionStatus.Rejected).Reason);
        Assert.Equal(30, (await repo.LoadAsync(Fx.ProfileId)).Profile!.Wallet);
    }

    [Fact]
    public async Task StaleProcessor_AfterConflict_RecoversOnlyByReopening()
    {
        var shared = new InMemoryProfileRepository();
        var a = await Env.CreateAsync(wallet: 100, repo: shared);
        var b = await Env.CreateAsync(repo: shared, createProfile: false);
        await a.Buy(null, Fx.OreA, 1);

        Assert.Equal(RejectionReason.PersistenceConflict, (await b.Buy(null, Fx.OreA, 1)).Reason);
        Assert.Equal(RejectionReason.ProfileNotOpen, (await b.Buy(null, Fx.OreA, 1)).Reason);

        Assert.True((await b.Proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
        Assert.Equal(TransactionStatus.Applied, (await b.Buy(null, Fx.OreA, 1)).Status);
        Assert.Equal(80, b.Snapshot.Wallet);
    }
}
