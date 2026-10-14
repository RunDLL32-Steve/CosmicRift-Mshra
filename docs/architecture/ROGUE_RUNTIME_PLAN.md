# Rogue Trader native runtime integration plan

Master reconciliation: [§1.3 fidelity checkpoint / §18 RT-F382-383](../../ROGUE_TRADER_2_PLAN.md), with open original features RT-F021/F026-F068. Source authority is unchanged canonical fowlmouth content at `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`; emulator source is pinned InfantryOnline server `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849`. This plan targets original native client plus emulator integration; Godot remains a separate inspection testbed.

Current stage: **Native entry In progress; runtime parity open**. RT-F382 source audit, RT-F383 initializer and RT-F392/RT-F394 full-asset bounded hosting are Complete with evidence. The approved native copy reached welcome only; Start gave no confirmed entry. Owned client/backend/private test key cleaned up. No desktop interaction during this documentation task.

## Active implementation queue

- [ ] Not started — native original asset transfer, compatibility/version proof and actual original map render; transmit full original LVL including graphical trailer, never replace it with server parser output.
- [ ] Not started — actual native F11 hull selection, starting weapon, spectator exit and correct native spawn/warp for all 12 hulls.
- [ ] Not started — original mining/drop/pickup and seven resource behavior, computer refining/trading/manufactories and native upgrade installation restrictions.
- [ ] Not started — repair, original bounty/reward formulas and event triggers with sentinel/scaling proof.
- [ ] Not started — durable original inventory/skills/currency/progression; standalone mode deliberately does not persist.
- [ ] Investigating — RT-F388 sell validation/transaction atomicity, RT-F389 server shop range/store eligibility and RT-F390 null-script full-arena join guard; source-derived risks below need isolated regression proof before any fix is checked.
- [ ] Deferred — original native client appearance/movement/combat and authoritative server validation; generic ping/source parser success does not complete these.

- [ ] Investigating - RT-F391 optional administrator prize-item eligibility, separate from canonical gameplay drops/initial spawns; no XML stub.

## RT-F382 completed source audit history

- Inspect actual script binding and original arena initialization route.
- Correct earlier switch-only event audit: all 36 nonempty original CFG [Event] command occurrences have direct-action or external-event-prefix dispatch; no unresolved command in this narrow CFG event block. Event grammar does not by itself prove execution or every VEH/ITM/LIO evaluator.
- Inspect login/first-time setup/hull and unspectate route, shop/computer production/pickup/repair/rewards and standalone persistence boundary.
- Record source hashes and concrete integration/risk queue in [runtime-audit.json](../../.tools/compatibility/reports/runtime-audit.json).

No dedicated `CosmicRift` or `RogueTrader` game script/class was found under the pinned server scripts/active ZoneServer. This is not evidence that original economy must be rebuilt wholesale. `ZoneServer.newArena` reads derived `server.xml` gameType, constructs `ScriptArena` for a registered type, or falls back to `ScriptArena(null)`. Base initialization copies all original LVL tiles, creates configured teams and LIO hide/door/switch/flag state; `ScriptArena.init` calls original initial hides when CFG `InitialHides=0`. Generic handlers already provide events, shops, pickups, repairs, computer production and rewards.

The prior no-client socket probe leaves gameType/scripts empty and creates zero arenas. RT-F383 extends acceptance to the actual original-data arena initializer. It calls `newArena` once after server initialization, never `begin`/`poll`, sends no gameplay inputs and inspects spawned state. Graphics loading remains false for this core-data initializer; it cannot establish native graphical asset delivery.

## Source route and limits

