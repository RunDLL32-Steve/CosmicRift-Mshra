# Client shaders implementation plan

Reviewed October 13, 2026. Master: [§16 original appearance](../../ROGUE_TRADER_2_PLAN.md#16-graphical-direction), [M6 graphics](../../ROGUE_TRADER_2_PLAN.md#milestone-6--release-preparation); original fidelity RT-F074 in the [catalog](../../docs/original-research/FEATURE_CATALOG.md). Reconcile with the [client plan](../IMPLEMENTATION_PLAN.md).

## Purpose and current status

**Not started.** This source folder is reserved and has no shader implementation. Godot currently uses CanvasItem drawing, PNG atlas regions and nearest filtering. Those derived RGB/RGBA previews do not reproduce all native palette/HSV/shadow/blit behavior.

## Active outstanding work

-   [ ] **Investigating — RT-F074:** original renderer semantics and supported modes through the compatible native engine.
-   [ ] **Not started — faithful render adaptation if needed by engine choice:** implement only proved behavior and verify original pixel/color treatment.
-   [ ] **Not started — optional modern effects:** deferred until exact reproduction acceptance; no effect is required merely to populate this folder.

No shader feature is complete. Native wrapper assets live separately in ignored compatibility tools and are not a Godot shader implementation. No rendering/build/simulation ran for this documentation review.
