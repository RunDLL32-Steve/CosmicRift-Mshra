# Original computer actors and spectator vehicle - implementation plan

Master authority: [ROGUE_TRADER_2_PLAN.md](../../../ROGUE_TRADER_2_PLAN.md), sections 9-10,18 F/M2/M3. Registry: [FEATURE_CATALOG.md](../../../docs/original-research/FEATURE_CATALOG.md). Progress: [PROGRESS.md](../../../docs/development-log/PROGRESS.md). Reconciled October 13, 2026.

Seventeen original computer VEH records plus distinct nonplayable type-4 Spectator. Preserve refinery, dock, satellite, factory, hazard and NPC roles/factions.

Canonical source: fowlmouth/cosmic-rift-assets commit eb1514eaae7f9b9425307d9cc043c74ecdd15e35. Originals remain unchanged; runtime outputs are separate. Original field values are authoritative. Current domain stage is **Investigating native integration**; each unchecked record below still lacks exact runtime acceptance. Source indexing, versioned parsing, the bounded package audit and arena initialization are completed narrower capabilities. Record status describes full gameplay acceptance, not whether the record is available. Use Not started / Investigating / In progress / Testing / Complete; update at every transition. Remove specifically completed rows from this active table and retain ID/source/evidence in completion history; master/catalog retain checked audit entries.

## Accomplished scope

All seventeen original type-5 computer records and the separate type-4 Spectator are parsed. RT-F383 initializes 25 original computer instances; generic production, repair, shop, event and reward routes are audited in RT-F382. Actor-specific behavior and native spectator transitions are not verified.

## Next and parity boundary

- [ ] **Not started — RT-F269–F286/F059–F063:** validate each actor faction/ownership/production/repair/combat/hazard role and spectator transitions without replacing them with a generic invented pirate.
- [ ] **Investigating — RT-F386:** resolve positive DropItemId66 on original manufactory vehicles425/426/427; no alias or invented item.
- [ ] **Native join/spectator acceptance after RT-F390:** the null-script capacity guard and 12 handler checks are complete; actual native arena join, team assignment and spectator transitions remain unverified. See [RuntimeChecks](../../../scripts/compatibility/RuntimeChecks/IMPLEMENTATION_PLAN.md).

Full original runtime remains unchecked. Native testing reached only the welcome screen; Start produced no confirmed entry and the private test was cleaned up. Do not resume desktop interaction until the user is available.

## Active acceptance steps

- [ ] Validate actor ownership/faction/logic and remaining runtime graphics/audio/product/item/child chains; full raw/parsed records are already retained.
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

Canonical raw indexing and the pinned compiled parser accept the unchanged ITM200/VEH30/RPG12/LIO81/LVL files. Package audit RT-F381 verifies 19,198 supported literal item values, twelve hull links and 222 map references. RT-F383 validates original arena initialization; RT-F392 validates bounded full-asset hosting without a native player. All 89 canonical BLOs are inventoried separately from the eleven-archive pixel export. Evidence: [package audit](../../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md), [runtime plan](../../../docs/architecture/ROGUE_RUNTIME_PLAN.md), [dependency plan](../../../scripts/original-assets/DEPENDENCY_PLAN.md). These scoped capabilities do not complete any unchecked record-specific gameplay feature here. Preserve completed ID/evidence in this history, the master audit and [CHANGELOG.md](../../../CHANGELOG.md).
