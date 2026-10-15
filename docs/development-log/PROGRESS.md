# Development progress — October 14, 2026

Authority:[master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap). Current handoff:[CONTINUATION.md](../../CONTINUATION.md).

**396 feature entries;18 scoped completions;378 open.** M0/M1 prototype, original viewer/export, inventory/package/runtime audits, actual arena initialization, bounded full-asset hosting and VehicleMaker field18 fix pass. October 14 adds scoped sell validation, null-script capacity and host radar safeguards: 35/35 focused checks and a clean 10-second headless host. Native welcome only; no player joined. Actual native delivery/gameplay/persistence remain unfinished. No desktop interaction occurred in this slice.
**395 feature entries;16 scoped completions;379 open.** M0/M1 prototype,original viewer/export,inventory/package/runtime audits,actual arena initialization,bounded full-asset hosting andVehicleMaker field18 fix pass. Native welcome only;no player joined. Actual native delivery/gameplay/persistence remain unfinished. Desktop interaction stopped;owned test processes/keys cleaned up.

## Current milestone status

- RT-F397: narrow outgoing length/queue acceptance complete on the isolated issue branch;46 canonical packets/60 inert fixtures pass, all baseline defects corrected. [NativeDeliveryChecks](../../scripts/compatibility/NativeDeliveryChecks/IMPLEMENTATION_PLAN.md) and [dated issue log](2026-10-14-rt-f397.md) record the two-overload length/queue scope. No native receiver/auth/UI acceptance or desktop use.

- M0/M1 engineering:Complete with historical acceptance.
- Original tooling/source/export/arena/hosting:scoped completions recorded in master/catalog.
- Native playable reproduction:In progress/unverified.
- M2 original gameplay loop/persistence:Not accepted;generic handlers exist but require runtime validation.
- M3 multiple-player/persistent service:Not accepted;bounded loopback hosting is narrower proof.
- Later additions:Deferred until original parity.

## Subplan index

Every path below is an implementation checklist with a reciprocal reference to the master. Checkboxes describe actual completed work; deferred work remains unchecked.

| Folder / subplan | Master scope |
|---|---|
| [Client](../../client/IMPLEMENTATION_PLAN.md) | M1 / §19 slices A–E, input, HUD, camera and standalone client |
| [Simulation](../../server/RogueTrader.Simulation/IMPLEMENTATION_PLAN.md) | M0/M1, §§5/7, flight and combat |
| [Game data](../../shared/RogueTrader.GameData/IMPLEMENTATION_PLAN.md) | M0/M1, §6.1 external validated definitions |
| [Data catalog](../../data/IMPLEMENTATION_PLAN.md) | M0/M1, provisional ships/weapons/sector |
| [Unit tests](../../tests/Unit/IMPLEMENTATION_PLAN.md) | M1 / §20.1 physics and combat |
| [Original content index](../../scripts/ORIGINAL_CONTENT_INDEX_PLAN.md) | §1.2 fidelity gate / original raw records and candidate dependencies |
| [Build and export](../../scripts/IMPLEMENTATION_PLAN.md) | M0/M1, reproducible tools and Windows artifact |
| [CI workflow](../../.github/workflows/IMPLEMENTATION_PLAN.md) | M0 automated build; hosted run unobserved |
| [Historical research](../original-research/IMPLEMENTATION_PLAN.md) | M0 / §§2/22 plus user-supplied archives/video |
| [Balance ledger](../balance/IMPLEMENTATION_PLAN.md) | M0 / §§5–8 provisional values and recovered raw values |
| [First Contact design](../game-design/IMPLEMENTATION_PLAN.md) | M1 slices A–E and fidelity acceptance |
| [Development log](IMPLEMENTATION_PLAN.md) | M0/M1 / §21 maintenance and reconciliation |
| [Runtime checks](../../scripts/compatibility/RuntimeChecks/IMPLEMENTATION_PLAN.md) | Fidelity prerequisites RT-F388/RT-F390/RT-F395; purchase safety RT-F396 remains open |
| [Architecture](../architecture/IMPLEMENTATION_PLAN.md) | M0/M1 / §§5.3/13/17/21 |
| [Server](../../server/RogueTrader.Server/IMPLEMENTATION_PLAN.md) | Deferred M3 |
| [Persistence](../../server/RogueTrader.Persistence/IMPLEMENTATION_PLAN.md) | Deferred M2/M3 |
| [AI](../../server/RogueTrader.AI/IMPLEMENTATION_PLAN.md) | Deferred M2 |
| [Protocol](../../shared/RogueTrader.Protocol/IMPLEMENTATION_PLAN.md) | Deferred M3 |
| [Integration](../../tests/Integration/IMPLEMENTATION_PLAN.md) | Deferred M2/M3 |
| [Network](../../tests/Network/IMPLEMENTATION_PLAN.md) | Deferred M3 |
| [Load tests](../../tests/LoadTests/IMPLEMENTATION_PLAN.md) | Deferred M5 |


## Current plans and evidence

Additional scoped plans:client/ORIGINAL_INSPECTOR_PLAN.md;scripts/original-assets/DEPENDENCY_PLAN.md;docs/architecture/NATIVE_CLIENT_PLAN.md,ROGUE_RUNTIME_PLAN.md,ROGUE_PACKAGE_AUDIT_PLAN.md;data/original domain queues. Each meaningful source folder records accomplished/next/parity status in its implementation plan or linked parent.

Evidence:artifacts/qa/OriginalContent;artifacts/rogue-package-audit;.tools/compatibility/reports;.tools/compatibility/local-working/session-host/reports;native-test-cleanup.json. Initializer25 computers and host-ready26 initial vehicles are distinct observations. Full LVL graphical trailer must remain intact for native delivery.

## Final validation evidence

Historical41 unit/prototype input/world checks are preserved in October12 log/master/changelog;they were not repeated for assets/documentation. Updated OriginalContent source/standalone input/render checks pass atworldtick0. Runtime/package/helper proofs are scoped;native entry remains unchecked.

October 14: [baseline](../../artifacts/rogue-runtime-checks/baseline.json) reproduces 16 defects across 35 actual-handler checks; [fixed](../../artifacts/rogue-runtime-checks/fixed.json) passes all 35. [Actual host](../../artifacts/rogue-runtime-checks/host/exit.json) exits 0 after 10.25 seconds, zero logged exceptions/clients, unchanged originals; invalid routing configuration exits 2 before readiness. Details: [dated log](2026-10-14.md). No baseline movement/combat simulations or native UI launches.

## Reconciliation rules

Update current stages and evidence with code changes. Master/catalog/projection IDs/checkmarks must match. Completed work leaves active queues and remains in history/CHANGELOG. Documentation-only checks do not launch builds, GUI or simulations.
