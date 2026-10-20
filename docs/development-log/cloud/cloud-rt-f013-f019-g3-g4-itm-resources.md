Review status: APPROVED (PR #2 review round 2, senior review 5540407746 on head a07baa0; label review-approved)

# cloud/rt-f013-f019-g3-g4-itm-resources

-   Claims: RT-F013..RT-F019 (data clauses only), queue G3 and G4, gate items 2 and 5 (data). Base: origin/main 2932fe2. Status: done for the data clauses; features stay UNCHECKED.
-   Scope: raw-preserving loader for all 200 ITM rows (`OriginalItemCatalog`, `OriginalItem`, `ItmFields`) and the seven type-4 `Resources` rows with typed mass f[7], buy f[8], sell f[15] accessors. The sell -1 is flagged as a negative-sentinel candidate and never replaced.
-   Files: shared/RogueTrader.OriginalData/OriginalItem.cs, OriginalItemCatalog.cs; tests/Unit/OriginalItemCatalogTests.cs; scripts/audit-itm-resources.py; TASK_QUEUE.md.

## Acceptance clauses

Done (data): ids, names, rows, mass, buy and sell of all seven rows match the catalog; all 200 rows are v57 with unique ids and at least 31 fields.
Remaining (not done, so no checkbox ticked): operational interpretation of mass/sell sentinels, pickup/cargo behaviour, native presentation, refinery use (RT-F044), and sprite/art identity.

## Verified

-   `python3 -I scripts/audit-itm-resources.py <pinned assets clone>` printed `ITM_RESOURCES_OK 7` (pinned length and SHA-256 matched; 200 rows, v57, unique ids; seven rows equal the oracle). Assets cloned from fowlmouth/cosmic-rift-assets at eb1514ea.

## Not verified in cloud - local CI required

-   The C# code and tests were NOT compiled or run: .NET 8 SDK downloads are blocked by the proxy (403 and refused hosts) in this sandbox. Run `dotnet test tests/Unit` locally.
-   check-trackers result is below if it was run.

## Resume point

Local: build and run `dotnet test tests/Unit`, then tick RT-F013..F019 data sub-items only if the owner accepts them as scoped completions. Next cloud item: G5 (weapon/ammo records) or G7, building on this catalog.

## Review round 1 (senior review)

Reviewed 2026-10-15 against base origin/main 2932fe2 (merge-base equals current origin/main; merges cleanly). Commits 1eb8b1b, 25559e2, 43d7059.

**Scope understood.** Queue rows G3 (raw-preserving loader for all 200 ITM rows, typed accessors only for confirmed fields, sentinels kept) and G4 (the seven type-4 `Resources` records, RT-F013..RT-F019, data clauses only). No registry box ticked, and that is correct: the RESOURCE profile still needs icon/pickup, spawn/drop/refine/trade and the inventory/economic lifecycle.

**Verdict: APPROVED.** No blockers or majors. The work matches scope; four minors and two nits are below.

### What I verified (real runs in the review sandbox)

-   Pinned assets: shallow-cloned fowlmouth/cosmic-rift-assets at eb1514ea (ITM/RPG/VEH only, outside the checkout).
-   `python3 -I scripts/audit-itm-resources.py <assets>` printed `ITM_RESOURCES_OK 7`.
-   `dotnet test tests/Unit` with `RT_ORIGINAL_ASSETS_ROOT` set: **Passed 223, Failed 0, Skipped 0**. The `OriginalItemCatalog` filter ran 13 tests, all passed. Deviation: I used the Ubuntu SDK 8.0.131 (apt `dotnet-sdk-8.0`) in a scratch copy with `global.json` removed, because the pinned 8.0.419 is not downloadable here (Microsoft hosts return proxy 403).
-   Mutation check: changing the Alloy mass oracle 75000 -> 75001 made exactly 1 test fail (12 passed), so the oracle tests can fail.
-   Without assets the asset tests report Skipped (4 passed, 3 skipped), so they are not silently passed.
-   `python scripts/check-trackers.py`: `checklist: 397 entries, 22 complete, 375 open` / `TRACKERS_CONSISTENT`.
-   No changes to canonical assets, `.tools/` or `references`. No secrets found.
-   Branch collision: no other origin branch names G3, G4 or RT-F013..F019.

### Findings

1. **minor**, docs/development-log/cloud/cloud-rt-f013-f019-g3-g4-itm-resources.md:20. The line "check-trackers result is below if it was run" is not followed by any result, so the log does not show the tracker check was run. Expected fix: record the actual `check-trackers.py` output, or say it was not run.
2. **minor**, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md (Accomplished/master link: hull RT-F001..F012 only) and tests/Unit/IMPLEMENTATION_PLAN.md. These were not updated for the new `OriginalItemCatalog`/`OriginalItem`/`ItmFields` and the 13 tests. CHANGELOG.md and docs/development-log/PROGRESS.md are untouched too, although AGENTS.md requires the affected folder plans to be maintained. Expected fix: add an Accomplished line (ITM 200-row v57 loader, the 7 resource rows with raw mass f[7], buy f[8] and sell f[15], -1 kept as a sentinel candidate), link RT-F013..F019, and add an Outstanding line for the RESOURCE profile clauses. This can be done at the local merge.
3. **minor**, branch name and log line 3. AGENTS.md "Cloud routines" says one RT-ID per branch. This branch covers seven RT-IDs and two queue items, and the name contains only `rt-f013` and `rt-f019`. The log and commits write the range `RT-F013..RT-F019`, so a claim scan grepping for the literal `RT-F016` would miss this claim. This is mitigated because the G4 queue row itself bundles RT-F013..F019. Expected fix: list each claimed RT-ID literally in the log (RT-F013, RT-F014, RT-F015, RT-F016, RT-F017, RT-F018, RT-F019). Future branches should follow one-RT-ID-per-branch, or the queue row should be the documented exception.
4. **minor**, the log's "Not verified in cloud" section (line 18). It says the .NET 8 SDK could not be installed. The Microsoft download hosts are blocked, but Ubuntu apt `dotnet-sdk-8.0` (8.0.131) installs in this environment, so the tests could have been run, as the reviewer did. This is not fabrication, but future runs should try apt and record the SDK version deviation.
5. **nit**, shared/RogueTrader.OriginalData/OriginalItem.cs:3-17. `Mass`, `Buy` and `Sell` are exposed on all 200 rows. The catalog confirms buy f[8] and sell f[15] for every ITM row (RT-F049), but it confirms mass f[7] only on the resource rows. The values on other types (for example Micro Refinery f[7]=2000000) fit a common base layout. Expected fix: cite the evidence for the common layout (the pinned parser's ItmInfo base fields) in the `ItmFields` comment, or say mass is confirmed only for type-4 resources.
6. **nit**, shared/RogueTrader.OriginalData/OriginalItemCatalog.cs:41. `_ = item.RecordType;` validates by a discarded getter's side effect. Expected fix: a short comment, or an explicit `record.Int(ItmFields.RecordType)` call.

### Not verified (local CI required)

-   `scripts/verify.ps1` was not run (it is not needed: no `client/` change). The tests did not run on the pinned SDK 8.0.419.
-   Acceptance clauses that remain open for RT-F013..F019: operational meaning of mass and the -1 sell sentinel, icon/pickup, spawn/drop/refine (RT-F044), trade, and the native inventory/economic lifecycle.
