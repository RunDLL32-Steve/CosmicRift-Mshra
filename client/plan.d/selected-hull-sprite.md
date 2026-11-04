# Selected hull sprite correction

Authority: [master §6 hull identities](../../ROGUE_TRADER_2_PLAN.md#6-spacecraft-and-loadout-system), [§16 appearance](../../ROGUE_TRADER_2_PLAN.md#16-graphical-direction) and [§18 M1 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). This is a testbed rendering bug fix; RT-F001–F012/F023/F075 and all milestone acceptance boxes remain unchanged, 397/22/375.

-   [x] Resolve player sprite from the ship definition's canonical vehicle ID and derived manifest, preserving source atlas dimensions, direction row and preview column 5.
-   [x] Preserve provisional Hornet practice-target artwork and geometric fallback for absent originals or unmapped/legacy hulls.
-   [x] Verify all twelve selected player atlas bindings, Hangar switch, reload/reopen and fallback cases with an isolated disposable profile at world tick 0; reproduce the old Asp binding failure.
-   [ ] Independent exact-head review and publication/merge gates.
-   [ ] Native hull banking, state, palette/shadow and complete original hull/gameplay parity.

Source and focused reproduction: [dated evidence](../../docs/development-log/2026-11-04-selected-hull-sprite.md). `--verify-hull-sprites` requires staged/imported canonical-derived content and always overrides any profile directory with a new smoke profile; exit removes that fixture. It checks the actual texture resource path bound immediately before the player draw call, without flight/combat simulation.
