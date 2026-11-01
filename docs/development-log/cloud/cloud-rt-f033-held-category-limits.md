Review status: APPROVED (senior review round 2, PR #19, head cbffec5)

# cloud/rt-f033-held-category-limits

-   Claims: RT-F033 (data clauses only: CFG [HeldCategory] Limit/ExtendedLimit rows and [Soul] UtilitySlots keys, raw); gate items 2 and 5 (data). No queue row.
-   Stacked on cloud/rt-f046-c2-cfg-terrain-access (PR #15, review-pending; provides OriginalCfg). Merge that first.
-   Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses

Done (data): raw typed read of rogue.cfg [HeldCategory] (30 Limit/ExtendedLimit pairs, lines 2967-3026; Limit1=500, Limit4..14=1, Limit15=2/Ext50, Limit16=2/Ext25, Limit17=2/Ext50, Limit20=2/Ext35, others 1 or -1) and [Soul] UtilitySlots (10 default, 5 range keys all 10), checked against an independent read of the pinned file.
Remaining: mapping of category ids 1..30 to ITM categories is NOT decided. Candidate evidence (reviewer observation, not proof): ITM f[20] is an integer constant per category for almost every category and lines up with the HeldCategory ids (Cannon 5, Beam 5/6, Energy Bomb 6, Special Weapons 6, Defensive 4, Space Mines 7, Rocket/Missile Systems 9, Hull Armor 11, Power Mods 12, Thruster Mods 13, Engine Mods 14, HyperDrive 18, Computer Mods 22; Resources Tellerium/Refined 15, Wreckage 16, Krandazite/Refined 17, Iron Ore/Alloy 20); only the cargo-resource ids 15/16/17/20 carry ExtendedLimit (50/25/50/35) and Limit11..14 = 1 match one armor/power/thruster/engine mod, ExtendedLimit semantics, NWS rows 33-34, purchase/install/use restriction enforcement, native verification.

## Files

shared/RogueTrader.OriginalData/OriginalHeldCategory.cs, tests/Unit/OriginalHeldCategoryTests.cs, shared/RogueTrader.OriginalData/plan.d/rt-f033.md.

## Verified (cloud)

-   apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 248, Failed 0, Skipped 0.
-   python scripts/check-trackers.py -> TRACKERS_CONSISTENT.

## Not verified in cloud - local CI required

-   Pinned SDK 8.0.419 run; native behaviour.

## Resume point

Find evidence for the category id order (e.g. other arena CFG/NWS text or shop UI strings) before any enforcement logic.

## Rework after round 1 follow-up (merge gate: conflict with main after #15 merged)

-   Merged origin/main (merge commit, no rebase); auto-merge resolved cleanly. OriginalCfg.cs, OriginalCfgTests.cs, TASK_QUEUE.md, IMPLEMENTATION_PLAN.md and the #15 log are identical to main; three-dot diff vs main is only OriginalHeldCategory.cs, OriginalHeldCategoryTests.cs, plan.d/rt-f033.md and this log.
-   Major NEEDS BASE FIX: cleared (#15 merged, PR retargeted to main). Minor (ITM f[20] wording): already fixed in 23a2419.
-   Tests after the merge (NuGet is blocked by this run's egress proxy, so real xunit could not restore): apt dotnet-sdk-8.0 8.0.131 compiled shared/RogueTrader.OriginalData plus tests/Unit/OriginalAssetsFixture.cs, Original\*Tests.cs and RawRecordReaderTests.cs against an out-of-tree xunit-API shim run by reflection; pinned assets eb1514ea: 123 passed/0 failed/0 skipped; without assets 42 passed/53 skipped/0 failed. `python scripts/check-trackers.py`: TRACKERS_CONSISTENT. Not verified in cloud - local CI required: real xunit on pinned SDK 8.0.419 and the Simulation/Persistence test projects (unchanged here).
