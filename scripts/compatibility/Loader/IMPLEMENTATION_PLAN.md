# Original data loader implementation plan

Purpose: invoke the actual pinned InfantryOnline Assets parser against unchanged Rogue Trader files and export structured diagnostics/raw metadata. Master authority: [§18 fidelity gate](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); parent [compatibility plan](../IMPLEMENTATION_PLAN.md). Reconciled October 13, 2026.

## Accomplished

The parser/runner engineering check is **complete for its bounded acceptance**. RT-F072 tracks derived RGB/RGBA export/staging, not this parser. [Program.cs](Program.cs) wraps errors into controlled exit codes, uses unique diagnostic CSV shards and field-only structural export to avoid by-ref CFG JSON serialization. Pinned source commit is bb97579efeceff1c63d781d8c1bb8dd4a5b9b849; isolated net10.0 runner uses SDK 10.0.401.

[Pristine report](../../../.tools/compatibility/reports/pristine-load-report.json) has failure count 0 and unchanged assets: CFG, ITM 200/v57, VEH 30/v53, RPG 12/v6, LIO 81/v23, LVL 1999×1998. Raw level metadata preserves 3,994,002 tiles and 116,065 nonzero physics masks. [Runner regression](../../../.tools/compatibility/reports/runner-error-regression.json) and [malformed exception regression](../../../.tools/compatibility/reports/malformed-exception-regression.json) establish controlled argument/malformed exits, not Windows crash dialogs. Initial diagnostic IOException/0xE0434352 is historical and likely correlated with the user's popup; the screenshot process was not independently identified.

RT-F385 derived VehicleMaker regression is **Complete**: eight values 0→1, all other 200-item properties and other exports unchanged. Fixed reports remain separate in `.tools/compatibility/reports/vehiclemaker-fixed`; pristine reports are retained.

## Next and parity

-   [ ] **Investigating:** version-specific unknown fields, sentinels, scale/evaluator semantics and sequential VEH consumption; accepted record load is narrower than exact behavior.
-   [ ] **Not started — RT-F387:** actual native packet/download/receiver acceptance; structured parser exports are not replacement native payloads.
-   [ ] **Not started:** original flight/combat/economy parity remains with runtime/domain plans.

Run from repository root only when a completed parser change requires checking: `./scripts/compatibility/run-data-loader.ps1 -Build`. This builds and executes a bounded hidden helper, writes captured logs/reports and verifies unchanged originals. No server sockets, native UI or world simulation are needed. Preserve separate report directories for future derived regressions.
