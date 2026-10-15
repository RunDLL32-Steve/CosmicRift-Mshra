# Build and packaging implementation plan

Master authority: [§18 M0](../ROGUE_TRADER_2_PLAN.md#milestone-0--repository-and-historical-research), [M1 engineering acceptance and fidelity gate](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [§19 slices A–E](../ROGUE_TRADER_2_PLAN.md#19-first-playable-build--version-01) and [§1.3 focused verification](../ROGUE_TRADER_2_PLAN.md#13-continued-exact-reproduction--october132026). Canonical acceptance IDs: [original feature catalog](../docs/original-research/FEATURE_CATALOG.md). Reconciled October 13, 2026 with [progress](../docs/development-log/PROGRESS.md). The master remains the status authority.

## Completion history and evidence

| Scoped feature | Current status | Evidence and limit |
|---|---|---|
| Portable pinned toolchain, isolated caches and build/launch/export commands | Complete with evidence | [bootstrap.ps1](bootstrap.ps1), [environment.ps1](environment.ps1), [verify.ps1](verify.ps1), [export-windows.ps1](export-windows.ps1) and [toolchain lock](../toolchain.lock.json). |
| M0/M1 engineering acceptance and reproducible Windows package | Complete with evidence | Prior 41 unit tests, three native-input/UI and six world phases in source/export; inspected captures and standalone launch without SDK/runtime discovery in [progress](../docs/development-log/PROGRESS.md#final-validation-evidence). [unit.trx](../artifacts/test-results/unit.trx) records 41 passed/0 failed; [Windows archive](../artifacts/windows/FirstContact-0.1.0-win-x64.zip) is the accepted provisional artifact. No new run occurred here. |
| Canonical reference acquisition and integrity verification | Complete with evidence | [sync](sync-reference-assets.ps1), [verifier](verify-reference-assets.ps1), [manifest](../docs/original-research/ASSET_MANIFEST.json) and prior 46-file verification in progress. Canonical commit: eb1514eaae7f9b9425307d9cc043c74ecdd15e35. |
| Raw original indexing | Complete with evidence | [index subplan](ORIGINAL_CONTENT_INDEX_PLAN.md), [derived index](../artifacts/research/original-content/index.json): 3,086 records, 12 RPG/VEH matches, 200 items, seven resources. Indexing only. |
| Selected sprite decoding/staging | Complete with evidence, selected slice only | [importer owner plan](original-assets/IMPLEMENTATION_PLAN.md) records 11 archives, 231 objects, 36,516 frames and 14 focused tests; [derived index](../artifacts/original-assets/index.json) records unchanged source hashes. Full RT-F069–RT-F075 acceptance remains open. |

Completed tasks are retained as history, outside the active checklist. Originals remain immutable; derived runtime/staging output and compatibility working copies remain separate.

## Active outstanding work

- [ ] **Investigating — RT-F069:** complete dependency closure, including remaining member candidates, audio and intentional sentinels. Zero unresolved filenames is insufficient.
- [ ] **Investigating — RT-F069 / RT-F074–RT-F075:** resolve runtime dependencies and native appearance/pose semantics. RT-F070–RT-F072 selected decoder/source inspector/actual parser acceptance is completed history; broader all-payload/native acceptance is separate.
- [ ] **Not started — RT-F074:** validate native palette/HSV, shadow/light and blit semantics before exact appearance acceptance.
- [ ] **Investigating — RT-F075:** resolve native direction rounding, bank/pose-column selection and animation state; visually balanced column 5 remains unverified.

Detailed importer work remains in scripts/original-assets; parser/server work remains in [legacy compatibility](../docs/architecture/LEGACY_COMPATIBILITY_PLAN.md) and scripts/compatibility. Unknown units, negative sentinels and state evaluators remain explicit in [field contracts](../docs/original-research/FIELD_INTERPRETATION.md). Source/parsing/build success does not prove a compatible native game.

Documentation uses read-only source/log/link checks. Gameplay simulation follows completion of the feature needing behavior verification; data/rendering use focused parser/integrity/image/build checks. Avoid concurrent builds against shared outputs. NEW expansions remain deferred until exact reproduction.

Completion history RT-F373: original-content self-contained export passes standalone native inspector input and ships/map/sprites resource/render checks, all exit0/tick0 with DOTNET_ROOT missing and PATH Windows-only. Logs/captures: artifacts/qa/OriginalContent; package artifacts/windows/OriginalContent-2026-10-13-win-x64.zip. ExportRelease zero warnings/errors. The Python user-site lookup is captured before portable APPDATA activation; fixes lost Pillow imports during staging. Original mode runs no baseline gameplay smoke. Command: ./scripts/export-windows.ps1 -Mode OriginalContent. Prototype mode retains historical workflow.

Current tooling coverage is documented in [compatibility/IMPLEMENTATION_PLAN.md](compatibility/IMPLEMENTATION_PLAN.md) and each Loader/ServerProbe/ArenaProbe/SessionHost/RuntimeChecks folder plan. RT-F375/376 inventories, RT-F381/382 package/source audits, RT-F383 initializer, RT-F385 derived parser fix and RT-F392/394 full-asset loopback/shared-Soccer acceptance are completed scoped history. October14 RT-F388/390/395 safeguards add35/35 actual-handler checks and a healthy10-second host;[evidence](../docs/development-log/2026-10-14.md). RT-F380/384 native entry/profile, RT-F387 delivery, RT-F393 actual CS_Ready guard and RT-F389/F396 store/buying safety remain open. No native UI or baseline flight/combat simulations in this slice.
