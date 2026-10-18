# AI implementation plan

Status authority: [master §12.1 initial NPC](../../ROGUE_TRADER_2_PLAN.md#121-initial-npc-pirate), [§12.2 states](../../ROGUE_TRADER_2_PLAN.md#122-ai-states), [M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), and [M3 shared NPCs](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md), [simulation plan](../RogueTrader.Simulation/IMPLEMENTATION_PLAN.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; no AI simulations.

Module status: **Provisional NEW pirate controller present (TASK_QUEUE M6, merged; see [plan.d/m6.md](plan.d/m6.md)); no original AI feature complete**. Stationary M1 targets have no AI. M0/M1 engineering acceptance is complete; validated original actor/controller dependencies precede the M2 enemy loop.

## Outstanding work

- [ ] **Investigating** — RT-F060/RT-F272/RT-F306: canonical PirateStation and Pirate Base controller/child-ship/loadout/spawn/drop/team relationships. Do not invent a generic pirate and call it original parity.
- [ ] **Investigating** — RT-F059/RT-F062 and relevant RT-F269–RT-F285 actors: distinct Null enemies, mold/spore ecology and production/defense/refinery/repair actors. Preserve original categories, faction/owner values and unresolved/missing references; detailed records remain in the catalog.
- [ ] **In progress (NEW engine groundwork)** — `PirateController.Think` already emits targeting, pursuit, turn/thrust and fire decisions; [M6](plan.d/m6.md) records tests against the player slot. Separate NPC execution, damage-to-player and reward attribution are still absent from `WorldSimulation`; original controller behavior remains unverified.
- [ ] **Investigating** — RT-F027–RT-F032/RT-F036–RT-F041/RT-F051/RT-F067: native movement/projectile/energy/reward units, negative sentinels and event order. Emulator bot formulas differ from its client-controlled player path and cannot independently prove native AI parity.
- [ ] **Not started** — per-feature tests for implemented NPC behavior and its original spawn/drop/reward chain, then M3 shared execution acceptance. Run focused simulation only after the specific gameplay feature exists and needs verification.
- [ ] **Investigating** — original direction/bank/pose and animation selection RT-F023–RT-F024/RT-F074–RT-F075 with the client. Visible original artwork cannot complete the AI feature.
- [ ] **Not started** — record implementation/evidence and reconcile only verified actor features to master/catalog; leave discovery and inferred behavior unchecked.

Completion history: NEW controller groundwork is recorded in [M6](plan.d/m6.md); no original AI feature or playable NPC integration is complete. Prior [41-test prototype acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) exercises stationary targets, not pursuit/NPC intelligence. No builds/launches/tests rerun for this review.

## Source audit — October 17, 2026

Current code and remaining integration limits are recorded in the [source documentation audit](../../docs/audits/2026-10-17-source-docs.md). Earlier dated test counts above are historical branch results, not a current-tree test run. No build, runtime check, GUI or simulation was run for this documentation audit. Global feature counts and completion status belong to [master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap).
