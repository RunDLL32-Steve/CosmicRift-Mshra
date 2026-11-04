Review status: review-pending

# Full-size original map sector (claude/project-thread-h9uu0u)

Scope: Steve's "Continue on the next major tasks" (November 4, 2026); the next major step after G10 slice 2 is F10, flying the sector at the original map size. Stacked on #115 (owned ships), which contains #114 (G10 slice 2).

Files: `server/RogueTrader.Simulation` (`SectorMap`, `MapCollision`, walls in `WorldSimulation`, terrain drop life in `MineralSpawns` and `CargoDrops`, `SectorSnapshot.MapVersion` and the legacy move), `server/RogueTrader.Trading` (`OriginalMapBuilder`; mineral, refinery and station positions at map pixels), `shared/RogueTrader.GameData/Definitions.cs` (`MapFile`, `Evidence`), `data/sectors/rogue.json` and `rogue_map.json` (derived), `data/minerals/mineral_spawns.json` and `data/stations/trading_outpost.json` (regenerated), `client/Scripts/GameClient.cs`, `WorldView.cs`, `client/UI/MinimapView.cs`, the station smoke test, and tests.

Verified in the cloud: `dotnet test tests/Unit` 1131 passed, 5 skipped (keys.txt only; the Rogue.lvl oracle tests ran with the LFS bytes fetched from media.githubusercontent.com), `tests/Integration` 6 passed, Godot 4.5.1 headless smoke (input, station and flight smoke all PASS) and a rendered capture under Xvfb showing the Cold Harbor bay walls and the radar.

Resume point: after #114 and #115 merge, merge main. Open questions for Steve: which team a new pilot joins (spawn), whether to split selling and repairs to the original Trading Posts and Repair Docks, and F12 dock flight.
