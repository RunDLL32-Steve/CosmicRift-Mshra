Review status: APPROVED (PR #17 review round 2, senior review on head 8417375; label review-approved)

# cloud/rt-f047-repair-items

- Claims: RT-F047 (data clause only: ITM Minor/Major/Full Repair rows 149/150/162 raw effect field); gate item 2 (field audit, data only). No queue row.
- Stacked on cloud/rt-f013-f019-g3-g4-itm-resources (APPROVED; provides the ITM catalog). Not on main yet: merge in order.
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): independent audit of pinned Rogue_mp.itm: rows 149/150/162 (ids 39/40/43, 53 fields, type 1, category Resources) carry f[33] = 5/10/50, buy 0, sell 0; they are the only type-1 rows with non-zero f[33] (26 rows in the file have non-zero f[33], 23 of other types, e.g. Plasma Mine 1, Turbo Capacitor 150). Credit-effect rows 84/85/90/168 (ids 17/18/19/44) use f[31] = 80/55/300/20 with sell -1, f[33] = 0 (useful for RT-F045).
Evidence for the unit (meaning still not decided): pinned Rogue.veh row 7 (id 407 Repair Dock) has blocks "Repair 5 Hull Points" -> item 39, "Repair 10 Hull Points" -> item 40 (f+7 = 10/20) and "Remove Armor" -> item 41; Full Repair 43 has no dock block.
Remaining: meaning/units of the amount (HP, percent, fraction), recipe/cost, dock range (VEH 407, LIO 36-39,58), NWS row 181, presentation, simulation and native verification.

## Files
shared/RogueTrader.OriginalData/OriginalRepairItems.cs, tests/Unit/OriginalRepairItemTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)
- dotnet-sdk 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 225, Failed 0, Skipped 0.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run; everything under Remaining.

## Resume point
Next: RT-F048 upgrade-family audit (same loader) or RT-F044/F045 once the LIO/VEH actor loaders exist.

## Rework after round 1 / merge gate
Merged origin/main (merge commit); plan line moved to plan.d/rt-f047.md (IMPLEMENTATION_PLAN.md = main); log wording corrected and VEH 407 block evidence recorded. No code changed; dotnet test not re-run (docs/plan only). check-trackers: TRACKERS_CONSISTENT.
