# Network test implementation plan

Status authority: [master §13 multiplayer](../../ROGUE_TRADER_2_PLAN.md#13-multiplayer-architecture), [M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer), and [§20.3 QA](../../ROGUE_TRADER_2_PLAN.md#203-multiplayer-tests). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [protocol plan](../../shared/RogueTrader.Protocol/IMPLEMENTATION_PLAN.md), [server plan](../../server/RogueTrader.Server/IMPLEMENTATION_PLAN.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; no network runs.

Module status: **Not started**. There is no network suite here. M0/M1 engineering acceptance is complete; original fidelity, M2 and production engine/native-client compatibility remain dependencies. Parser acceptance and local client smoke do not establish network parity.

## Outstanding work

- [ ] **Investigating** — settle native-client compatibility/trust boundary and expected protocol revision; [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md) distinguishes emulator client motion from eventual server authority. Unit/coordinate/yaw/timing/sentinel and bank/state semantics cannot be inferred from packets or provisional data alone.
- [ ] **Not started** — M3 connection/authentication/version mismatch, spawn, malformed/stale input rejection and authority validation. Client input must not directly overwrite world, damage, currency or inventory.
- [ ] **Not started** — prediction/reconciliation, reconnect and independent-client combat/shared mining/NPC acceptance after protocol/server implementation.
- [ ] **Not started** — original transition/ownership/visibility/progression cases RT-F050–RT-F058/RT-F064–RT-F068: invalid warps, team/spec prune, failed transfers, duplicate commands/rewards, retained inventory and reconnect state.
- [ ] **Not started** — exact evidence/master/catalog reconciliation after implemented tests pass. Keep original record identities/categories/raw values traceable; narrower successful connections cannot complete all original multiplayer behavior.

Completion history: no network feature/test is complete. [Prior prototype acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) covers local engineering only. No builds/launches/simulations rerun for this review.

## Current reconciliation — October 13, 2026

Documentation-only source/saved-log review; no new build/test/GUI/simulation. Master registry: **394 IDs, 15 narrowly completed scopes, 379 open**. Completed prototype work above is history; original parity remains open unless its exact catalog acceptance passed.

Separate compatibility evidence has advanced beyond parser-only: RT-F381/F382 package/runtime-source audits, RT-F383 actual original arena initialization, RT-F385 exact eight-value parser regression, and RT-F392/F394 full-asset loopback host/authentic shared Soccer are complete. [Runtime plan](../../docs/architecture/ROGUE_RUNTIME_PLAN.md) and [package plan](../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) carry those proofs. This does not implement production services or tests in reserved folders.

The native client reached Welcome FreeInfantry; Start produced no confirmed entry. Native asset delivery, actual original gameplay and durable progression remain unverified. Standalone compatibility mode does not save progression. Owned test client/backend/private namespace were removed; no desktop interaction while the user is playing. [Native plan](../../docs/architecture/NATIVE_CLIENT_PLAN.md).

Next remains the unchecked per-feature queue in this document, using original fowlmouth records unchanged. Completed original viewer/package/input scopes RT-F020/F073/F373/F374 are historical proof, not native original appearance. RT-F375 all-archive inventory is complete; RT-F069 runtime dependency closure and Portal/logo/numeric Ball/item66 eligibility remain open. Original units/evaluators/bank/blit rules and shop/proximity/null-script bugs remain unresolved. NEW prototype tuning and modern expansion proposals cannot replace original parity.

