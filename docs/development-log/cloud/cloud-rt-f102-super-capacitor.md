Review status: UNREVIEWED

# cloud/rt-f102-super-capacitor

- RT-IDs: RT-F102 (Super Capacitor, ITM/Rogue_mp.itm row 27, id 10, type 13), data/reference clause only. Gate items 2 and 4. Base: origin/main 1ce1d21. Not stacked. Run locally by the local development routine. Template: RT-F099 tests.

## Acceptance clauses (ITEM profile)
Done (data/reference): row 27, type 13, v57, 137 fields; category `Power Mods`, description "Increases overall ship energy capacity."; mass 0, buy 15000, sell 11000 raw; empty logic, no skill or percent references; held category candidate f[20]=12 with CFG Limit12=1/ExtendedLimit12=-1 at line 2989 (mapping unproven); both asset pairs None; no BloMember; 137 fields accounted for.
Remaining (box stays OPEN): capacity effect behaviour, held-category enforcement, shop, presentation, native verification.

## Files
- tests/Unit/OriginalSuperCapacitorTests.cs (new, 5 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f102.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 10` (pinned eb1514ea) matches every asserted value.
- `dotnet test tests/Unit --filter FullyQualifiedName~OriginalSuperCapacitorTests` with RT_ORIGINAL_ASSETS_ROOT set: Passed 5, Failed 0, Skipped 0.

## Not verified
- CI result pending; native behaviour, shop, effect values.

## Status
ready. No checkbox ticked. Resume: RT-F103 Turbocharger.
