# Mineral field data implementation plan

Authority: [master §§8 and 18 Milestone 2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), [data plan](../IMPLEMENTATION_PLAN.md), queue [M7 brief](../../docs/tasks/m7.md) and [RT-F042](../../docs/original-research/FEATURE_CATALOG.md#rt-f042). November 1, 2026.

## Purpose

`mineral_spawns.json` is the testbed mineral field: where mineral piles appear in the practice sector, what they hold and when they come back. `RogueTrader.Simulation.MineralFieldCatalog` loads it; the build stages it into `client/GameData/minerals/`. The file is derived by `OriginalMineralFieldBuilder` from the pinned originals; regenerate it with `RT_WRITE_DERIVED_MINERALS=1 dotnet test tests/Unit --filter CommittedMineralField` and never hand-edit it. `MiningRewardTests.CommittedMineralField_MatchesPinnedOriginals` fails if it drifts.

The pinned NWS trade-system text (rows 77–84) says minerals "appear randomly (and have to be found)", are refined at a refinery and traded at a trading post. So mining here means finding and collecting these piles; the queue M3 asteroid-extraction engine stays a NEW test fixture and is not used by the client.

## From the originals

| Data                                        | Source                                                                                                                                                                                 |
| ------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Which rows are mineral spawns, and item ids | `LIO/Rogue.lio` type 5 v23 rows whose f[20] is a Resources item: rows 15, 16, 25, 26, 50, 51, 54 (Tellerium 2003), 17, 53 (Krandazite 2005), 77, 78, 79 (Iron Ore 2019), hash-guarded. |
| Every raw integer field of each row         | Carried in the file under `source.raw` (f4–f22, f24–f27).                                                                                                                              |
| Pickup distance                             | `CFG/rogue.cfg` `[Arena]` `ItemPickupDistance=80` (line 35).                                                                                                                           |
| Detection radius text                       | `NWS/Rogue.nws` row 470: "A mineral spawn anchor has a detection radius to ensure no players are in the area."                                                                         |

## NEW/provisional (listed in the file's `newRules`)

-   Position f[4]/f[5] read as area centre and f[6]/f[7] as area size, drawn into the 1,400-unit practice sector around an assumed 32,000-unit map centre (RT-F021 map size unverified).
-   f[11]/f[12] read as failed-attempt and after-success delays in hundredths of a second (3000 = 30 s).
-   f[14] read as the player clear radius; f[18] (1) as units per pile; f[19] (100 or 50) as most piles alive per spawn. The earlier opposite reading gave 100-unit piles heavier than a whole hold; Steve chose small piles on 2026-11-02.
-   A fresh sector stocks every spawn with its full f[19] piles on the first tick (Steve, 2026-11-02); afterwards one pile per f[12] delay. Random placement avoids the NEW asteroids, up to 8 tries per pile. The client reseeds placement per world from the world id's GUID bytes; smoke runs use numbered world ids, so their field is the same every run.
-   The hold cap is per ship: the starter takes the Asp VEH f[48] StopWeight (2,500,000 raw = 50 Tellerium at ITM weight 50,000). The NormalWeight encumbrance penalty (RT-F030) is not applied.
-   Pickup radius is ItemPickupDistance at the same scale (3.5 units plus the ship radius).
-   Row 54 (f[18]/f[19] = -1/-1) is not spawned; f[13], f[15..17], f[21], f[22] and the 350/15 tail are not applied.

## Open

-   [ ] RT-F042: decode LIO type 5 timing, condition, count and trap fields (queue G6 owns the LIO reader) and validate them natively.
-   [ ] Original map scale and world extent (RT-F021).
-   [ ] Death and spec-mode mineral loss (NWS rows 218, 359, 472).
