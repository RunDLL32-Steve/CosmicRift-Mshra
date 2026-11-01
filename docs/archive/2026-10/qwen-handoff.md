> Historical snapshot. Preserved for dated evidence; its instructions, counts, branch state and next steps may be superseded. Use [current continuation](../../../CONTINUATION.md), [task queue](../../../TASK_QUEUE.md) and [documentation index](../../README.md) for current work.

# Qwen Handoff — October 14, 2026

Model: qwen3.6:27b-coding. This document summarizes state for any successor session.

## 1. Current State of CosmicRift Implementation

Cosmic Rift: Rogue Trader is an **exact original reproduction project**. The goal is to faithfully reproduce the original game using unchanged fowlmouth assets pinned at manifest commit `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`. Original names, values, categories, pixels, and files take precedence over all provisional proposals.

**Feature tracking**: 394 feature entries (RT-F001–F394), 15 narrowly scoped completions, 379 open. Completions are tooling and infrastructure (parser, viewer, export, arena init, bounded hosting), not gameplay.

**Tech stack**: Godot 4 + .NET inspection toolchain (SDK 8). Production engine choice is **open** — InfantryOnline is under evaluation but compatibility is unverified.

**Architecture**: Server-authoritative, fixed 60Hz simulation loop. Strict separation of simulation/rendering/input/data/networking boundaries. Original bytes preserved unchanged; derived outputs kept separate.

**Milestones**:

-   **M0/M1 (Engineering prototype)**: Complete with historical acceptance. 41 unit/source/export checks pass.
-   **M2 (Original gameplay loop/persistence)**: Not accepted. Generic handlers exist but require runtime validation.
-   **M3 (Multi-player/persistent service)**: Not accepted. Bounded loopback hosting is narrower proof.
-   **Later milestones**: Deferred until original parity.

## 2. Verified vs. Uncertain

### Verified (Accomplished)

| Area                              | Evidence                                                                                                 |
| --------------------------------- | -------------------------------------------------------------------------------------------------------- |
| Original content pinned/unchanged | 46 foundation records, 99 distinct canonical hashes                                                      |
| BLO archive inventory             | All 89 canonical directories, 1,262 members, 609 raw candidates audited                                  |
| Parser/package audit              | 200 items, 30 vehicles, 12 hull links, 81 LIO records, 19,198 supported values                           |
| Original arena initializer        | 25 computers, 100 mineral drops, 51 teams, 42 hides, zero clients/polls/physics                          |
| Bounded full-asset hosting        | Two loopback sockets, unchanged files, separate ready report lists 26 initial vehicles                   |
| VehicleMaker field 18 fix         | Eight values restored, all other exports unchanged                                                       |
| Authentic Soccer archive          | Resolves full-asset startup without changing 89 canonical archives                                       |
| Standalone viewer/export          | 12 hulls, 231 sprites, 1999×1998 overview, source/standalone input, three rendered pages at world tick 0 |
| Windows export artifact           | `artifacts/windows/OriginalContent-2026-10-13-win-x64.zip`                                               |
| Documentation reconciliation      | 23 changed documents, 262 relative paths checked, zero missing paths (completed Oct 13)                  |

### Uncertain / Unverified

| Area                                                                     | Status                                                                                                                                                                  |
| ------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Native client Start/profile behavior                                     | Client reached Welcome screen; Start button click produced no confirmed result. Desktop testing stopped while user played another game. **Requires user availability.** |
| Actual native asset transfer                                             | Full LVL trailer delivery to native client — unverified                                                                                                                 |
| Offline patch-announcement guard                                         | RT-F393 guard compiled but actual CS_Ready acceptance remains Testing                                                                                                   |
| Portal/PrizeCrateA references                                            | Missing — Investigated; genuinely absent from UtilityEffects.blo                                                                                                        |
| Uiart/logo reference                                                     | Missing — Investigated; genuinely absent from cruiart.blo                                                                                                               |
| Numeric BallGraphic                                                      | Investigated; ordinal 0 = GameBall.cfs; interpretation unproven                                                                                                         |
| Missing item 66 eligibility                                              | Investigated; genuinely absent from ITM catalog                                                                                                                         |
| Original movement/combat/mining/refining/trading/repair/upgrades/rewards | Not started — 379 open gameplay features                                                                                                                                |
| Original economy behavior                                                | Unverified                                                                                                                                                              |
| Shop sell quantity/atomicity                                             | Known bug — Investigating                                                                                                                                               |
| Store proximity                                                          | Known bug — Investigating                                                                                                                                               |
| Null-script capacity                                                     | Known bug — Investigating                                                                                                                                               |
| Upstream shutdown race                                                   | Known issue — open                                                                                                                                                      |
| Persistent progression                                                   | Standalone does not save; deferred M2/M3                                                                                                                                |
| Production engine choice                                                 | Open — InfantryOnline compatibility spike unchecked                                                                                                                     |
| CI hosted execution                                                      | Unverified                                                                                                                                                              |

## 3. Current Task and Next Three Actionable Steps

### Current Task

Documentation reconciliation was the active task. **It is complete** — all three reconciliation reports (runtime folders, tools/architecture, content/research) show consistent state with zero missing paths. IMPLEMENTATION_PLAN.md still reads "in progress" and should be updated to "complete."

### Next Three Steps (Priority Order)

1. **Update documentation status**: Change "documentation reconciliation is in progress" to "complete" in `IMPLEMENTATION_PLAN.md` and `CONTINUATION.md`. One-line change, no build required.

