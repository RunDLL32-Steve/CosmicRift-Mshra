using System.Numerics;
using System.Security.Cryptography;
using RogueTrader.GameData;
using RogueTrader.Persistence;
using RogueTrader.Simulation;
using RogueTrader.Trading;
using Xunit.Abstractions;

namespace RogueTrader.IntegrationTests;

/// <summary>
/// Queue M9 acceptance: drives the implemented loop (destroy a target, collect cargo, trade it at the Trading Outpost, buy an
/// upgrade, repair, save the ship) through the same services the Godot client composes, then exits by disposing every
/// in-memory service and relaunches from the save folder alone. This is engineering persistence acceptance, not proof of
/// original historical persistence parity: which fields the original kept across sessions is unrecoverable (RT-F068), so
/// retaining wallet, cargo, upgrades, hull and position is the NEW/provisional policy under test.
/// </summary>
public sealed class PersistedProgressionTests(ITestOutputHelper output)
{
    private const string ProfileId = "local-pilot"; // the client's profile id

    [Fact]
    public async Task FullLoop_ExitAndRelaunch_RestoresExactProgression()
    {
        using var folder = new SaveFolder();
        var content = Content.Load();
        output.WriteLine($"Content revision: {content.Revision}");

        PlayerProfile beforeExit;
        Vector2 savedPosition;
        float savedHull;
        long reward;
        string sector;
        TargetKill kill;
        CargoPickup pickup;
        using (var run = await Launch.OpenAsync(folder.Path, content))
        {
            Assert.True(run.Outcome.Created, run.Outcome.Message);
            var start = run.Profile.Wallet;

            // 1. Destroy the practice target ahead of the spawn and collect its reward.
            kill = run.FireUntilKill();
            sector = run.SectorInstanceId;
            reward = run.Session.KillReward.Credits;
            var paid = await run.Session.GrantKillRewardAsync(sector, kill);
            Assert.Equal(TransactionStatus.Applied, paid!.Status);
            Assert.Equal(start + reward, run.Profile.Wallet);

            // 2. Fly to the Wreckage pile, collect it, brake, and commit the pickup.
            var pile = content.Station.Collectibles.Single(p => p.ItemId == "2008");
            run.FlyUntil(new ShipInput(1, 0, false), w => w.Cargo.Quantity(pile.ItemId) == pile.Quantity, 600);
            run.FlyUntil(new ShipInput(-1, 0, false), w => w.Player.Velocity.Y >= 0, 600);
            pickup = Assert.Single(run.World.Pickups);
            Assert.Equal(TransactionStatus.Applied, (await run.Session.GrantPickupAsync(sector, pickup)).Status);
            Assert.Equal(pile.Quantity, run.Profile.Inventory[pile.ItemId]);

            // 3. Trade it at the outpost (the ship is still inside the dock radius).
            Assert.True(run.Session.IsDocked(run.World.Player.Position.X, run.World.Player.Position.Y), $"Ship at {run.World.Player.Position} must be docked.");
            var trade = content.Station.Trades.Single(t => t.InputItemId == pile.ItemId);
            Assert.Equal(TransactionStatus.Applied, (await run.Session.SellAsync(pile.ItemId, pile.Quantity, run.X, run.Y)).Status);
            run.ReconcileHold();
            Assert.Equal(start + reward + pile.Quantity * trade.CreditsPerUnit, run.Profile.Wallet);

            // 4. Buy the cheapest affordable speed upgrade and apply it to the flying ship.
            var upgrade = content.Station.Upgrades.Where(u => u.Effect.MaximumSpeed > 0 && u.Price <= run.Profile.Wallet
                && run.Session.UpgradeRefusal(u.Id) == RejectionReason.None).OrderBy(u => u.Price).First();
            Assert.Equal(TransactionStatus.Applied, (await run.Session.BuyUpgradeAsync(upgrade.Id, run.X, run.Y)).Status);
            run.ApplyUpgrades();
            Assert.True(run.World.Player.Definition.Physics.MaximumSpeed > content.Catalog.Ship.Physics.MaximumSpeed);

            // 5. Repair. Nothing in the sector damages the ship yet, so the damage is a NEW test fixture.
            var maximumHull = run.World.Player.Definition.Defense.MaximumHull;
            run.World.RestorePlayer(run.World.Player.Position, run.World.Player.Heading, maximumHull - 20);
            var repair = content.Station.Repairs[0];
            Assert.Equal(TransactionStatus.Applied, (await run.Session.RepairAsync(repair.Id, run.X, run.Y, run.World.Player.Hull, maximumHull)).Status);
            run.World.RepairPlayerHull(run.Session.RepairHullPoints(repair.Id));

            // 6. Save the ship (the client does this on pause, dock, undock and quit).
            Assert.True((await run.SaveShipAsync()).Saved);
            beforeExit = run.Profile;
            savedPosition = run.World.Player.Position;
            savedHull = run.World.Player.Hull;
            Assert.Equal(start + reward + pile.Quantity * trade.CreditsPerUnit - upgrade.Price - repair.Price, beforeExit.Wallet);
            output.WriteLine($"Pre-exit: {Describe(beforeExit)}");
        } // exit: repository lock released, session, processor and world dropped

        using (var relaunch = await Launch.OpenAsync(folder.Path, content))
        {
            Assert.Equal(ProfileLoadStatus.Loaded, relaunch.Outcome.Status);
            var after = relaunch.Profile;
            output.WriteLine($"Post-relaunch: {Describe(after)}");
            AssertSameProgression(beforeExit, after);
            Assert.Equal(PlayerProfile.CurrentSchemaVersion, after.SchemaVersion);
            Assert.Equal(savedPosition, relaunch.World.Player.Position);
            Assert.Equal(savedHull, relaunch.World.Player.Hull);
            Assert.Equal(Vector2.Zero, relaunch.World.Player.Velocity); // velocity is not saved (schema 3)
            Assert.True(relaunch.World.Player.Definition.Physics.MaximumSpeed > content.Catalog.Ship.Physics.MaximumSpeed, "The upgrade effect must survive relaunch.");
            Assert.Equal(after.Inventory.Where(e => e.Value > 0), relaunch.World.Cargo.Items.OrderBy(e => e.Key, StringComparer.Ordinal));

            // Duplicates after relaunch: the persisted idempotency window recognises the old kill, and committed cargo is not re-granted.
            var replay = await relaunch.Session.GrantKillRewardAsync(sector, kill);
            Assert.True(replay!.IsReplay);
            Assert.True((await relaunch.Session.GrantPickupAsync(sector, pickup)).IsReplay);
            AssertSameProgression(beforeExit, relaunch.Profile);
        }
    }

