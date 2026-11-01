Review status: APPROVED (round 1, senior review local, head 2530490)

# cloud/rt-f087-meson-cannon

-   RT-IDs: RT-F087 (Meson Cannon, ITM/Rogue_mp.itm row 12, id 1084, type 6, logic `!%1`), data/reference clause only. Base: origin/main b5fc80e. Not stacked. Run locally (not cloud).
-   Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change). Closest templates: RT-F081 MIRV (type 6, asset pairs, BLO case check) and RT-F082 Plasma Bomb (same `!%1` logic shape).

## Acceptance clauses (ITEM profile)

Done (data/reference): row 12, type 6, v57, 227 fields; category `Cannon`; description `Meson Energy Bomb`; mass f[7]=0, buy f[8]=9000, sell f[15]=7500 (raw); logic `!%1` = percent reference 1, no hull skills, no exchange; held category f[20]=5 candidate to CFG row 5 (Limit 1, Extended -1, rogue.cfg line 2975), mapping unproven so `HeldCategoryMapping` stays unresolved; ammo f[31]=0, explode f[142]=0, empty event string f[147], no chain; eleven asset pairs (five non-None) kept as raw text with BLO members unread; BLO files Ebombs.blo, ExplosionEffects.blo, WeaponSounds.blo present with exactly the item's case; no reverse or vehicle references; 99 named / 128 unnamed field split equals the other type-6/227 rows; NWS/Rogue.nws line 318 is the only "Meson" mention (3 round burst note, no numbers).
Remaining (box stays OPEN): the 3 round burst and all firing/damage/energy behaviour, shop and store behaviour, held-category meaning, BLO member content, presentation, native verification.

## Files

-   tests/Unit/OriginalMesonCannonTests.cs (new, 7 tests)
-   shared/RogueTrader.OriginalData/plan.d/rt-f087.md
-   this log

## Results

-   Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 1084` (pinned eb1514ea) supplied every item value; CFG limit line and NWS line number from direct grep of rogue.cfg and Rogue.nws.
-   `dotnet test tests/Unit --filter FullyQualifiedName~OriginalMesonCannonTests` with RT_ORIGINAL_ASSETS_ROOT set: Passed 7, Failed 0, Skipped 0.
-   check-trackers: see below / PR.

## Notes

-   The audit tool's NWS section lists only cannon-category lines; the Meson-specific line 318 was found by direct search and is pinned in its own test.
-   Unlike MIRV/Plasma Bomb there is no lower-case BLO file-name mismatch (Ebombs, ExplosionEffects, WeaponSounds match exactly).

## Not verified

-   CI result pending; burst behaviour, shop, native behaviour.

## Status

wip -> ready.
