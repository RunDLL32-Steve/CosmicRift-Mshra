# Reward data implementation plan

Authority: [master §18 Milestone 2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), [data plan](../IMPLEMENTATION_PLAN.md), queue [M7 brief](../../docs/tasks/m7.md) and [M9 brief](../../docs/tasks/m9.md). November 1, 2026.

## Purpose

`kill_reward.json` sets the credits the testbed client pays when the player destroys a sector practice target. `RogueTrader.Trading.KillRewardDefinition` loads it; the build stages it into `client/GameData/rewards/`.

## From the originals

| Data       | Source                                                                                                                                                                                                                    |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 75 credits | `CFG/rogue.cfg` `[Cash]` section at line 234 (`KillReward=75` on line 239) at pinned `eb1514e`, read by `OriginalRewardBlock` (RT-F051). `KillRewardTests.ShippedCreditsMatchThePinnedRogueCfg` fails if the file drifts. |

## NEW/provisional (listed in the file's `newRules`)

-   The raw value is paid as a flat credit amount per destroyed target. The units and formula of `[Cash]` (ShareRadius 0, SharePercent 0, PercentOfKiller 100, PercentOfTarget 1000) and `[Bounty]` are undecoded and not applied.
-   The three sector targets are NEW practice targets, not original NPCs. Each pays once per sector instance; targets come back on a sector reset or relaunch.
-   A kill drops nothing.

## Open

-   [ ] RT-F051: reward formula, sharing, bounty, award order and native verification.
-   [ ] Original NPC kills (pirates, RT-F060) and their drops; the provisional pirate controller (queue M6) is still not wired into the world.
