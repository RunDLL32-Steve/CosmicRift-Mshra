# Original inspector input implementation plan

Master reconciliation: [§1.3 fidelity checkpoint and §18 RT-F020/RT-F073/RT-F374](../ROGUE_TRADER_2_PLAN.md). Existing scoped display completions remain historical. This queue covers viewer navigation only; native appearance/controls/gameplay parity remain unchecked.

Current stage: **Complete** - RT-F374 passed source and standalone verification. Native original appearance/runtime parity remains Deferred.

## Active outstanding work

- [ ] Deferred - actual original native banking, blit/palette/HSV/shadow/timing/audio/HUD parity (RT-F023-026/F074-075).

## Source completion history - October 13, 2026

- [x] Twelve hulls, all 64 directions/all 11 preview states and valid atlas regions (8,448 hull frame selections).
- [x] All 231 sprite selectors and last-frame selection through actual widget signals.
- [x] Independent hull and per-sprite frame choices preserved across map/page/member changes.
- [x] Native focused SpinBox Enter commits row 31; F2/Escape and Close button; persisted F3/P remap open/close; preexisting controls restored.
- [x] World tick remains 0 with no reset/gameplay; pause flag and actual controls menu preserved after closing.

- [x] Updated standalone export repeats all three input phases and final pass with world tick 0 unchanged; exit 0 with SDK/runtime discovery disabled.

Root-owned Debug source build passed with zero warnings/errors. Native engine input harness printed all three `ORIGINAL_INPUT_PHASE_PASS` lines and final `ORIGINAL_INPUT_PASS`, exit 0. Evidence: [standalone input log](../artifacts/qa/OriginalContent/standalone-input.log) and [source verification record](../artifacts/qa/original-input-source-record.json). Root confirmed standalone exit 0 with `DOTNET_ROOT` pointing to an absent directory and `PATH` restricted to Windows. Three standalone resource pages also pass at tick 0; root inspected their rendered captures. Root reconciles the master/catalog/changelog. No baseline gameplay simulation ran.

## Implementation and boundaries

`UI/OriginalContentInspector.cs` retains frame selections separately for ships and each archive member; map navigation leaves hidden bounds untouched. `Scripts/OriginalContentInputCheck.cs` drives native input and actual UI signals with gameplay already paused, preserving/restoring control settings. Root-owned `GameClient` exposes `--verify-original-input` and routes ordinary inspector keys into focused GUI controls; physical Escape and mapped close actions remain intercepted.

This verifies the derived viewer controls. It does not prove original native banking, original gameplay controls or production engine parity. Column 5 remains a preview choice.

## Current reconciliation — October 13, 2026

Documentation-only source/saved-log review; no new build/test/GUI/simulation. Master registry: **394 IDs, 15 narrowly completed scopes, 379 open**. Completed prototype work above is history; original parity remains open unless its exact catalog acceptance passed.

Separate compatibility evidence has advanced beyond parser-only: RT-F381/F382 package/runtime-source audits, RT-F383 actual original arena initialization, RT-F385 exact eight-value parser regression, and RT-F392/F394 full-asset loopback host/authentic shared Soccer are complete. [Runtime plan](../docs/architecture/ROGUE_RUNTIME_PLAN.md) and [package plan](../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) carry those proofs. This does not implement production services or tests in reserved folders.

The native client reached Welcome FreeInfantry; Start produced no confirmed entry. Native asset delivery, actual original gameplay and durable progression remain unverified. Standalone compatibility mode does not save progression. Owned test client/backend/private namespace were removed; no desktop interaction while the user is playing. [Native plan](../docs/architecture/NATIVE_CLIENT_PLAN.md).

Next remains the unchecked per-feature queue in this document, using original fowlmouth records unchanged. Completed original viewer/package/input scopes RT-F020/F073/F373/F374 are historical proof, not native original appearance. RT-F375 all-archive inventory is complete; RT-F069 runtime dependency closure and Portal/logo/numeric Ball/item66 eligibility remain open. Original units/evaluators/bank/blit rules and shop/proximity/null-script bugs remain unresolved. NEW prototype tuning and modern expansion proposals cannot replace original parity.

