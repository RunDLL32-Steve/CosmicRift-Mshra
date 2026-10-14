# Game data implementation plan

Status authority: [master M0](../../ROGUE_TRADER_2_PLAN.md#milestone-0--repository-and-historical-research), [M1 engineering acceptance/fidelity gate](../../ROGUE_TRADER_2_PLAN.md#m0m1-completion-evidence--engineering-prototype), [§6 spacecraft/loadouts](../../ROGUE_TRADER_2_PLAN.md#6-spacecraft-and-loadout-system), [§8 resources/cargo](../../ROGUE_TRADER_2_PLAN.md#8-mining-and-resource-collection), and [M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay). Reconcile with the [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md), [data plan](../../data/IMPLEMENTATION_PLAN.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; documentation inspection only.

## Completion history — prototype engineering

| Feature | Current status / evidence |
|---|---|
| One provisional ship, weapon and sector; spawn, target and asteroid records | **Complete with evidence** — [Definitions.cs](Definitions.cs) marks defaults NEW; [GameDataTests.cs](../../tests/Unit/GameDataTests.cs) verifies actual external JSON. |
| Godot-independent JSON loading and validation | **Complete with evidence** — [GameCatalog.cs](GameCatalog.cs) rejects unknown fields, invalid/nonfinite values, duplicate IDs and derived acceleration/squared-magnitude overflow. |
| Prototype data acceptance | **Complete with evidence** — prior [41-case result](../../artifacts/test-results/unit.trx): 41 passed, zero failed/not executed on October 12, 2026; [dated acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) records build/client/export gates. No tests rerun for this review. |

These First Contact fixtures contain no original-content runtime adapter, cargo, economy or installed-equipment implementation. They do not complete any original hull/item.

## Outstanding original data work

Preserve `fowlmouth/cosmic-rift-assets` at `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`, pinned by [ASSET_MANIFEST.json](../../docs/original-research/ASSET_MANIFEST.json). Retain complete raw fields, names, category, version, ID, source row/path and hash beside derived values. Original records take precedence; adaptation outputs remain separate.

- [ ] **Investigating** — RT-F001–RT-F012 and RT-F069: prove original hull identity/dependency closure. The separately completed [raw index](../../scripts/ORIGINAL_CONTENT_INDEX_PLAN.md) recovered 3,086 records, 12 matched hulls, 200 items and seven resources; indexing does not validate all members or hull behavior.
- [ ] **Not started** — runtime adapters for RT-F001–RT-F019 and RT-F076–RT-F268. Preserve original resource, weapon, equipment, ammo, repair and credit categories; follow individual catalog acceptance rather than duplicating detailed content plans.
- [ ] **Investigating** — RT-F027–RT-F032 and RT-F036–RT-F041: native units, terrain overrides and sentinels. Reverse `-1/-1`, inheritance `-550`, MIRV lifetime `-65` and resale `-1` stay raw and unresolved; prototype defaults are not conversions.
- [ ] **Investigating** — RT-F033–RT-F035 and RT-F067: original slot/category limits, skill-expression grammar and ordered event actions. Installation/starting-loadout implementation is **Not started**.
- [ ] **Not started** — RT-F042–RT-F049 and RT-F054–RT-F057: interpreted pickup, refine, trade, repair, upgrade, warp and production definitions after original recipes/restrictions/values are validated. Six invented modifiers cannot complete the equipment catalog.
- [ ] **Investigating** — RT-F021/RT-F069: LVL/LIO references and runtime semantics. The separate [pristine parser report](../../.tools/compatibility/reports/pristine-load-report.json) accepts unchanged CFG, 200 ITM v57, 30 VEH v53, 81 LIO, 12 RPG v6 and 1999×1998 LVL. This does not prove collision, evaluator or native execution fidelity.
- [ ] **Not started** — focused adapter identity/version/raw-field/hash acceptance, then behavior verification only after the specific gameplay feature exists. Reconcile only that verified feature to master §18.

Palette/HSV/shadow and direction→bank/state selection (RT-F023, RT-F074–RT-F075) remain importer/client dependencies. A decoded sprite or parser count cannot complete a hull/loadout.
