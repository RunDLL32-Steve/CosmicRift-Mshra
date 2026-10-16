Review status: APPROVED (senior review round 1, PR #26, head c2ce2d9)

# cloud/rt-f031-shield-energy-fields — RT-F031 shield/energy/hull raw-field audit

- Claims: rt-f031 (data clauses only: gate items 2 and 6). Base: origin/main (not stacked). Box stays unchecked.
- Status: done for the data clauses; behavior clauses open.

## Scope and acceptance clauses
Done (data):
- Independent audit `scripts/audit-shield-energy.py <assets-root>` prints `SHIELD_ENERGY_OK` with hard asserts.
- Raw C# view `shared/RogueTrader.OriginalData/OriginalShieldEnergy.cs` (CFG keys fail loudly when missing; per-terrain rates with line numbers; per-hull HullPoints/EnergyMax/EnergyRate/EnergyMin as `OriginalInt`).
- Unit tests pin the audit values (asset tests skip without assets).
Remaining (open, unverified): native meaning of EnergyShieldMode=2, hit ordering, overflow/spillover, recharge, death, units/scales (hull 1700/1050/1700 vs [Soul] 1700000/250), terrain EnergyRate effect, damage-type protection, no separate hull regeneration without evidence.

## Audit key numbers (real output, assets eb1514ea)
- CFG rogue.cfg [Soul]: EnergyDefaultMax=1700000 (97), EnergyDefaultRate=250 (98), EnergyShieldMode=2 (100), EnergyDefaultStart=-1 (108).
- EnergyShieldMode: exactly one line (100) in each of the 31 CFG files, value 2 in all 31 (no variation).
- Terrains 0..15: EnergyRate nonzero only at Terrain9 (line 589), Terrain10 (626), Terrain15 (811), all -2000; HealthRate 0 and RepairRate 0 in all 16.
- 12 Car rows (VEH rows 18-29): f[6]=50, f[94..96]=1700,1050,1700 each; Asp is row 20.
- NWS/Rogue.nws rows 39, 40, 41, 131, 133 (132 blank; 133 continues on 134) matched verbatim; the only other "shield" hits are NWS lines in pz/smear (Power Zone Shield Flag paragraph, not cited).
- Other Shield/Energy/Health key-name matches in rogue.cfg ([Arena] Specs/Friendly/EnemySeeEnergy, SkillScreenEnergyNeeded, TeamSwitchMinEnergy, [Vehicle] EnergyOnDefaultChange, [Attribute] Health/Energy ratios, [UiartMetrics] gauges) are listed raw in the audit and not interpreted.

## Files
- scripts/audit-shield-energy.py (new)
- shared/RogueTrader.OriginalData/OriginalShieldEnergy.cs (new)
- tests/Unit/OriginalShieldEnergyTests.cs (new)
- shared/RogueTrader.OriginalData/plan.d/rt-f031.md (new)
- docs/original-research/FEATURE_CATALOG.md (rt-f031 row text only, no checkbox change)
- this log

## Verified in cloud
- `python3 scripts/audit-shield-energy.py /home/claude/assets/repo` -> `SHIELD_ENERGY_OK` (output recorded above).
- Shim harness (real xunit not available; NuGet blocked): with assets Passed 124, Failed 0, Skipped 0; without assets Passed 41, Failed 0, Skipped 55.
- `python3 scripts/check-trackers.py` -> TRACKERS_CONSISTENT.

## Not verified in cloud - local CI required
- `dotnet test tests/Unit` against real xunit 2.9.3 and the pinned SDK (cloud used a shim harness; the new tests use explicit types and `Assert.StartsWith(string, string)`).
- Windows `scripts/verify.ps1`.

## Resume point
Awaiting review. Next for RT-F031: behavior clauses (hit order, overflow, recharge, death, units) need native evidence; do not tick until verified.

## Coordinator review (this run)
- Renamed the raw CFG integer record to `OriginalCfgRawInt`: cloud/rt-f029-collision-fields (opened in parallel) declares a different `OriginalCfgInt` record in the same namespace, so the two branches would not compile together after both merge. Shim harness after the rename: with assets 124 passed / 0 failed / 0 skipped; without assets 41 passed / 55 skipped / 0 failed.
