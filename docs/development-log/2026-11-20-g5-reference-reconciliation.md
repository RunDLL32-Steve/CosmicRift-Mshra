# G5 reference reconciliation — November 20, 2026

The queue now records G5 as merged at `8135f3e`. The [task brief](../tasks/g5.md), [weapon audit](../original-research/G5_WEAPON_VALUE_AUDIT.md) and [OriginalData entry](../../shared/RogueTrader.OriginalData/plan.d/g5.md) retain the distinction between stored values and native behavior. [PR 106](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/106)

Pinned upstream source inspection confirmed that the player timing block in `ScriptArena.cs` is commented out at `bb97579`. That removes it as evidence of enforced timing. G10 W2/W3 applies reconstructed positive units to the three starting cannons. InheritedSpeed -550, aliveTime -65, child angles and offsets, collision shape and the ammo revision mismatch remain open. [G10 verification](2026-11-10-g10-weapon-unit-review.md)

The [author log](cloud/claude-project-thread-5cw3pv.md) records 1,010 unit passes, five skips and zero failures in its final reported cloud run. The [Windows check for the merged PR head](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/actions/runs/41400853459/job/125132142518) passed. For this Markdown reconciliation, `python scripts/check-trackers.py` returned `TRACKERS_CONSISTENT` and `git diff --check` passed. No new gameplay acceptance was recorded. RT-F036/F037/F040 remain open and the registry remains 397/22/375.
