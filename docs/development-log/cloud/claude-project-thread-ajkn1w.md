Review status: ready-for-review

# Branch log: claude/project-thread-ajkn1w (queue C10)

Scope: queue C10, a recoverable sector transfer model in `server/RogueTrader.Persistence/Transfers`. See the [brief](../../tasks/c10.md) and the [persistence entry](../../../server/RogueTrader.Persistence/plan.d/c10.md).

Selection (November 2, 2026): section A has no cloud-eligible row and section B has none open. In section C, C1 is claimed, C4/C6/C8 are merged, C9 is in review (PR #100), and C7 is being done outside this repository's threads. C10 depends only on M1, which is merged, and no branch names it. Claimed with an empty `CLAIM C10` commit on this branch. It touches no `shared/`, `server/RogueTrader.Server` or `server/RogueTrader.Simulation` file, so it cannot collide with C7, and it leaves the profile schema alone, so it cannot collide with C9 or PR #102.

## Round 1 — November 2, 2026

-   New `RogueTrader.Persistence.Transfers` namespace: `SectorTransferCoordinator`, `ITransferJournal`/`InMemoryTransferJournal`, `ISectorParticipant`/`InMemorySectorParticipant`, `PlayerPresence`, `TransferRecord`, `TransferManifest`, `SpawnPlacement`, `TransferInvariants`, `TransferOptions` and result/reason enums.
-   New `tests/Unit/Persistence/SectorTransferTests.cs` (42 cases).

Verified in the cloud container (SDK 8.0.133 substituted for the pinned 8.0.419; global.json change not committed): `dotnet test tests/Unit` with pinned assets at `eb1514e` 992 passed, 0 failed, 5 existing skips (after merging main with C9 #100); FORMAT_CLEAN; TRACKERS_CONSISTENT.

Not verified: the `verify.ps1` wrapper itself (Windows CI runs it), pinned SDK, Godot smoke (no client or simulation change).

## Round 2 — November 2, 2026

-   R1-1 (High): participants now record decisions under `SectorTransferCoordinator.ParticipantKey` (player, transfer id, source epoch) instead of the bare transfer id, so two players sharing an id or a retry of an id that left the Recent window can no longer wedge a player in `Aborting`. Contract text updated. Three regression tests (both review scenarios plus an evicted aborted id at the same epoch) fail on the old keying and pass now; the soaks now share ids across pilots with a window of 4. 45 transfer tests.
-   R1-2: main (#100) merged in at 17b653b, all entries kept, C10 row IN REVIEW.

Verified: `dotnet test tests/Unit` 995 passed, 0 failed, 5 skips; FORMAT_CLEAN; TRACKERS_CONSISTENT.

Resume point: independent review of this head.
