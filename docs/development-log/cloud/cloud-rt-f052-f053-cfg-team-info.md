Review status: APPROVED (senior review round 1, PR #30)

# cloud/rt-f052-f053-cfg-team-info

-   Claims: RT-F052 (public militia spawn/strongholds), RT-F053 (private/free trader teams); gate items 2, 5. Data clauses only; both boxes stay open.
-   Base: origin/main ac41dc9.
-   Status: done for the data clauses below; wip overall (behaviour needs G6 LIO reader + native/multiplayer verification).

## Acceptance clauses

Done (data only):

-   RT-F052: raw typed view of `[TeamInfo0..3]` (Cold Harbor / Northern Cross / Cassandra / Yankee Station, name lines 941/964/987/1010, MaxPlayers 10, hAdjust 25/0/65/75) inside the full `[TeamInfo0..49]` set.
-   RT-F053: `AllowPrivateFrequencies=1` at CFG line 31; `[TeamInfo4]` Free Traders (header 1032, Name line 1033, MaxPlayers 10, hAdjust 85).
-   Reproducible Python listing of the raw LIO/Rogue.lio record-type-4 rows (evidence in the audit output only).

Remaining (not done):

-   RT-F052: spawn areas/coordinates and team references from LIO type-4 rows (needs the queue-G6 LIO reader, branch claim/g6-level-reader - no C# LIO reader here); team ownership/colors/limits enforcement; native stronghold choice; respawn on death; original dock access (public dock vs private deep space, NWS 46-50); completed-feature verification.
-   RT-F053: private frequency creation semantics (FrequencyMax/MaxPerFrequency/DesiredFrequencies meanings unestablished), random deep-space spawn policy for private teams (NWS line 49-50 text, LIO rows 27/28 RandomHyperJump/DeepSpaceWarpA), historical team-size changes, independent-client multiplayer verification.

## Files touched

-   shared/RogueTrader.OriginalData/OriginalTeamInfo.cs (new: `OriginalTeamInfo`, `OriginalArenaFrequencies`)
-   tests/Unit/OriginalTeamInfoTests.cs (new)
-   scripts/original-assets/audit_rt_f052_f053.py (new, stdlib only)
-   shared/RogueTrader.OriginalData/plan.d/rt-f052-f053.md (new)
-   this log

## Pinned facts (CFG/rogue.cfg, sha256 82a9e40e..., 48399 bytes; audit output)

-   50 `[TeamInfoN]` sections, N=0..49 contiguous, header lines 940 + 23\*N (940..2067), 21 entries each; two key orders exist (Colors before or after RelativeVehicle).
-   Non-empty Name: all 50; no blank names. Real names (MaxPlayers>0): 0 Cold Harbor, 1 Northern Cross, 2 Cassandra, 3 Yankee Station, 4 Free Traders (all MaxPlayers 10), 5 Kurandis, 6 Chaeron, 7 Artemis, 8 Oort, 9 Methlus, 10 Cormus, 11 Flyers, 12 Fearless (MaxPlayers 9). Indices 13..49 are the literal text `Public13`..`Public49` with MaxPlayers 0 (37 blocks).
-   hAdjust for 0..12: 25,0,65,75,85,65,15,5,95,75,35,0,0; 13..49: 0. sAdjust/vAdjust 0 everywhere.
-   Identical in all 50: Colors=0xffffff,0xedeae0,0xf1ddc4; DefaultVehicle=-1; RelativeVehicle=0; RadarColor=0x000000; NameFontColor=-1; EventString and TeamEventStartGame empty; DisallowWinFlagGame=0; RtsState0..7=-1.
-   `[Arena]` (header line 27): FrequencyMax=5000 (l28), MaxPerFrequency=5 (l29), DesiredFrequencies=2 (l30), AllowPrivateFrequencies=1 (l31). Only the last is cited by the catalog; the other three are exposed raw by name, meaning unestablished.
-   LIO/Rogue.lio has 13 first-field-4 rows (lines 6-10, 27-32, 55-56). Raw observation, not interpreted and not asserted in C#: the last numeric field before `""` is 0 (line 6 Cold Harbor SpawnA), 1 (lines 7-9, 55 Northern Cross A-D), 2 (line 29 CassandraA), 3 (lines 30-32, 56 YankeeStation A-D), matching TeamInfo index 0-3 in order; HyperBuoyWarp (10), RandomHyperJump (27) and DeepSpaceWarpA (28; catalog "LIO28") have 0. All lines are in the audit output.

## Verified results

-   `python3 -I scripts/original-assets/audit_rt_f052_f053.py` against /tmp/claude-0/assets/repo (eb1514ea): hash matches OriginalCfg.Rogue pin; output as above; used to cross-check the oracle test values.
-   `rt-shim-test.sh ... OriginalTeamInfo`: Passed 9, Failed 0, Skipped 0 (5 synthetic + 4 oracle).
-   Full suite (offline xunit-compatible shim, apt SDK 8.0.131, not the pinned 8.0.419/real xunit): Passed 307, Failed 0, Skipped 0, Total 307 (main baseline 298 + 9 new). `python3 scripts/check-trackers.py`: TRACKERS_CONSISTENT (checklist 397 entries, 22 complete, 375 open; unchanged).

## Not verified in cloud - local CI required

-   Pinned SDK 8.0.419 with real xunit run (shim used: offline xunit-compatible shim, apt SDK 8.0.131).
-   Any BLO/LVL-dependent check (not available; LFS pointers only).
-   Native behaviour: spawn/respawn on death, stronghold choice, dock access, private-frequency creation, deep-space spawn, multiplayer.

## Resume point

After queue G6 lands the LIO reader, add a typed join of type-4 rows to `OriginalTeamInfo.Index` (verify the team-reference field order against the native parser before naming it), then resolve respawn/dock/private-frequency behaviour with native evidence.

## Coordinator review (development run 2026-10-16 14:20 UTC)

Implementation delegated to a Sonnet 5.5 High agent (matrix: data loaders from approved specs). The coordinator read the full diff, spot-checked the pinned values with its own Python read of the pinned files, re-ran the branch's audit script (exit 0) and re-ran the whole unit suite: offline xunit-compatible shim, apt SDK 8.0.131, RT_ORIGINAL_ASSETS_ROOT = pinned clone eb1514ea -> Passed 307, Failed 0, Skipped 0 (main baseline 298). check-trackers: TRACKERS_CONSISTENT. Accepted for senior review; no registry box ticked. The real xunit run on the pinned SDK 8.0.419 is not verified in cloud - local CI required (nuget.org is blocked here).
