Review status: UNREVIEWED

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
