using System.Collections.Immutable;
using RogueTrader.Persistence;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>Synthetic ids. Every number in these fixtures is NEW provisional test data, not game balance.</summary>
internal static class Fx
{
    public const string ProfileId = "fixture-profile-1";
    public const string Station = "fixture-station-1";
    public const string OtherStation = "fixture-station-2";
    public const string OreA = "fixture-ore-a";
    public const string OddItem = "fixture item/Ω #2";
    public const string UpgradeX = "fixture-upgrade-x";
    public const string UpgradeY = "fixture-upgrade-y";
    public const string Hull = "fixture-hull-1";

    public static string NewId() => Guid.NewGuid().ToString("N");
}

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rt-persistence-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset start) : TimeProvider
{
    private long _ticks = start.UtcTicks;

    public void Advance(TimeSpan by) => Interlocked.Add(ref _ticks, by.Ticks);

    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);
}

/// <summary>Dictionary-backed authoritative prices. Every price below is a NEW provisional test fixture, not game balance.</summary>
internal sealed class FixturePriceSource : IPriceSource
{
    public Dictionary<(string Station, string Item), long> Buy { get; } = new()
    {
        [(Fx.Station, Fx.OreA)] = 10,      // NEW provisional test fixture, not game balance
        [(Fx.Station, Fx.OddItem)] = 25,   // NEW provisional test fixture, not game balance
    };

    public Dictionary<(string Station, string Item), long> Sell { get; } = new()
    {
        [(Fx.Station, Fx.OreA)] = 7,       // NEW provisional test fixture, not game balance
        [(Fx.Station, Fx.OddItem)] = 20,   // NEW provisional test fixture, not game balance
    };

    public Dictionary<(string Station, string Upgrade), long> Upgrade { get; } = new()
    {
        [(Fx.Station, Fx.UpgradeX)] = 500, // NEW provisional test fixture, not game balance
        [(Fx.Station, Fx.UpgradeY)] = 300, // NEW provisional test fixture, not game balance
    };

    public bool TryGetBuyUnitPrice(string stationId, string itemId, out long unitPrice) => Buy.TryGetValue((stationId, itemId), out unitPrice);
    public bool TryGetSellUnitPrice(string stationId, string itemId, out long unitPrice) => Sell.TryGetValue((stationId, itemId), out unitPrice);
    public bool TryGetUpgradePrice(string stationId, string upgradeId, out long price) => Upgrade.TryGetValue((stationId, upgradeId), out price);
}

/// <summary>Sum of units must not exceed <see cref="Limit"/> (NEW provisional test fixture limit, not game balance).</summary>
internal sealed class FixtureCargoPolicy : ICargoPolicy
{
    public long Limit { get; set; } = 1_000_000; // NEW provisional test fixture, not game balance

    public bool Fits(PlayerProfile current, IReadOnlyDictionary<string, int> proposedInventory)
    {
        long sum = 0;
        foreach (var qty in proposedInventory.Values) sum += qty;
        return sum <= Limit;
    }
}

internal enum FaultMode { None, FailBeforeWrite, ThrowAfterWrite }

/// <summary>Wraps a repository and injects save failures: before the write (nothing stored) or after it (stored, then throws).</summary>
internal sealed class FaultInjectingProfileRepository(IProfileRepository inner) : IProfileRepository
{
    public FaultMode Mode { get; set; } = FaultMode.None;

    public Task<ProfileLoadResult> LoadAsync(string profileId, CancellationToken ct = default) => inner.LoadAsync(profileId, ct);

    public Task<ProfileSaveResult> CreateAsync(PlayerProfile initial, CancellationToken ct = default) => inner.CreateAsync(initial, ct);

    public async Task<ProfileSaveResult> SaveAsync(PlayerProfile next, long expectedRevision, CancellationToken ct = default)
    {
        switch (Mode)
        {
            case FaultMode.FailBeforeWrite:
                return new ProfileSaveResult(ProfileSaveStatus.IoError, expectedRevision,
                    new[] { new PersistenceDiagnostic(DiagnosticCode.IoFailure, "injected failure before write", null) });
            case FaultMode.ThrowAfterWrite:
                await inner.SaveAsync(next, expectedRevision, ct);
                throw new IOException("injected failure after write");
            default:
                return await inner.SaveAsync(next, expectedRevision, ct);
        }
    }
}

