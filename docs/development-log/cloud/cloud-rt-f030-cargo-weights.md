Review status: APPROVED (PR #22 review round 1, senior review 5545872002 on head 4c298df; label review-approved)

# cloud/rt-f030-cargo-weights — RT-F030 cargo weight data audit

-   RT-ID: RT-F030 (gate items 2/7). Base: origin/main, not stacked. Status: done for audit scope; RT-F030 stays UNCHECKED.

## Done (`python scripts/audit-cargo-weights.py <assets>` -> `CARGO_WEIGHTS_OK 12 hulls 7 resources ...`)

-   CFG/rogue.cfg [Soul] WeightNormalMax=25000, WeightStopMax=40000 (the same in all 31 CFGs).
-   VEH/Rogue.veh 12 hulls: f[47] NormalWeight = 1500000 for every hull; f[48] StopWeight is 2500000 (Asp, Hornet, Mastakith, Rak'Kir, Rip'Pir, Tarthist), 3000000 (Skithist, Zag'Gir, Excelsior) or 4000000 (Kragath, Zap'Pir, Ravager).
-   ITM/Rogue_mp.itm item weight f[7] of the 7 resources: Tellerium 50000, Refined Tellerium 25000, Krandazite 100000, Refined Krandazite 50000, Wreckage 200000, Iron Ore 100000, Alloy 75000.

## Remaining

-   Per-hull thresholds are 1500000/2500000+ while CFG is 25000/40000: scale relation (x60 for 1500000/25000, x62.5 for 2500000/40000) and the load/penalty formula, stacking, ammo/equipment weights, native verification are all unresolved. No C# added (no .NET SDK in sandbox): not verified in cloud - local CI required.

## Resume point

Typed per-hull weight accessor + item weight accessor in OriginalData with oracle tests; then derive the formula from NWS rows 91-99 and native evidence.
