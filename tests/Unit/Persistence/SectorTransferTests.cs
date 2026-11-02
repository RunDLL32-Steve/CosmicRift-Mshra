using System.Collections.Immutable;
using RogueTrader.Persistence.Transfers;

namespace RogueTrader.UnitTests.Persistence;

/// <summary>
/// Queue C10: prepare/commit/recover sector transfers. Sector ids, entries, positions and manifests are NEW synthetic fixtures,
/// not original sectors or game balance.
/// </summary>
public sealed class SectorTransferTests
{
    private const string Player = "fixture-pilot-1";
    private const string A = "fixture-sector-a";
    private const string B = "fixture-sector-b";
    private const string C = "fixture-sector-c";
    private const string GateB = "fixture-gate-b";

    private static readonly TransferManifest Cargo = new(new Dictionary<string, long>
    {
        ["ship:fixture-hull-1"] = 1,      // NEW provisional test fixture
        ["cargo:fixture-ore-a"] = 37,     // NEW provisional test fixture
        ["credits:carried"] = 1250,       // NEW provisional test fixture
    }, state: "hull=82.5;x=10;y=20");

    private static SectorTransferRequest Req(string id, string from = A, string to = B, string entry = GateB, string player = Player) =>
        new(id, player, from, to, entry);

    private static TransferContext Ctx(string player = Player) => new(player);

    // ---- Harness -------------------------------------------------------------------------------------------

    private sealed class World
    {
        public InMemoryTransferJournal Store { get; } = new();
        public FaultyJournal Journal { get; }
        public Dictionary<string, FaultySector> Sectors { get; } = new(StringComparer.Ordinal);
        public SectorTransferCoordinator Coordinator { get; private set; }

        public World(params string[] sectors)
        {
            Journal = new FaultyJournal(Store);
            foreach (var id in sectors.Length == 0 ? new[] { A, B } : sectors)
                Sectors[id] = new FaultySector(new InMemorySectorParticipant(id, new[]
                {
                    new SpawnPlacement("fixture-gate-" + id[^1], 100, 200),   // NEW provisional test fixture
                    new SpawnPlacement("fixture-dock-" + id[^1], -50, 75),    // NEW provisional test fixture
                }));
            Coordinator = Build();
        }

        public InMemorySectorParticipant Sector(string id) => Sectors[id].Inner;

        public SectorTransferCoordinator Build() => new(Journal, Sectors.Values);

        public async Task AdmitAsync(string player = Player, string sector = A, TransferManifest? manifest = null)
        {
            Assert.Equal(JournalWriteStatus.Saved, await Coordinator.RegisterAsync(player, sector));
            Sector(sector).Admit(player, manifest ?? Cargo, 1);
        }

        /// <summary>Simulates a process restart: participants rebuilt from their durable snapshots, a new coordinator, faults cleared.</summary>
        public void Reconstruct()
        {
            Journal.Clear();
            foreach (var id in Sectors.Keys.ToArray())
                Sectors[id] = new FaultySector(InMemorySectorParticipant.FromSnapshot(Sectors[id].Inner.Snapshot()));
            Coordinator = Build();
        }

        /// <summary>No loss, no duplicate: the player's ship is held somewhere and active in at most one sector.</summary>
        public void AssertNeverDuplicated(string player, TransferManifest manifest)
        {
            var held = Sectors.Values.Select(s => s.Inner.GetHolding(player)).Where(h => h is not null).ToArray();
            Assert.NotEmpty(held);
            Assert.True(held.Count(h => h!.State == HoldingState.Active) <= 1, "ship active in more than one sector");
            Assert.All(held, h => Assert.Equal(manifest, h!.Manifest));
        }

