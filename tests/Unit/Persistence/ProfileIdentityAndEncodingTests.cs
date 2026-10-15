using RogueTrader.Persistence;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>
/// Review regressions: profile identity on disk, reserved file names, ids that cannot be stored verbatim, parallel
/// profiles in the in-memory repository, caller-owned reward collections and boundary arithmetic. Fixture values are
/// NEW provisional test data, not game balance.
/// </summary>
public sealed class ProfileIdentityAndEncodingTests
{
    private static readonly PersistenceOptions Options = new();
    private static readonly IReadOnlyList<IProfileMigration> NoMigrations = Array.Empty<IProfileMigration>();

    private static FileProfileRepository Open(TempDirectory dir) => FileProfileRepository.Open(dir.Path, Options, NoMigrations);

    // ---- Identity ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task Load_FileHoldingAnotherProfile_IsCorruptAndLeftUntouched()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(PlayerProfile.CreateNew("fixture-owner", 500, null))).Status);
        var bytes = await File.ReadAllBytesAsync(dir.File("fixture-owner.profile.json"));
        await File.WriteAllBytesAsync(dir.File("fixture-other.profile.json"), bytes); // copied save

        var result = await repo.LoadAsync("fixture-other");

        Assert.Equal(ProfileLoadStatus.Corrupt, result.Status);
        Assert.Null(result.Profile);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCode.InvariantViolation);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(dir.File("fixture-other.profile.json"))); // not quarantined
        Assert.False(Directory.Exists(dir.File("quarantine")));
        var save = await repo.SaveAsync(PlayerProfile.CreateNew("fixture-other", 1, null) with { Revision = 1 }, 0);
        Assert.Equal(ProfileSaveStatus.RevisionConflict, save.Status);
    }

    [Fact]
    public async Task CaseVariantProfileIds_NeverShareState()
    {
        // On a case-insensitive file system both ids map to the same file; the variant must not be served the
        // other profile (it was: "alice" loaded and spent "Alice"'s wallet).
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(PlayerProfile.CreateNew("Alice", 500, null))).Status);
        var proc = new TransactionProcessor(repo, new FixturePriceSource(), new FixtureCargoPolicy(), Options);
        Assert.True((await proc.OpenProfileAsync("Alice")).Succeeded);

        var variant = await proc.OpenProfileAsync("alice");
        var buy = await proc.BuyAsync(new TransactionContext("alice", true), new BuyRequest("fixture-tx-1", "alice", Fx.Station, Fx.OreA, 3));

        Assert.Contains(variant.Status, new[] { ProfileLoadStatus.NotFound, ProfileLoadStatus.Corrupt });
        Assert.Equal(RejectionReason.ProfileNotOpen, buy.Reason);
        var alice = (await repo.LoadAsync("Alice")).Profile!;
        Assert.Equal(500, alice.Wallet);
        Assert.Equal(0, alice.Revision);
    }

    [Fact]
    public async Task OpenProfile_RepositoryReturningAnotherId_IsRefused()
    {
        var inner = new InMemoryProfileRepository();
        Assert.Equal(ProfileSaveStatus.Saved, (await inner.CreateAsync(PlayerProfile.CreateNew("fixture-owner", 500, null))).Status);
        var proc = new TransactionProcessor(new RedirectingRepository(inner, "fixture-owner"), new FixturePriceSource(), new FixtureCargoPolicy(), Options);

        var opened = await proc.OpenProfileAsync(Fx.ProfileId);
        var buy = await proc.BuyAsync(new TransactionContext(Fx.ProfileId, true), new BuyRequest(Fx.NewId(), Fx.ProfileId, Fx.Station, Fx.OreA, 1));

        Assert.Equal(ProfileLoadStatus.Corrupt, opened.Status);
        Assert.Null(opened.Profile);
        Assert.Equal(RejectionReason.ProfileNotOpen, buy.Reason);
    }

    // ---- Reserved names ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData("NUL")]
    [InlineData("con")]
    [InlineData("Aux")]
    [InlineData("prn")]
    [InlineData("COM1")]
    [InlineData("lpt9")]
    public async Task ReservedDeviceNames_AreInvalidProfileIds(string id)
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);

        Assert.Equal(ProfileSaveStatus.Invalid, (await repo.CreateAsync(PlayerProfile.CreateNew(id, 5, null))).Status);
        await Assert.ThrowsAsync<ArgumentException>(() => repo.LoadAsync(id));
        Assert.Equal(ProfileSaveStatus.Invalid, (await new InMemoryProfileRepository().CreateAsync(PlayerProfile.CreateNew(id, 5, null))).Status);
    }

    [Theory]
    [InlineData("NULL")]
    [InlineData("CONX")]
    [InlineData("COM")]
    [InlineData("LPTA")]
    [InlineData("com10")]
    public async Task NamesThatOnlyResembleDevices_AreAccepted(string id)
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);

        Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(PlayerProfile.CreateNew(id, 5, null))).Status);
        Assert.Equal(ProfileLoadStatus.Loaded, (await repo.LoadAsync(id)).Status);
    }

    // ---- Ids that cannot be stored verbatim ----------------------------------------------------------------------

    [Fact]
    public async Task UnpairedSurrogateIds_AreRejectedInsteadOfAlteredOnSave()
    {
        // Before the fix "\uD800" was saved as U+FFFD, so a reward replayed after a restart was applied twice.
        var env = await Env.CreateAsync(wallet: 0);
        var badTx = await env.Reward("fixture-tx-\uD800", 50);
        var badItem = await env.Reward(null, 0, ("fixture-ore-\uDC00", 1));
        var badStation = await env.Buy(null, Fx.OreA, 1, station: "\uD800");
        var badUpgrade = await env.BuyUpgrade(null, "\uDBFF");

        Assert.All(new[] { badTx, badItem, badStation, badUpgrade }, r => Assert.Equal(RejectionReason.InvalidRequest, r.Reason));
        Assert.Equal(0, env.Snapshot.Revision);
        Assert.Equal(TransactionStatus.Applied, (await env.Reward("fixture-tx-😀", 1, ("fixture-ore-😀", 1))).Status);
    }

    [Fact]
    public async Task Save_ProfileWithUnpairedSurrogate_IsInvalid()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        var start = PlayerProfile.CreateNew(Fx.ProfileId, 5, null);
        Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(start)).Status);

        var save = await repo.SaveAsync(start with { Revision = 1, Inventory = Env.Items(("bad-\uD800", 1)) }, 0);
        var hull = await repo.SaveAsync(start with { Revision = 1, ShipHullId = "\uDC00" }, 0);

        Assert.Equal(ProfileSaveStatus.Invalid, save.Status);
        Assert.Equal(ProfileSaveStatus.Invalid, hull.Status);
        Assert.Equal(5, (await repo.LoadAsync(Fx.ProfileId)).Profile!.Wallet);
    }

    // ---- In-memory repository under parallel profiles -------------------------------------------------------------

    [Fact]
    public async Task InMemoryRepository_ManyProfilesInParallel_StayConsistent()
    {
        // Different ids hold different locks, so the backing store itself must be thread-safe (it was a plain Dictionary).
        for (var round = 0; round < 5; round++)
        {
            var repo = new InMemoryProfileRepository();
            await Task.WhenAll(Enumerable.Range(0, 128).Select(i => Task.Run(async () =>
            {
                var id = $"fixture-p{round}-{i}";
                Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(PlayerProfile.CreateNew(id, 1, null))).Status);
                for (var r = 1; r <= 5; r++)
                {
                    var loaded = await repo.LoadAsync(id);
                    Assert.Equal(ProfileLoadStatus.Loaded, loaded.Status);
                    Assert.Equal(ProfileSaveStatus.Saved, (await repo.SaveAsync(loaded.Profile! with { Revision = r, Wallet = r }, r - 1)).Status);
                }
            })));
            for (var i = 0; i < 128; i++)
                Assert.Equal(5, (await repo.LoadAsync($"fixture-p{round}-{i}")).Profile!.Wallet);
        }
    }

    // ---- Caller-owned reward collections -------------------------------------------------------------------------

    [Fact]
    public async Task GrantReward_CallerMutatesItemsWhileQueued_AppliesTheItemsItSubmitted()
    {
        var repo = new InMemoryProfileRepository();
        Assert.Equal(ProfileSaveStatus.Saved, (await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, 0, null))).Status);
        var cargo = new GatedCargoPolicy();
        var proc = new TransactionProcessor(repo, new FixturePriceSource(), cargo, Options);
        Assert.True((await proc.OpenProfileAsync(Fx.ProfileId)).Succeeded);
        var ctx = new TransactionContext(Fx.ProfileId, false);
        GrantRewardRequest Request(string id, Dictionary<string, int> items) => new(id, Fx.ProfileId, "fixture-source-1", 0, items);

        cargo.BlockNext();
        var holder = Task.Run(() => proc.GrantRewardAsync(ctx, Request("fixture-tx-hold", new() { [Fx.OreA] = 1 }))); // holds the lock inside Fits
        Assert.True(cargo.Entered.Wait(TimeSpan.FromSeconds(10)));

        var items = new Dictionary<string, int>(StringComparer.Ordinal) { [Fx.OreA] = 2 };
        var queued = proc.GrantRewardAsync(ctx, Request("fixture-tx-queued", items));
        items[Fx.OreA] = -100;          // the caller reuses its collection while the request waits for the lock
        items[Fx.OddItem] = 7;
        cargo.Release();

        Assert.Equal(TransactionStatus.Applied, (await holder).Status);
        var result = await queued;
        Assert.Equal(TransactionStatus.Applied, result.Status);
        Assert.Equal(new[] { new KeyValuePair<string, int>(Fx.OreA, 2) }, result.ItemDeltas.ToArray());
        var snapshot = proc.GetSnapshot(Fx.ProfileId)!;
        Assert.Equal(3, snapshot.Inventory[Fx.OreA]);
        Assert.False(snapshot.Inventory.ContainsKey(Fx.OddItem));

        // The fingerprint was taken from the submitted items, so the original payload replays.
        var replay = await proc.GrantRewardAsync(ctx, Request("fixture-tx-queued", new() { [Fx.OreA] = 2 }));
        Assert.True(replay.IsReplay);
    }

    [Fact]
    public void Processor_ZeroIdempotencyWindow_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TransactionProcessor(new InMemoryProfileRepository(),
            new FixturePriceSource(), new FixtureCargoPolicy(), new PersistenceOptions { IdempotencyWindow = 0 }));
    }

    // ---- Boundaries and cross-processor stress ----------------------------------------------------------------------

    [Fact]
    public async Task WalletAtLongMaxValue_CreditsOverflowAndFullDebitWorks()
    {
        var env = await Env.CreateAsync(wallet: long.MaxValue);
        Assert.Equal(TransactionStatus.Applied, (await env.Reward(null, 0, (Fx.OreA, 1))).Status);

        Assert.Equal(RejectionReason.ArithmeticOverflow, (await env.Sell(null, Fx.OreA, 1)).Reason);
        Assert.Equal(RejectionReason.ArithmeticOverflow, (await env.Reward(null, 1)).Reason);
        env.Prices.Buy[(Fx.Station, Fx.OreA)] = long.MaxValue; // NEW provisional test fixture
        Assert.Equal(TransactionStatus.Applied, (await env.Buy(null, Fx.OreA, 1)).Status);
        Assert.Equal(0, env.Snapshot.Wallet);
        Assert.Equal(2, env.Snapshot.Inventory[Fx.OreA]);
    }

    [Fact]
    public async Task TwoProcessorsOnOneFileRepository_RetryingClients_ApplyEachPurchaseExactlyOnce()
    {
        using var dir = new TempDirectory();
        using var repo = Open(dir);
        var a = await Env.CreateAsync(wallet: 300, repo: repo, options: Options); // NEW fixture: 30 purchases at price 10
        var b = await Env.CreateAsync(repo: repo, options: Options, createProfile: false);
        var applied = 0;

        await Task.WhenAll(Enumerable.Range(0, 200).Select(i => Task.Run(async () =>
        {
            var env = i % 2 == 0 ? a : b;
            for (var attempt = 0; attempt < 1000; attempt++)
            {
                var r = await env.Buy($"fixture-tx-{i}", Fx.OreA, 1);
                if (r.Status == TransactionStatus.Applied) { if (!r.IsReplay) Interlocked.Increment(ref applied); return; }
                if (r.Reason == RejectionReason.InsufficientFunds) return;
                Assert.Contains(r.Reason, new[] { RejectionReason.ProfileNotOpen, RejectionReason.PersistenceConflict });
                await env.Proc.OpenProfileAsync(Fx.ProfileId);
            }
            Assert.Fail("A client never reached a final outcome.");
        })));

        var stored = (await repo.LoadAsync(Fx.ProfileId)).Profile!;
        Assert.Equal(30, applied);
        Assert.Equal(0, stored.Wallet);
        Assert.Equal(30, stored.Inventory[Fx.OreA]);
        Assert.Equal(30, stored.Revision);
    }

    private sealed class RedirectingRepository(IProfileRepository inner, string servedId) : IProfileRepository
    {
        public Task<ProfileLoadResult> LoadAsync(string profileId, CancellationToken ct = default) => inner.LoadAsync(servedId, ct);
        public Task<ProfileSaveResult> CreateAsync(PlayerProfile initial, CancellationToken ct = default) => inner.CreateAsync(initial, ct);
        public Task<ProfileSaveResult> SaveAsync(PlayerProfile next, long expectedRevision, CancellationToken ct = default) => inner.SaveAsync(next, expectedRevision, ct);
    }

    /// <summary>Fixture cargo policy that can block once inside <see cref="Fits"/> (no limit; NEW test fixture).</summary>
    private sealed class GatedCargoPolicy : ICargoPolicy
    {
        private readonly ManualResetEventSlim _gate = new(true);
        private int _blockNext;

        public ManualResetEventSlim Entered { get; } = new(false);

        public void BlockNext()
        {
            _gate.Reset();
            Volatile.Write(ref _blockNext, 1);
        }

        public void Release() => _gate.Set();

        public bool Fits(PlayerProfile current, IReadOnlyDictionary<string, int> proposedInventory)
        {
            if (Interlocked.Exchange(ref _blockNext, 0) == 1)
            {
                Entered.Set();
                _gate.Wait(TimeSpan.FromSeconds(10));
            }
            return true;
        }
    }
}
