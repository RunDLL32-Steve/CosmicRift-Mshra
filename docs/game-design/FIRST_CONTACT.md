# First Contact implementation scope

Authority: [master §19](../../ROGUE_TRADER_2_PLAN.md#19-first-playable-build--version-01), §18 M0/M1, and §§5–8. Implementation status: [local subplan](IMPLEMENTATION_PLAN.md); acceptance evidence: [project progress](../development-log/PROGRESS.md). Reconciled: October 13, 2026. Canonical acceptance IDs: [original feature catalog](../original-research/FEATURE_CATALOG.md). M0/M1 engineering status: **Complete with evidence** in the cited progress record; no new acceptance run occurred for this documentation pass.

The accepted M1 engineering deliverable is a Windows client that launches independently of the editor, renders an original placeholder ship, simulates inertia and thrust, lets the player fire at destructible targets, and presents an asteroid field with collision boundaries. The shared C# simulation owns gameplay state; Godot displays it and supplies inputs.

The user's later steering sets the destination as a **100% Rogue Trader reproduction**, using [fowlmouth/cosmic-rift-assets as the canonical foundation](../original-research/COSMIC_RIFT_ASSETS.md) with **original files unchanged**, supported by InfantryOnline schema research and the supplied video/map. This placeholder deliverable is an engineering foundation. The [canonical manifest](../original-research/ASSET_MANIFEST.json), [recovered mode sources](../original-research/INFANTRYONLINE.md) and [limited video observation](../original-research/VIDEO_REFERENCE.md) guide fidelity work; the placeholder ship, sector and HUD do not yet satisfy the reproduction target.

## Historical and new design choices

| Decision | Evidence boundary | Why it belongs in the first slice |
|---|---|---|
| Independent orientation and velocity | COMMUNITY-inspired momentum behavior; exact integrator NEW | Turning should not redirect drift; flight feel is the critical system. |
| Forward/reverse thrust | Generic controls VERIFIED, equal reverse capability COMMUNITY | Allows braking, retreat and lateral combat. |
| Ship velocity inheritance | VERIFIED qualitative cannon/bomb distinction in preserved RT help; weighting/boolean prototype NEW | Preserves the proposed movement/combat relationship and makes it testable. |
| One Interceptor | NEW ship | Minimizes art/content work while the pilot loop becomes runnable. |
| Plasma projectile and target | NEW baseline/damage tuning | Gives collision and destruction a visible, reproducible outcome. |
| Circular asteroid boundaries | COMMUNITY navigation inspiration; geometry/response NEW | Creates maneuvering constraints and validates collision stability. |
| Hull/shield and debug HUD | Broad shield protection VERIFIED in RT help; overflow/tuning/UI representation NEW | Exposes damage, speed and controls for tuning. |
| Modern bindings | NEW preset from master §5.4 | Familiar defaults; original RT bindings/modifiers remain the mandatory unfinished fidelity target (RT-F026). |

The [historical matrix](../original-research/MECHANICS.md) is the source of historical classifications; the [balance ledger](../balance/PROVISIONAL_VALUES.md) is the source of numeric classifications. Other modern remakes are comparative references only.

## Acceptance sequence

1. **M0 gate:** solution/project structure, compatible pinned Godot/.NET, build workflow, conventions, and research/provisional-value documentation. Record external-tool limitations explicitly.
2. **Slice A/B:** launch/render, capture controls, run a fixed simulation tick, accelerate, reverse, rotate and drift. Include meaningful fixed-step tests.
3. **Slice C:** directional firing, configurable inheritance, cooldown, swept or otherwise reliable collision, shield/hull damage and target destruction. Include combat tests.
4. **Slice D:** asteroid field, collision response, camera tracking and readable debug HUD. Inspect repeated contacts and a navigable gap.
5. **Slice E:** compile, run automated checks, create a Windows export, and launch the executable without the editor. Record the exact artifact and observed controls/outcomes before closing M1.

A build recipe is not a runnable-build observation. A successful managed compilation is not a successful Windows game launch. M0/M1 engineering acceptance is already complete; the original fidelity gate remains open independently.

## M1 playtest script

| Action | Observable pass condition | Master mapping |
|---|---|---|
| Start the exported Windows executable | Ship and sector visible, inputs responsive, no development editor needed | M1 launchable client / acceptance |
| Accelerate then release thrust | Ship continues drifting; HUD shows persistent speed | M1 forward/momentum; §5.5 |
| Rotate during drift | Heading changes while travel direction remains unchanged in free space | M1 rotation; §20.1 |
| Reverse thrust | Ship brakes/changes velocity opposite its facing direction | M1 reverse; §5.5 |
| Turn and fire during lateral drift | Projectiles travel according to configured inheritance | M1 firing; §7.2 |
| Move through a gap and hit an asteroid | Passage possible and collision resolves without trapping/jitter | M1 asteroids/collisions; §5.5 |
| Fire repeatedly at a target | Shield depletes before hull under NEW prototype rules; target is destroyed | M1 collisions/damage/combat |
| Compare automated outputs | Fixed-step inputs reproduce the expected state within documented tolerance | M1 movement/combat tests; §20.1 |

## Scope after the first slice

M2 targets the original resource generation/pickup → cargo → station sale/refining → upgrade → save/load loop, all twelve original RPG hulls and resolved original NPC/reward systems, after their dependencies validate. M3 still requires authoritative multiplayer and persistence acceptance; current native server mode must be evaluated before committing to new infrastructure. Resource outposts, bounty tuning, faction warfare and sector expansion stay with their assigned master milestones. Acceptance gates determine when that work starts.

The preserved RT catalog directly identifies Krandazite, Tellerium, Iron Ore and Wreckage, original hulls, and equipment definitions. Master §1.2 prioritizes these originals over NEW placeholder content and keeps new expansions after historical parity. The canonical snapshot is selected; validate units/sentinels and contradictory dated behavior before applying archived tuning. Decode/map originals into separate derived rendering data without altering source files, and validate full asset/UI/map parity. Record exact-file rights/notices as release provenance.

Prototype geometric artwork remains a NEW engineering fixture. Local original-content inspection uses separately derived resources with canonical provenance; exact-file licenses/notices and release redistribution status remain part of the release gate. Original source bytes are never changed.


## Current original-content checkpoint

The updated OriginalContent standalone package and inspector input/render checks are complete (RT-F373/F374); accepted original overview/import/display capabilities remain separate from this NEW training world. Generic InfantryOnline handlers, original-data arena initialization and bounded full-asset backend have scoped acceptance. Native Start reached no confirmed arena entry, and standalone progression is not persisted. Next is private native entry/delivery and original per-feature gameplay verification when the user is available. [Current handoff](../original-research/HANDOFF.md) and [native runtime plan](../architecture/ROGUE_RUNTIME_PLAN.md) supersede earlier proposed implementation routes while retaining M0/M1 engineering history.
