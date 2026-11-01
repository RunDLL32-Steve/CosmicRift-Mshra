Review status: ready-for-review

# Branch log: claude/project-thread-r5oe30 (queue M8)

Scope: queue M8, Godot HUD for cargo/currency, station UI and save/load flow. See [brief](../../tasks/m8.md), [client entry](../../../client/UI/plan.d/m8.md), [station data](../../../data/stations/IMPLEMENTATION_PLAN.md) and [persistence entry](../../../server/RogueTrader.Persistence/plan.d/m8.md).

Status: round 3 pushed (review round 2 fixes), awaiting re-review. Not merged by the author.

## Round 2 — November 1, 2026

-   Steve asked to address the round 1 limits and chose the "original-backed station": the fixture station is replaced by `data/stations/trading_outpost.json`, derived from the pinned ITM/VEH by `OriginalStationBuilder` and guarded by an oracle test; hull, position and heading are saved (schema 3); owned upgrades change the flying ship through labelled NEW effects.
-   R1-1 fixed: `StationProfileSession.RepairAsync` takes current and maximum hull and refuses with `NothingToRepair` (no core call, no charge) at full hull or when destroyed; REPAIR is disabled in that state; unit theory for both cases and a smoke step.
-   R1-2 fixed: the open/reload outcome is kept in its own field and the offline text is built from it, so it never stacks.
-   R1-3 fixed: a refused pickup commit no longer resets the hold; the units stay, the profile is reloaded after a save failure, and the commit retries once a second. The smoke test suppresses commits after a pickup, checks the units stay uncommitted in the hold, then checks they commit.

Verified in the cloud container (SDK 8.0.133 and Godot 4.5.1 mono Linux substitutions, as round 1): solution build 0 warnings; `dotnet test tests/Unit` with pinned assets at `eb1514e` 848 passed, 0 failed, 5 existing skips; Godot import exit 0; `--smoke-test` exit 0 with CLIENT_INPUT_SMOKE_PASS, CLIENT_STATION_SMOKE_PASS and CLIENT_SMOKE_PASS; `format-check.py` FORMAT_CLEAN; TRACKERS_CONSISTENT.

Not verified: the `verify.ps1` wrapper itself (its steps were run directly; Windows CI runs it), the Windows export, pinned SDK, runtime/native-delivery checks (paths untouched).

Resume point: independent re-review of the round 3 head; M9 after merge.

## Round 3 — November 1, 2026

-   R2-1 fixed: `StationProfileSession.BuyUpgradeAsync` refuses an upgrade that would not become active (`UpgradeHasNoEffect` for no effect yet, `UpgradeSlotHeld` when a higher-priced owned item holds its held category) before calling the core, so nothing is charged; the Upgrades tab shows such rows disabled with the reason. Unit test and smoke step.
-   R2-2 fixed: buying an upgrade or reloading the profile no longer rebuilds the world. `WorldSimulation.ApplyPlayerDefinition` swaps the ship definition in place (same radius required; hull, shield and energy clamped to the new maxima, never refilled), so piles, targets, velocity and a destroyed state are kept. Unit test and smoke checks (same world instance, same piles after upgrade and reload). Reopening the save folder no longer saves the ship first unless docked, so it reopens exactly what is on disk.
-   R2-3 fixed: the unused `StationScreen.BuyRequested` event is removed; `--no-incremental` builds of the solution and client report 0 warnings.

Verified in the cloud container: `dotnet test tests/Unit` 850 passed, 0 failed, 5 existing skips; Godot `--smoke-test` exit 0 with CLIENT_INPUT_SMOKE_PASS, CLIENT_STATION_SMOKE_PASS and CLIENT_SMOKE_PASS; `format-check.py` FORMAT_CLEAN; TRACKERS_CONSISTENT.
