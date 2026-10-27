Review status: review-changes-requested

# Branch log: claude/project-thread-e0ajlh (queue M5)

Scope: queue M5, basic repairs and equipment upgrades through the transaction core. See [brief](../../tasks/m5.md) and [Persistence plan entry](../../../server/RogueTrader.Persistence/plan.d/m5.md).

Status: wip, round 1 changes requested on PR #92.

Verified locally: `dotnet test tests/Unit` on SDK 8.0.132 (pinned 8.0.419 could not be downloaded in this environment): 781 passed, 0 failed, 5 skipped (same 5 key-listing skips as the 757-test baseline), with pinned original assets hydrated. 24 new tests in `tests/Unit/Persistence/RepairAndUpgradeTests.cs`. `python scripts/check-trackers.py` prints TRACKERS_CONSISTENT.

Not verified: `scripts/verify.ps1`, Godot smoke (client untouched), pinned SDK build, sprint integration scripts.

Resume point: author fixes R1-1 and R1-2, replies with finding IDs and fix SHAs, lists all files updated since reviewed head `96ac600fe14f38818e3cf20a9dbb4cd968ece8c3`, reruns applicable checks and tracker consistency, then BBQ and re-review. No original feature completion is accepted.

## Independent review round 1 — October 27, 2026

[Full review and inline findings](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/92#pullrequestreview-5662449848), exact head `96ac600fe14f38818e3cf20a9dbb4cd968ece8c3`, base `1ce1d21671a2f23781804875a7db353c42718a7f`. Verdict: CHANGES REQUESTED. Reviewer changes only this log; product fixes belong to the author.

- R1-1 (major/P1): `repairId` changes the strict save payload without advancing schema version 1. Reproduced with the previous reader compiled from origin/main: one ordinary purchase restores revision 0 from backup (wallet 460 to 500); two purchases quarantine both generations and return Corrupt. Introduce a newer schema and registered v1 migration; verify old saves load and the previous reader refuses new saves without moving files.
- R1-2 (minor/P2): a rejected `WithRepairPrices`/`WithUpgradePrices` batch partially mutates the existing table. Reproduced a duplicate repair batch that throws but leaves its first price active and blocks a corrected retry. Validate the complete batch before publishing entries; cover both methods and rejected-input classes.
- Coordinator read the full 15-file diff, M5 brief, master requirements and surrounding code. An independent read-only persistence agent found R1-1; coordinator inspected the actual sources and reproduced it. Model substitution: GPT-6.1 Sol High, because the named repository review models were unavailable.
- Exact-head verification on pinned SDK 8.0.419: 781 passed, 0 failed, 5 existing key-listing skips; original pricing oracle passed. TRACKERS_CONSISTENT (397/22/375); git diff --check passed. Two GitHub windows checks succeeded. NuGet vulnerability lookup unavailable (NU1900); cached restore/build/tests completed.
- Local sprint `sprint/review-92-2026-10-26-2359`: #92 merged cleanly; 12 PRs merged, #53 conflicted in AGENTS.md, no fetch failures. SPRINT_INVALID. Sprint unit tests: 825 passed, 0 failed, 5 skipped; 27 review-queue tests and 13 native-start tests passed. Godot verify and compatibility runtime/native-delivery are not verified in the sprint report. No sprint pushed. Local evidence: `artifacts/review-pr-92-sprint-validate.json`, `artifacts/review-pr-92/pr-92.trx`, and disposable reproductions under `artifacts/review-pr-92/repro/`.
- PR-specific client/Godot, native/runtime, GUI/export and standalone simulation checks skipped per unchanged paths. Wallet-only repair and five original categories versus six NEW fixtures are disclosed scope limits; simulation health, historical upgrade behavior and full M5 remain unfinished. Original feature boxes/counts unchanged. PR not merged.
