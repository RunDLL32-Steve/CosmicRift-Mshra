Review status: APPROVED (senior review round 1, PR #32)

# cloud/rt-f055-g7-outpost-vehicle-records

-   Claims: queue G7 (raw loader of all 30 VEH/Rogue.veh rows, exposing the 18 non-hull rows) and RT-F055 "Rogue Trader Outpost construction" (data clause only); gate items 2, 7.
-   Base: origin/main ac41dc9. No stacking.
-   Status: done for the data clauses below (development run 2026-10-16); review pending. Box RT-F055 stays unchecked (master/catalog/checklist untouched).

## Acceptance clauses

Done (data only):

-   G7: `OriginalVehicleCatalog` loads all 30 VEH rows raw (row number, type f[0], version token, id f[2], name f[3], field count, full `RawRecord`), hash-guarded with `PinnedAssets.Veh`; exposes `CarRows` (12), `Computers` (17), `NonHullRows` (18 = 17 computers + Spectator Mode 2030), `ById`, `ByName`. Unknown fields stay raw by index.
-   The 12 type-2 rows are exactly the rows `OriginalHullCatalog` links (same row numbers, ids, names, raw lines) - cross-check test.
-   RT-F055 data: `OriginalType7Links` (ITM type-7 rows -> VEH type-5 rows via f[93]), `OriginalRogueTraderOutpost` (ITM 169 / VEH 13 / ITM 199 / eight raw 14-field blocks of VEH row 13 / hash-guarded NWS lines 106-110), `OriginalNws` raw text reader pinned to NWS/Rogue.nws (18753 bytes, sha256 7fff837b...).

Remaining (RT-F055 clauses not done, no gameplay code written): factory prerequisite (only a raw candidate: ITM f[34]/f[35] = 2023/1 on the three Manufactories, 0/0 on rows 45-49; no VEH row contains 2023), placement/collision/owner/safe-zone, cost and trading behaviour (build costs are only corroborated as raw candidates), verified completed outpost. Nothing is natively verified.

## Pinned facts (independent audit scripts/original-assets/audit_rt_f055_g7.py = C# test oracle)

-   VEH: 30 rows; rows 1-17 type 5 ids 400,401,402,403,404,405,407,408,409,410,411,412,413,414,425,426,427 (names as in the test table), rows 18-29 type 2 (ids 2000-2006, 2008-2012), row 30 type 4 id 2030 "Spectator Mode" (202 fields). Field counts: 17 x 477, 12 x 454, 1 x 202. All version v53.
-   ITM type-7: exactly 8 rows (169,170,171,172,179,196,197,198 = ids 45,46,47,48,49,83,84,85), each 94 fields, f[93] = 413,412,410,411,408,425,426,427 = the id of exactly one VEH type-5 row (8 of 8). The link is NAMED (`ItmType7Fields.VehicleIdField`) on that evidence, not on documentation.
-   Row 169: type 7, id 45, "Trading Post", mass f7 0, buy f8 5000, sell f15 5000 - catalog claim buy5000/sell5000 CONFIRMED. VEH 13: id 413, 477 fields, f5 "Player Created Trading Post". ITM 199: type 4, id 2023, 31 fields, buy 5000, sell 2500. Catalog "NWS106-110 purchase/place Home/teamtrade" CONFIRMED (106 purchase at the SpaceDock, 108 place and activate with Home, 110 trade refined minerals).
-   VEH 13 blocks at f[253+14k]: Trade Tellerium (f+1=18 Tellerium Credits, f+5=2004), Trade Krandazite (17, 2006), Trade Wreckage (19, 2008), Trade Alloy (44, 2020), Build Gunsat (47; f+2=1, f+5=2020 Alloy, f+6=10, f+7=200), Build BeamSat (48; 20/400), Build Micro Refinery (46; 20/500), Build Micro Repair Dock (49; 20/500). NWS lines 114/116/118/120 state 200cr+10 Alloy, 400+20, 500+20, 500+20, matching f+7/f+6; exposed as raw `Raw6`/`Raw7` candidates only.

## Mismatches / notes

-   Assignment text said Solar Mold has 478 fields: that is a naive comma split. Its quoted description f[5] contains a comma; the quote-aware reader (same as the hull loader) counts 477, so all 17 computer rows have 477 fields. Test pins 477 and also asserts the 478-piece naive split and the quoted comma.
-   Catalog wording "Nanobot Control 2023 row199" is correct (type 4, not type 7).
-   Type-7 f[7] is nonzero on several rows with buy 0 (Micro Refinery 2000000, GunSat/BeamSat 500000); the "Mass" label of `ItmFields.Mass` may not describe type-7 rows. Not changed; observation only.

## Files touched

-   shared/RogueTrader.OriginalData/OriginalVehicleCatalog.cs (new), OriginalOutpost.cs (new), plan.d/rt-f055-g7.md (new)
-   tests/Unit/OriginalVehicleCatalogTests.cs (new, 6 tests), tests/Unit/OriginalOutpostTests.cs (new, 7 tests)
-   scripts/original-assets/audit_rt_f055_g7.py (new)
-   docs/development-log/cloud/cloud-rt-f055-g7-outpost-vehicle-records.md; TASK_QUEUE.md (G7 row CLAIMED -> REVIEW only)
-   Existing merged code (OriginalHullCatalog, OriginalTradingPost, OriginalRefinery) unchanged; only their constants reused.

## Verified

-   Offline xunit-compatible shim, apt SDK 8.0.131 (not the pinned 8.0.419 / real xunit), pinned assets at /tmp/claude-0/assets/repo present: focused OriginalVehicle* 6/6 passed, OriginalOutpost* 7/7 passed; full suite 311 passed, 0 failed, 0 skipped (main baseline 298, +13 new).
-   `python3 scripts/original-assets/audit_rt_f055_g7.py <assets root>` run; its output matches every pinned value.
-   `python3 scripts/check-trackers.py`: TRACKERS_CONSISTENT (397 entries, 22 complete, 375 open).

## Not verified in cloud - local CI required

-   Pinned SDK 8.0.419 real xunit run of `dotnet test tests/Unit` (only the offline shim was run).
-   Any BLO/LVL-dependent check (none used here).
-   All native/in-game behaviour: purchase at the SpaceDock, placing with Home, trading, satellite production, factory prerequisite, owner/safe-zone/collision.

## Resume point

Next RT-F055 work needs native evidence: decode what VEH f[34]/f[35]-style prerequisite and the type-5 field layout mean (compare the other type-5 rows), then placement/collision/owner/safe-zone and cost/trade flow; do not tick the box until a completed outpost is verified.

## Coordinator review (development run 2026-10-16 14:20 UTC)

Implementation delegated to a Sonnet 5.5 High agent. The coordinator read the full diff, independently confirmed the 8-of-8 ITM type-7 f[93] -> VEH type-5 id link with its own Python read (45->413, 46->412, 47->410, 48->411, 49->408, 83->425, 84->426, 85->427), re-ran the audit script (exit 0) and the whole unit suite: offline xunit-compatible shim, apt SDK 8.0.131, pinned assets eb1514ea -> Passed 311, Failed 0, Skipped 0 (main baseline 298). check-trackers: TRACKERS_CONSISTENT. Note for merge order: PR for cloud/rt-f026-keys-action-tokens also pins NWS/Rogue.nws (same size/hash) in its own class; harmless duplication, can be unified after both merge. Real xunit run on pinned SDK 8.0.419: not verified in cloud - local CI required.
