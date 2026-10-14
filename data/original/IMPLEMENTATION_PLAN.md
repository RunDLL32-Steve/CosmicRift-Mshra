# Canonical original content queues

Authority: [master §1.2, §§6–10 and §18 fidelity gate/M2](../../ROGUE_TRADER_2_PLAN.md), [feature catalog](../../docs/original-research/FEATURE_CATALOG.md), [progress](../../docs/development-log/PROGRESS.md) and [CHANGELOG.md](../../CHANGELOG.md). Reconciled October 13, 2026.

## Purpose and current stage

These folders track exact original content acceptance, not replacement JSON or rebalanced content. Canonical source is unchanged [fowlmouth/cosmic-rift-assets](https://github.com/fowlmouth/cosmic-rift-assets/tree/eb1514eaae7f9b9425307d9cc043c74ecdd15e35), pinned at `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`. Raw fields, names, categories and source IDs remain authoritative. Stage: **Investigating native integration; original per-record runtime acceptance remains open**.

## Accomplished

- Raw and compiled-parser inventory: twelve selectable RPG hulls, thirty vehicles, 200 items, seven resource records and 81 level-object records.
- Selected eleven-archive pixel importer, source/standalone inspector and canonical overview are verified scoped capabilities. These do not complete playable hulls or native presentation.
- All 89 canonical BLO directories/1,262 members and 609 dependency candidates are inventoried (RT-F375). Eight missing/unknown raw candidates remain; full runtime dependency closure RT-F069 is unchecked.
- Package/link audit RT-F381, eight-field derived parser fix RT-F385, actual arena initialization RT-F383, bounded host RT-F392 and authentic supplemental Soccer startup dependency RT-F394 are complete within their documented scope. No native arena entry or original gameplay loop is verified.

## Domain plans and next acceptance

| Folder | Accomplished scope | Next original acceptance |
|---|---|---|
| [ships](ships/IMPLEMENTATION_PLAN.md) | Twelve identity links and Small/Tiny sprite inspection | Native ship choice, loadout, movement/energy/cargo and same-race exchanges |
| [weapons](weapons/IMPLEMENTATION_PLAN.md) | Original item parsing and bounded value/link audit | Native firing, inherited speed/sentinels, effects, ammo/costs and child chains |
| [equipment](equipment/IMPLEMENTATION_PLAN.md) | Original categories and parser values retained; VehicleMaker omission fixed | Installation/modifier/warp/build/repair rules; shop safety |
| [resources](resources/IMPLEMENTATION_PLAN.md) | Seven resource identities; original arena generates initial drops | Actual mining/pickup/refining/trading/cargo/loss conservation |
| [sectors](sectors/IMPLEMENTATION_PLAN.md) | Exact overview, tile layers/map references and initial arena state | Full raw LVL delivery, native world/grid/collision and each LIO interaction |
| [actors](actors/IMPLEMENTATION_PLAN.md) | Eighteen nonplayer/spectator records parsed; 25 computers initialized | Original actor production/ownership/combat, hazards and spectator transitions |

Update the current stage at Investigating, In progress, Testing and Complete. Remove only completed acceptance tasks from active queues, retain the exact ID/evidence in history and changelog, and synchronize master/catalog. Generic server handlers being present is not a verified original feature. Use focused parser/hash/image/build checks for data; simulation only follows completed gameplay behavior requiring it. Native desktop interaction remains stopped until the user is available.
