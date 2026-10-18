# Automated build implementation plan

Master authority: [§13.1 architecture](../../ROGUE_TRADER_2_PLAN.md#131-initial-architecture), [§18 M0 automated build](../../ROGUE_TRADER_2_PLAN.md#milestone-0--repository-and-historical-research), [M1 acceptance](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype) and [§1.3 verification boundaries](../../ROGUE_TRADER_2_PLAN.md#13-continued-exact-reproduction--october132026). Original acceptance IDs: [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md). Reconciled October 13, 2026 with [progress](../../docs/development-log/PROGRESS.md). The master remains the status authority.

## Completion history and evidence

| Scoped feature | Current status | Evidence and limit |
|---|---|---|
| Windows workflow definition with pinned portable tools | Complete with evidence | [build.yml](build.yml) bootstraps verified tools, builds/tests/smokes the client, exports Windows and uploads zip/TRX evidence; [toolchain lock](../../toolchain.lock.json) pins tools/checksums. |
| Prior local M0/M1 command acceptance | Complete with evidence | [Acceptance record](../../docs/development-log/PROGRESS.md#final-validation-evidence): 41 tests, three native-input/UI and six world phases in source/export plus rendered/export QA. These were prior local runs, not a hosted CI result or new documentation run. |

## Active outstanding work

- [ ] **Not started — hosted CI acceptance:** observe a successful GitHub-hosted run after a remote is configured and the workflow is pushed. Read-only reconciliation found no repository remote. Record run URL/revision and downloadable artifact before claiming hosted success.
- [ ] **Not started — fidelity CI coverage:** add appropriate focused checks for the now-completed importer/viewer scopes and independently verify the exported feature. Existing workflow inspection establishes only the provisional First Contact sequence; RT-F069 dependency closure, RT-F074 native blits and RT-F075 state selection remain open.

Preserve unchanged originals with separately derived outputs. [Field contracts](../../docs/original-research/FIELD_INTERPRETATION.md) track unresolved units/sentinels/native evaluators; CI counts cannot resolve them. NEW proposals stay deferred until exact reproduction. Do not rerun baseline simulations for documentation/research changes or run concurrent builds against shared outputs.

## Current reconciliation — October 13, 2026

Documentation-only source/saved-log review; no new build/test/GUI/simulation. Master registry: **394 IDs, 15 narrowly completed scopes, 379 open**. Completed prototype work above is history; original parity remains open unless its exact catalog acceptance passed.

Separate compatibility evidence has advanced beyond parser-only: RT-F381/F382 package/runtime-source audits, RT-F383 actual original arena initialization, RT-F385 exact eight-value parser regression, and RT-F392/F394 full-asset loopback host/authentic shared Soccer are complete. [Runtime plan](../../docs/architecture/ROGUE_RUNTIME_PLAN.md) and [package plan](../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) carry those proofs. This does not implement production services or tests in reserved folders.

The native client reached Welcome FreeInfantry; Start produced no confirmed entry. Native asset delivery, actual original gameplay and durable progression remain unverified. Standalone compatibility mode does not save progression. Owned test client/backend/private namespace were removed; no desktop interaction while the user is playing. [Native plan](../../docs/architecture/NATIVE_CLIENT_PLAN.md).

Next remains the unchecked per-feature queue in this document, using original fowlmouth records unchanged. Completed original viewer/package/input scopes RT-F020/F073/F373/F374 are historical proof, not native original appearance. RT-F375 all-archive inventory is complete; RT-F069 runtime dependency closure and Portal/logo/numeric Ball/item66 eligibility remain open. Original units/evaluators/bank/blit rules and shop/proximity/null-script bugs remain unresolved. NEW prototype tuning and modern expansion proposals cannot replace original parity.

