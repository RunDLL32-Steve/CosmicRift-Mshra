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
- Choose and explicitly set each agent's reasoning effort for its task before spawning or assigning follow-up work. Do not let routine work inherit Ultra by default. When the tool requires a fresh context to set effort, supply the necessary context in that assignment.
- Use **low** for mechanical documentation updates, link/count checks and straightforward read-only audits; **medium** for documentation synthesis, bounded research, simple implementation and focused test work; **high** for complex implementation, architecture, subtle correctness issues or difficult debugging. Use higher settings, including **Ultra**, only when the task's complexity or unresolved evidence warrants them.
- Reassess effort when an agent's task changes. If follow-up work needs a different effort and the tool cannot change it, assign it to a suitably configured agent instead of silently reusing the previous setting.
- The coordinating agent must review every agent's work before accepting or integrating it: inspect the actual changes/findings, verify the relevant evidence and focused checks, and resolve identified defects. An agent's completion claim alone is not acceptance. Record the review outcome with the work's evidence; leave unverified feature checkboxes open.
- Keep this rule in force across sessions and handoffs unless the user explicitly changes it.

## Agent and branch workflow — October 14, 2026

- One scoped branch (and worktree) per issue; inspect existing checkout state first and preserve unrelated uncommitted work.
- The orchestrating session runs Sonnet 5.5 at medium effort. Set each agent's model and effort to fit its task: do not put a top-tier model on documentation, and do not put a lightweight model on code or review work. Reserve higher effort for complex implementation, architecture and subtle correctness.
- The coordinator must inspect every agent's actual diff and evidence before accepting it; a completion claim alone is not acceptance. Leave unverified feature checkboxes open.
- The coordinating CI/CD session reviews and tests each issue branch (`scripts/verify.ps1` plus the relevant compatibility checks) and merges to main only branches that fix their issue. Diagnostic or WIP branches are merged only as clearly labelled tooling and never mark a feature complete.
- At a usage/checkpoint stop, commit owned issue work first, then record the commit, verified results, incomplete reviews and exact resume point. Preservation commits never imply completed verification.
- No pushes unless the user asks. No desktop/GUI interaction is authorized without the user present.

## Current documentation checkpoint — October 13, 2026

Read CONTINUATION.md and the complete master before resuming. Current catalog/master:394 feature entries,15 scoped completions,379 open. Viewer source/export/input checks, archive/package/runtime audits, original arena initialization, bounded full-asset hosting and VehicleMaker field18 fix are verified. Native playable entry, actual delivery, full original gameplay and persistence remain unverified.

Every meaningful source folder has an IMPLEMENTATION_PLAN.md or a linked parent plan describing purpose, accomplished work/evidence, next work/current stage and original-parity limits. Remove completed tasks from active queues; retain master checkmarks, concise history and major-feature CHANGELOG entries. Original queues are data/original/{ships,weapons,equipment,resources,sectors,actors}. Canonical names/categories/values/bytes remain unchanged.

Update status at investigation, implementation, testing and completion. Run focused checks only for the completed feature requiring them; no builds, GUI launches or simulations for documentation reconciliation. Use parallel agents for independent work and avoid concurrent shared-output builds.

The isolated native client reached welcome only; Start had no verified result. Computer Use stopped while the user played another game. Owned processes/private test keys were cleaned up. Do not resume desktop interaction while the user is busy; follow action-time confirmation and authentication/security rules.

The current handoff is CONTINUATION.md. DOCS_RECONCILIATION_HANDOFF.md records earlier history; the latest documentation reports are DOCS_RUNTIME_FOLDERS_REPORT.md, DOCS_TOOLS_ARCHITECTURE_REPORT.md and DOCS_CONTENT_RESEARCH_REPORT.md.
