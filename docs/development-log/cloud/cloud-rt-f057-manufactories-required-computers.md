Review status: APPROVED (senior review round 1, PR #34)

# cloud/rt-f057-manufactories-required-computers

- RT-IDs: RT-F057 (data only). Gate items 4/5. Base origin/main ac41dc9, not stacked (independent of RT-F056 PR #33).
- Done: `OriginalManufactories` (shared/RogueTrader.OriginalData): ITM ids 83/84/85 (rows 196/197/198) -> VEH v53 type-5 rows 15/16/17 via f[93]; required computer ITM f[34]=2023 -> row 199 Nanobot Control System (f[35]=1 raw); 14-field recipe blocks from VEH f[253] (name, output id f+1/qty f+2, raw f+4=150, input id f+5/qty f+6): 9 / 5 / 2 blocks. Evidence: independent Python dump of the pinned files; block layout confirmed by names such as "Produce GunSat 1:25 A" (47 x1 from 25 Alloy 2020).
- Mismatches preserved visibly (open questions): catalog text says Advanced Fire Control is the required computer, the selected record names Nanobot Control System; Biostorm input quantity -1 vs "1:1"; Pulse Cannon Slugs output id 2021 has no ITM row; ITM f[5] on Munitions is a raw "!1&!2&..." string.
- Finding relevant to RT-F056: Defense Manufactory blocks give GunSat 1:25 Alloy and BeamSat 1:35 Alloy (NWS historical help said 10/20 Alloy).
- Remaining (box stays OPEN): factory recipe logic/costs (f+4=150 meaning), build time, range, native requirement enforcement, completed-product verification.
- Files: shared/RogueTrader.OriginalData/OriginalManufactories.cs, plan.d/rt-f057.md, tests/Unit/OriginalManufactoryTests.cs.
- Verified in cloud (pinned assets eb1514ea, SDK 8.0.131 via uncommitted global.json tweak): `dotnet test tests/Unit` 300 passed, 0 failed, 0 skipped; check-trackers TRACKERS_CONSISTENT.
- Not verified in cloud - local CI required: pinned SDK 8.0.419, scripts/verify.ps1.
- Resume: decode f+4 and cost fields; add RT-F056 cost evidence once reviewed.
