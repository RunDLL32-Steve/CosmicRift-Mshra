Review status: UNREVIEWED

# cloud/rt-f058-stealable-structures

- RT-IDs: RT-F058 (data only). Gate items 2/4/5. Base origin/main c81807c, not stacked. Claimed by the 18:25 UTC cloud run (claim commit only); resumed and completed by the 22:15 UTC cloud run of the same routine.
- Scope: typed raw view of the whole VEH v53 type-5 (Computer) layout and the stealable-structure audit for Micro Repair Dock 408, Micro Refinery 412 and Rogue Trader Outpost 413 against NWS line 295 (10% HP).
- Field-name evidence: the ORDER of reads in the InfantryOnline server parser (Infantry-Online-Server bb97579e, dotnetcore/Assets/Veh/VehInfo.cs, VehInfo.Computer.cs, VehInfo.ComputerProduct.cs, Shared/AnimatedSprite.cs, SpriteBlob.cs, HSV.cs, ArmorValues.cs), read as interpretation only (no code copied; that repo has no root licence). Upstream-emulator names, not original-executable evidence. `scripts/original-assets/veh_computer_field_map.py --check` validates the map against all 17 pinned type-5 rows: 477 fields each, 64 text slots, 412 integer slots, 0 problems; products start at f[253] (agrees with the merged RT-F055/F057 block views).

## Acceptance clauses
- Done (data): record identity, raw threshold-related fields and ownership-logic fields of the three structures pinned; NWS 295 pinned.
- Visible mismatches (kept raw): HitpointsRequiredToOperate f[211] = 5/5/10 = 2% of Hitpoints 250/250/500, not 10% (25/25/50); LogicTakeOwnership f[234] and LogicStealOwnership f[235] are empty for all three (non-empty only for GunSat 410, BeamSat 411, Mold Spore 414 `!`, Manufactories 425-427; take logic also on PirateStation 403). Where the native "10% HP" steal rule lives is unresolved.
- Remaining (box stays OPEN): native threshold/access/owner-transfer conditions, complete transfer with inventory/production, no duplication.

## Files
- scripts/original-assets/veh_computer_field_map.py
- shared/RogueTrader.OriginalData/OriginalComputerFields.cs (`VehComputerFields`, `OriginalComputerRecord`)
- shared/RogueTrader.OriginalData/OriginalStealableStructures.cs
- shared/RogueTrader.OriginalData/plan.d/rt-f058.md
- tests/Unit/OriginalComputerFieldsTests.cs, tests/Unit/OriginalStealableStructuresTests.cs

## Results
- NuGet (api.nuget.org) and dot.net are blocked by the cloud egress policy this run, so xunit could not be restored. Tests were executed with an xunit-compatible reflection shim (fake `Xunit` namespace compiled with OriginalData + tests/Unit/Original*.cs, SDK 8.0.131 via uncommitted global.json tweak, pinned assets eb1514ea): passed 261, failed 0, skipped 5 (the 5 skips are pre-existing, not from this branch). Coordinator re-ran independently: same numbers.
- veh_computer_field_map.py --check: CHECK PASSED (17 rows, 0 problems); confirmed it fails on a 478-field row and an unquoted number in a text slot.
- check-trackers: TRACKERS_CONSISTENT.
- Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419, scripts/verify.ps1.

## Status
done (data clause); resume point: native steal threshold and owner-transfer behaviour need the native client/server (desktop).