        /// <summary>Settled: no transfer in flight; the journal owner alone holds the ship, active at the journal epoch, unchanged.</summary>
        public async Task AssertSettledAsync(string owner, string player = Player, TransferManifest? manifest = null)
        {
            var p = await Store.LoadAsync(player);
            Assert.NotNull(p);
            Assert.Null(p!.Active);
            Assert.Equal(owner, p.OwnerSectorId);
            foreach (var (id, sector) in Sectors)
            {
                var h = sector.Inner.GetHolding(player);
                if (id != owner) { Assert.Null(h); continue; }
                Assert.NotNull(h);
                Assert.Equal(HoldingState.Active, h!.State);
                Assert.Equal(p.Epoch, h.Epoch);
                Assert.Equal(manifest ?? Cargo, h.Manifest);
            }
        }
    }

    private enum FaultMode { None, ThrowBeforeWrite, ThrowAfterWrite }

    private sealed class FaultyJournal(InMemoryTransferJournal inner) : ITransferJournal
    {
        private int _saves;
        public Func<int, FaultMode>? OnSave { get; set; }
        public int Saves => _saves;

        public void Clear()
        {
            OnSave = null;
            _saves = 0;
        }

        public Task<PlayerPresence?> LoadAsync(string playerId, CancellationToken ct = default) => inner.LoadAsync(playerId, ct);
        public Task<JournalWriteStatus> CreateAsync(PlayerPresence initial, CancellationToken ct = default) => inner.CreateAsync(initial, ct);
        public Task<IReadOnlyList<string>> ListInFlightAsync(CancellationToken ct = default) => inner.ListInFlightAsync(ct);

        public async Task<JournalWriteStatus> SaveAsync(PlayerPresence next, long expectedRevision, CancellationToken ct = default)
        {
            var n = Interlocked.Increment(ref _saves);
            var mode = OnSave?.Invoke(n) ?? FaultMode.None;
            if (mode == FaultMode.ThrowBeforeWrite) throw new IOException($"injected failure before save {n}");
            var status = await inner.SaveAsync(next, expectedRevision, ct);
            if (mode == FaultMode.ThrowAfterWrite) throw new IOException($"injected failure after save {n}");
            return status;
        }
    }

    private sealed class FaultySector(InMemorySectorParticipant inner) : ISectorParticipant
    {
        public InMemorySectorParticipant Inner { get; } = inner;
        public HashSet<string> FailOnce { get; } = new(StringComparer.Ordinal);
        /// <summary>Awaited before the named operation runs, to interleave a second coordinator.</summary>
        public Dictionary<string, Task> Gates { get; } = new(StringComparer.Ordinal);
        public List<string> Calls { get; } = new();

        public string SectorId => Inner.SectorId;

        private async Task Enter(string op)
        {
            lock (Calls) Calls.Add(op);
            if (Gates.Remove(op, out var gate)) await gate;
            if (FailOnce.Remove(op)) throw new InvalidOperationException($"injected {op} failure in {SectorId}");
        }

        public async Task<DetachOutcome> DetachAsync(string t, string p, long e, CancellationToken ct = default) { await Enter("detach"); return await Inner.DetachAsync(t, p, e, ct); }
        public async Task<ReserveOutcome> ReserveAsync(string t, string p, TransferManifest m, string entry, CancellationToken ct = default) { await Enter("reserve"); return await Inner.ReserveAsync(t, p, m, entry, ct); }
        public async Task ActivateAsync(string t, string p, long e, CancellationToken ct = default) { await Enter("activate"); await Inner.ActivateAsync(t, p, e, ct); }
        public async Task ReleaseAsync(string t, string p, CancellationToken ct = default) { await Enter("release"); await Inner.ReleaseAsync(t, p, ct); }
        public async Task RestoreAsync(string t, string p, CancellationToken ct = default) { await Enter("restore"); await Inner.RestoreAsync(t, p, ct); }
        public async Task CancelAsync(string t, string p, CancellationToken ct = default) { await Enter("cancel"); await Inner.CancelAsync(t, p, ct); }
    }

    // ---- Happy path and validation -------------------------------------------------------------------------

