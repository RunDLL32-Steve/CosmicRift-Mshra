Review status: ready-for-review

# Branch log: claude/project-thread-2ig6fb (queue M7 slice and M9)

Scope: the first playable progression loop. Queue M9 depends on M7 (kill/mining rewards through idempotent GrantReward), which was still OPEN, so this branch takes the M7 kill-reward slice and the M9 exit/relaunch acceptance together. See briefs [M7](../../tasks/m7.md) and [M9](../../tasks/m9.md), the [client entry](../../../client/UI/plan.d/m9.md), [reward data](../../../data/rewards/IMPLEMENTATION_PLAN.md) and the [integration plan](../../../tests/Integration/IMPLEMENTATION_PLAN.md).

Status: round 1 pushed, awaiting review. Not merged by the author.

## Round 1 — November 1, 2026

-   M7 slice: `WorldSimulation.Kills` (one `TargetKill` per destroyed target); `data/rewards/kill_reward.json` with the raw rogue.cfg `[Cash] KillReward=75` (ORIGINAL value, flat per-kill reading NEW) and an asset oracle test; `StationProfileSession.GrantKillRewardAsync` with a stable per-sector-instance transaction id; client queue/retry and HUD notice. Mining rewards stay the state-based pickup commit from M8 (no client mining yet).
-   M9: new `tests/Integration` project (added to the solution and `verify.ps1`) with six end-to-end tests over the real `FileProfileRepository`: full loop then exit/relaunch with exact revision, wallet, inventory, upgrades, ship, ledger and applied-transaction ids; kill saved but acknowledgement lost then relaunch retry is a duplicate; save refused before write changes nothing and the retry applies once; crash between temp write and replace keeps the acknowledged revision; torn primary recovers the previous generation; both generations corrupt keeps services offline and never creates an empty profile.
-   M8 review follow-up: a reload or reopen that drops uncommitted pickups now says so.
-   `RogueTrader2.sln`: the Protocol project entry was missing its preceding `EndProject`; fixed while adding the integration project.

Recorded full-loop run (content revision = first 12 hex of SHA-256 per file: ships/starter_interceptor.json 41fbbb2b300e; weapons/plasma_cannon.json 92999e877d03; sectors/first_contact.json 64f60354c898; stations/trading_outpost.json d80b72018597; rewards/kill_reward.json 4eb45ea713ff): pre-exit and post-relaunch both revision 6, wallet 225 (0 + 75 kill + 2,700 Wreckage − 2,500 Turbo AfterBurners − 50 Minor Repair), empty hold, upgrade 61, hull 85 at (0, −83.33), 5 applied transactions. Exit mechanism: dispose the repository (releases the folder lock) and drop session, processor and world; relaunch opens the folder fresh.

Verified in the cloud container (SDK 8.0.133 and Godot 4.5.1 mono Linux substitutions, as M8): solution `--no-incremental` build 0 warnings; `dotnet test tests/Unit` with pinned assets at `eb1514e` 863 passed, 0 failed, 5 existing skips (M8 head: 850); `dotnet test tests/Integration` 6 passed; Godot import exit 0; `--smoke-test` exit 0 with CLIENT_INPUT_SMOKE_PASS, CLIENT_STATION_SMOKE_PASS and CLIENT_SMOKE_PASS; `format-check.py` FORMAT_CLEAN; TRACKERS_CONSISTENT.

Not verified: the `verify.ps1` wrapper itself (its steps were run directly; Windows CI runs it), the Windows export, pinned SDK 8.0.419.

Fidelity: this is engineering persistence acceptance, not original persistence parity (RT-F068 retention is unrecoverable). RT-F051 and every master M2 box stay open; feature counts unchanged.

Resume point: independent review of the round 1 head.
