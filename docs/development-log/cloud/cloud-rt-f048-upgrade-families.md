Review status: APPROVED (PR #14 review round 2, senior review 5545706376 on head bd0c415; label review-approved)

# cloud/rt-f048-upgrade-families

- Claims: RT-F048 (data clause only: raw listing of the Engine/Thruster/Power Mods and HyperDrive Systems upgrade rows); gate items 2 and 5 (data). No queue row.
- Stacked on cloud/rt-f041-armor-records (PR #8; armor family already listed there, shares the type-13 modifier block finding) <- ... <- cloud/rt-f013-f019-g3-g4-itm-resources (PR #2). Merge in that order.
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): independent Python audit of pinned Rogue_mp.itm: Engine Mods rows 18,19,20,64,65,193,194,195; Thruster Mods 24,25,92,131,132,133; Power Mods 26,27,28,29,91,93; HyperDrive Systems 110 (Hyper Jump), 111 (HyperCore, type 4), 116-119 (Beacon, Controlled Jump, Recall, Gate; type 17). 20 type-13 rows carry f[42..44] (boosters f[42]/f[43], afterburners f[43], reaction thrusters f[44]; maneuver thrusters and power mods hold 0,0,0, so their effect is stored elsewhere, undecoded). Catalog erratum: engine "rows 17-19" are rows 18-20 and "193-195" is right; thrusters "24-25/131-133", power "26-29/93" are right; the catalog omits rows 64, 65, 91, 92.
Remaining: effect fields of capacitors/chargers/maneuver thrusters, composition/stacking/exclusivity, skills (raw logic only), slots, visual states, warp prerequisites and targeting, simulation and native verification.

## Files
shared/RogueTrader.OriginalData/OriginalUpgrade.cs, tests/Unit/OriginalUpgradeTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 253, Failed 0, Skipped 0. Without assets: Passed 195, Skipped 34, Failed 0.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
RT-F046 (SpaceDock store: needs LIO/CFG readers) and RT-F050+ (CFG-heavy; C2 CFG reader unclaimed prerequisite) are next.

## Conflict merge 2026-10-16

- Merged base tip 82d285c (origin/cloud/rt-f041-armor-records, updated after its own base conflict merge).
- Conflict: shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md (this branch had appended its RT-F048 status line there; the base moved the shared lines to plan.d and no longer carries that section).
- Resolution: kept the base's IMPLEMENTATION_PLAN.md unchanged; moved the RT-F048 status line to shared/RogueTrader.OriginalData/plan.d/rt-f048.md. No other conflicts or shared-tail appends.
- Tests: Passed 269, Failed 0, Skipped 0, Total 269 (offline xunit-compatible shim runner, SDK 8.0.131; pinned SDK 8.0.419 / real xunit not verified in cloud - local CI required).
- python3 scripts/check-trackers.py: TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).
