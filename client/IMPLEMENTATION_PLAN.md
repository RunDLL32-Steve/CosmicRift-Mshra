# Client implementation plan

This subplan reconciles with the master [ROGUE_TRADER_2_PLAN.md](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), especially sections 5.3-5.4, 13.3, 15-16, and First Contact slices A-E in section 19. The master is the status authority. M0/M1 engineering is complete; current checkpoint is the fidelity gate.

The current vector visuals, training sector, ship, and weapon are temporary **NEW/provisional** test fixtures. They demonstrate the engine and flight/combat architecture; they do not establish original Cosmic Rift visual or gameplay fidelity. Archive/asset and historical behavior research is a separate gate recorded by the master and research documentation.

## Scope and completion

- Completed: Godot 4.5.1 C# / .NET 8 client project and launch scene. Scoped build succeeds with zero warnings/errors.
- Completed: Load the authoritative catalog from shared JSON, staged from root `data/` into `client/GameData/` at build time and exported as resources.
- Completed: Thin bridge calls the shared C# world at 60 fixed physics ticks per second, independently from rendering.
- Completed: Forward/reverse thrust, rotation, momentum and firing captured with Godot InputMap.
- Completed: Keyboard remapping UI with persisted bindings in `user://controls.cfg`, conflict feedback, and restore defaults implemented and verified with native input plus UI callbacks.
- Completed: Original placeholder renderer for player/targets, asteroids, stars, plasma, thrust exhaust, shield/impact flashes, and destruction bursts implemented. Visual QA remains listed below.
- Completed: Camera follows simulation position; hull/shield/energy/speed/tick/FPS/projectile/sector/target telemetry implemented.
- Completed: Pause, resume, reset sector, quit, target completion and player destruction/reset messages implemented.
- Completed: Runtime smoke mode drives target destruction, forward thrust, drift, reverse, independent rotation and actual asteroid collision. All six phases pass in a headless client run with exit 0.
- Completed: Screenshot mode saves after 30 rendered frames and exits automatically; optional controls-menu capture supported.
- Completed: Windows x64 export preset with managed export templates and included JSON resources prepared.
- Completed: Root integrated solution verification and exported Windows executable smoke validation (authoritative completion evidence recorded by root).
- Completed: Rendered gameplay and controls-menu screenshot QA.
- Completed: Runtime controls-remapping persistence and pause/reset UI interaction checks: three native-input regression phases pass, including held-key remapping and default restoration.

## Module ownership

| Module | Responsibility |
|---|---|
| `Scripts/GameClient.cs` | Engine/simulation bridge, fixed-step tick, camera, lifecycle and QA modes |
| `Scripts/FlightInput.cs` | Default actions, input capture, remapping validation and persisted settings |
| `Scripts/WorldView.cs` | Rendering and presentation-only effects; no gameplay authority |
| `UI/FlightHud.cs` | Flight telemetry, objectives, pause and controls UI |
| `Scripts/ClientSmokeTest.cs` | Acceptance sequence against the actual shared world |
| `Scripts/ClientInputSmokeTest.cs` | Native keyboard, control-widget, persisted-binding and lifecycle regressions |
| `Scenes/Main.tscn` | Startup scene |
| `export_presets.cfg` | Windows x64 release packaging |

## Controls

W / Up = forward; S / Down = reverse; A / Left = turn left; D / Right = turn right; Space = plasma cannon. Escape opens pause/controls; R resets the whole training sector. Every implemented binding can be changed in the controls menu. A remapped movement action uses the chosen single key; defaults restore letter/arrow alternatives. Reset reloads the catalog into a new world; it is not a persistence or progression feature.

## Validation commands and evidence

From the repository root, use the pinned environment established by `scripts/environment.ps1`:

```powershell
./scripts/verify.ps1
./scripts/run-client.ps1
./scripts/export-windows.ps1
```

For direct QA, invoke Godot with `--path client -- --smoke-test` or `--path client -- --capture <absolute-png-path>`. Add `--capture-controls` to the capture invocation to show the pause/controls menu. Screenshot mode requires a rendered display; headless mode deliberately returns failure for screenshots. The smoke harness first drives native `Input.ParseInputEvent` sequences and the actual remap/default button callbacks through normal physics updates, then runs eight otherwise identical fixed world ticks per engine callback for bounded wall time. Existing user controls are preserved and restored after the input fixture.

Client-scoped evidence: `dotnet build client/RogueTrader.Client.csproj --configuration Debug --no-restore` completed with zero warnings/errors, Godot resource import completed, and headless runtime printed `CLIENT_READY` followed by all six `SMOKE PASS` lines and `CLIENT_SMOKE_PASS`, exit 0. Input regression evidence: three additional phases printed `CLIENT_INPUT_SMOKE_PASS`, covering held W while paused then rebind to Q and release W, persisted reload, resume/coast/new Q thrust, held Q while restoring saved W/Up defaults, and R reset from pause. A mutation run that omitted `Input.ActionRelease(action)` failed at the exact held-W/remap assertion with exit 1; the restored fix passes with exit 0. Root reconciled exported-client and rendered QA evidence into this subplan, the master M0/M1 checklists and the central progress log.

M2 mining, economy, upgrades, persistence, NPCs and M3 networking are deferred. No client-side authority for economy or multiplayer has been introduced.


Final reconciliation: master M0/M1 engineering checklists are checked. Full solution and 41 unit tests pass, source and exported clients pass native-input/UI and world smoke, and actual rendered gameplay/controls/export captures were inspected. See docs/development-log/PROGRESS.md for evidence and original reproduction/engine gates.



## Current source inspector checkpoint — October 13

Complete scoped RT-F020/F073:read-only canonical-derived library and paused inspector,12 hull identities/64 rows/11 columns,231 sprites and1999×1998 overview. Debug build zero warnings/errors;3 actual rendered pages at world tick0, exit0, resource validation passed and screenshots inspected. Logs/captures:artifacts/qa/original-{ships,map,sprites}.log/.png. Godot root-certificate-store warning does not prevent successful local import/render.

- [ ] In progress — RT-F373 updated Windows export/standalone viewer acceptance; existing FirstContact artifact is prior engineering build.
- [ ] In progress — RT-F374 wider native navigation/input/pause/resume inspector regression.
- [ ] Investigating — native bank/palette/HSV/shadow/blit/timing/audio/HUD.
- [ ] Not started — original LVL/LIO playable world and original hull/weapon/economy behavior.

Raw Asp/Hornet pixels render where staged, but training physics/effects remain provisional. Column5 preview selection is unvalidated native banking. Completed prototype tasks above are history; exact runtime checks remain open. User has resumed; current slice is packaging/input validation, with exact runtime parity still open.
