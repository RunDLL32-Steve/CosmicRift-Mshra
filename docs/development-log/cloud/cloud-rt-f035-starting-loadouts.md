Review status: READY FOR RE-REVIEW

# cloud/rt-f035-starting-loadouts

- Claims: RT-F035 (data clauses only: the twelve starting-loadout strings and the twelve type-16 same-race hull-exchange rows); gate items 2 and 5 (data). No queue row.
- Stacked on cloud/rt-f013-f019-g3-g4-itm-resources (PR #2, APPROVED log; provides OriginalItemCatalog).
- Status: done for the scoped data part.
- RT-F249..RT-F260 (rows 181-192) are read and tested here but NOT claimed; their boxes stay open. No registry box ticked.

## Acceptance clauses
Done (data): independent Python audit of pinned Rogue.veh f[129] for the 12 type-2 hulls (rows 18-29): exactly one `addinv=<name>` each. Plasma Cannon (item 1004): Hornet, Asp, Excelsior, Ravager; Bio Cannon (1036): Skithist, Kragath, Mastakith, Tarthist; D-Cannon (1041): Rak'Kir, Rip'Pir, Zap'Pir, Zag'Gir. All resolve by exact name to a unique ITM "Cannon" row. ITM rows 181-192 (type 16, "Ship Hulls-<race>"): buy 15000, sell 0, f[5] logic = skills of the three other same-race hulls, f[32] = RPG skill id of the named hull (all 12), f[31] repeats f[5] except Zag'Gir (f[5] `9|6|5`, f[31] `5|6|9`, same set).
Remaining: event grammar (RT-F067), class-change event order, whether the exchange is paid/free in the shipped flow (NWS rows 251-254), inventory/energy/velocity policy on hull change, native verification.

## Files
shared/RogueTrader.OriginalData/OriginalLoadout.cs, tests/Unit/OriginalLoadoutTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 226, Failed 0, Skipped 0 (223 + 3 new).
- Without assets: Passed 190, Skipped 17, Failed 0.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
Next unclaimed registry entry: RT-F042 (needs LIO reader, G6 claimed), RT-F044..F048.


## Rework after merge-gate round
Merged origin/main (merge commit, no conflicts remained); plan line moved to shared/RogueTrader.OriginalData/plan.d/rt-f035.md (IMPLEMENTATION_PLAN.md = main). Review fixes: RT-F249..F260 stated as read/tested but NOT claimed; all twelve hull names asserted; single-addinv limitation documented. dotnet test not re-run in cloud (no SDK); check-trackers consistent.