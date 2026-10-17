Review status: APPROVED (round 1, senior review, local)

# cloud/rt-f076-plasma-cannon-item

- RT-IDs: RT-F076 (data/reference clause only). Gate items 2 (formats/sentinels) and 4 (record identities/dependency closure). Base: origin/main 3452d48. Claimed by af5adaf.
- Scope: build a REUSABLE ITM reference resolver (later RT-F077..F099+ single-item entries add one oracle each on top) and pin the Plasma Cannon (ITM/Rogue_mp.itm row 1, id 1004) from the real file. No new field is named: every reference field comes from existing maps (ItmFields, WeaponChainFields, ItmProjectileFields, OriginalSkillLogic, OriginalHullExchange, OriginalHeldCategoryLimit, OriginalEventInventory). Field names that come from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence.
- Not written by design: no LIO reader (queue G6 claimed elsewhere), no BLO reader (queue C1).

## Acceptance clauses (ITEM profile)
Done (data/reference):
- Full versioned record: row 1, type 6, v57, 227 fields, all fields preserved (`Fields`); name Plasma Cannon, category Cannon, description (f[6], inferred position) "Cannon firing bursts of high energy plasma.", mass f[7]=0, buy f[8]=2000, sell f[15]=1500 (positive, so no sentinel), f[5] empty.
- Skill/category/quantity restrictions: f[5] empty -> `SkillLogicEmpty`, no skill ids, no `%n`. f[20]=5 links as a candidate to CFG [HeldCategory] Limit5=1, ExtendedLimit5=-1 (lines 2975/2976); the id-to-ITM-category mapping is not established (RT-F033) and is listed as unresolved. [Soul] UtilitySlots default 10 (resolver exposes it).
- Child/ammo/vehicle/blob references: f[31] ammo 0 and f[142] explode 0 (no chain, no type-8 entries); f[147] damage event string empty; no ITM field is named as a VEH id in any existing map (`VehicleReferences` empty; the audit lists numeric coincidences such as buy 2000 = VEH Hornet id only as coincidences); 11 structural asset pairs, 5 non-None: f[183..190] Ebombs,EbulletA; f[199..206] ExplosionEffects,bullettrail2 (ints 0,0,0,0,-20,0); f[207..214] Ebombs,EBulletHitA; f[215..218] WeaponSounds,gun1,8,0; f[219..222] WeaponSounds,Explode0,3,0; plus empty None,None tuples at f[23], 71, 79 (ints ...0,1), 175 (ints 24,1,0,0,0,0), 191, 223. All three archive files (Ebombs, ExplosionEffects, WeaponSounds) exist under BLO/ as Git LFS pointers; members unverified. All 92 type-6/227 rows have the same 11 pair positions.
- Field accounting: 99 of 227 fields are named by existing maps, 128 are raw only (e.g. f[17]=44800, f[36..51]=60000, f[101]=2200).
- Reverse references: only VEH f[129] `addinv=Plasma Cannon` on Hornet 2000 (row 18), Asp 2002 (20), Excelsior 2011 (28), Ravager 2012 (29), cross-checked against `OriginalStartingLoadout`. No ITM ammo/child/multi-item reference, no hull drop item names it. NWS/Rogue.nws cannot be linked by name or id (no "Plasma"/1004); lines 34, 144-146, 251-254 (one gun/cannon per slot, Cannon category text, basic starting cannon free to remove, store gives nothing) are seen as raw text only.
- Reusable: the same resolver was exercised on MIRV Launcher 1047 (skills 16|15 -> Ravager, Excelsior; ammo 2002; child 3008; 3008's six entries), Mass Driver 1024 (four hulls + %1), type-16 row 181 (target skill 8 = Hornet), and on all 200 items (no unresolved ammo/child/skill/exchange reference; 20 ammo links; 12 hull drop items -> Wreckage 2008; 12 addinv links).
Remaining (box stays OPEN): original effect, firing timing, energy/shield/hull damage, projectile motion, interactions and presentation; the five BLO members (queue C1); held-category id mapping and slot/quantity enforcement; shop purchase/resale flow; native verification. Raw fields with no name (128) stay unexplained.

## Visible mismatches
- Catalog row's "logic f[5]=(empty)" matches; the catalog does not mention f[20], f[31]/f[142] being 0 or the 11 asset pairs (all additional).
- The sell value 1500 differs from buy 2000; a sell of -1 elsewhere is a sentinel candidate, not here.
- ITM/Rogue.itm row 1 is byte-identical text to Rogue_mp.itm row 1 (not used by the loader).

## Files
- shared/RogueTrader.OriginalData/OriginalItemReferences.cs
- tests/Unit/OriginalItemReferencesTests.cs
- scripts/original-assets/audit_rt_f076.py
- shared/RogueTrader.OriginalData/plan.d/rt-f076.md
- docs/development-log/cloud/cloud-rt-f076-plasma-cannon-item.md

## Results
- NuGet is blocked in the cloud; xunit-compatible reflection shim (copy `xshim-f076`), pinned assets eb1514ea, global.json unmodified.
- Filter `OriginalItemReferencesTests`: passed 11, failed 0, skipped 0.
- Full set: passed 512, failed 0, skipped 5 (baseline 501/0/5).
- `python3 scripts/check-trackers.py`: TRACKERS_CONSISTENT.
- `python3 -I scripts/original-assets/audit_rt_f076.py /tmp/rtassets/src` reproduces the values independently.

## Not verified in cloud - local CI required
- Real xunit `dotnet test tests/Unit` on pinned SDK 8.0.419; scripts/verify.ps1; native behaviour of the Plasma Cannon.

## Status
done for the reference-resolution clause; box stays open. Resume point: later single-item entries call `OriginalItemReferenceResolver.Load(root).Resolve(id)` and add one oracle test each; next gameplay work for this item needs the BLO reader (C1) and an effect/timing/damage oracle (native or simulation).
