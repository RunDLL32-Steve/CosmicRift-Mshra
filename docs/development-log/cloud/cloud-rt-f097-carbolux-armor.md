Review status: UNREVIEWED

# cloud/rt-f097-carbolux-armor

- RT-IDs: RT-F097 (Carbolux Armor, ITM/Rogue_mp.itm row 22, id 5, type 13), data/reference clause only. Gate items 2 and 4. Base: origin/main 1ce1d21. Not stacked. Run locally by the local development routine. Template: RT-F099 tests.

## Acceptance clauses (ITEM profile)
Done (data/reference): row 22, type 13, v57, 137 fields; category `Hull Armor` and description; mass 0, buy 10000, sell 0 raw; empty logic, no skill or percent references; held category candidate f[20]=11 with CFG Limit11=1/ExtendedLimit11=-1 at line 2987 (mapping unproven); two asset pairs both None (second has trailing raw ints 0,0,1000,90,0,50, meaning unknown); no BloMember; 137 fields accounted for.
Remaining (box stays OPEN): armor damage-type protection behaviour, held-category enforcement, shop, presentation, native verification.

## Files
- tests/Unit/OriginalCarboluxArmorTests.cs (new, 5 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f097.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 5` (pinned eb1514ea) matches every asserted value.
- `dotnet test tests/Unit --filter FullyQualifiedName~OriginalCarboluxArmorTests` with RT_ORIGINAL_ASSETS_ROOT set: Passed 5, Failed 0, Skipped 0.

## Not verified
- CI result pending; native behaviour, shop, protection values.

## Status
ready. No checkbox ticked. Resume: RT-F098 Maxilite Armor.
