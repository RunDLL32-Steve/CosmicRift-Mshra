Review status: UNREVIEWED

# cloud/rt-f034-skill-logic

- Claims: RT-F034 (grammar parser + audit of the ITM f[5] logic strings; evaluator semantics NOT decided). Gate items 2 and 6 (data). Stacked on origin/cloud/rt-f013-f019-g3-g4-itm-resources (APPROVED; provides OriginalItemCatalog).
- Status: done for the scoped part. No registry box ticked.

## Acceptance clauses
Done (data): grammar recovered from the 53 distinct f[5] strings of the pinned Rogue_mp.itm (200 rows, 120 empty); parser + exact round-trip + evaluation hook; ambiguity (| mixed with &) rejected.
Remaining: exact `%` meaning, negation/implicit/empty semantics against original evaluator evidence (inferences marked), CFG ExitSpectatorLogic and other logic fields, allowed/forbidden loadout verification, native verification.

## Files
shared/RogueTrader.OriginalData/OriginalSkillLogic.cs, tests/Unit/OriginalSkillLogicTests.cs, shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 (global.json moved aside temporarily, not committed), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 239, Failed 0, Skipped 0.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run; original evaluator behaviour.

## Resume point
Next: find evaluator evidence (pinned InfantryOnline server source, local) to decide %n/implicit/empty; then RT-F035 loadouts.
