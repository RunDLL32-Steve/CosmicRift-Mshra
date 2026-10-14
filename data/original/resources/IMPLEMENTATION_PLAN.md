# Original resources and credit effects - implementation plan

Master authority: [ROGUE_TRADER_2_PLAN.md](../../../ROGUE_TRADER_2_PLAN.md), sections 8,18 M2. Registry: [FEATURE_CATALOG.md](../../../docs/original-research/FEATURE_CATALOG.md). Progress: [PROGRESS.md](../../../docs/development-log/PROGRESS.md). Reconciled October 13, 2026.

Seven original type-4 Resources records plus four distinct type-1 credit helpers. Credit helpers are not extra minerals. Preserve all original names, mass, categories and recipes.

Canonical source: fowlmouth/cosmic-rift-assets commit eb1514eaae7f9b9425307d9cc043c74ecdd15e35. Originals remain unchanged; runtime outputs are separate. Original field values are authoritative. Current domain stage is **Investigating native integration**; each unchecked record below still lacks exact runtime acceptance. Source indexing, versioned parsing, the bounded package audit and arena initialization are completed narrower capabilities. Record status describes full gameplay acceptance, not whether the record is available. Use Not started / Investigating / In progress / Testing / Complete; update at every transition. Remove specifically completed rows from this active table and retain ID/source/evidence in completion history; master/catalog retain checked audit entries.

## Accomplished scope

Seven original resource identities and four separate credit helpers are parsed with original names/categories. RT-F383 initializes 100 original mineral drops, a spawn-state observation without player inputs. Generic pickup/refining/trading handlers are present; no mining-to-persistence loop is accepted.

## Next and parity boundary

- [ ] **Not started — RT-F013–F019/F042–F047:** validate original generation, mining/pickup, cargo handling, refinery quantities/timing, trading and loss lifecycle.
- [ ] **Investigating — RT-F388/F389:** fix and verify relevant sell/proximity behavior before checking a resource/helper trade feature complete.
- [ ] **Not started — RT-F068:** verify session-to-session currency/inventory; standalone hosting currently does not persist.

Full original runtime remains unchecked. Native testing reached only the welcome screen; Start produced no confirmed entry and the private test was cleaned up. Do not resume desktop interaction until the user is available.

## Active acceptance steps

- [ ] Validate actual pickup/icon/drop/spawn eligibility and native mass/quantity semantics; source identities and initial drops are already retained.
- [ ] Decode exact refinery and trading recipes, quantities, prices and credit effects.
- [ ] Implement original cargo thresholds/handling and collection, refining, selling and loss policies.
- [ ] Verify conservation and transaction acceptance per completed material/helper.
- [ ] Reconcile completed ID/evidence and remove it from the active queue.

## Active original records

| Feature / open acceptance | Exact original source | Status | Required acceptance / owner |
|---|---|---|---|
| [ ] [RT-F013 - Tellerium](../../../docs/original-research/FEATURE_CATALOG.md#rt-f013) | `ITM/Rogue_mp.itm` row68, type`4`, id`2003` | Not started | RESOURCE: pickup/mass/spawn/drop/refine/trade; GameData + economy, unassigned |
| [ ] [RT-F014 - Refined Tellerium](../../../docs/original-research/FEATURE_CATALOG.md#rt-f014) | `ITM/Rogue_mp.itm` row69, type`4`, id`2004` | Not started | RESOURCE: pickup/mass/spawn/drop/refine/trade; GameData + economy, unassigned |
| [ ] [RT-F015 - Krandazite](../../../docs/original-research/FEATURE_CATALOG.md#rt-f015) | `ITM/Rogue_mp.itm` row70, type`4`, id`2005` | Not started | RESOURCE: pickup/mass/spawn/drop/refine/trade; GameData + economy, unassigned |
| [ ] [RT-F016 - Refined Krandazite](../../../docs/original-research/FEATURE_CATALOG.md#rt-f016) | `ITM/Rogue_mp.itm` row71, type`4`, id`2006` | Not started | RESOURCE: pickup/mass/spawn/drop/refine/trade; GameData + economy, unassigned |
| [ ] [RT-F155 - Krandazite Credits](../../../docs/original-research/FEATURE_CATALOG.md#rt-f155) | `ITM/Rogue_mp.itm` row84, type`1`, id`17` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F156 - Tellerium Credits](../../../docs/original-research/FEATURE_CATALOG.md#rt-f156) | `ITM/Rogue_mp.itm` row85, type`1`, id`18` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F017 - Wreckage](../../../docs/original-research/FEATURE_CATALOG.md#rt-f017) | `ITM/Rogue_mp.itm` row87, type`4`, id`2008` | Not started | RESOURCE: pickup/mass/spawn/drop/refine/trade; GameData + economy, unassigned |
| [ ] [RT-F160 - Scrap Credits](../../../docs/original-research/FEATURE_CATALOG.md#rt-f160) | `ITM/Rogue_mp.itm` row90, type`1`, id`19` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F018 - Iron Ore](../../../docs/original-research/FEATURE_CATALOG.md#rt-f018) | `ITM/Rogue_mp.itm` row166, type`4`, id`2019` | Not started | RESOURCE: pickup/mass/spawn/drop/refine/trade; GameData + economy, unassigned |
| [ ] [RT-F019 - Alloy](../../../docs/original-research/FEATURE_CATALOG.md#rt-f019) | `ITM/Rogue_mp.itm` row167, type`4`, id`2020` | Not started | RESOURCE: pickup/mass/spawn/drop/refine/trade; GameData + economy, unassigned |
| [ ] [RT-F236 - Alloy Credits](../../../docs/original-research/FEATURE_CATALOG.md#rt-f236) | `ITM/Rogue_mp.itm` row168, type`1`, id`44` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |

## Completion history

Canonical raw indexing and the pinned compiled parser accept the unchanged ITM200/VEH30/RPG12/LIO81/LVL files. Package audit RT-F381 verifies 19,198 supported literal item values, twelve hull links and 222 map references. RT-F383 validates original arena initialization; RT-F392 validates bounded full-asset hosting without a native player. All 89 canonical BLOs are inventoried separately from the eleven-archive pixel export. Evidence: [package audit](../../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md), [runtime plan](../../../docs/architecture/ROGUE_RUNTIME_PLAN.md), [dependency plan](../../../scripts/original-assets/DEPENDENCY_PLAN.md). These scoped capabilities do not complete any unchecked record-specific gameplay feature here. Preserve completed ID/evidence in this history, the master audit and [CHANGELOG.md](../../../CHANGELOG.md).