    [Fact]
    public async Task KillSavedButAcknowledgementLost_RelaunchRetryPaysOnce()
    {
        using var folder = new SaveFolder();
        var content = Content.Load();
        string sector;
        TargetKill kill;
        long start;
        using (var run = await Launch.OpenAsync(folder.Path, content, fault: FaultMode.ThrowAfterWrite))
        {
            start = run.Profile.Wallet;
            kill = run.FireUntilKill();
            sector = run.SectorInstanceId;
            var lost = await run.Session.GrantKillRewardAsync(sector, kill);
            Assert.Equal(RejectionReason.PersistenceFailed, lost!.Reason); // written to disk, then the process "crashed"
            Assert.Equal(start, run.Session.Profile!.Wallet);
        }

        using var relaunch = await Launch.OpenAsync(folder.Path, content);
        Assert.Equal(start + content.Reward.Credits, relaunch.Profile.Wallet);
        var retried = await relaunch.Session.GrantKillRewardAsync(sector, kill);
        Assert.True(retried!.IsReplay);
        Assert.Equal(start + content.Reward.Credits, relaunch.Profile.Wallet);
    }

    [Fact]
    public async Task SaveRefusedBeforeWrite_ChangesNothing_AndTheRetryAppliesOnce()
    {
        using var folder = new SaveFolder();
        var content = Content.Load();
        using var run = await Launch.OpenAsync(folder.Path, content, fault: FaultMode.FailBeforeWrite);
        var before = run.Profile;
        var kill = run.FireUntilKill();

        Assert.Equal(RejectionReason.PersistenceFailed, (await run.Session.GrantKillRewardAsync(run.SectorInstanceId, kill))!.Reason);
        Assert.Same(before, run.Session.Profile);
        run.Fault.Mode = FaultMode.None;
        Assert.True((await run.Session.ReloadAsync()).Succeeded); // the client reloads after a persistence failure
        AssertSameProgression(before, run.Profile);
        Assert.False((await run.Session.GrantKillRewardAsync(run.SectorInstanceId, kill))!.IsReplay);
        Assert.Equal(before.Wallet + content.Reward.Credits, run.Profile.Wallet);
        Assert.True((await run.Session.GrantKillRewardAsync(run.SectorInstanceId, kill))!.IsReplay);
        Assert.Equal(before.Revision + 1, run.Profile.Revision);
    }

