Review status: UNREVIEWED

# cloud/rt-f032-energy-defaults — RT-F032 energy defaults and power-mod rows (evidence audit)

- RT-ID: RT-F032. Gate item 2 (field audit) / 6 (flight evidence). Base: origin/main (not stacked).
- Status: done for the audit scope; RT-F032 stays UNCHECKED (unit/recharge/composition semantics open).

## Done (evidence, `python scripts/audit-energy-defaults.py <assets>` -> `ENERGY_DEFAULTS_OK 12 hulls 6 mods`)
- CFG `[Soul]` `EnergyDefaultMax=1700000`, `EnergyDefaultRate=250` in `CFG/rogue.cfg`; the same pair is present in all 31 CFG files (no per-config variation).
- All 12 hull rows (VEH/Rogue.veh rows 18..29) carry f[94..96] = 1700, 1050, 1700 (identical for every hull).
- ITM/Rogue_mp.itm type-13 "Power Mods" are exactly six rows, all f[31]=1, f[32]=8: Turbo Capacitor row 26 (f[33]=150), Super Capacitor 27 (250), Turbocharger 28 (f[34]=100), Supercharger 29 (200), Advanced Moltor Core 91 (f[33]=100, f[34]=250, skill `5|6|9|10`), Hyper Capacitor 93 (f[33]=400, skill `9|3|16`). The catalog listed five; row 91 is newly found.

## Remaining (not done)
- Meaning of f[33]/f[34] (capacity vs recharge delta; additive vs multiplicative) and the relation of hull f[94] 1700 to CFG 1700000 (x1000 is a hypothesis only); recharge tick semantics; stacking; native verification.
- No C# loader added: no .NET SDK in this sandbox (download blocked) so nothing C# could be built or tested ("not verified in cloud - local CI required").

## Files
scripts/audit-energy-defaults.py, FEATURE_CATALOG.md row RT-F032 note, this log.

## Resume point
Add a typed `ItmPowerMod` accessor (f[33]/f[34]) in shared/RogueTrader.OriginalData with an oracle test of the six rows, then validate semantics against native evidence.
