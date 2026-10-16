Review status: UNREVIEWED

# cloud/rt-f024-veh-layer-refs

- Claims: RT-F024; gate items 2, 5.
- Base: origin/main ac41dc9.
- Scope: raw VEH explosion/thrust layer tuples and sound references for the 12 hulls (data only).
- Status: done for the data-only slice (development run 2026-10-16 14:20 UTC); RT-F024 stays unchecked.

## Acceptance clauses (catalog RT-F024: "Resolve exact members and animation/palette/crop/layer timing; verify thrust/death/rotation presentation and audio for every hull")

Done (data only):
- Layer and sound tuple positions identified and verified identical in all 12 hull Car rows (VEH/Rogue.veh v53, 454 fields): graphic layers = 7 x 8 fields at f[382], f[390], f[398], f[406], f[414], f[422], f[430] (`archive, member, i0..i5`); sound references = 4 x 4 fields at f[438], f[442], f[446], f[450] (`archive, member, i0, i1`). Text (non-integer) fields at f>=374 are exactly these 22 positions in every hull.
- Per-hull values pinned for all 12 hulls (roll, death explosion, thrust members and ints; empty slots; sound refs).
- Catalog Asp tuples reproduced exactly: f[398..405] `ExplosionEffects,DeathExplosion03_,0,0,0,0,0,85`; f[406..413] `ExplosionEffects,thrust3,0,0,0,0,-10,51`; f[442..443] `UtilitySounds,Thrust`; f[446..447] `UtilitySounds,Rev`.
- Archive name -> `BLO/<archive>.blo` filename existence (case-insensitive, filename only) for the six referenced archives, all present in the clone: ExplosionEffects, GraziShipsSmall, SkithzarShipsSmall, TerranShipsSmall, UtilitySounds, WeaponSounds. Sound slot 0 (f[438..441]) and layer slots 1, 4, 5, 6 are `None,None,0...` in all 12 hulls.
- Naming by evidence only: slot 0 "Roll" (catalog RT-F023), slot 2 "Death explosion", slot 3 "Thrust" (catalog RT-F024 and FIELD_INTERPRETATION "broken/thrust sequences"); other slots position-named. The six layer ints follow the SpriteBlob.ParseV3 order from FIELD_INTERPRETATION (LightPermutation, PaletteOffset, HSV, AnimationTime); only HSV Value (5th) and the animation time (6th, 85/51 for Asp) are named by that document; units, scaling and effect stay undecoded and are exposed as raw `OriginalInt`.
- Other archive/member-looking pairs in the record: only `None,None` at f[74..75] and f[86..87] in all 12 hulls (meaning unknown, not exposed). f[380]=8, f[381]=30 precede the first layer in all 12 (meaning unknown).

Remaining (not done):
- BLO member existence (BLO content is Git-LFS only here: not verified), exact member resolution inside the archives.
- Animation, palette, crop, layer timing semantics; integer meanings (including the sound tuple ints, all 0 in the pinned file).
- Which game event plays which sound slot; thrust/death/rotation presentation and audio verification for every hull (local/native). Box RT-F024 not ticked.

## Files touched
- shared/RogueTrader.OriginalData/OriginalVehicleLayers.cs (new: `OriginalVehicleAssetRef`, `OriginalVehicleLayers`, `OriginalBloFiles`)
- tests/Unit/OriginalVehicleLayerTests.cs (new: 3 synthetic + 15 oracle tests)
- scripts/original-assets/audit_rt_f024.py (new, independent stdlib audit; prints RT_F024_AUDIT_OK)
- shared/RogueTrader.OriginalData/plan.d/rt-f024.md (new)
- this log

## Verified in cloud
- `python3 -I scripts/original-assets/audit_rt_f024.py /tmp/claude-0/assets/repo` printed RT_F024_AUDIT_OK; its output was used to write the test oracle.
- Offline xunit-compatible shim (apt SDK 8.0.131, not the pinned 8.0.419 / real xunit): filter OriginalVehicleLayer: Passed 18, Failed 0, Skipped 0; full suite: Passed 316, Failed 0, Skipped 0, Total 316 (main baseline 298). No compiler warnings seen in the shim output.
- `python3 scripts/check-trackers.py`: TRACKERS_CONSISTENT (checklist 397 entries, 22 complete, 375 open; unchanged).

## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 with the real xunit run (`dotnet test tests/Unit`).
- Any BLO/LVL-dependent check: archive members, sprite/animation/palette/crop data, audio contents (BLO is LFS pointers only here); the BLO check here is filename-only.
- All native behaviour: thrust/death/rotation presentation and sounds in the original client.

## Resume point
Next step for RT-F024 needs the real BLO content (local): resolve each (archive, member) in ExplosionEffects, the three *ShipsSmall archives, UtilitySounds and WeaponSounds, then decode timing/palette/crop and verify presentation/audio against the original client before ticking.

## Coordinator review (development run 2026-10-16 14:20 UTC)
Implementation delegated to a Sonnet 5.5 High agent (matrix: data loaders from approved specs). The coordinator read the full diff, spot-checked the pinned values with its own Python read of the pinned files, re-ran the branch's audit script (exit 0) and re-ran the whole unit suite: offline xunit-compatible shim, apt SDK 8.0.131, RT_ORIGINAL_ASSETS_ROOT = pinned clone eb1514ea -> Passed 316, Failed 0, Skipped 0 (main baseline 298). check-trackers: TRACKERS_CONSISTENT. Accepted for senior review; no registry box ticked. The real xunit run on the pinned SDK 8.0.419 is not verified in cloud - local CI required (nuget.org is blocked here).
