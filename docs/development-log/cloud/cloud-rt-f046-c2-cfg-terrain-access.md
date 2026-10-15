Review status: UNREVIEWED

# cloud/rt-f046-c2-cfg-terrain-access

- Claims: queue C2 (raw-preserving CFG reader, rogue.cfg) and RT-F046 (data clause only: per-terrain StoreEnabled/SkillEnabled/TeamChangeEnabled); gate items 2 and 5 (data). Also provides the reader that RT-F050/F051/F064 data clauses need.
- Base: origin/main (needs only G2's AssetIntegrity). Independent of the ITM-catalog stack.
- Status: done for the scoped data part. No registry box ticked; TASK_QUEUE C2 set to REVIEW (the reader covers sections/keys generically; the 16 terrain blocks and Rogue arena values are pinned by tests).

## Acceptance clauses
Done (data): `OriginalCfg` parses CFG/rogue.cfg (hash-guarded: 48399 bytes, sha 82a9e40e...; equals ASSET_MANIFEST originalSha256): 144 unique sections, 16 `[TerrainN]`, CRLF, no content dropped (non-section lines without '=' raise). Pinned values: Arena PointBlankDamageMultiplier=500 (line 63), Vehicle PhysicsDelay=400, Soul EnergyDefaultMax=1700000 / EnergyDefaultRate=250 / EnergyShieldMode=2. Terrain access (independent Python audit): StoreEnabled=0 in terrains 0, 9, 15; SkillEnabled=0 in 0, 9, 10, 15; TeamChangeEnabled=0 in 0, 9, 15; others all 1. Terrain0 section starts at line 248 (StoreEnabled line 252, matching the catalog's "rows 252-255").
Remaining: other-arena CFG files (only rogue.cfg is pinned/hashed here), meaning of the terrain numbering vs LVL tiles (needs G6), shop bounds, loadouts/ammo refill, prices/resale/safezone rules and store UI, enforcement (decision D: the pinned server never reads SkillEnabled), native verification.

## Files
shared/RogueTrader.OriginalData/OriginalCfg.cs, tests/Unit/OriginalCfgTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md, TASK_QUEUE.md (C2 row).

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 215, Failed 0, Skipped 0. Without assets: Passed 188, Skipped 14, Failed 0.
- python scripts/check-trackers.py -> TRACKERS_CONSISTENT.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
RT-F050 (CFG DeleteWeaponsOnClassChange etc.), RT-F051 (CFG Bounty/Cash/Point) and RT-F064 (LOS/Radar) data clauses can now use OriginalCfg.
