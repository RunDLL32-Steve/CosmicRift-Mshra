# Original data implementation plan

Master: [section 18 / M2 first task](../../ROGUE_TRADER_2_PLAN.md), [fidelity rules 1.2](../../ROGUE_TRADER_2_PLAN.md), [feature catalog RT-F001-RT-F012](../../docs/original-research/FEATURE_CATALOG.md#selectable-original-hulls--rt-f001rt-f012), [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md). Created October 15, 2026.

## Accomplished

-   Raw record reader (`RawRecordReader`): comma fields kept verbatim by index (padding in `RawText`, trimmed/unquoted `Value`), quoted strings with commas, CRLF/LF, blank-line skipping, trailing comma kept as an empty field, source file, 1-based physical row and `vNN` version token retained.
-   Integrity guard (`AssetIntegrity`, `PinnedAssets`): length and SHA-256 of `RPG/Rogue.rpg`, `VEH/Rogue.veh`, `ITM/Rogue_mp.itm` from the manifest are verified on the bytes that are then parsed; failures throw `AssetIntegrityException` (Missing, LengthMismatch, Sha256Mismatch).
-   Hull catalog (`OriginalHullCatalog.Load`): the 12 RPG hull rows (v6, f[2]=1) are linked to the VEH row that has f[0]=2 (Car), version v53 and f[2] equal to RPG f[3]; exactly one match and equal names are required (`OriginalHullLinkException`). Asp: RPG row 1 -> vehicle 2002 -> VEH row 20.
-   `OriginalHull`: name, skill id, vehicle id, category, complete raw RPG and VEH records, and raw typed values with field indexes: hull points f[6], weight f[7], cargo f[47..48], energy f[94..96], all sixteen speed blocks f[202..361] (ten fields each), bounce f[362], hyper energy cost f[373]. No units, scaling or interpretation are applied; negative values are flagged `IsNegativeSentinel` and returned as-is.
-   Tests: [tests/Unit](../../tests/Unit/IMPLEMENTATION_PLAN.md).

## Current source coverage — October 17, 2026

The module now extends beyond the initial hull loader. Source inspection confirms `OriginalItemCatalog` (all supported ITM records), `OriginalVehicleCatalog`, `OriginalCfg`, raw flight/collision/energy/weapon/upgrade/actor/visibility views, skill and event token parsers, `OriginalStartingLoadout`/`OriginalHullExchange`, and `OriginalItemReferenceResolver`. These expose raw data and candidate links; they do not execute original gameplay. `OriginalProgressionEvidence` records progression-policy evidence; `OriginalProgressionIds` supplies a NEW decimal-id contract for a separately callable persistence membership validator. It does not recover the historical save schema or apply wipe/exchange rules. See the [source audit](../../docs/audits/2026-10-17-source-docs.md) for exact source locations and inspection limits.

## Per-item entries

Branch names, review labels, shim results and asset-availability notes in older entries describe their authoring checkpoint; they are not a live PR status or a fresh current-tree run. Check current dependencies against source and use master §18 for completion. Each registry item's status (done vs remaining clauses, branch, evidence) lives in its own file in [plan.d/](plan.d/), one file per RT-ID, so parallel branches never collide here (AGENTS.md "One file per item").

## Outstanding

-   [ ] Interpretation of units, scaling, sentinel/fallback meaning, cargo and energy (FIELD_INTERPRETATION runtime contracts).
-   [ ] Execute and verify ITM installation restrictions, RPG skill evaluation and hull exchange behavior (RT-F249–RT-F260). Raw starting-loadout/exchange records and skill syntax parsers already exist; see [RT-F035](plan.d/rt-f035.md) and [RT-F034](plan.d/rt-f034.md).
-   [ ] Runtime adapters into simulation/client; none of RT-F001-RT-F012 is ticked until native selection, loadout and movement are verified.
