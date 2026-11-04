# Sectors fixture implementation plan

Authority: [master §18 M0/M1 engineering and M2](../../ROGUE_TRADER_2_PLAN.md), [data plan](../IMPLEMENTATION_PLAN.md) and [canonical sectors plan](../original/sectors/IMPLEMENTATION_PLAN.md). Reconciled October 13, 2026.

## Purpose and accomplished

One NEW First Contact sector in `first_contact.json`: player spawn, three stationary targets and twelve static asteroid circles. The Godot build stages it into `client/GameData/sectors/`. It is separate from original Rogue.lvl/LIO/BMP. Its tuning is explicitly **NEW/provisional**. The fixture is complete for the accepted engineering prototype; no code/data changes were made in this documentation pass.

November 1, 2026: `targetRespawnSeconds` (60, NEW, chosen by Steve) respawns the whole target group one minute after its last target is destroyed; 0 or absent means never. The minute is real time and keeps running while the player is docked or paused. Evidence: [TargetRespawnTests.cs](../../tests/Unit/TargetRespawnTests.cs) and the client station smoke test.

Evidence: M0/M1 placement, collision and target acceptance in the historical unit/source/export checks; current JSON matches the [balance ledger](../../docs/balance/PROVISIONAL_VALUES.md).

November 4, 2026 (full-size sector, G10 F10): the game flies `rogue.json` on `rogue_map.json`, both derived by `OriginalMapBuilder` from the pinned originals and checked by [OriginalMapTests.cs](../../tests/Unit/OriginalMapTests.cs) (set `RT_WRITE_DERIVED_MAP=1` to regenerate; the map needs the Git LFS bytes of `LVL/Rogue.lvl`). `rogue_map.json` holds the 1999 × 1998 tiles of the original map (wall bit and terrain number, run-length encoded) and each terrain's CFG PrizeExpire. `rogue.json` spawns the player at Cold Harbor SpawnA (LIO row 6, the CFG initial-entry warp for team 0, minus the 48-pixel LVL offset) and moves this file's targets and asteroids, unchanged in shape, to a NEW practice range at (4400, 1600) east of Cold Harbor. `first_contact.json` stays as the open practice sector the flight smoke test and the simulation tests fly. A sector saved before the change is moved onto the map once (`SectorMap.FromLegacyPracticeSector`), so nothing is refilled or lost.

## Next and parity boundary

-   [ ] **Deferred replacement until validated original route:** Preserve this accepted fixture; validate unchanged full original LVL delivery, native world/collision and LIO interactions through the canonical queue.

No new tuning is needed for the current engineering fixture. It is not canonical original content and does not complete any original per-record gameplay feature. The unchanged originals, source identities, current stage and feature-specific next steps belong in the canonical plan above; completed acceptance belongs in [CHANGELOG.md](../../CHANGELOG.md) and the master audit.
