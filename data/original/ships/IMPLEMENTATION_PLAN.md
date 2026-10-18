# Original ships and hull exchanges - implementation plan

Master authority: [ROGUE_TRADER_2_PLAN.md](../../../ROGUE_TRADER_2_PLAN.md), sections 5-7,18 F/M2. Registry: [FEATURE_CATALOG.md](../../../docs/original-research/FEATURE_CATALOG.md). Progress: [PROGRESS.md](../../../docs/development-log/PROGRESS.md). Reconciled October 13, 2026.

Twelve selectable RPG hulls and twelve type-16 same-race exchanges. Preserve full VEH physics, energy, cargo, starting equipment, restrictions and original appearance for every hull.

Canonical source: fowlmouth/cosmic-rift-assets commit eb1514eaae7f9b9425307d9cc043c74ecdd15e35. Originals remain unchanged; runtime outputs are separate. Original field values are authoritative. Current domain stage is **Investigating native integration**; each unchecked record below still lacks exact runtime acceptance. Source indexing, versioned parsing, the bounded package audit and arena initialization are completed narrower capabilities. Record status describes full gameplay acceptance, not whether the record is available. Use Not started / Investigating / In progress / Testing / Complete; update at every transition. Remove specifically completed rows from this active table and retain ID/source/evidence in completion history; master/catalog retain checked audit entries.

## Accomplished scope

Twelve exact RPG-to-VEH identity links are confirmed. All twelve Small/Tiny sprite pairs, 64 direction rows and eleven state columns are available in the verified source/standalone inspector (RT-F073/F373/F374). Native banking, palette/blitting and playable statistics are not accepted.

Tracked raw-data loader (initial October 15 evidence): `shared/RogueTrader.OriginalData` verifies the pinned SHA-256/length of RPG/Rogue.rpg, VEH/Rogue.veh and ITM/Rogue_mp.itm, reads every field by index, links the twelve RPG hull rows to their VEH Car rows (VEH f[0]=2, version v53, f[2] equal to RPG f[3]) and exposes raw hull points, cargo f[47..48], energy f[94..96], all sixteen terrain speed blocks f[202..361], bounce f[362] and hyper energy cost f[373]. Historical loader evidence: 81 unit tests passed (41 existing + 40 new with originals present); oracle table RT-F001-RT-F012 asserted. This is the suite size at that checkpoint, not the current whole-project test count. This is data availability only; no hull box below is ticked.

## Next and parity boundary

- [ ] **Investigating — RT-F380/F384:** enter the local original arena with the native client when the user is available.
- [ ] **In progress — RT-F001–F012/F249–F260:** raw hull records now load (see above); next validate native F11 selection, default vehicle/loadout, unspectating, hull-specific flight/energy/cargo and exact exchange costs/restrictions.
- [ ] **Investigating — RT-F023/F074/F075:** preserve every frame and prove native palette/bank/timing semantics; the inspector center-column preview is reconstructed.

Full original runtime remains unchecked. Native testing reached only the welcome screen; Start produced no confirmed entry and the private test was cleaned up. Do not resume desktop interaction until the user is available.

## Active acceptance steps

- [ ] Validate remaining runtime effect/sound links and native eligibility; RPG/VEH identities and full raw records are already retained.
- [ ] Interpret native movement, energy, cargo, terrain and negative defaults before applying values.
- [ ] Implement full directions, bank poses, native palette rendering and original starting loadout.
- [ ] Implement exact class/skill/slot restrictions and same-race exchange costs/transitions.
- [ ] Verify the particular completed hull or exchange, then reconcile its ID and master checkbox.

## Active original records

