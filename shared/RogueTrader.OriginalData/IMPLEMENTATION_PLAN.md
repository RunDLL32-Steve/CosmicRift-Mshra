# Original data implementation plan

Master: [section 18 / M2 first task](../../ROGUE_TRADER_2_PLAN.md), [fidelity rules 1.2](../../ROGUE_TRADER_2_PLAN.md), [feature catalog RT-F001-RT-F012](../../docs/original-research/FEATURE_CATALOG.md#selectable-original-hulls--rt-f001rt-f012), [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md). Created October 15, 2026.

## Accomplished

- Raw record reader (`RawRecordReader`): comma fields kept verbatim by index (padding in `RawText`, trimmed/unquoted `Value`), quoted strings with commas, CRLF/LF, blank-line skipping, trailing comma kept as an empty field, source file, 1-based physical row and `vNN` version token retained.
- Integrity guard (`AssetIntegrity`, `PinnedAssets`): length and SHA-256 of `RPG/Rogue.rpg`, `VEH/Rogue.veh`, `ITM/Rogue_mp.itm` from the manifest are verified on the bytes that are then parsed; failures throw `AssetIntegrityException` (Missing, LengthMismatch, Sha256Mismatch).
- Hull catalog (`OriginalHullCatalog.Load`): the 12 RPG hull rows (v6, f[2]=1) are linked to the VEH row that has f[0]=2 (Car), version v53 and f[2] equal to RPG f[3]; exactly one match and equal names are required (`OriginalHullLinkException`). Asp: RPG row 1 -> vehicle 2002 -> VEH row 20.
- `OriginalHull`: name, skill id, vehicle id, category, complete raw RPG and VEH records, and raw typed values with field indexes: hull points f[6], weight f[7], cargo f[47..48], energy f[94..96], all sixteen speed blocks f[202..361] (ten fields each), bounce f[362], hyper energy cost f[373]. No units, scaling or interpretation are applied; negative values are flagged `IsNegativeSentinel` and returned as-is.
- Tests: [tests/Unit](../../tests/Unit/IMPLEMENTATION_PLAN.md).

## Per-item entries

Each registry item's status (done vs remaining clauses, branch, evidence) lives in its own file in [plan.d/](plan.d/), one file per RT-ID, so parallel branches never collide here (AGENTS.md "One file per item").

## Outstanding

- [ ] Interpretation of units, scaling, sentinel/fallback meaning, cargo and energy (FIELD_INTERPRETATION runtime contracts).
- [ ] ITM loadout/installation restrictions, RPG skill requirements, and the twelve type-16 hull exchange items (RT-F249-RT-F260).
- [ ] Runtime adapters into simulation/client; none of RT-F001-RT-F012 is ticked until native selection, loadout and movement are verified.

## Projectile field view (G5 part, RT-F036 / RT-F037 data only) — branch cloud/rt-f036-f037-g5-weapon-records
- `OriginalProjectile` / `ItmProjectileFields`: raw typed view of ITM f[54] fireDelay, f[94] inheritedSpeed, f[101] muzzleVelocity, f[110] aliveTime, f[131], f[135..138], f[142] for the 92 type-6 / 227-field rows; the type-8 rows (145..193 fields, e.g. `None` at f[94]) get no view. Negatives (-550, -65) preserved and flagged.
- Oracle tests (FIELD_INTERPRETATION table: rows 1, 6, 7, 61, 141, 153, 156) in `tests/Unit/OriginalProjectileTests.cs`. Not done: launcher→ammo→child chain (RT-F040), scale/sentinel semantics, native behaviour; RT-F036/F037 stay unchecked.
