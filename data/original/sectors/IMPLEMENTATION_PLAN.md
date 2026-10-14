# Original Rogue Trader sector and level objects - implementation plan

Master authority: [ROGUE_TRADER_2_PLAN.md](../../../ROGUE_TRADER_2_PLAN.md), sections 8-10,15,18 F/M2/M3. Registry: [FEATURE_CATALOG.md](../../../docs/original-research/FEATURE_CATALOG.md). Progress: [PROGRESS.md](../../../docs/development-log/PROGRESS.md). Reconciled October 13, 2026.

Canonical Rogue.lvl, Rogue.bmp overview and all81 Rogue.lio records. Native world topology/collision, overview display and F3 grid behavior have separate acceptance.

Canonical source: fowlmouth/cosmic-rift-assets commit eb1514eaae7f9b9425307d9cc043c74ecdd15e35. Originals remain unchanged; runtime outputs are separate. Original field values are authoritative. Current domain stage is **Investigating native integration**; each unchecked record below still lacks exact runtime acceptance. Source indexing, versioned parsing, the bounded package audit and arena initialization are completed narrower capabilities. Record status describes full gameplay acceptance, not whether the record is available. Use Not started / Investigating / In progress / Testing / Complete; update at every transition. Remove specifically completed rows from this active table and retain ID/source/evidence in completion history; master/catalog retain checked audit entries.

## Accomplished scope

RT-F020/F373 verify the exact 1999×1998 overview in source and standalone inspector. Versioned LVL parsing exports 3,994,002 tiles/116,065 blocked masks; RT-F381 resolves 222 map archive/member links. RT-F383 initializes one original arena with 51 teams, 42 hides, 25 computers and 100 mineral drops. Native graphical delivery, grid, collision and LIO interactions remain unverified.

## Next and parity boundary

- [ ] **Investigating — RT-F387/F021/F022:** deliver the entire unchanged 1,290,994-byte LVL, including 380,052 graphical trailer bytes that the server parser leaves unread, and validate native rendering/grid/coordinates/collision.
- [ ] **Not started — RT-F287–F367:** verify each original LIO placement, timer, event, warp/safezone/resource/hazard interaction through the existing handlers.
- [ ] **Investigating — RT-F368–F372:** five help files are recovered unchanged from pinned corroborating Zone-Assets; native menu/key substitution behavior remains open.

Full original runtime remains unchecked. Native testing reached only the welcome screen; Start produced no confirmed entry and the private test was cleaned up. Do not resume desktop interaction until the user is available.

## Active acceptance steps

- [ ] Validate native coordinates/origin/collision meaning from the preserved original map/LIO fields; source bytes and hashes are already retained.
- [ ] Validate native use of parsed LVL tile/material/physics and graphical trailer; keep the unchanged raw LVL separate from the BMP overview.
- [ ] Resolve all LIO versions, areas, timers, conditions, actor/item/faction/warp/sound references.
- [ ] Render original objects and implement completed spawn, resource, warp and safezone events.
- [ ] Check display via image/input/no-tick acceptance; simulate only completed behavior requiring it.

## Active original records

