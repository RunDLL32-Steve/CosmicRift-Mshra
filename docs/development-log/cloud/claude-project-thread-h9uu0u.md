Review status: review-pending

# Full-size original map sector (claude/project-thread-h9uu0u)

Scope: Steve's "Continue on the next major tasks" (November 4, 2026); the next major step after G10 slice 2 is F10, flying the sector at the original map size. Based on main after #114, #115, #116 (C1) and #117 (C7) merged; main was merged into the branch.

Files: `server/RogueTrader.Simulation` (`SectorMap`, `MapCollision`, walls in `WorldSimulation`, terrain drop life in `MineralSpawns` and `CargoDrops`, `SectorSnapshot.MapVersion` and the legacy move), `server/RogueTrader.Trading` (`OriginalMapBuilder`; mineral, refinery and station positions at map pixels), `shared/RogueTrader.GameData/Definitions.cs` (`MapFile`, `Evidence`), `data/sectors/rogue.json` and `rogue_map.json` (derived), `data/minerals/mineral_spawns.json` and `data/stations/trading_outpost.json` (regenerated), `client/Scripts/GameClient.cs`, `WorldView.cs`, `client/UI/MinimapView.cs`, the station smoke test, and tests.

Verified in the cloud: `dotnet test tests/Unit` 1131 passed, 5 skipped (keys.txt only; the Rogue.lvl oracle tests ran with the LFS bytes fetched from media.githubusercontent.com), `tests/Integration` 6 passed, Godot 4.5.1 headless smoke (input, station and flight smoke all PASS) and a rendered capture under Xvfb showing the Cold Harbor bay walls and the radar.

Follow-up on Steve's answers (November 4, 2026): teams (first pilot Cold Harbor, then the emptiest team), selling at the Trading Posts, repairs at the Repair Docks, and a Hangar page at each home store to change ships. Files: `TeamRoster.cs`, `OriginalMapBuilder.cs` (teams), `OriginalStationBuilder.cs` (outposts), `StationDefinition.cs` (`ServiceSite`, per-service prices), `StationPriceTable.cs` (`at` station ids), `StationProfileSession.cs` (`DockedAt`, `SwitchShipAsync`), `SectorMap.NearestOpen`, `Definitions.cs` (`TeamDefinition`), the client's station screen, world view, radar and smoke test.

Verified in the cloud after the follow-up: `dotnet test tests/Unit` 1200 passed, 5 skipped, 1 failed (main's C1 `BloCanonicalTests`, because this cloud copy of the assets holds LFS pointer files for BLO), `tests/Integration` 6 passed, Godot 4.5.1 headless smoke all PASS.

Resume point: Steve reviews #118. Open: wall collision in client prediction (C7 area), F12 dock flight, the second refinery, and whether a home store serves only its own team.
