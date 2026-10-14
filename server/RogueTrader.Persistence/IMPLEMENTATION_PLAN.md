# Persistence implementation plan

Status authority: [master §14 persistent data](../../ROGUE_TRADER_2_PLAN.md#14-persistent-data), [§14.1 requirements](../../ROGUE_TRADER_2_PLAN.md#141-persistence-requirements), [M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), [M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer), and [§20.2 economy QA](../../ROGUE_TRADER_2_PLAN.md#202-economy-tests). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; no save/database runs.

Module status: **Not started**. No progression repository/service exists here. Prototype client settings persistence does not satisfy gameplay persistence. M0/M1 engineering acceptance is complete; M2 save/load follows the validated original core-loop contract.

## Outstanding work

- [ ] **Investigating** — RT-F068: selected original character/progression lifecycle and historical backend evidence. Canonical client records/news do not prove the original backend schema or ordering; any unavoidable reconstruction must remain explicit.
- [ ] **Not started** — M2 persistence interfaces and isolated safe local save/load for precisely chosen original hull/skills, inventory/equipment, currency and upgrades; preserve original names/IDs/categories and source version identity.
- [ ] **Investigating** — RT-F035/RT-F043/RT-F049–RT-F050/RT-F054: hull exchange, death/drop, resale/spec/team prune and warp/transfer retention policy. Resolve original event ordering, raw flags, sentinel values and integer currency semantics before defining retained state. Client bank/pose is not progression state without evidence.
- [ ] **Not started** — atomic inventory/currency updates, idempotent transactions, recoverable interrupted writes, versioned saves and explicit corruption handling. Do not silently erase progress or duplicate rewards/items.
- [ ] **Not started** — M2 exit/reload acceptance and economy regressions with [integration tests](../../tests/Integration/IMPLEMENTATION_PLAN.md); verify completed retention behavior after the implemented feature exists.
- [ ] **Not started** — M3 PostgreSQL schema/migrations and concurrency/transfer recovery through interfaces, followed by backup/recovery work in its master milestone. No database capacity/reliability is claimed.
- [ ] **Not started** — exact feature evidence and master/catalog reconciliation. Do not check off RT-F068 from settings files, a prototype build or successful original indexing.

Completion history: no gameplay persistence feature is complete. Prior [prototype acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) verifies local flight/combat only. Documentation review reran no simulations/tests.
