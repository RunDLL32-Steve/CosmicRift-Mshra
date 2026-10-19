Review status: APPROVED (round 1, senior review, local)

# cloud/rt-f064-los-radar-fields

-   RT-IDs: RT-F064 (data only). Gate items 2/5/6. Base: origin/main 3452d48 (claim commit a6b327e).
-   Scope: typed raw view of the CFG vision/LOS/radar keys, the vision/radar fields of every VEH row (per-hull overrides) and the pinned NWS lines with dated headings; no semantics, no simulation, no rendering. Field names for VEH are upstream-emulator parser names (Infantry-Online-Server bb97579e) via the F058 map, not original-executable evidence. LIO and BLO are not read (queues G6/C1).

## Acceptance clauses

-   Done (data): CFG [LOS] lines 119-122 = DefaultDistance 250, DefaultAngle 200, DefaultXray 0, TeamSharing 1 (header 118); [Arena] TeamVisionDistance 1200 (line 32); [Radar] header 2276 with eight keys on 2277-2284: ColorSoccerBall 0xffffff, ColorSelf 0x00ff00 (2278), ColorFriendly 0xffff00 (2279), ColorEnemy 0xff4040 (2280), ColorFlag 0xffffff, FontColorSelf 2, FontColorFriendly 4, FontColorEnemy 1.
-   Done (enumerated related keys, scan of every section for Los/Vision/Radar/Xray/Fog/Sensor/Sight/Visib/TeamShar): [Arena] SpecsSeeEnergy 1 (60), FriendlySeeEnergy 1 (61), EnemySeeEnergy 0 (62); [Level] RadarPrizeDistance 2000 (23); [Flag] CarrierOnRadar 0 (866), CarrierLineOfSight 0 (867); [Soccer] ShowRadar 1 (2099); [Uiart] Vision/WarFog/RadarBorder = cruiart,vision / cruiart,warfog / cruiart,newradar (2739/2742/2751); [LosType0..7] at 2767+12N (VisibleDistance -1,-1,-1,0,4,-1,12,16; Solid 0,1,1,0,0,0,0,0; SeeType0..7 = 1,1,1,1,1,0,1,1 except LosType5 all 1); [TeamInfoN] RadarColor is 0x000000 in all 50 blocks (existing OriginalTeamInfo). Excluded as unrelated: Loser*/Loss*/Lost*, Invisible*, ComputerExplosionDamageComputer, InitialHides, LoseCrownExpire.
-   Done (per-hull overrides): VEH f[132] LosDistance, f[133] LosAngle, f[134] LosXRay = 0/0/0 on all 12 hull Car rows (rows 18-29), on all 17 computers and on Spectator Mode; f[135] CombatAwarenessTime 0 everywhere; f[138..141] DisplayOnFriendlyRadar/FriendlyRadarColor/DisplayOnEnemyRadar/EnemyRadarColor = 1/-1/1/-1 on all 30 rows; f[65] RadarPrize -1 on the hulls, 402 and 2030, 0 on the other 16 computers. So no hull has a distinct per-hull LOS or radar value. No VEH field named scale, fog or sensor exists in the shared base block names used by the repo.
-   Done (NWS): line 258 "Re-Enabled Team vision sharing." under 2/27/03 (heading line 249); line 325 "Base LOS has been narrowed to 225 degrees. This will leave a definite \"blind spot\" on your tail." under 2/24/03 (heading line 293); lines 305-315 (Stealth Active/Enhanced/Passive, Distortion Wave/Zone, Turbo/Super Impellors, Ship Sensors Enhanced/Superior, Ship Sensor Array, Terran Combat Sensors) pinned under the same 2/24/03 heading; no ITM row name contains Sensor, Stealth or Distortion (and no such text in ITM/Rogue_mp.itm).
-   Remaining (box stays OPEN): LOS/angle/distance/X-ray semantics and units, whether 0 on a hull means "use the [LOS] default", the 200 vs 225 degree relation, TeamVisionDistance and TeamSharing rules, SeeEnergy flags, LosType meaning, fog/WarFog, sensor and stealth items and their effect, radar blip rules and rendering, LIO/BLO parts, and any simulation or rendering verification.

## Visible mismatches (kept raw, listed in `VisibleMismatches`)

-   Catalog says NWS 261 for team vision; line 261 is blank. The text is on line 258 (2/27/03).
-   NWS 325 says 225 degrees; CFG DefaultAngle is 200 (scale unproven).
-   Catalog "Radar 2278-2280" cites three of eight [Radar] keys; ColorEnemy is 0xff4040 (catalog says "red"; channel order unproven).
-   Catalog line numbers LOS 119-122, Arena 32 and NWS 325 match the pinned files.

## Files

-   shared/RogueTrader.OriginalData/OriginalVisibility.cs
-   shared/RogueTrader.OriginalData/plan.d/rt-f064.md
-   tests/Unit/OriginalVisibilityTests.cs
-   scripts/original-assets/audit_rt_f064.py
-   docs/development-log/cloud/cloud-rt-f064-los-radar-fields.md

## Results

-   NuGet is blocked in the cloud, so tests ran with the xunit-compatible reflection shim: full unit set passed 512, failed 0, skipped 5 (pre-existing skips; main baseline was 501/0/5); `OriginalVisibility` filter passed 11, failed 0, skipped 0. global.json unmodified.
-   `python3 -I scripts/original-assets/audit_rt_f064.py /tmp/rtassets/src`: ALL PASS.
-   check-trackers: TRACKERS_CONSISTENT.
-   Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419; scripts/verify.ps1.

## Status

done (data clause); resume point: native/simulation evidence for LOS angle/distance units and the 0-default question, team vision sharing rules and radar rendering; LIO/BLO sides after queues G6/C1.
