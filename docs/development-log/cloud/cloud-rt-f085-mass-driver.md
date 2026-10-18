Review status: UNREVIEWED

# cloud/rt-f085-mass-driver

- RT-IDs: RT-F085 (Mass Driver, ITM/Rogue_mp.itm row 10, id 1024, type 6 cannon), data/reference clause only. Base: origin/main b5fc80e. Not stacked. Run locally (not cloud).
- Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change). Template: RT-F081 (MIRV) and RT-F084.

## Acceptance clauses (ITEM profile)
Done (data/reference): row 10, type 6, v57, 227 fields; category `Cannon`; description; mass f[7]=0, buy f[8]=5000, sell f[15]=3500 (raw); skill logic `(2|8|15|16)!%1` (four hull skills, one percent reference, no exchange); held-category candidate f[20]=5 with CFG row 5 (Limit 1, Extended -1; mapping unresolved); ammo f[31]=2014 (row 134, Mass Driver Pellets), explode f[142]=0, no child entries; 11 asset pairs with 5 unresolved BLO members and the BLO file-name case check; no reverse or vehicle references; 99 named / 128 unnamed field coverage.
Remaining (box stays OPEN): firing behaviour, ammo consumption, damage, shop/store behaviour, presentation, held-category mapping, BLO member content (C1), native verification.

## Files
- tests/Unit/OriginalMassDriverTests.cs (new, 6 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f085.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 1024` (pinned eb1514ea) matches every value it prints; chain, held-limit line, asset-pair, unresolved and field-count values were read from the resolver output.
- `dotnet test tests/Unit --filter FullyQualifiedName~OriginalMassDriverTests` with RT_ORIGINAL_ASSETS_ROOT set: Passed 6, Failed 0, Skipped 0.
- check-trackers: TRACKERS_CONSISTENT.

## Not verified
- CI result pending; native behaviour, shop, ammo consumption, BLO content.

## Status
wip -> ready for review.