    [Fact]
    public async Task CrashBetweenTempWriteAndReplace_RelaunchKeepsTheLastAcknowledgedRevision()
    {
        using var folder = new SaveFolder();
        var content = Content.Load();
        PlayerProfile acknowledged;
        using (var run = await Launch.OpenAsync(folder.Path, content))
        {
            await run.Session.GrantKillRewardAsync(run.SectorInstanceId, run.FireUntilKill());
            acknowledged = run.Profile;
        }
        // The process died after writing the next revision's temp file but before replacing the primary: never acknowledged.
        var unacknowledged = acknowledged with { Revision = acknowledged.Revision + 1, Wallet = acknowledged.Wallet + 9999 };
        await File.WriteAllBytesAsync(Path.Combine(folder.Path, ProfileId + ".profile.json.tmp"), ProfileSerializer.Serialize(unacknowledged));

        using var relaunch = await Launch.OpenAsync(folder.Path, content);
        Assert.Equal(ProfileLoadStatus.Loaded, relaunch.Outcome.Status);
        AssertSameProgression(acknowledged, relaunch.Profile);
        Assert.False(File.Exists(Path.Combine(folder.Path, ProfileId + ".profile.json.tmp")));
    }

    [Fact]
    public async Task CorruptPrimary_RelaunchRecoversThePreviousGeneration()
    {
        using var folder = new SaveFolder();
        var content = Content.Load();
        PlayerProfile previous;
        using (var run = await Launch.OpenAsync(folder.Path, content))
        {
            await run.Session.GrantKillRewardAsync(run.SectorInstanceId, run.FireUntilKill());
            previous = run.Profile;
            run.FlyUntil(new ShipInput(1, 0, false), w => w.TickNumber % 30 == 0, 60);
            Assert.True((await run.SaveShipAsync()).Saved);
        }
        var primary = Path.Combine(folder.Path, ProfileId + ".profile.json");
        var bytes = await File.ReadAllBytesAsync(primary);
        await File.WriteAllBytesAsync(primary, bytes[..(bytes.Length / 2)]); // torn write

        using var relaunch = await Launch.OpenAsync(folder.Path, content);
        Assert.Equal(ProfileLoadStatus.RecoveredFromBackup, relaunch.Outcome.Status);
        Assert.Contains("recovered revision", relaunch.Outcome.Message);
        AssertSameProgression(previous, relaunch.Profile);
    }