    [Fact]
    public async Task Transfer_moves_ownership_and_ship_to_a_validated_spawn()
    {
        var w = new World();
        await w.AdmitAsync();

        var r = await w.Coordinator.TransferAsync(Ctx(), Req("t-1"));

        Assert.Equal(SectorTransferStatus.Completed, r.Status);
        Assert.False(r.IsReplay);
        Assert.Equal(B, r.OwnerSectorId);
        Assert.Equal(2, r.Epoch);
        Assert.Equal(new SpawnPlacement(GateB, 100, 200), r.Spawn);
        Assert.Equal(4, w.Journal.Saves); // preparing, prepared, committed, completed
        await w.AssertSettledAsync(B);
        Assert.Equal(new SpawnPlacement(GateB, 100, 200), w.Sector(B).GetHolding(Player)!.Spawn);

        var back = await w.Coordinator.TransferAsync(Ctx(), Req("t-2", B, A, "fixture-dock-a"));
        Assert.Equal(SectorTransferStatus.Completed, back.Status);
        Assert.Equal(3, back.Epoch);
        await w.AssertSettledAsync(A);
    }

    [Fact]
    public async Task Requests_failing_server_validation_change_nothing()
    {
        var w = new World(A, B, C);
        await w.AdmitAsync();
        var routed = new SectorTransferCoordinator(w.Journal, w.Sectors.Values, routeAllowed: (from, to) => to != C);

        Assert.Equal(TransferRejection.IdentityMismatch, (await w.Coordinator.TransferAsync(Ctx("someone-else"), Req("t-1"))).Reason);
        Assert.Equal(TransferRejection.InvalidRequest, (await w.Coordinator.TransferAsync(Ctx(), Req(""))).Reason);
        Assert.Equal(TransferRejection.InvalidRequest, (await w.Coordinator.TransferAsync(Ctx(), Req(new string('x', 129)))).Reason);
        Assert.Equal(TransferRejection.InvalidRequest, (await w.Coordinator.TransferAsync(Ctx(), Req("t-1", entry: ""))).Reason);
        Assert.Equal(TransferRejection.UnknownSector, (await w.Coordinator.TransferAsync(Ctx(), Req("t-1", to: "fixture-sector-z"))).Reason);
        Assert.Equal(TransferRejection.SameSector, (await w.Coordinator.TransferAsync(Ctx(), Req("t-1", to: A))).Reason);
        Assert.Equal(TransferRejection.NotInSourceSector, (await w.Coordinator.TransferAsync(Ctx(), Req("t-1", from: B, to: C, entry: "fixture-gate-c"))).Reason);
        Assert.Equal(TransferRejection.RouteNotAllowed, (await routed.TransferAsync(Ctx(), Req("t-1", to: C, entry: "fixture-gate-c"))).Reason);
        Assert.Equal(TransferRejection.NotRegistered, (await w.Coordinator.TransferAsync(Ctx("fixture-pilot-2"), Req("t-1", player: "fixture-pilot-2"))).Reason);

        Assert.Equal(0, w.Journal.Saves);
        Assert.Empty(w.Sectors[A].Calls);
        await w.AssertSettledAsync(A);
        Assert.Equal(1, (await w.Store.LoadAsync(Player))!.Epoch);
    }

    [Fact]
    public async Task Destination_refusing_an_invalid_spawn_aborts_and_restores_the_source_ship()
    {
        var w = new World();
        await w.AdmitAsync();

        var r = await w.Coordinator.TransferAsync(Ctx(), Req("t-1", entry: "fixture-gate-missing"));

        Assert.Equal(SectorTransferStatus.Aborted, r.Status);
        Assert.Equal(TransferRejection.DestinationRefused, r.Reason);
        Assert.Equal(A, r.OwnerSectorId);
        await w.AssertSettledAsync(A);
        // The aborted id is fenced on both sides: a stale coordinator cannot detach or reserve under it later.
        Assert.False((await w.Sector(A).DetachAsync("t-1", Player, 1)).Ok);
        Assert.False((await w.Sector(B).ReserveAsync("t-1", Player, Cargo, GateB)).Ok);
        // The ship can still leave under a new id.
        Assert.Equal(SectorTransferStatus.Completed, (await w.Coordinator.TransferAsync(Ctx(), Req("t-2"))).Status);
        await w.AssertSettledAsync(B);
    }

