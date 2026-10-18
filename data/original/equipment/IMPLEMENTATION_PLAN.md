# Original equipment, utilities and internal effects - implementation plan

Master authority: [ROGUE_TRADER_2_PLAN.md](../../../ROGUE_TRADER_2_PLAN.md), sections 7-10,18 F/M2/M3. Registry: [FEATURE_CATALOG.md](../../../docs/original-research/FEATURE_CATALOG.md). Progress: [PROGRESS.md](../../../docs/development-log/PROGRESS.md). Reconciled October 13, 2026.

Selected nonweapon/nonammo/nonresource/nonexchange records include mods, warp systems, structures, computers, repairs and internal effects. Do not reclassify every record as an upgrade.

Canonical source: fowlmouth/cosmic-rift-assets commit eb1514eaae7f9b9425307d9cc043c74ecdd15e35. Originals remain unchanged; runtime outputs are separate. Original field values are authoritative. Current domain stage is **Investigating native integration**; each unchecked record below still lacks exact runtime acceptance. Source indexing, versioned parsing, the bounded package audit and arena initialization are completed narrower capabilities. Record status describes full gameplay acceptance, not whether the record is available. Use Not started / Investigating / In progress / Testing / Complete; update at every transition. Remove specifically completed rows from this active table and retain ID/source/evidence in completion history; master/catalog retain checked audit entries.

## Accomplished scope

Original item categories and parsed values are available. Derived-only RT-F385 restores VehicleMaker bounty field18 on exactly eight items, without any other item/export/source changes. Generic shop, repair, warp and production handlers exist; native installation/use remains unverified.

## Next and parity boundary

- [ ] **Investigating — RT-F033–F035/F048/F049:** validate exact slot/class/skill logic, modifier composition, pricing and installation.
- [ ] **Investigating — RT-F386 / native trading:** establish missing item66 runtime eligibility and connected native shop/install behavior. RT-F388/F389/F396 bounded sell/store/buy guards are implemented and verified separately in [RuntimeChecks](../../../scripts/compatibility/RuntimeChecks/IMPLEMENTATION_PLAN.md); they do not complete an equipment record.
- [ ] **Not started — per-record table:** verify original build, warp, repair and utility behavior in the native route; preserve internal effects as internal.

Full original runtime remains unchecked. Native testing reached only the welcome screen; Start produced no confirmed entry and the private test was cleaned up. Do not resume desktop interaction until the user is available.

## Active acceptance steps

- [ ] Validate runtime eligibility of the preserved original types/categories/logic/cost/mass/use fields and remaining dependency graph.
- [ ] Resolve exact modifier composition, slots, prerequisites, skill grammar and event operators.
- [ ] Implement original warp, building, repair and control rules after their dependencies.
- [ ] Keep internal effects internal and reproduce applicable original store/presentation behavior.
- [ ] Verify completed buying, removal, installation, use and return/death behavior, then reconcile its ID.

## Active original records