    [Fact]
    public async Task BothGenerationsCorrupt_ServicesStayOffline_AndNoEmptyProfileReplacesThem()
    {
        using var folder = new SaveFolder();
        var content = Content.Load();
        using (var run = await Launch.OpenAsync(folder.Path, content))
        {
            await run.Session.GrantKillRewardAsync(run.SectorInstanceId, run.FireUntilKill());
            Assert.True((await run.SaveShipAsync()).Saved);
        }
        foreach (var name in new[] { ".profile.json", ".profile.bak.json" })
            await File.WriteAllTextAsync(Path.Combine(folder.Path, ProfileId + name), "{ not a profile");

        for (var launch = 0; launch < 2; launch++)
        {
            using var relaunch = await Launch.OpenAsync(folder.Path, content, requireSuccess: false);
            Assert.False(relaunch.Outcome.Succeeded);
            Assert.False(relaunch.Outcome.Created);
            Assert.Null(relaunch.Session.Profile);
        }
    }

    private static void AssertSameProgression(PlayerProfile expected, PlayerProfile actual)
    {
        Assert.Equal(expected.ProfileId, actual.ProfileId);
        Assert.Equal(expected.Revision, actual.Revision);
        Assert.Equal(expected.Wallet, actual.Wallet);
        Assert.Equal(expected.Inventory.ToArray(), actual.Inventory.ToArray());
        Assert.Equal(expected.Upgrades.ToArray(), actual.Upgrades.ToArray());
        Assert.Equal(expected.Ship, actual.Ship);
        Assert.Equal(expected.ShipHullId, actual.ShipHullId);
        Assert.Equal(expected.Ledger.ToArray(), actual.Ledger.ToArray());
        Assert.Equal(expected.AppliedTransactions.Select(t => t.TransactionId), actual.AppliedTransactions.Select(t => t.TransactionId));
    }

    private static string Describe(PlayerProfile p) =>
        $"revision {p.Revision}, wallet {p.Wallet}, inventory [{string.Join(", ", p.Inventory.Select(e => $"{e.Key}x{e.Value}"))}], "
        + $"upgrades [{string.Join(", ", p.Upgrades)}], ship {(p.Ship is { } s ? $"hull {s.Hull} at ({s.X:0.##}, {s.Y:0.##})" : "none")}, "
        + $"{p.AppliedTransactions.Count} applied transactions";

    /// <summary>The shipped definitions the client loads, with a content revision: SHA-256 over each file, in load order.</summary>
    private sealed record Content(GameCatalog Catalog, StationDefinition Station, KillRewardDefinition Reward, string Revision)
    {
        private static readonly string[] Files =
            ["ships/starter_interceptor.json", "weapons/plasma_cannon.json", "sectors/first_contact.json", "stations/trading_outpost.json", "rewards/kill_reward.json"];

        public static Content Load()
        {
            var text = Files.ToDictionary(f => f, f => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "data", f)));
            var revision = string.Join("; ", Files.Select(f => $"{f} {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "data", f))))[..12].ToLowerInvariant()}"));
            return new Content(
                GameCatalog.FromJson(text[Files[0]], text[Files[1]], text[Files[2]]),
                StationDefinition.FromJson(text[Files[3]]),
                KillRewardDefinition.FromJson(text[Files[4]]),
                revision);
        }
    }

    /// <summary>One client launch, composed as GameClient composes it (save folder, session, world restored from the profile).</summary>
    private sealed class Launch : IDisposable
    {
        private readonly FileProfileRepository _repository;
        private readonly Content _content;

        private Launch(FileProfileRepository repository, FaultInjectingRepository fault, StationProfileSession session, ProfileOpenOutcome outcome, Content content)
        {
            _repository = repository;
            Fault = fault;
            Session = session;
            Outcome = outcome;
            _content = content;
            World = NewWorld();
        }

        public FaultInjectingRepository Fault { get; }
        public StationProfileSession Session { get; }
        public ProfileOpenOutcome Outcome { get; }
        public WorldSimulation World { get; }
        public string SectorInstanceId { get; } = Guid.NewGuid().ToString("N");
        public PlayerProfile Profile => Session.Profile ?? throw new InvalidOperationException($"No profile: {Outcome.Message}");
        public float X => World.Player.Position.X;
        public float Y => World.Player.Position.Y;

