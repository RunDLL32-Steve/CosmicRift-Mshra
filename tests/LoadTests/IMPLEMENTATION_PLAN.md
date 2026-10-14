# Load test implementation plan

Status authority: [master §13.4 network targets](../../ROGUE_TRADER_2_PLAN.md#134-network-targets), [M5](../../ROGUE_TRADER_2_PLAN.md#milestone-5--advanced-multiplayer-systems), [M6 optimization](../../ROGUE_TRADER_2_PLAN.md#milestone-6--release-preparation), and [§20.4 performance](../../ROGUE_TRADER_2_PLAN.md#204-performance-requirements). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [server plan](../../server/RogueTrader.Server/IMPLEMENTATION_PLAN.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; no load runs.

Module status: **Not started**. No harness or measured multiplayer capacity exists here. M0/M1 engineering acceptance is complete; earlier original-fidelity/M2/M3 gates precede load testing. The master 60 FPS, 20 snapshots/second and 16–32 player figures are goals, not validated capacity.

## Outstanding work

- [ ] **Not started** — instrument an actually implemented authoritative world; record reproducible host/toolchain/data revision, client count, tick latency, CPU/GPU/memory/network and database/transaction behavior.
- [ ] **Not started** — M5 representative canonical workloads from completed RT-F042–RT-F068 resource/economy/reward/actor/ownership/visibility/transition/progression features and associated RT-F269–RT-F367 actors/objects. Do not fabricate workload rates from unexplained raw original timing fields.
- [ ] **Investigating** — production engine/native-client route and original runtime semantics through [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md). Units, sentinels, actor/event timing and native render/bank/state selection must be resolved before claims about representative original load; graphics/native-client profiling stays distinct from server profiling.
- [ ] **Not started** — measure realistic steady/burst activity, simultaneous transactions, reconnect/transfer recovery and long-lived memory behavior after those gameplay/services exist. Validate correctness/duplication along with throughput.
- [ ] **Not started** — justify player-limit increases from recorded measurements; identify bottlenecks before adding infrastructure. M6 optimization/regression follows earlier acceptance.
- [ ] **Not started** — record per-feature limits/evidence and reconcile only measured master targets. Index/parser counts or 41 prototype unit tests cannot complete performance acceptance.

Completion history: no load feature/test is complete. Prior [prototype acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) establishes local playability only. No builds/launches/simulations rerun for this documentation review; simulations wait until a completed gameplay feature requires verification.

## Current reconciliation — October 13, 2026

Documentation-only source/saved-log review; no new build/test/GUI/simulation. Master registry: **394 IDs, 15 narrowly completed scopes, 379 open**. Completed prototype work above is history; original parity remains open unless its exact catalog acceptance passed.

Separate compatibility evidence has advanced beyond parser-only: RT-F381/F382 package/runtime-source audits, RT-F383 actual original arena initialization, RT-F385 exact eight-value parser regression, and RT-F392/F394 full-asset loopback host/authentic shared Soccer are complete. [Runtime plan](../../docs/architecture/ROGUE_RUNTIME_PLAN.md) and [package plan](../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) carry those proofs. This does not implement production services or tests in reserved folders.

The native client reached Welcome FreeInfantry; Start produced no confirmed entry. Native asset delivery, actual original gameplay and durable progression remain unverified. Standalone compatibility mode does not save progression. Owned test client/backend/private namespace were removed; no desktop interaction while the user is playing. [Native plan](../../docs/architecture/NATIVE_CLIENT_PLAN.md).

Next remains the unchecked per-feature queue in this document, using original fowlmouth records unchanged. Completed original viewer/package/input scopes RT-F020/F073/F373/F374 are historical proof, not native original appearance. RT-F375 all-archive inventory is complete; RT-F069 runtime dependency closure and Portal/logo/numeric Ball/item66 eligibility remain open. Original units/evaluators/bank/blit rules and shop/proximity/null-script bugs remain unresolved. NEW prototype tuning and modern expansion proposals cannot replace original parity.

