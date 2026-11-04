# Original asset tools

Purpose/current progress: [decoder/staging implementation plan](IMPLEMENTATION_PLAN.md) and [dependency inventory](DEPENDENCY_PLAN.md) document completed selected11-archive/231-object/36,516-frame exports, staging and all89-archive/1,262-member/609-candidate inventory. Next work is native rendering/evaluator/dependency closure; source/standalone inspector rendering is verified but original native gameplay remains open. Originals are read only; outputs are derived and ignored. Reconciled October 13, 2026.

# Derived original sprite API

The generated `artifacts/original-assets/runtime.json` is a compact catalog; the full `index.json` retains palettes, frame crop metadata and unknown fields. Staging adds `map` and `staging` objects only to `client/OriginalContent/runtime.json`; generated metadata is read-only during staging.

`runtime.sprites` contains each original sprite's `assetKey`, `archiveName`, `sourcePath`, source SHA-256, exact `objectName`, `objectId` (zero-based BLO directory ordinal), CFS version, cell width/height, row/column/frame counts, raw animation time, row meaning, blit/lighting/shadow/compression values, transparent index, relative `atlasPng`, and PNG SHA-256. `runtime.ships` retains original VEH ID/name/row and fields 382/383 archive/member tokens, uniquely resolved to the matching `assetKey`.

Use a regular grid: frame index = row × columnCount + column; source region = `(column × width, row × height, width, height)`. Original crop offsets/reflections are already pasted into each original-sized cell. Ship direction pixel inspection shows row 0 up, 16 right, 32 down and 48 left. Keep columns independently selectable; original pose evaluator semantics remain unverified.

The staged `runtime.map` contains `sourcePath`, `sourceSha256`, `sourceCommit`, original byte length, native `width`/`height`, relative `png` (`Rogue.png`), output PNG SHA-256, decoded RGB pixel SHA-256, and a role explicitly limited to overview presentation.

Important concrete references:

| Asset                      | PNG                                         | Cell / grid                                      |
| -------------------------- | ------------------------------------------- | ------------------------------------------------ |
| Asp                        | `TerranShipsSmall/Asp.png`                  | 54×54 / 64 rows, 11 columns                      |
| Hornet                     | `TerranShipsSmall/hornet.png`               | 54×54 / 64 rows, 11 columns                      |
| Small Skithzar/Grazi hulls | Exact catalog paths                         | 54×54 / 64 rows, 11 columns                      |
| Tiny hull counterparts     | `rogueships/<original-name>Tiny.png`        | 34×34 / 64 rows, 11 columns                      |
| Plasma cannon projectile   | `Ebombs/EbulletA.png`                       | 12×12 / one row, 30 columns                      |
| Plasma impact              | `Ebombs/EBulletHitA.png`                    | 32×32 / one row, 30 columns                      |
| Canonical asteroid sprites | `O_Asteroids/RockA.png` through `RockI.png` | Original 24–64 pixel cells / one row, 60 columns |
| Energy panel               | `cruiart/gback.png`, `cruiart/gguage.png`   | 160×52 / respective one and 64 columns           |
| Map overview               | `Rogue.png` after staging                   | Native 1999×1998 RGB                             |

All exported palette RGB entries remain unchanged. Index 0 is transparent. Source shadow/light index ranges and blit modes remain raw metadata; correct native blending, HSV/user palette transforms, pose selection, animation timing, and all gameplay behavior remain separate fidelity work. UI objects retain their full original cells, including empty margins and segmented components. PNGs and QA sheets are derived artifacts; canonical originals are never edited.

# Original map art

`stage_level_art.py` stages the sprites `LVL/Rogue.lvl` places on the map (asteroid rocks, space stations, walls, gas clouds, safe-zone markings, map border) and the mineral prize sprites named by `ITM/Rogue_mp.itm` fields 23 and 24. It checks `LVL/Rogue.lvl` and `ITM/Rogue_mp.itm` against the canonical manifest and each BLO against `shared/RogueTrader.OriginalData/PinnedBloArchives.cs`, before and after reading. Output goes to the git-ignored `client/OriginalContent/level/`: one indexed PNG atlas per object with its original palette, and `level.json`, which the client's `OriginalLevelArt` draws from.

`level.json` holds `objects` (the LVL object table in order: archive, member, PNG, cell size, rows, columns, frame count, CFS animation time), `minerals` (item id to prize sprite) and `entities`, a flat array of six integers per placed sprite: `x, y, object, animationTime, phase, frame`. The entity reading is RECONSTRUCTED from a test render (rocks sit on their collision tiles, station trusses join up): x and y are the sprite cell's top-left in map pixels, object is uint16 at byte 4 masked to 13 bits and frame its top 3 bits (only the safe-zone markings use it), animationTime is uint16 at byte 6 in hundredths of a second per cycle (0 = a still frame) and phase is byte 14 in percent. Native blending (RT-F074) is not applied, so translucent originals such as gas clouds draw solid.

Run `python scripts/original-assets/stage_level_art.py` after `scripts/sync-reference-assets.ps1`, then let Godot import (`scripts/run-client.ps1` does both unless `-NoBuild` skips the import). `run-client.ps1` calls `python` as is, without the `PYTHONUSERBASE` setup `export-windows.ps1` uses; if Pillow is installed only in that user base, auto-staging prints a warning and the game keeps its NEW look until the script is run by hand. The client loads every atlas at start and falls back to the NEW look, with a warning, when the folder is partly staged or not imported.

The script also reports `uncoveredBlockedTiles`: blocked tiles that no placed sprite's cell overlaps (4,373 of 116,065 on Rogue.lvl, mostly 4 × 12-tile spans between station pylons and transoms). Whether the original drew anything there is unknown, so the client keeps the NEW wall colour on exactly those tiles rather than show invisible collision. `test_level_art.py` covers the reader with synthetic bytes.
