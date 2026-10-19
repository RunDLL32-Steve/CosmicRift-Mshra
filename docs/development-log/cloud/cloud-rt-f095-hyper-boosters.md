Review status: UNREVIEWED

# cloud/rt-f095-hyper-boosters

- RT-IDs: RT-F095 (HyperBoosters, ITM/Rogue_mp.itm row 20, id 3, type 13 Engine Mods), data/reference clause only. Gate items 2 and 4. Base: origin/main 3a205e2. Not stacked. Run locally by the local development routine.
- Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change), same pattern as merged RT-F093.

## Acceptance clauses (ITEM profile)
Done (data/reference): row 20, type 13, v57, 137 fields; category and description; mass 0, buy 12000, sell 8500 raw; logic `15|1|10` (Excelsior, Skithist, Zag'Gir; no percent ref); held category candidate f[20]=14 with CFG Limit14=1/ExtendedLimit14=-1 at line 2993 (mapping unproven); two asset pairs both None.
Remaining (box stays OPEN): the engine modification's numeric effect fields and semantics, installation/held-category enforcement, shop, native verification.

## Files
- tests/Unit/OriginalHyperBoostersTests.cs (new, 5 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f095.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 3` (pinned eb1514ea) matches every asserted value.
- `dotnet test tests/Unit --filter FullyQualifiedName~OriginalHyperBoostersTests` with RT_ORIGINAL_ASSETS_ROOT set (pinned .tools dotnet): Passed 5, Failed 0, Skipped 0.

## Not verified
- CI result pending; native behaviour, shop, effect values.

## Status
ready. No checkbox ticked.
