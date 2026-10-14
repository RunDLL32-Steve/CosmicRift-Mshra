# Derived original sprite API

The generated `artifacts/original-assets/runtime.json` is a compact catalog; the full `index.json` retains palettes, frame crop metadata and unknown fields. Staging adds `map` and `staging` objects only to `client/OriginalContent/runtime.json`; generated metadata is read-only during staging.

`runtime.sprites` contains each original sprite's `assetKey`, `archiveName`, `sourcePath`, source SHA-256, exact `objectName`, `objectId` (zero-based BLO directory ordinal), CFS version, cell width/height, row/column/frame counts, raw animation time, row meaning, blit/lighting/shadow/compression values, transparent index, relative `atlasPng`, and PNG SHA-256. `runtime.ships` retains original VEH ID/name/row and fields 382/383 archive/member tokens, uniquely resolved to the matching `assetKey`.

Use a regular grid: frame index = row × columnCount + column; source region = `(column × width, row × height, width, height)`. Original crop offsets/reflections are already pasted into each original-sized cell. Ship direction pixel inspection shows row 0 up, 16 right, 32 down and 48 left. Keep columns independently selectable; original pose evaluator semantics remain unverified.

The staged `runtime.map` contains `sourcePath`, `sourceSha256`, `sourceCommit`, original byte length, native `width`/`height`, relative `png` (`Rogue.png`), output PNG SHA-256, decoded RGB pixel SHA-256, and a role explicitly limited to overview presentation.

Important concrete references:

| Asset | PNG | Cell / grid |
|---|---|---|
| Asp | `TerranShipsSmall/Asp.png` | 54×54 / 64 rows, 11 columns |
| Hornet | `TerranShipsSmall/hornet.png` | 54×54 / 64 rows, 11 columns |
| Small Skithzar/Grazi hulls | Exact catalog paths | 54×54 / 64 rows, 11 columns |
| Tiny hull counterparts | `rogueships/<original-name>Tiny.png` | 34×34 / 64 rows, 11 columns |
| Plasma cannon projectile | `Ebombs/EbulletA.png` | 12×12 / one row, 30 columns |
| Plasma impact | `Ebombs/EBulletHitA.png` | 32×32 / one row, 30 columns |
| Canonical asteroid sprites | `O_Asteroids/RockA.png` through `RockI.png` | Original 24–64 pixel cells / one row, 60 columns |
| Energy panel | `cruiart/gback.png`, `cruiart/gguage.png` | 160×52 / respective one and 64 columns |
| Map overview | `Rogue.png` after staging | Native 1999×1998 RGB |

All exported palette RGB entries remain unchanged. Index 0 is transparent. Source shadow/light index ranges and blit modes remain raw metadata; correct native blending, HSV/user palette transforms, pose selection, animation timing, and all gameplay behavior remain separate fidelity work. UI objects retain their full original cells, including empty margins and segmented components. PNGs and QA sheets are derived artifacts; canonical originals are never edited.
