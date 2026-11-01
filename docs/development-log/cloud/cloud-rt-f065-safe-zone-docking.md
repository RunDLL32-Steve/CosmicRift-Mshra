Review status: APPROVED (round 1, senior review, local)

# cloud/rt-f065-safe-zone-docking

-   RT-IDs: RT-F065 (data only). Gate items 2/5/7. Base: origin/main 3452d48. Claimed by claim commit 2004a11.
-   Scope: typed raw view of the safe-zone / docking related keys of rogue.cfg (all 16 terrain blocks and the arena-level keys), the NWS lines about safe zones, forced spec, team change and doors, and raw-text sightings of the LIO docking rows. No LIO reader (queue G6 claimed elsewhere), no BLO/LVL reader.
-   Field-name evidence: the CFG key names are the file's own text; any meaning attached to them by the InfantryOnline emulator is an upstream-parser name, not original-executable evidence. Values were read from the pinned CFG/rogue.cfg, NWS/Rogue.nws and LIO/Rogue.lio (eb1514ea; sha256 guarded, LIO pinned in `OriginalSafeZones.LioRogue`).

## Acceptance clauses

-   Done (data): per-terrain keys pinned for all 16 terrains with line numbers (28 selected keys per block; 7 others named: StripShadows, SoccerEnabled, GoalFrequency, FontColor, Starfield, GoalPoints, FlagTimerSpeed). Safety=1 on Terrain9 "Repair Dock", Terrain10 "SpaceDock. Store Zone.", Terrain15 "Refinery Zone/Trade Zone"; EnergyRate=-2000 on the same three; MaxTimeAllowed=60 on 9 and 15; RepelVehicle=0, DeleteLiveWeapons=1, reward percents 0 on 10 and 15; Store/Skill/TeamChange enabled flags as RT-F046; PrizeEnableMode 3 everywhere; PrizeExpire 720 (T0) / 120; BountyAutoMax empty on 9 and 15; QuitDelaySecs 7 on T0 only.
-   Done (data): 26 arena-level keys with sections and lines (Arena spectator/team-switch keys lines 33-83, Door 841-842, Flag AllowSafety 873, Soul 97-108, Event 2492-2511).
-   Done (data): NWS 290 (2/25/03), 344/345 (2/16/03), 419 (9/06/01), 481/483 (8/27/01) verbatim with headings; the string "48360" occurs nowhere in NWS/Rogue.nws.
-   Done (raw sightings): LIO docking rows at lines 12, 40-48, 60-62 (13 rows, exactly the catalog's list); no LIO line contains "door".
-   Visible mismatches (kept in `VisibleMismatches`, nothing corrected): catalog "48360-second" is not in the news (483 says 60 seconds, 8/27/01); NWS 419 energy loss applies to all three Safety=1 terrains, not only the refinery/trade zone; MaxTimeAllowed=60 is on Repair Dock and Refinery/Trade zone, not SpaceDock; NWS 290 500 energy vs TeamSwitchMinEnergy=500000 and TeamChangeEnabled=0 on safe terrains 9, 15; NWS 481 vs 13 terrains with TeamChangeEnabled=1; NWS 345 "not currently functional" vs catalog "not yet functional", with doors absent from Rogue.lio.
-   Remaining (box stays OPEN): LIO docking records decode (coordinates, sizes, boundary), LVL terrain-id map and door placement (Git LFS, not available), zone entry/exit triggers, timing semantics of MaxTimeAllowed and EnergyRate units, original forced-spec behaviour, current door state, and any verified safe-zone, docking or forced-spec behaviour from the chosen revision (needs native or simulation validation).

## Files

-   shared/RogueTrader.OriginalData/OriginalSafeZones.cs
-   shared/RogueTrader.OriginalData/plan.d/rt-f065.md
-   tests/Unit/OriginalSafeZonesTests.cs
-   scripts/original-assets/audit_rt_f065.py
-   docs/development-log/cloud/cloud-rt-f065-safe-zone-docking.md

## Results

-   NuGet is blocked in the cloud, so tests ran with the xunit-compatible reflection shim: full set with assets passed 509, failed 0, skipped 5 (baseline 501/0/5 + 8 new); `OriginalSafeZones` filter with assets passed 8, failed 0, skipped 0; without assets the filter passed 1 (synthetic), skipped 7. global.json unmodified.
-   `python3 -I scripts/original-assets/audit_rt_f065.py /tmp/rtassets/src` reproduces the pinned values (hash checks True).
-   check-trackers: TRACKERS_CONSISTENT.
-   Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419; scripts/verify.ps1.

## Status

done (data clause); resume point: LIO reader (queue G6) for the docking rows, an LVL/BLO reader for the terrain-id map and door placement, then native/simulation validation of safe-zone energy loss, the 60 second limit and forced spec.
