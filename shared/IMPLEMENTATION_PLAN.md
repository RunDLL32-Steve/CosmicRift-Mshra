# Shared modules implementation plan

Reviewed October 13, 2026; documentation/source inspection only. Master: [§17 repository boundaries](../ROGUE_TRADER_2_PLAN.md#17-repository-structure), [§13 architecture](../ROGUE_TRADER_2_PLAN.md#13-multiplayer-architecture), [M1 fidelity/M2/M3](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype).

## Current modules

| Folder | Accomplished | Next and parity limit |
|---|---|---|
| [RogueTrader.GameData](RogueTrader.GameData/IMPLEMENTATION_PLAN.md) | .NET 8 NEW/provisional definitions, strict external JSON and finite/overflow validation; historical 41-test acceptance. | Investigating native semantics; canonical runtime adaptation Not started. Current schema is not an original catalog conversion. |
| [RogueTrader.OriginalData](RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md) | Raw loader for the twelve original RPG hulls linked to VEH Car rows, with SHA-256 integrity guard; 40 unit tests. | Interpretation (units, scaling, sentinels), ITM loadouts and runtime adapters Not started. |
| [RogueTrader.Protocol](RogueTrader.Protocol/IMPLEMENTATION_PLAN.md) | Reserved module plan only; no source project/wire code. | Investigating native route/authority boundary; M3 protocol work Not started and depends on engine decision. |

Original records come from the [pinned source manifest](../docs/original-research/ASSET_MANIFEST.json), with [per-domain queues](../data/original/IMPLEMENTATION_PLAN.md) and [feature acceptance](../docs/original-research/FEATURE_CATALOG.md). Native parser/package/arena/loopback results are separate compatibility evidence; they do not add original runtime adapters to this source tree.

## Active outstanding work

- [ ] **Investigating:** original units, evaluator/sentinel/default rules and production engine route.
- [ ] **Not started:** shared original adapters/contracts only where the selected engine needs them; retain raw fields/version/hash/categories.
- [ ] **Not started:** exact per-feature behavior and M2/M3 acceptance after implementation.

Completed prototype evidence remains in child completion histories and the master. Do not run baseline simulations for shared documentation changes.
