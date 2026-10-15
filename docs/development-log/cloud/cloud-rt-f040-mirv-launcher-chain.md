Review status: CHANGES REQUESTED

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

## Review round 1 (senior review)

Reviewer: senior review routine (did not author this work). Stacked base origin/cloud/rt-f036-f037-g5-weapon-records reviewed first in this run (APPROVED round 1); diff reviewed against that base.

Scope understood: RT-F040 data clause only - resolve launcher -> ammo (f[31], name inferred) -> child (f[142] explodeItem) for the 92 type-6 rows, with oracle tests on MIRV 1047 -> 2002 -> 3008. No box ticked. Branch name carries RT-F040, log lists it, no collision.

Verdict: CHANGES REQUESTED.

Findings:
1. major - shared/RogueTrader.OriginalData/OriginalWeaponChain.cs:14,41-46 and tests/Unit/OriginalWeaponChainTests.cs:21: `Children` stops at the first non-type-6 row, which in Rogue_mp.itm is every f[142] target (all are type-8 multi-item rows). The pinned record 3008 "MIRV Missile-Blast" (row 16, type 8, 133 fields) itself lists six sub-projectile entries 1045, 1048, 1045, 1046, 1049, 1046 with offsets -5, -10, -15, 5, 10, 15; 3015 MoltorClusterMd lists 1071 x5; 3011 Shards lists 1057 x10. So the "launcher -> ammo -> child chain" stops exactly before the real children, and `Assert.Single(c.Children)` bakes in a MIRV child count of 1, contradicting the source (RT-F040 asks to verify "child count/angles"). The log's "child 3008 resolves" is true but the API and IMPLEMENTATION_PLAN.md:22 ("follows ... transitively") read as a complete chain. Expected fix (either): (a) expose the type-8 entry list raw (item id + raw offset per entry, no interpretation) and resolve each id, with an oracle test asserting the six MIRV entries from the record; or (b) rename `Children` to something like `ExplodeItems`/`ExplodeTarget`, document that type-8 multi-item expansion is not resolved, drop the single-child implication from the test, and add "type-8 multi-item entry expansion (MIRV 3008 lists 6 entries)" as an explicit Remaining clause in this log and the IMPLEMENTATION_PLAN.
2. minor - shared/RogueTrader.OriginalData/OriginalWeaponChain.cs:41-48: the transitive type-6 -> type-6 path and the repeated-id stop are never exercised (no f[142] target in Rogue_mp.itm is type 6). Expected fix: a synthetic `OriginalItemCatalog.FromRecords` test covering a type-6 -> type-6 -> 0 chain and a cycle.
3. nit - OriginalWeaponChain.cs:53 linear `FirstOrDefault` re-implements `OriginalItemCatalog.ById`; reuse it (catching its exception or adding a TryById) so lookup semantics live in one place.
4. nit - f[31] "ammo" name is inferred (correctly flagged). Evidence supports it: the 20 non-zero values are either the row's own id (mines/defensive/Repulsor) or a type-4 "Ammo-*" row. Keep it flagged until a loader source names it.

Verified by the reviewer (cloud):
- Independent Python audit of pinned Rogue_mp.itm (eb1514eaae7f9b9425307d9cc043c74ecdd15e35): 20 type-6 rows with non-zero f[31] (all resolve; 9 self-references, 11 to type-4 Ammo-* rows, e.g. 1047->2002 row 82, 1050->2009, 1072->2017); every non-zero f[142] resolves (all to type-8 rows); MIRV, Shrapnel 1050->3011, Moltor Shower 1072->3015 confirmed.
- apt dotnet-sdk-8.0 8.0.131, global.json removed in a scratch worktree only, RT_ORIGINAL_ASSETS_ROOT set: `dotnet test tests/Unit` -> Passed 239, Failed 0, Skipped 0 (matches the log).
- `python scripts/check-trackers.py` -> TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).
- No canonical asset, .tools/references or secret changes.

Not verified in cloud - local CI required: pinned SDK 8.0.419 run.
