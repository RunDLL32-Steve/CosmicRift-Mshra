# Integration test implementation plan

Status authority: [master M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), [M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer), [§20.2 economy](../../ROGUE_TRADER_2_PLAN.md#202-economy-tests), and [§20.3 multiplayer](../../ROGUE_TRADER_2_PLAN.md#203-multiplayer-tests). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; no integration runs.

Module status: **Not started**. No integration test implementation exists here. Completed M0/M1 source/export smoke belongs to client/script acceptance; it does not complete the M2 original loop or M3 multi-client suite.

## Outstanding work

-   [ ] **Investigating** — establish original acceptance oracle for RT-F001–RT-F019/RT-F033–RT-F051/RT-F067–RT-F068: original units, installation restrictions, source categories, recipes, prices/sentinels, native event ordering and retained progression. Native bank/state/presentation comparisons remain separate fidelity requirements.
-   [ ] **Not started** — M2 complete original acquire/collect/cargo/refine/sell/buy/upgrade/repair/enemy/reward/exit/reload loop using canonical records. Preserve recovered mineral spawns; an invented mine/sell loop does not complete original acceptance.
-   [ ] **Not started** — end-to-end atomic/duplicate/failed transaction checks and corrupted/interrupted save recovery through the game loop; unit-level transaction/file-recovery checks already exist in `tests/Unit/Persistence`, and station composition cases in `tests/Unit/Trading`; verify inventory/currency conservation and precise selected retention policy.
-   [ ] **Not started** — M3 independent-client world/combat/resource/NPC/economy/progression acceptance and disconnect/reconnect/transfer duplication checks; validate RT-F050–RT-F058/RT-F064–RT-F068 where implemented.
-   [ ] **Not started** — record exact implemented feature/revision, expected result and actual acceptance evidence, then reconcile master/catalog. Whole original hull/item features remain unchecked when only an adapter/image/parser subfeature passes.

Completion history: no test in this module is complete. Prior [41 unit tests and source/export acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) are narrower engineering evidence. The separate [pristine parser report](../../.tools/compatibility/reports/pristine-load-report.json) proves accepted unchanged formats. Later original-arena and bounded full-asset loopback hosting also pass, but native entry and the original game loop remain unverified. Run simulations only after the specific gameplay feature exists and requires behavior verification; no baseline reruns for documentation.

## Source audit — October 17, 2026

Current code and remaining integration limits are recorded in the [source documentation audit](../../docs/audits/2026-10-17-source-docs.md). Earlier dated test counts above are historical branch results, not a current-tree test run. No build, runtime check, GUI or simulation was run for this documentation audit. Global feature counts and completion status belong to [master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap).
