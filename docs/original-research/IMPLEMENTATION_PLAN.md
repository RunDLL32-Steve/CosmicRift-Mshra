# Original research implementation plan

Authority: [master §1.2, §2, §§5–10 and §18 fidelity/M2](../../ROGUE_TRADER_2_PLAN.md), [feature catalog](FEATURE_CATALOG.md), [master projection](FEATURE_MASTER_CHECKLIST.md) and [progress](../development-log/PROGRESS.md). Reconciled October 13, 2026. Current stage: **Investigating native runtime parity**. Detailed handoff: [HANDOFF.md](HANDOFF.md).

## Accomplished and evidence

397 stable feature IDs are reconciled with22 scoped completions and375 open. Completed tasks are history, not the active queue:

-   Canonical provenance/raw index and compiled parser acceptance for CFG/200ITM/30VEH/12RPG/81LIO/1999×1998LVL; originals unchanged.
-   RT-F020/F070–F073: selected11 BLO indexing/decoding/export,231 sprites/36,516 frames,14 focused tests and source inspector/map overview with zero world ticks. [Importer plan](../../scripts/original-assets/IMPLEMENTATION_PLAN.md).
-   RT-F373/F374: updated standalone inspector packaging/input plus three rendered pages pass; native appearance remains separate. [Client plan](../../client/IMPLEMENTATION_PLAN.md).
-   RT-F375: all89 canonical archives/1,262 members/609 candidates inventoried, eight raw missing/unknown candidates; original99-file integrity verified. [Dependency plan](../../scripts/original-assets/DEPENDENCY_PLAN.md).
-   RT-F376: static native executable/package/channel inventory. Actual local entry remains unverified. [Native plan](../architecture/NATIVE_CLIENT_PLAN.md).
-   RT-F381/F385: bounded package audit19,198 values/twelve hull links/222 map links and eight-field derived-only parser correction. [Package audit](../architecture/ROGUE_PACKAGE_AUDIT_PLAN.md).
-   RT-F382/F383/F392/F394: generic runtime source audit, actual original-data arena initialization, bounded full-asset loopback host and authentic supplemental Soccer dependency. [Runtime plan](../architecture/ROGUE_RUNTIME_PLAN.md).
-   RT-F388/F390/F395: scoped sale validation, null-script capacity and diagnostic host radar safeguards;35/35 actual-handler checks and healthy10-second host. [October14 evidence](../development-log/2026-10-14.md). These do not complete original trading/native gameplay.

The native test reached welcome only. Start produced no confirmed arena entry; owned client/backend/private test registry namespace were cleaned up. No full original gameplay acceptance is claimed.

## Active outstanding queue

| Open acceptance                                                                                           | Current status                                                                     | Reference                                                                                                                                                                   |
| --------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [ ] Native channel/profile/Start/connection/hull selection/unspectating                                   | Investigating; desktop stopped until user available                                | RT-F380/F384, native plan                                                                                                                                                   |
| [ ] Actual unchanged asset delivery/full LVL graphics and runtime dependency eligibility                  | Investigating                                                                      | RT-F069/F021/F387; original graphical trailer stays intact                                                                                                                  |
| [ ] Portal/logo/numeric Ball and item66 references                                                        | Investigated; all four genuinely missing/unknown; runtime eligibility remains open | RT-F377–F379/F386; [MISSING_REFERENCES_FINDINGS.md](MISSING_REFERENCES_FINDINGS.md)                                                                                         |
| [ ] Native movement, reverse/strafe/hyper, energy, cargo, weapon scales/sentinels and event/skill grammar | Investigating                                                                      | F027–F041/F067; [FIELD_INTERPRETATION.md](FIELD_INTERPRETATION.md)                                                                                                          |
| [ ] Native palette/HSV/shadow/blit/bank/timing/HUD/audio and original help binding/menu behavior          | Investigating                                                                      | F023–F026/F074/F075/F368–F372                                                                                                                                               |
| [ ] Original mineral/refinery/trade/repair/upgrade/death/reward execution                                 | Not started for exact native acceptance                                            | F013–F019/F042–F051; dated source/emulator revisions remain distinct                                                                                                        |
| [ ] Connected native store/purchase behavior and durable transaction integration                          | Native acceptance open after bounded safeguards                                    | F389/F396 handler fixes are complete; [RuntimeChecks](../../scripts/compatibility/RuntimeChecks/IMPLEMENTATION_PLAN.md) records their scope                                 |
| [ ] Durable original progression integration and production shutdown                                      | Core/binding exist; native/M2 integration open                                     | F068/M2; [original-ID fixture evidence](../development-log/cloud/cloud-rt-f068-progression-contract.md), standalone has no saves, bounded exit is not upstream shutdown fix |
| [ ] Additional timestamped video/RT UI comparison and release provenance                                  | Not started / Investigating                                                        | [VIDEO_REFERENCE.md](VIDEO_REFERENCE.md), [MECHANICS.md](MECHANICS.md); only0:25 frame observed                                                                             |

## Domain ownership

The [six canonical queues](../../data/original/IMPLEMENTATION_PLAN.md) cover [ships](../../data/original/ships/IMPLEMENTATION_PLAN.md), [weapons](../../data/original/weapons/IMPLEMENTATION_PLAN.md), [equipment](../../data/original/equipment/IMPLEMENTATION_PLAN.md), [resources](../../data/original/resources/IMPLEMENTATION_PLAN.md), [sectors](../../data/original/sectors/IMPLEMENTATION_PLAN.md) and [actors](../../data/original/actors/IMPLEMENTATION_PLAN.md). Every original per-record gameplay checkbox remains open; parser/importer/initializer success is narrower evidence.

October13 documentation reconciliation is historical. October14 runtime safeguard implementation/acceptance is recorded above. Keep unchanged original names/categories/values, update stages with the master, and move completed tasks to history/[CHANGELOG.md](../../CHANGELOG.md). Focused parser/hash/image/build checks cover data; simulation only follows completed behavior needing it.
