# Original reproduction handoff

Authority: [master §1.2 and §18 fidelity/M2](../../ROGUE_TRADER_2_PLAN.md), [research plan](IMPLEMENTATION_PLAN.md), [feature catalog](FEATURE_CATALOG.md), [master projection](FEATURE_MASTER_CHECKLIST.md) and [progress](../development-log/PROGRESS.md). Current navigation updated October 17, 2026; dated evidence retains its original scope. Use [CONTINUATION.md](../../CONTINUATION.md) for the project-wide resume point.

Original source is unchanged fowlmouth commit `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`. Current feature totals and completion marks belong to [master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap) and its [checklist projection](FEATURE_MASTER_CHECKLIST.md); this handoff does not duplicate them. No native player entry has been verified in the recorded evidence, and full gameplay parity is not complete.

## Accomplished

- Raw catalog: 3,086 records; twelve matched RPG/VEH hulls, thirty vehicles, 200 items, seven resources and 81 LIO records. Compiled pinned parser accepts these originals and the 1999×1998 LVL.
- Selected eleven BLO archives: 231 sprites/36,516 frames, fourteen focused decoder/staging checks. Source and updated standalone inspector validate all twelve hull identities, 64 direction rows, eleven state columns and exact map overview; rendered/input acceptance passes at world tick0. RT-F020/F070–F073/F373/F374 are scoped completions, not native bank/blit/gameplay proof.
- All89 canonical BLO directories/1,262 members and609 candidates inventoried (RT-F375). Eight missing/unknown raw candidates remain; five help files are separately recovered from pinned Zone-Assets. Full runtime closure RT-F069 stays unchecked.
- Native package static inventory RT-F376, package audit RT-F381 (19,198 supported values/twelve hull links/222 map links), generic-handler source audit RT-F382, original arena initializer RT-F383, exactly eight derived VehicleMaker field18 corrections RT-F385, bounded full-asset backend RT-F392 and authentic supplemental Soccer startup dependency RT-F394 pass within their stated acceptance.
- Original arena initializes25 computers/100 mineral drops/51 teams/42 hides. Bounded host loads89 canonical BLOs plus authentic shared Soccer, listens on two loopbacks and exits0 after120seconds with maxclients0 and unchanged source hashes.

## Next

1. **Investigating — RT-F380/F384:** when the user is available, diagnose native welcome Start/no entry, verify private profile/channel identity, connect, select the original hull and unspectate. The prior desktop test stopped while the user was playing another game; owned processes/private test namespace were cleaned up.
2. **Investigating — RT-F069/F387:** prove actual original asset delivery, including all raw LVL bytes. The server parser leaves380,052 graphical trailer bytes unread; deliver the original1,290,994-byte file unchanged. Portal/logo/numeric Ball role and missing item66 eligibility remain open.
3. **Not started — canonical domain queues:** verify existing engine handlers for original flight, weapons, mining/pickup/refining/trading/repair/upgrades/rewards and each actor/LIO feature. Generic handler presence does not complete gameplay. Preserve [field/evaluator contracts](FIELD_INTERPRETATION.md).
4. **Remaining native trading/join acceptance:** RT-F388/RT-F389/RT-F390/RT-F396 sale/store/capacity/purchase guards have bounded handler evidence; RT-F391 optional admin policy and RT-F397 outgoing lengths are also scoped completed work. Native readiness (RT-F393), actual trading and durable transactions remain open. See [RuntimeChecks](../../scripts/compatibility/RuntimeChecks/IMPLEMENTATION_PLAN.md) and [delivery checks](../../scripts/compatibility/NativeDeliveryChecks/IMPLEMENTATION_PLAN.md).
5. **RT-F068 integration remains open:** the tracked persistence core, original-ID binding and content validator exist; the [cloud branch log](../development-log/cloud/cloud-rt-f068-progression-contract.md) records a reconstructed original-ID save/load fixture and its verification limits. They are not wired into the native/M2 loop; standalone native mode still does not save progression.

Six per-record plans are indexed in [data/original](../../data/original/IMPLEMENTATION_PLAN.md). Update stage at Investigating/In progress/Testing/Complete; remove completed active tasks but preserve master checked audit, history and [CHANGELOG.md](../../CHANGELOG.md). Do not repeat passed baseline simulations during documentation/data work. Only a completed behavior needing simulation triggers that focused verification.
