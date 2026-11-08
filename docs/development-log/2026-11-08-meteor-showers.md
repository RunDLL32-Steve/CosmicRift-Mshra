# Meteor showers (RT-F061) — November 8, 2026

Branch `claude/project-thread-w580ul`. Steve asked for the next roadmap features; this thread took the meteor hazard (armour, safe zones, projectiles, C1 and C7 were taken elsewhere).

## Sources

-   `LIO/Rogue.lio` rows 18, 49, 52 and 57 (MeteorShowerA to D): type 5 hide rows with f[20] = -402, area 15000 x 15000 around (16064, 13872), delays 6000/6000, 6000/12000, 3000/6000, 3000/6000 tenths of a second, MinPlayerDistance 500, MaxTypeInLevel 3, announcement "Meteor Shower Detected!".
-   `VEH/Rogue.veh` row 3 (id 402 MeteorShower): FireRadius 20000, RemoveGlobalTimer 5, RandomizeAim 5, Destroyable 0, InventoryItems0 1007.
-   `ITM/Rogue_mp.itm` row 72 (id 1007 Meteor): fireDelay 35, muzzleVelocity 4250, aliveTime 2000, proximity 56, kinetic 15000, drain 750000.
-   InfantryOnline emulator bb97579: `ArenaLio.hideSpawn`/`hideVehicle` (spawn loop), `Vehicle.Computer.poll`/`getClosestValidTarget` (fire at the nearest pilot within FireRadius every fireDelay), `Helpers.computeLeadFireAngle` (lead aim). The emulator never reads RemoveGlobalTimer or RandomizeAim.

## NEW readings

-   RemoveGlobalTimer 5 read as 5 seconds (Steve's call pending: 5 s, 5 min, or never as the emulator).
-   Meteor speed, life and radius scaled from the raw values by the same factors as the player's Plasma Cannon (projectile units are G10 W2/W3).
-   Meteors hit only the player's ship, not practice targets.

## Checks (cloud, apt SDK 8.0.133 with global.json temporarily pointed at it, not committed; pinned assets eb1514e)

-   `dotnet test tests/Unit`: all pass except `BloCanonicalTests.AllCanonicalArchivesMatchIndependentDirectoryOracleAndDirectSourceSlices`, which needs Git LFS BLO bytes the cloud checkout does not have (environmental, also fails on main in cloud). `MeteorShowerTests`: 15 passed.
-   Godot 4.5.1 mono headless import and `--smoke-test`: CLIENT_INPUT_SMOKE_PASS, CLIENT_STATION_SMOKE_PASS, CLIENT_SMOKE_PASS with showers on the map.
-   Not run: `scripts/verify.ps1` on Windows (needs a local run).
