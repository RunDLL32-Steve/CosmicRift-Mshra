# Controlled-ship prediction implementation plan

Authority: [master §13.3 client responsibilities](../../ROGUE_TRADER_2_PLAN.md#133-client-responsibilities), [§18 M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer), and [§20.3 QA](../../ROGUE_TRADER_2_PLAN.md#203-multiplayer-tests). See the [C7 design](../../docs/architecture/C7_PREDICTION_RECONCILIATION.md) and [per-item evidence](plan.d/c7.md).

Status: **IN PROGRESS**. `ShipPredictor` is a pure C# library referencing Protocol and Simulation. It copies a controlled ship baseline, queues one intent per local tick, predicts motion with shared fixed-step movement/collision, and corrects/replays from authoritative snapshots. The caller supplies a matching validated catalog, controlled ID, fresh baseline and positive pending bound; it serializes all calls.

- [x] Motion-only source and focused deterministic checks against the actual C6 authority: 50 passed, zero failed/skipped (36 prediction plus 14 existing movement).
- [ ] Independent review and complete gate evidence; full verification currently exits 1 due to two inherited newline-oracle failures.
- [ ] Production renderer, transport, ownership/context establishment and reconnect integration.
- [ ] Master M3 independent-client gameplay acceptance and original fidelity.

Fire is outbound intent only; hull/shield remain authoritative. No combat, rewards, cargo, mining or persistence effects are predicted. The C5 wire format is unchanged. Queue policy, identifiers, fixtures and controls are NEW/provisional. Original/master feature boxes and registry counts remain unchanged (397/22/375).
