# Original arena initializer implementation plan

Purpose: create the actual Rogue Trader arena through pinned generic engine initialization and inspect original spawned state without begin/poll/physics/client input. Master authority: [§18 fidelity gate, RT-F383](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); parent [compatibility plan](../IMPLEMENTATION_PLAN.md), [runtime architecture](../../../docs/architecture/ROGUE_RUNTIME_PLAN.md). Reconciled October 13, 2026.

## Accomplished

RT-F383 is **Complete for original arena initialization**. [Program.cs](Program.cs) uses `ScriptArena(null)` with unchanged original CFG/ITM/VEH/RPG/LIO/LVL and authentic help files, calls `newArena` once and does not call socket begin/poll. [Report](../../../.tools/compatibility/reports/original-arena-probe.json) and [result](../../../.tools/compatibility/reports/original-arena-probe-result.json) record exit 0, one arena, zero clients/inputs/polls/physics ticks and unchanged hashes.

Verified original map 1999×1998, 200 items, 30 vehicles, 12 hull links; 25 computers (2 Refineries, 2 Trading Outposts, 5 Repair Docks, 12 GunSats, 3 MeteorShowers, 1 Solar Mold), 51 teams and 42 hides. Initial 100 drops are 45 Iron Ore, 45 Tellerium and 10 Krandazite. No doors/flags were initialized. This initializer uses `loadBlobs=false`; later full-asset hosting is a separate feature.

## Next and parity

- [ ] **Not started:** native join, F11 hull selection, unspectating, original spawn/warp acceptance.
- [ ] **Investigating:** original entity/evaluator lifecycle beyond initial state, including building-type identity and absent item 66 references.
- [ ] **Not started:** player mining/refining/trading/upgrades/rewards/persistence acceptance; initial drops alone do not prove these.

A relevant completed initializer change may be checked with root-coordinated `./scripts/compatibility/run-arena-probe.ps1 -Build`, which builds/runs a hidden owned helper with a 30-second bound. Documentation reconciliation runs no probe or simulation.
