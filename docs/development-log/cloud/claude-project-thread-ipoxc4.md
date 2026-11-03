# Branch log: claude/project-thread-ipoxc4 (queue G10, cloud slice)

Scope: queue [G10](../../tasks/g10.md), first bounded slice: an expected/observed table for flight, weapon, controls and HUD ([G10_VALIDATION.md](../../original-research/G10_VALIDATION.md)), with `EmulatorFlightReading` in `shared/RogueTrader.OriginalData` and `tests/Unit/G10FlightValidationTests.cs`. No gameplay change; no RT-F box ticked.

Selection (November 3, 2026): Steve asked to resolve the conflicts on PR #111 (G6) and start the next major task. #111's only conflict was CHANGELOG.md (both entries kept, the G6 thread pushed the same resolution as a1a2545). G10 depends on G6, so this branch is based on the #111 branch. G10 is listed as a local item; this slice does only what the cloud can do and lists the native questions for Steve. No overlap with C7 (Codex) or C1.

## Round 1 — November 3, 2026

-   Expected column: pinned VEH/ITM values through the emulator's readings (IOS bb97579 `ZoneServer/Game/Arena/Bot/MovementController.cs`, `Protocol/Helpers/Math.cs:207–233`, `Game/Objects/Vehicle.Computer.cs:75–84`, `Game/Arena/ScriptArena.cs:1250–1254`), re-read in a scratch clone. RECONSTRUCTED, not native.
-   Observed column: measured by running `WorldSimulation` with the shipped ship and weapon in an empty sector (top speed, acceleration both ways, time to top speed, turn rate, coasting, fire interval).
-   Result: 2 MATCH (reverse equals forward; no drag in open space), 7 MISMATCH (top speed, acceleration, time to top speed, turn rate, bounce, map crossing time, fire interval), the rest MISSING or OPEN. The largest gap is scale: the practice sector draws the original map at 1,400/32,000 while the ship flies at near-pixel speed.

Verified in the cloud container (SDK 8.0.133 substituted for the pinned 8.0.419; global.json change not committed): `dotnet test tests/Unit` with pinned assets and the LVL LFS content: 1060 passed (10 new), 0 failed, 5 existing skips. `format-check.py` FORMAT_CLEAN, `check-trackers.py` TRACKERS_CONSISTENT. Skipped: `verify.ps1` and the Godot smoke, because nothing in the client or the simulation changed.

Not verified / still open: every native unit (needs the original client, G1); default key bindings; HUD layout; projectile speed and lifetime units.
