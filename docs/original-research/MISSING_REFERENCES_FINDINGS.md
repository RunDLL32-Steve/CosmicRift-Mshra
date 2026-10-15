# Missing References Investigation Findings

Date: 2026-10-14. Four data-level gaps in `data/original/` that blocked accurate arena initialization. No invented replacements. No canonical values modified.

## Summary

| Reference | Issue ID | Source | Finding |
|-----------|----------|--------|---------|
| Portal/PrizeCrateA | RT-F377 | ITM/Rogue_mp.itm:112 | Genuinely absent from UtilityEffects.blo |
| Uiart/logo | RT-F378 | CFG/rogue.cfg:2752 | Genuinely absent from cruiart.blo |
| BallGraphic (numeric) | RT-F379 | CFG/rogue.cfg:2107 | Ordinal 0 = GameBall.cfs; interpretation unproven |
| Computer DropItemId66 | RT-F386 | VEH records 425/426/427 | Item 66 absent from ITM catalog |

---

## 1. Portal/PrizeCrateA (RT-F377)

**Source**: ITM/Rogue_mp.itm row 112, type 17 v57, ID 26 (Portal), fields 23/24.
**Raw token**: `UtilityEffects,PrizeCrateA`

**Investigation**: Exhaustive examination of `UtilityEffects.blo` members (objectId 0-6).

**Archive contents** (7 members):
- objectId 0: FlagGraphic.cfs
- objectId 1: PortalGraphic.cfs
- objectId 2: RepelA.cfs
- objectId 3: RepelB.cfs
- objectId 4: SuperBombPrize.cfs
- objectId 5: SuperMinePrize.cfs
- objectId 6: SuperMultiPrize.cfs

**Conclusion**: `PrizeCrateA` is genuinely absent. No member with that name or any close variant exists in the archive. The archive contains prize-related graphics (SuperBombPrize, SuperMinePrize, SuperMultiPrize) but nothing named PrizeCrateA. This is a genuine data gap in the original assets.

**Implication**: Portal item's visual reference cannot be resolved from the canonical archives. Requires either a fallback mechanism or identification of the intended graphic from runtime behavior.

---

## 2. Uiart/logo (RT-F378)

**Source**: CFG/rogue.cfg row 2752, Uiart section, Logo field.
**Raw token**: `cruiart,logo`

**Investigation**: Exhaustive examination of `cruiart.blo` members (objectId 0-32).

**Archive contents** (33 members):
- CRFONT1-5.cfs (5 files), button1-5.cfs (5 files), cashback.cfs, cursor.cfs,
- font1-5.cfs (5 files), gback.cfs, gguage.cfs, header.cfs, mainb.cfs,
- messageb.cfs, mtitle.cfs, newradar.cfs, notepada.cfs, notepadp.cfs,
- physics.cfs, scrollb1.cfs, scrollb2.cfs, tinynum.cfs, vision.cfs,
- warfog.cfs, warpPorter.cfs

**Conclusion**: `logo` is genuinely absent. No member named "logo" or any logo-like variant exists anywhere in the 33-member archive. This is a genuine data gap in the original assets.

**Implication**: The Uiart Logo field references a file that does not exist in the canonical UI art archive. May indicate a file that was intended but never shipped, or a fallback to default behavior.

---

## 3. BallGraphic Numeric Reference (RT-F379)

**Source**: CFG/rogue.cfg row 2107, Soccer section, BallGraphic field.
**Raw token**: `Soccer,Ball,0,0,0,0,35`

**Investigation**: Examination of `ball.blo` members (objectId 0-1).

**Archive contents** (2 members):
- objectId 0: GameBall.cfs
- objectId 1: GameBallTrail.cfs

**Conclusion**: The `,0` in the BallGraphic field corresponds to objectId 0 (`GameBall.cfs`), supporting the hypothesis that the numeric field is a BLO ordinal reference. However, this interpretation remains `numeric_member_role_unknown` at the data level because:
- The mapping is circumstantial (ordinal 0 happens to be the main ball graphic).
- No other CFG field has been verified to use the same ordinal-reference pattern.
- The role of the remaining numeric fields (`,0,0,0,0,35`) is uninterpreted.

**Implication**: The data supports but does not prove the ordinal-reference hypothesis. Runtime validation or cross-referencing with additional CFG sections is needed for confirmation.

---

## 4. Computer DropItemId66 (RT-F386)

**Source**: VEH records 425, 426, 427 (manufactory vehicles referenced by VehicleMaker items 83/84/85).
**Cross-reference chain**: ITM VehicleMaker (items 83/84/85) → VEH IDs 425/426/427 → DropItemId field.

**Investigation**: Examination of `vehiclemaker-fixed-audit.json` cross-references for all three VEH records.

**Findings**:

| VEH Record | DropItemId | Status |
|------------|------------|--------|
| 425 | 66 | `missing` |
| 426 | 66 | `missing` |
| 427 | 66 | `missing` |

All three manufactory vehicles reference ITM ID 66 as their DropItemId. Item 66 does not exist in the Rogue_mp.itm catalog (which contains 200 items, IDs 1-200, but item 66 is absent per the audit's `missing` status).

**Corroborating pattern**: Other VEH records show a consistent DropItemId pattern:
- VEH 400: DropItemId=0 (sentinel/default)
- VEH 401: DropItemId=2008 (present)
- VEH 402: DropItemId=0 (sentinel/default)
- VEH 403: DropItemId=2008 (present)
- VEH 404: DropItemId=0 (sentinel/default)
- VEH 405: DropItemId=2008 (present)
- VEH 407: DropItemId=0 (sentinel/default)

VEH 425/426/427 are unique in referencing ID 66, which is not a sentinel value (0) and not a present item.

**Conclusion**: Item 66 is genuinely missing from the ITM catalog. This is not a default/sentinel value — it is a positive reference to a non-existent item. The manufactory vehicles expect to drop an item that has no definition in the item catalog.

**Implication**: Arena initialization cannot resolve the drop-item behavior for manufactory vehicles without either defining item 66 (which requires original data, not invention) or implementing a fallback for missing drop targets.

---

## Related Documents

- [Dependency Plan](../../scripts/original-assets/DEPENDENCY_PLAN.md) — RT-F375 inventory, outstanding items
- [Package Audit Plan](../architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) — RT-F381/F385 parser audit
- [Runtime Plan](../architecture/ROGUE_RUNTIME_PLAN.md) — RT-F382/F383/F392/F394 runtime source
- [Actor Domain Queue](../../data/original/actors/IMPLEMENTATION_PLAN.md) — RT-F386 investigation
- [Feature Catalog](FEATURE_CATALOG.md) — RT-F377 through RT-F386 definitions
- [Handoff](../QWEN_HANDOFF.md) — Verified vs uncertain state tables
