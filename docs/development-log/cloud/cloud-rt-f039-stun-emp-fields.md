Review status: UNREVIEWED

# cloud/rt-f039-stun-emp-fields

- Claims: RT-F039 (data clause only: raw stun/EMP/disable field audit); gate items 2 and 5 (data). No queue row exists for it.
- Stacked on origin/cloud/rt-f040-mirv-launcher-chain (APPROVED round 2; provides OriginalProjectile with AntiRecharge/AntiFire/AntiThrust/AntiRotate f[135..138]), itself on the APPROVED G5/G3 branches. Merge in that order.
- Status: wip.
- Status: done for the scoped data part (replaces wip). No registry box ticked.

## Acceptance clauses
Done (data): independent Python audit of pinned Rogue_mp.itm found exactly 10 type-6 rows with non-zero f[135..138]: rows 11, 17, 74, 95, 200 (f[138]=50), 61 and 62 (150 x4), 104 Disruptor Projector (f[135]=400), 105 BioPulse Bomb (50,0,50,0), 156 Disabler (f[137]=300). Rows 104, 105 and the 50-valued special-effect rows are not in the catalog text. Blackout Bomb row 155 has all four fields 0 although its help text says it drains shield energy: its effect lives in other, undecoded fields.
Remaining: time unit of durations, which fields mean "inhibit" vs recovery, Blackout shield-drain field, Disabler help text ("thrust and maneuver") vs f[138]=0, Concussion Mine launch effect, simulation/native verification.

## Files
shared/RogueTrader.OriginalData/OriginalDisableEffects.cs, tests/Unit/OriginalDisableEffectsTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 with global.json temporarily moved aside (restored), pinned assets eb1514ea at RT_ORIGINAL_ASSETS_ROOT: dotnet test tests/Unit -> Passed 245, Failed 0, Skipped 0 (242 + 3 new).
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
Next unclaimed registry entry: RT-F041 (damage types/armor, data audit of ITM armor rows 20-23,63,66, rows 151/152).
