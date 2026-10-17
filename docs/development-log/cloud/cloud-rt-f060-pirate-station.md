Review status: APPROVED (senior review round 1, PR #38)

# cloud/rt-f060-pirate-station

- RT-IDs: RT-F060 (data only). Gate items 2/4/5. Stacked on cloud/rt-f058-stealable-structures (8a8be93; uses its `VehComputerFields`/`OriginalComputerRecord`). Claim commit b22ed1b.
- Scope: typed raw data view of VEH row 4 (id 403 PirateStation) with the ITM/VEH/RPG rows its fields name, the f[234] ownership expression parsed with the existing `OriginalSkillLogic`, and NWS lines 21, 51, 338 and 340. Data clause only; no behaviour.
- Field-name evidence: upstream-parser names (InfantryOnline bb97579e, via RT-F058's map), not original-executable evidence. Nothing is scaled, defaulted or corrected.

## Acceptance clauses
- Done (data): record identity (row 4, v53, 477 fields); raw combat fields (Hitpoints 300, IsControllable f[44] 1, RotateSpeed 1200, TrackingTime 50, TrackingRadius 2500, TrackingWeightHigh 10000000, FireRadius 2000, RepairRate 33, HitpointsRequiredToOperate 50, ObeyLos 1, Destroyable 1, ChainedTurretId -1, density/frequency limits -1); loadout (InventoryItems0 3002 = ITM row 73 BaseStationCannon, type 8 SpecialEffects; other 5 slots 0; ChildVehicles f[56..63] all 0); ExplodeItemId 3004 = ITM row 89 SpaceStationExplode; Drop 2008 = ITM row 87 Wreckage x1; sprites SpaceStations stationB_baseV1 / stationB_turretV1; all 16 product blocks default; NWS lines 21, 51, 338, 340 verbatim.
- Ownership logic result (raw, no meaning invented): f[234] `5|6|9|10|1|3|4|11|2|8|15|16` parses via `OriginalSkillLogic.Parse` as a `SkillLogicChain` of twelve plain skill ids joined only by `|`, round-trips exactly; the id set equals the set of the twelve RPG f[1] hull skill ids (5 Rak'Kir, 6 Rip'Pir, 9 Zap'Pir, 10 Zag'Gir, 1 Skithist, 3 Kragath, 4 Mastakith, 11 Tarthist, 2 Asp, 8 Hornet, 15 Excelsior, 16 Ravager). f[235] LogicStealOwnership is empty. What the expression gates natively is not established.
- Visible mismatches (kept raw): NWS 340 says Null/Pirates drop "a large amount of wreckage" but f[68]/f[69] = 2008 x1; VEH 401 Null Death Orb and 405 Null Kill Station carry the identical drop 2008 x1 and explode 3004, and the twelve player hulls also have drop 2008 x1 although NWS 338 says player ships drop "more than 1 wreckage", so the amount is not in these fields. ChildVehicles all 0 and products all default: the VEH row names no child ships and no production.
- Remaining (box stays OPEN): the controlling actor (f[44] is a raw flag only); LIO/Rogue.lio row 20 "Pirate Base" (raw special 6, actor -403, timed 48000/48000, warning text; queue G6 owns the LIO reader, not read here); spawn/respawn; team relationship; native drop amount; original presentation; verified completed NPC feature.

## Files
- shared/RogueTrader.OriginalData/OriginalPirateStation.cs
- shared/RogueTrader.OriginalData/plan.d/rt-f060.md
- tests/Unit/OriginalPirateStationTests.cs

## Results
- NuGet (api.nuget.org) and dot.net are blocked by the cloud egress policy, so xunit could not be restored. Tests ran with the xunit-compatible reflection shim (SDK 8.0.131 via temporary global.json edit, restored; pinned assets eb1514ea). Focused: `SHIM RESULT: passed 10, failed 0, skipped 0` (OriginalPirateStation filter). Full tests/Unit/Original* set with shim: `SHIM RESULT: passed 271, failed 0, skipped 5` (the 5 skips are pre-existing).
- Oracle values were read from the pinned VEH/ITM/RPG/NWS files before the assertions were written. One correction during testing: the raw fingerprint also holds `74/75/86/87=None` (animated-sprite blob names), which the first draft omitted.
- check-trackers: TRACKERS_CONSISTENT (no tracker counts changed; RT-F060 box not ticked).
- Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419, scripts/verify.ps1.

## Status
done (data clause); resume point: LIO row 20 once queue G6 lands, then native controller/spawn/team behaviour, which needs the native client/server (desktop).
