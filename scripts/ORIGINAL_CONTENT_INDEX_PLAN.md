# Original content index implementation plan

Owner scope: `scripts/index-original-content.py` and this subplan. The generated JSON belongs outside the original reference checkout at `artifacts/research/original-content/index.json` by default.

Reconcile with the master [fidelity clarification, §1.2](../ROGUE_TRADER_2_PLAN.md#12-user-fidelity-clarification-and-implementation-tracking--october-12-2026), [ship data, §6](../ROGUE_TRADER_2_PLAN.md#6-spacecraft-and-loadout-system), [combat, §7](../ROGUE_TRADER_2_PLAN.md#7-combat-system), [resources, §8](../ROGUE_TRADER_2_PLAN.md#8-mining-and-resource-collection), and [§18 M1 exact reproduction gate](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). [PROGRESS.md](../docs/development-log/PROGRESS.md) indexes completion evidence. The master remains the authority: raw indexing is complete, while exact dependency closure and original behavior remain unchecked. Canonical acceptance IDs: [FEATURE_CATALOG.md](../docs/original-research/FEATURE_CATALOG.md), especially RT-F069–RT-F072. Current raw-index feature status: **Complete with evidence**; evidence below is historical, with no new run for documentation reconciliation.

## Completion history — Complete with evidence

- Read only the five canonical Rogue Trader text inputs at pinned commit `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`: CFG, VEH, ITM, RPG, and LIO.
- Verify the manifest commit, checkout commit when Git metadata exists, and exact SHA-256/byte length of all five inputs before indexing.
- Detect compatible UTF-8/BOM or CP1252 decoding without normalization of source hashes.
- Preserve record type, version, identifier, original name, source row, every field as a string, raw field lexemes, and raw record text. CFG preserves all entries, sections, comments and blank rows.
- Retain different observed header positions: VEH v53 / ITM v57 / LIO v23 use type/version/id/name; RPG v6 uses version/id/type and name at field 39.
- Cross-reference the 12 RPG hull names with candidate VEH field-3 IDs and matching names. All 12 match in the pinned package.
- Index 200 original item records, including 112 type-6/8 weapon/effect candidates and seven type-4 Resources records. Credits/repair records remain separate even when their original category also says Resources.
- Record case-insensitive exact filename/BLO-stem candidates against manifest paths while preserving original source spelling and archive-member candidate tokens.
- Record unresolved filename/stem references separately and explicitly count unresolved archive-member candidates.
- Prevent output inside the original folder or over the manifest; write derived JSON atomically outside originals.
- Re-hash every input after parsing; verify original hashes are unchanged.
- Run actual indexing and meaningful parser/integrity/output checks described below.
- [ ] **Investigating — RT-F069–RT-F072:** resolve remaining archive/member identities and complete dependency closure. The accepted selected decoder slice is recorded in [its owner plan](original-assets/IMPLEMENTATION_PLAN.md); it does not complete all raw-index candidates.
- [ ] **Investigating — RT-F027–RT-F041 / RT-F067:** finish version-aware evaluator, sentinel, units, combat and equipment/runtime interpretation. Selected formats are accepted by the pinned loader; native operational semantics remain open in [field contracts](../docs/original-research/FIELD_INTERPRETATION.md).
- [ ] **Investigating — RT-F069:** reconcile complete dependency/record identity acceptance with the master; full original gameplay/appearance parity remains unfinished.

## Commands

From the repository root with Python 3.11+:

```powershell
python scripts/index-original-content.py
python scripts/index-original-content.py --validate-only
python scripts/index-original-content.py --source-root .tools/references/cosmic-rift-assets --manifest docs/original-research/ASSET_MANIFEST.json --out artifacts/research/original-content/index.json
```

The indexer uses only the Python standard library and a read-only `git rev-parse HEAD` call when the source has Git metadata. It never invokes upstream scripts or binaries. An archive copy without Git metadata must still pass all five exact canonical byte hashes. All numeric fields remain strings; field indexes are zero-based and source rows one-based.

## Actual output and evidence — October 13, 2026

The generated index contains 3,086 records: 2,763 CFG entries, 30 VEH records, 200 ITM records, 12 RPG records, and 81 LIO records. It records 551 candidate references spanning 32 manifest files, including 26 BLO files. All explicit filename and known-local-stem candidates resolve to manifest paths; the raw index retains its baseline of **545 unvalidated member candidates**. Subsequent selected decoding resolves the 12 hull sprite identities but does not recompute/complete that whole candidate inventory; zero unresolved filenames still does not prove dependency closure.

The indexed hull names are Asp, Excelsior, Ravager, Hornet, Skithist, Mastakith, Tarthist, Kragath, Rak'Kir, Rip'Pir, Zap'Pir, and Zag'Gir. This is the canonical Rogue Trader RPG/VEH catalog cross-match; it does not reinterpret a separate overview's count of all Cosmic Rift spacecraft.

The seven original type-4 Resources records are Tellerium, Refined Tellerium, Krandazite, Refined Krandazite, Wreckage, Iron Ore, and Alloy. Moltor Crystals/Rocks/Stones are retained in their original ammunition categories rather than being silently classified as minerals. Original type-6/8 candidates include Plasma Cannon, MIRV Missile Launcher, Fusion Bomb, Laser Cannon, and many child projectiles, debris, effects and NPC records; availability as playable equipment remains unvalidated.

Executed checks passed: all 12 hull/VEH IDs and names match; all 323 CSV records round-trip to the exact decoded source text; CFG entries plus structural rows round-trip every original line; numeric fields stay strings; a quoted-comma/doubled-quote/empty-field case retains raw tokens; output inside originals is rejected; a tampered expected SHA-256 is rejected; and all five source hashes remain unchanged before and after checks. The CLI generated its JSON with exit 0. Source-specific integrity is also corroborated by the root's 45-file raw reference verifier.

No source artwork, stats, maps, names or bytes were changed. No gameplay behavior or engine compatibility is claimed by this index.

