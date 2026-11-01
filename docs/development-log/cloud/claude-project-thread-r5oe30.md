Review status: ready-for-review

# Branch log: claude/project-thread-r5oe30 (queue M8)

Scope: queue M8, Godot HUD for cargo/currency, station UI and save/load flow. See [brief](../../tasks/m8.md), [client entry](../../../client/UI/plan.d/m8.md) and [station fixture plan](../../../data/stations/IMPLEMENTATION_PLAN.md).

Status: implementation complete, awaiting independent review. Not merged by the author.

Verified in the cloud container on November 1, 2026:

- Toolchain substitution: .NET SDK 8.0.133 (Ubuntu package; the pinned 8.0.419 download host is blocked here, `global.json` was relaxed locally only and is not committed) and the official Godot 4.5.1 mono **Linux** build instead of the pinned Windows build.
- `dotnet build RogueTrader2.sln`: succeeded, 0 warnings.
- `dotnet test tests/Unit` with pinned original assets hydrated at `eb1514e`: 838 passed, 0 failed, 5 skipped (the existing key-listing skips). 10 new tests in `tests/Unit/Trading/StationProfileSessionTests.cs`.
- Godot `--headless --editor --import --quit`: exit 0. Godot `--headless -- --smoke-test`: exit 0, with `CLIENT_INPUT_SMOKE_PASS`, `CLIENT_STATION_SMOKE_PASS` and `CLIENT_SMOKE_PASS`.
- Rendered captures under Xvfb/OpenGL for the HUD, the pause menu and the station screen (attached to the PR).
- `python scripts/check-trackers.py`: TRACKERS_CONSISTENT.

Not verified: `scripts/verify.ps1` itself (PowerShell wrapper; its build, unit test, import and smoke steps were run directly), the Windows export, pinned SDK, runtime/native-delivery checks (paths untouched).

Resume point: independent review of the PR; M9 (exit/relaunch acceptance) can start after merge.
