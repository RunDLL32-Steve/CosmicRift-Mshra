Review status: APPROVED (senior review round 2, PR #16, head 514441d)

# cloud/rt-f050-class-change-prune

-   Claims: RT-F050 (data clause only: the three [Arena] CFG flags); gate items 2 and 5 (data). No queue row.
-   Stacked on cloud/rt-f046-c2-cfg-terrain-access (PR #15, provides OriginalCfg).
-   Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses

Done (data): pinned rogue.cfg [Arena] DeleteWeaponsOnClassChange=1 (line 72), DeathForceUtilityOff=1 (line 75), TeamSwitchMinEnergy=500000 (line 83), matching the catalog rows; independent grep/awk audit confirms the section and values. Missing keys raise instead of defaulting.
Remaining: native class/spec/team event order, units of TeamSwitchMinEnergy, NWS 218-220 spectator loss list (unrefined/wreckage/unused nanobots; also CFG Hold1/Hold2 WipeInv lists, not yet modeled), permitted transitions, inventory/score/utility/cash handling, native verification.

## Files

shared/RogueTrader.OriginalData/OriginalCfg.cs (record appended), tests/Unit/OriginalClassChangePruneTests.cs, shared/RogueTrader.OriginalData/plan.d/rt-f050.md.

## Verified (cloud)

-   apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 217, Failed 0, Skipped 0. Without assets: Passed 189, Skipped 15, Failed 0.

## Not verified in cloud - local CI required

-   Pinned SDK 8.0.419 run.

## Resume point

CFG [Hold1]/[Hold2] WipeInv lists are an obvious follow-up for RT-F050 (spectate loss list); RT-F051 (Bounty/Cash/Point) next.

## Rework after round 1 (merge gate: conflict with main)

-   Merged origin/main (merge commit, no rebase). Conflict in shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md resolved by taking main's file unchanged and moving the RT-F050 status line into shared/RogueTrader.OriginalData/plan.d/rt-f050.md (AGENTS.md "One file per item"). OriginalCfg.cs = main's version plus only the `OriginalClassChangePrune` record. Three-dot diff vs main: OriginalCfg.cs, OriginalClassChangePruneTests.cs, plan.d/rt-f050.md and this log.
-   Minor (plan line in shared IMPLEMENTATION_PLAN.md): fixed by the move above.
-   Tests after the merge (NuGet is blocked by this run's egress proxy, so real xunit could not restore): apt dotnet-sdk-8.0 8.0.131 compiled shared/RogueTrader.OriginalData plus tests/Unit/OriginalAssetsFixture.cs, Original\*Tests.cs and RawRecordReaderTests.cs against an out-of-tree xunit-API shim run by reflection; pinned assets eb1514ea: 121 passed/0 failed/0 skipped; without assets 41 passed/52 skipped/0 failed. `python scripts/check-trackers.py`: TRACKERS_CONSISTENT. Not verified in cloud - local CI required: real xunit on pinned SDK 8.0.419 and the Simulation/Persistence test projects (unchanged here).
