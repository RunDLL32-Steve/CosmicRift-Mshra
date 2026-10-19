# Original inspector input implementation plan

Master reconciliation: [§1.3 fidelity checkpoint and §18 RT-F020/RT-F073/RT-F374](../ROGUE_TRADER_2_PLAN.md). Existing scoped display completions remain historical. This queue covers viewer navigation only; native appearance/controls/gameplay parity remain unchecked.

Current stage: **Complete** - RT-F374 passed source and standalone verification. Native original appearance/runtime parity remains Deferred.

## Active outstanding work

-   [ ] Deferred - actual original native banking, blit/palette/HSV/shadow/timing/audio/HUD parity (RT-F023-026/F074-075).

## Source completion history - October 13, 2026

-   [x] Twelve hulls, all 64 directions/all 11 preview states and valid atlas regions (8,448 hull frame selections).
-   [x] All 231 sprite selectors and last-frame selection through actual widget signals.
-   [x] Independent hull and per-sprite frame choices preserved across map/page/member changes.
-   [x] Native focused SpinBox Enter commits row 31; F2/Escape and Close button; persisted F3/P remap open/close; preexisting controls restored.
-   [x] World tick remains 0 with no reset/gameplay; pause flag and actual controls menu preserved after closing.

-   [x] Updated standalone export repeats all three input phases and final pass with world tick 0 unchanged; exit 0 with SDK/runtime discovery disabled.

Root-owned Debug source build passed with zero warnings/errors. Native engine input harness printed all three `ORIGINAL_INPUT_PHASE_PASS` lines and final `ORIGINAL_INPUT_PASS`, exit 0. Evidence: [standalone input log](../artifacts/qa/OriginalContent/standalone-input.log) and [source verification record](../artifacts/qa/original-input-source-record.json). Root confirmed standalone exit 0 with `DOTNET_ROOT` pointing to an absent directory and `PATH` restricted to Windows. Three standalone resource pages also pass at tick 0; root inspected their rendered captures. Root reconciles the master/catalog/changelog. No baseline gameplay simulation ran.

## Implementation and boundaries

`UI/OriginalContentInspector.cs` retains frame selections separately for ships and each archive member; map navigation leaves hidden bounds untouched. `Scripts/OriginalContentInputCheck.cs` drives native input and actual UI signals with gameplay already paused, preserving/restoring control settings. Root-owned `GameClient` exposes `--verify-original-input` and routes ordinary inspector keys into focused GUI controls; physical Escape and mapped close actions remain intercepted.

This verifies the derived viewer controls. It does not prove original native banking, original gameplay controls or production engine parity. Column 5 remains a preview choice.

## Source audit — October 17, 2026

Current code and remaining integration limits are recorded in the [source documentation audit](../docs/audits/2026-10-17-source-docs.md). Earlier dated test counts above are historical branch results, not a current-tree test run. No build, runtime check, GUI or simulation was run for this documentation audit. Global feature counts and completion status belong to [master §18](../ROGUE_TRADER_2_PLAN.md#18-development-roadmap).
