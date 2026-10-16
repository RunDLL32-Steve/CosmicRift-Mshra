Review status: APPROVED (senior review round 1, PR #25, head f58cc90)

# cloud/rt-f029-collision-fields — RT-F029 collision radius, bounce, point-blank and terrain repel raw-field audit

- Claims: RT-F029 (data clauses only: gate items 2 field values/sentinels evidence and 6). Base: origin/main (not stacked).
- Status: done for the data clauses; RT-F029 box stays open.

## Acceptance clauses
Done:
- Independent audit `scripts/audit-collision-fields.py <assets-root>` (read-only).
- Raw C# accessors `OriginalCollisionFields` (new file only; `OriginalHull.cs` and `OriginalCfg.cs` untouched).
- Unit tests pinning the audit values; catalog row note; plan.d entry.

Remaining (not done, box stays open): meaning/units of f[35] (radius is only a catalog candidate) and f[362]; bounce response and scale; point-blank damage mechanics and scale; terrain repel mechanics (vehicle vs weapon); relation of `PhysicsDelay` to the simulation (explicitly not equated to the 60 Hz step); asteroid/map contact behavior; hit triggers; any simulation.

## Files
- scripts/audit-collision-fields.py
- shared/RogueTrader.OriginalData/OriginalCollisionFields.cs
- tests/Unit/OriginalCollisionFieldsTests.cs
- shared/RogueTrader.OriginalData/plan.d/rt-f029.md
- docs/original-research/FEATURE_CATALOG.md (row rt-f029 note only)
- docs/development-log/cloud/cloud-rt-f029-collision-fields.md

## Verified results
Audit against pinned assets (eb1514ea), final line: `COLLISION_FIELDS_OK 12 hulls f35 [21] f362 [727] pointblank 500 physicsdelay 400 repelvehicle0_terrains [10, 15] cfgs 31 pb_variants 4 pd_variants 1`.
- 12 VEH Car rows (rows 18-29): f[35]=21 and f[362]=727 in every row; no negative values.
- rogue.cfg: `[Arena] PointBlankDamageMultiplier=500` line 63; `[Vehicle] PhysicsDelay=400` line 125.
- Terrain0..15: RepelWeapons=1 in all 16; RepelVehicle=1 except Terrain10 (line 631) and Terrain15 (line 816) = 0.
- Across all 31 CFG files: PhysicsDelay=400 in all 31. PointBlankDamageMultiplier is NOT constant: 500 (rogue.cfg only), 1000 (18 files), 1250 (1), 1500 (11). RepelVehicle has 4 distinct patterns, RepelWeapons 3, so these are per-arena settings.
- Unit tests via the xunit-API shim harness (not the real dotnet test; NuGet is blocked): with assets Passed 123, Failed 0, Skipped 0; without assets Passed 41, Failed 0, Skipped 54 (asset tests skip, never pass).
- `python3 scripts/check-trackers.py`: TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).

## Not verified in cloud - local CI required
- Compilation and run under real xunit 2.9.3 / `dotnet test tests/Unit` (the shim harness approximates the xunit API).
- `scripts/verify.ps1` on Windows.
- Any gameplay meaning of these fields; no simulation was run.

## Resume point
Data clauses are complete pending review. A later gameplay branch must establish f[35]/f[362] semantics, the point-blank and repel mechanics, and whether PhysicsDelay relates to timing, using evidence beyond these raw values.
