Review status: UNREVIEWED

# cloud/rt-f083-repulsor-item

- RT-IDs: RT-F083 (Repulsor, ITM/Rogue_mp.itm row 8, id 101), data/reference clause only. Gate items 2 and 4. Base origin/main. Not stacked.
- Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production code change).

## Acceptance clauses (ITEM profile)
Done (data/reference): full versioned record (row 8, type 6, v57, 227 fields, `BaseLimitedUse`, mass 1000, buy 0, sell -1 negative sentinel raw); empty skill logic; f[20]=0 with no CFG [HeldCategory] row; ammo f[31]=101 is itself, explode f[142]=0; 11 asset pairs, 3 non-None (UtilityEffects/SuperMultiPrize, UtilityEffects/RepelB, UtilitySounds/Repel), BLO never read; only reverse reference is its own ammo field.
Remaining (box stays OPEN): repulsion behaviour, presentation/sounds, BLO members (C1), meaning of sell -1 and f[20]=0, shop/native verification. Unexplained fields stay raw.

## Files
- tests/Unit/OriginalRepulsorTests.cs (new, 6 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f083.md
- docs/development-log/cloud/cloud-rt-f083-repulsor-item.md

## Results
- `python3 -I scripts/original-assets/audit_rt_f076.py <assets> 101` against pinned eb1514ea reproduces every value above (referenced-by list: row 8 f[31] only).

## Not verified in cloud - local CI required
- No .NET SDK in this sandbox: the C# tests were not executed here; GitHub Actions compiles them (asset tests SKIP in CI). The self-ammo and no-CFG-row assertions follow the resolver code by reading. Still local: scripts/verify.ps1 with assets; native behaviour.

## Status
ready once CI is green. Next entry RT-F084 Light Mass Driver Pellets.
