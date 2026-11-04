# Changelog

## The sector is the full-size original map — November 4, 2026

-   G10 F10: the game now flies the original Rogue map at its real size, 1999 × 1998 tiles of 16 pixels, instead of a practice sector drawn about 23 times smaller. Crossing it at the Asp's top speed takes 98 s, as the original reading predicts. World units are the original map's pixels, and every LIO position is moved by the map's 48-pixel offset as the community server does.
-   The walls come from `LVL/Rogue.lvl` through the G6 reader (`data/sectors/rogue_map.json`, derived and checked tile by tile against the pinned map). A ship bounces off a wall the way the community server's bot does: it keeps 72.7 % of its speed and reverses the part that crossed into the wall. Shots stop at walls, and nothing can leave the map.
-   The player spawns at Cold Harbor SpawnA, the warp the CFG's initial entry group gives the Cold Harbor team (assuming the solo pilot is on that team, NEW). The Trading Outpost screen now sits at the Cold Harbor SpaceDock, the original store zone. The refinery and every mineral spawn stand at their original places, so the refinery is about 9,000 pixels (half a minute) from the spawn. The practice targets and asteroids keep their layout and moved to a range east of Cold Harbor.
-   A drop now lasts as long as the terrain it lands on allows (CFG PrizeExpire): 12 minutes in open space and 2 minutes in a dock, refinery or trade zone. This covers mineral drops and a destroyed ship's spilled cargo. The pickup distance is the original 80 pixels, and drops never land on a wall.
-   A NEW radar in the bottom-right corner shows the whole map with you, the station, the refinery, targets and drops. Whether the original had a radar is still unknown.
-   Saves carry over: a sector saved before this change is moved onto the map once, so no pile is refilled or lost. A ship saved in the old practice sector keeps its hull and starts at Cold Harbor. Not done yet: flight changes inside docks and trade zones (F12), selling at the original Trading Posts and repairing at the Repair Docks instead of all at Cold Harbor, and the second refinery.

## Owned ships, a ship picker and the cost of being destroyed — November 3, 2026