| Area | Actual upstream source | Consequence / acceptance still required |
|---|---|---|
| Binding / arena | `Game/ZoneArenas.cs:147-201`, `Game/Arena/ScriptArena.cs:44-63` | Derived gameType controls script selection, not canonical CFG. Generic null-script path is testable without inventing RT scripts. |
| Original map / objects | `Game/Arena/Arena.cs:495-519`, `ArenaLio.cs:243,521` | Server uses unchanged LVL tile grid and LIO initial hides; full native LVL graphics remain original transfer responsibility. |
| CFG events | `Logic/General/Assets.Events.cs:49-106,120-172,188-490` | Lowercase then comma split; `event...` prefix recursively dispatches named events. `EventJoinTeam`/`eventHold1` are supported; keep originals unchanged. |
| Fresh player | `Logic/Packets/Login/Login.cs:124-130`, `ArenaState.cs:178-217` | Standalone always assigns fresh stats and runs original first-time events after arena setup. Original `FirstTimeSkillSetup=wipeskill,setexp=1`; hull must be chosen with native F11 UI. |
| Ship / spawn | `ScriptArena.cs:1010-1103`, `Game/Objects/Player.cs:1342-1382` | CFG exit logic lists original skills; selected skill sets original VEH; exit event invokes JoinTeam, clears raw cargo/deployable records, sets energy and warps via group0. Actual native login/skill/select/spawn untested. |
| Shops / upgrades | `ScriptArena.cs:1606-1715`, `Logic/Packets/Update/Game.cs:290-312` | Generic inventory/skill buying uses original prices/skillLogic. Does not establish all native upgrade effects or safe server-side trading. |
| Refining / production | `ScriptArena.cs:808-949`, `ArenaComputer.cs:103-339` | Computer product index/range/dead/skill/material/cash checks exist, quantity=-1 consumes all held required material; negative output quantity multiplies consumed input. Actual canonical products/sentinels need runtime proof. |
| Skills / structures | `Logic/General/Assets.Skills.cs:18-199` | Parser implements prefixes %, @, #, negative attribute IDs and booleans; BuildingCheck compares runtime vehicle `_id`, requiring validation against canonical building-type references. |
| Rewards / repairs | `Logic/General/Rewards.cs`, `ScriptArena.cs:2406-2560` | Existing formulas/repair paths need Rogue-specific field/sentinel and behavior acceptance, not replacements with provisional tuning. |
| Native movement | `ScriptArena.cs:1303-1412` | Direct native update copies position/velocity/energy/health. Native engine handles much of original flight/appearance; emulator is not fully authoritative for movement. |
| Persistence | `Logic/Packets/Login/Login.cs:124-130`, `Database/Database.cs:159-178` | Standalone explicitly does not save; connected/local original database route needs separate isolated completion proof. |

## Concrete risks and smallest next changes

1. Preserve the completed initializer/hosting evidence; next executable acceptance is actual native entry/delivery when the user is available. Do not rerun passed initialization for documentation.
2. Preserve original bytes while supplying complete native asset dependencies and compatible client. Native entry requires actual client login/assets/arena selection/hull selection/unspectate evidence.
3. Add a focused shop regression before altering generic shop code: negative sell quantity is compared to positive owned quantity; Cash changes before an ignored inventoryModify result. Oversell can bypass the first gate. Packet/shop path contains no station/range gate. Exact historical eligibility still needs evidence; authoritative validation cannot depend solely on native UI.
4. Guard null script in full-arena join: `_scriptType.Equals` dereferences the fallback null when capacity is reached. Zero-client initializer does not exercise it.
5. Inspect original building logic against `BuildingCheck`'s runtime vehicle `_id`, distinct from `_type.Id`; do not assume raw type IDs already work.
6. Retain completed RT-F385 VehicleMaker regression in history: eight field18 values corrected in derived parser, all other exports unchanged. Original gameplay bounty evaluation remains open.
7. Add persistence after native original behavior works, with explicit isolated backend configuration. Keep external services/account requests absent from bounded diagnostics.

## Verification contract

Root owns builds/runs to avoid concurrent SDK outputs. New `scripts/compatibility/run-arena-probe.ps1 -Build` prepares separate `.tools/compatibility/arena-working/rogue` copies, verifies hashes, runs an owned hidden helper with 30-second timeout and structured exit report. Program catches failures, logs controlled exits and makes no server socket begin or arena poll calls. Expected report: `.tools/compatibility/reports/original-arena-probe.json`, wrapper exit report and `logs/arena-probe.*.log`. RT-F383 actual initialization acceptance passed with controlled exit 0. Full native runtime acceptance stays unchecked even if initializer passes.

## RT-F383 bounded initializer completion history

- Actual original-data arena initialized through generic ScriptArena(null), including original CFG settings and initial LIO hide spawning.
- Exact 1999 x 1998 original map, 3,994,002 tiles / 116,065 blocked-mask tiles; 200 items, 30 vehicles and 12 RPG hull-to-VEH identities checked.
- Original initial entities inspected: 25 computer vehicles (2 Refineries, 2 Trading Outposts, 5 Repair Docks, 12 GunSats, 3 MeteorShowers and 1 Solar Mold), 51 named teams including spectator and 42 hide records; zero doors/flags.
- Original initial 100 material drops inspected (45 Iron Ore, 45 Tellerium, 10 Krandazite); all 12 copied canonical data/help hashes unchanged before/after.
- Controlled exit 0 with one arena, zero clients/player inputs and zero socket begin, poll or physics-tick calls; bounded owned helper stopped.

