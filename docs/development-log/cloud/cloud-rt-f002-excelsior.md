Review status: UNREVIEWED

# cloud/rt-f002-excelsior

- Claims: RT-F002 (Excelsior hull, HULL profile; data/reader clauses only). No TASK_QUEUE row exists for RT-F002 (nothing to mark CLAIMED). Base: origin/main 1ce1d21.
- Stacked on: none. The local branch cloud/rt-f001-asp-hull-oracle carries no commits of its own (points at an old main commit, no remote branch, no PR), so there was no F001 work to reuse; the generic OriginalHullCatalog (G2, on main) and its existing RT-F001..F012 table test are reused unchanged.
- Status: done for the scoped data part. No checkbox ticked.

## Acceptance clauses (HULL profile)
Done (data): Excelsior RPG row 2 (v6, 51 fields; skill 15, vehicle 2011, "Ship Hull - Terran") linked to VEH row 28 (v53, 454 fields; hull f[6]=50, weight f[7]=10000, cargo f[47..48]=1500000/3000000, drop f[68..69]=2008x1, energy f[94..96]=1700/1050/1700, bounce f[362]=727, hyper cost f[373]=1100, terrain 0 f[202..211]=0,0,45,1380,3575,22,3800,50,-1,-1, roll TerranShipsSmall/Excelsior); verified against an independent comma-split read of the pinned files. Also pinned the complete set of 10 VEH fields where Excelsior differs from Asp (incl. raw f[367]=11, f[368]=14, f[413]=0, meaning unknown).
Remaining (not claimed): sprite/effect/sound record resolution (BLO, queue C1/G11), directional rendering, validation of energy/movement/terrain/installation/loadout fields against behavior, selected-revision behavior, native hull acceptance.

## Files
- tests/Unit/OriginalExcelsiorHullTests.cs (new, 4 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f002.md
- this log

## Verified (local, real assets)
- Pinned SDK 8.0.419, RT_ORIGINAL_ASSETS_ROOT = .tools/references/cosmic-rift-assets (pinned eb1514ea): dotnet test tests/Unit --filter OriginalExcelsiorHullTests -> Passed 4, Failed 0, Skipped 0.
- dotnet test tests/Unit (full) -> Passed 761, Failed 0, Skipped 5, Total 766 (the 5 skips are OriginalKeyListing tests, unrelated).
- python scripts/check-trackers.py -> TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).
## Not verified
- CI result; Godot smoke skipped (no client/ change); native behavior and everything under Remaining.

## Resume point
Next hull: RT-F003 Ravager with the same pattern (diff vs Asp via the same audit).
