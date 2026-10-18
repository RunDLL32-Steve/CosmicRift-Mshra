# Provisional gameplay values

Master references: [§5 physics](../../ROGUE_TRADER_2_PLAN.md#5-spacecraft-movement-and-physics), §6 ship definitions, §7 combat, §8 resources, and §18 M0/M1. Status: [balance subplan](IMPLEMENTATION_PLAN.md) and [development progress](../development-log/PROGRESS.md). Reconciled: October 13, 2026. Canonical acceptance IDs: [original feature catalog](../original-research/FEATURE_CATALOG.md). Prototype ledger status: **Complete with evidence**; native original interpretation remains **Investigating** in the local subplan.

Every gameplay tuning value used by the First Contact engineering fixture is **NEW / provisional**. Original decoded pixel dimensions, palette metadata and raw content values retain their original provenance rather than this tuning classification. The [canonical original asset manifest](../original-research/ASSET_MANIFEST.json) and its preserved definitions are authoritative original-content data for the reproduction. Raw original numbers are available; remaining validation concerns their engine units, sentinel meanings, timing and evaluator behavior. The [historical matrix](../original-research/MECHANICS.md) establishes evidence independently; it does not turn prototype numbers into original Cosmic Rift statistics.

## Master example, not runtime authority

The master §6.1 JSON is an illustrative starter definition. These values are recorded to preserve its baseline before implementation tuning; actual shipped values must be reconciled from the external data files.

| Parameter | Master example / target | Classification | Intended unit / meaning |
|---|---:|---|---|
| Simulation rate | 60 | NEW / provisional | Fixed simulation ticks per second, per §5.3. |
| Ship mass | 100 | NEW / provisional | Game mass units. |
| Forward thrust | 900 | NEW / provisional | Game mass × world units / second². |
| Reverse thrust | 900 | NEW / provisional | Same units; equal thrust is a design interpretation of a COMMUNITY speed recollection. |
| Rotation speed | 3.8 | NEW / provisional | Radians per second; positive-turn convention belongs to simulation docs. |
| Maximum speed | 350 | NEW / provisional | World units per second. |
| Maximum hull | 100 | NEW / provisional | Damage capacity units. |
| Maximum shield | 50 | NEW / provisional | Shield damage capacity units. |
| Cargo capacity | 20 | NEW / provisional | Capacity units; mass/stack policy deferred to M2. |
| Primary slots | 1 | NEW / provisional | Installation limit example; full loadouts M2. |
| Secondary slots | 1 | NEW / provisional | Installation limit example; full loadouts M2. |
| Utility slots | 2 | NEW / provisional | Installation limit example; full loadouts M2. |

At the illustrated thrust/mass ratio, acceleration is 9 world units per second². That result is a consequence of the NEW equations; it is not measured historical handling. Prototype data may choose faster acceleration for immediate feedback.

## Runtime reconciliation

Actual JSON definitions were inspected on October 12, 2026. The values below are all **NEW/provisional**, including values different from the illustrative master example. Source files, not this ledger, are runtime authority. One world unit is treated as one game meter by the simulation design; rendering scale is a separate presentation choice, not recovered original scale.

| Runtime parameter | Implemented value | Source / unit |
|---|---:|---|
| Fixed tick | 60 | [Simulation](../../server/RogueTrader.Simulation); ticks/second |
| Ship mass | 100 | [starter_interceptor.json](../../data/ships/starter_interceptor.json); game kilograms |
| Forward / reverse thrust | 12000 / 12000 | Same ship; game newtons, producing 120 world units/second² |
| Rotation rate | 3.3 | Same ship; radians/second |
| Speed cap | 360 | Same ship; world units/second |
| Ship collision radius | 14 | Same ship; world units |
| Asteroid restitution | 0.25 | Same ship; dimensionless |
| Hull / shield maxima | 100 / 50 | Same ship; damage units |
| Reactor capacity / regeneration | 100 / 20 | Same ship; energy units and energy units/second |
| Projectile muzzle speed | 700 | [plasma_cannon.json](../../data/weapons/plasma_cannon.json); world units/second |
| Projectile radius | 3 | Same weapon; world units |
| Projectile damage | 28 | Same weapon; damage units |
| Cooldown | 0.2 | Same weapon; seconds, equivalent to 12 fixed ticks |
| Shot energy cost | 6 | Same weapon; energy units |
| Projectile lifetime | 2.5 | Same weapon; seconds, equivalent to 150 fixed ticks |
| Inherit ship velocity | true | Same weapon; NEW boolean rule with historical qualitative inspiration |
| Player spawn | (0, 0), heading −π/2 | [first_contact.json](../../data/sectors/first_contact.json); world units/radians |
| Static targets | 3; radius 20, hull 100, shield 40 | Same sector; target alpha (0, −280), beta (340, −440), gamma (−340, 300) |
| Asteroid environment | 12 circles; radii 42–88 | Same sector; full exact positions/radii in JSON |

Collision design uses swept circles for projectile contacts and deterministic asteroid separation with a 0.001-world-unit margin, confirmed in [CircleCollision.cs](../../server/RogueTrader.Simulation/CircleCollision.cs). Collisions cause no hull damage. Cargo slots/capacity from the master's illustration are not included in the implemented M1 ship JSON.

The Godot renderer uses approximately one screen pixel per world unit at its normal view, with a follow camera. This is a NEW presentation scale. The [supplied video parity checklist](../original-research/VIDEO_REFERENCE.md) tracks original presentation fidelity separately.

## Additional engine fixtures — source audit October 17, 2026

`data/ai/pirate_ai.json` is also NEW/provisional tuning; [its plan](../../data/ai/IMPLEMENTATION_PLAN.md) describes units and health thresholds. `PirateAiConfig` loads it for the standalone controller/tests; GameClient does not construct a controller. Generic cargo/pickup and station tests supply NEW capacities, pile quantities/radii, dock radii and prices through their own fixtures. `StationPriceTable.FromOriginalItems` offers a provisional raw ITM price binding (positive raw fields traded, zero and negative fields omitted); this is not a validated original pricing or eligibility rule. `OriginalProgressionIds` uses a NEW decimal-string profile-id contract; it does not recover historical storage. Exact source references and limits are in the [source audit](../audits/2026-10-17-source-docs.md). No new tuning or gameplay verification was performed.

## Accepted prototype verification scope

- Release thrust after acceleration: speed should persist in open space.
- Turn while drifting: velocity direction should remain independent of heading.
- Apply reverse thrust: first brake an aligned drift, then accelerate in reverse.
- Fire while drifting sideways: inherited projectile velocity should include lateral motion where enabled.
- Test disabled inheritance: muzzle velocity should be independent of shooter velocity.
- Navigate asteroid gaps and inspect collision stability; adjust radius/restitution as NEW tuning.
- Shoot a target until shields and hull are depleted; document constant damage/cooldown and attribution.
- Prior light/heavy NEW unit fixtures verify the provisional model; compare completed original playable hulls separately under RT-F001–RT-F012 / RT-F027–RT-F030.

Automated results establish consistency, not good game feel. A real launch/playtest establishes usability, not historical accuracy. Keep the two evidence types distinct in [progress](../development-log/PROGRESS.md).

Final validation reported by the lead agent on October 12, 2026: 41 unit cases passed, six headless world-acceptance phases passed and three input/UI acceptance phases passed. The standalone exported Windows executable launched, rendered and passed smoke checks without requiring a separately installed SDK/runtime. Coverage includes repeatable input replay, a 30/60/144 FPS fixed-step harness, inertia, reverse, light/heavy test fixtures, collisions, projectile tunneling and combat. See the [simulation subplan](../../server/RogueTrader.Simulation/IMPLEMENTATION_PLAN.md), [unit tests](../../tests/Unit) and [project progress](../development-log/PROGRESS.md) for final commands/artifacts. These checks complete prototype consistency/launch evidence; original asset/engine parity remains open.

Original resource prices, death losses, reward formulas, refinery rates and upgrades remain unfinished pending native interpretation and their assigned acceptance; M1 engineering completion does not adopt invented tuning for them. The archive now directly documents Krandazite, Tellerium, Iron Ore and Wreckage, plus selected raw historic config/item fields; see [INFANTRYONLINE.md](../original-research/INFANTRYONLINE.md). Those recorded snapshot values remain separate from the NEW prototype tuning. Version, units and evaluator/sentinel behavior need validation before adoption. No unverified bounty formula is adopted.


## Current original-value integration checkpoint

RT-F381 verifies19,198 supported literal original item assignments, all200 item/30 vehicle/12 skill/81LIO identities and twelve hull links. RT-F385 restores eight omitted VehicleMaker field18 values in derived parser code; all other item properties and five other exports remain identical. Original content is never rebalanced. [Package audit](../architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) documents exact evidence. Original arena/bounded-host acceptance does not validate native player formulas, and standalone hosting currently has no progression saves. Continue per-feature native semantics and safe economy verification in [canonical queues](../../data/original/IMPLEMENTATION_PLAN.md); this NEW fixture ledger remains unchanged.
