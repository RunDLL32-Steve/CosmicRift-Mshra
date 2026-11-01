Review status: ready-for-review

# Branch log: claude/project-thread-yaij0l (queue M7 mining slice)

Scope: the half of queue [M7](../../tasks/m7.md) left open after PR 94 merged the kill slice: "connect the generic kill/mining outcome to GrantReward using stable idempotency identity". Section A rows were not cloud-eligible (G1 blocked, G5 claimed by `cloud/rt-f036-f037-g5-weapon-records`, G6/G9 claimed, G8 local, G10 waits on G6), so M7 was the first open row. Data: [minerals](../../../data/minerals/IMPLEMENTATION_PLAN.md), [stations](../../../data/stations/IMPLEMENTATION_PLAN.md).

Status: round 2 pushed (review round 1 fixes), awaiting re-review. Not merged by the author.

## Round 1 — November 1, 2026

-   What mining is: the pinned NWS trade-system text (rows 77–84) says minerals appear randomly and have to be found, are refined at a refinery and traded at a trading post, and the catalog says not to replace mineral spawns with invented mining asteroids. So the client mines by collecting piles from the original LIO mineral spawns; the queue M3 asteroid-extraction engine is untouched and unused by the client.
-   `OriginalMineralSpawns` (OriginalData): narrow raw view of the 12 LIO type 5 v23 rows whose f[20] is a Resources item, through `RawRecordReader` and the existing LIO hash pin. Not the queue G6 LIO reader; no field is named beyond id, name and item.
-   `OriginalMineralFieldBuilder` derives `data/minerals/mineral_spawns.json` (oracle test, `RT_WRITE_DERIVED_MINERALS=1`): 11 spawns (row 54 is -1/-1 and skipped), each with its raw fields and the NEW readings listed in `newRules`.
-   Simulation: `MineralFieldCatalog` and a spawner in `WorldSimulation` (SplitMix64 placement, reproducible per seed; hashed only when configured, so other replay digests are unchanged); `WorldSimulation.Pickups` logs every pickup.
-   Rewards: `StationProfileSession.GrantPickupAsync` grants each pickup with the stable id `pickup-<sector instance>-<sequence>` and source `pickup:<pile id>`; `PickupCommitQueue` keeps unsaved pickups queued so the hold is always saved inventory plus pending pickups. This replaces the state-based `CommitPickupsAsync` from M8: a reload no longer drops unsaved cargo (the M8 "reload discarded" warning is gone); only a sector reset or relaunch can lose pickups that still fail their last save, and the player is told.
-   Refinery: `TransactionKind.Refine`, `RefineRequest`, `IPriceSource.TryGetRefineRecipe`, a refinery site in the derived station (VEH id 400 recipes, LIO row 13 position), dock with F, Refinery page in the station screen. Profile schema 4 (identity 3-to-4 migration) so older builds refuse the new kind as newer instead of quarantining.

Verified in the cloud container (SDK 8.0.133 and Godot 4.5.1 mono Linux substitutions, as M8/M9): solution build 0 errors; `dotnet test tests/Unit` with pinned assets at `eb1514e` 877 passed, 0 failed, 5 existing skips (M9 head: 863); `dotnet test tests/Integration` 6 passed; Godot import exit 0; `--smoke-test` exit 0 with CLIENT_INPUT_SMOKE_PASS, CLIENT_STATION_SMOKE_PASS (now mines 44 Tellerium through a failed save and a reload, refines it and trades it for 2,420 credits) and CLIENT_SMOKE_PASS.

Not verified: the `verify.ps1` wrapper itself, the Windows export, pinned SDK 8.0.419, a rendered (non-headless) capture.

Fidelity: RT-F042 and RT-F044 stay open; every LIO type 5 reading, the map scale, the one-for-one free refining and all placement are NEW. Feature counts unchanged. Sector resets still re-pay the practice targets (Steve's respawn rule is being built in the M9 thread, not here).

## Round 2 — November 1, 2026 (review round 1 at bc8e5d7)

-   R1-1: pickup ids and mineral placement now key on a per-world id (a new GUID in every `NewWorld`), never on the sector instance, so a reset that keeps the sector (PR #95) cannot repeat `pickup-<id>-0`. Test `ResetThatKeepsTheSector_StillSavesTheSamePileAgain`.
-   R1-2: `PickupCommitQueue.HoldContents` skips queued pickups whose id is already in the profile's applied transactions, so a reload after a lost acknowledgement no longer double-counts the hold. `MinedPickup_IsSavedOnce_AcrossRetryAndReload` now asserts it.
-   R1-3: quitting gives queued pickups a last save, as kills already had.

Verified: unit 878 passed, 0 failed, 5 skips; integration 6; Godot import 0 and smoke 0 (all three PASS lines); FORMAT_CLEAN; TRACKERS_CONSISTENT.

Resume point: re-review of the round 2 head.
