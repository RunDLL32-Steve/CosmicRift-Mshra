Review status: ready-for-review

# Branch log: claude/project-thread-61bdln (queue C8)

Scope: queue C8, authoritative input validation (speed/rate/teleport sanity) at the `RogueTrader.Server` session boundary. See the [brief](../../tasks/c8.md) and the [server entry](../../../server/RogueTrader.Server/plan.d/c8.md).

Selection (November 2, 2026): section A has no cloud-eligible row and section B has no open row. In section C, C1 is claimed by `claim/c1-blo-reader`, C4/C6 merged in PR #98, C9 was taken by another thread, and C7 hard-depends on the just-merged C6. C8's dependencies (C5, and C6 for the session it extends) are merged; no branch named C8. Claimed with an empty `CLAIM C8` commit on this branch. It touches only `server/RogueTrader.Server`, its tests and tracker docs, not Persistence.

## Round 1 — November 2, 2026

-   `InputVerdict` gains `OutOfRange`, `FutureSnapshot` and `RateLimited`; `SessionOptions` gains `InputsPerTick`, `InputBurst` and `DisplacementTolerance` (all NEW defaults); new `MotionViolation`/`MotionViolationKind`, motion counters on `InputCounters`, and `AuthoritativeSession.RestorePlayer` and `MotionViolations`.
-   New `tests/Unit/Server/InputValidationTests.cs` (24 cases).

Verified in the cloud container (SDK 8.0.133 substituted for the pinned 8.0.419; global.json change not committed): solution builds with 0 errors; `dotnet test tests/Unit` with pinned assets at `eb1514e` 928 passed, 0 failed, 5 existing skips (37 server tests: 13 C6 plus 24 C8); `dotnet test tests/Integration` 6 passed; benchmark exit 0 with BENCHMARK_REPORTED and identical digests; FORMAT_CLEAN; TRACKERS_CONSISTENT.

Not verified: the `verify.ps1` wrapper itself (Windows CI runs it), pinned SDK, Godot smoke (no client change).

Resume point: independent review of this head. Approval of the NEW thresholds is the owner's call.
