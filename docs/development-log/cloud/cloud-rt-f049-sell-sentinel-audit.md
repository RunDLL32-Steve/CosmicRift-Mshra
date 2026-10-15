Review status: UNREVIEWED

# cloud/rt-f049-sell-sentinel-audit

- Claims: RT-F049 (data clause only: raw buy f[8]/sell f[15] distribution incl. the -1 sentinel); gate item 2 (sentinel inventory). No queue row.
- Stacked on cloud/rt-f043-hull-drops <- cloud/rt-f041-armor-records <- cloud/rt-f039-stun-emp-fields (all UNREVIEWED) <- APPROVED cloud/rt-f040-mirv-launcher-chain and the G3/G4/G5 branches. The new test only needs main plus the G3 catalog (OriginalItem.Buy/Sell). Merge in that order.
- Status: done for the scoped data part. No registry box ticked, no production code changed (test-only evidence).

## Acceptance clauses
Done (data): independent Python audit of pinned Rogue_mp.itm over 200 rows: no negative buy; sell is -1 (34 rows, always buy 0, categories BaseChild, BaseLimitedUse, Child Effects, ChildProjectiles, Defensive Weapons, Resources, SpecialEffects), 0 (72 rows), or positive (94 rows); 114 rows have buy > 0; the single positive-sell/zero-buy row is 165 Wake Caster-Child (id 1082, sell 500). Only 3 of the 94 positive-sell rows have sell = buy/2, so no "resale half" rule is derivable from these fields alone.
Remaining: NWS buy/resale/drop policy revision choice, meaning of -1 (no resale vs not sold), ammo/weapon drop flags, shop exploit checks, simulation and native verification.

## Files
tests/Unit/OriginalSellSentinelTests.cs.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 with global.json temporarily moved aside (restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 253, Failed 0, Skipped 0 (250 + 3 new). Without assets: Passed 195, Skipped 34, Failed 0.
- python scripts/check-trackers.py: TRACKERS_CONSISTENT.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
Next: RT-F047/F048 item-table audits (repair rows 149/150/162, upgrade families), or G7 non-hull VEH loader (also resolves the unresolved Manufactory drop id 66).
