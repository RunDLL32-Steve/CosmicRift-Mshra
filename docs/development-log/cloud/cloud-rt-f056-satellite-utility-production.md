Review status: APPROVED (senior review round 2, PR #33)

# cloud/rt-f056-satellite-utility-production

-   RT-IDs: RT-F056 (data only). Gate items 4/5. No TASK_QUEUE row. Base origin/main ac41dc9, not stacked.
-   Done: `OriginalProductionItems` (shared/RogueTrader.OriginalData) lists ITM type-7 Equipment ids 46..49 (rows 170/171/172/179) and links each by raw f[93] (412/410/411/408) to exactly one VEH v53 type-5 row (rows 12/10/11/8, in that item order; VEH f[6] = 250/50/75/250). Records stay raw. Test `OriginalProductionItemTests`; evidence script `scripts/original-assets/audit_rt_f056.py` (read-only).
-   Cost evidence (corrected in rework round 1): VEH id 413 "Rogue Trader Outpost" (row 13) has Build blocks at f[253+14k], k=4..7: items 47/48/46/49, f+5 = 2020 (Alloy), f+6 = 10/20/20/20, f+7 = 200/400/500/500, equal to NWS 114/116/118/120. These are raw cost candidates (f+6 alloy count, f+7 credits); PR #32 (`OriginalRogueTraderOutpost`) exposes them. Cost semantics and production flow remain open.

## Catalog clauses (box stays OPEN)

| Clause                                        | Status                                                                                                                 |
| --------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Decode outpost manufacturing fields and costs | partial: item/VEH identities done; cost candidates located in VEH 413 Build blocks (raw, via #32); semantics undecoded |
| Inventory package masses                      | raw ITM f[7] kept (2000000/500000/500000/2000000), meaning undecoded, remaining                                        |
| Deployment                                    | remaining                                                                                                              |
| Defenses (GunSat/BeamSat)                     | remaining                                                                                                              |
| Repair/refining                               | remaining                                                                                                              |
| Stacking limits                               | remaining                                                                                                              |
| Verification of completed products            | remaining (needs native/behavior)                                                                                      |

-   Files: shared/RogueTrader.OriginalData/OriginalProductionItems.cs, plan.d/rt-f056.md, tests/Unit/OriginalProductionItemTests.cs, scripts/original-assets/audit_rt_f056.py.
-   Verified in cloud (pinned assets eb1514ea, SDK 8.0.131 via uncommitted global.json tweak): `dotnet test tests/Unit` 299 passed before rework; after rework 299 passed, 0 failed, 0 skipped. `check-trackers.py` TRACKERS_CONSISTENT.
-   Not verified in cloud - local CI required: pinned SDK 8.0.419, scripts/verify.ps1.
-   Resume: decode cost/mass semantics against PR #32 once merged.

## Rework after round 1

-   major (costs "not located"): fixed, see Cost evidence above and plan.d.
-   minor (audit script not committed): committed scripts/original-assets/audit_rt_f056.py and cited it.
-   minor (order-insensitive VEH rows): test pins 12,10,11,8 and VEH f[6] = 250/50/75/250.
-   minor (thin clause list / package masses): clause table added.
