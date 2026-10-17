Review status: UNREVIEWED

# cloud/rt-f081-mirv-launcher-item

- RT-IDs: RT-F081 (MIRV Missile Launcher, ITM/Rogue_mp.itm row 6, id 1047), data/reference clause only. Gate items 2 (formats/sentinels) and 4 (record identities/dependency closure). Base: origin/main 85c07bd (RT-F076 resolver and RT-F077..F080 already merged). Not stacked.
- Scope: pin the full resolved reference oracle for the launcher with the existing `OriginalItemReferenceResolver` (no production code change). RT-F040 (MIRV chain) and RT-F077..F080 (children) are separate entries; their acceptance is not claimed here.

## Acceptance clauses (ITEM profile)
Done (data/reference):
- Full versioned record: row 6, type 6, v57, 227 fields; category `Rocket/Missile Systems`; description "Launch System firing missiles packed with mini missiles."; mass f[7]=0, buy f[8]=14000, sell f[15]=5000 (raw).
- Skill logic f[5] `16|15` resolved to hulls Ravager, Excelsior; no percent refs, no exchange target.
- Held category f[20]=9; CFG [HeldCategory] row 9 Limit 1 / ExtendedLimit -1 (line 2983); listed as unresolved `HeldCategoryMapping` (mapping not proven).
- Ammo f[31]=2002 (row 82 "MIRV Missiles", type 4); explode f[142]=3008 (row 16 "MIRV Missile-Blast", type 8) with child entries 1045,1048,1045,1046,1049,1046 (offsets -5,-10,-15,5,10,15 raw); f[147] event string empty.
- 11 asset pairs, 5 non-None (TerranWeapons/MirvMissile, ExplosionEffects/contrail2, ExplosionEffects/canonXplosionSm, WeaponSounds/missile2, WeaponSounds/Explode1); BLO content never read; `terranweapons.blo` is lower case in the package vs `TerranWeapons` in the ITM.
- No record references the launcher (no ITM ammo/explode/entry link, no hull drop, no event/addinv name).
Remaining (box stays OPEN): firing behaviour, missile spawn/flight/damage, presentation and sounds, BLO members (queue C1), held-category mapping, shop/native verification, RT-F040 chain acceptance. Unexplained fields stay raw.

## Files
- tests/Unit/OriginalMirvLauncherTests.cs (new, 6 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f081.md
- docs/development-log/cloud/cloud-rt-f081-mirv-launcher-item.md

## Results
- Independent evidence: `python3 -I scripts/original-assets/audit_rt_f076.py <assets> 1047` against pinned eb1514ea (hash-checked) reproduces every value above; its "referenced by" list is empty.
- `python3 scripts/check-trackers.py`: see below.

## Not verified in cloud - local CI required
- The .NET SDK cannot be downloaded in this cloud sandbox, so the test was not run locally; GitHub Actions job `windows` on d891d74 concluded success (it compiles the test; asset-dependent tests SKIP in CI because CI has no original assets). The asset assertions were therefore checked only against the Python audit output, not by executing the C# test. Still local: scripts/verify.ps1 with assets; native behaviour.

## Status
wip -> ready once CI is green. Resume: fix any compile/assert failure shown by CI; next entry RT-F082 Plasma Bomb (same resolver).
