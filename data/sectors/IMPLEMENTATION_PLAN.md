# Sectors fixture implementation plan

Authority: [master §18 M0/M1 engineering and M2](../../ROGUE_TRADER_2_PLAN.md), [data plan](../IMPLEMENTATION_PLAN.md) and [canonical sectors plan](../original/sectors/IMPLEMENTATION_PLAN.md). Reconciled October 13, 2026.

## Purpose and accomplished

One NEW First Contact sector in `first_contact.json`: player spawn, three stationary targets and twelve static asteroid circles. The Godot build stages it into `client/GameData/sectors/`. It is separate from original Rogue.lvl/LIO/BMP. Its tuning is explicitly **NEW/provisional**. The fixture is complete for the accepted engineering prototype; no code/data changes were made in this documentation pass.

Evidence: M0/M1 placement, collision and target acceptance in the historical unit/source/export checks; current JSON matches the [balance ledger](../../docs/balance/PROVISIONAL_VALUES.md).

## Next and parity boundary

- [ ] **Deferred replacement until validated original route:** Preserve this accepted fixture; validate unchanged full original LVL delivery, native world/collision and LIO interactions through the canonical queue.

No new tuning is needed for the current engineering fixture. It is not canonical original content and does not complete any original per-record gameplay feature. The unchanged originals, source identities, current stage and feature-specific next steps belong in the canonical plan above; completed acceptance belongs in [CHANGELOG.md](../../CHANGELOG.md) and the master audit.
