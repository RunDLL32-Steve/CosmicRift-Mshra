# Plasma Mines (RT-F038 / RT-F088 slice) — November 16, 2026

The values come from pinned `ITM/Rogue_mp.itm` row 13 (cosmic-rift-assets `eb1514ea`). Field names follow the InfantryOnline `ItmInfo.Projectile.cs` (`bb97579e`) for the same indexes: f[31] useAmmoID 102 (the mine itself), f[32] 1 per drop, f[54] fireDelay 150, f[94] inheritedSpeed 0, f[101] muzzleVelocity 0, f[108] proximityRadius 56, f[110] aliveTime 6000, f[115] inactiveTime 0, f[131] maxLivePerPlayer 3, f[132] maxLivePerTeam 15, f[159..161] electronic radius/inner/outer 100/20000/5000, f[171..173] energy radius/inner/outer 100/750000/0, f[8] buy 25, f[15] sell 25, f[13] maxAllowed -10. The derived values are in `data/stations/trading_outpost.json` `munitions`, with one NEW note per reading.

Checks run in a cloud container (.NET SDK 8.0.134, with global.json changed only locally; Godot 4.5.1 mono headless):

-   `dotnet test tests/Unit` after merging main (`22f0cd4`, with safe zones #128, armour #129, cannon units #133 and wreckage #134): 1,307 passed, 5 skipped. The pinned assets are present, so the original-data case ran.
-   Godot import and `--smoke-test`: all flight, input and station smoke steps pass, including the new mine step.
-   `python scripts/check-trackers.py`: TRACKERS_CONSISTENT, 397/22/375 unchanged.

Feature boxes RT-F038 and RT-F088 stay open. See the [plan item](../../server/RogueTrader.Simulation/plan.d/rt-f038.md).
