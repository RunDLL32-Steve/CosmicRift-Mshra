# Missing References Investigation Findings

Date: 2026-10-14; evidence corrected October 17 against canonical CFG and the pinned parser. These are unresolved source-reference/field questions, not demonstrated blockers to the already verified arena initializer. No invented replacements or canonical value changes.

## Summary

| Reference | Issue ID | Source | Finding |
|-----------|----------|--------|---------|
| Portal/PrizeCrateA | RT-F377 | ITM/Rogue_mp.itm:112 | Genuinely absent from UtilityEffects.blo |
| Uiart/logo | RT-F378 | CFG/rogue.cfg:2752 | Genuinely absent from cruiart.blo |
| BallGraphic trailing numeric fields | RT-F379 | CFG/rogue.cfg:2107 | `Soccer,Ball,0,0,0,0,35`; refers to supplemental Soccer/Ball, not canonical ball.blo |
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

**October18 pinned-loader role**: The [Portal source checkpoint](../development-log/2026-10-18-g9-portal-source.md) identifies f23/24 as this fixed-index loader's iconGraphic; prefireGraphic71/72 and warpGraphic101/102 normalize from raw leading-space `" None"`/`"None"` to None/None. This is not native field/display/revision proof or a missing-warp-effect finding. The selected archive-registration/factory/inherited ReadFile operations inspected do not demonstrate named CFS member resolution, display or fallback; the exact tracked-C# ReadBlobEntries search is bounded and does not exclude dynamic/native consumers.

**Implication**: The known missing icon pair under this loader remains unresolved. Establish matching original/native icon-consumer/version/display/fallback evidence; do not substitute existing PortalGraphic.cfs or invent artwork. RT-F377 stays open.

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

**Conclusion**: The accepted33-member canonical cruiart inventory lacks the exact `logo` member name. This establishes exact-name absence, not the visual identity of other members, shipping history or native intent. The [Uiart source checkpoint](../development-log/2026-10-18-g9-uiart-source.md) records string access/archive registration and a bounded pinned tracked-C# search; these do not prove native display/member lookup or that the field is unused.

**Implication**: Matching original/native logo consumer, revision, display/evaluator and fallback remain UNKNOWN. No substitute art or interpretation of another member as a logo is justified; RT-F378 remains open.

---

## 3. BallGraphic Numeric Reference (RT-F379)

**Source**: CFG/rogue.cfg row 2107, Soccer section, BallGraphic field.
**Raw token**: `Soccer,Ball,0,0,0,0,35`

**Correction**: The full canonical token names archive `Soccer` and member `Ball`. Inspecting canonical `ball.blo` ordinal 0 (`GameBall.cfs`) does not establish a mapping for that different archive/member pair. The former ordinal-reference conclusion was unsupported and is withdrawn.

Pinned `CfgInfo.Soccer.cs` reads `BallGraphic` as a string and registers the archive through `Parser.GetBlob`; it also registers Trail/Shadow/Catch/Throw/Bounce/Request unconditionally despite `BallCount=0`. The separately recovered authentic `soccer.blo` has the required named members and passed bounded full-asset startup (RT-F394), as documented in [NATIVE_CLIENT_PLAN.md](../architecture/NATIVE_CLIENT_PLAN.md#authentic-shared-engine-dependency--rt-f394-complete).

**October18 source applicability**: The [bounded pinned-source checkpoint](../development-log/2026-10-18-g9-ballgraphic-source.md) finds only declaration/string assignment/archive registration in the tracked-C# BallGraphic search. Nearby SpriteBlob field orders and VEH-version dispatches have no demonstrated CFG join; a conditional seven-token comparison under CsvFile.LineParser is source inference, not an executed parser test or CFG validation. Neither35 nor another trailing field acquires HSV/animation/ordinal meaning from shape.

**Remaining acceptance**: establish the matching original CFG consumer, its selected grammar/version/evaluator, then the trailing numeric fields and native graphical/evaluator behavior from matching evidence. Inventory's `numeric_member_role_unknown` is a raw-scanning classification, not proof that a numeric field is a BLO ordinal. RT-F379 remains open; neither GameBall substitution nor an invented sprite is justified.

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

All three manufactory vehicles reference ITM ID 66 as their DropItemId. Item 66 does not exist among the 200 Rogue_mp.itm records. Record count is not an ID range: IDs are sparse and include values such as 1004 and 2008.

**Corroborating pattern**: Other VEH records show a consistent DropItemId pattern:
- VEH 400: DropItemId=0 (sentinel/default)
- VEH 401: DropItemId=2008 (present)
- VEH 402: DropItemId=0 (sentinel/default)
- VEH 403: DropItemId=2008 (present)
- VEH 404: DropItemId=0 (sentinel/default)
- VEH 405: DropItemId=2008 (present)
- VEH 407: DropItemId=0 (sentinel/default)

VEH 425/426/427 share positive reference 66; it is absent from the item catalog. That static fact does not establish whether or when the native/emulator lifecycle evaluates it.

**Conclusion**: The canonical manufactory rows record positive DropItemId66 and quantity1, while the canonical ITM catalog has no ID66. This establishes a dangling recorded reference; whether it is dynamically evaluated or an unused/legacy value remains unverified.

**October 18 source checkpoint**: The [G9 ledger](../../scripts/original-assets/plan.d/g9.md#bounded-pinned-source-trace-for-dropitem66) traces a conditional explosion path through inherited two-argument kill and the generic event/item/null/quantity guards before Computer's Destroyable-gated one-argument destruction. It also distinguishes the Car-only Bot path. This is source reachability evidence, not actual dynamic eligibility or a fresh runtime test; item 66 remains absent and RT-F386 open.

**Implication**: Trace the actual destruction/drop path for these manufactory types before proposing any change. RT-F383 arena initialization already passed, and that result does not evaluate every eventual drop path. Preserve the unresolved reference; add no invented item or fallback.

---

## Related Documents

- [Dependency Plan](../../scripts/original-assets/DEPENDENCY_PLAN.md) — RT-F375 inventory, outstanding items
- [Package Audit Plan](../architecture/ROGUE_PACKAGE_AUDIT_PLAN.md) — RT-F381/F385 parser audit
- [Runtime Plan](../architecture/ROGUE_RUNTIME_PLAN.md) — RT-F382/F383/F392/F394 runtime source
- [Actor Domain Queue](../../data/original/actors/IMPLEMENTATION_PLAN.md) — RT-F386 investigation
- [Feature Catalog](FEATURE_CATALOG.md) — RT-F377 through RT-F386 definitions
- [Handoff](../archive/2026-10/qwen-handoff.md) — Verified vs uncertain state tables
