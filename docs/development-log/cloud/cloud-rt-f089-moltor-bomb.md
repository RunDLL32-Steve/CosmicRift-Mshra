Review status: APPROVED (round 1, senior review local)

# cloud/rt-f089-moltor-bomb

- RT-IDs: RT-F089 (Moltor Bomb, ITM/Rogue_mp.itm row 14, id 1042, type 6 Energy Bomb Projectors), data/reference clause only. Gate items 2 and 4. Base: origin/main 02f661f. Not stacked. Run locally (not cloud) by the local development routine.
- Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change). Template: RT-F082 Plasma Bomb tests.

## Acceptance clauses (ITEM profile)
Done (data/reference): row 14, type 6, v57, 227 fields; category and description; mass 0, buy 5500, sell 3500 (raw); logic `(5|6|9|10)!%1` (hull skills 5,6,9,10 = Grazi hulls Rak'Kir, Rip'Pir, Zap'Pir, Zag'Gir; percent ref 1); held category candidate f[20]=6 with CFG Limit6=1/ExtendedLimit6=-1 (mapping unproven); ammo f[31]=0, explode f[142]=0, no event string; 11 asset pairs (5 non-None) with five BloMember entries unresolved; BLO archives GraziWeapons, Ebombs, ExplosionEffects, WeaponSounds present by exact name; no reverse references; 227 fields accounted for.
Remaining (box stays OPEN): Moltor Energy Core charging, bomb firing/blast behaviour, held-category limit enforcement, shop behaviour, BLO decoding and presentation, native verification.

## Files
- tests/Unit/OriginalMoltorBombTests.cs (new, 6 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f089.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 1042` (pinned eb1514ea) matches every asserted value; CFG Limit6=1 at line 2977 per audit.
- `dotnet test tests/Unit --filter FullyQualifiedName~OriginalMoltorBombTests` with RT_ORIGINAL_ASSETS_ROOT set: Passed 6, Failed 0, Skipped 0.

## Not verified
- CI result pending; native behaviour, shop, bomb mechanics, BLO contents.

## Status
ready. No checkbox ticked. Resume: next unclaimed item RT-F090 Moltor Mine.
