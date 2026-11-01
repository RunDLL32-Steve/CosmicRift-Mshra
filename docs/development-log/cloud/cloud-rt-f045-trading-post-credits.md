Review status: APPROVED (PR #12 review round 1, senior review 5545504938 on head 8787a9b; label review-approved)

# cloud/rt-f045-trading-post-credits

-   Claims: RT-F045 (data clause only: Trading Outpost VEH row 5 trade recipes and ITM credit effects 17/18/19/44); gate items 2 and 5 (data). No queue row.
-   Stacked on cloud/rt-f013-f019-g3-g4-itm-resources (PR #2, provides OriginalItemCatalog).
-   Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses

Done (data): independent Python audit of pinned Rogue.veh row 5 (id 404, Trading Outpost): four 14-field blocks at f[253], f[267], f[281], f[295] named Trade Tellerium / Krandazite / Wreckage / Alloy. Block f+1 = credit effect item (18 Tellerium Credits row 85, 17 Krandazite Credits row 84, 19 Scrap Credits row 90, 44 Alloy Credits row 168); f+5 = input item (2004 Refined Tellerium, 2006 Refined Krandazite, 2008 Wreckage, 2020 Alloy); f+4 = 150,150,0,0 (meaning unknown, kept raw). Credit item rows are type 1/Resources with f[31] = 55, 80, 300, 20 (meaning/scale unknown).
Remaining: meaning of f+4 and f[31] (award amount/rate?), eligibility, NWS row 303 Wreckage 20 vs earlier 100 revision choice (raw f[31] for Scrap Credits is 300, which matches neither), LIO 21/22 outpost placement (needs LIO reader), transaction acceptance, native verification.

## Files

shared/RogueTrader.OriginalData/OriginalTradingPost.cs, tests/Unit/OriginalTradingPostTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)

-   apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 225, Failed 0, Skipped 0. Without assets: Passed 190, Skipped 16, Failed 0.

## Not verified in cloud - local CI required

-   Pinned SDK 8.0.419 run.

## Resume point

RT-F044 (public refinery) is the same shape (VEH id 400 recipes) and would reuse OriginalTradeRecipe style; RT-F046/F048 follow.
