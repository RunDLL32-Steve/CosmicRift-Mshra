Review status: APPROVED (round 1, senior review local, head 471dd78)

# cloud/rt-f086-meson-bomb

-   RT-IDs: RT-F086 (Meson Bomb, ITM/Rogue_mp.itm row 11, id 1033, type 6 energy bomb projector), data/reference clause only. Base: origin/main b5fc80e. Not stacked. Run locally (not cloud). Template: RT-F082 `OriginalPlasmaBombTests` and RT-F081 `OriginalMirvLauncherTests`.
-   Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change).

## Acceptance clauses (ITEM profile)

Done (data/reference): row 11, type 6, v57, 227 fields; category `Energy Bomb Projectors`; description `Meson Energy Bomb`; mass f[7]=0, buy f[8]=6000, sell f[15]=4000 (raw); logic `(3|16|9)!%1` = skills 3,16,9 resolving to hulls Kragath, Ravager, Zap'Pir plus percent reference 1, no exchange; held category f[20]=6 candidate with CFG row 6 (limit 1, extended -1, line 2977), mapping unproven so HeldCategoryMapping stays unresolved; ammo f[31]=0 and explode f[142]=0 (no chain), empty event string; 11 asset pairs (5 non-None) as raw text with five BloMember unresolved entries; BLO archive names Ebombs/ExplosionEffects/WeaponSounds present with exact case; no reverse or vehicle references; field coverage 227.
Remaining (box stays OPEN): firing/damage/bomb behaviour, shop and store behaviour, presentation, BLO content decoding (queue C1), native verification.

## Files

-   tests/Unit/OriginalMesonBombTests.cs (new, 6 tests)
-   shared/RogueTrader.OriginalData/plan.d/rt-f086.md
-   this log

## Results

-   Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 1033` (pinned eb1514ea) matches every value; skill-to-hull names (Kragath, Ravager, Zap'Pir) come from the resolver output, Ravager matching the MIRV test for skill 16.
-   `dotnet test tests/Unit --filter FullyQualifiedName~OriginalMesonBombTests` with RT_ORIGINAL_ASSETS_ROOT set: Passed 6, Failed 0, Skipped 0.
-   check-trackers: see PR.

## Not verified

-   CI result pending; native behaviour, shop, bomb firing and damage; the audit script lists only raw NWS text mentions of bombs, none specific to this item.

## Status

wip -> ready. Next entry: RT-F087 (same resolver).
