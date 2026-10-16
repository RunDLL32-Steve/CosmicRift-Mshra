Review status: CHANGES REQUESTED (PR #19 review round 1, senior review 5545813998 on head e749660: NEEDS BASE FIX #15 plus 1 minor; label review-changes-requested)

# cloud/rt-f033-held-category-limits

- Claims: RT-F033 (data clauses only: CFG [HeldCategory] Limit/ExtendedLimit rows and [Soul] UtilitySlots keys, raw); gate items 2 and 5 (data). No queue row.
- Stacked on cloud/rt-f046-c2-cfg-terrain-access (PR #15, review-pending; provides OriginalCfg). Merge that first.
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): raw typed read of rogue.cfg [HeldCategory] (30 Limit/ExtendedLimit pairs, lines 2967-3026; Limit1=500, Limit4..14=1, Limit15=2/Ext50, Limit16=2/Ext25, Limit17=2/Ext50, Limit20=2/Ext35, others 1 or -1) and [Soul] UtilitySlots (10 default, 5 range keys all 10), checked against an independent read of the pinned file.
Remaining: mapping of category ids 1..30 to the ITM category strings (ITM has 28 distinct strings; no evidence found in ITM fields, docs or NWS that fixes the order), ExtendedLimit semantics, NWS rows 33-34, purchase/install/use restriction enforcement, native verification.

## Files
shared/RogueTrader.OriginalData/OriginalHeldCategory.cs, tests/Unit/OriginalHeldCategoryTests.cs, shared/RogueTrader.OriginalData/plan.d/rt-f033.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 248, Failed 0, Skipped 0.
- python scripts/check-trackers.py -> TRACKERS_CONSISTENT.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run; native behaviour.

## Resume point
Find evidence for the category id order (e.g. other arena CFG/NWS text or shop UI strings) before any enforcement logic.
