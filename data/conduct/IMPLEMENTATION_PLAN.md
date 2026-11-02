# Conduct (bounty and reputation) data implementation plan

Authority: [master §10 bounty and reputation](../../ROGUE_TRADER_2_PLAN.md#10-bounty-and-reputation-system), [data plan](../IMPLEMENTATION_PLAN.md) and the queue [C9 brief](../../docs/tasks/c9.md). November 2, 2026.

## Purpose

`rules.json` is the data-driven rule file for `RogueTrader.Persistence.ConductRuleBook`. Each event names a fixed bounty change and fixed reputation changes per occurrence; the transaction core applies them through `RecordConductAsync` and saves the result in the profile (schema 5).

## From the originals

Nothing. The pinned `CFG/rogue.cfg` `[Bounty]` block (RT-F051: Start 1, PercentOfKiller 80, PercentOfAssist 20, Fixed 0) has undecoded units and formula, so no value from it is used. `OriginalRewardBlock` keeps the raw view.

## NEW/provisional

-   The whole contract (per-occurrence fixed changes, clamping after all occurrences, no decay or reset, neutral standing stored as absent) and every number in `rules.json`: the five categories from master §10.2, the ±1000 reputation range, the 100,000 bounty cap and all seven events.
-   No code raises these events yet: the client and the server session do not record conduct. Wiring kills, sales and mining to these events, bounty payouts and protected-sector rules (master §10.1) are later work.

## Open

-   [ ] RT-F051 bounty/cash/points variants: original semantics, formula and native verification (claimed by `cloud/rt-f051-reward-variants`; not touched here).
-   [ ] Reputation effects on station access, contracts, NPC hostility, equipment and sector permissions (master §10.2).
