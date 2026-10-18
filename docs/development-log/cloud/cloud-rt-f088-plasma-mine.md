Review status: APPROVED (round 1, senior review local, head 480a34b)

# cloud/rt-f088-plasma-mine

- RT-IDs: RT-F088 (Plasma Mine, ITM/Rogue_mp.itm row 13, id 102, type 6 Space Mines), data/reference clause only. Gate items 2 and 4. Base: origin/main b5fc80e. Not stacked. Run locally (not cloud).
- Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change). Template: RT-F082 Plasma Bomb / RT-F081 MIRV tests.

## Acceptance clauses (ITEM profile)
Done (data/reference): row 13, type 6, v57, 227 fields; category `Space Mines`; description; mass f[7]=0, buy f[8]=25, sell f[15]=25 (raw); logic `!%1` (percent ref 1, no skills, no exchange); held category candidate f[20]=7 with CFG Limit7=1/ExtendedLimit7=-1 at line 2979 (mapping unproven, HeldCategoryMapping unresolved); ammo f[31]=102 (self), explode f[142]=0, no event string; 11 asset pairs (4 non-None) with four BloMember entries unresolved; BLO archives GraziWeapons, ExplosionEffects, WeaponSounds present by exact name; only reverse reference is the mine itself (row 13, f[31]); 227 fields accounted for.
Remaining (box stays OPEN): mine laying/proximity trigger and plasma blast behaviour, held-category limit enforcement, shop and store behaviour, BLO decoding and presentation, native verification.

## Files
- tests/Unit/OriginalPlasmaMineTests.cs (new, 6 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f088.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 102` (pinned eb1514ea) matches every asserted value; CFG line 2979 (Limit7=1) confirmed by grep in CFG/rogue.cfg.
- `dotnet test tests/Unit --filter FullyQualifiedName~OriginalPlasmaMineTests` with RT_ORIGINAL_ASSETS_ROOT set: Passed 6, Failed 0, Skipped 0.
- check-trackers: TRACKERS_CONSISTENT.

## Not verified
- CI result pending; native behaviour, shop, mine mechanics, BLO contents.

## Status
wip -> ready. No checkbox ticked.