2. **Investigate missing references**: Portal/PrizeCrateA, Uiart/logo, numeric BallGraphic, and item 66 eligibility. These are data-level gaps in `data/original/` that block accurate arena initialization. Read only the specific data files and audit reports needed.

3. **Confirm compatible engine**: InfantryOnline server + native client compatibility spike. Read `docs/architecture/ENGINE_EVALUATION.md` and `docs/architecture/ROGUE_RUNTIME_PLAN.md` to understand current evaluation state, then execute a focused compatibility test.

## 4. Important File Paths and Dependencies

### Master Documents

-   `ROGUE_TRADER_2_PLAN.md` — Master specification (2018 lines). All milestones, feature checklist, physics, combat, economy, architecture.
-   `AGENTS.md` — Development conventions. Rules: incremental milestones, simulation/rendering separation, no fabricated history.
-   `CONTINUATION.md` — Handoff state. Accomplished/next/investigating/not-started queues.
-   `PROJECT_START.md` — Entry directive. Current phase: original/native-engine fidelity.
-   `IMPLEMENTATION_PLAN.md` — Repository index. Should mark doc reconciliation as complete.
-   `CHANGELOG.md` — Change history.

### Architecture Plans

-   `docs/architecture/ENGINE_EVALUATION.md` — InfantryOnline evaluation status.
-   `docs/architecture/ROGUE_RUNTIME_PLAN.md` — Runtime reproduction plan.
-   `docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md` — Package audit for RT-F381/F385.
-   `docs/architecture/NATIVE_CLIENT_PLAN.md` — Native client entry plan.
-   `docs/architecture/TOOLCHAIN.md` — Build toolchain.
-   `docs/architecture/LEGACY_COMPATIBILITY_PLAN.md` — Legacy compatibility.

### Development Logs

-   `docs/development-log/PROGRESS.md` — Milestone status. M0/M1 complete, M2-M5 not accepted.
-   `docs/development-log/2026-10-13.md` — Last checkpoint. 14 importer checks, 11 BLOs, 46 originals verified.

### Evidence and Reports

-   `artifacts/qa/OriginalContent` — Viewer/export evidence.
-   `artifacts/rogue-package-audit` — Package audit evidence.
-   `.tools/compatibility/reports` — Compatibility test reports.
-   `.tools/compatibility/local-working/session-host/reports` — Session host evidence.
-   `.tools/native-client-inspection/native-test-cleanup.json` — Native client cleanup state.
-   `DOCS_RUNTIME_FOLDERS_REPORT.md` — Runtime folder reconciliation (complete).
-   `DOCS_TOOLS_ARCHITECTURE_REPORT.md` — Tools/architecture reconciliation (complete).
-   `DOCS_CONTENT_RESEARCH_REPORT.md` — Content/research reconciliation (complete).

### Source Code

-   `client/` — Godot 4 .NET client. Scripts, UI, Scenes, Assets, Audio, Shaders subfolders.
-   `server/RogueTrader.Simulation/` — Simulation engine. M0/M1, flight and combat.
-   `server/RogueTrader.Server/` — Dedicated server. Deferred M3.
-   `server/RogueTrader.AI/` — AI module. Deferred M2.
-   `server/RogueTrader.Persistence/` — Persistence. Deferred M2/M3.
-   `shared/RogueTrader.GameData/` — Provisional .NET schema.
-   `shared/RogueTrader.Protocol/` — Protocol. Deferred M3.
-   `tests/` — Unit, Integration, Network, LoadTests.

### Data

-   `data/` — Provisional ships/weapons/sector definitions.
-   `data/original/` — Canonical original content (ships, weapons, sectors, actors, equipment, resources).
-   `scripts/` — Build and export tools. Original content index plan.
-   `scripts/original-assets/` — Dependency plan, implementation plan.

### Original Research

-   `docs/original-research/` — FEATURE_CATALOG, FIELD_INTERPRETATION, HANDOFF, VIDEO_REFERENCE.
-   `docs/balance/` — Provisional values, implementation plan.
-   `docs/game-design/` — FIRST_CONTACT, implementation plan.

## 5. Unfinished Work and Blockers

### Blockers

-   **User availability for desktop testing**: Native client Start/profile diagnosis requires physical interaction. Do NOT resume desktop automation while the user is busy with other tasks. `PROJECT_START.md` explicitly states this rule.
-   **Production engine choice**: Open. Everything downstream depends on this. InfantryOnline is the candidate but compatibility is unverified.

### Unfinished Work (Ordered by Impact)

1. Documentation status update — one-line change to mark reconciliation complete.
2. Missing reference investigation — Portal/PrizeCrateA, Uiart/logo, numeric BallGraphic, item 66.
3. Engine compatibility confirmation — InfantryOnline spike.
4. Native client entry diagnosis — blocked on user availability.
5. Original gameplay verification — 379 open features.
6. Known server bugs — shop sell quantity, store proximity, null-script capacity, upstream shutdown race.
7. Persistent progression — deferred until original gameplay verified.

### Rules to Follow

-   **One bounded task at a time**. Read only files required for that task.
-   **No file dumps** — use targeted reads, Grep, and Glob.
-   **Persist findings to Markdown** — write reports to `docs/` for cross-session continuity.
-   **Small tasks** — complete and test within one session.
-   **No commit or push** — `CONTINUATION.md` explicitly forbids this.
-   **Preserve originals** — never modify files in `data/original/` or canonical asset paths.
-   **Distinguish generic-handler availability from verified gameplay** — existence of a handler does not mean the feature works correctly.
-   **Do not modify Codex-owned files** without explicit instructions.
