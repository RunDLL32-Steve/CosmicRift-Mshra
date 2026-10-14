# Dedicated server implementation plan

Status authority: [master §13.2 authority](../../ROGUE_TRADER_2_PLAN.md#132-server-responsibilities), [§13.5 validation](../../ROGUE_TRADER_2_PLAN.md#135-anti-cheat-principles), [M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer), and [§20.3 QA](../../ROGUE_TRADER_2_PLAN.md#203-multiplayer-tests). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [engine evaluation](../../docs/architecture/ENGINE_EVALUATION.md), [protocol plan](../../shared/RogueTrader.Protocol/IMPLEMENTATION_PLAN.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; documentation inspection only.

Module status: **Not started**. This folder is reserved; no dedicated RogueTrader server service is implemented. M0/M1 engineering acceptance is complete. Original fidelity, engine selection and the M2 loop remain dependencies.

## Outstanding work

- [ ] **Investigating** — pinned InfantryOnline server/native-client route. Separate compile/parser acceptance does not demonstrate a running compatible game, authenticated sessions or native parity. Existing compatibility probe rejects initialization; missing package dependencies remain visible in its logs. Do not describe the emulator player-update path as the eventual authoritative physics implementation.
- [ ] **Not started** — M3 sessions/authentication, validated input, authoritative movement/combat/economy, shared mining/NPCs, reconnect, protocol integration and database integration. Keep [simulation](../RogueTrader.Simulation/IMPLEMENTATION_PLAN.md), AI and persistence modular.
- [ ] **Investigating** — original transition/reward/spawn/warp contracts RT-F050–RT-F054/RT-F067: native event ordering, team/skill restrictions, targets, coordinate/timing units and location sentinels require [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md). Runtime implementation is **Not started**.
- [ ] **Not started** — original ownership/production/visibility/dock behavior RT-F055–RT-F058/RT-F064–RT-F065 after per-record dependencies are validated; preserve original actor/object identities, recipes and values.
- [ ] **Not started** — master M3 independent-client acceptance: join, move, fight, mine, trade and retain progress without duplication/desynchronization. [Network](../../tests/Network/IMPLEMENTATION_PLAN.md)/[integration](../../tests/Integration/IMPLEMENTATION_PLAN.md) provide evidence only after actual implementations.
- [ ] **Not started** — record exact feature/test evidence and reconcile master/catalog status. Native palette/banking/state selection remains a client dependency; snapshots must not invent original state semantics.

Completion history: no dedicated server feature is complete. The prior [M0/M1 acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) concerns the local provisional prototype. No builds/launches/simulations ran for this review.
