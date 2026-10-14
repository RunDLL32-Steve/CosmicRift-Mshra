# Original resources and credit effects - implementation plan

Master authority: [ROGUE_TRADER_2_PLAN.md](../../../ROGUE_TRADER_2_PLAN.md), sections 8,18 M2. Registry: [FEATURE_CATALOG.md](../../../docs/original-research/FEATURE_CATALOG.md). Progress: [PROGRESS.md](../../../docs/development-log/PROGRESS.md). Reconciled October 13, 2026.

Seven original type-4 Resources records plus four distinct type-1 credit helpers. Credit helpers are not extra minerals. Preserve all original names, mass, categories and recipes.

Canonical source: fowlmouth/cosmic-rift-assets commit eb1514eaae7f9b9425307d9cc043c74ecdd15e35. Originals remain unchanged; runtime outputs are separate. Original field values are authoritative. Every record below is **Not started** for exact runtime behavior. Source indexing/pristine parsing are completed research, not runtime completion. Status transitions: Not started / Investigation / In progress / Testing / Done after feature-specific acceptance. Update status while working. Remove Done entries from this active table and retain ID/source/evidence in completion history; master/catalog retain their checked audit entries.

## Active acceptance steps

- [ ] Resolve original pickup, icon, drop and spawn identities, item mass and inventory quantities.
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

Research completed: canonical raw catalog indexed and the compiled pinned upstream parser accepted selected pristine ITM/VEH/RPG/LIO/LVL files. These are parser/hash checks. No original gameplay feature in this folder is complete. Record major implemented features in [CHANGELOG.md](../../../CHANGELOG.md); retain completed ID/source/acceptance evidence here after removing it from the active queue.
