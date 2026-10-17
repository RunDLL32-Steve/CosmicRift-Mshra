# AI tuning data implementation plan

Authority: [master §12.1/§12.2](../../ROGUE_TRADER_2_PLAN.md#121-initial-npc-pirate), [AI module plan](../../server/RogueTrader.AI/IMPLEMENTATION_PLAN.md) and [data plan](../IMPLEMENTATION_PLAN.md). Added with TASK_QUEUE M6.

## Purpose and accomplished

`pirate_ai.json` is the external tuning for the initial NEW pirate controller (`PirateAiConfig`, strict loader: unknown fields rejected, ranges/health fractions validated). Every value is **NEW/provisional**: detection/lose/engage/standoff ranges, closing-speed limit, fire cone and range fraction, steering gains, patrol speed, seeded idle/acquisition/evade tick ranges, evade jink angle, and the 0.3 flee / 0.6 re-engage health fractions. Distances are world units, angles radians, durations 60 Hz ticks, speeds units per second. Evidence: `PirateAiTests` in `tests/Unit` load this file.

## Next and parity boundary

- [ ] Replace with recovered original controller/actor values when RT-F060/F272/F306 evidence exists; original data takes precedence. Until then this is an engineering fixture and not original parity.
- [ ] Playtest-tune after the AI is wired into a simulation that gives NPCs their own input.
