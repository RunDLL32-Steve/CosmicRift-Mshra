Review status: APPROVED (senior review round 1, head 43af34e)

# cloud/rt-f082-plasma-bomb-item

-   RT-IDs: RT-F082 (Plasma Bomb, ITM/Rogue_mp.itm row 7, id 1026), data/reference clause only. Gate items 2 (formats/sentinels) and 4 (record identities/dependency closure). Base: origin/main da0aba5. Not stacked.
-   Scope: pin the full resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production code change).

## Acceptance clauses (ITEM profile)

Done (data/reference): full versioned record (row 7, type 6, v57, 227 fields, category `Energy Bomb Projectors`, mass 0, buy 5000, sell 3500 raw); skill logic `!%1` = percent ref 1 only, no hull skills; held category f[20]=6 with CFG [HeldCategory] row 6 (Limit 1 / Extended -1, line 2977, mapping unproven); ammo f[31]=0 and explode f[142]=0 so no chain; 11 asset pairs, 5 non-None (WeaponEffects/EnergyBoltD, Ebombs/EBombTrail, ExplosionEffects/bombhit2\_, WeaponSounds/bomb2, WeaponSounds/Explode1), BLO never read (`weaponeffects.blo` is lower case in the package); no reverse references.
Remaining (box stays OPEN): firing behaviour, bomb spawn/flight/damage, presentation and sounds, BLO members (queue C1), held-category mapping, meaning of percent ref `%1`, shop/native verification. Unexplained fields stay raw.

## Files

-   tests/Unit/OriginalPlasmaBombTests.cs (new, 6 tests)
-   shared/RogueTrader.OriginalData/plan.d/rt-f082.md
-   docs/development-log/cloud/cloud-rt-f082-plasma-bomb-item.md

## Results

-   Independent evidence: `python3 -I scripts/original-assets/audit_rt_f076.py <assets> 1026` against pinned eb1514ea reproduces every value above; its "referenced by" list is empty; CFG Limit6/ExtendedLimit6 at lines 2977/2978 checked directly.

## Not verified in cloud - local CI required

-   No .NET SDK in this cloud sandbox, so the C# test was not executed here; GitHub Actions compiles it (asset tests SKIP in CI). Chain/Logic assertions for the zero-ammo and percent-only cases follow the resolver code by reading, not by execution. Still local: scripts/verify.ps1 with assets; native behaviour.

## Status

wip -> ready once CI is green. Next entry RT-F083 Repulsor (same resolver).
