Review status: UNREVIEWED

# cloud/rt-f041-armor-records

- Claims: RT-F041 (data clause only: armor rows and Remove Armor resolution); gate items 2 and 5 (data). No queue row.
- Stacked on cloud/rt-f039-stun-emp-fields (UNREVIEWED), which is on APPROVED cloud/rt-f040-mirv-launcher-chain and the G3/G4/G5 branches. Merge in that order; this branch touches no file of the F039 branch except the shared folder plan (append-only, may conflict trivially).
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): independent Python audit of pinned Rogue_mp.itm: exactly 9 type-13/137-field "Hull Armor" rows (rows 21,22,23,63,66,108,109,136,137; ids 4,5,6,13,16,23,24,37,38). The catalog text lists rows 20-23, but row 20 is HyperBoosters (Engine Mods); the armors start at row 21 and 108/109/136/137 were not listed. Remove Armor (id 41, row 151) lists (armor id, 42) pairs from f[31] for all nine armors; each resolves, effect 42 = ArmorRemoveHack row 152. f[42..44] hold negative per-armor values whose order follows the help-text protection grades for most but not all rows (Sillicate "no protection versus Energy" = -25), so they are exposed unnamed.
Remaining: damage-type scale and hit order, meaning/sign of f[42..44], NWS row 131-134/323 confirmation flow, paid removal, upgrade restrictions, simulation and native verification.

## Files
shared/RogueTrader.OriginalData/OriginalArmor.cs, tests/Unit/OriginalArmorTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 with global.json temporarily moved aside (restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 248, Failed 0, Skipped 0 (245 + 3 new).
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
Next unclaimed registry entry in order: RT-F042 (LIO type-5 mineral spawns; needs the LIO reader, claimed by G6) or RT-F047/F048/F049 item-table audits.