    [Fact]
    public async Task Source_refusing_to_detach_aborts_without_touching_the_destination()
    {
        var w = new World();
        await w.Coordinator.RegisterAsync(Player, A); // journal says A, but the sector never admitted the ship

        var r = await w.Coordinator.TransferAsync(Ctx(), Req("t-1"));

        Assert.Equal(SectorTransferStatus.Aborted, r.Status);
        Assert.Equal(TransferRejection.SourceRefused, r.Reason);
        Assert.DoesNotContain("reserve", w.Sectors[B].Calls);
        Assert.Null(w.Sector(B).GetHolding(Player));
        Assert.Null((await w.Store.LoadAsync(Player))!.Active);
    }

    // ---- Duplicates and concurrency ------------------------------------------------------------------------

    [Fact]
    public async Task Duplicate_commit_retry_replays_the_original_outcome()
    {
        var w = new World();
        await w.AdmitAsync();
        var first = await w.Coordinator.TransferAsync(Ctx(), Req("t-1"));
        var saves = w.Journal.Saves;

        var again = await w.Coordinator.TransferAsync(Ctx(), Req("t-1"));
        var restarted = new SectorTransferCoordinator(w.Journal, w.Sectors.Values);
        var afterRestart = await restarted.TransferAsync(Ctx(), Req("t-1"));

        Assert.True(again.IsReplay);
        Assert.True(afterRestart.IsReplay);
        Assert.Equal(first with { IsReplay = true }, again);
        Assert.Equal(first with { IsReplay = true }, afterRestart);
        Assert.Equal(saves, w.Journal.Saves);
        await w.AssertSettledAsync(B);

        var reused = await w.Coordinator.TransferAsync(Ctx(), Req("t-1", B, A, "fixture-gate-a"));
        Assert.Equal(TransferRejection.TransferIdReused, reused.Reason);
        await w.AssertSettledAsync(B);
    }

