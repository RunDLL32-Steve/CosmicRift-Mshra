# CI and export implementation plan

Current CI documentation lives here. The dated `.github/workflows/IMPLEMENTATION_PLAN.md` is retained as a superseded historical snapshot: GitHub rejected changes to that Markdown file because the current token lacks workflow scope. Workflow YAML remains unchanged.

Master authority: [§13.1 architecture](../../ROGUE_TRADER_2_PLAN.md#131-initial-architecture), [§18 M0 automated build](../../ROGUE_TRADER_2_PLAN.md#milestone-0--repository-and-historical-research), [M1 acceptance](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype) and [§1.3 verification boundaries](../../ROGUE_TRADER_2_PLAN.md#13-continued-exact-reproduction--october132026). Original acceptance IDs: [canonical feature catalog](../../docs/original-research/FEATURE_CATALOG.md). Reconciled October 13, 2026 with [progress](../../docs/development-log/PROGRESS.md). The master remains the status authority.

## Completion history and evidence

| Scoped feature                                         | Current status         | Evidence and limit                                                                                                                                                                                                                                                  |
| ------------------------------------------------------ | ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Windows workflow definition with pinned portable tools | Complete with evidence | [build.yml](../../.github/workflows/build.yml) bootstraps verified tools, builds/tests/smokes the client, exports Windows and uploads zip/TRX evidence; [toolchain lock](../../toolchain.lock.json) pins tools/checksums.                                           |
| Prior local M0/M1 command acceptance                   | Complete with evidence | [Acceptance record](../../docs/development-log/PROGRESS.md#final-validation-evidence): 41 tests, three native-input/UI and six world phases in source/export plus rendered/export QA. These were prior local runs, not a hosted CI result or new documentation run. |

## Active outstanding work

-   [ ] **Partially observed — hosted CI acceptance:** origin is configured for RunDLL32-Steve/CosmicRift-Mshra. Read-only REST inspection confirms the Windows check on `d14df02` concluded success ([run](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/actions/runs/39051255053/job/117450136234)). Artifact download and this documentation PR's later head are not verified by that observation; keep export/asset-dependent/native acceptance distinct.
-   [ ] **Not started — fidelity CI coverage:** add appropriate focused checks for the now-completed importer/viewer scopes and independently verify the exported feature. Existing workflow inspection establishes only the provisional First Contact sequence; RT-F069 dependency closure, RT-F074 native blits and RT-F075 state selection remain open.

Preserve unchanged originals with separately derived outputs. [Field contracts](../../docs/original-research/FIELD_INTERPRETATION.md) track unresolved units/sentinels/native evaluators; CI counts cannot resolve them. NEW proposals stay deferred until exact reproduction. Do not rerun baseline simulations for documentation/research changes or run concurrent builds against shared outputs.

## Source audit — October 17, 2026

[build.yml](../../.github/workflows/build.yml) runs bootstrap, `scripts/verify.ps1`, default prototype export and artifact upload on Windows. It does not add original assets or native-client interaction. Current feature status belongs to [master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap); see the [documentation audit](../../docs/audits/2026-10-17-documentation-audit.md) for this source comparison. No workflow YAML, build script or CI execution was changed by the audit.
