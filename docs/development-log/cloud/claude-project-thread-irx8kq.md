Review status: review-pending

# Zoomable sector map on M (claude/project-thread-irx8kq)

Scope: Steve's "Pushing 'M' should show the user the map, and where they are on it live as they are moving. It should be zoomable as well in and out" (November 4, 2026). Stacked on PR #118 (claude/project-thread-h9uu0u), which is not merged yet.

Files: `client/UI/SectorMapOverlay.cs` (new), `client/Scripts/GameClient.cs` (wiring, keys, `--capture-map` / `--capture-map-zoom=N`), `client/Scripts/FlightInput.cs` (`sector_map` binding, default M), `client/UI/FlightHud.cs` (controls line, slimmer binding rows so the pause menu still fits 720 px), `client/Scripts/ClientInputSmokeTest.cs`, `client/UI/plan.d/sector-map.md`, `CHANGELOG.md`.

Verified in the cloud: client build clean, Godot 4.5.1 headless smoke all PASS (input smoke includes the new sector map pass, station and flight smoke unchanged), rendered captures under Xvfb of the whole map, a 6x zoom at Cold Harbor and the pause menu with the new binding row. Unit tests were not run: nothing under `server/`, `shared/` or `tests/Unit` changed.

Resume point: independent review, then Steve merges after #118.
