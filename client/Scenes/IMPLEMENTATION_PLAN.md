# Client scenes implementation plan

Reviewed October 13, 2026. Master: [M0 structure](../../ROGUE_TRADER_2_PLAN.md#milestone-0--repository-and-historical-research), [M1 launch](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). Reconcile with the [client plan](../IMPLEMENTATION_PLAN.md).

## Purpose and accomplished work

[Main.tscn](Main.tscn) is the only source scene. Its FirstContact Node2D attaches GameClient; view, camera, HUD and inspector are constructed in C#. project.godot selects this scene. Historical prototype launch/export and current RT-F373 original viewer launch/resource/render checks passed; [standalone log](../../artifacts/qa/OriginalContent/standalone-ships.log). This is an engineering scene, not the original Rogue Trader arena.

## Active outstanding work

-   [ ] **Investigating — production engine decision:** determine whether Godot scenes are needed for the final recreation after native client/server entry is proved.
-   [ ] **Not started — original playable level/interaction presentation if using Godot:** RT-F021/RT-F022 and M2; validate native LVL/LIO semantics before scene assembly.

No additional scene work is required for the completed paused inspector scope. Generated/imported editor resources are not source milestones; no launch or simulation ran for this documentation review.
