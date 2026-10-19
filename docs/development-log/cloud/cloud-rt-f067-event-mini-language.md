Review status: APPROVED (senior review round 1, PR #36)

# cloud/rt-f067-event-mini-language

-   RT-IDs: RT-F067 (data only). Gate items 2/5. Base origin/main c81807c, not stacked. Claimed by the 18:25 UTC cloud run (claim commit only); resumed and completed by the 22:15 UTC cloud run of the same routine.
-   Scope: raw-preserving tokenizer for original event/action strings and a hash-verified inventory of every event slot in the selected package, with verbs counted and item/event-call arguments resolved; unknown operators kept visible.

## Acceptance clauses

-   Done (data): every event slot read: VEH f[129..131] (EventString1..3; position from the upstream InfantryOnline bb97579e VehInfo.cs field order, confirmed by quoting in all 30 rows) = 90 slots, 12 non-empty (hull rows 18-29, all in f[129]: addinv=Plasma Cannon x4, Bio Cannon x4, D-Cannon x4); CFG [Event] 20 keys (13 non-empty, lines 2492-2511), [TerrainN] EventString 16, [TeamInfoN] EventString/TeamEventStartGame 100, ITM type-6 f[147] 92 (all empty). 318 slots, 25 non-empty, 48 actions, 19 exact verb spellings (16 case-folded), 0 unknown tokens.
-   Observed grammar: actions separated by bare `,`; action = `verb` or `verb=argument`; `=` the only operator; no condition operators, `:` quantities or `pick` lists occur in the original data (the tokenizer keeps them as Unknown tokens, never guesses).
-   Visible findings: verb casing varies (WipeInv/Wipeinv/wipeinv; Event/event prefixes); `wreckage` resolves to ITM 2008 Wreckage only case-insensitively; `null wreckage` resolves to no ITM row (consistent with RT-F059's finding); all 9 `event<Name>` calls resolve to [Event] keys exactly. Upstream Assets.Events.cs lower-cases and splits at the first `=`; upstream Actions.cs splits on every `=` and lacks 6 of the 16 folded verbs. These are upstream-emulator interpretations, not original-executable evidence.
-   Remaining (box stays OPEN): verb semantics, targets, ordering and transactions, case rules, condition operators, LIO trap/warp strings (LIO reader is queue G6, claimed elsewhere; a Python grep found no `=` in LIO/Rogue.lio), native behaviour.

## Files

-   shared/RogueTrader.OriginalData/OriginalEventStrings.cs
-   tests/Unit/OriginalEventStringsTests.cs
-   scripts/original-assets/audit_rt_f067.py
-   shared/RogueTrader.OriginalData/plan.d/rt-f067.md

## Results

-   NuGet and dot.net are blocked by the cloud egress policy this run; xunit tests were executed with an xunit-compatible reflection shim (SDK 8.0.131 via uncommitted global.json tweak, pinned assets eb1514ea): passed 283, failed 0, skipped 5 (38 new tests all pass; the 5 skips pre-exist). Coordinator re-ran: same.
-   audit_rt_f067.py (independent Python): 48 actions, 19 exact / 16 folded verbs, 0 unknown actions; matches the C# oracle.
-   check-trackers: TRACKERS_CONSISTENT.
-   Not verified in cloud - local CI required: real xunit `dotnet test tests/Unit` with pinned SDK 8.0.419, scripts/verify.ps1.

## Status

done (data clause); resume point: verb semantics need the native client/server or a dated original help source; LIO strings after G6 merges.
