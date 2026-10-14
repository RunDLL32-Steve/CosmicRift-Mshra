# Simulation implementation plan

Status authority: [master §5 flight](../../ROGUE_TRADER_2_PLAN.md#5-spacecraft-movement-and-physics), [§7 combat](../../ROGUE_TRADER_2_PLAN.md#7-combat-system), [M1 engineering acceptance/fidelity gate](../../ROGUE_TRADER_2_PLAN.md#m0m1-completion-evidence--engineering-prototype), [M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), and [§20.1 physics QA](../../ROGUE_TRADER_2_PLAN.md#201-physics-tests). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; no simulation/build run for this review.

## Completion history — prototype engineering

| Feature | Current status / evidence |
|---|---|
| Independent .NET 8 authority and fixed 60 Hz stepping | **Complete with evidence** — [WorldSimulation.cs](WorldSimulation.cs), [FixedStepRunner.cs](FixedStepRunner.cs), read-only public [world state](WorldState.cs); prior replay/30/60/144 FPS cases in [MovementTests.cs](../../tests/Unit/MovementTests.cs). |
| Forward/reverse thrust, independent rotation, drift, mass-based acceleration and caps | **Complete with evidence** — prior movement tests, including light/heavy parameter fixtures; those fixtures are not additional playable hulls. |
| Static asteroid contacts and swept projectile combat | **Complete with evidence** — [CircleCollision.cs](CircleCollision.cs), [CombatTests.cs](../../tests/Unit/CombatTests.cs): contact recovery, tunneling, first impact, inheritance, energy/cooldown/lifetime, shield→hull spillover and owner/weapon attribution. |
| First Contact acceptance | **Complete with evidence** — prior [41 passing cases](../../artifacts/test-results/unit.trx); [dated acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation) and [progress evidence](../../docs/development-log/PROGRESS.md#final-validation-evidence). Client owns three native-input/UI and six world phases in source/export plus rendered standalone launch. |

The prototype uses explicit world units, seconds and radians from the [NEW/provisional ledger](../../docs/balance/PROVISIONAL_VALUES.md); positive rotation is clockwise. Each tick moves, regenerates energy, fires if eligible, then sweeps projectiles. Cooldown/lifetime round up to ticks; the firing tick counts toward lifetime. Asteroid collisions cause no hull damage, targets are stationary, and shield/energy pools are separate. Replay is verified on the pinned runtime; cross-platform bitwise equivalence is not promised. These engineering choices do not establish original behavior.

## Outstanding original behavior work

- [ ] **Investigating** — RT-F001–RT-F012/RT-F027: native per-hull mass, thrust, friction, yaw, speed, terrain and units. [GameData](../../shared/RogueTrader.GameData/IMPLEMENTATION_PLAN.md) preserves raw originals rather than substituting Interceptor tuning.
- [ ] **Investigating** — RT-F028/RT-F030: reverse `-1` fallback, strafe/hyper conditions/energy and cargo threshold/overload composition. Equal prototype reverse thrust and heavy fixtures do not complete them.
- [ ] **Investigating** — RT-F021/RT-F029: native origin/extent, tile physics/terrain, radius/bounce/point-blank scales and timing. [Pristine parser evidence](../../.tools/compatibility/reports/pristine-load-report.json) accepts 3,994,002 tiles and labels 116,065 nonzero physics entries `BlockedTiles`; nonzero physics is not yet final ship traversal/solidity. Trailing LVL graphical entities remain omitted by that parser.
- [ ] **Investigating** — RT-F031–RT-F032/RT-F041: energy-shield relation, recharge/overrides, armor/damage types and hit/death ordering; prototype separate pools/spillover remain provisional.
- [ ] **Investigating** — RT-F036–RT-F040: weighted inheritance and negative inheritance/lifetime sentinels, fire/expiry/muzzle scales, mines, stun/EMP and MIRV chains. Original weapon implementation is **Not started**; a boolean inheritance switch is insufficient.
- [ ] **Not started** — RT-F042–RT-F043: original resource generation/pickup and wreckage/drop lifecycle. Preserve LIO spawn/trap values; invented mineral asteroids do not replace original spawns.
- [ ] **Not started** — RT-F048/RT-F050–RT-F051/RT-F054: interpreted upgrades, class/spec/death transitions, rewards and warp actions after evaluator dependencies are validated.
- [ ] **Investigating** — RT-F061–RT-F063: original meteor/mold/spore/radiation dependencies and revision mismatches; behavior implementation is **Not started**.
- [ ] **Investigating** — RT-F075: native yaw/direction rounding and bank/pose state with the client. Preserving 64×11 frames does not establish state selection.
- [ ] **Not started** — per-feature original behavior acceptance and master/catalog reconciliation. Run simulation only once the specific gameplay feature is implemented and needs it; no baseline reruns for images/indexing/documentation.

Pinned InfantryOnline parser/bot formulas are interpretation evidence; its player-update path accepts client motion state. Native player physics and server/client compatibility remain **Investigating**, with no exact engine parity established. Persistence, AI and networking retain separate plans.

## Current reconciliation — October 13, 2026

Documentation-only source/saved-log review; no new build/test/GUI/simulation. Master registry: **394 IDs, 15 narrowly completed scopes, 379 open**. Completed prototype work above is history; original parity remains open unless its exact catalog acceptance passed.

Separate compatibility evidence has advanced beyond parser-only: RT-F381/F382 package/runtime-source audits, RT-F383 actual original arena initialization, RT-F385 exact eight-value parser regression, and RT-F392/F394 full-asset loopback host/authentic shared Soccer are complete. [Runtime plan](../../docs/architecture/ROGUE_RUNTIME_PLAN.md) and [package plan](../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) carry those proofs. This does not implement production services or tests in reserved folders.

The native client reached Welcome FreeInfantry; Start produced no confirmed entry. Native asset delivery, actual original gameplay and durable progression remain unverified. Standalone compatibility mode does not save progression. Owned test client/backend/private namespace were removed; no desktop interaction while the user is playing. [Native plan](../../docs/architecture/NATIVE_CLIENT_PLAN.md).

Next remains the unchecked per-feature queue in this document, using original fowlmouth records unchanged. Completed original viewer/package/input scopes RT-F020/F073/F373/F374 are historical proof, not native original appearance. RT-F375 all-archive inventory is complete; RT-F069 runtime dependency closure and Portal/logo/numeric Ball/item66 eligibility remain open. Original units/evaluators/bank/blit rules and shop/proximity/null-script bugs remain unresolved. NEW prototype tuning and modern expansion proposals cannot replace original parity.