Evidence: [actual initializer report](../../.tools/compatibility/reports/original-arena-probe.json), [exit result](../../.tools/compatibility/reports/original-arena-probe-result.json), [stdout](../../.tools/compatibility/logs/arena-probe.stdout.log), [build log](../../.tools/compatibility/logs/arena-probe-build.log). The first build had zero errors and inherited upstream nullable warnings; no native original movement/combat/graphics/persistence acceptance is inferred from this completed initializer.

### Optional PrizeItems.xml source role - RT-F391 unchecked

The successful initializer logs a missing `Assets/PrizeItems.xml`, then continues. `Arena.initLists` (`Game/Arena/Arena.cs:1437-1457`) loads optional `<Item ID="..."/>` entries only if present; absence sets `Prizelist=false`. The inspected use is `Game/Commands/Mod/Basic.cs:1252-1271`: Level1 granted-player admin prize requests for buyPrice=0 items require a permitted ID/list and fail if no list. It does not block original LIO initial mineral/station spawning or define canonical mining drops. No canonical permission list has been established; do not create an empty/reconstructed XML merely to suppress the diagnostic. Actual administrator eligibility remains unchecked.

Build note: first acceptance build reported 1,488 warnings / zero errors, including upstream nullable warnings and one probe dictionary-key nullability warning. The probe-only warning selector was corrected statically to `path => Path.GetFileName(path)!`; no extra initializer or gameplay run was triggered for this annotation-only change. Root may compile it with the next coordinated compatibility build.

## Bounded native SessionHost - RT-F392

Current stage: **Complete for bounded hosting; native entry remains unverified.** Root compiled and ran the completed helper after authentic shared Soccer recovery. The approved native copy reached welcome only; desktop automation was stopped while the user was playing another game.

- [ ] In progress — RT-F380/RT-F384 native welcome/profile/entry; RT-F387 actual asset delivery remains unverified. F11 ship selection/unspectate follows confirmed entry.

Prepare/build without a server run: `./scripts/compatibility/run-native-session-host.ps1 -Build`. Explicit later backend start: `./scripts/compatibility/run-native-session-host.ps1 -Start -DurationSeconds 120`. The launch wrapper returns an owned PID and readiness report once both sockets bind; it launches no native UI. Write `.tools/compatibility/local-working/session-host/stop.request` to request early stop; maximum is 120 seconds after readiness. Reports are `reports/ready.json` and `reports/exit.json`, with local logs and byte hashes beside them. Program never generates synthetic player movement/combat inputs. Aggregate reports avoid aliases/credentials/chat/UID. Use an ephemeral test alias if a native client subsequently connects; no real account/personal credentials.

Routing detail: actual `allocatePlayer` uses `_namedArenas` loaded from canonical named arena rows and otherwise searches names starting `Arena`. To route a default login to requested `Rogue Trader` while keeping original CFG bytes/rows unchanged, the helper supplies separate NEW runtime `NamedArena` metadata to `_namedArenas`, creates that actual public named arena and marks it active. Original balance/skills/teams/data are retained. This derived local diagnostic name is not claimed as an original arena-rule change.

Host end deliberately avoids upstream `ZoneServer.end` pending-receive rearm race, sets `run=false`, stops owned ping responder, joins host thread and returns normally so OS releases remaining socket handles. Full server production shutdown, persistence and known economy/capacity bugs remain open.
## Hosting completion history and desktop stop

RT-F392 passed actual root execution: full assets loaded, one public Rogue Trader arena, game127.0.0.1:21337 and ping127.0.0.1:21338 ready. Canonical89 BLOs plus separately sourced authentic soccer.blo and12 original core/help records remain byte-identical. The bounded host exited0 after120.17seconds, maximumclientcount0, no synthetic inputs, all original hashes unchanged. Evidence: .tools/compatibility/local-working/session-host/reports/ready.json and exit.json.

NativePID4088 reached welcome; no arena entry verified. PhysicalEscape stopped Computer Use while another game was active. Owned client/backend were stopped, unique test registry namespace removed, existing Infantry profiles untouched. See .tools/native-client-inspection/native-test-cleanup.json. The broader native connection/gameplay/proximity/persistence queues remain unchecked.

RT-F393 offline patch-announcement guard is implemented/compiled and included in source-hash preflight; actual native CS_Ready acceptance remains Testing. RT-F394 authentic shared Soccer dependency is Complete for extraction/required-member/startup acceptance. Neither modifies canonical Rogue CFG/map/ships/items.