        public static async Task<Launch> OpenAsync(string folder, Content content, FaultMode fault = FaultMode.None, bool requireSuccess = true)
        {
            var repository = FileProfileRepository.Open(folder, new PersistenceOptions(), ProfileSchemaMigrations.BuiltIn);
            var faulty = new FaultInjectingRepository(repository) { Mode = fault };
            var session = new StationProfileSession(faulty, content.Station, content.Catalog.Ship.Id) { KillReward = content.Reward };
            var outcome = await session.OpenOrCreateAsync(ProfileId);
            if (requireSuccess) Assert.True(outcome.Succeeded, outcome.Message);
            return new Launch(repository, faulty, session, outcome, content);
        }

        private GameCatalog EffectiveCatalog() => new(
            UpgradeEffects.Apply(_content.Catalog.Ship, _content.Station, Session.Profile?.Upgrades ?? Enumerable.Empty<string>()),
            _content.Catalog.Weapon, _content.Catalog.Sector);

        private WorldSimulation NewWorld()
        {
            var hold = (Session.Profile?.Inventory ?? Enumerable.Empty<KeyValuePair<string, int>>()).Where(e => e.Value > 0);
            var world = new WorldSimulation(EffectiveCatalog(), _content.Station.CreateCargoCatalog(), hold);
            if (Session.Profile?.Ship is { } ship) world.RestorePlayer(new Vector2(ship.X, ship.Y), ship.Heading, ship.Hull);
            return world;
        }

        public void ApplyUpgrades() => World.ApplyPlayerDefinition(EffectiveCatalog().Ship);

        public void ReconcileHold() => World.Cargo.ReplaceContents(Profile.Inventory.Where(e => e.Value > 0).ToArray());

        public TargetKill FireUntilKill()
        {
            FlyUntil(new ShipInput(0, 0, true), w => w.Kills.Count > 0, WorldSimulation.TickRate * 20);
            return World.Kills[^1];
        }

        public void FlyUntil(ShipInput input, Func<WorldSimulation, bool> done, int maximumTicks)
        {
            for (var tick = 0; tick < maximumTicks; tick++)
            {
                World.Tick(input);
                if (done(World)) return;
            }
            throw new Xunit.Sdk.XunitException($"Condition not reached within {maximumTicks} ticks (ship at {World.Player.Position}).");
        }

        public Task<ShipConditionResult> SaveShipAsync() =>
            Session.SaveShipAsync(World.Player.Hull, World.Player.Position.X, World.Player.Position.Y, World.Player.Heading);

        public void Dispose() => _repository.Dispose();
    }

    private enum FaultMode { None, FailBeforeWrite, ThrowAfterWrite }

    /// <summary>Injects save failures: refused before the write (nothing stored) or thrown after it (stored, acknowledgement lost).</summary>
    private sealed class FaultInjectingRepository(IProfileRepository inner) : IProfileRepository
    {
        public FaultMode Mode { get; set; }

        public Task<ProfileLoadResult> LoadAsync(string profileId, CancellationToken ct = default) => inner.LoadAsync(profileId, ct);

        public Task<ProfileSaveResult> CreateAsync(PlayerProfile initial, CancellationToken ct = default) => inner.CreateAsync(initial, ct);

        public async Task<ProfileSaveResult> SaveAsync(PlayerProfile next, long expectedRevision, CancellationToken ct = default)
        {
            switch (Mode)
            {
                case FaultMode.FailBeforeWrite:
                    return new ProfileSaveResult(ProfileSaveStatus.IoError, expectedRevision,
                        [new PersistenceDiagnostic(DiagnosticCode.IoFailure, "injected failure before write", null)]);
                case FaultMode.ThrowAfterWrite:
                    await inner.SaveAsync(next, expectedRevision, ct);
                    throw new IOException("injected failure after write");
                default:
                    return await inner.SaveAsync(next, expectedRevision, ct);
            }
        }
    }

    private sealed class SaveFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rt-integration-tests", Guid.NewGuid().ToString("N"));

        public SaveFolder() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
