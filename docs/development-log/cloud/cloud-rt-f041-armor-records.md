Review status: READY FOR RE-REVIEW

# cloud/rt-f041-armor-records

- Claims: RT-F041 (data clause only: armor rows and Remove Armor resolution); gate items 2 and 5 (data). No queue row.
- Stacked on cloud/rt-f039-stun-emp-fields (UNREVIEWED), which is on APPROVED cloud/rt-f040-mirv-launcher-chain and the G3/G4/G5 branches. Merge in that order; this branch touches no file of the F039 branch except the shared folder plan (append-only, may conflict trivially).
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): independent Python audit of pinned Rogue_mp.itm: exactly 9 type-13/137-field "Hull Armor" rows (rows 21,22,23,63,66,108,109,136,137; ids 4,5,6,13,16,23,24,37,38). The catalog text lists rows 20-23, but row 20 is HyperBoosters (Engine Mods); the armors start at row 21 and 108/109/136/137 were not listed. Remove Armor (id 41, row 151) lists (armor id, 42) pairs from f[31] for all nine armors; each resolves, effect 42 = ArmorRemoveHack row 152. f[42..44] hold negative per-armor values whose order follows the help-text protection grades for most but not all rows (Sillicate "no protection versus Energy" = -25), so they are exposed unnamed.
Remaining: damage-type scale and hit order, meaning/sign of f[42..44], NWS row 131-134/323 confirmation flow, paid removal, upgrade restrictions, simulation and native verification.

