# Original ships and hull exchanges - implementation plan

Master authority: [ROGUE_TRADER_2_PLAN.md](../../../ROGUE_TRADER_2_PLAN.md), sections 5-7,18 F/M2. Registry: [FEATURE_CATALOG.md](../../../docs/original-research/FEATURE_CATALOG.md). Progress: [PROGRESS.md](../../../docs/development-log/PROGRESS.md). Reconciled October 13, 2026.

Twelve selectable RPG hulls and twelve type-16 same-race exchanges. Preserve full VEH physics, energy, cargo, starting equipment, restrictions and original appearance for every hull.

Canonical source: fowlmouth/cosmic-rift-assets commit eb1514eaae7f9b9425307d9cc043c74ecdd15e35. Originals remain unchanged; runtime outputs are separate. Original field values are authoritative. Every record below is **Not started** for exact runtime behavior. Source indexing/pristine parsing are completed research, not runtime completion. Status transitions: Not started / Investigation / In progress / Testing / Done after feature-specific acceptance. Update status while working. Remove Done entries from this active table and retain ID/source/evidence in completion history; master/catalog retain their checked audit entries.

## Active acceptance steps

- [ ] Resolve all RPG-to-VEH-to-original sprite/effect/sound identities and full raw records.
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

Research completed: canonical raw catalog indexed and the compiled pinned upstream parser accepted selected pristine ITM/VEH/RPG/LIO/LVL files. These are parser/hash checks. No original gameplay feature in this folder is complete. Record major implemented features in [CHANGELOG.md](../../../CHANGELOG.md); retain completed ID/source/acceptance evidence here after removing it from the active queue.
