Review status: CHANGES REQUESTED (senior review round 1, PR #33)

# cloud/rt-f056-satellite-utility-production

- RT-IDs: RT-F056 (data only). Gate items 4/5. No TASK_QUEUE row. Base origin/main ac41dc9, not stacked.
- Done: `OriginalProductionItems` (shared/RogueTrader.OriginalData) lists ITM type-7 Equipment ids 46..49 (rows 170/171/172/179) and links each by raw f[93] (412/410/411/408) to exactly one VEH v53 type-5 row (rows 12/10/11/8); records stay raw. Test `OriginalProductionItemTests` (oracle: independent Python field dump of the pinned files).
- Remaining (box stays OPEN): manufacturing cost (ITM buy is 0; VEH f[6]=250/50/75/250 unexplained; NWS costs 200+10 Alloy etc. not located), deployment, defense, repair/refine behavior, stacking, verification of completed products.
- Files: shared/RogueTrader.OriginalData/OriginalProductionItems.cs, plan.d/rt-f056.md, tests/Unit/OriginalProductionItemTests.cs.
- Verified in cloud (pinned assets eb1514ea, .NET SDK 8.0.131 vs pinned 8.0.419 via local global.json tweak, not committed): new test passed; `dotnet test tests/Unit` 299 passed, 0 failed, 0 skipped. `check-trackers.py` TRACKERS_CONSISTENT.
- Not verified in cloud - local CI required: pinned SDK 8.0.419 build, scripts/verify.ps1.
- Resume: decode cost/recipe fields from NWS/VEH once field meanings are evidenced.
