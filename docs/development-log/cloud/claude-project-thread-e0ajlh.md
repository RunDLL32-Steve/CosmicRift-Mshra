Review status: review-pending

# Branch log: claude/project-thread-e0ajlh (queue M5)

Scope: queue M5, basic repairs and equipment upgrades through the transaction core. See [brief](../../tasks/m5.md) and [Persistence plan entry](../../../server/RogueTrader.Persistence/plan.d/m5.md).

Status: wip, ready for review once the PR is opened.

Verified locally: `dotnet test tests/Unit` on SDK 8.0.132 (pinned 8.0.419 could not be downloaded in this environment): 781 passed, 0 failed, 5 skipped (same 5 key-listing skips as the 757-test baseline), with pinned original assets hydrated. 24 new tests in `tests/Unit/Persistence/RepairAndUpgradeTests.cs`. `python scripts/check-trackers.py` prints TRACKERS_CONSISTENT.

Not verified: `scripts/verify.ps1`, Godot smoke (client untouched), pinned SDK build, sprint integration scripts.

Resume point: address review findings, then BBQ.
