Review status: APPROVED (senior review round 1, PR #37)

# cloud/rt-f059-null-npcs

- RT-IDs: RT-F059 (data only). Gate items 2/4/5. Stacked on cloud/rt-f058-stealable-structures (8a8be93; uses its VehComputerFields/OriginalComputerRecord and the `Fingerprint` test helper). Claim commit 1f7e8dc.
- Scope: typed raw view of the two Null NPC VEH type-5 rows (401 Null Death Orb, 405 Null Kill Station) beside the pinned NWS lines 190, 191, 268, 340, 367, with the item-id fields resolved against ITM/Rogue_mp.itm and a Null Wreckage audit.
- Field-name evidence: same as RT-F058 (upstream-parser names from InfantryOnline bb97579e read as interpretation only; not original-executable evidence).
- No LIO reader was written: queue G6 is claimed by another owner. LIO/Rogue.lio line 19 (`5,v23,47,"DeathOrbSpawn",...,-401,...,"Attention! A Null Death Orb has been detected! All pilots on alert!"`) and line 23 (`5,v23,52,"KillStationSpawn",...,-405,...,"Attention! A Null Kill Station has been detected! All pilots on alert!"`) were only grepped for the raw actor id and warning text and are not parsed or tested here.

## Acceptance clauses
Catalog acceptance: "Resolve actual actor logic, combat/loadout/faction, timed spawns/warnings and drops. Check whether Null Wreckage is an alternate/missing item in selected records, not ordinary Wreckage assumed."
- Done (data): raw combat/loadout fields of both rows pinned (Hitpoints 500, RotateSpeed 1500, TrackingTime 50, TrackingRadius 2500, TrackingWeightHigh 10000000, FireRadius 2000, RepairRate 33, HitpointsRequiredToOperate 50, ObeyLos 1, Destroyable 1, density/frequency limits -1, ChainedTurretId -1, RelativeId 0, empty ownership logic, sprite blob refs); InventoryItems0 1014 DeathOrbCannon / 1037 DeathRay, ExplodeItemId 3004 SpaceStationExplode, DropItemId 2008 Wreckage x1 resolved by ITM id; the five NWS lines pinned verbatim.
- Done (audit): no ITM row is named "Null Wreckage" (exact or case-insensitive) and none of the 200 item names contains "null"; the Null NPC drop is ordinary ITM 2008 Wreckage (type 4 Resources), identical to the drop of VEH 403, 410, 411, 413, 414.
- Visible mismatches (kept raw): NWS 268 "unique item: Null Wreckage" has no ITM row; NWS 340 "large amount of wreckage" versus raw DropItemQuantity 1; CFG Hold1 `wipeinv=null wreckage` (noted only, another branch handles event strings) refers to a name no ITM row has.
- Remaining (box stays OPEN): LIO 19/23 timed spawns and warnings (G6); whether NWS 268 "Null sats" means these two rows or GunSat/BeamSat; actor AI, firing and targeting behaviour; faction; runtime drop/claim behaviour of Null Wreckage (alternate item, server-side tag, or never implemented).

## Files
- shared/RogueTrader.OriginalData/OriginalNullNpcs.cs (`OriginalNullNpcs`, `OriginalNullNpc`, `OriginalItemRef`, `OriginalNullWreckageAudit`)
- shared/RogueTrader.OriginalData/plan.d/rt-f059.md
- tests/Unit/OriginalNullNpcsTests.cs
- docs/development-log/cloud/cloud-rt-f059-null-npcs.md

## Results
- NuGet and dot.net are blocked by the cloud egress policy, so tests ran through the xunit-compatible reflection shim (/tmp/claude-0/shim/run.sh, SDK 8.0.131 via a temporary global.json tweak restored afterwards; pinned assets eb1514ea): full run `SHIM RESULT: passed 270, failed 0, skipped 5` (261 from the F058 base + 9 new; the 5 skips are pre-existing); filter OriginalNullNpcs: passed 9, failed 0, skipped 0 (6 asset-backed, 3 pure synthetic).
- check-trackers: TRACKERS_CONSISTENT (no counts changed; box open).
- Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419, scripts/verify.ps1. Skipped as not applicable: runtime/native-delivery checks, simulations (no gameplay behaviour added).

## Status
done (data clause); wip for the feature. Resume point: after the LIO reader (G6) lands, add rows 19/23 (spawn timing, actor ids -401/-405, warning strings) to this view; actor behaviour/faction/drops need the native client/server.
