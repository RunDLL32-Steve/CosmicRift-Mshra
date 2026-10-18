# Repository implementation index

Authority: [master §18](ROGUE_TRADER_2_PLAN.md#18-development-roadmap). Startup: [PROJECT_START.md](PROJECT_START.md), [AGENTS.md](AGENTS.md), [CONTINUATION.md](CONTINUATION.md). Current registry: **397 feature entries, 22 scoped completions, 375 open**.

Purpose: index implemented source and its acceptance boundaries. Use [docs/README.md](docs/README.md) to navigate architecture, research, current evidence and archives; use [TASK_QUEUE.md](TASK_QUEUE.md) for separate task briefs.

The provisional prototype and original viewer have historical engineering acceptance. Raw original-data readers, generic cargo, persistence/transactions, trading, replay, protocol codec and provisional AI exist in source. Production server/transport, native playable entry and the full original gameplay/progression loop are not accepted. Completed queue slices do not check off full original features.

Folder plans: [client](client/IMPLEMENTATION_PLAN.md), [shared](shared/IMPLEMENTATION_PLAN.md), [server](server/IMPLEMENTATION_PLAN.md), [data](data/IMPLEMENTATION_PLAN.md), [tests](tests/IMPLEMENTATION_PLAN.md), [scripts](scripts/IMPLEMENTATION_PLAN.md), [docs](docs/IMPLEMENTATION_PLAN.md) and [CI](.github/workflows/IMPLEMENTATION_PLAN.md). Item-specific changes/evidence belong in each folder's `plan.d/` file.

Current documentation audit: [October 17 coverage and review](docs/audits/2026-10-17-documentation-audit.md). No source, asset, build or gameplay acceptance changed. Next work remains the unchecked original-fidelity dependencies in the task index and master.
