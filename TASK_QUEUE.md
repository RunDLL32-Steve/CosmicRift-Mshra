# Task queue: ordered active index

This file schedules unfinished queue items. Each active row links to one actionable brief in [docs/tasks/README.md](docs/tasks/README.md). The complete feature catalog remains in [ROGUE_TRADER_2_PLAN.md §18](ROGUE_TRADER_2_PLAN.md#18-development-roadmap). Queue IDs G/M/C are scheduling identifiers; queue M1 is not master Milestone 1.

## How to select work

-   Process section A before B, then C, following the master fidelity gate and dependencies. A cloud run takes the first OPEN eligible row, skipping rows claimed by any non-main origin branch or whose dependencies are not MERGED on origin/main. Local-only rows require the listed tools/assets/desktop.
-   Before work, inspect current origin/main, branches, PRs and the item log. Table status is a repository snapshot, not live GitHub state. A row is eligible only when OPEN on current origin/main and every dependency is MERGED there.
-   Any non-main origin branch containing the queue ID or RT-ID in its name, commits or log claims that item, except when its newest commit starts RELEASE under the release procedure below. Claims never expire. Do not take over another owner's claim.
-   Claim before starting: local work creates `claim/<id>-<slug>` with an empty `CLAIM <id> <task> by local` commit, pushes only when authorized, and confirms it with `git ls-remote` before implementation. Cloud work uses its `cloud/<id>-<slug>` branch as the origin-visible claim. A conflicting branch/PR claim means the item is taken. Follow AGENTS.md for push authorization and PR review; without required authorization, stop before scoped work.
-   To release an abandoned claim, only its owner may arrange a reviewed update of the main row to OPEN (claim abandoned: reason) and push a claim-branch commit beginning RELEASE; this is not a timeout or transfer of ownership.
-   Do not push main from a task branch. Branch push, PR, review and merge permissions follow AGENTS.md and the user's current authorization. Do not infer standing authorization from this index.
-   Preserve canonical originals byte-for-byte; put derived results elsewhere. Keep unknown meanings visible; mark invented fixtures NEW. Narrow parsers, generic systems and renderers do not complete original gameplay parity.
-   Run only the focused checks in the brief, following AGENTS.md. Markdown-only changes use python scripts/check-trackers.py.

## A. Fidelity gate

| ID  | Task                                                                            | Where                     | Run             | Depends on                         | Status snapshot                                                                                                                                                 | Brief                    |
| --- | ------------------------------------------------------------------------------- | ------------------------- | --------------- | ---------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------ |
| G1  | Isolated original-client/emulator compatibility and production-engine choice    | Native client             | Local (desktop) | —                                  | BLOCKED: user must be present (RT-F380/F384/F387/F393)                                                                                                          | [G1](docs/tasks/g1.md)   |
| G5  | Original weapon/ammo records and child-chain value audit; no firing             | OriginalData              | Cloud/local     | G3 MERGED                          | IN REVIEW (remainder audit on `claude/project-thread-5cw3pv`); RT-F036/F037/F040 data scopes merged                                                             | [G5](docs/tasks/g5.md)   |
| G6  | Original LVL/LIO reader and collision grid, RT-F021/F022 data only              | OriginalData              | Cloud/local     | —                                  | IN REVIEW (`claude/project-thread-jzhlen`; Steve took over his own Oct 15 `claim/g6-level-reader`)                                                              | [G6](docs/tasks/g6.md)   |
| G8  | Validate flight scaling/sentinels against recorded evidence; no gameplay change | Docs + OriginalData tests | Local           | G2, G3 MERGED                      | IN REVIEW (`claude/project-thread-9vx2de`: cloud evidence table; native measurements and G8a subscope remain)                                                   | [G8](docs/tasks/g8.md)   |
| G9  | BLO member identity and dependency closure recompute, RT-F069                   | original-assets tools     | Local           | —                                  | CLAIMED `claim/g9-dependency-checkpoint`; read-only checkpoint verified, overall acceptance incomplete                                                          | [G9](docs/tasks/g9.md)   |
| G10 | Original flight/weapons/controls/HUD validation against evidence                | Client + simulation       | Local           | G2, G3 MERGED; G6 must be resolved | IN REVIEW (table #113, Asp flight #114, map scale #118 merged; projectile units W2/W3 on `claude/project-thread-kpj76j`; strafe/hyper and native checks remain) | [G10](docs/tasks/g10.md) |

## B. Milestone 2 loop

No active rows: M1 to M9 are merged (see the history below).

## C. Additional groundwork

Only after every earlier cloud-eligible row is claimed, merged or blocked. Apply the listed dependencies as hard gates.

No active rows: C1 is merged; C7 scoped library acceptance is verified and its [PR 117](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/117) awaits final gates.

## Completed queue history (not active work)

-   C7 — scoped motion prediction/reconciliation acceptance verified; [PR 117](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/117) awaits independent current-head review and merge gates. [Evidence](docs/development-log/2026-11-03-c7-publication.md). Master M3 remains open.
-   C1 — raw directory/member acceptance independently reproduced at `2a302a2` (89 archives / 1,262 members); [PR116](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/116) merged at `e2a5aa6`. No RT-F023/RT-F069 or original fidelity completion. [Evidence](docs/development-log/2026-11-03-c1-publication.md).

-   G2 — merged 2e76140 (loader fe879b1): 12 raw hull records and integrity guard; native ship choice/loadout/movement and field meaning remain open.
-   G3 — merged 7ed3648: 200 raw-preserving ITM records; feature-specific interpretations remain open.
-   G4 — merged 7ed3648 with G3: seven resource records/categories; gameplay rules remain open.
-   G7 — merged 3f2ae03: 30 VEH rows and 18 non-hull rows; RT-F055 clauses remain open.
-   M1 — merged b2ac2bf: generic persistence/transaction core; full original lifecycle binding and M2 acceptance remain open.
-   M2 — merged 7684b05: generic cargo/pickup engine slice; RT-F030/F042 remain open.
-   M3 — merged `1037592dc48991d61ccc8900374b1f371da58368` ([PR 60](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/60)): generic finite-deposit mining/equipment extraction and replay v2; original RT-F042, client/transport/persistence integration and master M2 original acceptance remain open.
-   M4 — merged 3e29142: generic trading engine slice; RT-F044/F045/F049 remain open.
-   M5 — merged ([PR 92](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/92)), November 1, 2026: repairs and original upgrade pricing, profile schema 2; RT-F047/F048/F049 remain open.
-   M6 — merged 759a66a: NEW provisional pirate state machine, not wired into WorldSimulation; RT-F060/F272/F306 remain open.
-   M9 — merged ([PR 94](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/94)), November 1, 2026: exit/relaunch integration suite together with the M7 kill-reward slice; engineering persistence acceptance only, RT-F051/F068 remain open.
-   M8 — merged ([PR 93](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/93)), November 1, 2026: Godot HUD, derived Trading Outpost, save/load and ship condition (schema 3); native UI parity and RT-F025/F026/F068 remain open.
-   C2 — merged 8f66688: rogue.cfg only; other arena CFGs were not pinned.
-   C3 — merged 952d29d: same-runtime replay digests; does not establish cross-runtime determinism.
-   C4 and C6 — merged ([PR 98](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/98)), November 2, 2026: Stopwatch tick benchmark in `tests/LoadTests`, and the headless `AuthoritativeSession` with a bounded input queue in `RogueTrader.Server` (no sockets); no multiplayer capacity or production authority claim.
-   C8 — merged ([PR 101](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/101)), November 2, 2026: NEW range, snapshot-acknowledgement and rate checks on submitted input plus a report-only motion audit in `RogueTrader.Server`; no transport.
-   M7 — merged in two slices: kills with M9 ([PR 94](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/94)), and mining, piles, refinery and the per-ship hold weight cap ([PR 102](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/102)), November 2, 2026; RT-F030/F042/F044 remain open.
-   C9 — merged ([PR 100](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/100)), November 2, 2026: NEW data-driven bounty/reputation rules, profile schema 5; nothing in the game raises conduct events yet, RT-F051 remains open.
-   C10 — merged ([PR 103](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/103)), November 2, 2026: NEW recoverable sector transfer model in `RogueTrader.Persistence/Transfers`; not wired into the game (one sector).
-   C5 — merged 403499f: NEW provisional wire format v1, round-trip/truncation/fuzz checks, no transport; original RT features untouched.

## Historical claim notes (not live claims)

The prior queue contained the following branch/PR notes. Its observation time was not recorded, so none is asserted current. Check `git ls-remote`, current branch logs and open PRs before selecting an item. Old names are preserved here only to prevent accidental claim loss:

-   `claim/c1-blo-reader` (C1); `claim/g6-level-reader` (G6); `claim/g8a-format-audit` (subscope of G8); `claim/g11-hull-reference-resolution` (unrowed G11, ambiguous).
-   `claim/c1-blo-reader` was released by its owner (Steve) on November 3, 2026: its only commits were two empty `CLAIM C1` markers from October 15, 2026, on top of main at `239188e`, with no implementation and no open PR. Its newest commit now starts `RELEASE`, so it no longer claims C1. Steve is handing C1 to Codex.
-   `claim/g2-original-hulls` and `claim/m1-persistence-core` conflicted with already-merged queue rows in that old snapshot; reconcile with current main.
-   `cloud/rt-f063-space-storm-audit`, `cloud/rt-f064-los-radar-fields`, `cloud/rt-f065-safe-zone-docking`, `cloud/rt-f068-progression-contract`, `cloud/rt-f076-plasma-cannon-item`, and `cloud/rt-f077-f080-micromissile-children` were listed as claims; inspect live status before touching any related ID.
-   `local/bbq-cooked-workflow` was listed as workflow tooling with no RT-ID.
-   Prior owner PR numbers #23 and #41–#46 were not mapped to branches. No current PR association is implied.

## D. Closed decisions (do not redo)

-   RT-F391 is closed as validated optional admin policy: PrizeItems.xml is an optional granted-moderator whitelist; no file or stub. See [2026-10-15 log](docs/development-log/2026-10-15-rt-f391.md).
-   Do not build a skill-shop location gate: the pinned server parses terrain SkillEnabled but never enforces it; adding a gate invents a rule.
-   RT-F388/F389/F390/F395/F396/F397 are complete narrow derived-emulator safeguards. They do not close broader original feature entries; see the feature catalog and their logs.

## Scope not yet queued

The prior index noted master §18 work for original asset appearance/behavior (RT-F023/RT-F025) without a dedicated row. This index does not invent or schedule a new task; reconcile it with the current master/catalog before assigning work. For asset-dependent tasks, Git LFS pointers are not source data: if originals cannot be hydrated, report checks SKIPPED and local verification required.
