# Server modules implementation plan

Reviewed October 13, 2026; documentation/source/evidence inspection only. Master: [§13 server authority](../ROGUE_TRADER_2_PLAN.md#13-multiplayer-architecture), [M1 fidelity gate](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [M2](../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay) and [M3](../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer).

## Accomplished and module map

| Folder | Current code and status | Next |
|---|---|---|
| [Simulation](RogueTrader.Simulation/IMPLEMENTATION_PLAN.md) | Complete provisional .NET 8 fixed-60-Hz flight/combat; historical 41-case acceptance. | Investigating original units/physics/evaluators; original gameplay remains open. |
| [Server](RogueTrader.Server/IMPLEMENTATION_PLAN.md) | Reserved plan; no dedicated source service here. | Investigating selected engine route and native entry. |
| [Persistence](RogueTrader.Persistence/IMPLEMENTATION_PLAN.md) | Reserved plan; no gameplay save implementation. | Original retention policy and actual durable progression. |
| [AI](RogueTrader.AI/IMPLEMENTATION_PLAN.md) | Reserved plan; stationary prototype targets have no AI. | Validate original actor/controllers through selected engine. |

Separate ignored InfantryOnline compatibility source/harnesses now pass parser, package audit, original arena initialization and bounded full-asset loopback hosting: RT-F381/F382/F383/F385/F392/F394. See [runtime plan](../docs/architecture/ROGUE_RUNTIME_PLAN.md), [package audit](../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) and [native plan](../docs/architecture/NATIVE_CLIENT_PLAN.md). These do not implement production services in the reserved folders.

## Active outstanding work

- [ ] **Investigating:** native welcome Start/no-result, actual entry and unchanged file delivery. Owned test client/server/private namespace cleaned; no desktop interaction while the user plays.
- [ ] **Investigating:** original event/flight/combat/economy/actor behavior, shop quantity/proximity and null-script/full-arena defects.
- [ ] **Not started:** durable original progression; standalone compatibility mode does not save.
- [ ] **Not started:** complete original M2 loop and M3 independent-client acceptance through the chosen engine.

All original values remain authoritative and unchanged. 394 feature IDs/15 scoped completions in the master do not establish full gameplay parity. No build/test/server/simulation was run in this documentation review.
