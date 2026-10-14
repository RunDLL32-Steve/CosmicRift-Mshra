# Original computer actors and spectator vehicle - implementation plan

Master authority: [ROGUE_TRADER_2_PLAN.md](../../../ROGUE_TRADER_2_PLAN.md), sections 9-10,18 F/M2/M3. Registry: [FEATURE_CATALOG.md](../../../docs/original-research/FEATURE_CATALOG.md). Progress: [PROGRESS.md](../../../docs/development-log/PROGRESS.md). Reconciled October 13, 2026.

Seventeen original computer VEH records plus distinct nonplayable type-4 Spectator. Preserve refinery, dock, satellite, factory, hazard and NPC roles/factions.

Canonical source: fowlmouth/cosmic-rift-assets commit eb1514eaae7f9b9425307d9cc043c74ecdd15e35. Originals remain unchanged; runtime outputs are separate. Original field values are authoritative. Every record below is **Not started** for exact runtime behavior. Source indexing/pristine parsing are completed research, not runtime completion. Status transitions: Not started / Investigation / In progress / Testing / Done after feature-specific acceptance. Update status while working. Remove Done entries from this active table and retain ID/source/evidence in completion history; master/catalog retain their checked audit entries.

## Active acceptance steps

- [ ] Resolve full actor records, ownership/faction/logic, graphics/audio and product/item/child chains.
- [ ] Decode exact spawn, production, repair, trade, combat, count and steal/destroy rules.
- [ ] Keep player, bot and turret rewards separate and retain distinct original NPC factions.
- [ ] Implement each actor role and dependencies before its behavior acceptance.
- [ ] Verify the particular completed actor and reconcile its ID; static research requires no simulation.

## Active original records

| Feature / open acceptance | Exact original source | Status | Required acceptance / owner |
|---|---|---|---|
| [ ] [RT-F269 - Refinery](../../../docs/original-research/FEATURE_CATALOG.md#rt-f269) | `VEH/Rogue.veh` row1, type`5`, id`400` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F270 - Null Death Orb](../../../docs/original-research/FEATURE_CATALOG.md#rt-f270) | `VEH/Rogue.veh` row2, type`5`, id`401` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F271 - MeteorShower](../../../docs/original-research/FEATURE_CATALOG.md#rt-f271) | `VEH/Rogue.veh` row3, type`5`, id`402` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F272 - PirateStation](../../../docs/original-research/FEATURE_CATALOG.md#rt-f272) | `VEH/Rogue.veh` row4, type`5`, id`403` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F273 - Trading Outpost](../../../docs/original-research/FEATURE_CATALOG.md#rt-f273) | `VEH/Rogue.veh` row5, type`5`, id`404` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F274 - Null Kill Station](../../../docs/original-research/FEATURE_CATALOG.md#rt-f274) | `VEH/Rogue.veh` row6, type`5`, id`405` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F275 - Repair Dock](../../../docs/original-research/FEATURE_CATALOG.md#rt-f275) | `VEH/Rogue.veh` row7, type`5`, id`407` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F276 - Micro Repair Dock](../../../docs/original-research/FEATURE_CATALOG.md#rt-f276) | `VEH/Rogue.veh` row8, type`5`, id`408` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F277 - Solar Mold](../../../docs/original-research/FEATURE_CATALOG.md#rt-f277) | `VEH/Rogue.veh` row9, type`5`, id`409` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F278 - GunSat](../../../docs/original-research/FEATURE_CATALOG.md#rt-f278) | `VEH/Rogue.veh` row10, type`5`, id`410` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F279 - BeamSat](../../../docs/original-research/FEATURE_CATALOG.md#rt-f279) | `VEH/Rogue.veh` row11, type`5`, id`411` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F280 - Micro Refinery](../../../docs/original-research/FEATURE_CATALOG.md#rt-f280) | `VEH/Rogue.veh` row12, type`5`, id`412` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F281 - Rogue Trader Outpost](../../../docs/original-research/FEATURE_CATALOG.md#rt-f281) | `VEH/Rogue.veh` row13, type`5`, id`413` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F282 - Mold Spore](../../../docs/original-research/FEATURE_CATALOG.md#rt-f282) | `VEH/Rogue.veh` row14, type`5`, id`414` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F283 - Manufactory - Missile](../../../docs/original-research/FEATURE_CATALOG.md#rt-f283) | `VEH/Rogue.veh` row15, type`5`, id`425` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F284 - Manufactory - Munitions](../../../docs/original-research/FEATURE_CATALOG.md#rt-f284) | `VEH/Rogue.veh` row16, type`5`, id`426` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F285 - Manufactory - Defense](../../../docs/original-research/FEATURE_CATALOG.md#rt-f285) | `VEH/Rogue.veh` row17, type`5`, id`427` | Not started | ACTOR: faction/ownership/products/combat/presentation; AI/world/economy + Client, unassigned |
| [ ] [RT-F286 - Spectator Mode](../../../docs/original-research/FEATURE_CATALOG.md#rt-f286) | `VEH/Rogue.veh` row30, type`4`, id`2030` | Not started | Native spectator transition, behavior and UI; Client + server, unassigned |

## Completion history

Research completed: canonical raw catalog indexed and the compiled pinned upstream parser accepted selected pristine ITM/VEH/RPG/LIO/LVL files. These are parser/hash checks. No original gameplay feature in this folder is complete. Record major implemented features in [CHANGELOG.md](../../../CHANGELOG.md); retain completed ID/source/acceptance evidence here after removing it from the active queue.
