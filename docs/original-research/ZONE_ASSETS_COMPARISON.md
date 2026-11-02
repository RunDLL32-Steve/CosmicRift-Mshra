# Zone-Assets Rogue Trader comparison

November 2, 2026. Steve pointed at [InfantryOnline/Zone-Assets](https://github.com/InfantryOnline/Zone-Assets) as a source of original Rogue Trader zone assets. This note says what that repository holds for Rogue Trader, how it compares with the canonical files we already pin, and which open questions it settles. It is research only: no game code, data file or canonical byte changes, and no binary asset is copied into this repository.

## Sources

| Source                                                                                                                                          | Commit                                                                                                                                       | Use here                                                               |
| ----------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| [InfantryOnline/Zone-Assets](https://github.com/InfantryOnline/Zone-Assets)                                                                     | [`401633b`](https://github.com/InfantryOnline/Zone-Assets/tree/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9) (still `master` HEAD on November 2) | The archive Steve linked                                               |
| [fowlmouth/cosmic-rift-assets](https://github.com/fowlmouth/cosmic-rift-assets)                                                                 | `eb1514e` (our canonical pin)                                                                                                                | Comparison baseline                                                    |
| [InfantryOnline/Infantry-Online-Server](https://github.com/InfantryOnline/Infantry-Online-Server/tree/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849) | `bb97579` (the commit FLIGHT_FIELD_EVIDENCE already cites)                                                                                   | Field names and emulator logic for the LVL, LIO hide and terrain files |

The emulator is a community reimplementation of the same engine. Where this note says "the emulator does X" that is VERIFIED for the emulator and RECONSTRUCTED for the original server; no native run backs it.

## 1. What Zone-Assets holds for Rogue Trader

[INFANTRYONLINE.md](INFANTRYONLINE.md) already pinned this exact commit in October, so the repository is not new to the project. What is new below is the byte-level comparison, the two map files and the emulator readings in sections 3 and 4.

-   One package, `Incompatible Zone Archives/SilentClientCrash/rogue/`: `rogue.cfg`, `Rogue_mp.itm`, `Rogue.veh`, `Rogue.rpg`, `Rogue.lio`, `Rogue.nws`, `Rogue.lvl` and six help/text files (`keys.txt`, `rules.txt`, `comms.txt`, `mods.txt`, `infcmdlst.txt`, `standard.tip`).
-   Two orphan map files: `Orphaned Archive Files/lvl/roguetrader.lvl` and `Orphaned Archive Files/lvb/roguetrader.lvb`.
-   Orphan BLOs already inventoried in INFANTRYONLINE.md (`rogueships`, `RogueWeapons`, `cruiart`, `spacestations`, `Resources` and others).
-   No other zone in the repository contains any Rogue Trader item name (searched every text file for Tellerium, Krandazite, Skithist, Mastakith, Rak'Kir and "Rogue Trader"). The `cr`, `riftball`, `chaoszone` and `CRPL` folders are other Cosmic Rift modes.

## 2. Byte comparison with the canonical pin

| Zone-Assets file                    | Canonical file (`eb1514e`)                        | Result                                                                                         |
| ----------------------------------- | ------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `rogue.cfg` (3,051 lines)           | `CFG/rogue.cfg`                                   | Identical after stripping CR. Canonical uses CRLF, Zone-Assets uses LF; no other byte differs. |
| `Rogue_mp.itm` (200 rows)           | `ITM/Rogue_mp.itm`                                | Identical after stripping CR                                                                   |
| `Rogue.veh` (30 rows)               | `VEH/Rogue.veh`                                   | Identical after stripping CR                                                                   |
| `Rogue.rpg` (12 rows)               | `RPG/Rogue.rpg`                                   | Identical after stripping CR                                                                   |
| `Rogue.lio` (81 rows)               | `LIO/Rogue.lio`                                   | Identical after stripping CR                                                                   |
| `Rogue.nws` (482 lines)             | `NWS/Rogue.nws`                                   | Identical after stripping CR                                                                   |
| `Rogue.lvl` (1,290,994 B)           | `LVL/Rogue.lvl` (LFS)                             | Same SHA-256 `70344768…cf02f`                                                                  |
| `roguetrader.lvl` (1,298,731 B)     | none                                              | A different, older revision of the same map (section 3)                                        |
| `cruiart.blo`, `UtilityEffects.blo` | `BLO/cruiart.blo`, `BLO/UtilityEffects.blo` (LFS) | Same SHA-256; neither has a `logo` or `PrizeCrateA` member, so RT-F377/F378 stay open          |

So for the hull, item, LIO, mineral and CFG data **Zone-Assets is the same revision we already have**. It cannot settle any question that depends on a different data revision, in particular:

-   **Ammo weight: still 0.** Every type-4 ammo row has f[7] = 0 in both copies of `Rogue_mp.itm`. The canonical repository's alternative `ITM/Rogue.itm` (not loaded by the CFG, and not in Zone-Assets) was also checked: its ammo rows are 0 too. Its only differences from `Rogue_mp.itm` are listed below for the record. The NWS 3/01/03 "mines now have weight" and "adjusted the weights of missile and cannon ammo" notes therefore describe a revision that neither archive holds; the G5 revision-mismatch item stays open.
-   **Mineral weights: unchanged.** Tellerium 50,000, Refined Tellerium 25,000, Krandazite 100,000, Refined Krandazite 50,000, Wreckage 200,000, Iron Ore 100,000, Alloy 75,000 (ITM f[7]), the same in every copy.

`ITM/Rogue.itm` against `ITM/Rogue_mp.itm` (raw values, `Rogue.itm` first): Scrap Credits 19 f[31] 100 vs 300; Hyper Beacon System 29 f[5] empty vs the hull restriction string; Mass Driver 1024 and Light Mass Driver 1028 f[33] ammoCapacity 5 vs 0; MIRV 1047, BioMissile Storm 1063 and Moltor Shower 1072 f[36] 0 vs 150000 and f[54] fireDelay 150 vs 400; MIRV Missiles 2002 f[13] -5 vs -4; BioStorm Missiles 2013 f[13] -6 vs -4; Munitions Manufactory 84 only in `Rogue_mp.itm`. `Rogue_mp.itm` stays the selected file.

## 3. The map: which terrains the Rogue LVL uses (closes G8 item 6)

Both LVL files were decoded with the layout of the emulator's LVL reader (`Parsers/Level.cs`, `dotnetcore/Assets/Lvl/LvlInfo*.cs`): a 5,152-byte header, 64-byte blob references when the version is 6 or higher, then three RLE planes of width × height bytes. The terrain of a tile is `TerrainLookup[plane0 & 0x7F]`, with the lookup table at header offset 0x820 taken modulo 16; collision is `plane2 & 0x1F` and vision `plane2 >> 5`. All three planes decode to exactly width × height bytes in both files.

| Field                      | `Rogue.lvl` (canonical and Zone-Assets)           | `roguetrader.lvl` (Zone-Assets orphan)          |
| -------------------------- | ------------------------------------------------- | ----------------------------------------------- |
| LVL version                | 6                                                 | 5                                               |
| Size in tiles              | 1999 × 1998                                       | 1999 × 1998                                     |
| Floor / object blob refs   | 9 / 213, embedded in the file                     | 9 / 213 counts; names kept in `roguetrader.lvb` |
| Terrains used (tile count) | 0 (3,976,963), 9 (2,600), 10 (10,343), 15 (4,096) | 0 (3,976,963), 1 (10,343), 2 (6,696)            |
| Collision/vision plane     | 116,065 blocked tiles                             | Byte-identical to `Rogue.lvl`                   |

**Result for G8 section 3:** the Rogue map uses only terrains 0, 9, 10 and 15. No tile uses 1–8 or 11–14, so the `0×8,-1,-1` flight blocks in those CFG sections are never reached in this zone and need no fallback rule. Classification: VERIFIED for the pinned bytes under the emulator's reader; the reader itself is RECONSTRUCTED (it is not the native client).

The terrain regions sit on the LIO stations, which supports the decoding:

| Terrain                       | Regions (centre in pixels, size in tiles)                                                                                                     | Matching LIO rows                                                                                        |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| 9 "Repair Dock"               | (1760, 2976) 20×18; (27568, 1856) 20×18; (24384, 22992) 20×19; (4992, 26464) 20×19; (11696, 8448) 34×34                                       | RepairDockA–E at (1808, 3024), (27616, 1904), (24432, 23008), (5040, 26480), (11712, 8480)               |
| 15 "Refinery Zone/Trade Zone" | (10704, 592), (29440, 29728), (4384, 20800), (28064, 7568), each 32×32                                                                        | RefineryA (10752, 624), RefineryB (29504, 29776), TradingPostA (4448, 20848), TradingPostB (28112, 7616) |
| 10 "SpaceDock. Store Zone."   | 11 regions: one large region each near (1888, 1696), (25872, 21328) and (13648, 8992), plus small groups near (26112, 3232) and (6832, 27264) | Near the ColdHarbour, Cassandra, Northern and Yankee defence-satellite groups and the map centre         |

Every match is within 64 pixels (4 tiles). The physical map extent of 1999 × 16 = 31,984 pixels already in FLIGHT_FIELD_EVIDENCE section 1 is confirmed by the header.

`roguetrader.lvl` is an older build of the same map: same size, same walls and vision, the same per-tile terrain-lookup indexes and the same 9 floor and 213 object reference counts, but a version-5 header that keeps the blob names in `roguetrader.lvb` (not decoded here). Its lookup table maps those indexes to terrains 1 and 2 where the current map uses 9, 10 and 15: tile for tile, every current terrain-10 tile is terrain 1 there, and every current terrain-9 and terrain-15 tile is terrain 2. The two files' first plane differs only in bit 7, whose meaning is not known. It is evidence that the dock/store/refinery terrain split was added later; it does not change the canonical map.

This is not the G6 reader. G6 (claimed) still owns a tested parser in `shared/`; the decode here is a throw-away research script described under Reproduction.

## 4. Mineral spawns: what the emulator says the LIO hide fields mean

The emulator names every LIO hide column (`dotnetcore/Assets/Lio/LioInfo.Hide.cs`) and implements the spawn loop (`dotnetcore/ZoneServer/Game/Arena/ArenaLio.cs`, `hideSpawn`/`hideItem`). For the Rogue mineral rows this gives a reading that **differs from the one our `OriginalMineralFieldBuilder` uses**:

| LIO field | Emulator name     | Rogue mineral values                             | Our builder's reading today    | Emulator behaviour                                                                                                          |
| --------- | ----------------- | ------------------------------------------------ | ------------------------------ | --------------------------------------------------------------------------------------------------------------------------- |
| f[10]     | InitialCount      | 1                                                | not used                       | Number of spawn events when the arena is created                                                                            |
| f[11]     | AttemptDelay      | 1000–6000                                        | retry delay, hundredths of s   | Wait after a failed attempt; emulator multiplies by 100 ms                                                                  |
| f[12]     | SucceedDelay      | 3000–8500                                        | success delay, hundredths of s | Wait after a successful spawn; emulator multiplies by 100 ms                                                                |
| f[13]     | Probability       | 1                                                | kept raw                       | 0 would mean "initial spawn only"                                                                                           |
| f[14]     | MinPlayers        | 0, or 40/60/80 on Tellerium D–G and Krandazite B | player clear radius            | Spawn is skipped while fewer than this many players are in the arena                                                        |
| f[15]     | MaxPlayers        | 500                                              | kept raw                       | Skipped above this many players                                                                                             |
| f[16]     | MinPlayerDistance | 0                                                | kept raw                       | Skip if a player is this close (0 = no check)                                                                               |
| f[18]     | MaxTypeInArea     | 1 (-1 on Tellerium G)                            | units in one pile              | Skip if the hide's own w × h rectangle already holds this many units of the item (-1 = no limit)                            |
| f[19]     | MaxTypeInLevel    | 100 Tellerium and Iron, 50 Krandazite (-1 on G)  | most piles per spawn           | Skip if the **whole level** already holds this many units of the item, counted over every drop of that item (-1 = no limit) |
| f[21]     | HideQuantity      | 1                                                | kept raw                       | Units in each drop                                                                                                          |
| f[26]     | ClumpRadius       | 350                                              | kept raw                       | Drops of one event land within this box around a random anchor in the area                                                  |
| f[27]     | ClumpQuantity     | 15 (Tellerium, Iron), 10 (Krandazite)            | kept raw                       | Drops placed per spawn event                                                                                                |

What this means, if the emulator matches the original:

1.  **Pile size is 1 unit.** f[21] HideQuantity = 1, so each drop is one unit. That agrees with Steve's November 2 decision and today's 1-unit piles, only from a different column.
2.  **A spawn event drops a clump**, 15 one-unit drops of Tellerium or Iron Ore, or 10 of Krandazite, inside a 350-pixel box, not one pile at a time.
3.  **An area refills only when empty.** With f[18] = 1 an area gets a new clump only when none of its mineral is left inside its rectangle.
4.  **The cap is per mineral for the whole map, not per spawn.** At most 100 Tellerium, 100 Iron Ore and 50 Krandazite can lie in space at once (Tellerium G has no cap at all). Today each of our eleven derived spawns keeps up to 100 piles (50 for Krandazite), 1,000 units in all, and Tellerium G is skipped.
5.  **At arena start the emulator places exactly 100 drops**: Tellerium A, B, C (3 × 15), Krandazite A (10) and Iron A, B, C (3 × 15). The other five mineral spawns need 40–80 players. This is the same "100 mineral drops" the ArenaProbe recorded in October (INFANTRYONLINE.md checkpoint). The probe runs the same emulator code, so that is a consistency check of this reading, not independent proof.
6.  **Timing is open.** The emulator turns 6,000 into 600 seconds (×100 ms); our builder reads hundredths of a second (60 s). Nothing read here fixes the unit.
7.  **Drops expire.** The emulator gives every dropped item the `PrizeExpire` of the terrain it lands on, in seconds: 720 (12 minutes) for terrain 0 open space, 120 in docks and trade zones (`rogue.cfg` `[Terrain0]`, `[Terrain9]`, `[Terrain10]`, `[Terrain15]`). Unpicked minerals would vanish after 12 minutes, and that is what lets an untouched area respawn. Our world keeps every pile until it is picked up.
8.  `rogue.cfg` `[StartGame] InitialHides=0`: in the emulator that means the initial spawns run once when the arena is created and the hide timers run all the time, without a "game" having to start. That fits the persistent-world rule.

Weight check with the pinned values: a 15-unit Tellerium clump weighs 750,000 against the Asp's 2,500,000 StopWeight, and the 100-unit Tellerium map cap weighs 5,000,000, enough for two empty Asp holds. Steve's November 2 rule asks for each field to hold enough for about five ships at once, so adopting the emulator reading as it stands would give less mineral than that rule. **This is a decision for Steve**, not something this note changes.

## 5. Status of the open items

| Item                                       | After this comparison                                                                                                                                                                                |
| ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| G8 item 6, terrain indexes                 | **Settled**: terrains 0, 9, 10, 15 only (section 3).                                                                                                                                                 |
| G8 items 1–5 (speeds, turns, decay, hyper) | Open. Zone-Assets has no client or footage; these still need a local native run.                                                                                                                     |
| G6 LVL/LIO reader                          | Still open and claimed. The format, version-5/6 split and expected counts above give the reader concrete test values (1999 × 1998, 22,356 entities, 9 floor and 213 object refs, terrain histogram). |
| G9 missing members                         | Open. Zone-Assets `cruiart.blo` and `UtilityEffects.blo` are the same bytes as ours, so `logo` and `PrizeCrateA` are still absent.                                                                   |
| G10                                        | Still waiting on G6 and native measurements; the terrain result removes one unknown from the flight side.                                                                                            |
| G5 ammo weight                             | Open. No copy of the item file gives ammo any weight.                                                                                                                                                |
| Mineral pile size and weights              | Pile size 1 confirmed from f[21]; weights unchanged. Clumps, area-empty refill, map-wide caps, MinPlayers gating, timing unit and expiry are new readings that need Steve's call (section 4).        |
| RT-F026/F368–F372 help files               | No change; the same six text files were already recovered.                                                                                                                                           |

## Reproduction

All read-only, in a scratch folder outside the repository; run Python with `-I`.

1.  Clone Zone-Assets at `401633b`, fowlmouth at `eb1514e` and Infantry-Online-Server at `bb97579` (blobless clones are enough).
2.  Section 2: strip `\r` from each canonical text file and `cmp` it with the Zone-Assets file; compare the LVL and BLO SHA-256 with the canonical LFS pointer `oid`.
3.  Section 3: read the header with `struct` (`<6i` at 0 for version, width, height, entity, floor and object counts; `<128i` at 0x820 for the terrain lookup), skip 64 × (floors + objects) bytes when the version is 6 or higher, then decode three RLE planes (a count byte, or 0 followed by a big-endian 16-bit count, then the value byte) of width × height bytes each. Count `lookup[p0 & 0x7F] % 16` per tile and flood-fill each non-zero terrain to get the regions.
4.  Section 4: parse `Rogue.lio` rows with `csv` (`skipinitialspace=True`, latin-1) and read the type-5 rows with the column names in `LioInfo.Hide.cs`.
