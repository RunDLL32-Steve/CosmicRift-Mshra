Review status: UNREVIEWED

# cloud/rt-f050-class-change-prune

- Claims: RT-F050 (data clause only: the three [Arena] CFG flags); gate items 2 and 5 (data). No queue row.
- Stacked on cloud/rt-f046-c2-cfg-terrain-access (PR #15, provides OriginalCfg).
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): pinned rogue.cfg [Arena] DeleteWeaponsOnClassChange=1 (line 72), DeathForceUtilityOff=1 (line 75), TeamSwitchMinEnergy=500000 (line 83), matching the catalog rows; independent grep/awk audit confirms the section and values. Missing keys raise instead of defaulting.
Remaining: native class/spec/team event order, units of TeamSwitchMinEnergy, NWS 218-220 spectator loss list (unrefined/wreckage/unused nanobots; also CFG Hold1/Hold2 WipeInv lists, not yet modeled), permitted transitions, inventory/score/utility/cash handling, native verification.

## Files
shared/RogueTrader.OriginalData/OriginalCfg.cs (record appended), tests/Unit/OriginalClassChangePruneTests.cs, folder plan.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 217, Failed 0, Skipped 0. Without assets: Passed 189, Skipped 15, Failed 0.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
CFG [Hold1]/[Hold2] WipeInv lists are an obvious follow-up for RT-F050 (spectate loss list); RT-F051 (Bounty/Cash/Point) next.
