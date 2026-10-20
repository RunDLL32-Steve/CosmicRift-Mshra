Review status: APPROVED (senior review round 2, PR #24)

# cloud/rt-f027-f028-flight-fields — RT-F027..F028 per-hull flight, strafe, hyper and reverse raw-field audit

-   Claims: RT-F027 (momentum/thrust/rotation), RT-F028 (strafe, hyper thrust and reverse defaults). Gate items 2 (field values/sentinels evidence) and 6 (flight evidence), data clauses only. Base: origin/main, not stacked.
-   Status: done for the data-audit scope; RT-F027 and RT-F028 stay UNCHECKED (acceptance needs native flight verification).

## Acceptance clauses

Done:

-   Independent audit script `scripts/audit-flight-fields.py` (read-only csv reader) over 12 hulls x 16 terrain blocks x 10 raw fields, active-block list, distinct shapes, negative-value inventory by block position, f[373].
-   Raw C# view `OriginalFlightProfile` (shared/RogueTrader.OriginalData) with source field indexes; no units/scaling/fallback resolution.
-   Oracle tests `tests/Unit/OriginalFlightProfileTests.cs` (RequiresOriginalAssets\* attributes).
-   Catalog rows rt-f027/rt-f028 audit notes; plan.d entry.

Remaining (open, unchecked):

-   Native units, timestep, friction/thrust/rotation/speed clamp, terrain-override semantics (RT-F027).
-   Native meaning of the -1 reverse values (fallback vs disabled vs derived), lateral thrust, hyper energy cost/timing and permitted states (RT-F028).
-   Exact forward/reverse/hyper/strafe interaction verification after a flight feature exists.

## Files

-   scripts/audit-flight-fields.py (new)
-   shared/RogueTrader.OriginalData/OriginalFlightProfile.cs (new)
-   shared/RogueTrader.OriginalData/plan.d/rt-f027-f028.md (new)
-   tests/Unit/OriginalFlightProfileTests.cs (new)
-   docs/original-research/FEATURE_CATALOG.md (rows rt-f027, rt-f028 text only)
-   this log

## Verified results

-   `python3 scripts/audit-flight-fields.py <assets>` (assets pinned eb1514ea) -> `FLIGHT_FIELDS_OK 12 hulls 48 nonzero-blocks`.
    -   All 192 blocks (12 x 16) end with `-1,-1` (positions 8 and 9); these are the only negative values in f[202..361] (384 values, 32 per hull); no negative f[373].
    -   144 blocks are only `0,0,0,0,0,0,0,0,-1,-1`; 48 are active (terrains 0, 9, 10, 15 for every hull) in 12 distinct shapes.
    -   Terrains 9 and 10: `1,45,45,1000,3000,15,0,0,-1,-1` for all hulls. Terrain 15: `1,30,45,1300,3000,15,0,0,-1,-1`, except Hornet `1,30,50,1656,3000,15,0,0,-1,-1`.
    -   Terrain 0 strafe = 22 for all hulls; hyper top speed 3500/3600/3750/3800 and hyper thrust 50/55/60 by hull; f[362] = 727 for all hulls.
    -   f[373]: 1100 for Asp, Excelsior, Ravager, Hornet, Rak'Kir, Rip'Pir, Zap'Pir, Zag'Gir; 990 for Skithist, Tarthist, Kragath; 1200 for Mastakith.
-   Test harness (xunit-API shim, compiles shared/RogueTrader.OriginalData + tests/Unit/Original\*Tests.cs): with assets `Passed 145, Failed 0, Skipped 0`; without assets `Passed 40, Failed 0, Skipped 55`.
-   `python3 scripts/check-trackers.py` -> TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).

## Not verified in cloud - local CI required

-   Real `dotnet test` with xunit 2.9.3 and the repo build (NuGet is blocked in the cloud; a shim harness was used).
-   Native client flight behaviour, any simulation, and every operational meaning listed under Remaining.

## Resume point

Next: native evidence for units/timestep and the -1 reverse semantics (RT-F027/RT-F028 acceptance), then a flight feature reading these raw fields, verified against the client.

## Rework after round 1 (merge gate: conflict with main after #25/#26)

-   Merged origin/main (merge commit, no rebase/force-push). Only conflict: FEATURE_CATALOG.md rows rt-f027..rt-f029 (adjacent lines). Resolved by keeping this branch's rt-f027/rt-f028 rows and main's rt-f029 row (from #25) unchanged; no other file changed by the merge. Three-dot diff vs main is the same six files as the approved head a1b964e.
-   Nit (NonZeroTerrainIndexes): not carried; a bare base merge carries no nits (it rides the next code push to OriginalFlightProfile.cs, if any).
-   Tests after the merge (xunit-API shim, apt SDK 8.0.131; OriginalData tests incl. #25/#26 now on main): with pinned assets 164 passed / 0 failed / 0 skipped; without assets 47 passed / 67 skipped / 0 failed. check-trackers: TRACKERS_CONSISTENT.
