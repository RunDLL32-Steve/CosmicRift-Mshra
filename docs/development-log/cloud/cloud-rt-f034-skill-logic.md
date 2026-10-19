Review status: APPROVED (PR #3 review round 2, senior review 5540408041 on head 0264954; label review-approved)

# cloud/rt-f034-skill-logic

-   Claims: RT-F034 (grammar parser + audit of the ITM f[5] logic strings; evaluator semantics NOT decided). Gate items 2 and 6 (data). Stacked on origin/cloud/rt-f013-f019-g3-g4-itm-resources (APPROVED; provides OriginalItemCatalog).
-   Status: done for the scoped part. No registry box ticked.

## Acceptance clauses

Done (data): grammar recovered from the 53 distinct f[5] strings of the pinned Rogue_mp.itm (200 rows, 120 empty); parser + exact round-trip + evaluation hook; ambiguity (| mixed with &) rejected.
Remaining: exact `%` meaning, negation/implicit/empty semantics against original evaluator evidence (inferences marked), CFG ExitSpectatorLogic and other logic fields, allowed/forbidden loadout verification, native verification.

## Files

shared/RogueTrader.OriginalData/OriginalSkillLogic.cs, tests/Unit/OriginalSkillLogicTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)

-   apt dotnet-sdk-8.0 8.0.131 (global.json moved aside temporarily, not committed), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 239, Failed 0, Skipped 0.

## Not verified in cloud - local CI required

-   Pinned SDK 8.0.419 run; original evaluator behaviour.

## Resume point

Next: find evaluator evidence (pinned InfantryOnline server source, local) to decide %n/implicit/empty; then RT-F035 loadouts.

## Review round 1 (senior review)

Reviewer: senior review routine (did not author this work). Base origin/cloud/rt-f013-f019-g3-g4-itm-resources is APPROVED (a07baa0); diff reviewed against that base.

Scope understood: RT-F034 grammar parser and exact round-trip audit of the ITM f[5] logic strings, with an evaluation hook whose semantics are explicitly inferred. No registry box ticked; Remaining (exact `%`, negation/implicit/empty semantics from the original evaluator, CFG ExitSpectatorLogic, allowed/forbidden loadouts, native) listed in the log and IMPLEMENTATION_PLAN. Branch name carries RT-F034, log lists it, no collision.

Verdict: APPROVED.

Findings:

1. minor - shared/RogueTrader.OriginalData/OriginalSkillLogic.cs:80-84: any token that is not `|`, `&` or `)` after a term is taken as an implicit join, so the parser accepts unevidenced forms such as `1!2`, `%1%2`, `1(2)`, `(1)(2)` and `1&2(3)`. The pinned data has exactly one implicit pattern, `)!` (6 distinct strings), and the class doc says the implicit operator sits "between a parenthesised group and a following term". This contradicts the parser's own stated rule of rejecting what is not evidenced (as it does for mixed `|`/`&`). Expected fix: accept an implicit join only after a `)` (optionally only before `!`), and add `1!2`, `%1%2` and `1(2)` to `RejectsMalformedOrUnevidenced`.
2. nit - OriginalSkillLogic.cs:113 `Number()` is greedy, so `%12` is %12, not %1 followed by implicit 2; harmless (only `%1` occurs, 14 strings) but note it in the doc as a choice.
3. nit - "verbatim" round-trip is against `Record.Value(5)` (quotes/padding removed), not the raw field bytes; the raw record is still preserved by the catalog, so just say "unquoted value" in the doc.
4. nit - RT-F034 has no TASK_QUEUE row (the queue lists G/M/C items only); the log explains the gate mapping, acceptable, but add the RT-ID to a queue row note when the owner merges so the claim stays visible.

Evidence check: `(2|8|15|16)!%1` and `(2|8|15|16)&!%1` both occur in the pinned file, which is consistent with (not proof of) the implicit = AND inference; correctly kept as inference.

Verified by the reviewer (cloud):

-   Independent Python audit of pinned Rogue_mp.itm (eb1514ea): 200 rows, 53 distinct f[5] strings, 120 empty, only `%1` used, only `)!` implicit joins - matches the test assertions.
-   apt dotnet-sdk-8.0 8.0.131, global.json removed in a scratch worktree only, RT_ORIGINAL_ASSETS_ROOT set: `dotnet test tests/Unit` -> Passed 239, Failed 0, Skipped 0 (matches the log).
-   `python scripts/check-trackers.py` -> TRACKERS_CONSISTENT (397/22/375).
-   No canonical asset, .tools/references, secret or unrelated changes.

Not verified in cloud - local CI required: pinned SDK 8.0.419 run; original evaluator semantics (needs the pinned InfantryOnline server source locally).
