# Client scripts implementation plan

Reviewed October 13, 2026; source/log inspection only. Status authority: [master §5.3–5.4](../../ROGUE_TRADER_2_PLAN.md#53-physics-requirements), [M1 engineering/fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype) and [§18 original feature registry](../../ROGUE_TRADER_2_PLAN.md#original-feature-registry--october-14-checkpoint). Reconcile with the [client plan](../IMPLEMENTATION_PLAN.md) and [catalog](../../docs/original-research/FEATURE_CATALOG.md).

## Purpose and accomplished work

| Source | Current accomplishment |
|---|---|
| GameClient.cs | Thin startup/lifecycle/camera bridge; loads provisional JSON; fixed world calls; controls/inspector routing; controlled startup errors and bounded screenshot/QA modes. |
| FlightInput.cs | Eight default/remappable keyboard actions, conflict checks, saved controls and held-action release before rebinding. |
| WorldView.cs | Presentation only: canonical-derived Asp/Hornet if staged, geometric fallback, provisional range/rocks/projectiles/shield rings and visual bursts. |
| OriginalSpriteLibrary.cs | Read-only derived original manifest, resource path/commit/identity/layout checks, cached atlas/map textures and explicit frame bounds. Does not evaluate gameplay fields. |
| ClientInputSmokeTest.cs / ClientSmokeTest.cs | Historical prototype native-input/UI three-phase and world six-phase acceptance harnesses. |
| OriginalContentInputCheck.cs | Completed RT-F374 inspector-only native input/widget/frame regression; existing controls restored; world tick remains 0. |

Evidence: historical [M0/M1 acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation); current [source input record](../../artifacts/qa/original-input-source-record.json), [standalone input](../../artifacts/qa/OriginalContent/standalone-input.log) and [resource/render logs](../../artifacts/qa/OriginalContent/standalone-ships.log). RT-F020/F073/F373/F374 are narrow completed display/package/control scopes.

## Active outstanding work

- [ ] **Investigating — RT-F023/RT-F074/RT-F075:** native palette/blit/HSV/shadow, yaw rounding and banking/state/timing. DirectionRow and column 5 remain preview mappings.
- [ ] **Investigating — RT-F021/RT-F022:** native LVL/LIO origin, tile/object semantics and collisions before a playable original world.
- [ ] **Not started — canonical runtime adapter:** original hull/weapon/equipment/resource behavior after production engine choice and interpreted data contracts; existing prototype tuning stays NEW/provisional.
- [ ] **Not started — original gameplay input, effects/audio and HUD acceptance:** RT-F024–RT-F026; viewer controls do not complete original gameplay controls.

Keep authoritative state in the pure C# world, not Node2D. Run only checks required by an implemented feature; original input/resource checks do not require baseline gameplay simulation. Native engine execution belongs to the separate compatibility route and remains unverified beyond welcome.
