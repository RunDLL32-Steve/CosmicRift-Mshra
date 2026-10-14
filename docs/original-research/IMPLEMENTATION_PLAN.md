# Historical research implementation plan

Master: [ROGUE_TRADER_2_PLAN.md](../../ROGUE_TRADER_2_PLAN.md), §1.2,§2,§§5–10,§18 exact reproduction gate/M2–M3 and§22. Reconciled October13,2026. Status: [PROGRESS.md](../development-log/PROGRESS.md). Next steps: [HANDOFF.md](HANDOFF.md).

Completed work leaves this active queue and remains in history. [FEATURE_CATALOG.md](FEATURE_CATALOG.md) has372 stable IDs with exact sources/raw values/classifications/acceptance/owners; [FEATURE_MASTER_CHECKLIST.md](FEATURE_MASTER_CHECKLIST.md) projects status into the master. Completed narrow import/display tools do not complete native gameplay.

## Active work

| Unfinished acceptance | Status | Master / feature reference |
|---|---|---|
| [ ] Validate native movement, reverse/strafe/hyper, energy, damage and cargo scales | Investigation | §§5–8,F027–F041; [FIELD_INTERPRETATION.md](FIELD_INTERPRETATION.md) separates proven parser assignments from emulator/native hypotheses. |
| [ ] Resolve negative projectile/backward sentinels and skill/event operators | Investigation | F028,F034,F036–F040,F067; preserve raw values, prove evaluator meaning. |
| [ ] Reconcile bounty/economy/death/resale operational revision | Investigation | F043–F051; dated news and current emulator formulas differ. |
| [ ] Resolve full member/child/audio/map dependency closure | Investigation | F021,F024,F069; selected11 BLO decoding complete, native LVL graphics/collision and full89-BLO closure still open. |
| [ ] Validate native palette/HSV/shadow blits and bank choice | Investigation | F023,F074–F075; center-column/alpha display adapter remains reconstructed. |
| [ ] Validate original help/menu dependencies and native default key mapping | Investigation | F026,F368–F372; five exact ancillary files located in corroborating archive, tokens are not literal keys. |
| [ ] Review original gameplay footage using actual timestamped observations | Not started | §22F; only supplied Chaos Zone frame0:25 observed, not full video/RT economy. |
| [ ] Verify original persistence/save policy | Not started | F068,§14; canonical data does not establish historical backend ordering. |
| [ ] Maintain component-specific reuse/provenance evidence | Investigation | §1.2/§16; separate original media, emulator and tool notices. |
| [ ] Reconcile updated export verification after importer/client integration | Testing pending root | Current source inspector QA is complete; source acceptance does not prove updated exported artifact. |

Research runs no gameplay simulations. Use source/parser/hash/image checks; behavior simulation follows implementation of the specific feature requiring it.

## Scoped active queues

Every original record has its own feature ID, open acceptance and current runtime status. Keep source types/categories unchanged; verified Done entries leave active tables and retain ID/source/evidence in completion history. Catalog/master retain checked audit entries.

- [Ships/hull exchanges](../../data/original/ships/IMPLEMENTATION_PLAN.md):12 hulls plus12 exchange records.
- [Weapons/child effects/ammunition](../../data/original/weapons/IMPLEMENTATION_PLAN.md):125 records.
- [Equipment/utilities/internal effects](../../data/original/equipment/IMPLEMENTATION_PLAN.md):52 records; not all are upgrades.
- [Resources/credit effects](../../data/original/resources/IMPLEMENTATION_PLAN.md):seven materials plusfour distinct helpers.
- [Original sector/level objects](../../data/original/sectors/IMPLEMENTATION_PLAN.md):81LIO records, separate overview/native map/grid acceptance and ancillary help dependencies.
- [Computer actors/spectator](../../data/original/actors/IMPLEMENTATION_PLAN.md):17computer records plus nonplayable Spectator.

## Completion history

Research documentation complete: startup/master inspection; historical/COMMUNITY [MECHANICS.md](MECHANICS.md); [provisional ledger](../balance/PROVISIONAL_VALUES.md); [InfantryOnline](INFANTRYONLINE.md)/[canonical archive](COSMIC_RIFT_ASSETS.md) audits; [SOURCE_MANIFEST.json](SOURCE_MANIFEST.json)/[ASSET_MANIFEST.json](ASSET_MANIFEST.json); [video reference](VIDEO_REFERENCE.md)/exact map; full raw original-content index and pristine compiled-parser acceptance; [field contracts](FIELD_INTERPRETATION.md);372-entry catalog/projection; six record-specific queues.

Narrow implemented tooling F070–F072 complete: selected11 BLO indexing/CFSv4 decode,231objects/36,516frames and supported export/staging,14focused tests,13source hashes unchanged. [Importer plan](../../scripts/original-assets/IMPLEMENTATION_PLAN.md) records scope; native original blits/banks/dependency closure remain open.

Narrow source-client display F020/F073 complete:231textures validated, all12hulls/64directions/11columns, canonical overview1999×1998. Root inspected ships/map/sprites captures; each of the three pages recorded simulationTicks0. Evidence:`artifacts/qa/original-{ships,map,sprites}.log/png`; source build0warnings/errors. Updated exported-client verification is separate and pending root. These display checks do not complete hull physics, loadouts, native bank/blit/HUD or world collisions.

M0/M1 provisional engineering acceptance remains complete under the master. Major implemented features belong in [CHANGELOG.md](../../CHANGELOG.md) and dated development evidence. Update catalog, local plan, log and master together; discovery/partial implementation never completes a broader feature.
