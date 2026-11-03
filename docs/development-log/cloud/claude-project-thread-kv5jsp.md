Review status: review-pending

# Owned ships and ship picker (claude/project-thread-kv5jsp)

Scope: Steve's comment on PR #112 (November 3, 2026). A player owns several ships, one of each, saved on their account, and selects one when the client and map load. A destroyed ship respawns with its weapons and every purchased item, and the player loses 20% of their credits.

Files: `server/RogueTrader.Persistence` (schema 6, migration, pre-migration copy, `BuyShip`, `ShipDestroyed`, `SelectShipAsync`), `server/RogueTrader.Trading` (ship prices, session methods, per-ship cargo cap), `server/RogueTrader.Simulation/WorldSimulation.cs` (`DamagePlayer`), `shared/RogueTrader.GameData/ShipRoster.cs`, `data/ships/roster.json`, `client/Scripts/GameClient.cs`, `client/UI/ShipPicker.cs`, `client/Scripts/ClientStationSmokeTest.cs` and tests.

Verified in the cloud: `dotnet test tests/Unit` and `tests/Integration` pass (original-asset tests skip without the pinned assets). Godot smoke: see the PR.

Resume point: add the 12 original hulls (ITM rows 181-192) to the roster with `OriginalShipPhysics.From(hull)` once G10 merges.
