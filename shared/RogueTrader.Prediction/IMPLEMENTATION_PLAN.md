# Controlled-ship prediction implementation plan

Authority: [master §13.3 client responsibilities](../../ROGUE_TRADER_2_PLAN.md#133-client-responsibilities), [§18 M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer), and [§20.3 QA](../../ROGUE_TRADER_2_PLAN.md#203-multiplayer-tests). See the [C7 design](../../docs/architecture/C7_PREDICTION_RECONCILIATION.md) and [per-item evidence](plan.d/c7.md).

Status: **IMPLEMENTED / LOCAL VERIFICATION PASSED**. `ShipPredictor` is a pure C# library referencing Protocol and Simulation. It copies a controlled ship baseline, queues one intent per local tick, predicts through shared movement/collision, and corrects/replays snapshots. The caller supplies a matching catalog, controlled ID, fresh baseline and positive bound, serializing all calls. R1-1 rejects nonfinite derived motion atomically.

-   [x] Motion-only source and focused checks against actual C6 authority: 52 passed, zero failed/skipped (38 prediction plus 14 movement).
-   [x] Local full verification: 1,060 unit passes, zero failures, five key skips; integration 6/6; Godot import/smoke exit 0. Build zero errors, six NU1900 feed warnings; nonfatal certificate diagnostic.
-   [ ] Independent exact-head review/publication; see the [review record](../../docs/development-log/2026-11-02-c7-review.md).
-   [ ] Production renderer, transport, ownership/context establishment and reconnect integration.
-   [ ] Master M3 independent-client gameplay acceptance and original fidelity.

Fire is outbound intent only; hull/shield remain authoritative. No combat, rewards, cargo, mining or persistence effects are predicted. The C5 wire format is unchanged. Queue policy, identifiers, fixtures and controls are NEW/provisional. Original/master feature boxes and registry counts remain unchanged (397/22/375).
