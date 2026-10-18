# Data implementation plan

Authority: [master §1.2, §§6–8 and §18 M0/M1/M2](../ROGUE_TRADER_2_PLAN.md), [feature catalog](../docs/original-research/FEATURE_CATALOG.md) and [progress](../docs/development-log/PROGRESS.md). Reconciled October 13, 2026.

## Purpose and current stage

`ships/`, `weapons/`, `sectors/` and `ai/` hold the accepted **NEW/provisional First Contact fixtures**. The Godot build stages these JSON files into `client/GameData/`; they remain the simulation's current training definitions. They are not original Rogue Trader values. The build copies all `data/**/*.json`; GameClient reads only the three ship/weapon/sector files. AI tuning is loaded by the standalone AI module/tests and is not connected to the client. [mining/](mining/IMPLEMENTATION_PLAN.md) holds an explicitly NEW test fixture, loaded only by mining tests; the client does not enable mining. [original/](original/IMPLEMENTATION_PLAN.md) holds record-specific parity queues; canonical source bytes stay outside runtime working outputs.

## Accomplished

- Accepted M0/M1 fixtures: one Interceptor, one plasma weapon, three targets and twelve static asteroids. JSON loads, NEW classifications, placement and target destruction passed the historical 41-unit-case and source/export acceptance.
- Read-only raw catalog: 3,086 records, twelve RPG/VEH hull matches, 200 items, seven Resources records and 81 LIO entries; exact names/IDs in [ORIGINAL_CATALOG.md](ORIGINAL_CATALOG.md).
- Pinned original parser accepts original CFG/ITM/VEH/RPG/LIO/LVL. Scoped package audit RT-F381 matches 19,198 literal item values, twelve hull links and 222 map archive/member references. Derived-only RT-F385 restores eight omitted VehicleMaker field18 values; originals and all other exported fields remain unchanged.
- Original-data arena initialization RT-F383 and bounded full-asset backend RT-F392 pass. Generic existing handlers remain candidates for runtime reuse; no native player entered the arena.

Evidence: [package audit plan](../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md), [runtime plan](../docs/architecture/ROGUE_RUNTIME_PLAN.md), [balance ledger](../docs/balance/PROVISIONAL_VALUES.md) and the master completion history. No verification was rerun for this documentation update.

## Next

- [ ] **Investigating — RT-F069/F380/F384:** validate native profile/connection and actual unchanged asset delivery before selecting the production engine.
- [ ] **Not started — M2, RT-F001–F019 and per-record queues:** verify original hulls, weapons, equipment, resources, actors and sector interactions through the chosen engine. Existing parser/initializer success does not complete gameplay acceptance.
- [ ] **Investigating — RT-F386 and broader economy:** item66 eligibility and original shop/transaction behavior remain open. The narrower compatibility sale, proximity, null-script and purchase safeguards RT-F388/F389/F390/F396 already have historical acceptance; do not repeat them as unfinished fixes.
- [ ] **Not started — RT-F068/M2 persistence:** demonstrate durable progression; the current standalone native host does not save it.

Use the specific [canonical domain plans](original/IMPLEMENTATION_PLAN.md). Keep fixture JSON until a validated original route replaces the relevant engineering scope; never overwrite canonical files or balance the originals to fit prototype equations. Completed tasks leave active queues but remain in master audit, completion history and [CHANGELOG.md](../CHANGELOG.md).
