# Controlled-ship prediction implementation plan

Authority: [master §13.3 client responsibilities](../../ROGUE_TRADER_2_PLAN.md#133-client-responsibilities), [§18 M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer), and [§20.3 QA](../../ROGUE_TRADER_2_PLAN.md#203-multiplayer-tests). See the [C7 design](../../docs/architecture/C7_PREDICTION_RECONCILIATION.md) and [per-item evidence](plan.d/c7.md).

Status: **LOCAL ACCEPTANCE VERIFIED / UNPUBLISHED**. `ShipPredictor` is a pure C# library referencing Protocol and Simulation. It copies a controlled ship baseline, queues one intent per local tick, predicts through shared movement/collision, and corrects/replays snapshots. The caller supplies a matching catalog, controlled ID, fresh baseline and positive bound, serializing all calls. R1-1 rejects nonfinite derived motion atomically.

-   [x] All five pure-library clauses covered: queued prediction, correction/replay, stale/duplicate snapshots, acknowledgement validation and latency/loss. Three added two-way wire-codec/C6 schedules pass during ongoing input issue; the prediction selection now has 41 cases plus 14 movement cases.
-   [x] Completion unit run: 1,063 passed, zero failed, five key skips (1,068 total). Earlier unchanged-production full-wrapper evidence: 1,060 unit passes/5 skips, integration 6/6, import/smoke exit 0; wrapper not rerun for completion test/docs changes.
        Independent acceptance verdict: [completion audit](../../docs/development-log/2026-11-02-c7-completion-audit.md).

-   [ ] Publication PR/sprint/merge gates.
-   [ ] Production renderer, transport, ownership/context establishment and reconnect integration.
-   [ ] Master M3 independent-client gameplay acceptance and original fidelity.

Fire is outbound intent only; hull/shield remain authoritative. No combat, rewards, cargo, mining or persistence effects are predicted. The C5 wire format is unchanged. Queue policy, identifiers, fixtures and controls are NEW/provisional. Original/master feature boxes and registry counts remain unchanged (397/22/375).
