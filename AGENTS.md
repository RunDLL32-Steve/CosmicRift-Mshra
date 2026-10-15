# Rogue Trader 2.0 development conventions

Read `PROJECT_START.md` and the complete `ROGUE_TRADER_2_PLAN.md` before changing scope. The latter is the master specification; Milestones 0 and 1 engineering acceptance is complete. Current work is the exact-reproduction fidelity gate and incremental Milestone 2 work after its dependencies are validated.

- Complete a milestone's mandatory acceptance criteria before starting the next. Keep a compilable, runnable vertical slice.
- Separate pure C# simulation and data definitions from Godot rendering, input and UI. Future networking, persistence and AI remain separate modules. The eventual server is authoritative.
- Use fixed 60 Hz simulation with explicit world units. Never use rendering-node state as authoritative gameplay state.
- Use external JSON definitions. Mark every invented number NEW/provisional. Record historical classifications and source access in `docs/original-research/MECHANICS.md`; never fabricate evidence. Use the user-authorized canonical original content without altering its source bytes; keep derived runtime outputs separate.
- Keep changes small and test critical movement/combat behavior. Run only the focused checks required by a completed feature; reserve simulations for completed gameplay behavior needing verification.
- Maintain `README.md`, `CHANGELOG.md`, `docs/development-log/PROGRESS.md`, the dated log, and each affected folder's `IMPLEMENTATION_PLAN.md`.
- Reconcile subplans with the master: link the exact milestone/section, check off only implemented and verified work, attach evidence, and leave blocked or deferred work unchecked. The master remains the status authority.
- Produce a reproducible Windows artifact at milestone boundaries. A successful source build alone does not prove the exported client launches.
- Prefer simple modules and canonical originals where decoded; keep remaining placeholders explicitly provisional. Do not introduce multiplayer infrastructure or unrelated features before the flight/combat prototype passes acceptance.
- Use the pinned toolchain in `toolchain.lock.json` and `global.json`. Portable dependencies, caches and build artifacts stay ignored in `.tools/` and `artifacts/`.
- Treat fowlmouth/cosmic-rift-assets at the pinned ASSET_MANIFEST commit as the foundational original content source for 100% Rogue Trader reproduction. Never alter original source files, names or values; store derived decoding/adaptation outputs separately and run scripts/verify-reference-assets.ps1. Original data takes precedence over provisional/new proposals. Evaluate InfantryOnline server/native-client compatibility before extending the Godot testbed into a production engine choice.
- Inspect licenses before reusing external code or data. A public repository is evidence of availability, not automatic redistribution permission.

Useful commands (PowerShell, repository root):

```powershell
./scripts/bootstrap.ps1
./scripts/verify.ps1
./scripts/run-client.ps1
./scripts/export-windows.ps1
```



Current user requirements (October13,2026):

- Catalog every newly discovered original feature with a stable ID and an unchecked box in the master and relevant subplan. Discovery, source availability and a provisional implementation do not complete the feature.
- Check off only the specifically implemented feature after its acceptance verification passes; do not imply complete original parity from a narrower renderer/parser milestone.
- Use parallel subagents for independent research, implementation, tests and fixes; avoid concurrent builds against the same outputs.
- Follow the maintained agent qualification and review rule below for every delegation.
- Run simulations only once a completed gameplay feature requires simulation verification. Use focused parser, byte-integrity, image and build checks for data/rendering work. Do not repeatedly rerun baseline gameplay simulations during research or documentation changes.
- The archived source names, pixels, map and values are authoritative. Interpret version/scaling/evaluator semantics with evidence, preserve raw fields, and keep unexplained behavior visible as unfinished work.

## Maintained agent qualification and review rule — October 14, 2026

- Use agents for independent work that advances the current project goals. Give each agent a bounded task, file ownership, required context and acceptance criteria; avoid overlapping edits and shared-output builds.
- Choose and explicitly set each agent's model and reasoning effort from the "Model and effort matrix" below before spawning or assigning follow-up work. Do not let routine work inherit the top model or Ultra by default; give the reason when deviating from the matrix. When the tool requires a fresh context to set effort, supply the necessary context in that assignment.
- Reassess when an agent's task changes; if the tool cannot change effort, assign the follow-up to a suitably configured agent instead of silently reusing the previous setting.
- The coordinating agent must review every agent's work before accepting or integrating it: inspect the actual changes/findings, verify the relevant evidence and focused checks, and resolve identified defects. An agent's completion claim alone is not acceptance. Record the review outcome with the work's evidence; leave unverified feature checkboxes open.
- Keep this rule in force across sessions and handoffs unless the user explicitly changes it.

## Agent and branch workflow — October 15, 2026

