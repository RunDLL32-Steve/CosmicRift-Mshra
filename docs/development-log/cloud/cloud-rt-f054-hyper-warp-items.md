Review status: APPROVED (senior review round 1, PR #31)

# cloud/rt-f054-hyper-warp-items

-   Claims: RT-F054; gate items 2, 7.
-   Base: origin/main ac41dc9 (no stacking).
-   Scope: raw, unscaled, sentinel-flagged typed view of the original HyperCore and warp item records (ITM/Rogue_mp.itm), data only.
-   Status: done for the data slice; RT-F054 stays OPEN (no checkbox ticked in master/catalog/checklist).

## Acceptance clauses (catalog: "Resolve prerequisites, energy, targeting, location sentinels and named warp/owned-outpost links; verify completed original jump features")

Done (data only):

-   Item rows resolved and pinned: 110 Hyper Jump (id 25), 111 HyperCore (2011, type 4, 31 fields), 116 Hyper Beacon System (29), 117 Controlled Hyper Jump (30), 118 Hyper Recall (31), 119 Hyper Gate (33). All catalog row numbers and ids verified correct; no mismatch.
-   Rows 112-115 explained: 112 Portal (26, "BaseLimitedUse"), 113 Warp-Start (27, "BaseWarp"), 114 Warp-Team (28, "BaseWarp") are real type-17 warp-family rows the catalog does not list and RT-F048's category filter (`HyperDrive Systems`) excludes; they are now included explicitly in `OriginalWarpItems`. Row 115 is `Skithzar ClusterBurst-Child` (id 3009, type 8, ChildProjectiles), unrelated to warping. The pinned file has exactly eight type-17 rows (all 113 fields), all in this set.
-   Fields that differ between the eight type-17 rows found by comparing records: 36 integer fields (f[8,11,13,14,15,20,27,30,31,32,34,35,36..51,57,60,61,93,94,97,98,111]) and 7 text fields (f[5,23,24,101,102,109,110], plus category/name/description). Exposed as raw indexed ints (`DistinguishingInts`); only f[5] logic, f[8] buy, f[15] sell (existing catalog names) and f[34] (inferred below) are named.
-   Sentinels kept: f[13] -1 (six rows), -2 (Portal), 0 (Warp-Start); f[36..51] is -2 x16 in seven rows and {0,-1,-1,-1,0 x12} in Portal. All flagged `IsNegativeSentinel`, values unchanged.
-   Prerequisite evidence (inferred, not proven): f[34]=2011 (= HyperCore id) and f[35]=1 in exactly the five HyperDrive Systems rows (25,29,30,31,33), 0/0 in Portal/Warp-Start/Warp-Team; NWS row 37 says all HyperDrive systems require a HyperCore. 2011 is also VEH hull "Excelsior" (and an RPG row), so it is not proven to be an item-id lookup. Logic text f[5]: Beacon `!1&!2&!3&!4&!5&!6&!8&!9&!10&!11&!15&!16`, Portal and Warp-Team `!%1`, others empty (meaning = skill-logic evaluator, undecoded).
-   Evidence-only (no LIO reader, queue G6): Rogue.lio row 10 id 8 `HyperBuoyWarp` (0,0,250,250,..., last tail 10), row 27 id 56 `RandomHyperJump` (16576,14608,15000,15000, tail 9), row 28 id 57 `DeepSpaceWarpA` (16832,15888,10000,10000, tail 0,1,0). Catalog's "DeepSpaceWarp28" is actually named `DeepSpaceWarpA`; catalog LIO numbers are row numbers, not ids. NWS 410-411 (9/06/01): "Hyper Buoy was removed ... Hyper Recall ... jump to the vicinity of your Post. Otherwise ... Hyper Jump Randomly" - the HyperBuoyWarp LIO record and Recall's description ("or Hyper Jump Buoy") thus describe revision-mismatched behaviour, to be resolved when the LIO reader exists.

Remaining (unfinished, left open): energy cost/requirement semantics ("requires full energy" for Warp-Start/Warp-Team per description; f[93]/f[94]/f[97]/f[98]/f[111] unnamed), targeting (teammate selection, Gate target), what the -2 block and f[13] sentinels mean, how f[34]/f[35] and skill logic gate use, LIO-side warp/outpost/trading-post links (G6), art/sound resolution (cruiart/warpPorter, crwav/warpin, UtilitySounds), and every verification of a completed jump feature (needs native/runtime evidence).

## Files touched

-   shared/RogueTrader.OriginalData/OriginalWarpItem.cs (new: `OriginalWarpItem`, `OriginalWarpItems`)
-   tests/Unit/OriginalWarpItemTests.cs (new)
-   scripts/original-assets/audit_rt_f054.py (new, stdlib only; independent audit, prints test oracle values plus raw LIO/NWS evidence lines)
-   shared/RogueTrader.OriginalData/plan.d/rt-f054.md (new)
-   docs/development-log/cloud/cloud-rt-f054-hyper-warp-items.md (this file)

## Verified (cloud)

-   `python3 -I scripts/original-assets/audit_rt_f054.py /tmp/claude-0/assets/repo`: ITM sha256 ok, 200 rows; its output was used to cross-check every pinned value.
-   Focused: `rt-shim-test.sh <wt> OriginalWarpItem` -> Passed 8, Failed 0, Skipped 0, Total 8 (2 synthetic always-run, 6 oracle against pinned assets).
-   Full suite: `SHIM RESULT: Passed 306, Failed 0, Skipped 0, Total 306` (main baseline 298; +8). Offline xunit-compatible shim, apt SDK 8.0.131, not the pinned 8.0.419/real xunit.
-   `python3 scripts/check-trackers.py` -> TRACKERS_CONSISTENT (22 complete, 375 open; no counts changed).

## Not verified in cloud - local CI required

-   Real xunit run on the pinned SDK 8.0.419 (`scripts/verify.ps1`).
-   Anything depending on BLO/LVL (LFS pointers only here), LIO decoding (G6), native client behaviour, actual jump energy/targeting/prerequisite behaviour, art/sound resolution.

## Resume point

After the LIO reader (G6) exists: link LIO ids 8/56/57 (and owned-outpost/trading-post records) to these item rows, then decode f[93]/f[94]/f[97]/f[98]/f[111], the -2 block and f[13] with native evidence; only then verify completed jump features and tick RT-F054.

## Coordinator review (development run 2026-10-16 14:20 UTC)

Implementation delegated to a Sonnet 5.5 High agent (matrix: data loaders from approved specs). The coordinator read the full diff, spot-checked the pinned values with its own Python read of the pinned files, re-ran the branch's audit script (exit 0) and re-ran the whole unit suite: offline xunit-compatible shim, apt SDK 8.0.131, RT_ORIGINAL_ASSETS_ROOT = pinned clone eb1514ea -> Passed 306, Failed 0, Skipped 0 (main baseline 298). check-trackers: TRACKERS_CONSISTENT. Accepted for senior review; no registry box ticked. The real xunit run on the pinned SDK 8.0.419 is not verified in cloud - local CI required (nuget.org is blocked here).
