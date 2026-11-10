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

## Review round 1 fixes — November 10, 2026

-   Merged the approved #128 (safe zones), #129 (armour) and #133 (shot units) branches into this one, as the review's merge order puts #130 after them, and resolved the conflicts: practice targets and the player are not hit in a safe zone (`IsSafe`), `ProjectileState` takes `DamageType` from the weapon, and the CHANGELOG and derived cannon files combine both sides (cannon files regenerated with `RT_WRITE_DERIVED_SHIPS=1`).
-   R1-1: meteors cannot hit a pilot in a safe zone. New test `Meteors_CannotHitAPilotInASafeZone` (a Repair Dock lets meteors fly through without hitting the docked pilot; the same shower hits a pilot outside).
-   R1-2: the meteor is Kinetic (ITM f[152]); `data/hazards/meteor_showers.json` regenerated, and the "damage type and armour not applied" lines are gone.
-   #133 units: meteors now fly at 425 px/s for 20 s (raw 4250 and 2000 in the emulator's units) instead of the Plasma-scaled 1,352 px/s and 9.1 s; the hit radius keeps its NEW 42.
-   Minor: meteors in flight are saved with the sector (`SectorSnapshot.MeteorsInFlight`) and come back after a relaunch with their position, velocity and age, so a relaunch cannot dodge a hit. A restored meteor faster than a meteor is slowed to meteor speed, and one past its life is dropped. Tests `MeteorsInFlight_SurviveARelaunch` and `RestoredMeteors_CannotBeFasterOrOlderThanAMeteor`.
-   Danger probe again (scratch test, not committed; Asp, rogue.json, rogue_map.json, meteor_showers.json, 40 world salts, 30 s each, 3 showers each): idle at the Cold Harbor spawn 0 of 40 worlds hit; slow circles at (12000, 9000) 6 of 40 hit, 2 destroyed; idle at (12000, 9000) 4 of 40 hit, 4 destroyed. Still with the 5 s shower life that waits on Steve.
-   Checks: unit tests all pass except the environmental `BloCanonicalTests` (LFS bytes missing in cloud); integration 6/6; Godot headless import and `--smoke-test` (input, station, flight) pass.

## Review round 2 follow-up — November 10, 2026

-   R2-1 (minor): meteors in flight now age through time spent away (`ElapseWithoutTicking`: docked, paused or closed), so a meteor saved mid-flight cannot hit hours later on relaunch. Ones whose 20 s life runs out while away are dropped; they do not move while the world is not ticking. Test `MeteorsInFlight_AgeThroughTimeAway` (still there and older after a 1 s pause, gone after 21 s away); 19 cases.
