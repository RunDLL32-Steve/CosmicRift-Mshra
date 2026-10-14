# Unit test implementation plan

Status authority: [master M1 engineering acceptance/fidelity gate](../../ROGUE_TRADER_2_PLAN.md#m0m1-completion-evidence--engineering-prototype), [§20.1 physics](../../ROGUE_TRADER_2_PLAN.md#201-physics-tests), [§20.2 economy](../../ROGUE_TRADER_2_PLAN.md#202-economy-tests), and [M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay). Reconcile with [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [field interpretation](../../docs/original-research/FIELD_INTERPRETATION.md), and [project progress](../../docs/development-log/PROGRESS.md). Reviewed October 13, 2026; read-only test/result inspection, no reruns.

## Completion history — prototype engineering

| Feature | Current status / evidence |
|---|---|
| Pinned test infrastructure | **Complete with evidence** — [project](RogueTrader.UnitTests.csproj): .NET 8, xUnit 2.9.3, runner 2.8.2, Test SDK 17.12.0. |
| Fixed-step replay/movement/collision regression | **Complete with evidence** — [MovementTests.cs](MovementTests.cs): identical inputs, 30/60/144 FPS, drift/independent rotation, reverse/braking/caps, light/heavy mass fixtures, stable contact/overlap/tunneling and invalid input/time. |
| Projectile/energy/damage regression | **Complete with evidence** — [CombatTests.cs](CombatTests.cs): optional inheritance, cooldown, regeneration/cost/exhaustion/lifetime, shield/hull/attribution, one destruction, nearest impact and asteroid blocking. |
| External JSON regression | **Complete with evidence** — [GameDataTests.cs](GameDataTests.cs): NEW classification, real JSON, invalid mass/duplicate IDs/unknown fields/safe placement; eight overflow regressions cover tiny finite mass and squared magnitudes. |
| Prior executed acceptance | **Complete with evidence** — [unit.trx](../../artifacts/test-results/unit.trx), October 12, 2026: 41 executed/passed, zero failed/not executed; [dated acceptance](../../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation). SDK 8.0.419. Source/export smoke and rendered launch remain distinct client evidence. |

The test data copies the same provisional JSON consumed by the client. Heavy fixtures do not add original playable ships. No original parity, M2 economy, AI or M3 network tests are claimed complete.

## Outstanding feature verification

- [ ] **Investigating** — define original expected results for RT-F027–RT-F041/RT-F075 after native units, reverse/inheritance/lifetime sentinels, shield/armor ordering and bank/state selection are resolved. Prototype expectations are not original acceptance oracles.
- [ ] **Not started** — focused original runtime adapter cases for RT-F001–RT-F019/RT-F033–RT-F035/RT-F067/RT-F069: version/identity/category/raw preservation, grammar and unsupported semantics, after adapter implementation.
- [ ] **Not started** — original gameplay cases for completed features RT-F042–RT-F051/RT-F054/RT-F068: quantities, cargo, recipes, integer prices, duplicate requests, upgrade/repair/reward/retention behavior. Use original records and selected revision, not invented replacements.
- [ ] **Not started** — relevant critical regressions and exact master/catalog reconciliation when each feature is implemented. Run simulation only where that completed gameplay feature needs it; parser/hash/bounds/image checks serve data/rendering work. Do not rerun the baseline solely for documentation.

Historical reproduction command from the root (when justified by implementation changes):

```powershell
. ./scripts/environment.ps1
.tools/dotnet/dotnet.exe test tests/Unit/RogueTrader.UnitTests.csproj --nologo --verbosity minimal
```
