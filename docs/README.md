# Documentation index

Use this index for navigation; acceptance status belongs to the [master §18](../ROGUE_TRADER_2_PLAN.md#18-development-roadmap) and the reciprocal original-feature trackers.

## Current work

- [Startup](../PROJECT_START.md) and [development rules](../AGENTS.md)
- [Current continuation](../CONTINUATION.md)
- [Task queue](../TASK_QUEUE.md) and [separate task briefs](tasks/README.md)
- [Progress and folder-plan index](development-log/PROGRESS.md)
- [Original feature catalog](original-research/FEATURE_CATALOG.md) and [checklist projection](original-research/FEATURE_MASTER_CHECKLIST.md)
- [Repository implementation index](../IMPLEMENTATION_PLAN.md)

## Design, code and evidence

- [Architecture](architecture/IMPLEMENTATION_PLAN.md): engine choice, native compatibility, runtime/package audits, toolchains and persistence design
- [Original research](original-research/IMPLEMENTATION_PLAN.md): provenance, raw fields, mechanics, references and acceptance profiles
- [Game design](game-design/IMPLEMENTATION_PLAN.md) and [provisional values](balance/IMPLEMENTATION_PLAN.md)
- [Data catalog](../data/IMPLEMENTATION_PLAN.md) and original domain queues under `data/original/`
- [Dated development evidence](development-log/IMPLEMENTATION_PLAN.md) and [changelog](../CHANGELOG.md)
- [October 17 source/documentation audit](audits/2026-10-17-documentation-audit.md)
- [Third-party notices](../THIRD_PARTY_NOTICES.md)

## Maintenance

Keep root files limited to entry points, the master, project rules, required status indices and notices. Store detailed task assignments in `docs/tasks/<id>.md`, item implementation evidence in the affected folder's `plan.d/<id>.md`, and audit reports in `docs/audits/`. Preserve dated handoffs in [archive](archive/README.md); do not use their old next steps as current instructions.

Current plans describe existing source and open acceptance separately. Historical logs describe what ran at their recorded revision. Ignored `.tools/` and `artifacts/` evidence is not guaranteed present in another checkout. Full original scope remains in the master/catalog even when the active task queue selects only a bounded portion.