    [Fact]
    public async Task Parallel_retries_of_one_transfer_apply_it_once()
    {
        var w = new World();
        await w.AdmitAsync();

        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => w.Coordinator.TransferAsync(Ctx(), Req("t-1")))));

        Assert.All(results, r => Assert.Equal(SectorTransferStatus.Completed, r.Status));
        Assert.Single(results, r => !r.IsReplay);
        Assert.Single(w.Sectors[A].Calls, c => c == "detach");
        Assert.Single(w.Sectors[B].Calls, c => c == "activate");
        await w.AssertSettledAsync(B);
    }

    [Fact]
    public async Task Parallel_competing_transfers_move_the_ship_once()
    {
        var w = new World(A, B, C);
        await w.AdmitAsync();

        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => Task.Run(() =>
            w.Coordinator.TransferAsync(Ctx(), i % 2 == 0 ? Req("t-b-" + i) : Req("t-c-" + i, to: C, entry: "fixture-gate-c")))));

        var winner = Assert.Single(results, r => r.Status == SectorTransferStatus.Completed);
        Assert.All(results.Where(r => r != winner), r => Assert.Equal(TransferRejection.NotInSourceSector, r.Reason));
        await w.AssertSettledAsync(winner.OwnerSectorId!);
    }

    [Fact]
    public async Task Two_coordinators_on_one_journal_cannot_both_move_the_ship()
    {
        var w = new World(A, B, C);
        await w.AdmitAsync();
        var other = new SectorTransferCoordinator(w.Journal, w.Sectors.Values);

        var results = await Task.WhenAll(
            Task.Run(() => w.Coordinator.TransferAsync(Ctx(), Req("t-b"))),
            Task.Run(() => other.TransferAsync(Ctx(), Req("t-c", to: C, entry: "fixture-gate-c"))));
        await w.Build().RecoverAllAsync();

        Assert.True(results.Count(r => r.Status == SectorTransferStatus.Completed) <= 1);
        var owner = (await w.Store.LoadAsync(Player))!.OwnerSectorId;
        await w.AssertSettledAsync(owner);
    }

    // ---- Failure before/after prepare and commit, then recovery after reconstruction ------------------------

    public static TheoryData<string, string, string> CrashPoints => new()
    {
        // (fault, owner after recovery, owner after retrying the same request)
        { "save1-before", A, B },  // nothing journalled
        { "save1-after", A, B },   // preparing journalled, nothing detached
        { "detach", A, A },        // aborted in line
        { "reserve", A, A },       // aborted in line after the source froze the ship
        { "save2-before", A, B },  // detached and reserved, prepared not journalled
        { "save2-after", A, B },   // prepared journalled
        { "save3-before", A, B },  // commit write lost
        { "save3-after", B, B },   // committed: recovery rolls forward
        { "activate", B, B },
        { "release", B, B },
        { "save4-before", B, B },
        { "save4-after", B, B },
    };

    private static void Inject(World w, string fault)
    {
        if (fault.StartsWith("save", StringComparison.Ordinal))
        {
            var n = fault[4] - '0';
            var mode = fault.EndsWith("before", StringComparison.Ordinal) ? FaultMode.ThrowBeforeWrite : FaultMode.ThrowAfterWrite;
            w.Journal.OnSave = i => i == n ? mode : FaultMode.None;
        }
        else (fault is "detach" or "release" ? w.Sectors[A] : w.Sectors[B]).FailOnce.Add(fault);
    }

    [Theory]
    [MemberData(nameof(CrashPoints))]
    public async Task Recovery_after_reconstruction_settles_every_crash_point(string fault, string recoveredOwner, string _)
    {
        var w = new World();
        await w.AdmitAsync();
        Inject(w, fault);

        var r = await w.Coordinator.TransferAsync(Ctx(), Req("t-1"));
        Assert.NotEqual(SectorTransferStatus.Completed, r.Status);
        w.AssertNeverDuplicated(Player, Cargo);

        w.Reconstruct();
        await w.Coordinator.RecoverAllAsync();
        await w.AssertSettledAsync(recoveredOwner);

        // Recovery is idempotent, and a later retry of the same id agrees with the recovered decision.
        Assert.Empty(await w.Coordinator.RecoverAllAsync());
        // Only when nothing was journalled is the retry a fresh transfer; otherwise it replays the recovered outcome.
        var retry = await w.Coordinator.TransferAsync(Ctx(), Req("t-1"));
        var expected = fault == "save1-before" ? B : recoveredOwner;
        Assert.Equal(fault != "save1-before", retry.IsReplay);
        Assert.Equal(expected, retry.OwnerSectorId);
        await w.AssertSettledAsync(expected);
    }

    [Theory]
    [MemberData(nameof(CrashPoints))]
    public async Task Retrying_the_same_request_after_reconstruction_finishes_it(string fault, string _, string retriedOwner)
    {
        var w = new World();
        await w.AdmitAsync();
        Inject(w, fault);
        await w.Coordinator.TransferAsync(Ctx(), Req("t-1"));

        w.Reconstruct();
        var r = await w.Coordinator.TransferAsync(Ctx(), Req("t-1"));

        Assert.Equal(retriedOwner == B ? SectorTransferStatus.Completed : SectorTransferStatus.Aborted, r.Status);
        Assert.Equal(retriedOwner, r.OwnerSectorId);
        await w.AssertSettledAsync(retriedOwner);
    }

    [Fact]
    public async Task Failure_while_aborting_is_finished_by_recovery()
    {
        var w = new World();
        await w.AdmitAsync();
        w.Sectors[A].FailOnce.Add("restore");

        var r = await w.Coordinator.TransferAsync(Ctx(), Req("t-1", entry: "fixture-gate-missing"));
        Assert.Equal(SectorTransferStatus.Unresolved, r.Status);
        Assert.Equal(TransferPhase.Aborting, r.Phase);
        w.AssertNeverDuplicated(Player, Cargo);

        w.Reconstruct();
        var recovered = Assert.Single(await w.Coordinator.RecoverAllAsync());
        Assert.Equal(SectorTransferStatus.Aborted, recovered.Status);
        Assert.Equal(TransferRejection.DestinationRefused, recovered.Reason);
        await w.AssertSettledAsync(A);
    }

    [Fact]
    public async Task Another_transfer_waits_until_the_in_flight_one_is_recovered()
    {
        var w = new World(A, B, C);
        await w.AdmitAsync();
        w.Journal.OnSave = i => i == 3 ? FaultMode.ThrowAfterWrite : FaultMode.None; // committed, then crash
        await w.Coordinator.TransferAsync(Ctx(), Req("t-1"));
        w.Journal.OnSave = null;

        var blocked = await w.Coordinator.TransferAsync(Ctx(), Req("t-2", B, C, "fixture-gate-c"));
        Assert.Equal(TransferRejection.TransferInProgress, blocked.Reason);

        await w.Coordinator.RecoverAsync(Player);
        Assert.Equal(SectorTransferStatus.Completed, (await w.Coordinator.TransferAsync(Ctx(), Req("t-2", B, C, "fixture-gate-c"))).Status);
        await w.AssertSettledAsync(C);
    }

    // ---- Stale coordinators are fenced ---------------------------------------------------------------------

    [Fact]
    public async Task Stale_coordinator_cannot_commit_after_recovery_aborted_its_transfer()
    {
        var w = new World();
        await w.AdmitAsync();
        var release = new TaskCompletionSource();
        w.Sectors[B].Gates["reserve"] = release.Task;

        var stale = Task.Run(() => w.Coordinator.TransferAsync(Ctx(), Req("t-1")));
        while (!w.Sectors[B].Calls.Contains("reserve")) await Task.Delay(1);

        // A restarted coordinator recovers while the first one is still inside Reserve.
        var recovered = await w.Build().RecoverAsync(Player);
        Assert.Equal(TransferRejection.RecoveredBeforeCommit, recovered!.Reason);
        release.SetResult();
        var late = await stale;

        Assert.NotEqual(SectorTransferStatus.Completed, late.Status);
        await w.AssertSettledAsync(A);
    }

    [Fact]
    public async Task Stale_coordinator_after_the_commit_point_agrees_with_roll_forward()
    {
        var w = new World();
        await w.AdmitAsync();
        var release = new TaskCompletionSource();
        w.Sectors[B].Gates["activate"] = release.Task;

        var stale = Task.Run(() => w.Coordinator.TransferAsync(Ctx(), Req("t-1")));
        while (!w.Sectors[B].Calls.Contains("activate")) await Task.Delay(1);

        var recovered = await w.Build().RecoverAsync(Player);
        Assert.Equal(SectorTransferStatus.Completed, recovered!.Status);
        release.SetResult();
        var late = await stale;

        Assert.Equal(SectorTransferStatus.Unresolved, late.Status);
        Assert.Equal(TransferRejection.PersistenceConflict, late.Reason);
        await w.AssertSettledAsync(B);
    }

    [Fact]
    public async Task Participants_refuse_contradictory_decisions()
    {
        var sector = new InMemorySectorParticipant(A, new[] { new SpawnPlacement("fixture-gate-a", 0, 0) });
        sector.Admit(Player, Cargo, 1);
        await sector.DetachAsync("t-1", Player, 1);
        await sector.RestoreAsync("t-1", Player);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sector.ReleaseAsync("t-1", Player));
        Assert.False((await sector.DetachAsync("t-1", Player, 1)).Ok);
        Assert.False((await sector.DetachAsync("t-2", Player, 7)).Ok); // stale epoch
        Assert.Equal(HoldingState.Active, sector.GetHolding(Player)!.State);

        var dest = new InMemorySectorParticipant(B, new[] { new SpawnPlacement(GateB, 1, 2) });
        Assert.True((await dest.ReserveAsync("t-2", Player, Cargo, GateB)).Ok);
        Assert.False((await dest.ReserveAsync("t-2", Player, new TransferManifest(new Dictionary<string, long> { ["x"] = 1 }), GateB)).Ok);
        await dest.ActivateAsync("t-2", Player, 2);
        await Assert.ThrowsAsync<InvalidOperationException>(() => dest.CancelAsync("t-2", Player));
        await dest.ActivateAsync("t-2", Player, 2); // idempotent
        Assert.Equal(2, dest.GetHolding(Player)!.Epoch);
    }

    [Fact]
    public async Task Journal_refuses_documents_whose_ownership_contradicts_the_decision()
    {
        var journal = new InMemoryTransferJournal();
        var p = PlayerPresence.Create(Player, A);
        Assert.Equal(JournalWriteStatus.Saved, await journal.CreateAsync(p));
        Assert.Equal(JournalWriteStatus.AlreadyExists, await journal.CreateAsync(p));

        var committedButStillA = p with
        {
            Revision = 1,
            Active = new TransferRecord("t-1", Player, A, B, GateB, "fp", TransferPhase.Committed, 1, Cargo, new SpawnPlacement(GateB, 0, 0)),
        };
        Assert.Equal(JournalWriteStatus.Invalid, await journal.SaveAsync(committedButStillA, 0));
        Assert.Equal(JournalWriteStatus.Saved, await journal.SaveAsync(committedButStillA with { OwnerSectorId = B, Epoch = 2 }, 0));
        Assert.Equal(JournalWriteStatus.RevisionConflict, await journal.SaveAsync(p with { Revision = 1 }, 0));
        Assert.Equal(new[] { Player }, await journal.ListInFlightAsync());
    }

    // ---- Randomized fault soak -----------------------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Random_faults_restarts_and_retries_never_lose_or_duplicate_a_ship(int seed)
    {
        var rng = new Random(seed);
        var sectors = new[] { A, B, C };
        var w = new World(sectors);
        var pilots = new[] { "fixture-pilot-1", "fixture-pilot-2", "fixture-pilot-3" };
        var manifests = pilots.ToDictionary(p => p, p => new TransferManifest(new Dictionary<string, long>
        {
            ["ship:" + p] = 1, ["cargo:fixture-ore-a"] = 10 + p[^1], // NEW provisional test fixture
        }));
        foreach (var p in pilots) await w.AdmitAsync(p, A, manifests[p]);
        var ops = new[] { "detach", "reserve", "activate", "release", "restore", "cancel" };

        for (var step = 0; step < 150; step++)
        {
            var pilot = pilots[rng.Next(pilots.Length)];
            var owner = (await w.Store.LoadAsync(pilot))!.OwnerSectorId;
            var from = rng.Next(5) == 0 ? sectors[rng.Next(3)] : owner;
            var to = sectors.Where(s => s != from).ElementAt(rng.Next(2));
            var entry = rng.Next(8) == 0 ? "fixture-gate-missing" : "fixture-gate-" + to[^1];

            if (rng.Next(3) == 0)
            {
                var n = w.Journal.Saves + 1 + rng.Next(5);
                var mode = rng.Next(2) == 0 ? FaultMode.ThrowBeforeWrite : FaultMode.ThrowAfterWrite;
                w.Journal.OnSave = i => i == n ? mode : FaultMode.None;
            }
            if (rng.Next(4) == 0) w.Sectors[sectors[rng.Next(3)]].FailOnce.Add(ops[rng.Next(ops.Length)]);

            var id = $"t-{seed}-{step}";
            await w.Coordinator.TransferAsync(Ctx(pilot), new SectorTransferRequest(id, pilot, from, to, entry));
            foreach (var p in pilots) w.AssertNeverDuplicated(p, manifests[p]);

            switch (rng.Next(4))
            {
                case 0: w.Reconstruct(); break;
                case 1: await w.Coordinator.TransferAsync(Ctx(pilot), new SectorTransferRequest(id, pilot, from, to, entry)); break;
                case 2: await w.Coordinator.RecoverAsync(pilot); break;
            }
            foreach (var p in pilots) w.AssertNeverDuplicated(p, manifests[p]);
            foreach (var s in w.Sectors.Values) s.FailOnce.Clear();
        }

        w.Reconstruct();
        await w.Coordinator.RecoverAllAsync();
        foreach (var p in pilots)
            await w.AssertSettledAsync((await w.Store.LoadAsync(p))!.OwnerSectorId, p, manifests[p]);
    }
}
