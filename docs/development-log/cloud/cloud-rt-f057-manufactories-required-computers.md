Review status: READY FOR RE-REVIEW

# cloud/rt-f057-manufactories-required-computers

- RT-IDs: RT-F057 (data only). Gate items 4/5. Base origin/main ac41dc9, not stacked (independent of RT-F056 PR #33).
- Done: `OriginalManufactories` (shared/RogueTrader.OriginalData): ITM ids 83/84/85 (rows 196/197/198) -> VEH v53 type-5 rows 15/16/17 via f[93]; required computer ITM f[34]=2023 -> row 199 Nanobot Control System (f[35]=1 raw); 14-field recipe blocks from VEH f[253] (name, output id f+1/qty f+2, raw f+4=150, input id f+5/qty f+6): 9 / 5 / 2 blocks. Evidence: independent Python dump of the pinned files; block layout confirmed by names such as "Produce GunSat 1:25 A" (47 x1 from 25 Alloy 2020).
- Required computer: ITM f[34]=2023 = Nanobot Control System (row 199) is consistent with NWS 224 ("building requires a Nanobot Control System") and the catalog row. NWS 222 ties Advanced Fire Control to using missiles/beam weapons, not to the manufactories; the open item is that no ITM row is named "Advanced Fire Control" (0 matches, asserted by test).
- Other mismatches preserved visibly: Biostorm input quantity -1 vs "1:1"; Pulse Cannon Slugs output id 2021 has no ITM row; ITM f[5] on Munitions is a raw "!1&!2&..." string.
- Additional fact: the NWS 10/20 Alloy (+200..500 credits) costs belong to the Rogue Trader Outpost Build blocks (VEH 413, f+6/f+7, RT-F056); the 25/35 Alloy GunSat/BeamSat recipes here are a separate production path.
- Remaining (box stays OPEN): factory recipe logic/costs (f+4=150 meaning), build time, range, native requirement enforcement, completed-product verification.
- Files: shared/RogueTrader.OriginalData/OriginalManufactories.cs, plan.d/rt-f057.md, tests/Unit/OriginalManufactoryTests.cs.
- Verified in cloud (pinned assets eb1514ea, SDK 8.0.131 via uncommitted global.json tweak): `dotnet test tests/Unit` 300 passed, 0 failed, 0 skipped; check-trackers TRACKERS_CONSISTENT.
- Not verified in cloud - local CI required: pinned SDK 8.0.419, scripts/verify.ps1.
- Resume: decode f+4 and cost fields; add RT-F056 cost evidence once reviewed.

## Rework after round 1 (APPROVED with 4 minors)
- All 16 blocks pinned (name, out id/qty, in id/qty, f+7) plus the three VEH names; new RawField7.
- Block loop: any non-empty name slot after the last block now throws OriginalDataException.
- Wording corrected (AFC vs Nanobot, 10/20 vs 25/35 Alloy); test asserts no ITM named Advanced Fire Control.
- `dotnet test tests/Unit` 301 passed, 0 failed, 0 skipped; TRACKERS_CONSISTENT.
