Review status: READY FOR RE-REVIEW

# cloud/rt-f036-f037-g5-weapon-records

- Claims: RT-F036, RT-F037 (data clauses only; also touches the RT-F038, RT-F039, RT-F040 raw fields but claims none of them); queue G5 (projectile field part only); gate items 2 and 5 (data). Stacked on origin/cloud/rt-f013-f019-g3-g4-itm-resources (APPROVED; provides OriginalItemCatalog).
- Status: done for the scoped part. No registry box ticked.
- check-trackers: TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).

## Acceptance clauses
Done (data): raw, unscaled, sentinel-flagged access to fireDelay/inheritedSpeed/muzzleVelocity/aliveTime/maxLive/anti durations/explodeItem; values for rows 1, 6, 7, 61, 141, 153 match the FIELD_INTERPRETATION table; row 156 Disabler f[137]=300, f[138]=0 matches the RT-F039 catalog text; Disruptor Mine f[131]=3 and 150 x4 match.
Remaining: meaning/scales of -550 inheritance and -65 lifetime, launch formula, expiry/trigger modes, collision shape, ammo/child chains (RT-F040, G5 remainder), native behaviour verification.
Finding: the projectile layout is type 6 with 227 fields (92 rows). Type-8 rows (145..193 fields) hold non-integers at these indexes (row 42 f[94]="None"), so the view is deliberately restricted.

## Files
shared/RogueTrader.OriginalData/OriginalProjectile.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md, tests/Unit/OriginalProjectileTests.cs, TASK_QUEUE.md.

## Verified (cloud)
- Ubuntu apt dotnet-sdk-8.0 (8.0.131), scratch copy with global.json removed (pinned 8.0.419 not installable here), RT_ORIGINAL_ASSETS_ROOT = pinned clone eb1514ea: dotnet test tests/Unit -> Passed 234, Failed 0, Skipped 0 (223 before + 11 new).
- Without assets: Passed 191, Skipped 19, Failed 0.
## Not verified in cloud - local CI required
- Run on the pinned SDK 8.0.419; scripts/verify.ps1 not needed (no client change).

## Resume point
Next: G5 remainder (ammo records + launcher→ammo→child chain, RT-F040), then G7.

## Review round 1 (senior review)

Reviewer: senior review routine (did not author this work). Base: origin/cloud/rt-f013-f019-g3-g4-itm-resources (APPROVED round 1), diff reviewed against that base.

Scope understood: a raw, unscaled, sentinel-flagged typed view (`OriginalProjectile`/`ItmProjectileFields`) over the ITM v57 projectile fields f[54], f[94], f[101], f[110], f[131], f[135..138], f[142], restricted to type-6/227-field rows, with oracle tests copied from FIELD_INTERPRETATION.md and FEATURE_CATALOG RT-F036..F040. Data clauses of RT-F036/RT-F037 only; no box ticked. Branch name carries RT-F036/F037 and the log lists them; no other branch claims RT-F036/F037 (no collision).

Verdict: APPROVED (no blockers or majors; matches scope; RT-F036/F037 correctly left open because scale/formula/native clauses remain).

Findings:
1. minor - tests/Unit/OriginalProjectileTests.cs:85-88 `RejectsRowsTooShortForTheView` passes for the wrong reason: f[0] is "0", so `TryOf` rejects on record type before the field count is ever considered. Expected fix: set `fields[0] = "6"` (type 6, 31 fields) so the rejection is caused by the field count, or rename the test to what it actually checks.
2. nit - tests/Unit/OriginalProjectileTests.cs:62 row 156 (Disabler) is asserted only for f[137]/f[138]; its documented fireDelay 450 / inheritedSpeed 1000 / muzzle 2000 / aliveTime 2000 (FIELD_INTERPRETATION.md:71) are not in the `Rows` oracle although the IMPLEMENTATION_PLAN lists row 156 among the oracle rows. Expected fix: add `new(156, 1076, "Disabler", 450, 1000, 2000, 2000, 0)` to `Rows`.

Verified by the reviewer (cloud):
- Independent Python audit of pinned Rogue_mp.itm (fowlmouth/cosmic-rift-assets eb1514eaae7f9b9425307d9cc043c74ecdd15e35): 200 rows; every type-6 row has 227 fields (92 rows); type-8 rows have 109..193 fields; row 156 = 1076 Disabler f[54]=450 f[94]=1000 f[101]=2000 f[110]=2000 f[137]=300 f[138]=0; values match the oracle table.
- apt dotnet-sdk-8.0 8.0.131, global.json removed in a scratch worktree only: `dotnet test tests/Unit` with RT_ORIGINAL_ASSETS_ROOT -> Passed 234, Failed 0, Skipped 0; projectile filter -> 11 passed. Without assets -> Passed 191, Skipped 19, Failed 0 (matches the log).
- `python scripts/check-trackers.py` -> TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).
- No canonical asset, .tools/references, client or secret changes in the diff.

Not verified in cloud - local CI required: pinned SDK 8.0.419 run.

## Rework after merge-gate round
Merge-gate finding: PR not mergeable (conflict at end of OriginalData IMPLEMENTATION_PLAN.md). Fixed: merged origin/main (merge commit ad71a92, no rebase), then moved this branch's two plan lines into shared/RogueTrader.OriginalData/plan.d/rt-f036-f037.md and restored IMPLEMENTATION_PLAN.md to main's version (new per-item rule). check-trackers: TRACKERS_CONSISTENT. dotnet test: not re-run in cloud (no SDK in this sandbox; reviewer previously got 234 passed); no C# changed by this rework.
