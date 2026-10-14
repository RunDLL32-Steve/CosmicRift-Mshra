# Engine feasibility evaluation — original Rogue Trader fidelity

Reviewed October 13, 2026 (America/Chicago). Status authority: [master §1.2 fidelity clarification](../../ROGUE_TRADER_2_PLAN.md#12-user-fidelity-clarification-and-implementation-tracking--october-12-2026), §§5, 7, 13, 16 and M0/M1. Reconcile with [architecture subplan](IMPLEMENTATION_PLAN.md), [project progress](../development-log/PROGRESS.md), [InfantryOnline research](../original-research/INFANTRYONLINE.md) and the [fowlmouth archive audit](../original-research/COSMIC_RIFT_ASSETS.md).

**Decision: engine choice remains open.** InfantryOnline is a credible path to testing the preserved content with a legacy client, but the inspected repository provides a server emulator and tools, rather than a recovered client rendering/physics engine. Neither this review nor the completed Godot engineering prototype establishes original Rogue Trader parity. The least rewriting route depends on a compatible client actually loading the preserved zone and reproducing its mechanics.

The initial evaluation was static. Subsequent actual parser/arena/backend/native-bootstrap evidence is now recorded in the current checkpoint below; these scoped completions replace the old pending-execution statements. No original asset was changed or published. Documentation reconciliation launched no program or simulation.

## What is available, and what has not been proved

| Question | Documented fact | Implication / uncertainty |
|---|---|---|
| Is this the original game engine source? | The [server README][server-readme] identifies its output as a zone-server emulator and directs users to acquire a separate client. The complete pinned server tree contains launcher, editor/parser and server projects. | Emulator and launcher source do not supply the client renderer, original player physics implementation or full client UI source. None was identified in this inventory. |
| Is a separate client repository present? | The [complete organization API inventory][org-api] returned seven repositories: server, Zone-Assets, Tools, Discord-Bots, IGBL-Website, Playable-Zones and Editor. No client engine repository appeared. | This is a bounded finding about the public organization inventory, not proof that client source exists nowhere. |
| Can a client be obtained? | The maintainer's [download page][client-download] links to [FreeInfantry on Steam][steam-client] and other distribution routes. | A current FreeInfantry binary is available through an identified channel. Actual native binaries have since been acquired/hashed; a derived private-profile copy reached welcome. Supported build identity and Cosmic Rift/RT arena entry remain unproved; see NATIVE_CLIENT_PLAN.md. A launcher is not itself the rendering client. |
| Does the server understand relevant content? | [Vehicle definitions][vehicle-parser], [Car definitions][car-parser], [terrain speed definitions][speed-parser], [projectile definitions][projectile-parser] and [level tiles][level-tiles] represent controllable vehicles, radius/weight, thrust/rotation/reverse/strafe/hyper thrust, bounce, projectile inheritance/lifetime/damage, and tile physics/terrain. | The shared formats are a strong compatibility lead. Presence of fields does not establish their original client algorithms, units, sentinel interpretation, or complete RT behavior. |
| Is overhead space presentation supported? | Archived `rogue.cfg` sets `Isometric=0`; [server initialization][server-init] selects Y scale 1.0 for that setting and 0.7 for isometric zones. | Overhead data is recognized. Exact RT rendering, controls, palettes, ship directions, UI and camera scale still require client/content validation. |
| Is the recovered package already playable? | The pinned [Zone-Assets archive][rogue-archive] places it under `Incompatible Zone Archives/SilentClientCrash`; the [archive README][archive-readme] describes those folders as observed incompatibility categories. The matching fowlmouth payloads/text are documented in the archive audit. | Matching bytes establish provenance, not a repaired zone. Current native bootstrap reached welcome only; no RT arena entry or archived crash cause is proved. Do not rename the package compatible merely because an emulator can parse selected values. |
| Does the latest server build match the prototype SDK? | The pinned [ZoneServer project][server-project] targets `net10.0`. Its active solution is [`dotnetcore/InfServerNetCore.sln`][modern-solution]; the root solution currently lists only LogEvictor. | README instructions for the older `InfServer.sln` layout are insufficient for a reproducible modern build. A separate toolchain/version choice is needed for a compatibility spike; the existing Godot/.NET 8 prototype need not change. |

The asset archive supplies substantially more original content than the initial plan knew about. It does not supply the missing execution semantics by itself. The current package's hull names, items, equipment restrictions, maps and original presentation are fidelity targets; the master's explicitly provisional values should be replaced only through a validated mapping.

## Version 53 / 57 parsing: promising, not certified

`Rogue.veh` records identify version 53. The [active vehicle base parser][vehicle-parser] reads the version and branches by revision, including fields introduced through version 52; the [Car parser][car-parser] also uses revision conditions for sprite layouts, thrust, hyper thrust and other fields. Version 53 falls within those branches. The generic [CSV loader][csv-parser] checks record consumption. Actual pinned parser acceptance now passes RT-F072. Exact native evaluator/client compatibility remains open.

`Rogue_mp.itm` identifies version 57. The [active item base loader][item-loader] reads that version, but the [active projectile loader][projectile-parser] accesses fixed field positions without version-conditioned dispatch. The older [Parsers/Items/Projectile.cs][legacy-projectile-parser] does have revision branches, with many fields still named as unknowns. There are two parser families to compare; neither should be described as a fully validated v57 behavioral specification.

An in-memory, quote-aware CSV length audit found 92 type-6 projectile records in the preserved local `Rogue_mp.itm`; none was shorter than the active projectile loader's largest direct numeric `values[index]` access (174). This limited check did not run the emulator parser, validate graphics/sound layouts or all item types, or prove that any field means what the client expects. The existing research audit records candidate plasma/MIRV/laser indices and negative values; retain those raw values until sentinel and scaling semantics are resolved.

A decoder must report record type/version, consumed columns, unresolved IDs and unsupported fields explicitly. Silently discarding unknown fields or forcing all records into the prototype's single plasma model would lose source fidelity.

## Client authority and missing mode behavior

The emulator's [ScriptArena player-update path][player-update] assigns client-provided energy, positions and velocities to server state; it also applies client health to an occupied vehicle. Item ownership/skill/ammo checks exist, and [client explosion notifications][explosion-update] drive damage to applicable server vehicles. These facts do not establish a wholly server-authoritative 60 Hz simulation of player flight/projectile combat. Reusing the emulator therefore requires reconciling its trust model with master §§13.2 and 13.5, rather than assuming that requirement is automatically satisfied.

The [bot movement controller][bot-movement] contains thrust/friction/speed scaling and tile collisions, but also a fixed terrain selection and incomplete-physics comments. It is evidence for an emulator model, not recovered original player-engine source. Applying its scaling constants to all RT values without measurements would be another reconstruction.

The complete pinned tree has no Cosmic/Rift/Rogue-named GameType path. That does **not** prove every RT rule needs a new script: the archived CFG has events, and the ITM/RPG/LIO data can encode equipment, transformations and triggers. The emulator supplies [generic buying/selling and script hooks][shop-update] and [script registration/selection][script-arena]. Still unproved are the full mineral/refinery chain, cargo mass effects, outpost ownership/build limits, bounties, death losses and persistence for this zone/revision. Trace those data/event commands to their evaluators first, then list precisely which rules work, differ, or require a game script.

The [README][server-readme] documents standalone operation by setting database connection delay to zero. The [implementation][standalone-init] confirms `database/connectionDelay=0` disables the database connection. This is useful for a later local compatibility test; it does not prove between-session progression. Directory-free local zone selection is described in the [quickstart][quickstart]. No account was created or external server contacted beyond reading public source pages.

A concrete local-isolation detail: [current socket initialization][socket-bind] binds the game and ping sockets to `0.0.0.0`, despite storing a configured `bindIP`. Completed derived bounded hosts verify actual game/ping loopback endpoints; upstream production binding semantics still need reconciliation. Merely putting `127.0.0.1` in a zone listing or config is insufficient to establish local-only network exposure for this pinned implementation.

## Routes to compare before choosing an engine

| Route | Likely work saved | Required work / limits | Feasibility now |
|---|---|---|---|
| Preserved client + compatible emulator + RT zone | If compatible, retains original client rendering/UI, sprite handling and client movement/projectile semantics; may avoid rebuilding their details. | Pin a obtainable client build; resolve SilentClientCrash and content dependencies; establish supported data revisions; implement only missing RT rules; reconcile persistence and authority; determine maintenance/packaging constraints of a client whose source was not found. | Best candidate for an early compatibility investigation. It is not yet a verified foundation for the recreation. |
| Godot client + original-content decoder + authoritative C# rules | Retains the tested modular prototype, modern build/export process and controllable authority model. | Decode BLO/LVL and versioned definitions; preserve sprite/UI appearance; recover units and algorithms; map all reference IDs; implement and compare weighted inheritance, shield rules, strafe/hyper thrust and economy. Raw archive values cannot simply replace provisional JSON numbers. | Engineering baseline already works; original fidelity requires substantial additional validation and implementation. |
| Use emulator source as if it were the whole client engine | Some schemas, server operations and script dispatch are available for study. | A renderer/client engine is still absent, player authority differs, and bot formulas do not close the original-client gap. | Not a demonstrated shortcut. Evaluate components on their own evidence rather than calling the repository an interchangeable engine. |

**Conditional recommendation:** investigate the legacy client/zone compatibility path before spending heavily on a full behavior rewrite. Continue maintaining the Godot prototype while that investigation produces evidence. Select the legacy route only after the source revision, crash fix, actual flight/combat presentation, mode rules, persistence and authority constraints are understood. If that route cannot meet the project requirements, use its validated observations and preserved content mapping to guide the Godot recreation. No production engine change has been made; choice depends on compatibility evidence.

## Compatibility spike and decision gates

Reference/tool/parser initialization and bounded loopback/full-asset hosting are now completed history; native entry, behavior and production decisions remain pending. See the scoped current evidence below.

1. **Freeze references.** Pin all three repositories and verified LFS payload hashes in a manifest. Store originals in an ignored immutable reference area and hash before/after tool sessions. Editors must never save there.
2. **Separate working copies.** Use an ignored derived zone such as `.tools/compatibility/working-zones/rt-compat-001/` and a fresh isolated client profile. Log every rename/conversion/config change with original and derived hashes. Exclude reference/working assets and original binaries from release exports.
3. **Validate data first.** Follow `rogue.cfg` through all LVL/LIO/VEH/ITM/RPG/help and BLO/object/item/vehicle dependencies. Compare `Rogue.itm` with `Rogue_mp.itm`. Report unsupported revisions, column mismatches, missing IDs/assets and unknown event commands. Recover map collision/terrain separately from BMP presentation. Preserve unresolved units and negative/sentinel values; invent no conversions.
4. **Pin and isolate execution before any authorized launch.** Record client build/hash/channel, emulator commit/toolchain and configs/scripts. Verify database delay zero, disabled directory/remote updates and actual game/ping sockets. Begin with a compatible control zone, then add RT dependencies in stages. Reproduce/log the first failure; patch only a derived working copy.
5. **Measure flight/combat fidelity.** Compare forward/reverse/coast/rotation/strafe/hyper thrust, cargo mass, collisions, weapon inheritance, cooldown/energy/lifetime, shield-to-hull behavior and MIRV child IDs. Compare original sprite/UI/map scale to applicable footage. Validate original scaling against the chosen client; the emulator bot constants are hypotheses, not recovered truth.
6. **Trace mode rules.** Check collection/refining/selling, equipment limits/upgrades, outposts, bounties/death losses and logout/restart persistence. Identify whether each comes from data, generic code or custom scripts. Loading a zone or killing a target does not establish the RT loop.
7. **Choose from evidence.** Compare fidelity, unresolved authority/persistence requirements, missing-source dependencies, maintenance and packaging. Record an architecture decision and reconcile master §1.2/subplans before switching engines or committing to the full Godot decoder. Preserve all failed/unproved gates as open tasks.
Licensing/provenance is an input to that decision: inspected repository metadata reports no root license grant for the server/archives, while selected tool components have their own notices. The original client and original media have separate provenance. Public downloads and successful decoding do not themselves document redistribution permissions. Record the applicable terms/permission for each component before bundling it. This evaluation reaches no legal conclusion and does not block the permitted metadata, source and compatibility-design work.

## Evidence and remaining status

- Read current master fidelity scope and prior archive audits.
- Inspect complete organization inventory and pinned server tree; distinguish server/launcher/client availability.
- Inspect active build target, parsing families, vehicle/projectile/map schemas, update authority, script hooks and standalone behavior.
- Specify immutable references, separate working-zone provenance and concrete comparison gates.
- [ ] Validate remaining v53/v57 evaluator/native semantics and runtime dependency closure; actual parser/inventory/package audits are completed history.
- [ ] Reproduce and resolve SilentClientCrash on a pinned compatible client.
- [ ] Verify actual native RT arena entry/unspectating; actual parser/backend execution and welcome-screen bootstrap are completed narrower history.
- [ ] Recover exact original movement/combat semantics and full RT economy/persistence.
- [ ] Choose an engine and reconcile the master acceptance criteria with measured fidelity.

[org-api]: https://api.github.com/orgs/InfantryOnline/repos?per_page=100
[server-readme]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/README.md
[quickstart]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/quick-start.md
[server-project]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/ZoneServer.csproj
[modern-solution]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/InfServerNetCore.sln
[client-download]: https://www.freeinfantry.com/download/
[steam-client]: https://store.steampowered.com/app/2830720/FreeInfantry/
[archive-readme]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/README.md
[rogue-archive]: https://github.com/InfantryOnline/Zone-Assets/tree/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue
[vehicle-parser]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Veh/VehInfo.cs#L125-L268
[car-parser]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Veh/VehInfo.Car.cs#L50-L155
[speed-parser]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/SpeedValues.cs
[item-loader]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Itm/ItmInfo.Item.cs#L73-L100
[projectile-parser]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Itm/ItmInfo.Projectile.cs#L139-L279
[legacy-projectile-parser]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/Parsers/Items/Projectile.cs#L92-L220
[csv-parser]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/CsvFile.cs
[level-tiles]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Lvl/LvlInfo.Tile.cs
[server-init]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/ZoneServer.cs#L201-L211
[standalone-init]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/ZoneServer.cs#L338-L352
[socket-bind]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/ZoneServer.cs#L426-L436
[player-update]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/Arena/ScriptArena.cs#L1295-L1346
[explosion-update]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/Arena/ScriptArena.cs#L1179-L1204
[shop-update]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/Arena/ScriptArena.cs#L1606-L1666
[script-arena]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/Arena/ScriptArena.cs#L64-L96
[bot-movement]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/Arena/Bot/MovementController.cs#L25-L29



## Current measured checkpoint — October 13, 2026

| Gate | Current evidence |
|---|---|
| Reference/toolchain/data | Complete: pinned originals/server, isolated SDK10 actual parser; 200 items, 30 vehicles, 12 hulls, 81 LIO records, original 1999×1998 LVL. |
| Asset inspection | Complete for scoped Godot original viewer/export and inventories: standalone input plus three rendered pages, world tick 0; 89 BLOs/1,262 members/609 raw candidates. Native appearance remains open. |
| Package/source model | RT-F381/382/385 Complete: 19,198 direct values, 12 hull links, 222 map member links, offline 46-file roundtrip, generic original event/runtime routes and eight-field derived VehicleMaker repair. |
| Actual original arena | RT-F383 Complete: 25 computers, 100 drops, 51 teams, 42 hides; no clients/begin/poll/physics. |
| Full-asset backend | RT-F392/394 Complete: 89 canonical plus authentic shared Soccer working BLOs, one original RT arena, two actual loopbacks, 120.17-second exit 0, maximum clients 0, original hashes unchanged. |
| Native entry | RT-F380/384 In progress: approved derived private namespace copy reached welcome; user Start gave no confirmed entry. Computer Use stopped while user played another game; owned processes/private registry cleaned up. |
| Native delivery/gameplay/persistence | Open: RT-F387, original rendering/flight/combat/mining/economy/upgrades/rewards, durable progression, authority, shutdown and final engine choice. RT-F393 compiled guard awaits actual CS_Ready acceptance. |

Current owners and exact evidence: [legacy](LEGACY_COMPATIBILITY_PLAN.md), [native](NATIVE_CLIENT_PLAN.md), [package](ROGUE_PACKAGE_AUDIT_PLAN.md), [runtime](ROGUE_RUNTIME_PLAN.md), [tooling folders](../../scripts/compatibility/IMPLEMENTATION_PLAN.md). These checkpoints refine the historical investigation steps above; completed work is not an active rerun task. No desktop interaction while the user is playing another game.