internal static class ProfileAssert
{
    /// <summary>Record equality compares immutable collections by reference, so compare member by member.</summary>
    public static void Equivalent(PlayerProfile expected, PlayerProfile actual)
    {
        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.ProfileId, actual.ProfileId);
        Assert.Equal(expected.Revision, actual.Revision);
        Assert.Equal(expected.Wallet, actual.Wallet);
        Assert.Equal(expected.ShipHullId, actual.ShipHullId);
        Assert.Equal(expected.NextLedgerSequence, actual.NextLedgerSequence);
        Assert.Equal(expected.Inventory.ToArray(), actual.Inventory.ToArray());
        Assert.Equal(expected.Upgrades.ToArray(), actual.Upgrades.ToArray());
        Assert.Equal(expected.Ledger.ToArray(), actual.Ledger.ToArray());
        Assert.Equal(expected.AppliedTransactions.Count, actual.AppliedTransactions.Count);
        for (var i = 0; i < expected.AppliedTransactions.Count; i++)
        {
            var e = expected.AppliedTransactions[i];
            var a = actual.AppliedTransactions[i];
            Assert.Equal(e.TransactionId, a.TransactionId);
            Assert.Equal(e.Kind, a.Kind);
            Assert.Equal(e.RequestFingerprint, a.RequestFingerprint);
            Assert.Equal(e.CurrencyDelta, a.CurrencyDelta);
            Assert.Equal(e.ItemDeltas.ToArray(), a.ItemDeltas.ToArray());
            Assert.Equal(e.UpgradeId, a.UpgradeId);
            Assert.Equal(e.WalletAfter, a.WalletAfter);
            Assert.Equal(e.Revision, a.Revision);
            Assert.Equal(e.AppliedAtUtc, a.AppliedAtUtc);
        }
    }
}

/// <summary>A processor wired to fixtures and an opened profile.</summary>
internal sealed class Env
{
    public required IProfileRepository Repo { get; init; }
    public required TransactionProcessor Proc { get; init; }
    public required FixturePriceSource Prices { get; init; }
    public required FixtureCargoPolicy Cargo { get; init; }
    public required PersistenceOptions Options { get; init; }
    public required FixedTimeProvider Time { get; init; }

    public TransactionContext Ctx(bool stationAccess = true) => new(Fx.ProfileId, stationAccess);
    public PlayerProfile Snapshot => Proc.GetSnapshot(Fx.ProfileId)!;

    /// <summary>Builds the fixtures. With <paramref name="createProfile"/> the profile is created in the repository first.</summary>
    public static async Task<Env> CreateAsync(long wallet = 1000, IProfileRepository? repo = null, PersistenceOptions? options = null,
        bool createProfile = true, FixturePriceSource? prices = null, FixtureCargoPolicy? cargo = null)
    {
        options ??= new PersistenceOptions();
        repo ??= new InMemoryProfileRepository(options);
        if (createProfile)
        {
            var created = await repo.CreateAsync(PlayerProfile.CreateNew(Fx.ProfileId, wallet, Fx.Hull));
            Assert.Equal(ProfileSaveStatus.Saved, created.Status);
        }
        prices ??= new FixturePriceSource();
        cargo ??= new FixtureCargoPolicy();
        var time = new FixedTimeProvider(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero)); // NEW fixture clock
        var proc = new TransactionProcessor(repo, prices, cargo, options, time);
        var opened = await proc.OpenProfileAsync(Fx.ProfileId);
        Assert.True(opened.Succeeded);
        return new Env { Repo = repo, Proc = proc, Prices = prices, Cargo = cargo, Options = options, Time = time };
    }

    public Task<TransactionResult> Buy(string? id, string item, int qty, bool stationAccess = true, string station = Fx.Station) =>
        Proc.BuyAsync(Ctx(stationAccess), new BuyRequest(id ?? Fx.NewId(), Fx.ProfileId, station, item, qty));

    public Task<TransactionResult> Sell(string? id, string item, int qty, bool stationAccess = true, string station = Fx.Station) =>
        Proc.SellAsync(Ctx(stationAccess), new SellRequest(id ?? Fx.NewId(), Fx.ProfileId, station, item, qty));

    public Task<TransactionResult> BuyUpgrade(string? id, string upgrade, bool stationAccess = true) =>
        Proc.BuyUpgradeAsync(Ctx(stationAccess), new BuyUpgradeRequest(id ?? Fx.NewId(), Fx.ProfileId, Fx.Station, upgrade));

    public Task<TransactionResult> Reward(string? id, long currency, params (string Item, int Qty)[] items) =>
        Proc.GrantRewardAsync(Ctx(false), new GrantRewardRequest(id ?? Fx.NewId(), Fx.ProfileId, "fixture-source-1", currency,
            items.ToDictionary(i => i.Item, i => i.Qty, StringComparer.Ordinal)));

    public static ImmutableSortedDictionary<string, int> Items(params (string Item, int Qty)[] items) =>
        ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, items.Select(i => new KeyValuePair<string, int>(i.Item, i.Qty)));
}
