# Client/server protocol implementation plan

Status authority: [master §13 multiplayer](../../ROGUE_TRADER_2_PLAN.md#13-multiplayer-architecture), [§13.5 validation](../../ROGUE_TRADER_2_PLAN.md#135-anti-cheat-principles), [M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer), and [§20.3 QA](../../ROGUE_TRADER_2_PLAN.md#203-multiplayer-tests). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [engine evaluation](../../docs/architecture/ENGINE_EVALUATION.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; no network runs.

Module status: **Not started** for M3 acceptance; a NEW/provisional message codec exists (see [plan.d/c5.md](plan.d/c5.md), TASK_QUEUE C5, merged) but no transport/session behavior. M0/M1 engineering acceptance is complete; original fidelity and M2 remain earlier dependencies. Separate parser execution is not session/network acceptance.

## Outstanding work

-   [ ] **Investigating** — production engine/native-client route with the compatibility owner. Emulator client-state trust differs from eventual master authority; define the boundary before transport/message selection.
-   [ ] **In progress (codec groundwork only)** — `Messages.cs` already defines versioned PlayerInput, WorldSnapshot, TransactionRequest and TransactionResponse; `MessageCodec` serializes them. Authenticated sessions, spawn routing, acknowledgment processing, prediction/reconciliation and reconnect remain unimplemented. Master snapshot/player-count goals are not measured capacity.
-   [ ] **Not started** — state contracts for RT-F050–RT-F054, RT-F058 and RT-F064–RT-F068: spectator/team/reward/stronghold/private-team/warp/ownership/visibility/docking/event/progression behavior. Preserve original identities; reject arbitrary client inventory/currency/damage/item creation.
-   [ ] **Investigating** — [original units/sentinels](../../docs/original-research/FIELD_INTERPRETATION.md): coordinates/velocity/yaw, target/location sentinel and timing meanings, native bank/state selection. Do not serialize provisional units as original units or infer packets from parsed fields.
-   [ ] **Not started** — [network acceptance](../../tests/Network/IMPLEMENTATION_PLAN.md): malformed/stale commands, authority rejection, independent clients, reconciliation/reconnect and duplicate-state prevention.
-   [ ] **Not started** — record actual implemented files/test evidence and reconcile exact verified behavior to master M3; original features stay unchecked until full acceptance.

Completion history: NEW codec groundwork is recorded in [C5](plan.d/c5.md); no live network feature or M3 acceptance is complete. Prior [prototype acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) covers local simulation/source/export only. No builds/launches/simulations rerun for this review.

## Source audit — October 17, 2026

Current code and remaining integration limits are recorded in the [source documentation audit](../../docs/audits/2026-10-17-source-docs.md). Earlier dated test counts above are historical branch results, not a current-tree test run. No build, runtime check, GUI or simulation was run for this documentation audit. Global feature counts and completion status belong to [master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap).
