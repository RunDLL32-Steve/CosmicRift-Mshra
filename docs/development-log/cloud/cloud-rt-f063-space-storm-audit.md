Review status: APPROVED (round 1, senior review, local)

# cloud/rt-f063-space-storm-audit

-   RT-IDs: RT-F063 (record/revision audit only). Gate items 2/4. Base: origin/main 3452d48. Claimed by the claim commit 734bed0.
-   Scope: pin NWS/Rogue.nws 329-332 with the dated heading; search every ITM/VEH/RPG/CFG record of the selected package for a storm/radiation implementation; record the documented revision mismatch. No LIO reader (queue G6 claimed elsewhere), no BLO reader. Field names are not used in this view.
-   Acceptance profile: "Trace actual selected item/event implementation or document revision mismatch; reproduce only once original dependency is resolved and feature completed."

## Acceptance clauses

-   Done (trace / revision mismatch): NWS lines 329 ("Added Space Storms to the map. These storms start somewhere random and grow over time, then shrink and finally disappear."), 330 (blank), 331 ("The storms discharge various high intensity radiations. These discharges seriously effect your ship and it's shields and can destroy a players"), 332 ("ship if their energy gets low.") pinned under the heading "**\*\*\*\***2/20/03" at line 327. Search of ITM (200 rows), VEH (30), RPG (12), CFG (2907 non-blank lines) for storm/radiat/discharg/radio/grow/shrink/nebula/cloud: 14 text hits, 6 of them name hits (ITM 1063 BioMissile Storm Launcher f3; 3012 BioStorm-Blast f3,f6; 2013 BioStorm Missiles f3,f6; VEH 425 f267 "Produce Biostorm Missiles 1:1 RT"), all the Skithzar BioStorm missile and each dismissed with a reason; `SpaceStormCandidates` is empty. No field has "radiat" or "discharg"; RPG has no hit; mechanism hits are unrelated (ITM 102/2003/2004 "radio...", ITM 1061/1062/1064/1066 BioCloud child projectiles, CFG line 2724 `TrickleGrow=0` in [Jackpot]). All 17 VEH type-5 rows (400-405, 407-414, 425-427) are named and none is a storm. Other NWS storm lines 363 "adjusted Bio Storm." and 427 "Buffed up the Moltor Storm Launcher." are other storms.
-   LIO (seen as raw text, not read/pinned; plain substring search only): LIO/Rogue.lio (the CFG LioFile) has no line with storm/radiat/discharg, matching the catalog's "selected LIO names do not identify a Space Storm record". Only LIO/chaos3.lio lines 261-264 (not the selected package; `5,v25,262,"Storm-Master"` with `Space Storm Alert!!!`, three `Storm-Sub` rows) contain the word; they are not interpreted.
-   Remaining (box stays OPEN): the storm itself (random start, grow/shrink/disappear, radiation discharges affecting ship, shields and energy, destroying at low energy) is not found in any readable original record of this package, so nothing is implemented, simulated or validated; resolving the dependency needs the LIO reader (G6) and chaos3-style level data or native evidence. Reproduction waits for that dependency and the completed feature.

## Visible mismatches (in `VisibleMismatches`)

1. The 2/20/03 NWS storm text has no matching record in ITM/VEH/RPG/CFG of the selected package. 2. Rogue.lio has no storm line (raw text). 3. The 17 VEH computer rows contain no storm. 4. Other "storm" NWS lines (363, 427) are different weapons.

## Files

-   shared/RogueTrader.OriginalData/OriginalSpaceStorm.cs
-   shared/RogueTrader.OriginalData/plan.d/rt-f063.md
-   tests/Unit/OriginalSpaceStormTests.cs
-   scripts/original-assets/audit_rt_f063.py (stdlib-only; reproduces the NWS lines, the 14 hits, the type-5 rows and the LIO raw-text hits; verified against the five pinned hashes)

## Results

-   NuGet is blocked in the cloud; tests ran with the xunit-compatible reflection shim: `OriginalSpaceStorm` filter passed 6, failed 0, skipped 0; full unit set passed 507, failed 0, skipped 5 (baseline 501 + 6 new; pre-existing skips). global.json unmodified.
-   `python3 -I scripts/original-assets/audit_rt_f063.py /tmp/rtassets/src` run: output matches the pinned test values.
-   check-trackers: TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open; unchanged).
-   Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419; LFS-backed BLO/LVL assets were not available.

## Status

done (audit clause); resume point: LIO reader (queue G6) and a decision on whether chaos3-style Storm rows belong to this feature; native executable evidence for the discharge effect. Box stays open.
