Review status: APPROVED (round 1, senior review local, head f4dc05f)

# cloud/rt-f084-light-mass-driver-pellets

- RT-IDs: RT-F084 (Light Mass Driver Pellets, ITM/Rogue_mp.itm row 9, id 2000, type 4 ammo), data/reference clause only. Gate items 2 and 4. Base: origin/main b5fc80e. Not stacked. Run locally (not cloud).
- Scope: pin the resolved reference oracle with the existing `OriginalItemReferenceResolver` (no production change).

## Acceptance clauses (ITEM profile)
Done (data/reference): row 9, type 4, v57, 31 fields; category `Ammo-Projectile`; description; mass f[7]=0, buy f[8]=1, sell f[15]=1 (raw); empty logic; no ammo/explode/entry fields; single asset pair f[23..30] None/None with six zero ints; only reverse reference is Light Mass Driver (row 86, id 1028) f[31].
Remaining (box stays OPEN): ammo consumption/firing behaviour, shop and store behaviour, presentation, native verification.

## Files
- tests/Unit/OriginalLightMassDriverPelletsTests.cs (new, 4 tests)
- shared/RogueTrader.OriginalData/plan.d/rt-f084.md
- this log

## Results
- Independent evidence: `python -I scripts/original-assets/audit_rt_f076.py <assets> 2000` (pinned eb1514ea) matches every value.
- `dotnet test tests/Unit --filter OriginalLightMassDriverPellets` with RT_ORIGINAL_ASSETS_ROOT set: Passed 4, Failed 0, Skipped 0.
- check-trackers: see PR.

## Not verified
- CI result pending; native behaviour, shop, ammo consumption.

## Status
wip -> ready. Next entry: RT-F085 Mass Driver (same resolver).
