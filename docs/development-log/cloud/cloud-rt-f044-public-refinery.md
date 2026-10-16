Review status: APPROVED (PR #13 review round 1, senior review 5545505602 on head 1e6847d; label review-approved)

# cloud/rt-f044-public-refinery

- Claims: RT-F044 (data clause only: Refinery VEH row 1 refine blocks); gate items 2 and 5 (data). No queue row.
- Stacked on cloud/rt-f045-trading-post-credits (PR #12, shares the VEH 14-field block layout) <- cloud/rt-f013-f019-g3-g4-itm-resources (PR #2).
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): independent Python audit of pinned Rogue.veh row 1 (id 400, "Refinery"): three 14-field blocks at f[253], f[267], f[281]: Refine Tellerium (output 2004 Refined Tellerium, input 2003), Refine Krandazite (2006 from 2005), Refine Iron Ore (output 2020 Alloy, input 2019 Iron Ore). f+4 = 150,150,0 kept raw. The catalog's NWS row 270 "wreckage -> Alloy" is not in the pinned VEH record (no input 2008): recorded as a data/revision mismatch, not resolved.
Remaining: meaning of f+4, input/output quantities and costs, radius and conditions, LIO 13/14 spawn of actor -400 (needs LIO reader), per-conversion inventory conservation, native verification.

## Files
shared/RogueTrader.OriginalData/OriginalRefinery.cs, tests/Unit/OriginalRefineryTests.cs, folder plan.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 227, Failed 0, Skipped 0. Without assets: Passed 190, Skipped 18, Failed 0.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
RT-F046 (SpaceDock store) and RT-F048 (upgrade families); both are mostly ITM/NWS/CFG data; RT-F047 is claimed by cloud/rt-f047-repair-items.
