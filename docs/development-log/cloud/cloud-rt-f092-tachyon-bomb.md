Review status: APPROVED (round 1, senior review local)

# cloud/rt-f092-tachyon-bomb

- RT-IDs: RT-F092 (Tachyon Bomb, ITM/Rogue_mp.itm row 17, id 1000, type 6 Energy Bomb Projectors), data/reference clause only. Gate items 2 and 4. Base: origin/main 02f661f. Not stacked. Run locally by the local development routine. RT-F091 (MIRV Missile-Blast, a child effect) was skipped here: not an independent item record to pin ahead of its parent launcher branch.
- Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change). Template: RT-F082 Plasma Bomb tests.

## Acceptance clauses (ITEM profile)
Done (data/reference): row 17, type 6, v57, 227 fields; category and description; mass 0, buy 6000, sell 4000 raw; logic `(9|3|16|1|15)!%1` (Zap'Pir, Kragath, Ravager, Skithist, Excelsior; percent ref 1); held category candidate f[20]=6 with CFG Limit6=1/ExtendedLimit6=-1 at line 2977 (mapping unproven); ammo f[31]=0, explode f[142]=0, no event string; 11 asset pairs (5 non-None) with five BloMember entries unresolved; BLO archives present (weaponeffects.blo lower case); no reverse references; 227 fields accounted for.
Remaining (box stays OPEN): bomb firing/blast behaviour, held-category enforcement, shop, BLO decoding and presentation, native verification.

## Files
- tests/Unit/OriginalTachyonBombTests.cs (new, 6 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f092.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 1000` (pinned eb1514ea) matches every asserted value.
- `dotnet test tests/Unit --filter FullyQualifiedName~OriginalTachyonBombTests` with RT_ORIGINAL_ASSETS_ROOT set: Passed 6, Failed 0, Skipped 0.

## Not verified
- CI result pending; native behaviour, shop, bomb mechanics, BLO contents.

## Status
ready. No checkbox ticked. Resume: next unclaimed item RT-F093 SuperBoosters.
