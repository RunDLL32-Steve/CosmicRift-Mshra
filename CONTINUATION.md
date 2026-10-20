# Current continuation — October 17, 2026

Read [PROJECT_START.md](PROJECT_START.md), [AGENTS.md](AGENTS.md), the complete [master](ROGUE_TRADER_2_PLAN.md), and the selected task's folder plan. **397 feature entries, 22 scoped completions, 375 open.** The source/documentation audit baseline is `d14df02` from `origin/main`; later remote changes require reconciliation.

## Current implementation

- The Godot client still runs the provisional flight/combat range and the original-content inspector. It references GameData and Simulation; it does not connect the trading, persistence, protocol or AI modules into a Rogue Trader loop.
- `shared/RogueTrader.OriginalData` contains raw RPG/VEH/ITM/CFG readers and LIO reference views, typed reference views and canonical guards. Later audits/oracles expand field/reference coverage. Data availability does not complete native runtime behavior.
- `server/RogueTrader.Simulation` includes generic cargo pickup, optional finite-deposit mining extraction and replay recording/hash comparison (format v2; v1 is explicitly rejected because input/hash semantics changed). Cargo masses/capacities and pickup rules are not accepted original semantics.
- `server/RogueTrader.Persistence` contains profile repositories and the transaction core; original-ID content validation is also present. `server/RogueTrader.Trading` supplies generic proximity and buy/sell orchestration. No client exit-and-return progression is accepted.
- `shared/RogueTrader.Protocol` contains a binary message codec; there is no production transport/authentication session. `server/RogueTrader.AI` contains a NEW/provisional pirate controller; original pirates and world/client integration remain open.
- The derived compatibility pipeline includes scoped sell, buy, store-location, null-script capacity, radar-config and packet-length fixes. RT-F388/389/390/391/395/396/397 have their narrow recorded acceptance; they do not establish a playable native session or production shutdown safety.

## Recorded acceptance and limits

M0/M1's 41-unit-test source/export/input/world acceptance is historical, as are the October 13 original inspector/export checks (12 hulls, 231 sprites, exact 1999×1998 overview). See [progress and evidence](docs/development-log/PROGRESS.md), [CHANGELOG](CHANGELOG.md) and dated logs. No gameplay checks were rerun for the October 17 documentation audit.

The October 13 parser/package/arena/host work records 200 items, 30 vehicles, 12 hull links, 81 LIO records, 25 initialized computers and 100 drops. The separate host-ready observation of 26 initial vehicles is a different measurement. Native Welcome was observed; Start/arena entry was not confirmed. Old test processes/private keys were cleaned up at that checkpoint; this is not a claim about every process currently running.

RT-F397 merged previously; its historical evidence covers 46 canonical packets and 60 inert queue fixtures. RT-F396's dated buy slice records 50 total checks; current RuntimeChecks source has 15 `Buy.*` checks and the later fixed report records 61 total checks. Totals from different revisions are not interchangeable. Wrappers now check the cached process handle/exit code: blank or non-zero exit is failure, even if a report exists.

Ignored local artifacts may be absent in another checkout. Historical test totals describe the dated run, not the current suite size. Original data/runtime oracle skips must be disclosed, and source inspection cannot substitute for native reception, launch or gameplay acceptance.

## Next

G9 is CLAIMED / in progress on `claim/g9-dependency-checkpoint`; the [ledger](scripts/original-assets/plan.d/g9.md) indexes accepted dated evidence and remaining scope. Current [initialized reset-input observation](docs/development-log/2026-10-19-g9-reset-input-observation.md) captures the selected standalone initialized object before newArena; use the [branch log](docs/development-log/cloud/claim-g9-dependency-checkpoint.md) and its current PR for live workflow status. PR85 conditional lifecycle source and PR76 selected catalog are accepted dated history. Follow that live workflow record before selecting remaining G9 work from the ledger; preserve earlier accepted results; native consumers/gameplay/full closure and original feature boxes stay open,397/22/375/raw609/eight unchanged. No claim release or M5 work follows.

Queue M3 generic mining merged in [PR 60](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/60) at `1037592dc48991d61ccc8900374b1f371da58368` after independent exact-head review and successful Windows CI; see [local evidence](docs/development-log/2026-10-18-m3-mining.md). The generic slice is completed queue history; do not reclaim it. Original RT-F042 remains open. Next integration remains M8 client input/HUD and the original equipment/spawn/evaluator bindings after their dependencies are validated. Mining intent is not carried by the current Protocol codec; transport integration remains outside M3.

Select an unclaimed item from [TASK_QUEUE.md](TASK_QUEUE.md), then read its separate brief under [docs/tasks](docs/tasks/README.md). Check live PR/branch claims before implementation. Generic queue work items already present in source are history, with their original-parity gaps retained.

The remaining fidelity gates include native Start/profile/entry and engine choice (RT-F380/F384), actual delivery/offline patch readiness (RT-F387/F393), unresolved original reference semantics (RT-F377–379/F386/F069), and original gameplay/persistence acceptance. Use the task briefs and [feature catalog](docs/original-research/FEATURE_CATALOG.md) for exact scope; all unfinished feature boxes stay open.

Do not repeat merged hull-loader, persistence-core, cargo, mining, trading, replay, codec or provisional-AI work. Extend only after its dependencies and original semantics are validated. Preserve canonical data, distinguish generic tests from original behavior and keep later expansion deferred.

## Handoff and review

The [documentation index](docs/README.md) is the navigation entry. [Archived handoffs](docs/archive/README.md) are dated snapshots. The [October 17 audit](docs/audits/2026-10-17-documentation-audit.md) records coverage, source evidence, mechanical checks and the review outcome. Queue M3 adds a generic engine slice with local verification recorded separately; it does not mark original gameplay acceptance.
