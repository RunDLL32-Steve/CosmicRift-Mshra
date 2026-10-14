# Gameplay design implementation plan

Master authority: [§1.2 original target](../../ROGUE_TRADER_2_PLAN.md#12-user-fidelity-clarification-and-implementation-tracking--october-12-2026), [§5.4 controls](../../ROGUE_TRADER_2_PLAN.md#54-default-controls), [§6 content](../../ROGUE_TRADER_2_PLAN.md#6-spacecraft-and-loadout-system), [§18 M1 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay) and [§19 First Contact](../../ROGUE_TRADER_2_PLAN.md#19-first-playable-build--version-01). Canonical acceptance IDs: [original feature catalog](../original-research/FEATURE_CATALOG.md). Reconciled October 13, 2026 with [progress](../development-log/PROGRESS.md). The master remains the status authority.

## Completion history and evidence

| Scoped feature | Current status | Evidence and limit |
|---|---|---|
| Define/implement First Contact engineering scope | Complete with evidence | [FIRST_CONTACT.md](FIRST_CONTACT.md) records NEW fixtures/slices A–E; master M0/M1 checkmarks preserve accepted engineering history. |
| Flight/combat/asteroid/camera/HUD/remappable modern input | Complete with evidence | Prior 41 tests, three native-input/UI and six world phases in source/export; standalone launch/render/controls captures in [progress](../development-log/PROGRESS.md#final-validation-evidence). Geometry/modern controls are prototype acceptance only; no new run occurred. |
| Document original-source precedence and fidelity boundary | Complete with evidence, documentation only | [Manifest](../original-research/ASSET_MANIFEST.json), [historical matrix](../original-research/MECHANICS.md), [field contracts](../original-research/FIELD_INTERPRETATION.md) and [balance ledger](../balance/PROVISIONAL_VALUES.md). Source presence does not finish original content features. |

## Active outstanding work

- [ ] **In progress — RT-F073:** client owner is implementing the paused original-content inspector. Verify exact identities/full 12-hull roster/all directions and poses/provenance rejection/zero gameplay ticks before completing the tool. It cannot complete playable RT-F001–RT-F012 hulls.
- [ ] **Investigating — RT-F020–RT-F024 / RT-F074–RT-F075:** verify original map overview/coordinates/world data, hull poses/effects and native palette/HSV/shadow/bank evaluators. Accepted selected decoded pixels are a narrower importer feature.
- [ ] **Investigating — RT-F025–RT-F026:** resolve RT HUD/store/selection art and actual action-token bindings/modifiers including strafe/hyper thrust. Original controls are mandatory; a modern preset may remain optional afterward. Generic command lists or Chaos Zone footage alone cannot validate RT scope.
- [ ] **Investigating — RT-F027–RT-F041:** interpret native movement, shield/energy, restrictions and per-weapon execution. Preserve raw units/sentinels; a provisional inheritance flag does not prove exact weapon behavior.
- [ ] **Not started — M2 original loop, RT-F013–RT-F019 / RT-F030 / RT-F033–RT-F035 / RT-F042–RT-F051 / RT-F068:** implement spawn/pickup, cargo, store/refining/trade/repair, equipment, rewards and persistence incrementally after dependencies validate. M1 engineering acceptance is already complete.
- [ ] **Not started — M2 actors, RT-F059–RT-F063:** implement resolved original NPC/hazard roles individually. An invented generic pirate does not complete the cataloged actors.

NEW ships/minerals/sectors, unrelated expansion and modern presentation remain deferred until exact reproduction. Detailed content acceptance remains in the canonical catalog and data/original plans. Keep unfinished features unchecked and complete only the specifically verified feature. Parser/hash/image/build checks cover data/presentation; gameplay simulation follows completion of the behavior needing verification.
