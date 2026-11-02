# Branch log: claude/project-thread-5cw3pv (queue G5 remainder)

Scope: the remainder of queue [G5](../../tasks/g5.md), a value audit with no firing behavior. Result: [G5 weapon value audit](../../original-research/G5_WEAPON_VALUE_AUDIT.md) and the [OriginalData entry](../../../shared/RogueTrader.OriginalData/plan.d/g5.md).

Selection (November 2, 2026): Steve asked for the next three tasks and the coordinator assigned G5. The G5 row is OPEN on main with the RT-F036/F037/F040 data clauses merged; the old `cloud/rt-f036-f037-g5-weapon-records` and `cloud/rt-f040-mirv-launcher-chain` branches are those merged partial scopes (PRs #4 and #6, closed), and the queue text names the remainder as open. Claimed with an empty `CLAIM G5` commit on this branch. No overlap with G8, C7 (no `shared/` prediction file touched), PR #102 or C10.

## Round 1 — November 2, 2026

-   New `scripts/original-assets/audit_g5_weapon_values.py` (hash-verified, read-only) and `docs/original-research/G5_WEAPON_VALUE_AUDIT.md`.
-   Evidence added: the InfantryOnline loaders at `bb97579e` (already cited by FIELD_INTERPRETATION.md) name f[31..33] useAmmoID/ammoUsedPerShot/ammoCapacity and the type-8 entry as id/deltaX/deltaY/deltaZ/deltaAngle/multiLinkMode; that emulator never moves projectiles, so no negative-value evaluator exists there. rogue.cfg `[Soccer] DefaultInheritSpeedPercent=1000` shows per-mille inherit percentages for the ball (analogy only). NWS 1/13/02 confirms X-Laser projectile life was decreased; NWS 2/25/03 and 3/01/03 ammo weight and half-price resale notes do not match the pinned ITM (ammo mass 0), recorded as an open revision mismatch.
-   Code: `OriginalMultiItemEntry` now carries all six raw ints (closes RT-F040 review round 4 item 1); `OriginalProjectile` exposes seven more raw fields. New `tests/Unit/OriginalG5WeaponValueAuditTests.cs` (9 cases).

Verified in the cloud container (SDK 8.0.133 substituted for the pinned 8.0.419; global.json change not committed): `dotnet test tests/Unit` with pinned assets at `eb1514e`: 1004 passed, 0 failed, 5 existing skips. Audit script exits 0 with hashes verified.

Not verified: the pinned SDK and `verify.ps1` on Windows (no client change, so no Godot smoke needed).
