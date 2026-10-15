Review status: APPROVED (PR #5 review round 2, senior review 5540408913 on head d6754da; label review-approved)

# cloud/rt-f038-mine-limits

- Claims: RT-F038 (data clause only: raw mine records and simultaneous-limit field audit). Gate items 2 and 5 (data). Stacked on origin/cloud/rt-f036-f037-g5-weapon-records (APPROVED; provides OriginalProjectile), itself on the APPROVED cloud/rt-f013-f019-g3-g4-itm-resources.
- Status: done for the scoped part. No registry box ticked.

## Acceptance clauses
Done (data): the RT-F038 text's seven mines and shared raw fields f[94]=0, f[101]=0, f[110]=6000, f[131]=3 verified against pinned Rogue_mp.itm; Shard Mine is the only one with an explode child (3011); Space Mines category membership; ClusterMine-Child row 54 raw values noted.
Remaining: placement/proximity/arming/expiry/owner/category-limit behaviour, units of 6000, acquisition, explosive child effects, simulation and native verification.

## Files
tests/Unit/OriginalMineRecordTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 (global.json moved aside temporarily, not committed), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 243, Failed 0, Skipped 0 (incl. 8 new).
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run; any mine behaviour.

## Resume point
Next: RT-F039 f[135..138] audit (Disruptor/Concussion/Disabler/Blackout Bomb) on the same OriginalProjectile view.

## Review round 1 (senior review)

Reviewer: senior review routine (did not author this work). Base origin/cloud/rt-f036-f037-g5-weapon-records is APPROVED (3f73342), itself on the APPROVED G3/G4 branch; diff reviewed against the base tip.

Scope understood: RT-F038 data clause only - oracle tests that the seven catalogued mines carry the catalog's raw values and the related category/child facts. Test-only plus one IMPLEMENTATION_PLAN line; no registry box ticked; Remaining (placement/proximity/arming/expiry/owner/category limits, units of 6000, acquisition, explosive child effects, simulation, native) listed. Branch name carries RT-F038, log lists it, no collision.

Verdict: APPROVED.

Findings:
1. nit - tests/Unit/OriginalMineRecordTests.cs:28: asserts through the base's `MaxLivePerPlayer` name for f[131], an interpretation of the "simultaneous limits" wording rather than a source-named field. Keep the log/IMPLEMENTATION_PLAN wording "raw f[131]=3" (as now) and do not treat the property name as evidence of the limit semantics.
2. nit - the work is a thin re-assertion of catalog values over an existing view; legitimate as the scoped data clause and honestly labelled, but it does not advance RT-F038 behaviour. Note also that 1080 Orion Booster (row 161, Defensive Weapons) carries f[131]=3 too, with f[110]=-50: worth recording in the f[131] evidence when the limit semantics are investigated.
3. nit - RT-F038 has no TASK_QUEUE row of its own (falls under G5); acceptable, the log states the mapping.

Verified by the reviewer (cloud):
- Independent Python audit of pinned Rogue_mp.itm (eb1514ea): rows 13/15/51/56/61/62/160 = ids 102/1044/1039/1056/1001/1005/1079 with f[94]=0, f[101]=0, f[110]=6000, f[131]=3; only 1056 has f[142]=3011; `Space Mines` = exactly the first six in row order; Repulsor is `Defensive Weapons`; 1055 ClusterMine-Child row 54 = f[101]=100, f[110]=2500, f[131]=2. All match the tests.
- apt dotnet-sdk-8.0 8.0.131, global.json removed in a scratch worktree only, RT_ORIGINAL_ASSETS_ROOT set: `dotnet test tests/Unit` -> Passed 243, Failed 0, Skipped 0 (matches the log).
- `python scripts/check-trackers.py` -> TRACKERS_CONSISTENT (397/22/375).
- No canonical asset, .tools/references, secret or unrelated changes.

Not verified in cloud - local CI required: pinned SDK 8.0.419 run; any mine behaviour.