## Files
shared/RogueTrader.OriginalData/OriginalArmor.cs, tests/Unit/OriginalArmorTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 with global.json temporarily moved aside (restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 248, Failed 0, Skipped 0 (245 + 3 new).
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run.

## Resume point
Next unclaimed registry entry in order: RT-F042 (LIO type-5 mineral spawns; needs the LIO reader, claimed by G6) or RT-F047/F048/F049 item-table audits.

## Review round 1 (senior review)

Scope understood: RT-F041 data clause only. List the nine Hull Armor rows with raw f[42..44], and resolve the Remove Armor (id 41, row 151) pair list to the armors and ArmorRemoveHack (id 42, row 152). The NWS rows, damage scale and the paid-removal flow remain open. Stacked on cloud/rt-f039-stun-emp-fields (APPROVED in this run).

Verdict: CHANGES REQUESTED (1 major).

Findings:
1. major - shared/RogueTrader.OriginalData/OriginalArmor.cs:4-8 and :28-32, and the log's "Acceptance clauses / Done" line. The code documents f[42..44] as "candidate kinetic/explosive/energy modifiers ... ordered consistently with the grade". The log calls the order one that "follows the help-text protection grades for most but not all rows". The pinned data contradicts this interpretation.
   (a) f[42..44] is a shared field block of every type-13/137 row, not an armor-specific one. Engine Mods: SuperBoosters, whose help says "boosts Top Speed and HyperThrust", carries 80,100,0. TurboBoosters carries 50,50,0. HyperBoosters carries 80,150,0. Turbo/Super/Hyper AfterBurners, whose help says "boost HyperThrust", carry 0,75,0 / 0,125,0 / 0,180,0. Thruster Mods: Turbo/Super Reaction Thrusters, whose help says "enhances your basic thrust rate", carry 0,0,50 / 0,0,100, and Bio Reactor Pods carry 0,0,150. Read across the families, f[42] tracks top speed, f[43] HyperThrust and f[44] thrust. The armors' negatives therefore look like speed/thrust penalties, not damage resistances. This is also unvalidated, but it is the reading the data supports.
   (b) The damage-grade mapping fails outright for rows the branch itself lists. Ablative Shell row 136 ("good protection versus Energy ... no protection versus Kinetic") has -75,-75,-20, identical to Duraplas row 21 ("Medium ... Kinetic"). Absorption Shell row 137 ("Excellent protection versus Energy ... no protection versus Kinetic") has -100,-100,-35, identical to Tiathamate row 66. The most negative value sits at "kinetic" exactly where the help text says there is none.
   (c) "in the five armors whose help text gives a protection grade" is false: all nine armor rows give grades in their help text.
   The fidelity rules (AGENTS.md: interpret semantics with evidence, keep unexplained behavior visible, never fabricate) forbid shipping an interpretation that the same file's data refutes, even one labelled "candidate".
   Expected fix: remove the kinetic/explosive/energy wording from the class and property summaries and from the log. Document f[42..44] as a type-13 modifier block shared with Engine Mods and Thruster Mods, with the observed per-family pattern and no semantic decision. State that the protection grades in the help text are not stored in f[42..44] and that their storage is undecoded. Add an oracle test pinning at least one non-armor type-13 row's f[42..44] (for example SuperBoosters row 18 = 80,100,0, Turbo Reaction Thrusters row 131 = 0,0,50) and the Ablative/Duraplas equality, so the contradiction stays visible. Update the folder IMPLEMENTATION_PLAN line to match.
2. minor - the log's "Done" line says "The catalog text lists rows 20-23, but row 20 is HyperBoosters (Engine Mods)". This is correct and useful, but the catalog row itself (FEATURE_CATALOG.md RT-F041) still says rows 20-23. Expected fix: either correct the catalog text to rows 21-23, 63, 66, 108, 109, 136, 137 (a factual correction of a research note, not a status change), or record it as an open catalog erratum in the log so the next documentation pass picks it up.
3. nit - OriginalRemoveArmor.Pairs stops at the first armor id of 0 and ignores the rest of the row. This is fine for the pinned row 151 (pairs at f[31..48], zeros afterwards to f[62]), but a test asserting that every field after the terminator is 0 would keep a silent truncation from hiding data.

What is correct: the nine Hull Armor rows (21, 22, 23, 63, 66, 108, 109, 136, 137; ids 4, 5, 6, 13, 16, 23, 24, 37, 38), the buy prices, the raw f[42..44] values of the first five armors, and the Remove Armor pairs (37, 38, 23, 5, 4, 24, 6, 13, 16, each with 42) all match an independent Python audit. Every armor has sell 0.

Branch name, claims and collisions: the name carries RT-F041 and the log claims RT-F041. No other branch names RT-F041. No queue row.

Verified in cloud (this review):
- apt dotnet-sdk-8.0 8.0.131 in a detached scratch worktree, global.json moved aside and restored (not committed), RT_ORIGINAL_ASSETS_ROOT = pinned assets eb1514ea: dotnet test tests/Unit -> Passed 248, Failed 0, Skipped 0. This matches the log.
- python scripts/check-trackers.py -> TRACKERS_CONSISTENT.
- An independent Python audit of all type-13 rows produced the evidence in finding 1. No canonical asset, .tools/references, secret or unrelated changes.

Not verified (local CI required): a run with the pinned SDK 8.0.419; damage scale/order, the NWS 131-134/323 flow, paid removal and simulation/native behavior. These remain open by design.

## Rework after round 1
1. major (f[42..44] interpretation) - fixed. The kinetic/explosive/energy wording is removed from the class and property summaries; f[42..44] is now documented as a type-13 modifier block shared with Engine and Thruster Mods, with no semantic decision, and the help-text protection grades are stated not to be stored there (storage undecoded). New oracle test `ModifierBlockIsSharedWithNonArmorType13RowsAndDoesNotEncodeProtectionGrades` pins SuperBoosters row 18 = 80,100,0, Turbo Reaction Thrusters row 131 = 0,0,50, Ablative(136)==Duraplas(21) = -75,-75,-20 and Absorption(137)==Tiathamate(66) = -100,-100,-35. Folder IMPLEMENTATION_PLAN line updated. The earlier "Done" wording about "help-text order for most rows" is superseded by this section.
2. minor (catalog rows) - fixed: FEATURE_CATALOG.md RT-F041 text corrected to rows 21-23,63,66,108,109,136,137 (row 20 is HyperBoosters); research-note correction only, no status change.
3. nit (pair terminator) - fixed: test `RemoveArmorRowHoldsNothingAfterThePairTerminator` asserts every field after the pairs is 0.

Verified in cloud: apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), RT_ORIGINAL_ASSETS_ROOT = pinned eb1514ea: dotnet test tests/Unit -> Passed 250, Failed 0, Skipped 0. check-trackers -> TRACKERS_CONSISTENT. Not verified: pinned SDK 8.0.419 (local CI required).

## Conflict merge 2026-10-16

- Merged base tip 945df8d (origin/cloud/rt-f039-stun-emp-fields, updated after its own base conflict merge).
- Conflict: shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md (this branch had appended its RT-F041 status line there; the base moved the shared lines to plan.d and no longer carries that section).
- Resolution: kept the base's IMPLEMENTATION_PLAN.md unchanged; moved the RT-F041 status line to shared/RogueTrader.OriginalData/plan.d/rt-f041.md. No other conflicts or shared-tail appends.
- Tests: Passed 266, Failed 0, Skipped 0, Total 266 (offline xunit-compatible shim runner, SDK 8.0.131; pinned SDK 8.0.419 / real xunit not verified in cloud - local CI required).
- python3 scripts/check-trackers.py: TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).
