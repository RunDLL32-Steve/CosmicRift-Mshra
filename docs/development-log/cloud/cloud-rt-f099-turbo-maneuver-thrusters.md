Review status: UNREVIEWED

# cloud/rt-f099-turbo-maneuver-thrusters

- RT-IDs: RT-F099 (Turbo Maneuver Thrusters, ITM/Rogue_mp.itm row 24, id 7, type 13 Thruster Mods), data/reference clause only. Gate items 2 and 4. Base: origin/main 3a205e2. Not stacked. Run locally by the local development routine.
- Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change), same pattern as merged RT-F093.

## Acceptance clauses (ITEM profile)
Done (data/reference): row 24, type 13, v57, 137 fields; category and description; mass 0, buy 5000, sell 3000 raw; empty skill logic (no skill/percent references); held category candidate f[20]=13 with CFG Limit13=1/ExtendedLimit13=-1 at line 2991 (mapping unproven); two asset pairs both None.
Remaining (box stays OPEN): numeric effect fields and semantics, installation/held-category enforcement, shop, native verification.

## Files
- tests/Unit/OriginalTurboManeuverThrustersTests.cs (new, 5 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f099.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 7` (pinned eb1514ea) matches every asserted value.
- `dotnet test tests/Unit --filter FullyQualifiedName~OriginalTurboManeuverThrustersTests` with RT_ORIGINAL_ASSETS_ROOT set (pinned .tools dotnet): Passed 5, Failed 0, Skipped 0.

## Not verified
- CI result pending; native behaviour, shop, effect values.

## Status
ready. No checkbox ticked.
