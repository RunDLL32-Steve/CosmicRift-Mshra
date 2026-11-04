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

## Field meanings (InfantryOnline emulator, RECONSTRUCTED)

Steve asked on 2026-11-03 for the field to follow the emulator's reading (`LioInfo.Hide.cs`, `ArenaLio.hideSpawn`/`hideItem` at bb97579; see [ZONE_ASSETS_COMPARISON.md](../../docs/original-research/ZONE_ASSETS_COMPARISON.md)). The emulator is a reconstruction of the server, not the native one.

| Field                      | Meaning                                          | Rogue.lio values                                                   |
| -------------------------- | ------------------------------------------------ | ------------------------------------------------------------------ |
| f[10]                      | Events run on the first attempt                  | 1                                                                  |
| f[11]                      | Delay after a failed attempt, tenths of a second | 3000 (300 s)                                                       |
| f[12]                      | Delay after a success, tenths of a second        | 6000 (600 s); the next try uses min(f11, f12)                      |
| f[13]                      | Probability (0 = only the first attempt)         | 1                                                                  |
| f[14]/f[15]                | Min/max players in the sector                    | 0, 40, 60 or 80 / 500                                              |
| f[16]/f[17]                | Min/max player distance (99999 = none)           | 0 / 99999                                                          |
| f[18]                      | Most units of the item in the area               | 1, so an area refills only when empty                              |
| f[19]                      | Most units of the item in the whole sector       | 100 Tellerium, 100 Iron Ore, 50 Krandazite; -1 on Tellerium SpawnG |
| f[21]                      | Units per drop                                   | 1                                                                  |
| f[26]/f[27]                | Clump box and drops per event                    | 350 / 15 (10 Krandazite)                                           |
| CFG [Terrain0] PrizeExpire | Seconds before a drop vanishes                   | 720                                                                |

With one player, only the spawns with f[14]=0 run (Tellerium A-C, Krandazite A, Iron Ore A-C), so a fresh sector holds about 45 Tellerium, 10 Krandazite and 45 Iron Ore.

## NEW/provisional (listed in the file's `newRules`)

-   Position f[4]/f[5] read as area centre and f[6]/f[7] as area size in original map pixels, moved by the LVL offset (48 pixels) as the emulator moves LIO positions; f[16], f[17] and f[26] are pixels too. Since November 4, 2026 the game flies the full-size Rogue map, so world units are map pixels (before, the field was drawn into a 1,400-unit practice sector).
-   The practice sector has no terrain map, so every drop takes the open-space expiry (720 s); the emulator uses 120 s in docks and trade zones.
-   A drop that would overlap a NEW practice-sector asteroid uses one of the emulator's 10 tries per event (5 back per drop placed). Iron Ore SpawnA's area sits on asteroid_12, so it usually drops little or nothing.
-   A destroyed ship spills its cargo as one `wreck:` pile per item, 12 units from where it died, radius 3.5, with the same 720 s open-space expiry (Steve, November 4, 2026). The placement, one pile per item and the radius are NEW.
-   The players-in-sector count is 1 in the local build; a dedicated server will supply the real count.
-   Drop expiry and spawn timers run in real time while docked, paused or closed, and are saved with the sector, so a relaunch neither refills nor clears the field early (Steve, 2026-11-02).
-   The hold cap is per ship: the starter takes the Asp VEH f[48] StopWeight (2,500,000 raw = 50 Tellerium at ITM weight 50,000). The NormalWeight encumbrance penalty (RT-F030) is not applied.
-   Pickup radius is ItemPickupDistance, 80 pixels plus the ship radius. A drop lasts the PrizeExpire of the terrain it lands on (720 s open space, 120 s docks and trade zones) when the world has the Rogue map.
-   f[22], f[24] and f[25] are kept raw and not applied.

## Open

-   [ ] RT-F042: decode LIO type 5 timing, condition, count and trap fields (queue G6 owns the LIO reader) and validate them natively.
-   [ ] Original map scale and world extent (RT-F021).
-   [ ] Death and spec-mode mineral loss (NWS rows 218, 359, 472).
