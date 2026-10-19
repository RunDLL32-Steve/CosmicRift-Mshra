Review status: APPROVED (PR #10 review round 2, senior review 5545490657 on head 6ea1de6; label review-approved)

# cloud/rt-f049-sell-sentinel-audit

-   Claims: RT-F049 (data clause only: raw buy f[8]/sell f[15] distribution incl. the -1 sentinel); the RT-F049 buy/sell data clause only (gate item 2 cross-file sentinel inventory is owned by claim/g8a-format-audit). No queue row.
-   Stacked on cloud/rt-f043-hull-drops <- cloud/rt-f041-armor-records <- cloud/rt-f039-stun-emp-fields (F041/F043 reworked and READY FOR RE-REVIEW) <- APPROVED cloud/rt-f040-mirv-launcher-chain and the G3/G4/G5 branches. The new test only needs main plus the G3 catalog (OriginalItem.Buy/Sell). Merge in that order.
-   Status: done for the scoped data part. No registry box ticked, no production code changed (test-only evidence).

## Acceptance clauses

Done (data): independent Python audit of pinned Rogue_mp.itm over 200 rows: no negative buy; sell is -1 (34 rows, always buy 0, categories BaseChild, BaseLimitedUse, Child Effects, ChildProjectiles, Defensive Weapons, Resources, SpecialEffects), 0 (72 rows), or positive (94 rows); 114 rows have buy > 0; the single positive-sell/zero-buy row is 165 Wake Caster-Child (id 1082, sell 500). Only 3 of the 94 positive-sell rows have sell = buy/2, so no "resale half" rule is derivable from these fields alone.
Remaining: NWS buy/resale/drop policy revision choice, meaning of -1 (no resale vs not sold), ammo/weapon drop flags, shop exploit checks, simulation and native verification.

## Files

tests/Unit/OriginalSellSentinelTests.cs.

## Verified (cloud)

-   apt dotnet-sdk-8.0 8.0.131 with global.json temporarily moved aside (restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 253, Failed 0, Skipped 0 (250 + 3 new). Without assets: Passed 195, Skipped 34, Failed 0.
-   python scripts/check-trackers.py: TRACKERS_CONSISTENT.

## Not verified in cloud - local CI required

-   Pinned SDK 8.0.419 run.

## Resume point

Next: RT-F047/F048 item-table audits (repair rows 149/150/162, upgrade families), or G7 non-hull VEH loader (also resolves the unresolved Manufactory drop id 66).

## Review round 1 (senior review)

Scope understood: RT-F049 data clause only. A test-only audit of the raw buy f[8] and sell f[15] distribution over the 200 pinned ITM rows, including the -1 sell sentinel; nothing is interpreted. Stacked on cloud/rt-f043-hull-drops, which sits on cloud/rt-f041-armor-records.

Verdict: CHANGES REQUESTED. The branch's own content would be approvable (no own blocker or major). The stack base cloud/rt-f041-armor-records is CHANGES REQUESTED in this run, and cloud/rt-f043-hull-drops is therefore held as well: NEEDS BASE FIX.

Findings:

1. blocker (stack) - docs/development-log/cloud/cloud-rt-f049-sell-sentinel-audit.md:6. This branch contains the F041 commits with the open major. Expected fix: once cloud/rt-f043-hull-drops has merged the reworked F041 base, merge it into this branch with a normal merge (no rebase or force-push). Then fix the minors below and set READY FOR RE-REVIEW.
2. minor - tests/Unit/OriginalSellSentinelTests.cs:27. The test name SellSentinelAlwaysHasZeroBuyAndNoShopCategory claims "no shop category", which is neither asserted nor true. The -1 rows include Refined Tellerium (row 69, id 2004) and Refined Krandazite (row 71, id 2006), two of the seven G4 trade resources, and Orion Booster (row 161, Defensive Weapons). Expected fix: rename the test to something like SellSentinelAlwaysHasZeroBuy. Record in the log that both refined resources carry sell -1/buy 0, because that bears directly on M4 trading and RT-F014/F016 pricing.
3. minor - scope overlap. The log claims "the RT-F049 buy/sell data clause only (gate item 2 cross-file sentinel inventory is owned by claim/g8a-format-audit)", and the local claim branch claim/g8a-format-audit already claims the "negative sentinel inventory over ITM/VEH/RPG (fidelity gate item 2)". This is not an RT-ID collision under the claim rule, since G8a does not name RT-F049. Expected fix: claim only RT-F049's buy/sell data clause, drop "sentinel inventory" from the gate claim, and note that G8a owns the cross-file inventory.

Verified independently (Python audit of the pinned Rogue_mp.itm): 200 rows, no negative buy, no sell below -1. Sell is -1 in 34 rows, all with buy 0, across the 7 categories listed in the test. Sell is 0 in 72 rows and positive in 94. 114 rows have buy > 0. The only row with positive sell and zero buy is 165 Wake Caster-Child (id 1082, sell 500), which is also the only row with sell > buy. 3 positive-sell rows have sell exactly buy/2. Six -1 rows are Resources (69, 71, 84, 85, 90, 168). All of this matches the test and the log.

Branch name, claims and collisions: the name carries RT-F049 and the log claims RT-F049. No other branch names RT-F049. No queue row.

Verified in cloud (this review):

-   apt dotnet-sdk-8.0 8.0.131 in a detached scratch worktree, global.json moved aside and restored (not committed), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 253, Failed 0, Skipped 0. Without RT_ORIGINAL_ASSETS_ROOT: Passed 195, Skipped 34, Failed 0. Both match the log.
-   python scripts/check-trackers.py -> TRACKERS_CONSISTENT. No canonical asset, .tools/references, secret or unrelated changes.

Not verified (local CI required): a run with the pinned SDK 8.0.419; the meaning of -1, the NWS policy revision choice, drop flags and shop exploit behavior.

## Rework after round 1

1. blocker (stack) - fixed: merged the reworked cloud/rt-f043-hull-drops (which carries the reworked F041) with a normal merge.
2. minor (test name) - fixed: renamed to SellSentinelAlwaysHasZeroBuy. Recorded for M4 trading and RT-F014/F016 pricing: Refined Tellerium (row 69, id 2004) and Refined Krandazite (row 71, id 2006), two of the seven G4 trade resources, carry sell -1 and buy 0, as does Orion Booster (row 161, Defensive Weapons). The meaning of -1 stays undecoded.
3. minor (scope overlap) - fixed: claim narrowed to the RT-F049 buy/sell clause; G8a owns the cross-file sentinel inventory.
   Verified in cloud: dotnet-sdk 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Failed: 0, Passed: 255, Skipped: 0, Total: 255, Duration: 1 s; check-trackers -> TRACKERS_CONSISTENT. Not verified: pinned SDK 8.0.419.
