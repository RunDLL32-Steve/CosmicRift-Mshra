# Test modules implementation plan

Reviewed October 13, 2026; documentation and saved result inspection only. Master: [§20 QA](../ROGUE_TRADER_2_PLAN.md#20-quality-assurance), [M1 fidelity gate](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [M2](../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay) and [M3](../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer).

## Current accomplished work and next

| Folder | Status and evidence | Next |
|---|---|---|
| [Unit](Unit/IMPLEMENTATION_PLAN.md) | Current .NET 8 suite includes original raw-data, cargo, persistence/transactions, trading, replay, protocol and NEW AI cases. Historical M1 evidence: [41 cases passed](../artifacts/test-results/unit.trx); see [current source coverage](Unit/IMPLEMENTATION_PLAN.md). | Original acceptance oracles and focused tests after behavior implementation. |
| [Integration](Integration/IMPLEMENTATION_PLAN.md) | Reserved module; no suite here. | Original native entry/full game loop/save and multi-client acceptance. |
| [Network](Network/IMPLEMENTATION_PLAN.md) | Reserved module; no suite here. | Actual protocol/client/server and authority/reconnect tests. |
| [LoadTests](LoadTests/IMPLEMENTATION_PLAN.md) | Reserved module; no harness or measured capacity. | Representative loads after native/runtime/core-loop acceptance. |

Completed inspector source/standalone checks reside in client harnesses: RT-F373/F374 [input evidence](../artifacts/qa/OriginalContent/standalone-input.log), world tick 0. Original parser/package/arena/loopback evidence resides in separate scripts/compatibility and architecture plans: RT-F381/F382/F383/F385/F392/F394. These narrower proofs are not M2/M3 end-to-end tests. Current global counts and exact feature completion status belong to master §18.

## Active outstanding work

- [ ] **Investigating:** native welcome Start/no-result and original data/runtime acceptance expectations.
- [ ] **Not started:** exact original flight/combat/resource/economy/upgrade/repair/reward/progression cases after implementation.
- [ ] **Not started:** real independent clients, full unchanged asset delivery, retained progression and conservation/duplication acceptance.
- [ ] **Not started:** performance/load measurement after earlier gates.

No tests, GUI, builds or simulations ran during this documentation update. Run only focused checks required by a completed feature; baseline gameplay simulations are not triggered by documentation/assets.
