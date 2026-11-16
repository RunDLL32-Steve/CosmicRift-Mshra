# C7 follow-up: boost and wall prediction — November 9, 2026

Steve's dashboard listed "client prediction does not yet predict the Shift boost or walls" as next. C7 itself merged in PR 117; no open claim covers this follow-up (TASK_QUEUE has no active rows; open PR 124 strafing touches `ShipMotion`/`ShipInput` in a nearby place and may need a small merge).

Changes: `InputFlags.Hyper`; `EntityState.Energy` and protocol version 2; `ShipMotion` with energy and an optional `SectorMap`, shared with `WorldSimulation`; `ShipPredictor` map parameter and Hyper prediction; session audit allows the boost top speed.

Checks (cloud, .NET SDK 8.0.133, pinned assets at eb1514e without LFS bytes): `dotnet test tests/Unit` 1,226 passed, 13 skipped, 1 failed (`BloCanonicalTests`, BLO files are LFS pointers in this container; unrelated). Godot 4.5.1 mono import plus `--smoke-test` passed (CLIENT_SMOKE_PASS and CLIENT_STATION_SMOKE_PASS). `format-check.py` FORMAT_CLEAN, `check-trackers.py` TRACKERS_CONSISTENT. The `WorldSimulation` refactor keeps the exact operation order (move, pickups, mining, recharge, fire), and the existing hull boost, sector map and replay tests pass unchanged.

## November 15 dependency review follow-up

-   R1-2 (#127): authority and prediction select dock or trade-zone flight at tick start. The shared step uses that terrain's thrust, turn, boost and friction before clamping speed. The motion audit checks the same terrain, including its own boost speed when engaged. Asteroids use the terrain restitution and walls use the hull restitution.
-   R1-3 (#128): authority and prediction apply the energy rate from the post-movement position. The boost that engaged determines whether the reactor recharges. The three dock/trade-zone crossing tests verify wire inputs, equal motion and energy at each tick, depletion and boost recovery. The audit regression also covers a boost available only in the starting terrain.
-   Windows .NET SDK 8.0.419 passed all 77 focused unit cases covering prediction, terrain flight, safe zones, the authoritative session and the protocol. Tracker registry 397/22/375 is consistent. Independent review and Windows CI are pending.
