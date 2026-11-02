Review status: ready-for-review

# Branch log: claude/project-thread-9vx2de (queue G8 evidence table)

Scope: queue [G8](../../tasks/g8.md), "Compare raw original flight constants to actually recorded native evidence. Produce a field-by-field evidence table". The row is marked Local; this branch does the part a cloud container can do (pinned originals, preserved emulator source, news text) and lists the native measurements that still need a local run. The cross-file negative-sentinel inventory belongs to `claim/g8a-format-audit` and is not repeated.

## Round 1 — November 2, 2026

-   New [FLIGHT_FIELD_EVIDENCE.md](../../original-research/FLIGHT_FIELD_EVIDENCE.md): unit chain (position px, velocity 0.001 px per 10 ms, yaw 240 per turn, no Y scaling for `Isometric=0`), one row per flight field f[202..211], f[362], f[363], f[373] with raw values, emulator use, proposed meaning, evidence class/confidence and counter-evidence; terrain index table from `rogue.cfg`; the −1 reverse sentinel with a cross-zone VEH survey (273 of 274 v53 Car rows hold −1/−1) and the July 15, 2002 chaos.nws statement.
-   Links from `FIELD_INTERPRETATION.md`, the G8 brief and the queue row.
-   No code, parser, test or game value changed. RT-F027/RT-F028 stay unchecked; feature counts unchanged.
-   Round 1b: recorded Steve's November 2 confirmation that reverse thrust equals forward thrust (section 4); reading adopted, still no value change.

Model/effort: brief asks Opus 5.5 / High; this run used Opus 5.5 at a lower effort setting.

Verified in the cloud container: `python scripts/audit-flight-fields.py` with pinned assets at `eb1514e` printed FLIGHT_FIELDS_OK; `python scripts/check-trackers.py` TRACKERS_CONSISTENT; `python scripts/format-check.py` FORMAT_CLEAN.

Not verified in cloud - local CI required: every "native" cell in the table. No native client was run and no footage was measured.
