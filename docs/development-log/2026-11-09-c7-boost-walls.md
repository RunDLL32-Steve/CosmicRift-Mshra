# C7 follow-up: boost and wall prediction — November 9, 2026

Steve's dashboard listed "client prediction does not yet predict the Shift boost or walls" as next. C7 itself merged in PR 117; no open claim covers this follow-up (TASK_QUEUE has no active rows; open PR 124 strafing touches `ShipMotion`/`ShipInput` in a nearby place and may need a small merge).

Changes: `InputFlags.Hyper`; `EntityState.Energy` and protocol version 2; `ShipMotion` with energy and an optional `SectorMap`, shared with `WorldSimulation`; `ShipPredictor` map parameter and Hyper prediction; session audit allows the boost top speed.

Checks (cloud, .NET SDK 8.0.133, pinned assets at eb1514e without LFS bytes): `dotnet test tests/Unit` 1,226 passed, 13 skipped, 1 failed (`BloCanonicalTests`, BLO files are LFS pointers in this container; unrelated). Godot 4.5.1 mono import plus `--smoke-test` passed (CLIENT_SMOKE_PASS and CLIENT_STATION_SMOKE_PASS). `format-check.py` FORMAT_CLEAN, `check-trackers.py` TRACKERS_CONSISTENT. The `WorldSimulation` refactor keeps the exact operation order (move, pickups, mining, recharge, fire), and the existing hull boost, sector map and replay tests pass unchanged.
