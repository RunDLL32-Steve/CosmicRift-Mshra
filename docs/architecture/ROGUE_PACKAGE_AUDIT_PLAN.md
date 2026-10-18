# Rogue Trader package audit

RT-F381 **Complete for bounded data/model/transfer audit; native delivery remains unverified**, October 13, 2026. Reconciles with [master §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), original RT-F069 closure and native engine route. Root owns master/catalog reconciliation. Originals remain authoritative and unchanged.

## Completed audit history

- Canonical CFG core filenames resolve to unchanged Rogue.lvl, Rogue.veh, Rogue_mp.itm, Rogue.rpg, Rogue.lio and Rogue.nws. Their hashes match the pristine pinned parser load evidence. All200 item/30 vehicle/12 skill/81 LIO IDs, names and source versions match parsed exports.
- All12 original RPG hull DefaultVehicleId values match raw field3 and existing VEH names. Canonical hull/stat identities are preserved for the native route; this audit does not prove client delivery or port/rebalance them.
- 19,198 direct item property/value checks match source-supported literal CSV assignments and pristine exports. The audit follows actual CSVReader quote behavior and avoids applying general fields to types that omit the general loader. It is a bounded source check, not a complete C# evaluator.
- 1,364 model-supported ammo/required-item/explosion/child/vehicle/skill/inventory links cataloged. Positive absent references are retained; zero/negative IDs remain sentinel/default interpretation pending.
- All222 LVL object/floor archive-member references resolve to exact originals. Of528 parsed-model sprite/sound/selection references,527 resolve; Portal's UtilityEffects/PrizeCrateA remains missing. Raw inventory logo and BallGraphic references are separate CFG candidates; actual eligibility is not inferred from raw presence.
- Native LVLv6 header1999×1998, offsets/stride evidence and all three raw RLE layers cross-check the pinned parser exports: each layer3,994,002 bytes. Native header entityCount22,356, floorCount9, objectCount213. The server parser consumes through byte910,942 and leaves380,052 graphical trailer bytes unread; the original entire1,290,994-byte LVL remains unchanged for native delivery.
- Expected canonical transfer inventory contains46 files (seven core files plus39 BLO files) with original basenames, SHA256, length and CRC32. Independent offline zlib compression/decompression round-trips every file unchanged. Five separately recovered help files require their existing corroborating provenance; they are not mislabeled canonical fowlmouth files.
- Original source transfer route audited: AssetManager registers parsed/core/BLO/news/help files; SC_AssetInfo sends original basenames+CRC; cache checks unchanged fileCRC, compresses original filebytes; CS_StartUpdate returns original basename/uncompressed length/compressed bytes. No original content is rewritten by this route. Actual .NET cache/packet/native receiver delivery has not been executed by this audit.
- Two focused RLE/CRC fixtures pass, malformed/truncated/oversized/zero-run inputs reject. All99 distinct original files hash-identical before/after audit. No world simulation, server or client launched.

## RT-F385 derived original-value fix — Complete

Progress: In progress → Testing → Complete after focused parser regression, October 13, 2026. The minimal derived-only patch adds `vehicleMaker.prizeBountyPoints = CSVReader.GetInt(values[18]);` after the existing field17 radar assignment. It preserves the canonical v57 field18 value without calling the broader general loader or changing other defaults. `prepare-derived-source.ps1` reapplies it from unchanged pinned source and records original/derived SHA256 in the local compatibility manifest.

Root built and ran the controlled loader once after this completed feature fix, with exit0/unchanged inputs. Fixed exports are separate in `.tools/compatibility/reports/vehiclemaker-fixed`; pristine reports remain baseline evidence. `check-vehiclemaker-fix.py` verifies exactly eight fields change0→1 on IDs45/46/47/48/49/83/84/85; every other property across all200 item exports remains identical. Vehicles/skills/LIO/config/level exports are identical to the five pristine exports. Negative checks reject an incorrect corrected field and an unrelated changed value.

The fixed package audit independently retains19,198 source-supported equal property checks and12 exact hull links, with zero remaining nonzero common-field omission mismatches. All99 canonical original hashes remain unchanged; foundation verifier passes46 records. This is a parser model fix, not evidence that the native gameplay bounty evaluator or full reproduction works. Missing drop-item66 eligibility (RT-F386), native delivery and all other outstanding items remain open.

```powershell
python scripts/compatibility/check-vehiclemaker-fix.py
python scripts/compatibility/audit-rogue-package.py --parser-reports .tools/compatibility/reports/vehiclemaker-fixed --output artifacts/rogue-package-audit/vehiclemaker-fixed-audit.json
```

Evidence: `artifacts/rogue-package-audit/vehiclemaker-fix.json`, `vehiclemaker-fixed-audit.json`, `.tools/compatibility/logs/vehiclemaker-fixed-build.log` / `vehiclemaker-fixed.log`; controlled stderr empty. No world/server/client simulation was run for this parser fix. The generic loader wrapper defaults to pristine report paths: use a separate report directory for future derived checks to preserve historical evidence.

## Concrete unresolved work

- [ ] Establish eligibility/behavior of missing positive DropItemId66 on parsed vehicle425/426/427. The original item catalog lacks66; no invented item or alias is substituted.
- [ ] Resolve Portal UtilityEffects/PrizeCrateA source revision/fallback and actual native evaluation. Existing RT-F182/F377 context applies.
- [ ] Prove native client receives the46 canonical transfer files plus five separately recovered help files unchanged, including full original LVL graphical trailer. Independent zlib roundtrip does not prove actual packet/download/receiver behavior.
- [ ] Verify asset cache filename casing against native requests. Upstream BLO request list lowercases tokens; cache dictionary uses case-sensitive basenames. No code change is justified without a concrete failed native request.
- [ ] Interpret unaccounted fields and remaining version-specific model/evaluator semantics. ItemFieldCoverage records literal/known-helper coverage, not blanket claims of ignored fields. Vehicle sequential parser full end coverage remains unvalidated.
- [ ] Validate original map rendering/entities, LIO interactions and ship/item gameplay in the native engine route after package setup. Audit identity links do not complete runtime parity or engine selection.

## Reproduction and boundaries

```powershell
python scripts/compatibility/audit-rogue-package.py --self-test
python scripts/compatibility/audit-rogue-package.py
```

Standard-library Python3.11+. Inputs are pinned originals, prior verified raw/dependency indexes and pristine .NET parser exports in `.tools/compatibility/reports`. Output is ignored `artifacts/rogue-package-audit/index.json`. `--parser-reports` selects explicit pristine or derived-fixed parser evidence; `--output` supports a separate JSON path; originals, pinned source, parser reports and raw input indexes cannot be overwritten.

Report schema1 contains identity/hull links, itemValueChecks, itemFieldCoverage, nonzeroCommonFieldOmissions, relationships, level raw-layer/trailer metadata,222 mapMemberLinks,528 modeledBlobLinks, expectedTransferFiles and pinned source evidence hashes. `nativeDeliveryVerified=false` remains explicit. Parent/root handles new feature IDs, major-feature CHANGELOG and current master checkboxes.

RT-F397 (merged to main, 8b34c38) adds actual cache/packet serialization plus both outgoing length overloads and inert reliable queue boundaries; [issue plan](../../scripts/compatibility/NativeDeliveryChecks/IMPLEMENTATION_PLAN.md). Root baseline/fixed runs pass46 canonical packets and60 inert fixtures, preserving full LVL trailer and original hashes. Successful headless payload hashing does not complete RT-F387 native delivery.
