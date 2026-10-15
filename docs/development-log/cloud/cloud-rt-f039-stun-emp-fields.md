Review status: APPROVED

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

## Review round 1 (senior review)

Scope understood: RT-F039 data clause only. Expose the raw f[135..138] (AntiRecharge/AntiFire/AntiThrust/AntiRotate) values of the type-6/227 projectile rows, list the rows that carry a non-zero value, and record that Blackout Bomb row 155 carries none. No behavior, no tick. Stacked on cloud/rt-f040-mirv-launcher-chain (APPROVED round 2).

Verdict: APPROVED. No blockers or majors. The work matches its scope, and the registry box correctly stays open.

Findings:
1. nit - docs/development-log/cloud/cloud-rt-f039-stun-emp-fields.md:7-8. The log has both "Status: wip." and "Status: done ... (replaces wip)". Expected fix: keep one status line (done for the scoped data part).
2. nit - tests/Unit/OriginalDisableEffectsTests.cs:46. SyntheticItem() returns default! (null), so the record is built with a null OriginalItem. The test passes because IsDisabling never touches Item, but a null in a non-nullable record is fragile. Expected fix: build a real item from RawRecordReader.Parse of a one-line synthetic v57 record, or note in a comment that the item is intentionally unused.
3. nit - Find scans only the type-6/227 view, as documented. An independent check found that the six type-8 rows with more than 138 fields (42, 43, 44, 89, 101, 115) are all zero at f[135..138], so nothing is missed today. Optional: say this in the log.

Branch name, claims and collisions: the name carries RT-F039 and the log claims RT-F039. No other origin branch (cloud/*, claim/*) names RT-F039. There is no TASK_QUEUE row, which is correct for a registry-only item.

Verified in cloud (this review):
- apt dotnet-sdk-8.0 8.0.131 in a detached scratch worktree, global.json moved aside and restored (not committed), RT_ORIGINAL_ASSETS_ROOT = pinned fowlmouth/cosmic-rift-assets eb1514eaae7f9b9425307d9cc043c74ecdd15e35 shallow-cloned outside the checkout.
- dotnet test tests/Unit -> Passed 245, Failed 0, Skipped 0. This matches the log.
- python scripts/check-trackers.py -> TRACKERS_CONSISTENT.
- An independent Python audit of Rogue_mp.itm found exactly 92 type-6/227 rows. Ten of them have non-zero f[135..138]: rows 11 Meson Bomb, 17 Tachyon Bomb, 74 DeathOrbCannon, 95 DeathRay and 200 DeathRay v2 (0,0,0,50); 61 Disruptor Mine and 62 Concussion Mine (150 x4); 104 Disruptor Projector (400,0,0,0); 105 BioPulse Bomb (50,0,50,0); 156 Disabler (0,0,300,0). Row 155 Blackout Bomb is 0,0,0,0. All values match the test oracle and the log.
- The diff touches only the four listed files. No canonical asset, .tools/references, secret or unrelated changes.

Not verified (local CI required): a run with the pinned SDK 8.0.419; the time unit, inhibit-versus-recovery semantics and simulation/native behavior. These remain open by design.