| Feature / open acceptance | Exact original source | Status | Required acceptance / owner |
|---|---|---|---|
| [ ] [RT-F249 - Hornet](../../../docs/original-research/FEATURE_CATALOG.md#rt-f249) | `ITM/Rogue_mp.itm` row181, type`16`, id`32` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F250 - Asp](../../../docs/original-research/FEATURE_CATALOG.md#rt-f250) | `ITM/Rogue_mp.itm` row182, type`16`, id`50` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F251 - Ravager](../../../docs/original-research/FEATURE_CATALOG.md#rt-f251) | `ITM/Rogue_mp.itm` row183, type`16`, id`51` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F252 - Excelsior](../../../docs/original-research/FEATURE_CATALOG.md#rt-f252) | `ITM/Rogue_mp.itm` row184, type`16`, id`52` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F253 - Rip'Pir](../../../docs/original-research/FEATURE_CATALOG.md#rt-f253) | `ITM/Rogue_mp.itm` row185, type`16`, id`53` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F254 - Rak'Kir](../../../docs/original-research/FEATURE_CATALOG.md#rt-f254) | `ITM/Rogue_mp.itm` row186, type`16`, id`54` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F255 - Zap'Pir](../../../docs/original-research/FEATURE_CATALOG.md#rt-f255) | `ITM/Rogue_mp.itm` row187, type`16`, id`55` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F256 - Zag'Gir](../../../docs/original-research/FEATURE_CATALOG.md#rt-f256) | `ITM/Rogue_mp.itm` row188, type`16`, id`56` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F257 - Tarthist](../../../docs/original-research/FEATURE_CATALOG.md#rt-f257) | `ITM/Rogue_mp.itm` row189, type`16`, id`57` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F258 - Skithist](../../../docs/original-research/FEATURE_CATALOG.md#rt-f258) | `ITM/Rogue_mp.itm` row190, type`16`, id`58` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F259 - Kragath](../../../docs/original-research/FEATURE_CATALOG.md#rt-f259) | `ITM/Rogue_mp.itm` row191, type`16`, id`59` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F260 - Mastakith](../../../docs/original-research/FEATURE_CATALOG.md#rt-f260) | `ITM/Rogue_mp.itm` row192, type`16`, id`60` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F001 - Asp](../../../docs/original-research/FEATURE_CATALOG.md#rt-f001) | `RPG/Rogue.rpg` row1, type`1`, id`2` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F002 - Excelsior](../../../docs/original-research/FEATURE_CATALOG.md#rt-f002) | `RPG/Rogue.rpg` row2, type`1`, id`15` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F003 - Ravager](../../../docs/original-research/FEATURE_CATALOG.md#rt-f003) | `RPG/Rogue.rpg` row3, type`1`, id`16` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F004 - Hornet](../../../docs/original-research/FEATURE_CATALOG.md#rt-f004) | `RPG/Rogue.rpg` row4, type`1`, id`8` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F005 - Skithist](../../../docs/original-research/FEATURE_CATALOG.md#rt-f005) | `RPG/Rogue.rpg` row5, type`1`, id`1` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F006 - Mastakith](../../../docs/original-research/FEATURE_CATALOG.md#rt-f006) | `RPG/Rogue.rpg` row6, type`1`, id`4` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F007 - Tarthist](../../../docs/original-research/FEATURE_CATALOG.md#rt-f007) | `RPG/Rogue.rpg` row7, type`1`, id`11` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F008 - Kragath](../../../docs/original-research/FEATURE_CATALOG.md#rt-f008) | `RPG/Rogue.rpg` row8, type`1`, id`3` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F009 - Rak'Kir](../../../docs/original-research/FEATURE_CATALOG.md#rt-f009) | `RPG/Rogue.rpg` row9, type`1`, id`5` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F010 - Rip'Pir](../../../docs/original-research/FEATURE_CATALOG.md#rt-f010) | `RPG/Rogue.rpg` row10, type`1`, id`6` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F011 - Zap'Pir](../../../docs/original-research/FEATURE_CATALOG.md#rt-f011) | `RPG/Rogue.rpg` row11, type`1`, id`9` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |
| [ ] [RT-F012 - Zag'Gir](../../../docs/original-research/FEATURE_CATALOG.md#rt-f012) | `RPG/Rogue.rpg` row12, type`1`, id`10` | Not started | HULL: identity/frame/physics/cargo/energy/loadout/skills; GameData + Sim + Client, unassigned |

## Completion history

Canonical raw indexing and the pinned compiled parser accept the unchanged ITM200/VEH30/RPG12/LIO81/LVL files. Package audit RT-F381 verifies 19,198 supported literal item values, twelve hull links and 222 map references. RT-F383 validates original arena initialization; RT-F392 validates bounded full-asset hosting without a native player. All 89 canonical BLOs are inventoried separately from the eleven-archive pixel export. Evidence: [package audit](../../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md), [runtime plan](../../../docs/architecture/ROGUE_RUNTIME_PLAN.md), [dependency plan](../../../scripts/original-assets/DEPENDENCY_PLAN.md). These scoped capabilities do not complete any unchecked record-specific gameplay feature here. Preserve completed ID/evidence in this history, the master audit and [CHANGELOG.md](../../../CHANGELOG.md).
