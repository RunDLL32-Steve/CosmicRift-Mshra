# Client/server protocol implementation plan

Status authority: [master §13 multiplayer](../../ROGUE_TRADER_2_PLAN.md#13-multiplayer-architecture), [§13.5 validation](../../ROGUE_TRADER_2_PLAN.md#135-anti-cheat-principles), [M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer), and [§20.3 QA](../../ROGUE_TRADER_2_PLAN.md#203-multiplayer-tests). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [engine evaluation](../../docs/architecture/ENGINE_EVALUATION.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; no network runs.

Module status: **Not started** for M3 acceptance; a NEW/provisional message codec exists (see [plan.d/c5.md](plan.d/c5.md), TASK_QUEUE C5, merged) but no transport/session behavior. M0/M1 engineering acceptance is complete; original fidelity and M2 remain earlier dependencies. Separate parser execution is not session/network acceptance.

## Outstanding work

- [ ] **Investigating** — production engine/native-client route with the compatibility owner. Emulator client-state trust differs from eventual master authority; define the boundary before transport/message selection.
- [ ] **Not started** — M3 versioned/authenticated input, spawn/session messages, authoritative snapshots, acknowledgments, prediction/reconciliation and reconnect. Master snapshot/player-count goals are not measured capacity.
- [ ] **Not started** — state contracts for RT-F050–RT-F054, RT-F058 and RT-F064–RT-F068: spectator/team/reward/stronghold/private-team/warp/ownership/visibility/docking/event/progression behavior. Preserve original identities; reject arbitrary client inventory/currency/damage/item creation.
- [ ] **Investigating** — [original units/sentinels](../../docs/original-research/FIELD_INTERPRETATION.md): coordinates/velocity/yaw, target/location sentinel and timing meanings, native bank/state selection. Do not serialize provisional units as original units or infer packets from parsed fields.
- [ ] **Not started** — [network acceptance](../../tests/Network/IMPLEMENTATION_PLAN.md): malformed/stale commands, authority rejection, independent clients, reconciliation/reconnect and duplicate-state prevention.
- [ ] **Not started** — record actual implemented files/test evidence and reconcile exact verified behavior to master M3; original features stay unchecked until full acceptance.

Completion history: no protocol feature is complete. Prior [prototype acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) covers local simulation/source/export only. No builds/launches/simulations rerun for this review.

## Current reconciliation — October 13, 2026

Documentation-only source/saved-log review; no new build/test/GUI/simulation. Master registry: **394 IDs, 15 narrowly completed scopes, 379 open**. Completed prototype work above is history; original parity remains open unless its exact catalog acceptance passed.

Separate compatibility evidence has advanced beyond parser-only: RT-F381/F382 package/runtime-source audits, RT-F383 actual original arena initialization, RT-F385 exact eight-value parser regression, and RT-F392/F394 full-asset loopback host/authentic shared Soccer are complete. [Runtime plan](../../docs/architecture/ROGUE_RUNTIME_PLAN.md) and [package plan](../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) carry those proofs. This does not implement production services or tests in reserved folders.

The native client reached Welcome FreeInfantry; Start produced no confirmed entry. Native asset delivery, actual original gameplay and durable progression remain unverified. Standalone compatibility mode does not save progression. Owned test client/backend/private namespace were removed; no desktop interaction while the user is playing. [Native plan](../../docs/architecture/NATIVE_CLIENT_PLAN.md).

Next remains the unchecked per-feature queue in this document, using original fowlmouth records unchanged. Completed original viewer/package/input scopes RT-F020/F073/F373/F374 are historical proof, not native original appearance. RT-F375 all-archive inventory is complete; RT-F069 runtime dependency closure and Portal/logo/numeric Ball/item66 eligibility remain open. Original units/evaluators/bank/blit rules and shop/proximity/null-script bugs remain unresolved. NEW prototype tuning and modern expansion proposals cannot replace original parity.

