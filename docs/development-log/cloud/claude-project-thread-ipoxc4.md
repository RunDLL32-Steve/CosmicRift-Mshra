# Branch log: claude/project-thread-ipoxc4 (queue G10, cloud slice)

Scope: queue [G10](../../tasks/g10.md), first bounded slice: an expected/observed table for flight, weapon, controls and HUD ([G10_VALIDATION.md](../../original-research/G10_VALIDATION.md)), with `EmulatorFlightReading` in `shared/RogueTrader.OriginalData` and `tests/Unit/G10FlightValidationTests.cs`. No gameplay change; no RT-F box ticked.

Selection (November 3, 2026): Steve asked to resolve the conflicts on PR #111 (G6) and start the next major task. #111's only conflict was CHANGELOG.md (both entries kept, the G6 thread pushed the same resolution as a1a2545). G10 depends on G6, so this branch is based on the #111 branch. G10 is listed as a local item; this slice does only what the cloud can do and lists the native questions for Steve. No overlap with C7 (Codex) or C1.

## Round 1 — November 3, 2026

-   Expected column: pinned VEH/ITM values through the emulator's readings (IOS bb97579 `ZoneServer/Game/Arena/Bot/MovementController.cs`, `Protocol/Helpers/Math.cs:207–233`, `Game/Objects/Vehicle.Computer.cs:75–84`; the `Game/Arena/ScriptArena.cs:1248–1254` fire-timing check is commented out there and is not used as evidence), re-read in a scratch clone. RECONSTRUCTED, not native.
-   Observed column: measured by running `WorldSimulation` with the shipped ship and weapon in an empty sector (top speed, acceleration both ways, time to top speed, turn rate, coasting, fire interval).
-   Result: 2 MATCH (reverse equals forward; no drag in open space), 7 MISMATCH (top speed, acceleration, time to top speed, turn rate, bounce, map crossing time, fire interval), the rest MISSING or OPEN. The largest gap is scale: the practice sector draws the original map at 1,400/32,000 while the ship flies at near-pixel speed.

Verified in the cloud container (SDK 8.0.133 substituted for the pinned 8.0.419; global.json change not committed): `dotnet test tests/Unit` with pinned assets and the LVL LFS content: 1060 passed (10 new), 0 failed, 5 existing skips. `format-check.py` FORMAT_CLEAN, `check-trackers.py` TRACKERS_CONSISTENT. Skipped: `verify.ps1` and the Godot smoke, because nothing in the client or the simulation changed.

Not verified / still open: every native unit (needs the original client, G1); default key bindings; HUD layout; projectile speed and lifetime units.

## Round 2 — November 3, 2026

-   Review R1-1: the `ScriptArena.cs` fire-timing check is inside a block comment at bb97579, so W1 now rests on the bot path alone and its confidence is Low. The verdict is still MISMATCH (0.20 s vs 0.25 s).

## Second slice — November 3, 2026 (after #113 merged)

-   Steve: "Continue and implement the original models" (answering the flight-switch decision with "now"). Branch restarted from main 4f5ff70.
-   `OriginalShipPhysics` (server/RogueTrader.Trading) maps `EmulatorFlightReading` to `ShipPhysics`; the shipped ship JSON now carries the Asp's values and the Plasma Cannon fixture its 0.25 s interval. A test pins both JSON files to the derived originals.
-   The Godot client smoke's scripted flight path hit a NEW practice asteroid with the stronger thrust; phase 1 now thrusts to a fixed 180 units/s cruising speed (or 90 ticks) instead of a fixed 90 ticks, so the path no longer depends on acceleration.
-   Not changed: sector scale (needs the original spawn point, the G6 walls and a radar), strafe and hyper (key bindings unknown), W2–W5 (units unknown), code defaults in `ShipDefinition`/`WeaponDefinition` (test fixtures, still NEW). Coordinated with the owned-ships thread (claude/project-thread-kv5jsp), which will call `OriginalShipPhysics.From` for its hulls and does not touch these files.
-   Verified: `dotnet test tests/Unit` 1061 passed, 5 skipped; `tests/Integration` 6 passed; Godot 4.5.1 headless import and `--smoke-test` exit 0 (client, input and station smokes pass); FORMAT_CLEAN; TRACKERS_CONSISTENT. `verify.ps1` itself needs Windows.
