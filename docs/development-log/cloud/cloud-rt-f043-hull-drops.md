Review status: UNREVIEWED

# cloud/rt-f043-hull-drops

- Claims: RT-F043 (data clause only: hull VEH DropItemId/DropQuantity f[68..69]); gate items 2 and 4/5 (data). No queue row.
- Stacked on cloud/rt-f041-armor-records (UNREVIEWED) <- cloud/rt-f039-stun-emp-fields (UNREVIEWED) <- APPROVED cloud/rt-f040-mirv-launcher-chain and the G3/G4/G5 branches. Merge in that order. The change itself only needs main plus the G3 ITM catalog (for resolving the item id). It edits OriginalHull.cs (main code) and appends to no shared plan.
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): independent Python audit of pinned Rogue.veh: all 12 playable hulls (VEH ids 2000-2012) carry f[68]=2008, f[69]=1; 2008 resolves to ITM row 87 "Wreckage". Same audit of the 18 non-hull VEH rows (not loaded yet, G7): actors 401,403,405,410,411,413,414 drop 2008x1; 400,402,404,407,408,409,412 and 2030 drop 0x0; Manufactory 425/426/427 carry f[68]=66, f[69]=1 and ITM has no item 66 (unresolved reference, to be examined with G7/RT-F057).
Remaining: older-vs-newer NWS drop conflicts (rows 301,338-340 vs 35, 41), carried-versus-produced wreck quantity, death/spec/team-change order, pickup lifecycle, simulation and native verification.

## Files
shared/RogueTrader.OriginalData/OriginalHull.cs (DropItemId, DropQuantity), tests/Unit/OriginalHullDropTests.cs.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 with global.json temporarily moved aside (restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 250, Failed 0, Skipped 0 (248 + 2 new).
- python scripts/check-trackers.py: TRACKERS_CONSISTENT.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
Next: RT-F047/F048/F049 item-table audits (repair items rows 149/150/162; upgrade families; f[8]/f[15] sell sentinels), or G7 non-hull VEH loader.
