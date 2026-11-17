Review status: review-pending

# claude/project-thread-m9yj8x

-   RT-ID: RT-F049 (original buy/resale/drop policy), slice only; the box stays open. Base: main 22f0cd4.

## Implemented

-   `SellUpgrade` transaction (idempotent, station-gated, `UpgradeNotOwned` / `UpgradeNotSellable`), wired through `TradingService`, `StationProfileSession.SellUpgradeAsync` and a SELL button on the Upgrades page (`StationScreen`, `GameClient`).
-   Buy path: `IPriceSource.TryGetMaxQuantity` (|ITM f[13]|) and `MaxAllowedExceeded`; zero-price upgrades are never offered by `StationDefinition.CreatePriceTable`.
-   Derived station data now records sell, maxAllowed and droppable; regenerated with `RT_WRITE_DERIVED_STATION=1`.
-   No schema bump: `TransactionKind.SellUpgrade` was appended and kinds persist by name.

## Decisions

-   0-credit sale offered for installed upgrades and armour only (all nine armours resell for 0); cargo keeps "sell > 0".
-   Armour is an upgrade-set member, so the sale also unfits it.
-   Droppable (f[10]) is recorded only: the remake has no voluntary drop, and the emulator's death drop uses pruneOdds, not f[10]; the hold is still emptied on destruction.
-   NWS revision: the pinned ITM ammo rows match the 9/6/01 (1:1) and 3/1/03 (not droppable) lines, not 3/12/03 (droppable, resale 0). The selected ITM wins.

## Results

-   `dotnet test tests/Unit`: 1306 passed, 13 skipped, 1 failed (environmental BloCanonicalTests).
-   Godot import and `--smoke-test`: CLIENT_STATION_SMOKE_PASS including the new sale step (sold module stays sold across destruction and relaunch).

## Files

-   server/RogueTrader.Persistence (SellUpgradeRequest, TransactionKind, RejectionReason, IPriceSource, ITransactionProcessor, TransactionProcessor), server/RogueTrader.Trading (StationPriceTable, StationDefinition, OriginalStationBuilder, StationProfileSession, TradingService), shared/RogueTrader.OriginalData/OriginalItem.cs, client/UI/StationScreen.cs, client/Scripts/GameClient.cs, client/Scripts/ClientStationSmokeTest.cs, data/stations/trading_outpost.json (derived), tests/Unit/Persistence/BuySellPolicyTests.cs and fixtures, tests/Unit/Trading, docs.
