Review status: UNREVIEWED

# cloud/rt-f062-solar-mold-spores

- RT-IDs: RT-F062 (data only). Gate items 2/4/5. Stacked on cloud/rt-f058-stealable-structures (8a8be93; uses its VehComputerFields/OriginalComputerRecord). Claimed by the claim commit b830690.
- Scope: typed raw view of VEH 409 Solar Mold and 414 Mold Spore with their NWS lines (278, 336, 337, 367) under the dated headings and the resolved ITM references. No LIO reader (queue G6 claimed elsewhere); LIO rows 80/81 stay remaining work.
- Field-name evidence: upstream-emulator parser names (Infantry-Online-Server bb97579e) via the F058 map; not original-executable evidence. Values below were read from the pinned VEH/Rogue.veh, ITM/Rogue_mp.itm and NWS/Rogue.nws (eb1514ea).

## Acceptance clauses
- Done (data): raw combat, spawn-limit and ownership fields of both rows pinned; all 16 product blocks of both rows are empty (Solar Mold has no product to create spores; no VEH/ITM field outside the id column equals 409 or 414); 414 references resolved in ITM (InventoryItems0 1088 MoldSporeBeam, ExplodeItemId 3004 SpaceStationExplode, DropItemId 2008 x1 Wreckage); NWS lines pinned with headings: 278 under 2/26/03 (heading line 264), 336 and 337 under 2/19/03 (line 334), 367 under 5/2/02 (line 365).
- Raw values: 409 Hitpoints 500, RepairRate 100, RelativeId f[137] 20, no weapon/inventory, all spawn-limit fields 0 (MaxTypeByPlayerRegardlessOfTeam -1); 414 Hitpoints 75, RepairRate 50, HitpointsRequiredToOperate 5, RelativeId 0, Explode 3004, Drop 2008 x1, DensityRadius 3500, DensityMaxType 4, FrequencyMaxType 8, FrequencyDensityMaxType 4, Active/Inactive -1, EnergyRate 500, EnergyMin 1000, TrackingRadius/FireRadius 2500, LogicTakeOwnership/LogicStealOwnership "!".
- Visible mismatches (kept raw, listed in `VisibleMismatches`): NWS 337 says 35 HP, VEH 414 Hitpoints is 75 (no VEH row is named Sprite); 409 holds no spawn limits or product to spawn spores; catalog "faction 20" is RelativeId on 409 only; NWS 367 is dated 5/2/02 (2002), outside the catalog's 2003; DropItemId 2008 is an ITM Wreckage but VEH 2008 is Zap'Pir (the item namespace is assumed from the upstream name); NWS 336 "prizes" vs Wreckage unproven.
- Remaining (box stays OPEN): LIO rows 80 ("Solar Mold Spawn", -409, "Solar Mold infestation reported!") and 81 ("MoldSporeSpawn", -414) are only seen as raw text lines, not read or pinned (no LIO reader); mold count, spawn time/radius, special 20 and warnings; spawn-limit enforcement, spore combat (tracking, MoldSporeBeam), drops and hazard/resource interactions; RelativeId meaning.

## Files
- shared/RogueTrader.OriginalData/OriginalSolarMold.cs
- shared/RogueTrader.OriginalData/plan.d/rt-f062.md
- tests/Unit/OriginalSolarMoldTests.cs

## Results
- NuGet is blocked in the cloud, so tests ran with the xunit-compatible reflection shim: full unit set passed 268, failed 0, skipped 5 (pre-existing skips); `OriginalSolarMold` filter passed 7, failed 0, skipped 0. global.json unmodified.
- check-trackers: TRACKERS_CONSISTENT.
- Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419.

## Status
done (data clause, c4e4a94); resume point: LIO reader (queue G6) then rows 80/81 and the spawn/combat behaviour need native or simulation validation.