- One scoped branch (and worktree) per issue/feature/fix; inspect existing checkout state first and preserve unrelated uncommitted work.
- **The author never merges their own work into main.** A different, newly started review agent reviews the branch (diff, intent, tests, conflicts) and merges it only if approved. If not approved, the reviewer states why, may fix the change on that branch and then merge; it must not merge anything it has not itself understood. For code the reviewer re-runs the relevant tests (`scripts/verify.ps1`, `prepare-derived-source.ps1` plus runtime/native-delivery checks as applicable) and judges by the JSON reports (the two compatibility wrappers print a spurious exit failure under Windows PowerShell 5.1).
- The orchestrating session runs Sonnet 5.5 at medium effort and delegates per the matrix. The coordinator inspects every agent's actual diff and evidence; a completion claim alone is not acceptance. Leave unverified feature checkboxes open. Diagnostic or WIP branches merge only as clearly labelled tooling and never mark a feature complete.
- At a usage/checkpoint stop, commit owned issue work on its branch first, then record the commit, verified results, incomplete reviews and the exact resume point. Preservation commits never imply completed verification.
- Pushes only to feature branches and only where the user has authorized it (the cloud routine below); never push main without the user. No desktop/GUI interaction without the user present.

### Test selection and marking off work

- **Run only the checks that apply to what changed.** Do not run login/native-delivery checks for a weapon change or flight tests for a shop fix. Map the change to its checks:
  - `shared/`, `server/RogueTrader.*`, `tests/Unit` -> `dotnet test tests/Unit` only (plus the Godot smoke in `verify.ps1` only if `client/` or the simulation/physics shared with the client changed).
  - `client/` -> `scripts/verify.ps1` (build, unit tests, Godot import and smoke).
  - `scripts/compatibility/patches/shop-*`, null-script capacity, radar routing, `RuntimeChecks/` -> `prepare-derived-source.ps1` then `run-runtime-checks.ps1 -Build`.
  - `patches/packet-length.ps1`, `NativeDeliveryChecks/`, asset packet code -> `run-native-delivery-checks.ps1 -Mode fixed`.
  - `scripts/compatibility/*native-start*` -> `python scripts/compatibility/test-native-start.py`.
  - Markdown only -> `python scripts/check-trackers.py` and nothing else.
  Run the full set only for a cross-cutting change (shared build props, solution layout, the derived-source pipeline itself) or before a release. Report which checks were skipped and why.
- **Mark off finished work in the same branch that finishes it**, so nobody repeats it: tick the item in `docs/original-research/FEATURE_MASTER_CHECKLIST.md` and `ROGUE_TRADER_2_PLAN.md` section 18, set the catalog row, update the folder `IMPLEMENTATION_PLAN.md` line and `CONTINUATION.md` "Next", and move every status count together. Only tick with verified acceptance evidence; leave anything unverified open and say so. `python scripts/check-trackers.py` must print `TRACKERS_CONSISTENT` before a branch is reviewed or merged, and the reviewer re-runs it.
- Judge compatibility-wrapper results by their exit code; since the PS 5.1 handle fix a blank or non-zero exit is a real failure. The JSON report (`Accepted`) remains the evidence of record.

### Model and effort matrix

