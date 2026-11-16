# claude/project-thread-if91k5: dock and trade-zone flight (G10 F12)

-   Scope: ships fly each hull's original VEH terrain block inside Repair Docks (terrain 9), SpaceDocks (10) and Trading Post / refinery zones (15): top speed, thrust, turn, friction and no boost. Asked by the coordinator for Steve ("continue working on the next three features", November 4, 2026). No RT-F box ticked.
-   Acceptance: every hull file carries terrains 9, 10 and 15 equal to the pinned VEH blocks; the shipped Asp measured in each terrain matches the emulator reading within 2 % (G10 table F12); open space is unchanged; the boost neither engages nor costs energy in those terrains; upgrades scale terrain flight; the ship's own definition is unchanged after a terrain tick.
-   Files: `shared/RogueTrader.GameData/Definitions.cs`, `GameCatalog.cs`, `shared/RogueTrader.OriginalData/EmulatorFlightReading.cs`, `server/RogueTrader.Trading/OriginalShipBuilder.cs`, `OriginalShipPhysics.cs`, `UpgradeEffects.cs`, `server/RogueTrader.Simulation/WorldSimulation.cs`, `data/ships/original_*.json` (regenerated), tests `TerrainFlightTests.cs`, `G10FlightValidationTests.cs`, `Trading/OriginalStationTests.cs`, docs `G10_VALIDATION.md`, `CHANGELOG.md`, plan.d `g10-f12.md`.
-   Verified (cloud, .NET SDK 8.0.133, pinned assets eb1514e without LFS bytes): unit tests 1233 passed, 13 skipped (LFS map/key listing), 1 failed `BloCanonicalTests` (LFS pointer files only, the known cloud failure on main); integration 6 passed; `format-check.py` FORMAT_CLEAN; `check-trackers.py` TRACKERS_CONSISTENT.
-   Not verified: `scripts/verify.ps1` and the Godot client on Windows; play feel in docks (no client change, but the client flies `WorldSimulation`).
-   Review follow-ups: round 1 and 2 approved (40fb814, 56a7a83). The round 2 note (a phantom reverse thrust along the course would pass the relaxed coast check) is closed: `ClientInputSmokeTest` now expects the exact dock friction over the ticks that passed (`TerrainSpeedRetainedPerTick`), so any phantom thrust in any direction fails it.
-   Status: done, in review. Resume point: review findings on the PR.

## November 10 review follow-up

-   Kept the existing F11 radius evidence and removed stale text about the selectable roster. The exact dock-friction coast smoke check remains in place.
-   A terrain-only boost now uses its own reactor recharge policy and drag. The new theory checks a terrain boost with no open-space boost and covers both recharge settings. The three original dock/trade-zone profiles still have no boost.
-   Windows SDK 8.0.419 verify wrapper exited 0. Build had zero warnings and errors. Unit tests passed 1,244 with five archived-key skips, and all six integration cases passed. Godot import, original level art, input, station and flight smoke passed. FORMAT_CLEAN and TRACKERS_CONSISTENT (397/22/375). New head review and merge gates remain open.
