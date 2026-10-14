# Client UI implementation plan

Reviewed October 13, 2026; documentation inspection only. Master: [§15 interfaces](../../ROGUE_TRADER_2_PLAN.md#15-user-interface), [M1 fidelity gate/registry](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). Reconcile with the [client plan](../IMPLEMENTATION_PLAN.md), [inspector completion](../ORIGINAL_INSPECTOR_PLAN.md) and [catalog](../../docs/original-research/FEATURE_CATALOG.md).

## Purpose and completed history

[FlightHud.cs](FlightHud.cs) constructs provisional flight telemetry, target objectives, pause/resume/reset/quit, the persisted controls editor and inspector entry. It does not implement native shops, inventory, ship selection or a gameplay connection screen.

[OriginalContentInspector.cs](OriginalContentInspector.cs) constructs paused hull/sprite/map pages and OriginalAssetCanvas. Twelve hulls share explicit direction/column selection; 231 archive members retain individual row/column choices. Map navigation does not clamp hidden frame controls. Numeric editing, selector/button signals, remapped open/close, physical Escape and pause/input isolation passed RT-F374. The default hull row 16/column 5 is a preview.

Evidence: [source input record](../../artifacts/qa/original-input-source-record.json), [standalone input log](../../artifacts/qa/OriginalContent/standalone-input.log) and [three rendered package pages](../../artifacts/qa/OriginalContent/standalone-map.log), all at world tick 0. Scoped RT-F020/F073/F373/F374 are complete; no new run occurred in this review.

## Active outstanding work

- [ ] **Investigating — RT-F025/RT-F026:** native HUD/store/selection artwork layout, original keyboard modifiers/actions and actual resolution behavior. Rendering archived UI pixels in an inspector does not integrate original UI.
- [ ] **Investigating — RT-F023/RT-F074/RT-F075:** native color/shadow/blit/state choices before exact appearance acceptance.
- [ ] **Not started — M2 interfaces through the selected engine:** actual original cargo, refinery/trading, repairs, upgrades and progression.
- [ ] **Not started — M3/M6 acceptance:** verified native connection/game entry, supported resolutions/UI scaling/accessibility where required by the master.

Presentation widgets do not own gameplay transactions/state. Preserve original categories, names and pixels; optional modern interfaces follow exact reproduction. Completed viewer navigation is history, not an active task.