| Feature / open acceptance | Exact original source | Status | Required acceptance / owner |
|---|---|---|---|
| [ ] [RT-F021 - Native Rogue.lvl topology/collision](../../../docs/original-research/FEATURE_CATALOG.md#rt-f021) | Canonical CFG/LVL/LIO/BMP | Investigation | Actual LVL tile/material/physics and trailing graphic entities / Sim + Client, unassigned |
| [ ] [RT-F022 - Original F3 map/grid/coordinates](../../../docs/original-research/FEATURE_CATALOG.md#rt-f022) | Canonical CFG/LVL/LIO/BMP | Investigation | Original map, markers, colors, scale and coordinates / Client, root |
| [ ] [RT-F287 - Planets - Jupiter](../../../docs/original-research/FEATURE_CATALOG.md#rt-f287) | `LIO/Rogue.lio` row1, type`9`, id`2` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F288 - Planets - Mars](../../../docs/original-research/FEATURE_CATALOG.md#rt-f288) | `LIO/Rogue.lio` row2, type`9`, id`31` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F289 - LNebula2](../../../docs/original-research/FEATURE_CATALOG.md#rt-f289) | `LIO/Rogue.lio` row3, type`9`, id`32` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F290 - Planets - Moon1](../../../docs/original-research/FEATURE_CATALOG.md#rt-f290) | `LIO/Rogue.lio` row4, type`9`, id`33` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F291 - Planets - Moon0](../../../docs/original-research/FEATURE_CATALOG.md#rt-f291) | `LIO/Rogue.lio` row5, type`9`, id`34` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F292 - Cold Harbor SpawnA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f292) | `LIO/Rogue.lio` row6, type`4`, id`1` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F293 - Northern CrossA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f293) | `LIO/Rogue.lio` row7, type`4`, id`5` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F294 - Northern CrossB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f294) | `LIO/Rogue.lio` row8, type`4`, id`6` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F295 - Northern CrossC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f295) | `LIO/Rogue.lio` row9, type`4`, id`7` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F296 - HyperBuoyWarp](../../../docs/original-research/FEATURE_CATALOG.md#rt-f296) | `LIO/Rogue.lio` row10, type`4`, id`8` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F297 - ColdHarbor](../../../docs/original-research/FEATURE_CATALOG.md#rt-f297) | `LIO/Rogue.lio` row11, type`8`, id`9` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F298 - ColdHarborDock](../../../docs/original-research/FEATURE_CATALOG.md#rt-f298) | `LIO/Rogue.lio` row12, type`7`, id`10` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F299 - RefineryA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f299) | `LIO/Rogue.lio` row13, type`5`, id`11` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F300 - RefineryB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f300) | `LIO/Rogue.lio` row14, type`5`, id`12` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F301 - Global Tellerium SpawnA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f301) | `LIO/Rogue.lio` row15, type`5`, id`16` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F302 - Global Tellerium SpawnC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f302) | `LIO/Rogue.lio` row16, type`5`, id`26` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F303 - Global Krandazite SpawnA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f303) | `LIO/Rogue.lio` row17, type`5`, id`27` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F304 - MeteorShowerA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f304) | `LIO/Rogue.lio` row18, type`5`, id`46` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F305 - DeathOrbSpawn](../../../docs/original-research/FEATURE_CATALOG.md#rt-f305) | `LIO/Rogue.lio` row19, type`5`, id`47` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F306 - Pirate Base](../../../docs/original-research/FEATURE_CATALOG.md#rt-f306) | `LIO/Rogue.lio` row20, type`5`, id`48` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F307 - TradingPostA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f307) | `LIO/Rogue.lio` row21, type`5`, id`49` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F308 - TradingPostB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f308) | `LIO/Rogue.lio` row22, type`5`, id`50` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F309 - KillStationSpawn](../../../docs/original-research/FEATURE_CATALOG.md#rt-f309) | `LIO/Rogue.lio` row23, type`5`, id`52` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F310 - NorthernCross](../../../docs/original-research/FEATURE_CATALOG.md#rt-f310) | `LIO/Rogue.lio` row24, type`8`, id`53` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F311 - Global Tellerium SpawnB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f311) | `LIO/Rogue.lio` row25, type`5`, id`54` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F312 - Global Tellerium SpawnD](../../../docs/original-research/FEATURE_CATALOG.md#rt-f312) | `LIO/Rogue.lio` row26, type`5`, id`55` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F313 - RandomHyperJump](../../../docs/original-research/FEATURE_CATALOG.md#rt-f313) | `LIO/Rogue.lio` row27, type`4`, id`56` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F314 - DeepSpaceWarpA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f314) | `LIO/Rogue.lio` row28, type`4`, id`57` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F315 - CassandraA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f315) | `LIO/Rogue.lio` row29, type`4`, id`58` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F316 - YankeeStationA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f316) | `LIO/Rogue.lio` row30, type`4`, id`61` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F317 - YankeeStationB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f317) | `LIO/Rogue.lio` row31, type`4`, id`62` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F318 - YankeeStationC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f318) | `LIO/Rogue.lio` row32, type`4`, id`63` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F319 - Yankee Station](../../../docs/original-research/FEATURE_CATALOG.md#rt-f319) | `LIO/Rogue.lio` row33, type`8`, id`64` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F320 - Cassandra Station](../../../docs/original-research/FEATURE_CATALOG.md#rt-f320) | `LIO/Rogue.lio` row34, type`8`, id`65` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F321 - Scorpius Nebula](../../../docs/original-research/FEATURE_CATALOG.md#rt-f321) | `LIO/Rogue.lio` row35, type`8`, id`66` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F322 - RepairDockA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f322) | `LIO/Rogue.lio` row36, type`5`, id`67` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F323 - RepairDockB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f323) | `LIO/Rogue.lio` row37, type`5`, id`68` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F324 - RepairDockC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f324) | `LIO/Rogue.lio` row38, type`5`, id`69` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F325 - RepairDockD](../../../docs/original-research/FEATURE_CATALOG.md#rt-f325) | `LIO/Rogue.lio` row39, type`5`, id`70` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F326 - Northern Cross DockA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f326) | `LIO/Rogue.lio` row40, type`7`, id`71` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F327 - Northern Cross DockB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f327) | `LIO/Rogue.lio` row41, type`7`, id`72` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F328 - Northern Cross DockC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f328) | `LIO/Rogue.lio` row42, type`7`, id`73` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F329 - Northern Cross DockD](../../../docs/original-research/FEATURE_CATALOG.md#rt-f329) | `LIO/Rogue.lio` row43, type`7`, id`74` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F330 - Cassandra Dock](../../../docs/original-research/FEATURE_CATALOG.md#rt-f330) | `LIO/Rogue.lio` row44, type`7`, id`75` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F331 - YankeeDockA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f331) | `LIO/Rogue.lio` row45, type`7`, id`76` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F332 - YankeeDockB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f332) | `LIO/Rogue.lio` row46, type`7`, id`77` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F333 - YankeeDockC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f333) | `LIO/Rogue.lio` row47, type`7`, id`78` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F334 - YankeeDockD](../../../docs/original-research/FEATURE_CATALOG.md#rt-f334) | `LIO/Rogue.lio` row48, type`7`, id`79` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F335 - MeteorShowerB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f335) | `LIO/Rogue.lio` row49, type`5`, id`80` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F336 - Global Tellerium SpawnE](../../../docs/original-research/FEATURE_CATALOG.md#rt-f336) | `LIO/Rogue.lio` row50, type`5`, id`81` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F337 - Global Tellerium SpawnF](../../../docs/original-research/FEATURE_CATALOG.md#rt-f337) | `LIO/Rogue.lio` row51, type`5`, id`82` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F338 - MeteorShowerC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f338) | `LIO/Rogue.lio` row52, type`5`, id`83` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F339 - Global Krandazite SpawnB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f339) | `LIO/Rogue.lio` row53, type`5`, id`84` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F340 - Global Tellerium SpawnG](../../../docs/original-research/FEATURE_CATALOG.md#rt-f340) | `LIO/Rogue.lio` row54, type`5`, id`85` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F341 - Northern CrossD](../../../docs/original-research/FEATURE_CATALOG.md#rt-f341) | `LIO/Rogue.lio` row55, type`4`, id`3` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F342 - YankeeStationD](../../../docs/original-research/FEATURE_CATALOG.md#rt-f342) | `LIO/Rogue.lio` row56, type`4`, id`4` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F343 - MeteorShowerD](../../../docs/original-research/FEATURE_CATALOG.md#rt-f343) | `LIO/Rogue.lio` row57, type`5`, id`13` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F344 - RepairDockE](../../../docs/original-research/FEATURE_CATALOG.md#rt-f344) | `LIO/Rogue.lio` row58, type`5`, id`51` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F345 - Marianas FreePort](../../../docs/original-research/FEATURE_CATALOG.md#rt-f345) | `LIO/Rogue.lio` row59, type`8`, id`59` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F346 - MarianasDockA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f346) | `LIO/Rogue.lio` row60, type`7`, id`60` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F347 - MarianasDockB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f347) | `LIO/Rogue.lio` row61, type`7`, id`86` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F348 - MarianasDockC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f348) | `LIO/Rogue.lio` row62, type`7`, id`87` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F349 - AmbientA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f349) | `LIO/Rogue.lio` row63, type`7`, id`88` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F350 - AmbientB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f350) | `LIO/Rogue.lio` row64, type`7`, id`89` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F351 - ColdHarbourDefSatA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f351) | `LIO/Rogue.lio` row65, type`5`, id`90` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F352 - ColdHarbourDefSatB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f352) | `LIO/Rogue.lio` row66, type`5`, id`91` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F353 - ColdHarbourDefSatC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f353) | `LIO/Rogue.lio` row67, type`5`, id`92` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F354 - NorthernDefSatA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f354) | `LIO/Rogue.lio` row68, type`5`, id`93` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F355 - NorthernDefSatB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f355) | `LIO/Rogue.lio` row69, type`5`, id`94` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F356 - NorthernDefSatC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f356) | `LIO/Rogue.lio` row70, type`5`, id`95` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F357 - CassandraDefSatA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f357) | `LIO/Rogue.lio` row71, type`5`, id`96` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F358 - CassandraDefSatB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f358) | `LIO/Rogue.lio` row72, type`5`, id`97` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F359 - CassandraDefSatC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f359) | `LIO/Rogue.lio` row73, type`5`, id`98` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F360 - YankeeDefStatA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f360) | `LIO/Rogue.lio` row74, type`5`, id`99` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F361 - YankeeDefStatB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f361) | `LIO/Rogue.lio` row75, type`5`, id`100` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F362 - YankeeDefStatC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f362) | `LIO/Rogue.lio` row76, type`5`, id`101` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F363 - Global Iron SpawnA](../../../docs/original-research/FEATURE_CATALOG.md#rt-f363) | `LIO/Rogue.lio` row77, type`5`, id`14` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F364 - Global Iron SpawnB](../../../docs/original-research/FEATURE_CATALOG.md#rt-f364) | `LIO/Rogue.lio` row78, type`5`, id`15` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F365 - Global Iron SpawnC](../../../docs/original-research/FEATURE_CATALOG.md#rt-f365) | `LIO/Rogue.lio` row79, type`5`, id`17` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F366 - Solar Mold Spawn](../../../docs/original-research/FEATURE_CATALOG.md#rt-f366) | `LIO/Rogue.lio` row80, type`5`, id`18` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |
| [ ] [RT-F367 - MoldSporeSpawn](../../../docs/original-research/FEATURE_CATALOG.md#rt-f367) | `LIO/Rogue.lio` row81, type`5`, id`19` | Not started | OBJECT: placement/condition/timer/action/dependencies; world + Client, unassigned |

## Completion history

Canonical raw indexing and the pinned compiled parser accept the unchanged ITM200/VEH30/RPG12/LIO81/LVL files. Package audit RT-F381 verifies 19,198 supported literal item values, twelve hull links and 222 map references. RT-F383 validates original arena initialization; RT-F392 validates bounded full-asset hosting without a native player. All 89 canonical BLOs are inventoried separately from the eleven-archive pixel export. Evidence: [package audit](../../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md), [runtime plan](../../../docs/architecture/ROGUE_RUNTIME_PLAN.md), [dependency plan](../../../scripts/original-assets/DEPENDENCY_PLAN.md). These scoped capabilities do not complete any unchecked record-specific gameplay feature here. Preserve completed ID/evidence in this history, the master audit and [CHANGELOG.md](../../../CHANGELOG.md).

Original overview RT-F020 and standalone package RT-F373 are Complete for original 1999×1998 pixels/proportions/source identity, inspected source/export captures and unchanged world tick0. They are removed from active work. Native RT-F021/F022 and full graphical LVL delivery RT-F387 remain open.

## Ancillary original zone dependencies

- [ ] [RT-F368 - keys.txt](../../../docs/original-research/FEATURE_CATALOG.md#rt-f368): Investigating native behavior. Canonical CFG HelpMenu row183 references original pinned Zone-Assets rogue ancillary text; preserve exact source/provenance and verify integration, no placeholder.
- [ ] [RT-F369 - infcmdlst.txt](../../../docs/original-research/FEATURE_CATALOG.md#rt-f369): Investigating native behavior. Canonical CFG HelpMenu row184 references original pinned Zone-Assets rogue ancillary text; preserve exact source/provenance and verify integration, no placeholder.
- [ ] [RT-F370 - comms.txt](../../../docs/original-research/FEATURE_CATALOG.md#rt-f370): Investigating native behavior. Canonical CFG HelpMenu row185 references original pinned Zone-Assets rogue ancillary text; preserve exact source/provenance and verify integration, no placeholder.
- [ ] [RT-F371 - rules.txt](../../../docs/original-research/FEATURE_CATALOG.md#rt-f371): Investigating native behavior. Canonical CFG HelpMenu row186 references original pinned Zone-Assets rogue ancillary text; preserve exact source/provenance and verify integration, no placeholder.
- [ ] [RT-F372 - mods.txt](../../../docs/original-research/FEATURE_CATALOG.md#rt-f372): Investigating native behavior. Canonical CFG HelpMenu row187 references original pinned Zone-Assets rogue ancillary text; preserve exact source/provenance and verify integration, no placeholder.
