# Architecture implementation plan

Master authority: [§5.3 simulation boundaries](../../ROGUE_TRADER_2_PLAN.md#53-physics-requirements), [§13 multiplayer](../../ROGUE_TRADER_2_PLAN.md#13-multiplayer-architecture), [§14 persistence](../../ROGUE_TRADER_2_PLAN.md#14-persistent-data), [§18 M0/M1 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay) and [M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer). Canonical acceptance IDs: [original feature catalog](../original-research/FEATURE_CATALOG.md). Reconciled October 13, 2026 with [progress](../development-log/PROGRESS.md). The master remains the status authority.

## Completion history and evidence

| Scoped feature | Current status | Evidence and limit |
|---|---|---|
| Godot-independent simulation, external JSON, separate input/render/UI | Complete with evidence | [Simulation](../../server/RogueTrader.Simulation/IMPLEMENTATION_PLAN.md), [GameData](../../shared/RogueTrader.GameData/IMPLEMENTATION_PLAN.md), [client](../../client/IMPLEMENTATION_PLAN.md) and historical M0/M1 acceptance. Shared rules currently execute locally; no dedicated server/prediction/persistence is implied. |
| Pinned toolchain/reproducible export foundation | Complete with evidence | [TOOLCHAIN.md](TOOLCHAIN.md), [lock](../../toolchain.lock.json), [global.json](../../global.json), prior 41 tests/source-export smoke/standalone launch in [progress](../development-log/PROGRESS.md#final-validation-evidence). No new build/launch occurred here. |
| Source-based InfantryOnline feasibility/spike design | Complete with evidence, source evaluation only | [ENGINE_EVALUATION.md](ENGINE_EVALUATION.md) separates native client/emulator/authority/formats. Production engine decision remains open. |
| Pristine selected-file parsing and diagnostic runner repair | Complete with evidence, loader only | Existing [load report](../../.tools/compatibility/reports/pristine-load-report.json): FailureCount 0/AssetsUnchanged true; CFG, ITM 200, VEH 30, LVL 1999×1998/3,994,002 tiles/116,065 nonzero physics, LIO 81, RPG 12. [Runner regression](../../.tools/compatibility/reports/runner-error-regression.json) records controlled exits 2/1; [outer exception regression](../../.tools/compatibility/reports/malformed-exception-regression.json) records exit 1. Narrow parser/runner acceptance only. |

## Active outstanding work

- [ ] **Investigating — production engine/server/native-client compatibility:** follow [engine evaluation](ENGINE_EVALUATION.md) and [legacy spike](LEGACY_COMPATIBILITY_PLAN.md). Existing local server-probe logs record initialization dependencies/failures; compilation/parsing do not prove a running RT world or native entry/unspectate.
- [ ] **Investigating — RT-F021 / RT-F069:** finish level/object/collision/terrain and dependency interpretation. Active LVL parser skips trailing graphical entities; nonzero physics counts/overview BMP do not prove native collision traversal.
- [ ] **Investigating — RT-F027–RT-F041 / RT-F067:** establish versioned units, negative sentinels, skill/event grammar and execution order. Preserve raw fields plus sourced conversion hypotheses. Bot arithmetic and native player behavior are separate contracts.
- [ ] **Not started — RT-F074 / Investigating — RT-F075:** resolve palette/HSV/shadow blits, direction rounding and bank/state selection. Original HSV Value 5 does not prove neutral column 5.
- [ ] **Not started — M2 persistence boundary, RT-F068:** define original progression contracts/interfaces after dependencies validate; document unavoidable reconstructed backend policy. Directory scaffolds do not implement save/load.
- [ ] **Not started — M3 authoritative multiplayer:** design/verify protocol, input validation, reconciliation and transactions after the original loop/engine dependencies pass. Current emulator accepts client state/energy updates; reconcile that path with master server authority before production adoption.

Original names/categories/values/bytes remain unchanged at eb1514eaae7f9b9425307d9cc043c74ecdd15e35; derived adapters/resources remain separate. [Field contracts](../original-research/FIELD_INTERPRETATION.md) retain unresolved semantics. NEW expansions stay deferred. Source availability, parsing, decoded pixels and prototype tests do not establish exact parity. Inspect licenses before reuse. Documentation reconciliation reads existing logs/source; simulation follows a completed feature requiring behavioral verification.