| Task | Model | Effort |
|---|---|---|
| Reconstruct gameplay from recovered files/footage; analyze incomplete binaries, formats, undocumented behavior | Opus 5.5 | High; XHigh only when evidence conflicts or two High attempts failed |
| Authoritative multiplayer architecture; complex bugs spanning client, server and simulation | Opus 5.5 | XHigh |
| Complete game architecture; deterministic movement/physics specs; networking, prediction and reconciliation; persistence, progression and transaction design; major refactors | Opus 5.5 | High |
| Native client Start/entry diagnosis (RT-F380/F384) | Opus 5.5 | High |
| Format identification and unfamiliar binary parsers; network synchronization and multiplayer testing; critical architecture/security reviews | Opus 5.5 | High |
| Investigating persistent performance bottlenecks | Opus 5.5 | High/XHigh |
| Gameplay systems from approved specs (movement, controls, weapons, mining, inventory, upgrades, economy, enemy AI, map rendering/collision, Godot/C#) | Sonnet 5.5 | High |
| Economy and shop rules (RT-F388/F389/F396 style) | Opus 5.5 designs the rule, Sonnet 5.5 implements and tests | High / High |
| New gameplay features for Rogue Trader 2.0 | Opus 5.5 plans, Sonnet 5.5 implements | High / High |
| Multiplayer UI, database CRUD/admin interfaces | Sonnet 5.5 | Medium |
| Behavioral and integration tests; code reviews for gameplay correctness; performance profiling and optimization | Sonnet 5.5 | High |
| Merge-conflict resolution; review-and-merge of code branches | Sonnet 5.5 | High (Opus High if the change touches networking, persistence, transactions or security) |
| Asset conversion scripts and import utilities | Sonnet 5.5 | Medium |
| Asset inventory and metadata extraction; boilerplate; basic unit-test scaffolding | Haiku 5.5 | Low/Medium |
| Running CI and reporting results (diagnosis of a failure moves to Sonnet) | Haiku 5.5 | Medium |
| Documentation updates from verified implementations; review-and-merge of markdown-only branches | Haiku 5.5 | Medium |

Never use a Fable-class or Opus model for routine documentation. Documentation count/status edits must be followed by a mechanical consistency check of the counts across the master checklist, catalog, README, PROJECT_START, CONTINUATION and PROGRESS.

### Cloud routines (unattended, PC off)

Two scheduled cloud routines run while the owner's PC is off: **CosmicRift overnight development** (Sonnet 5.5, 4:15/7:15/10:15 Chicago time) and **CosmicRift senior reviewer** (Opus 5.5 High, 90 minutes after each development run). Both start from the latest `origin/main` and obey every rule above.

- **Branches and claims.** One item per branch named `cloud/<lowercase RT-ID>-<slug>`, one RT-ID per branch. Cloud branches are not merged overnight, so main's checkboxes lag behind: any RT-ID touched by an `origin/cloud/*` branch (name, commits, log) is claimed and must not be started again. A branch that truly depends on an unmerged cloud branch is cut from it and says so in its log. Cloud routines push only their own `cloud/*` branch and never push or merge main.
- **Branch log.** `docs/development-log/cloud/<branch with / replaced by ->.md`. Its first line is `Review status: UNREVIEWED | READY FOR RE-REVIEW | CHANGES REQUESTED | APPROVED | NEEDS HUMAN`; it also records scope and acceptance criteria, RT-IDs claimed, files touched, status (done/wip/blocked), verified and unverified results with numbers, and the resume point.
- **Review loop.** The author never reviews or approves its own work. The senior reviewer reviews pending branches (status UNREVIEWED or READY FOR RE-REVIEW) against scope and acceptance criteria, runs what it can, and writes a detailed `## Review round N (senior review)` section in the log plus a commit whose subject is `REVIEW round N: APPROVED|CHANGES REQUESTED` and whose body repeats every finding (empty commit if nothing else changed). Findings carry file:line, severity (blocker/major/minor/nit), evidence and the expected fix. The reviewer never fixes the author's code. APPROVED requires no blockers or majors and a match to scope.
- **Rework.** The next development run takes the oldest CHANGES REQUESTED branch before any new item, fixes every blocker and major on that same branch, answers each finding in a `## Rework after round N` log section (fixed with commit / disputed with reasoning / cannot fix and why), sets READY FOR RE-REVIEW and pushes. The senior reviewer re-reviews. After round 3 with blockers open the status becomes NEEDS HUMAN.
- **Merging.** Only APPROVED branches are merged into main, by a local review agent after the owner's PC is back and the relevant tests are re-run locally (stacked branches in order).
- **Verification limits.** Cloud runs lack the ignored local toolchain (`.tools/` SDKs, Godot, pinned upstream and original assets). Anything a cloud run cannot test is recorded as "not verified in cloud - local CI required" and never marked complete; results are never fabricated.
- **Stop conditions.** Stop and write the resume point to the branch log when a task needs desktop interaction, an authentication ticket, a user decision or a toolchain the cloud lacks.

## Current documentation checkpoint — October 13, 2026

Read CONTINUATION.md and the complete master before resuming. Current catalog/master:394 feature entries,15 scoped completions,379 open. Viewer source/export/input checks, archive/package/runtime audits, original arena initialization, bounded full-asset hosting and VehicleMaker field18 fix are verified. Native playable entry, actual delivery, full original gameplay and persistence remain unverified.

Every meaningful source folder has an IMPLEMENTATION_PLAN.md or a linked parent plan describing purpose, accomplished work/evidence, next work/current stage and original-parity limits. Remove completed tasks from active queues; retain master checkmarks, concise history and major-feature CHANGELOG entries. Original queues are data/original/{ships,weapons,equipment,resources,sectors,actors}. Canonical names/categories/values/bytes remain unchanged.

Update status at investigation, implementation, testing and completion. Run focused checks only for the completed feature requiring them; no builds, GUI launches or simulations for documentation reconciliation. Use parallel agents for independent work and avoid concurrent shared-output builds.

The isolated native client reached welcome only; Start had no verified result. Computer Use stopped while the user played another game. Owned processes/private test keys were cleaned up. Do not resume desktop interaction while the user is busy; follow action-time confirmation and authentication/security rules.

The current handoff is CONTINUATION.md. DOCS_RECONCILIATION_HANDOFF.md records earlier history; the latest documentation reports are DOCS_RUNTIME_FOLDERS_REPORT.md, DOCS_TOOLS_ARCHITECTURE_REPORT.md and DOCS_CONTENT_RESEARCH_REPORT.md.