-   Steve's rules (November 3, 2026): a player owns several ships, at most one of each hull, saved on their account, and picks one when they get into the game (the picker opens when the client and map load). The picked ship enters with the hull it was saved with, so quitting or switching ships never repairs damage for free; the station's Full Repair restores a full hull for 500 credits (Steve, November 4, 2026). A new or just-bought ship starts at full hull. A destroyed ship respawns at the spawn with a full hull and keeps its weapons and everything bought, but the cargo in its hold (minerals and every other hold item) leaves the account when it reaches 0 hull and is dropped into space where it died, one pile per item, which anyone can pick up and which expires after 720 s like other drops (Steve, November 4, 2026). The player also loses 20% of the credits they hold, rounded down. Cargo picked up just before the destruction is saved first, so it drops too and cannot come back after the respawn. Full Repair now fills any hull, including the legacy Interceptor's 100 points.
-   Profile schema 6 adds `ownedShips`. A schema 5 save loads with its hull as its only owned ship, still active and in the same condition; nothing else changes. Before the first save at schema 6 the repository keeps a byte-for-byte copy of the old file as `<id>.profile.schema5.json`, which later saves never overwrite.
-   The server decides every change. Buying a hull (`BuyShip`, at a station, one of each) and the destruction penalty (`ShipDestroyed`) are idempotent transactions saved before they are acknowledged. A destruction is keyed by world and tick, so a retry, reload or relaunch never charges twice. Switching ships parks the old ship exactly as it was, so switching is never a free repair. A ship save at hull 0 is refused, so a wreck cannot be saved and reloaded to dodge the penalty. A reset while the destruction is unsaved is refused too.
-   The 12 original hulls are real ships now (Steve, November 3, 2026). `OriginalShipBuilder` derives `data/ships/original_*.json` and `data/ships/roster.json` from the pinned files: name, flight (G10's `OriginalShipPhysics`), hold cap (VEH f[48] StopWeight), hull points (VEH f[6], raw 50). New pilots start in the Asp (RPG row 1). Older saves keep their Interceptor, which stays on the roster and counts as a Terran hull.
-   The Trading Outpost gets a Ship Hulls page that sells the 12 type-16 exchanges (ITM rows 181–192) at their original 15,000 credits. As in the original, a hull can only be bought by a pilot who already owns one of the same-race hulls that ITM f[5] names (`ShipRequirementNotMet` otherwise).
-   Placeholders, listed in each ship file's `newValues`: radius 14 (VEH f[35]=21 is only a candidate), shield 50, energy 100 and 20/s (raw f[94..96] 1700/1050/1700, units undecoded). Every hull fires the Plasma Cannon for now; the Grazi start with the D-Cannon and the Skithzar with the Bio Cannon in the originals. Hull sprites are not per-hull yet.
-   The client reads the flyable hulls from the roster, shows the picker at launch, and gives each ship its own hold cap.

## The ship flies like the original Asp — November 3, 2026

-   Steve asked to implement the original models after the G10 check. The ship now uses the Asp's own flight values from `VEH/Rogue.veh`, read the way the InfantryOnline emulator reads them: top speed 325 px/s (was 360), acceleration 168.7 px/s² forward and backward (was 120), so full speed comes in 1.9 s instead of 3.0 s, a turn rate of 207°/s (was 189°/s), and a bounce that keeps 72.7 % of the speed (was 25 %). The Plasma Cannon fires every 250 ms (was 200 ms), from its original fireDelay of 25. Ships still coast without drag in open space, as in the original. A test fails if the ship or weapon file drifts from the original values.
-   `OriginalShipPhysics.From(hull)` and `OriginalShipPhysics.All(hulls)` give each of the twelve original hulls its own flight values (for example the Hornet turns at 248°/s and the Excelsior tops out at 357.5 px/s), ready for the owned-ships roster.
-   Not changed yet: the practice sector is still drawn at 1/23 of the original map size, so it takes about 4 s to cross instead of about 98 s. Fixing that needs the original spawn point, the map walls and a radar. Strafe and hyper wait on how the original keys worked, and projectile speed, range, damage and energy keep their NEW values until their units are known. Old saves load unchanged (position, heading, hull and upgrades are kept; upgrades still add their percentage on top).
-   Engine upgrades now raise reverse thrust by the same share as forward thrust. Before, only forward thrust went up, which broke the rule that reverse equals forward.

## Flight and weapon check against the originals — November 3, 2026

-   Queue G10, first slice: a [validation table](docs/original-research/G10_VALIDATION.md) compares the prototype's flight, weapon, controls and HUD with the original files read the way the InfantryOnline emulator reads them. The prototype numbers are measured by running the simulation, and a test fails if either side changes. Two rows match the original reading (reverse thrust equals forward thrust, and ships coast without drag in open space). The rest differ or are missing: the Asp's top speed reads as 325 px/s against the prototype's 360, it reaches it in 1.9 s against 3.0 s, it turns at 207°/s against 189°/s, it bounces keeping 73 % of its speed against 25 %, and the Plasma Cannon fires every 250 ms against 200 ms. The biggest gap is scale: the practice sector shrinks the original map about 23 times, so it takes 4 s to cross instead of about 98 s. Strafe, hyper, dock terrains and the original HUD are not in the prototype yet. Nothing in the game changes; the table lists questions only the original game can answer. Counts unchanged.

## A sector reset keeps hull damage — November 3, 2026

-   Steve's rule (November 3, 2026): resetting the sector puts your ship back at the spawn with the hull it had, so a reset is no longer a free repair. A destroyed ship still comes back with a full hull (NEW/provisional, unchanged). Repairs stay at the station.

## Original map reader: walls, terrain zones and map objects — November 3, 2026

-   Queue G6: the game can now read the original Rogue Trader map file (`LVL/Rogue.lvl`, 1999 × 1998 tiles of 16 pixels) and its object list (`LIO/Rogue.lio`). `OriginalLevel` keeps every byte of the map (header, the 222 art references, the three tile layers and 22,356 placed-object records), `OriginalCollisionGrid` answers per tile whether it is a wall (116,065 wall tiles), its vision value and its terrain zone (open space 0, repair dock 9, space dock 10, refinery/trade zone 15), and `OriginalLio` reads all 81 map objects (warp fields, spawns, sounds, texts, planets) with their position and size. Every refinery, trading post and repair dock spawn point lands inside its matching terrain zone, which cross-checks the two files. A broken or unknown file version is refused with a clear reason. Data only: nothing in the game uses the map yet, and field meanings follow the community server's reader, not the original client. [OriginalData entry](shared/RogueTrader.OriginalData/plan.d/g6.md). Counts unchanged.

## The mineral field follows the original server's rules — November 3, 2026

-   Steve asked for the mineral field to follow the InfantryOnline emulator's reading of the Rogue.lio hide rows (from the Zone-Assets comparison, now in this change). This replaces the "about five full holds" cap from earlier in the same change. Each drop is 1 unit (f[21]). A spawn drops a clump of 15 (Krandazite 10) in a small box around a random point in its area, and an area refills only once it is empty (f[18]=1). The whole sector holds at most 100 Tellerium, 100 Iron Ore and 50 Krandazite (f[19]). f[14] is now read as the fewest players the spawn needs (0, 40, 60 or 80), not a clear radius, so in single player only Tellerium A-C, Krandazite A and Iron Ore A-C run. Drops vanish 720 s after landing ([Terrain0] PrizeExpire). Failed tries wait 300 s and the next try after a success comes 300 s later (f[11]/f[12] read as tenths of a second). Tellerium SpawnG, skipped before, is now included.
-   A fresh sector starts with one clump per active spawn, about 45 Tellerium, 10 Krandazite and 45 Iron Ore, not every spawn full.
-   Drop expiry is saved with the sector and keeps running while docked, paused or closed, like the spawn timers. A sector saved before this change gives its piles a full 720 s from the next launch, so a field stocked under the old limits drains back down.
-   NEW: there is no terrain map yet, so the open-space expiry applies everywhere; a drop that would land in an asteroid uses up a try; the player count is 1 until a dedicated server supplies it.

## Zone-Assets Rogue Trader comparison — November 2, 2026

-   Research note [ZONE_ASSETS_COMPARISON.md](docs/original-research/ZONE_ASSETS_COMPARISON.md). The Rogue Trader files in InfantryOnline/Zone-Assets (`401633b`) are the same revision as our canonical pin: CFG, ITM, VEH, RPG, LIO and NWS differ only in line endings, and `Rogue.lvl`, `cruiart.blo` and `UtilityEffects.blo` have the same SHA-256. Ammo still has no weight in any copy, and mineral weights are unchanged.
-   Decoding the map settles G8 item 6: the Rogue LVL uses only terrains 0, 9, 10 and 15, which sit on the LIO repair docks, space docks, refineries and trading posts. The orphan `roguetrader.lvl` is an older version-5 build of the same map with the same walls.
-   The InfantryOnline emulator's LIO hide names read the mineral rows differently from our builder: 1-unit drops (f[21]) placed in clumps of 15 or 10, an area refilling only when empty, map-wide caps of 100 Tellerium, 100 Iron Ore and 50 Krandazite, five spawns gated on 40–80 players, and dropped items expiring after 720 s in open space. Steve adopted this reading on November 3 (entry above).

## Weapon and ammo value audit — November 2, 2026

-   Queue G5 remainder: a [value audit](docs/original-research/G5_WEAPON_VALUE_AUDIT.md) of the original weapon, ammo and child-chain records, with the exact row, field and raw value for each open question. The MIRV/Wake Caster "offset" turns out to be one of six child-entry fields (sideways and forward offsets, height, heading change and a link mode, per the upstream parser's names); all six are now kept. Seven more raw projectile fields are exposed (ammo per shot, ammo capacity, bounce count, proximity radius, inactive time, damage mode, point-blank). The meanings of inheritedSpeed -550 and aliveTime -65, all units, trigger/expiry behavior and collision shape remain open, and the pinned ammo weights (all 0) disagree with the 2003 news notes. No firing behavior. [OriginalData entry](shared/RogueTrader.OriginalData/plan.d/g5.md). Counts unchanged.

## The world keeps its state: no more credit farms from relaunch or reset — November 2, 2026

-   Owner rule (Steve, November 2, 2026): the game will be multiplayer, so the world keeps its state for as long as the server runs. Relaunching the client, logging out or resetting the sector no longer gives back the targets, the cargo piles (about 2,700 credits of wreckage) or the mineral field. A collected pile stays gone, a destroyed target group comes back only 1 minute (real time) after the last target fell, and mineral spawns carry on with their own timers and random state instead of restocking.
-   In the local build the client hosts the world, so the save folder stands in for the running server. The sector (`WorldSimulation.CaptureSector`/`RestoreSector`) is written to `world/<sector>.sector.json` next to the profile, under the same single-owner lock, with an atomic write, one backup generation and quarantine for unreadable files (`SectorStateStore`). It is saved before any pickup or kill reward is granted to the profile, and rewards still owed are kept in that file, so a crash between the two saves can neither refill the sector nor lose the reward. Sector reset now only puts your ship back at the spawn.
-   Mineral respawns keep running in real time while you are docked, paused or in the inspector, and while the game is closed (Steve, November 2, 2026). The piles that fall due appear exactly as they would have in flight, and each spawn still stops at its pile limit.
-   Sector saves are coalesced to at most one every half second (each one flushes about 90 KB, median ~5 ms in the cloud container), and a reward is granted once the save covering it is done.
-   Players keep their items: wallet, hold, upgrades and ship were already saved in the profile; now cargo and kill rewards that were not saved yet also survive a quit or crash and are granted once on the next launch.
-   NEW and still open: a player who edits or deletes files in their own save folder, or moves the system clock forward, can still cheat the local build. That ends when a dedicated server owns this state.

## Old saves over the new hold cap can refine again — November 2, 2026

-   A save made under the old 4,000,000 hold cap can carry more than the Asp's new 2,500,000, and until now it could not refine at all: refining adds the refined good, so the cargo check ran and refused the whole hold. A change to the hold is now allowed when the result is within the cap or no heavier than the hold already is (`CargoCatalog.AllowsChange`, used by `CatalogCargoPolicy`). Every original refinery recipe makes the hold lighter (Tellerium 50,000 to 25,000, Krandazite 100,000 to 50,000, Iron Ore 100,000 to Alloy 75,000), so an over-cap save can refine straight away; it still cannot pick up or buy anything until it is back under the cap. No profile is rewritten and the schema is unchanged. The no-heavier rule is NEW.
-   TASK_QUEUE.md: M7, C9 and C10 move from the active rows to the merged history.

## Smaller mineral piles, per-ship hold weight and reproducible smoke runs — November 2, 2026

-   Mineral piles now hold 1 unit each, and each spawn area keeps up to 100 piles (50 for Krandazite). The LIO fields are read the other way round from before: f[18]=1 is the pile size and f[19] the most piles at a time. A fresh sector starts each spawn full, as Steve asked. Before this, a single 100-unit Tellerium pile weighed more than a whole hold.
-   The hold's weight cap belongs to the ship now (`ShipDefinition.Cargo.Capacity`), not the station. The starter ship takes the original Asp's VEH f[48] StopWeight of 2,500,000, which is 50 Tellerium at its original ITM weight, replacing the made-up 4,000,000. Reading StopWeight as a hard cap is NEW; the half-capacity performance penalty stays open in RT-F030.
-   The derived station and mineral JSON are now written with LF line endings on every OS, so their pinned-data oracle tests pass under `scripts/verify.ps1` on Windows (the inherited CRLF mismatch from review R3-2).
-   Fixes a flaky station smoke test. Mineral placement was seeded from `string.GetHashCode` of a random world id, which .NET randomizes per process, so a Tellerium pile sometimes landed on the launch lane and filled the hold before the wreckage step. Placement now takes its salt from the world id's GUID bytes (`MineralFieldCatalog.ForWorld`), so a logged world id replays its field. Smoke runs number their worlds, so every acceptance run lays the field out the same way. Normal play still gets a new random field for every world.

## Recoverable sector transfers — November 2, 2026

-   Queue C10: a server-side model for moving a player's ship from one sector to another that survives crashes and retries. One journal entry per player says which sector owns the ship; a transfer freezes the ship in the source, reserves a checked spawn point in the destination, and only then flips ownership in a single write. After a crash, anything before that write is undone and anything after it is finished, and repeating a request never moves the ship twice. Tests crash it at every step, restart it, run two servers at once and soak it with random faults, and the ship is never lost or active in two sectors. Not wired into the game yet (there is one sector), and no original sector or gate rule is encoded; all of it is NEW. [Persistence entry](server/RogueTrader.Persistence/plan.d/c10.md). Counts unchanged.

## Bounty and reputation rules — November 2, 2026

-   Queue C9: a NEW data-driven rule file (`data/conduct/rules.json`) turns named conduct events into bounty and per-category reputation changes, and the transaction core records them once per transaction id, with clamping and no decay. Profile schema 5 saves bounty and standing; older saves open with no bounty and neutral standing. Nothing in the game raises these events yet. [Persistence entry](server/RogueTrader.Persistence/plan.d/c9.md), [data](data/conduct/IMPLEMENTATION_PLAN.md). RT-F051 stays open; counts unchanged.

## Server input validation — November 2, 2026

-   Queue C8: the headless server session now refuses thrust or turn outside [-1, 1], inputs that acknowledge a snapshot the server has not produced, and submissions beyond a per-sender allowance (four per tick, bursts up to 120), all before anything is queued. A motion audit flags any tick in which the ship moves faster than its maximum speed and any move between ticks that bypassed the session. Findings are reported, not enforced. Every threshold is NEW pending approval. [Server entry](server/RogueTrader.Server/plan.d/c8.md). Counts unchanged.

## Ignore rules for build and local files — November 2, 2026

-   `.gitignore` now also covers test results and coverage (`TestResults/`, `*.trx`, coverage files), BenchmarkDotNet output, NuGet packages, Godot `.mono/`, `.import/`, crash dumps and `export_credentials.cfg`, VS Code/Rider files, more OS junk, logs and temp files. Godot `*.import`/`*.uid` files, `project.godot` and `export_presets.cfg` stay committed. No tracked file is newly ignored.

## Headless server session and tick benchmark — November 2, 2026

-   Queue C6: a new `RogueTrader.Server` library runs the simulation headless in fixed 60 Hz ticks and takes client input only as queued intent for a named tick. Competing inputs for one tick resolve by highest sequence whatever the arrival order; late, stale, duplicate, too-far-ahead, over-capacity and malformed inputs are refused without changing anything. No sockets, snapshots or multiple clients yet; all queue rules are NEW. [Server entry](server/RogueTrader.Server/plan.d/c6.md).
-   Queue C4: `tests/LoadTests` is a Stopwatch tick benchmark that reports the tick-time distribution with host, runtime, data hashes and revision, for the bare simulation and for the C6 session path. It reports only and asserts no threshold. [Load test entry](tests/LoadTests/plan.d/c4.md), [run](docs/development-log/2026-11-02-c4-c6.md). Counts unchanged.

## Target respawn timer runs while docked or paused — November 1, 2026

-   The one-minute target respawn countdown now keeps running while the player is docked, paused or in the inspector (`WorldSimulation.ElapseWithoutTicking`), as Steve asked. Nothing else in the world moves while it is not ticking.

## Mining, refining and pickup rewards — November 1, 2026

-   Queue M7 mining slice: the practice sector now holds the original mineral spawns (11 of the 12 `LIO/Rogue.lio` type 5 mineral rows: Tellerium, Krandazite, Iron Ore), which respawn on their own delays. Every pickup, mineral or not, is saved once through the idempotent GrantReward core with a stable per-sector-instance id, so a failed save or a reload no longer loses or duplicates cargo. A refinery (VEH id 400 recipes, LIO row 13 position) turns ore into refined goods the Trading Outpost buys; profile schema 4 adds the `Refine` kind. Every reading of the undecoded LIO fields, the map scale, one-for-one free refining and placement is NEW and listed in [data/minerals](data/minerals/IMPLEMENTATION_PLAN.md) and [data/stations](data/stations/IMPLEMENTATION_PLAN.md). RT-F042/F044 and master M2 boxes stay open; counts unchanged.

## Practice target respawn — November 1, 2026

-   Once all three practice targets are destroyed, the group respawns after one minute (`targetRespawnSeconds` in the sector file, NEW) and each new wave pays its kill rewards once. A sector reset no longer brings destroyed targets back or re-arms their rewards; relaunching still starts a fresh group. The HUD counts down to the next group. [Client entry](client/UI/plan.d/m9.md), [reward data](data/rewards/IMPLEMENTATION_PLAN.md). Counts unchanged.

## First playable progression loop — November 1, 2026

-   Queues M7 (kill-reward slice) and M9: destroying a practice target pays the raw rogue.cfg `[Cash] KillReward` of 75 credits once, through the idempotent transaction core with a stable per-sector-instance transaction id, and the HUD shows it. Paying it flat per kill, the practice targets and their respawn on reset/relaunch are NEW and listed in [data/rewards](data/rewards/IMPLEMENTATION_PLAN.md). A new [integration suite](tests/Integration/IMPLEMENTATION_PLAN.md) plays the loop (kill, pickup, trade, upgrade, repair), exits and relaunches from the save folder, and checks exact progression, duplicate kills, lost acknowledgements, refused saves, interrupted and corrupt saves. Reloading the profile now reports cargo it discarded because it was never saved (M8 review follow-up). [Client entry](client/UI/plan.d/m9.md). Engineering persistence acceptance only; RT-F051/F068 and master M2 boxes stay open; counts unchanged.

## HUD, Trading Outpost and save/load — November 1, 2026

-   Queue M8: the Godot client opens a local saved profile through the transaction core and shows credits, cargo and upgrades on the HUD. Key F docks at a Trading Outpost derived from the pinned originals (four trade recipes, three repair rows, 35 upgrade and armor rows at original prices), with NEW placement, wallet, capacity, repair prices and upgrade effects listed in the file. Profile schema 3 saves hull, position and heading. Repairs at full hull are refused without charge, and collected cargo survives a failed save. [Client entry](client/UI/plan.d/m8.md), [station data](data/stations/IMPLEMENTATION_PLAN.md), [persistence entry](server/RogueTrader.Persistence/plan.d/m8.md). No native UI parity; RT-F025/F026/F041/F045/F047/F048/F049/F068 and master M2 boxes stay open; counts unchanged.

## Repairs and upgrade pricing — October 27, 2026

-   Queue M5: add `RepairAsync` (priced, station-gated, idempotent wallet debit with `RepairId` recorded; profile schema 2 with a built-in 1-to-2 migration so older saves load and older builds refuse the newer file) and price the original upgrade categories (35 ITM rows) at their raw buy field in `StationPriceTable`. Repair prices are never derived from original data (raw buy 0, amount/unit undecoded). The original data names five upgrade groups, not six. [Persistence entry](server/RogueTrader.Persistence/plan.d/m5.md). RT-F047/F048/F049 and master M2 boxes stay open; counts unchanged.

## G9 initialized reset input observation — October 19, 2026

-   Add one actual selected standalone StartGame.vehicleReset snapshot after accepted catalog/provenance and before arena creation, source-backed expected false, and the same JSON evidence on success/controlled rejection. [Contract/evidence and limits](docs/development-log/2026-10-19-g9-reset-input-observation.md); [live branch log](docs/development-log/cloud/claim-g9-dependency-checkpoint.md). No session/native reset eligibility or original completion.

## G9 conditional reset lifecycle source — October 19, 2026

-   Record guarded gameStart/gameReset → resetVehicles → selected destroy body and contrast its direct calls with kill(Player,int). [Evidence and limits](docs/development-log/2026-10-19-g9-reset-lifecycle-source.md). Markdown only; no runtime/native, universal no-drop or original completion claim. Live workflow status: [PR85](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/85) and its [branch log](docs/development-log/cloud/claim-g9-dependency-checkpoint.md).

## G9 selected-catalog probe strengthening — October 18, 2026

-   Implement complete raw/actual-ID and object-lookup diagnostics in existing ArenaProbe, with selected input/digest/loaded-assembly provenance and literal missing66 check. [Readiness/evidence](docs/development-log/2026-10-18-g9-catalog-membership.md): first-author and cleared round1-rework wrapper/helper runs exit0, Accepted=true,200 exact IDs with consistent object lookups and expected missing66 Warning. No original feature/count or production rule change.

## G9 Uiart/logo documentation precision — October 18, 2026

-   Preserve known exact-name absence while removing unsupported visual/intent/shipping/fallback claims; record bounded pinned Uiart string/archive access and consolidate stable G9 summaries around its ledger. [Evidence and limits](docs/development-log/2026-10-18-g9-uiart-source.md). Markdown only; no accepted checks rerun, original changes or feature/count completion.

## G9 Portal source roles — October 18, 2026

-   Distinguish this pinned loader's Portal iconGraphic pair from its None/None prefire/warp slots and the selected archive-container loading path. [Raw/normalized roles, source evidence and limits](docs/development-log/2026-10-18-g9-portal-source.md). Markdown only; no missing-warp-effect/replacement/native-role acceptance, runtime rerun or feature/count change.

## G9 BallGraphic source applicability — October 18, 2026

-   Document the pinned C# string/archive-access boundary and nearby sprite-reader/version contexts without treating a seven-token shape as CFG grammar evidence. [Source table and limits](docs/development-log/2026-10-18-g9-ballgraphic-source.md). Markdown only; no parser/runtime rerun, original value change or acceptance/count change. G9/RT-F069/RT-F379 remain open.

## G9 bounded maker/death diagnostic — October 18, 2026

-   Trace pinned VehicleMaker83/84/85 → Computer425/426/427 sources and add nine actual-handler diagnostics to existing RuntimeChecks. Isolated packet/action/factory/death/veto controls pass within70 checks, retaining canonical bytes/values and Nanobot2023; no native client, poll or physics ticks. [Evidence and limitations](docs/development-log/2026-10-18-g9-creation-source.md). G9/RT-F069/RT-F386 remain open; no production rule or original completion claim.

## G9 dependency documentation checkpoint — October 18, 2026

-   Reproduce the existing 89-archive / 1,262-member / 609-candidate inventory in memory, retaining eight raw residuals, and reconcile current supplemental byte access with historical package/startup evidence. Record conditional Computer explosion/death source paths and remaining offline/native work in the [G9 ledger](scripts/original-assets/plan.d/g9.md). No executable behavior, canonical content, registry count or acceptance checkbox changes; G9 and RT-F069 remain in progress/open.

## Generic mining extraction — October 18, 2026

-   Queue M3: optional external deposit/equipment definitions, deterministic surface-range extraction, finite asteroid depletion and collectible-to-cargo capacity handling. All extraction tuning and rules are NEW/provisional. Existing clients omit mining definitions and keep their current behavior.
-   Replay format v2 records Mine input and hashes deposits, cooldown, generated-pile sequence and pile geometry; v1 is explicitly rejected. [Local evidence and remaining fidelity limits](docs/development-log/2026-10-18-m3-mining.md). RT-F042 and original Milestone 2 acceptance remain unchecked; registry counts unchanged.

## Documentation and task organization — October 17, 2026

-   Reconcile active source/module, merge-state, compatibility-check and parity-limit descriptions against `d14df02`; distinguish dated runs from current source and full original acceptance. Correct the BallGraphic archive/member interpretation and one vehicle-loader XML field-count comment using preserved source evidence. No executable behavior or canonical content changes.
-   Move superseded root reports/handoffs to `docs/archive/2026-10/`, add a documentation index and give each active queue item a separate source-referenced brief plus a reusable template. Keep original feature/milestone acceptance flags and full scope unchanged.
-   Record audit coverage, local-reference/tracker checks and independent review under `docs/audits/`; no builds, native desktop runs or gameplay simulations for this documentation work.

## Trading station buy/sell - October 16, 2026

-   Add `server/RogueTrader.Trading` (queue item M4): a dock-radius proximity predicate in world units, a `StationPriceTable` price source (explicit tables or a provisional binding of the original ITM buy/sell fields with sentinels kept raw), `CatalogCargoPolicy` (the production `ICargoPolicy` over the simulation `CargoCatalog`) and `TradingService`, which passes the server-side docking verdict to the Persistence transaction core. Selling credits and removes atomically, buying checks wallet and cargo capacity, and replays stay idempotent. A new small project was unavoidable because Persistence and Simulation do not reference each other.
-   28 new unit tests (553 total, 5 pre-existing skips) cover success, insufficient funds, cargo full, out of range, replay, atomicity under injected faults and conservation. Every dock radius, capacity and price is NEW/provisional; the raw ITM sell value -1 is treated as not traded only in this provisional rule. RT-F044, RT-F045 and RT-F049 stay open (original station inventories, units and proximity are unresolved); nothing is wired into the client.

## Generic cargo capacity and collectible pickup - October 16, 2026

-   Add `CargoCatalog`/`CargoHold` to `server/RogueTrader.Simulation`: data-driven (JSON or code) hard weight capacity and per-item unit weights; pickup adds as many units as fit, refuses the rest, never creates or destroys units. `WorldSimulation` collects `CollectibleState` piles along the swept ship path each fixed tick, in catalog order, leaving any remainder in space; an optional initial inventory seeds the hold from a persisted profile.
-   19 new unit tests (525 total) cover capacity edges, partial/refused pickup, per-tick conservation, seeded random invariants, replay and 30 Hz determinism, JSON validation, `ICargoPolicy` equivalence and the seven original Resources masses. All capacities, quantities and radii are NEW/provisional. RT-F030 and RT-F042 stay open: the native threshold/overload formula, pickup range and LIO spawn semantics are unresolved, and nothing is wired into the client or transaction processor yet.

## Original hull raw-data loader - October 15, 2026

-   Add `shared/RogueTrader.OriginalData`: raw CSV record reader (every field by index, source file, 1-based row, version token), SHA-256/length guard for RPG/Rogue.rpg, VEH/Rogue.veh and ITM/Rogue_mp.itm, and a loader that links the twelve RPG hull rows to VEH Car rows by vehicle id. Typed accessors return raw unscaled values with their field index; negative values are flagged, never replaced; all sixteen terrain speed blocks stay available.
-   40 new unit tests (81 total) assert the RT-F001-RT-F012 catalog values; assets are located from the environment, repository or git and tests are reported as skipped if absent. Originals unchanged. No hull checkbox ticked: native selection, loadout and movement remain unverified.

## Persistence and transaction core — October 15, 2026

-   Add `server/RogueTrader.Persistence`: immutable player profile, in-memory and isolated file repositories (atomic temp-and-replace write, previous-generation backup, SHA-256 checksum envelope, quarantine instead of deletion, versioned schema with migration hook, single-owner lock) and an idempotent, per-profile serialized, revision-checked transaction processor (buy, sell, buy upgrade, grant reward) with typed rejection reasons.
-   129 new unit tests cover the master 20.2 economy bullets, concurrent double-spend, crash recovery and corruption handling (210/210 total with the hull loader). Review fixes: a save file holding another (or case-variant) profile id is refused instead of served, Windows device names are invalid profile ids, ids with unpaired surrogates are rejected instead of being altered on save, reward items are snapshotted, and the in-memory repository is thread-safe across profiles. The layer is data-agnostic and not wired into gameplay; no original-parity feature or master M2 box is checked.

## Native runtime safeguards — October 14, 2026

-   Fix derived emulator sales: validate owned quantity and widened cash arithmetic, recheck script mutations, remove inventory before crediting and sync the committed balance.
-   Fix null-script capacity crashes while retaining existing sport queue exceptions.
-   Correct bounded-host radar configuration; reject invalid divisors and logged runtime exceptions. Share fresh configuration generation and compile before execution to prevent stale verification.
-   Verify 35 actual-handler checks against 16 reproduced baseline failures, plus invalid-config rejection and a 10-second full-asset host with zero exceptions. Canonical/upstream files remain unchanged.
-   Catalog purchase overflow/fulfillment/cash-ordering separately as open RT-F396. Native entry, store eligibility, durable progression and full original gameplay remain unverified.

## RT-F397 isolated issue branch — October 14, 2026

-   Prepare a derived-only correction for full int length accounting in both outgoing byte-array writers, preserving serialized bytes and received-packet parsing. Add canonical cache/packet checks and60 whole/indexed boundary/queue fixtures. Root baseline exposed29 canonical length discrepancies,24 synthetic length defects and16 incorrect queue choices; fixed passes all46 canonical packets/60 fixtures with unchanged wire bytes and originals; native receiver delivery is unverified.

## Original package and native-route verification — October 13, 2026

-   Produce standalone OriginalContent Windows package;source/export nativeUI frame/navigation/remap regression andthree renderedpages pass atworldtick0 withoutSDK/runtime discovery.
-   Inventoryall89 archives/1,262members and609candidates;retainmissing/unknownreferences withoutsubstitution. Crosscheck12hulls,200items,30vehicles,81LIO,19,198originalpropertyvalues and222maprefs;preserve fullLVL graphicaltrailer.
-   Initialize actualoriginal genericarena:25computers,100materialdrops,51teams,42hides withzero gameplayticks/clientinputs.
-   FixderivedVehicleMaker originalfield18 omission:exactly8 bountyvalues corrected,allotherexports unchanged;originals intact.
-   RecoverauthenticsharedSoccerBLO andverifyfullasset boundedloopbackhost/controlled120sexit. Compileofflinelegacy patch-endpoint guard;nativehandshake acceptance remainsopen.
-   Prepare/run user-approved isolatednativecopy;welcomeobserved,Start/entrynotverified. Desktopautomation stoppedwhileuserplaying;onlyownedprocesses/testkeys cleanedup.394featurechecks/subplans reconciled.

## Original fidelity checkpoint — October 13, 2026

-   Add verified original BLO/CFS importer/stager:11 archives,231 objects,36,516 frames;14 focused pixel/format/hash checks passed.
-   Add source-client inspector:all12 original hulls/direction/state frames, sprite members and exact map overview;3 rendered pages verify tick0/resources and Debug build has zero warnings/errors.
-   Build/run actual pinned InfantryOnline parser and bounded offline loopback/ping host; original help dependencies recovered with provenance, owned processes/sockets cleaned up.
-   Fix compatibility runner unhandled diagnostic IOException/by-ref JSON serialization; retain separate upstream shutdown race/native-client parity as open.
-   Catalog372 stable original features and six per-record domain plans; reconcile folder docs with two additional agents and save continuation checkpoint at user's usage-window boundary.
-   Updated standalone export, native appearance/gameplay/economy and full dependency closure remain unfinished.

## 0.1.0 — First Contact — October 12, 2026

-   Establish Godot/.NET solution, portable pinned toolchain, local Git, modular folders, conventions, scripts and automated Windows workflow.
-   Implement 60 Hz momentum flight and projectile combat in a Godot-independent C# simulation, driven by validated external JSON.
-   Add playable Godot client with camera, telemetry HUD, asteroids/targets, geometric placeholder graphics, remappable controls and pause/reset.
-   Add automated movement, combat and data validation tests plus source/export runtime acceptance harnesses and rendered QA captures.
-   Document original Rogue Trader evidence, InfantryOnline archive/parser/asset candidates, supplied gameplay reference and historical-vs-provisional differences.
-   Reconcile master scope with the requested original gameplay and assets for Rogue Trader; add folder subplans and a central progress index.
-   Install the requested Watch skill separately from the game project.

Windows prototype acceptance passed: 41 unit tests, native-input/world smoke in source and export, and rendered standalone launch without SDK/runtime discovery. Original asset/content fidelity, Rogue Trader economy/progression and multiplayer remain subsequent work.

Canonical fowlmouth original assets are now acquired as an unchanged reference foundation with 46 hash/byte-length checks. Original reproduction and InfantryOnline engine compatibility remain separate open gates.

-   Add unchanged original-content indexing and canonical hull/resource catalog; validate3,086 raw records and preserve source hashes.
-   Evaluate InfantryOnline/native-client compatibility and record a concrete original-content engine spike before committing to the production engine.
