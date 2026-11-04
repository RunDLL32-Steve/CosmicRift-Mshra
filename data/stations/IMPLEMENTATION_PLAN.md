# Station data implementation plan

Authority: [master §18 Milestone 2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), [data plan](../IMPLEMENTATION_PLAN.md) and queue [M8 brief](../../docs/tasks/m8.md). November 1, 2026.

## Purpose

`trading_outpost.json` is the testbed station the Godot client docks at. It is a **derived output**: `RogueTrader.Trading.OriginalStationBuilder` builds it from the pinned originals (`ITM/Rogue_mp.itm`, `VEH/Rogue.veh` at `eb1514e`), and `OriginalStationTests.CommittedStationMatchesPinnedOriginals` fails if the file drifts. Regenerate with `RT_WRITE_DERIVED_STATION=1 dotnet test tests/Unit --filter CommittedStationMatchesPinnedOriginals`; never hand-edit it. The build stages it into `client/GameData/stations/`. The file is in `.prettierignore` because the oracle test compares exact bytes.

## From the originals

| Data                                                                                                          | Source                               |
| ------------------------------------------------------------------------------------------------------------- | ------------------------------------ |
| Station name "Trading Outpost"                                                                                | VEH id 404                           |
| 7 resources, names and raw mass (used as cargo weight)                                                        | ITM type-4 Resources rows            |
| 4 trade recipes (Tellerium, Krandazite, Wreckage, Alloy), input item and credit item                          | VEH id 404 trade blocks (RT-F045)    |
| Credits per unit 55 / 80 / 300 / 20                                                                           | credit item raw f[31]                |
| Minor / Major / Full Repair, amounts 5 / 10 / 50                                                              | ITM ids 39/40/43 raw f[33] (RT-F047) |
| 35 upgrades: 26 Engine/Thruster/Power/HyperDrive rows and 9 Hull Armor rows, names and raw buy prices         | ITM (RT-F041/F048)                   |
| Held category and raw modifier block                                                                          | ITM f[20] and f[42..44]              |
| Refinery name and 3 recipes (Tellerium, Krandazite, Iron Ore to Refined Tellerium, Refined Krandazite, Alloy) | VEH id 400 refine blocks (RT-F044)   |
| Refinery map position (10752, 624)                                                                            | LIO row 13 "RefineryA" (actor -400)  |

No original resource has a positive buy price (sell is 0 or -1), so there is no buy/sell market: resources are only handed in through the four recipes.

## NEW/provisional (listed in the file's `newValues`)

-   Position (90, 20) and dock radius 150; starting wallet 0 (CFG FirstTimeInvSetup setcash=0; RPG f[38]=3499 is the unproven alternative); the hold cap moved to the ship (`data/ships/starter_interceptor.json` `cargo.capacity` = the Asp VEH f[48] StopWeight 2,500,000; the station keeps only the item weights).
-   Each recipe pays the raw f[31] value per input unit; recipe quantities and f+4 are undecoded.
-   Repair amounts are read as hull points; price is 10 credits per point (original buy is 0). A repair at full hull or on a destroyed ship is refused without charge.
-   Upgrade effects: f[42..44] / 1000 as forward thrust / maximum speed / rotation fractions (armor's negative values slow the ship); maneuver thrusters +5/10/15% rotation; capacitors +25/50/75% maximum energy; chargers and the Moltor core +25/50/75% regeneration; armor adds buy/200 hull points; HyperDrive abilities have no effect yet. Only the highest-priced owned item per held category is active.
-   Refinery: map position drawn at the mineral-field scale (see [minerals](../minerals/IMPLEMENTATION_PLAN.md)), dock radius 150, one input unit becomes one output unit, free (quantities, cost, f+4 and timing undecoded). Profile schema 4 adds the `Refine` transaction kind.
-   Collectible piles: 9 Wreckage at (0, -70), 12 Alloy at (-170, 20), 6 Refined Tellerium at (40, 160).

## Open

-   [ ] RT-F044 refinery quantities, cost, radius and conditions; the second refinery (LIO row 14) is not placed.
-   [ ] Services are split by site (Steve, 2026-11-04): goods at the Trading Posts (LIO rows 21 and 22), repairs at the Repair Docks (rows 36 to 39 and 58), upgrades, hulls and the hangar at each team's home store. Whether a store served only its own team, and the original dock radius, are unknown (dock radius 150 is NEW).
-   [ ] Original station placement/dock rule, recipe quantities, repair cost, starting cash and every upgrade effect (RT-F041/F045/F047/F048/F049, RT-F068).
-   [ ] Collectible piles respawn on a sector reset (not on an upgrade or reload); the original respawn rule is unknown.
