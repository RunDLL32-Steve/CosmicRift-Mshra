# Ships fixture implementation plan

Authority: [master §18 M0/M1 engineering and M2](../../ROGUE_TRADER_2_PLAN.md), [data plan](../IMPLEMENTATION_PLAN.md) and [canonical ships plan](../original/ships/IMPLEMENTATION_PLAN.md). Reconciled October 13, 2026.

## Purpose and accomplished

One NEW Interceptor engineering fixture in `starter_interceptor.json`. The Godot build stages it into `client/GameData/ships/`. Its tuning is explicitly **NEW/provisional**. The fixture is complete for the accepted engineering prototype; no code/data changes were made in this documentation pass.

Evidence: M0/M1 historical 41-unit-case and source/export acceptance; current JSON matches the [balance ledger](../../docs/balance/PROVISIONAL_VALUES.md).

## Next and parity boundary

-   [ ] **Deferred replacement until validated original route:** Preserve this accepted fixture while native compatibility is evaluated; validate all twelve original hulls and exchanges in the canonical queue.

No new tuning is needed for the current engineering fixture. It is not canonical original content and does not complete any original per-record gameplay feature. The unchanged originals, source identities, current stage and feature-specific next steps belong in the canonical plan above; completed acceptance belongs in [CHANGELOG.md](../../CHANGELOG.md) and the master audit.
