# Original dependency inventory

Status: **Complete for directory/candidate inventory; runtime closure remains In progress**, October 13, 2026. Reconciles with [master §18 fidelity gate / RT-F069](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [original feature catalog](../../docs/original-research/FEATURE_CATALOG.md#rt-f069), and scoped inventory feature RT-F375. Root owns master/catalog status reconciliation.

## Outstanding

- [ ] RT-F069: validate version-specific field roles, runtime eligibility and actual dependency closure. A unique filename/member candidate is inventory evidence, not original runtime acceptance.
- [ ] Resolve missing exact `UtilityEffects/PrizeCrateA` visual reference: ITM/Rogue_mp.itm row112, type17 v57 ID26 Portal (RT-F182), fields23/24. No such member/stem exists among all1,262 members. No replacement is selected.
- [ ] Resolve missing `cruiart/logo`: CFG/rogue.cfg row2752, Uiart Logo value components0/1. Exact native use/version and intended fallback remain unknown.
- [ ] Validate `BallGraphic=Ball,0`: CFG/rogue.cfg row2107. BLO/ball.blo ordinal0 is GameBall.cfs; numeric field interpretation remains unknown, so the index does not automatically accept this mapping.
- [ ] Reconcile five HelpMenu files absent from the canonical tree with separately recovered pinned Zone-Assets help dependencies: keys.txt, infcmdlst.txt, comms.txt, rules.txt and mods.txt. Existing RT-F368–372 menu behavior remains unfinished; the prior offline host recovery is narrower evidence.
- [ ] Interpret non-file links (item/vehicle IDs, event scripts/evaluators) against their version-specific schemas. The inventory retains non-CFG record IDs/versions without guessing all integer field roles.

## Completed history / evidence

- RT-F375 scoped inventory: original standard-library read-only `index_dependencies.py` indexes all89 canonical BLO v2 directories,1,262 exact member names, original zero-based ordinals, offsets/lengths and payload hashes. Directory/payload bounds, overlaps and unsafe/duplicate names are rejected; originals are never written.
- Before/after checks validate99 distinct canonical file hashes: all89 LFS payloads against pinned Git pointer OIDs/lengths plus the existing46 foundation manifest records with overlap. This does not expand the foundation manifest count or claim all files in the repository are hydrated.
- All initial551 candidates are accounted for:544 unique member candidates,6 unique filenames and1 missing member. CFG CSV component scanning adds58 candidates that the initial raw index did not expose.
- Expanded609 candidate inventory:595 unique member candidates,6 unique filename candidates,5 missing filenames,2 missing members and1 numeric-role unknown. No ambiguous candidates. Eight residual/unknown candidates are listed in `unresolved`; `runtimeClosureVerified` remains false. Source raw lexemes, field index, CFG component index, record IDs/versions and original member names remain intact.
- Eight focused fixture tests pass: BLO v1/v2, invalid/truncated directories, duplicate/unsafe names, overlap, altered same-size canonical bytes, CFG CSV components, exact-name/stem ambiguity with no fuzzy replacement, output guards and numeric non-substitution.
- Live directory cross-check: all89 archives/1,262 members match the existing independently written decoder's names/ordinals/spans/hashes. All609 dependency raw lexemes, record IDs/versions agree with the raw source inventory. Initial551 candidate keys are retained.
- `scripts/verify-reference-assets.ps1` passes46 canonical foundation records after the inventory. No pixel export, native client, rendering or gameplay simulation was run for this feature.

## Commands and output contract

```powershell
python scripts/original-assets/index_dependencies.py --self-test
python scripts/original-assets/index_dependencies.py
./scripts/verify-reference-assets.ps1
```

Standard library Python3.11+, no Pillow requirement. Flags: `--source-root`, `--manifest`, `--raw-index`, `--output`. Default output is ignored `artifacts/original-dependencies/index.json`. Outputs inside originals, at the input manifest/raw index or at the script itself are rejected. The source checkout must match pinned commit and have no actual tracked/untracked changes.

JSON schema1 exposes `archives` (exact directory metadata), `dependencies` (raw candidate links with classification), `unresolved` (eight unknown/missing candidates), `recordIdentities` (versioned IDs), `sourceIntegrityBeforeAfter` (99 hash/length validations), and `summary`. Exact member names and case-insensitive exact stems are candidates; no partial/fuzzy matching or silently invented artwork/value is used. All record roles remain explicitly version-sensitive and unvalidated.
