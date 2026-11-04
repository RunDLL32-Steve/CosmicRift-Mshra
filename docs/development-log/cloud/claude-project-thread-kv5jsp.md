Review status: review-pending

# Owned ships and ship picker (claude/project-thread-kv5jsp)

Scope: Steve's comment on PR #112 (November 3, 2026). A player owns several ships, one of each, saved on their account, and selects one when the client and map load. A destroyed ship respawns with its weapons and every purchased item, and the player loses 20% of their credits.

Files: `server/RogueTrader.Persistence` (schema 6, migration, pre-migration copy, `BuyShip`, `ShipDestroyed`, `SelectShipAsync`), `server/RogueTrader.Trading` (ship prices, session methods, per-ship cargo cap), `server/RogueTrader.Simulation/WorldSimulation.cs` (`DamagePlayer`), `shared/RogueTrader.GameData/ShipRoster.cs`, `data/ships/roster.json`, `client/Scripts/GameClient.cs`, `client/UI/ShipPicker.cs`, `client/Scripts/ClientStationSmokeTest.cs` and tests.

Verified in the cloud: `dotnet test tests/Unit` and `tests/Integration` pass (original-asset tests skip without the pinned assets). Godot smoke: see the PR.

Part 2 (stacked on G10, #114): the 12 original hulls derived by `OriginalShipBuilder` into `data/ships/original_*.json` and `roster.json`, plus 12 hull exchanges on the Trading Outpost (`trading_outpost.json` regenerated) with the original same-race rule. Godot headless smoke passes, including the picker, the destruction cost and the Ship Hulls page.

Part 3 (Steve, Nov 3 2026): a destroyed ship now loses its cargo. `ShipDestroyed` empties the inventory with negative item deltas; the client saves pending pickups before recording the loss, so none can return after the respawn. The ship picked at game entry starts at full hull, which is saved. The station smoke test now runs the destruction check after the trades, with a pickup grant to load the hold.

Part 4 (Steve, Nov 4 2026): "The repair should cost 500 credits." Picking a ship no longer gives a full hull, so a damaged ship stays damaged across a relaunch or ship pick. Full Repair (ITM 43, 50 points at 10 credits each) already costs 500 and fills every original hull; a unit test and the smoke test now pin that. Minor and Major Repair are unchanged.

Part 5 (Steve's PR comment, Nov 4 2026): "Cargo should be dropped when a players ship hits 0 or below 0 health." The hold still leaves the profile through `ShipDestroyed`; the client then spills exactly the removed units as `wreck:<world>:<tick>:<item>` piles at the death point (`WorldSimulation.DropCargo`, 720 s expiry, carried with the sector). Drop expiry now also runs in a world without mineral spawns. Review R3-1: `StationRepair.RestoresFullHull` makes Full Repair fill any hull (the legacy Interceptor has 100). Asked Steve on a card whether "dropped" means spilled into space (done) or simply removed.

Resume point: after #114 merges, merge main. Open questions for Steve: decoding radius, shield and energy, per-race starting weapons, and whether to convert an old Interceptor into an Asp.
