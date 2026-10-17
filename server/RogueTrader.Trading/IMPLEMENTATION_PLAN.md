# Trading implementation plan

Status authority: [master M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), [§14 persistence](../../ROGUE_TRADER_2_PLAN.md#14-persistent-data), [§20.2 economy tests](../../ROGUE_TRADER_2_PLAN.md#202-economy-tests), and the original-economy catalog rows [RT-F044](../../docs/original-research/FEATURE_CATALOG.md#rt-f044), [RT-F045](../../docs/original-research/FEATURE_CATALOG.md#rt-f045) and [RT-F049](../../docs/original-research/FEATURE_CATALOG.md#rt-f049). Queue item M4 in [TASK_QUEUE.md](../../TASK_QUEUE.md). Created October 16, 2026.

## Purpose and design decision

A small .NET 8 composition project that lets a docked ship buy and sell through the Persistence transaction core. It exists because `RogueTrader.Persistence` (wallet, inventory, idempotent transactions, `ICargoPolicy`, `IPriceSource`) and `RogueTrader.Simulation` (`CargoCatalog`, world units) do not reference each other and should not; a new project referencing both plus `RogueTrader.OriginalData` is the narrowest place for the adapter and orchestration. No existing project gained a dependency.

| Piece | Role |
|---|---|
| `StationSite`, `Docking`, `StationDirectory` | Station position and dock radius in world units; inclusive, double-precision distance test; unknown ids and non-finite positions are not docked. |
| `StationPriceTable` | `IPriceSource`: explicit tables (a repeated item id is refused, never silently overwritten), or a provisional binding of the original ITM buy (f[8]) / sell (f[15]) fields. Raw values are kept in `RawPrices`; sentinel -1 (34 rows) and raw 0 are "not traded" in this provisional rule only. |
| `CatalogCargoPolicy` | Production `ICargoPolicy` over `CargoCatalog.Fits` (unknown items refused). |
| `TradingService` | Computes the docking verdict from the authoritative ship position and calls `BuyAsync`/`SellAsync`. The processor is always called so an already applied transaction id replays even after the ship moved away. |

## Accomplished (October 16, 2026)

- [x] **Complete with evidence (engine-side, NEW fixtures)** — buy/sell through the transaction core with docking, price source and cargo capacity. Evidence: [TradingStationTests.cs](../../tests/Unit/Trading/TradingStationTests.cs), 28 cases, `dotnet test tests/Unit` 548 passed, 5 pre-existing skips, 0 failed. Does not complete RT-F044, RT-F045 or RT-F049.

## Outstanding work

- [ ] **Investigating** — RT-F045/RT-F049: original station inventories, credit units/scaling, the meaning of the raw sell -1 (and 0) and shop eligibility. The all-stations-trade-all-priced-items rule, dock radii and fixture capacities are NEW/provisional.
- [ ] **Investigating** — RT-F044: refinery recipes are not a buy/sell flow and are not modelled here.
- [ ] **Not started** — M5 upgrades and repairs through the same service (`TryGetUpgradePrice` returns false today).
- [ ] **Not started** — client HUD/station UI and server wiring (queue item M8); ship position must come from the authoritative simulation, never from the request.
