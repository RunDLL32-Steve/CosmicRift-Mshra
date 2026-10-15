Review status: UNREVIEWED

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
