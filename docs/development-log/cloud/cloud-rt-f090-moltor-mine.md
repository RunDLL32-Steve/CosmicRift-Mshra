Review status: UNREVIEWED

# cloud/rt-f090-moltor-mine

- RT-IDs: RT-F090 (Moltor Mine, ITM/Rogue_mp.itm row 15, id 1044, type 6 Space Mines), data/reference clause only. Gate items 2 and 4. Base: origin/main 02f661f. Not stacked. Run locally by the local development routine.
- Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change). Template: RT-F088 Plasma Mine tests (PR 66, content only borrowed, not stacked).

## Acceptance clauses (ITEM profile)
Done (data/reference): row 15, type 6, v57, 227 fields; category and description; mass 0, buy 25, sell 25 raw; logic `(5|6|9|10)&!%1` (Grazi hulls Rak'Kir, Rip'Pir, Zap'Pir, Zag'Gir; percent ref 1); held category candidate f[20]=7 with CFG Limit7=1/ExtendedLimit7=-1 (mapping unproven); ammo f[31]=1044 (self), explode f[142]=0, no event string; 11 asset pairs (4 non-None) with four BloMember entries unresolved; BLO archives GraziWeapons, ExplosionEffects, WeaponSounds present by exact name; only reverse reference is the mine itself; 227 fields accounted for.
Remaining (box stays OPEN): Moltor Core charging, mine laying/proximity behaviour, held-category enforcement, shop, BLO decoding and presentation, native verification.

## Files
- tests/Unit/OriginalMoltorMineTests.cs (new, 6 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f090.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 1044` (pinned eb1514ea) matches every asserted value.
- `dotnet test tests/Unit --filter FullyQualifiedName~OriginalMoltorMineTests` with RT_ORIGINAL_ASSETS_ROOT set: Passed 6, Failed 0, Skipped 0.

## Not verified
- CI result pending; native behaviour, shop, mine mechanics, BLO contents.

## Status
ready. No checkbox ticked. Resume: next unclaimed item RT-F091 MIRV Missile-Blast.
