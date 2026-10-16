Review status: APPROVED (PR #9 review round 2, senior review 5545490028 on head 2da5ca0; label review-approved)

# cloud/rt-f043-hull-drops

- Claims: RT-F043 (data clause only: hull VEH DropItemId/DropQuantity f[68..69]); gate items 2 and 4/5 (data). No queue row.
- Stacked on cloud/rt-f041-armor-records (UNREVIEWED) <- cloud/rt-f039-stun-emp-fields (UNREVIEWED) <- APPROVED cloud/rt-f040-mirv-launcher-chain and the G3/G4/G5 branches. Merge in that order. The change itself only needs main plus the G3 ITM catalog (for resolving the item id). It edits OriginalHull.cs (main code) and appends to no shared plan.
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): independent Python audit of pinned Rogue.veh: all 12 type-2 hulls (VEH ids 2000-2006, 2008-2012; 2007 does not exist) carry f[68]=2008, f[69]=1; 2008 resolves to ITM row 87 "Wreckage". Same audit of the 18 non-hull VEH rows (not loaded yet, G7): actors 401,403,405,410,411,413,414 drop 2008x1; 400,402,404,407,408,409,412 and 2030 drop 0x0; Manufactory 425/426/427 carry f[68]=66, f[69]=1 and ITM has no item 66 (unresolved reference, to be examined with G7/RT-F057).
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

## Review round 1 (senior review)

Scope understood: RT-F043 data clause only. Expose VEH f[68] DropItemId and f[69] DropQuantity on OriginalHull and check that every playable hull drops item 2008 (Wreckage, ITM row 87) x1. The NWS conflicts and the death/pickup lifecycle remain open. Stacked on cloud/rt-f041-armor-records.

Verdict: CHANGES REQUESTED. The branch's own content would be approvable (no own blocker or major). It cannot be approved because its stacked base, cloud/rt-f041-armor-records, is CHANGES REQUESTED in this run: NEEDS BASE FIX.

Findings:
1. blocker (stack) - docs/development-log/cloud/cloud-rt-f043-hull-drops.md:6. This branch contains the base's commits, and the base has an open major (the f[42..44] armor interpretation). Expected fix: after cloud/rt-f041-armor-records is reworked and pushed, merge it into this branch with a normal merge (no rebase or force-push). Then update the "Stacked on" line, fix the minors below and set READY FOR RE-REVIEW.
2. minor - shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md was not updated. The log says "appends to no shared plan", but AGENTS.md requires each affected folder's IMPLEMENTATION_PLAN.md to be maintained, and this branch adds public fields to OriginalHull in that folder. Expected fix: one line, like the F039/F041 lines, stating the DropItemId/DropQuantity exposure, the audit result and that RT-F043 stays open.
3. nit - shared/RogueTrader.OriginalData/OriginalHull.cs:27 and :132 say "dropped on death". That is the catalog's lifecycle interpretation, which this branch leaves unverified (the death/spec/team-change order is listed as remaining). Expected fix: "catalog DropItemId/DropQuantity; drop trigger unvalidated (RT-F043)".
4. nit - the log says "all 12 type-2 hulls (VEH ids 2000-2006, 2008-2012; 2007 does not exist)". The hull ids are 2000-2006 and 2008-2012; 2007 does not exist. Expected fix: list the ids, or say "12 type-2 rows".

Verified independently: the VEH audit matches the log exactly. Rows 18-29 are the 12 type-2 hulls, all with 2008,1 resolving to ITM row 87 Wreckage. Actors 401, 403, 405, 410, 411, 413 and 414 drop 2008x1. Actors 400, 402, 404, 407, 408, 409 and 412, and 2030 Spectator Mode, drop 0x0. The Manufactories 425, 426 and 427 carry 66,1, and ITM has no id 66.

Branch name, claims and collisions: the name carries RT-F043 and the log claims RT-F043. No other branch names RT-F043. claim/g11-hull-reference-resolution (local) resolves hull references to BLO members and does not claim f[68..69]; no collision. No queue row.

Verified in cloud (this review):
- apt dotnet-sdk-8.0 8.0.131 in a detached scratch worktree, global.json moved aside and restored (not committed), RT_ORIGINAL_ASSETS_ROOT = pinned assets eb1514ea: dotnet test tests/Unit -> Passed 250, Failed 0, Skipped 0. This matches the log.
- python scripts/check-trackers.py -> TRACKERS_CONSISTENT. No canonical asset, .tools/references, secret or unrelated changes.

Not verified (local CI required): a run with the pinned SDK 8.0.419; the NWS revision choice, wreck quantity semantics and simulation/native lifecycle.

## Rework after round 1
1. blocker (stack base) - fixed: merged the reworked cloud/rt-f041-armor-records (commit 7c5ae99, round 1 major fixed) into this branch with a normal merge; Stacked on cloud/rt-f041-armor-records (READY FOR RE-REVIEW).
2. minor (folder plan) - fixed: IMPLEMENTATION_PLAN.md line added.
3. nit ("dropped on death") - fixed: both OriginalHull.cs summaries now say the drop trigger is unvalidated.
4. nit (hull ids) - fixed in the log: ids 2000-2006, 2008-2012.
Verified in cloud: dotnet-sdk 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 252, Failed 0, Skipped 0; check-trackers -> TRACKERS_CONSISTENT. Not verified: pinned SDK 8.0.419.

## Conflict merge 2026-10-16

- Merged base tip 82d285c (origin/cloud/rt-f041-armor-records, updated after its own base conflict merge).
- Conflict: shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md (this branch had appended its RT-F043 status line there; the base moved the shared lines to plan.d and no longer carries that section).
- Resolution: kept the base's IMPLEMENTATION_PLAN.md unchanged; moved the RT-F043 status line to shared/RogueTrader.OriginalData/plan.d/rt-f043.md. No other conflicts or shared-tail appends.
- Tests: Passed 268, Failed 0, Skipped 0, Total 268 (offline xunit-compatible shim runner, SDK 8.0.131; pinned SDK 8.0.419 / real xunit not verified in cloud - local CI required).
- python3 scripts/check-trackers.py: TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).
