Review status: UNREVIEWED

# cloud/rt-f077-f080-micromissile-children

- RT-IDs: RT-F077 (MicroMissile-Child-Left1, id 1045, ITM row 2), RT-F078 (Left2, 1048, row 3), RT-F079 (Right1, 1046, row 4), RT-F080 (Right2, 1049, row 5), data/reference clause only. Gate items 2 (formats/sentinels) and 4 (record identities/dependency closure). Claimed by 2cd7b50.
- Stacked on cloud/rt-f076-plasma-cannon-item (PR #45): uses `OriginalItemReferenceResolver` from shared/RogueTrader.OriginalData/OriginalItemReferences.cs. Merge #45 first; this PR is then retargeted to main. Base chain: origin/main 3452d48 -> RT-F076 2a6523e -> claim 2cd7b50.
- Scope: pin the full resolved reference oracle for the four child projectile rows with the existing resolver. The resolver was NOT modified (no genuine gap): no C# production file changed. Parent features are RT-F081 (MIRV Missile Launcher 1047) and RT-F040 (MIRV chain, `OriginalWeaponChain`); their acceptance is not claimed here. Field names that come from the InfantryOnline emulator parser are upstream-parser names, not original-executable evidence.
- Not written by design: no LIO reader (queue G6), no BLO reader (queue C1).

## Acceptance clauses (ITEM profile)
Done (data/reference), identical for all four unless stated:
- Full versioned record: ITM/Rogue_mp.itm rows 2..5, type 6, v57, 227 fields, all preserved; category ChildProjectiles; description (f[6], inferred position) "Low microton fusion warhead."; mass f[7]=1000, buy f[8]=0, sell f[15]=-1 (raw, `IsNegativeSentinel`; native meaning NOT established); f[5] empty (`SkillLogicEmpty`, no skill ids, no `%n`).
- Held category: f[20]=0; no CFG [HeldCategory] row has id 0 (Limit=null), listed as unresolved `HeldCategoryMapping`; nothing enforced.
- Ammo/child/vehicle/blob references: f[31]=0, f[142]=0, so no chain, no type-8 entries of their own; f[147] event string empty; no VEH-id field. 11 structural asset pairs, 5 non-None: f[183..190] TerranWeapons,MissileF (ints 0,0,95,0,-5,0); f[199..206] ExplosionEffects,contrail3 (0,0,90,-5,-5,50); f[207..214] ExplosionEffects,canonXplosionSm (0,0,90,0,0,80); f[215..218] WeaponSounds,missile1 (4,0); f[219..222] WeaponSounds,Explode1 (3,0); None,None tuples at f[23] (ints f[25..30]), 71, 79 (ints ...0,1), 175 (24,1,0,0,0,0), 191, 223. BLO content never read; the five members are listed as unresolved `BloMember`. Unresolved list per child is exactly 6 entries (1 HeldCategoryMapping + 5 BloMember).
- Field accounting: 99 of 227 fields named by existing maps, 128 raw only (same set for the four rows).
- Symmetry facts (raw, no interpretation): the four rows differ only in f[2], f[3] and five other fields, in id order 1045/1048/1046/1049: f[27] = 0,90,0,0; f[30] = 0,0,50,0 (both inside the first None,None tuple's trailing ints); f[101] = 1500,1250,1500,1250; f[113] = -350,-350,350,350; f[114] = 75,50,-75,-50. Every other field (category, description, mass/buy/sell, all other asset ints) is identical across the four.
- Reverse references (resolver, with row and field index): only ITM/Rogue_mp.itm row 16 id 3008 "MIRV Missile-Blast" (type 8) entry list, stride 6 from f[97], raw offset at entry +4: 1045 at f[97] (offset -5) and f[109] (-15); 1048 at f[103] (-10); 1046 at f[115] (5) and f[127] (15); 1049 at f[121] (10). Entry order 1045,1048,1045,1046,1049,1046 = the existing RT-F040 chain entries. 3008 is reached only from row 6 id 1047 "MIRV Missile Launcher" f[142] (ammo f[31]=2002 "MIRV Missiles", type 4 row 82). No ITM ammo/explode link, no hull drop item, no event-string/addinv name references a child; no hull starting loadout holds one.
- The same four ids appear in the other 11 ITM files (Cyg, Rogue, ballgame, chaos-extended, chaosadvanced, core, core-test, lame, sfctf, totalwar): category TerranChildWeapons in most, Unknown in chaosadvanced, byte-identical rows 2..5 in Rogue.itm and Rogue_mp.itm; only listed by file/line in the audit, not read by the loaders.
Remaining (boxes stay OPEN): the original child behaviour (spawn offsets/angles, velocity, lifetime, damage, trail/explosion presentation, sounds); BLO members (queue C1); sell -1 meaning; held-category mapping; parent launcher/MIRV chain acceptance (RT-F081, RT-F040); native verification. The 128 raw fields stay unexplained.

## Visible mismatches
- ITM names archive `TerranWeapons`; the package file is `BLO/terranweapons.blo` (lower case). Case-insensitive match only; exact-case lookup fails on a case-sensitive file system (the C# test therefore locates it case-insensitively). The other archives (ExplosionEffects, WeaponSounds) match exactly. Probably harmless on Windows; not asserted as native behaviour.
- The catalog says only mass/buy/sell/logic; it does not mention f[20]=0, f[31]/f[142]=0, the 5 non-None asset pairs, the type-8 parent or the case mismatch (all additional).
- Catalog cell for the "ChildProjectiles" category holds 27 rows in the pinned file; only these four are rows 2..5.

## Files
- tests/Unit/OriginalMicroMissileChildTests.cs
- scripts/original-assets/audit_rt_f077_f080.py
- shared/RogueTrader.OriginalData/plan.d/rt-f077-f080.md
- docs/development-log/cloud/cloud-rt-f077-f080-micromissile-children.md

## Results
- NuGet is blocked in the cloud; xunit-compatible reflection shim (copy `xshim-f077`), pinned assets eb1514ea, global.json unmodified.
- Filter `OriginalMicroMissileChildTests`: passed 11, failed 0, skipped 0.
- Full set: passed 523, failed 0, skipped 5 (RT-F076 base 512/0/5, +11).
- `python3 scripts/check-trackers.py`: TRACKERS_CONSISTENT.
- `python3 -I scripts/original-assets/audit_rt_f077_f080.py /tmp/rtassets/src` reproduces the values independently.

## Not verified in cloud - local CI required
- Real xunit `dotnet test tests/Unit` on pinned SDK 8.0.419; scripts/verify.ps1; native behaviour of the child projectiles; the exact-case BLO file name on Windows.

## Status
done for the reference-resolution clause of all four; boxes stay open. Resume point: after PR #45 merges retarget this PR to main; next work needs the BLO reader (C1) and a child spawn/motion/damage oracle (native or simulation) together with RT-F081 and RT-F040 parent acceptance.
