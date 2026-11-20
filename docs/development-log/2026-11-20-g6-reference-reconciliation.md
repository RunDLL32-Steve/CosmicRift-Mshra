# G6 reference reconciliation — November 20, 2026

The queue now records the G6 LVL/LIO reader as merged at `df171c7` and marks the data dependency for G10 as satisfied. The [task brief](../tasks/g6.md) points to the preserved original fields, the canonical parser checks and the downstream Godot map. [PR 111](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/111)

The reader retains the 22,356 raw placed-entity records. The Godot map uses emulator-derived tile queries. Separate artwork staging now retains all 380,052 entity bytes in schema 4. Native layout, field interpretation, collision response and actual asset delivery remain open. The G10 evidence now identifies Asp at physical line 20 of the pinned VEH file. [Placed-art evidence](../../scripts/original-assets/plan.d/rt-f074.md)

The [author log](cloud/claude-project-thread-jzhlen.md) records 1,045 unit passes, five skips and zero failures, including 23 parser cases with the hydrated LVL. The [Windows check for the merged PR head](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/actions/runs/41574925021/job/125716092224) passed. For this Markdown reconciliation, `python scripts/check-trackers.py` returned `TRACKERS_CONSISTENT` and `git diff --check` passed. RT-F021/F022 remain open and the registry remains 397/22/375.