| Feature / open acceptance | Exact original source | Status | Required acceptance / owner |
|---|---|---|---|
| [ ] [RT-F093 - SuperBoosters](../../../docs/original-research/FEATURE_CATALOG.md#rt-f093) | `ITM/Rogue_mp.itm` row18, type`13`, id`1` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F094 - TurboBoosters](../../../docs/original-research/FEATURE_CATALOG.md#rt-f094) | `ITM/Rogue_mp.itm` row19, type`13`, id`2` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F095 - HyperBoosters](../../../docs/original-research/FEATURE_CATALOG.md#rt-f095) | `ITM/Rogue_mp.itm` row20, type`13`, id`3` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F096 - Duraplas Armor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f096) | `ITM/Rogue_mp.itm` row21, type`13`, id`4` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F097 - Carbolux Armor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f097) | `ITM/Rogue_mp.itm` row22, type`13`, id`5` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F098 - Maxilite Armor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f098) | `ITM/Rogue_mp.itm` row23, type`13`, id`6` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F099 - Turbo Maneuver Thrusters](../../../docs/original-research/FEATURE_CATALOG.md#rt-f099) | `ITM/Rogue_mp.itm` row24, type`13`, id`7` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F100 - Super Maneuver Thrusters](../../../docs/original-research/FEATURE_CATALOG.md#rt-f100) | `ITM/Rogue_mp.itm` row25, type`13`, id`8` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F101 - Turbo Capacitor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f101) | `ITM/Rogue_mp.itm` row26, type`13`, id`9` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F102 - Super Capacitor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f102) | `ITM/Rogue_mp.itm` row27, type`13`, id`10` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F103 - Turbocharger](../../../docs/original-research/FEATURE_CATALOG.md#rt-f103) | `ITM/Rogue_mp.itm` row28, type`13`, id`11` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F104 - Supercharger](../../../docs/original-research/FEATURE_CATALOG.md#rt-f104) | `ITM/Rogue_mp.itm` row29, type`13`, id`12` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F138 - Sillicate Armor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f138) | `ITM/Rogue_mp.itm` row63, type`13`, id`13` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F139 - BioResonance Chamber](../../../docs/original-research/FEATURE_CATALOG.md#rt-f139) | `ITM/Rogue_mp.itm` row64, type`13`, id`14` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F140 - BioBooster Pod](../../../docs/original-research/FEATURE_CATALOG.md#rt-f140) | `ITM/Rogue_mp.itm` row65, type`13`, id`15` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F141 - Tiathamate Armor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f141) | `ITM/Rogue_mp.itm` row66, type`13`, id`16` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F161 - Advanced Moltor Core](../../../docs/original-research/FEATURE_CATALOG.md#rt-f161) | `ITM/Rogue_mp.itm` row91, type`13`, id`20` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F162 - Bio Repellor Pods](../../../docs/original-research/FEATURE_CATALOG.md#rt-f162) | `ITM/Rogue_mp.itm` row92, type`13`, id`21` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F163 - Hyper Capacitor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f163) | `ITM/Rogue_mp.itm` row93, type`13`, id`22` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F178 - Bio Carapace](../../../docs/original-research/FEATURE_CATALOG.md#rt-f178) | `ITM/Rogue_mp.itm` row108, type`13`, id`23` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F179 - Enhanced Bio Carapace](../../../docs/original-research/FEATURE_CATALOG.md#rt-f179) | `ITM/Rogue_mp.itm` row109, type`13`, id`24` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F180 - Hyper Jump](../../../docs/original-research/FEATURE_CATALOG.md#rt-f180) | `ITM/Rogue_mp.itm` row110, type`17`, id`25` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F181 - HyperCore](../../../docs/original-research/FEATURE_CATALOG.md#rt-f181) | `ITM/Rogue_mp.itm` row111, type`4`, id`2011` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F182 - Portal](../../../docs/original-research/FEATURE_CATALOG.md#rt-f182) | `ITM/Rogue_mp.itm` row112, type`17`, id`26` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F183 - Warp-Start](../../../docs/original-research/FEATURE_CATALOG.md#rt-f183) | `ITM/Rogue_mp.itm` row113, type`17`, id`27` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F184 - Warp-Team](../../../docs/original-research/FEATURE_CATALOG.md#rt-f184) | `ITM/Rogue_mp.itm` row114, type`17`, id`28` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F186 - Hyper Beacon System](../../../docs/original-research/FEATURE_CATALOG.md#rt-f186) | `ITM/Rogue_mp.itm` row116, type`17`, id`29` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F187 - Controlled Hyper Jump](../../../docs/original-research/FEATURE_CATALOG.md#rt-f187) | `ITM/Rogue_mp.itm` row117, type`17`, id`30` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F188 - Hyper Recall](../../../docs/original-research/FEATURE_CATALOG.md#rt-f188) | `ITM/Rogue_mp.itm` row118, type`17`, id`31` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F189 - Hyper Gate](../../../docs/original-research/FEATURE_CATALOG.md#rt-f189) | `ITM/Rogue_mp.itm` row119, type`17`, id`33` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F201 - Turbo Reaction Thrusters](../../../docs/original-research/FEATURE_CATALOG.md#rt-f201) | `ITM/Rogue_mp.itm` row131, type`13`, id`34` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F202 - Super Reaction Thrusters](../../../docs/original-research/FEATURE_CATALOG.md#rt-f202) | `ITM/Rogue_mp.itm` row132, type`13`, id`35` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F203 - Bio Reactor Pods](../../../docs/original-research/FEATURE_CATALOG.md#rt-f203) | `ITM/Rogue_mp.itm` row133, type`13`, id`36` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F206 - Ablative Shell](../../../docs/original-research/FEATURE_CATALOG.md#rt-f206) | `ITM/Rogue_mp.itm` row136, type`13`, id`37` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F207 - Absorption Shell](../../../docs/original-research/FEATURE_CATALOG.md#rt-f207) | `ITM/Rogue_mp.itm` row137, type`13`, id`38` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F219 - Minor Repair](../../../docs/original-research/FEATURE_CATALOG.md#rt-f219) | `ITM/Rogue_mp.itm` row149, type`1`, id`39` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F220 - Major Repair](../../../docs/original-research/FEATURE_CATALOG.md#rt-f220) | `ITM/Rogue_mp.itm` row150, type`1`, id`40` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F221 - Remove Armor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f221) | `ITM/Rogue_mp.itm` row151, type`15`, id`41` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F222 - ArmorRemoveHack](../../../docs/original-research/FEATURE_CATALOG.md#rt-f222) | `ITM/Rogue_mp.itm` row152, type`1`, id`42` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F232 - Full Repair](../../../docs/original-research/FEATURE_CATALOG.md#rt-f232) | `ITM/Rogue_mp.itm` row162, type`1`, id`43` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F237 - Trading Post](../../../docs/original-research/FEATURE_CATALOG.md#rt-f237) | `ITM/Rogue_mp.itm` row169, type`7`, id`45` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F238 - Micro Refinery](../../../docs/original-research/FEATURE_CATALOG.md#rt-f238) | `ITM/Rogue_mp.itm` row170, type`7`, id`46` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F239 - GunSat](../../../docs/original-research/FEATURE_CATALOG.md#rt-f239) | `ITM/Rogue_mp.itm` row171, type`7`, id`47` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F240 - BeamSat](../../../docs/original-research/FEATURE_CATALOG.md#rt-f240) | `ITM/Rogue_mp.itm` row172, type`7`, id`48` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F247 - Micro Repair Dock](../../../docs/original-research/FEATURE_CATALOG.md#rt-f247) | `ITM/Rogue_mp.itm` row179, type`7`, id`49` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F261 - Turbo AfterBurners](../../../docs/original-research/FEATURE_CATALOG.md#rt-f261) | `ITM/Rogue_mp.itm` row193, type`13`, id`61` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F262 - Super AfterBurners](../../../docs/original-research/FEATURE_CATALOG.md#rt-f262) | `ITM/Rogue_mp.itm` row194, type`13`, id`62` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F263 - Hyper AfterBurners](../../../docs/original-research/FEATURE_CATALOG.md#rt-f263) | `ITM/Rogue_mp.itm` row195, type`13`, id`63` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F264 - Missile Manufactory](../../../docs/original-research/FEATURE_CATALOG.md#rt-f264) | `ITM/Rogue_mp.itm` row196, type`7`, id`83` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F265 - Munitions Manufactory](../../../docs/original-research/FEATURE_CATALOG.md#rt-f265) | `ITM/Rogue_mp.itm` row197, type`7`, id`84` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F266 - Defense Manufactory](../../../docs/original-research/FEATURE_CATALOG.md#rt-f266) | `ITM/Rogue_mp.itm` row198, type`7`, id`85` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |
| [ ] [RT-F267 - Nanobot Control System](../../../docs/original-research/FEATURE_CATALOG.md#rt-f267) | `ITM/Rogue_mp.itm` row199, type`4`, id`2023` | Not started | ITEM: version/reference/logic/cost/use/presentation; GameData + Sim/economy + Client, unassigned |

## Completion history

Canonical raw indexing and the pinned compiled parser accept the unchanged ITM200/VEH30/RPG12/LIO81/LVL files. Package audit RT-F381 verifies 19,198 supported literal item values, twelve hull links and 222 map references. RT-F383 validates original arena initialization; RT-F392 validates bounded full-asset hosting without a native player. All 89 canonical BLOs are inventoried separately from the eleven-archive pixel export. Evidence: [package audit](../../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md), [runtime plan](../../../docs/architecture/ROGUE_RUNTIME_PLAN.md), [dependency plan](../../../scripts/original-assets/DEPENDENCY_PLAN.md). These scoped capabilities do not complete any unchecked record-specific gameplay feature here. Preserve completed ID/evidence in this history, the master audit and [CHANGELOG.md](../../../CHANGELOG.md).
