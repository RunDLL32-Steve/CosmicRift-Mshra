# Station defense satellites (RT-F278) — November 9, 2026

Branch `claude/project-thread-n1dvi7`, stacked on the meteor shower branch (`claude/project-thread-w580ul`, PR 130), whose turret firing, lead aim and player-hit code it reuses. Steve asked for the next three tasks. This thread first claimed RT-F043, then dropped it because another thread had claimed it two minutes earlier. It took the station defense satellites instead, a server/world feature that does not touch drops, wreckage or the Refinery.

## Sources

-   `LIO/Rogue.lio` rows 65 to 76 (ids 90 to 101, ColdHarbourDefSatA to YankeeDefStatC): type 5 hide rows with f[20] = -410, a 100 x 100 area, InitialCount 1, Attempt/SucceedDelay 6000/6000 tenths of a second, MaxTypeInArea 1, MaxTypeInLevel -1, f[25] AssignFrequency 0 to 3 (three per team).
-   `VEH/Rogue.veh` row 10 (id 410 GunSat): Hitpoints 50, PhysicalRadius 36, TrackingRadius and FireRadius 2500, RepairRate 50, HitpointsRequiredToOperate 5, ObeyLos 1, Destroyable 1, AngleStart/AngleLength 0, TrackingWeight 0 to 10,000,000, ComputerEnergyMax 0, InventoryItems0 1038, ExplodeItemId 3004, DropItemId 2008 x1.
-   `ITM/Rogue_mp.itm` row 98 (id 1038 PirateStationCannon): fireDelay 25, ammoCapacity 5, reloadDelayNormal 150 (10 ms units), muzzleVelocity 4000, aliveTime 550, proximity 4, hull damage 5000 and drain 250000 thousandths.
-   InfantryOnline emulator bb97579: `ArenaLio.hideSpawn`/`hideVehicle` (spawn loop, team from AssignFrequency), `Vehicle.Computer` constructor (fireDelay and reloadDelayNormal x 10 ms, clip from ammoCapacity) and `poll`/`getClosestValidTarget`/`isValidTarget` (repair, operate threshold, no fire at teammates, fire radius, weight and angle limits, line of sight).

## NEW readings

-   Line of sight: walls stand in for LVL vision tiles, because the derived map keeps only blocked tiles.
-   Repair as a steady interval. The emulator ties it to its update-packet timer.
-   No reward, explosion or drop on destruction yet (the drop is left to the RT-F043 work).
-   The shot's speed, life and radius use the Plasma Cannon scale, as the meteors do (G10 W2/W3).
-   Friendly satellites let the player's shots through.

## Checks (cloud, apt SDK 8.0.133 with global.json temporarily pointed at it, not committed; pinned assets eb1514e)

-   `dotnet test tests/Unit`: 1252 passed, 13 skipped, 1 failed. The failure is `BloCanonicalTests.AllCanonicalArchivesMatchIndependentDirectoryOracleAndDirectSourceSlices`, which needs Git LFS BLO bytes that the cloud checkout does not have. It is environmental and also fails on main in cloud. `DefenseSatelliteTests`: 19 passed.
-   Not run: the Godot headless smoke test (running the downloaded editor was not allowed in this session) and `scripts/verify.ps1` on Windows. Both need a local run.
