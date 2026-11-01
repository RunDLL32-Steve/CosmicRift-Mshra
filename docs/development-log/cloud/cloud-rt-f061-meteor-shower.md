Review status: APPROVED (senior review round 1, PR #39)

# cloud/rt-f061-meteor-shower

-   RT-IDs: RT-F061 (data clause only). Gate items 2/4/5. Stacked on cloud/rt-f058-stealable-structures (8a8be93; uses its VehComputerFields/OriginalComputerRecord).
-   Scope: raw data view of the Meteor hazard actor: VEH id 402 "MeteorShower" as a validated type-5 record, the ITM item it lists (1007 "Meteor") with its existing raw projectile/chain views, and NWS line 188. Catalog acceptance "Decode native trap event and produced meteor/projectile chains, movement/collision/damage and spawn timing; verify completed hazard" is NOT met.
-   Evidence: VEH/Rogue.veh row 3, ITM/Rogue_mp.itm row 72, NWS/Rogue.nws line 188 at the pinned commit eb1514ea. VEH field names are upstream-parser names (InfantryOnline bb97579e), not original-executable evidence.

## Acceptance clauses

-   Done (data): VEH 402 identity and raw fields pinned (Hitpoints 50, Weight 100000, Description quoted "0", InventoryItems0 = 1007, Sprites0 effects/gfx00203, EnergyMax 1700, EnergyRate 1150, AngleLength 360, TrackingRadius 20000, FireRadius 20000, RemoveGlobalTimer 5, RandomizeAim 5, Destroyable 0, DensityRadius 750, IsWarpable 1, all 16 TerrainModifiers -1, all 30 hold-item limit slots -1, full non-default field fingerprint); item 1007 resolved (ITM row 72, type 6 / 227 fields, "Child Effects", mass 1000, buy 0, sell -1) with raw projectile fields (fireDelay 35, inheritedSpeed 0, muzzleVelocity 4250, aliveTime 2000, explodeItem 0) and an empty reference chain (ammo f[31] = 0, explodeItem f[142] = 0); NWS 188 pinned.
-   Visible facts kept raw: the item's sprite/effect strings (`creffects`/`Meteor`, `creffects`/`MeteorTrail`, `ExplosionEffects`/`bombhit3_`) are present in its record but not interpreted; VEH f[65] RadarPrize and f[43] PickupItemId are -1; Destroyable is 0 while Hitpoints is 50.
-   Remaining (box stays OPEN): LIO rows 18/49/52/57 (raw actor-402, area 15000x15000, warning, timing 3000/6000/12000) are not read (LIO reader is queue G6, claimed elsewhere); native trap event, how a meteor is spawned/aimed (RandomizeAim, FireRadius, TrackingRadius meanings), movement, collision, damage and spawn timing are undecoded and unimplemented.

## Files

-   shared/RogueTrader.OriginalData/OriginalMeteorShower.cs
-   shared/RogueTrader.OriginalData/plan.d/rt-f061.md
-   tests/Unit/OriginalMeteorShowerTests.cs

## Results

-   NuGet (api.nuget.org) is blocked by the cloud egress policy, so xunit could not be restored. Tests were run with the xunit-compatible reflection shim (SDK 8.0.131 via temporary global.json tweak, restored afterwards; pinned assets eb1514ea): filtered OriginalMeteorShowerTests passed 6, failed 0, skipped 0; full tests/Unit/Original\*.cs set passed 267, failed 0, skipped 5 (the 5 skips pre-date this branch).
-   Oracle values were read from the pinned files with an independent CSV dump before being asserted; two initial test expectations (item Sell f[15] = -1 and four extra -1 slots f[13], f[56], f[141]) were corrected to the file values, not the other way round.
-   check-trackers: TRACKERS_CONSISTENT (no status counts changed; box stays open).
-   Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419, scripts/verify.ps1.

## Status

done (data clause, a4feb69); resume point: LIO rows 18/49/52/57 once the G6 LIO reader exists, then the native trap event and meteor behaviour (desktop/native evidence).
