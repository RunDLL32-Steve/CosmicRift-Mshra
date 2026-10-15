# Original data implementation plan

Master: [section 18 / M2 first task](../../ROGUE_TRADER_2_PLAN.md), [fidelity rules 1.2](../../ROGUE_TRADER_2_PLAN.md), [feature catalog RT-F001-RT-F012](../../docs/original-research/FEATURE_CATALOG.md#selectable-original-hulls--rt-f001rt-f012), [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md). Created October 15, 2026.

## Accomplished

- Raw record reader (`RawRecordReader`): comma fields kept verbatim by index (padding in `RawText`, trimmed/unquoted `Value`), quoted strings with commas, CRLF/LF, blank-line skipping, trailing comma kept as an empty field, source file, 1-based physical row and `vNN` version token retained.
- Integrity guard (`AssetIntegrity`, `PinnedAssets`): length and SHA-256 of `RPG/Rogue.rpg`, `VEH/Rogue.veh`, `ITM/Rogue_mp.itm` from the manifest are verified on the bytes that are then parsed; failures throw `AssetIntegrityException` (Missing, LengthMismatch, Sha256Mismatch).
- Hull catalog (`OriginalHullCatalog.Load`): the 12 RPG hull rows (v6, f[2]=1) are linked to the VEH row that has f[0]=2 (Car), version v53 and f[2] equal to RPG f[3]; exactly one match and equal names are required (`OriginalHullLinkException`). Asp: RPG row 1 -> vehicle 2002 -> VEH row 20.
- `OriginalHull`: name, skill id, vehicle id, category, complete raw RPG and VEH records, and raw typed values with field indexes: hull points f[6], weight f[7], cargo f[47..48], energy f[94..96], all sixteen speed blocks f[202..361] (ten fields each), bounce f[362], hyper energy cost f[373]. No units, scaling or interpretation are applied; negative values are flagged `IsNegativeSentinel` and returned as-is.
- Tests: [tests/Unit](../../tests/Unit/IMPLEMENTATION_PLAN.md).

## Outstanding

- [ ] Interpretation of units, scaling, sentinel/fallback meaning, cargo and energy (FIELD_INTERPRETATION runtime contracts).
- [ ] ITM loadout/installation restrictions, RPG skill requirements, and the twelve type-16 hull exchange items (RT-F249-RT-F260).
- [ ] Runtime adapters into simulation/client; none of RT-F001-RT-F012 is ticked until native selection, loadout and movement are verified.
- C2 + RT-F046 (data only, cloud/rt-f046-c2-cfg-terrain-access, review pending): `OriginalCfg` raw-preserving INI reader (pinned CFG/rogue.cfg hash guard; 144 sections, duplicates kept, no interpretation) and `OriginalTerrainAccess` (16 Terrain blocks: store disabled in 0/9/15, skill shop disabled in 0/9/10/15, team change disabled in 0/9/15). Enforcement, store UI, loadouts, prices and safezone rules unverified; RT-F046 box stays open.
- RT-F050 (data only, cloud/rt-f050-class-change-prune, review pending): `OriginalClassChangePrune` pins the [Arena] flags DeleteWeaponsOnClassChange=1 (line 72), DeathForceUtilityOff=1 (75), TeamSwitchMinEnergy=500000 (83). Event order, units, NWS 218-220 loss list and inventory/score/cash handling unverified; box stays open.
