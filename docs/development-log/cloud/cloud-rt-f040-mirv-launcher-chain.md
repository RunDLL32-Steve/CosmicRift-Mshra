Review status: UNREVIEWED

# cloud/rt-f040-mirv-launcher-chain

- Claims: RT-F040 (data clause only: launcher -> ammo -> child chain resolution); queue G5 remainder; gate items 2 and 5 (data). Stacked on origin/cloud/rt-f036-f037-g5-weapon-records (UNREVIEWED; provides OriginalProjectile), itself on the APPROVED cloud/rt-f013-f019-g3-g4-itm-resources. Merge in that order.
- Status: done for the scoped part. No registry box ticked.

## Acceptance clauses
Done (data): MIRV chain 1047 (row 6) -> ammo 2002 (row 82) -> child 3008 resolves; audit of the 92 type-6/227-field rows: every non-zero f[31] (20 rows) and f[142] resolves to an existing item id; mines/defensive weapons reference themselves; Shrapnel 1050 -> 2009 -> 3011; Moltor Shower 1072 -> 2017 -> 3015.
Remaining: meaning of aliveTime -65 and inheritedSpeed -550 as release trigger, child count/angles/inheritance/damage, native verification. f[31] is a name inferred from the pattern, not stated in the pinned notes.

## Files
shared/RogueTrader.OriginalData/OriginalWeaponChain.cs, tests/Unit/OriginalWeaponChainTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md, TASK_QUEUE.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 in a scratch copy without global.json, pinned assets eb1514ea: dotnet test tests/Unit -> Passed 239, Failed 0, Skipped 0 (234 + 5 new). Without assets: Passed 191, Skipped 24, Failed 0.
- python scripts/check-trackers.py: TRACKERS_CONSISTENT.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
Next: G5 value audit of ammo records and remaining weapon fields; G7 (18 remaining VEH records).
