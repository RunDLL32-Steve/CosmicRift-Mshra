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
- Run simulations only once a completed gameplay feature requires simulation verification. Use focused parser, byte-integrity, image and build checks for data/rendering work. Do not repeatedly rerun baseline gameplay simulations during research or documentation changes.
- The archived source names, pixels, map and values are authoritative. Interpret version/scaling/evaluator semantics with evidence, preserve raw fields, and keep unexplained behavior visible as unfinished work.

## Current checkpoint and documentation rule — October 13, 2026

The user stopped research and requested wrap-up. Read [CONTINUATION.md](CONTINUATION.md) before resuming. M0/M1 engineering acceptance is historical; importer/source inspector/parser/loopback milestones are now verified.372 original IDs have matching master checkboxes;5 narrowly complete,367 open. Updated standalone export and native/runtime parity remain unfinished.

Specific canonical queues: data/original/{ships,weapons,equipment,resources,sectors,actors}/IMPLEMENTATION_PLAN.md. Update current stage whenever work becomes Investigating, In progress, Testing or Complete. Remove completed tasks from active outstanding lists, retain completion ID/evidence in history and CHANGELOG, and keep checked master audit synchronized. Original assets/names/values/categories come from pinned fowlmouth originals unchanged.

Do not run broad verification/simulations for documentation/assets. Only a completed behavior requiring simulation triggers that check. The earlier stop applied to that checkpoint; the user has now explicitly resumed work. Separate two-agent documentation handoff: DOCS_RECONCILIATION_HANDOFF.md.

## Resumed slice — October 13, 2026

User resumed from CONTINUATION.md. Active work: original viewer standalone packaging (RT-F373), inspector input/navigation fixes and validation (RT-F374), dependency inventory (RT-F375/RT-F069), native package inventory (RT-F376). Root owns packaging/master/reconciliation; parallel agents own inspector, dependency script and native metadata scope. Avoid concurrent builds. Only focused completed-feature checks; no baseline gameplay smoke for this slice.
