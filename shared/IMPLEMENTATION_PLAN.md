# Shared modules implementation plan

Source-audited October 17, 2026; documentation/source inspection only, no tests rerun. Master: [§17 repository boundaries](../ROGUE_TRADER_2_PLAN.md#17-repository-structure), [§13 architecture](../ROGUE_TRADER_2_PLAN.md#13-multiplayer-architecture), [M1 fidelity/M2/M3](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype).

## Current modules

| Folder | Accomplished | Next and parity limit |
|---|---|---|
| [RogueTrader.GameData](RogueTrader.GameData/IMPLEMENTATION_PLAN.md) | .NET 8 NEW/provisional definitions, strict external JSON and finite/overflow validation; historical 41-test acceptance. | Investigating native semantics; canonical runtime integration remains open. Current schema is not an original catalog conversion. |
| [RogueTrader.OriginalData](RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md) | Raw hull/ITM/VEH/CFG loaders, integrity guards and data-only field/reference views; see the child plan and per-item records. | Native units, evaluator execution and gameplay adapters remain open; parsed loadout and skill-logic records already exist. |
| [RogueTrader.Protocol](RogueTrader.Protocol/IMPLEMENTATION_PLAN.md) | NEW/provisional pure C# message model and bounded binary codec ([MessageCodec.cs](RogueTrader.Protocol/MessageCodec.cs), [C5 record](RogueTrader.Protocol/plan.d/c5.md)). | No transport, authenticated session, network wiring or M3 acceptance. |

Original records come from the [pinned source manifest](../docs/original-research/ASSET_MANIFEST.json), with [per-domain queues](../data/original/IMPLEMENTATION_PLAN.md) and [feature acceptance](../docs/original-research/FEATURE_CATALOG.md). Native parser/package/arena/loopback results are separate compatibility evidence; they do not add original runtime adapters to this source tree.

## Active outstanding work

- [ ] **Investigating:** original units, evaluator/sentinel/default rules and production engine route.
- [ ] **In progress (data/binding only):** raw original contracts and an optional progression-id seam exist; gameplay adapters remain open where the selected engine needs them. Retain raw fields/version/hash/categories.
- [ ] **Not started:** exact per-feature behavior and M2/M3 acceptance after implementation.

Completed prototype evidence remains in child completion histories and the master. Do not run baseline simulations for shared documentation changes.
