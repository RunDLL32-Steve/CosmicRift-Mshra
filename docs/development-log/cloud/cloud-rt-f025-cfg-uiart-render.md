Review status: CHANGES REQUESTED (senior review round 1, PR #28)

# cloud/rt-f025-cfg-uiart-render

- Claims: RT-F025 (Original HUD/store/selection art); gate items 2, 5. Not claimed: RT-F378 (Logo=cruiart,logo has no exact member) - noted only.
- Base: origin/main ac41dc9.
- Status: done for the data-only slice below; RT-F025 box stays OPEN (nothing ticked in master/catalog/checklist).

## Acceptance clauses
Done (data only, raw and uninterpreted):
- Typed raw view of CFG `[Uiart]` (header line 2728, 27 entries lines 2729-2755): each key -> `archive,member` with line number; `NotepadBorder=` (line 2750) kept explicitly empty; malformed values (not empty and not exactly two non-empty whitespace-free comma tokens) throw. 25 entries use `cruiart`, `Portal=utilityeffects,portalgraphic` (2746) is the only other archive. `Logo=cruiart,logo` is line 2752 (matches catalog RT-F378).
- `[Uiwav]` (header 2757, lines 2758-2765): same shape, 8 keys, all `crwav`.
- `[Render]` (header 2184, lines 2185-2191): Static=0, BackgroundColor=0x000000 (text kept, hex parsed to 0, no channel interpretation), FlattenLights=0, TrailsFirst=1, VehTrailsFirst=1, StarfieldGapNear=250, StarfieldGapFar=350; units/meaning not interpreted.
- Catalog row numbers verified against the file: 2186 (BackgroundColor), 2190-2191 (star gaps), 2729-2755 (Uiart) all match; no mismatch.
- BLO filename existence (case-insensitive, `BLO/<archive>.blo`, clone lists 89 BLO files): cruiart, utilityeffects (file UtilityEffects.blo) and crwav are all listed. Member existence NOT checked (BLO content LFS-only).

Remaining (open): UI record geometry, fonts/colors, gauges, panels, chat, selection and store behavior, member existence/decoding of the referenced BLO records, native/original comparison evidence, RT-F378 logo fallback.

## Files touched
- shared/RogueTrader.OriginalData/OriginalUiArt.cs (new; uses hash-guarded `OriginalCfg.LoadRogue`)
- tests/Unit/OriginalUiArtTests.cs (new; 13 test cases per the shim: 9 synthetic incl. theory rows, 4 oracle)
- scripts/original-assets/audit_rt_f025.py (new, stdlib; prints the pinned values and BLO filename check; run against /tmp/claude-0/assets/repo, output matched all test oracle values)
- shared/RogueTrader.OriginalData/plan.d/rt-f025.md (new)

## Verified (cloud)
- `rt-shim-test.sh ... OriginalUiArt`: Passed 13, Failed 0, Skipped 0, Total 13 (oracle tests ran against the pinned clone).
- Full suite: Passed 311, Failed 0, Skipped 0, Total 311 (main baseline 298). Offline xunit-compatible shim, apt SDK 8.0.131, not the pinned 8.0.419/real xunit.
- `python3 scripts/check-trackers.py`: TRACKERS_CONSISTENT (checklist 397 entries, 22 complete, 375 open).

## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 real xunit run (and warning-clean build under it).
- Any BLO/LVL-dependent check (member existence of cruiart/utilityeffects/crwav records, sprite geometry).
- Native behaviour: how the original client uses these references, version/fallback (RT-F378), fonts/colors/gauges/panels/chat/selection/store, and any comparison against original footage.

## Resume point
Decode the `cruiart` BLO records referenced by `[Uiart]` once BLO content is available locally (member existence, frame geometry), then compare against native/original captures; only then consider ticking RT-F025.

## Coordinator review (development run 2026-10-16 14:20 UTC)
Implementation delegated to a Sonnet 5.5 High agent (matrix: data loaders from approved specs). The coordinator read the full diff, spot-checked the pinned values with its own Python read of the pinned files, re-ran the branch's audit script (exit 0) and re-ran the whole unit suite: offline xunit-compatible shim, apt SDK 8.0.131, RT_ORIGINAL_ASSETS_ROOT = pinned clone eb1514ea -> Passed 311, Failed 0, Skipped 0 (main baseline 298). check-trackers: TRACKERS_CONSISTENT. Accepted for senior review; no registry box ticked. The real xunit run on the pinned SDK 8.0.419 is not verified in cloud - local CI required (nuget.org is blocked here).
