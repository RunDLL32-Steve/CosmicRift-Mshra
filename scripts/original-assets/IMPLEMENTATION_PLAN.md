# Original asset decoder and staging plan

This subplan reconciles with [master §1.2](../../ROGUE_TRADER_2_PLAN.md#12-user-fidelity-clarification-and-implementation-tracking--october-12-2026), [§6 original ship catalog](../../ROGUE_TRADER_2_PLAN.md#6-spacecraft-and-loadout-system), [§16 graphics](../../ROGUE_TRADER_2_PLAN.md#16-graphical-direction), and the [§18 exact reproduction gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). [PROGRESS.md](../../docs/development-log/PROGRESS.md) indexes evidence; the master remains the status authority.

## Completed and verified

- Read canonical source records and inspect pinned primary BLO/CFS format sources without copying or executing upstream implementations.
- Implement original Python BLO v1/v2 table parsing with directory/payload bounds, overlap, duplicate-name and safe-path rejection.
- Implement CFS v2-v5 headers, palette, frame directory, scanline nibble RLE, extended lengths, raw uncompressed pixels and signed-dimension reflection. Actual selected originals contain CFS v3/v4/v5.
- Preserve original frame/canvas dimensions, crop X/Y, row/column order, raw palette words, user data, flags, descriptions, unknown bytes, and original names/table ordinals.
- Export palette-indexed PNGs with original RGB entries and index-zero transparency. Every PNG round-trips its original decoded palette and pixel-index bytes.
- Decode all 12 Small hull sprites, all 12 Tiny counterparts, canonical asteroids, plasma/projectile/effect assets, resources, and original UI sprite objects.
- Verify all selected input SHA-256/byte lengths against the pinned manifest before decoding; re-hash inputs after export and confirm unchanged originals.
- Generate full `index.json` and compact `runtime.json`; resolve all 12 original VEH sprite pairs at fields 382/383 uniquely to exact BLO member names and table ordinals.
- Inspect actual contact sheets for all 231 exported objects plus directional strips for every Small/Tiny ship. Observe clockwise directions beginning upward; retain all 11 pose columns without assigning unverified meanings.
- Preserve the full preset index/runtime during partial diagnostic exports. Regression confirms the full index hash remains identical after an O_Asteroids-only run.
- Stage generated PNGs byte-for-byte outside originals, verifying every atlas against re-decoded canonical CFS pixels/palette/transparency and original header/table metadata.
- Stage canonical `Rogue.bmp` as a native 1999×1998 RGB PNG with exact decoded RGB pixels and provenance in the staged runtime catalog.
- Run 10 decoder tests and four focused staging tests. Validate malformed/truncated data, RLE bounds, signed reflection, output guards, altered originals, changed generated pixels with forged metadata hash, and wrong member identities.
- Inspect the actual staged map preview. No world simulation or engine application was run for these parser/export features.

## Still pending

- [ ] Establish native-client bank/pose-column selection. Column 5 looks visually balanced but is not proven to be the original neutral state.
- [ ] Implement native user-palette/HSV transformations, shadow/light blend modes and all original sprite blit behavior. Raw source palette/index values are retained; no approximate recoloring is applied.
- [ ] Validate exact native animation timing and state evaluators. CFS animation-time values are preserved raw; preview-tool timing is not sufficient proof of original runtime behavior.
- [ ] Decode audio and other non-CFS payloads. Zero unsupported entries in this selected batch does not establish support for all BLO archives.
- [ ] Resolve all remaining content dependencies outside this selected sprite batch and prove complete runtime record closure.
- [ ] Validate native LVL/LIO collision/terrain/object execution and original renderer behavior. Actual parser/arena initialization is Complete in the compatibility plans; the BMP preview does not prove playable collision behavior.
- [ ] Validate exact native rendered appearance/gameplay evidence. Source and standalone Godot inspector input plus three rendered pages are Complete (RT-F073/RT-F373/RT-F374), world tick 0; native palette/blit/pose acceptance remains open.
- [ ] Declare full original visual/gameplay parity. This exporter establishes preserved source pixels and identities, not complete native rendering or gameplay parity.

## Reproducible commands

From the repository root, use Python 3.11+ with Pillow (the current environment provides Python 3.11.5 / Pillow 9.3.0):

```powershell
python scripts/original-assets/test_decoder.py
python scripts/original-assets/test_staging.py
python scripts/original-assets/decode_blo.py
python scripts/original-assets/stage_original_assets.py
```

The default decoder batch contains 11 archives and writes `artifacts/original-assets/index.json`, `runtime.json`, original indexed atlases, and QA contact/direction sheets. The staging default writes `client/OriginalContent/runtime.json`, the unchanged generated PNG bytes, and `Rogue.png`. Originals remain under `.tools/references/cosmic-rift-assets` unchanged.

A diagnostic subset such as `--archives O_Asteroids` writes `index-selected-<selection-hash>.json` and retains the full preset's index/runtime. `--source-root`, `--manifest`, and `--out` support explicit locations. Output inside originals and child-path traversal/symlink escapes are rejected. Staging must also remain separate from the generated export root.

## Actual evidence — October 13, 2026

The full preset decoded **231 CFS objects / 36,516 frames across 11 original BLO archives**, including 24 hull sprite variants and nine RockA-I asteroid objects. Every indexed PNG preserves exact decoded pixel-index bytes, original RGB palette and index-zero transparency. Staging verifies 13 original source files (11 BLO archives, the VEH file and BMP), all 12 exact ship references, and 231 atlas copies. The map is exactly 1999×1998 RGB. Original and generated source metadata hashes remain unchanged.

Selected archives: TerranShipsSmall, SkithzarShipsSmall, GraziShipsSmall, rogueships, Ebombs, Resources, ExplosionEffects, weaponeffects, rogueweapons, cruiart and O_Asteroids.

Small hull cells are 54×54; Tiny hull cells are 34×34. Both have 64 directional rows and 11 pose columns (704 cells each). Pixel inspection establishes row 0 up, 16 right, 32 down and 48 left. Asteroid cells are RockA 64, RockB 48, RockC 64, RockD 48, RockE 24, RockF 64, RockG 48, RockH 32 and RockI 24 pixels square, each with 60 columns.

The full-index preservation regression passed after a selected O_Asteroids export. All 14 focused decoder/staging tests pass. Checks include a generated PNG changed and its declared hash updated: staging still rejects it by comparing canonical CFS pixels directly.

## Format provenance

Format facts were read from [InfantryOnline/Infantry-Online-Tools at 93001c57845269e87b32a2bf042257c7ef61298c](https://github.com/InfantryOnline/Infantry-Online-Tools/tree/93001c57845269e87b32a2bf042257c7ef61298c/InfantryOnline.Tools/Tools.External/Gibbed). The preserved Gibbed format tooling is attributed to Rick (2012); the scripts here are independently written Python, not redistributed upstream source. BLO names/offsets/sizes, CFS packed fields and scanline coding are format facts. Index-zero transparency is corroborated by the preserved [CFS decompiler](https://github.com/InfantryOnline/Infantry-Online-Tools/blob/93001c57845269e87b32a2bf042257c7ef61298c/InfantryOnline.Tools/Tools.External/Gibbed/Gibbed.Infantry.DecompileCFS/Program.cs) and BlobEditor. Original content provenance and exact hashes come from [ASSET_MANIFEST.json](../../docs/original-research/ASSET_MANIFEST.json).

Current dependency inventory RT-F375 is Complete; see [DEPENDENCY_PLAN.md](DEPENDENCY_PLAN.md): all89 BLOs/1,262 members/609 candidates/99 distinct canonical hashes. This directory owns raw decoding/index/staging, not native gameplay; actual bounded full-asset server startup and native welcome-only checkpoint are recorded in [compatibility](../compatibility/IMPLEMENTATION_PLAN.md). No parser tests/pixel exports/simulations were rerun for documentation changes.
