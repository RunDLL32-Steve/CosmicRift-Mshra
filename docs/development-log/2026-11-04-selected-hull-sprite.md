# Selected hull sprite correction — November 4, 2026

Review status: independent review pending. Branch: `codex/selected-hull-sprite`, based on `f1a1999` (`origin/main`). This fixes the reported player sprite remaining Asp after selecting another owned ship. Scope authority: [master §6](../../ROGUE_TRADER_2_PLAN.md#6-spacecraft-and-loadout-system), [§16](../../ROGUE_TRADER_2_PLAN.md#16-graphical-direction), [§18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [client item plan](../../client/plan.d/selected-hull-sprite.md).

## Change and acceptance

The ship selection/profile flow already replaces the world's player definition. The renderer ignored its ID and called `HullSprite("Asp")` for every player. `OriginalSpriteLibrary.HullSpriteForVehicle` now resolves the unchanged derived manifest's canonical vehicle ID; `WorldView.DrawShip` binds that player's atlas with the existing source dimensions, direction row and preview column 5. Inspector name lookup stays available. Practice targets keep the explicit provisional Hornet preview because their current simulation definitions inherit the player hull. Unknown/legacy IDs and absent originals draw the existing geometric fallback.

The regression mode `--verify-hull-sprites` creates a fresh GUID profile under the worktree's isolated portable user profile, owning only fixture roster hulls with a zero wallet. The mode overrides any supplied profile directory and removes its fixture at exit. No user profile, canonical source bytes, original value or data JSON is edited.

## Focused evidence

-   `artifacts/qa/hull-sprites.log`: headless Godot exit 0, all 12 picker selections bind 12 distinct canonical `Texture2D.ResourcePath` values immediately before `DrawTextureRectRegion`; saved active hull, actual Hangar button switch, reload and disk reopen retain the expected binding. `starter_interceptor` draws geometric fallback; an unknown ID returns no original mapping; absent originals draw fallback. World tick stays 0.
-   `artifacts/qa/hull-baseline.log`: temporarily restoring only the old hardcoded Asp player lookup makes the same fixture exit 1 on Excelsior: actual `TerranShipsSmall/Asp.png`, expected `TerranShipsSmall/Excelsior.png`. The fixed source was restored before final verification.
-   `artifacts/qa/hull-import.log`: isolated Godot import exit 0. Initial client build: zero errors; advisory-feed `NU1900` warnings reflect unavailable NuGet network access. Headless startup also reports the environment's certificate-store error and existing deferred focus warnings; the bounded assertions pass.
-   `artifacts/qa/hull-resources.log`: original-content resource inspection exit 0, 12 hull identities, 231 atlases, 1999×1998 map and level art (213 objects, three mineral sprites, 22,356 placements); world tick 0.
-   `artifacts/qa/hull-reference-integrity.log`: `scripts/verify-reference-assets.ps1` passes all 46 foundational source hashes/lengths at canonical commit `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`.
-   `scripts/verify.ps1` exit 0 with `RT_ORIGINAL_ASSETS_ROOT=C:/Repos/CosmicRift/.tools/references/cosmic-rift-assets`: solution build zero errors and six offline `NU1900` warnings; units **1,227 passed / 0 failed / 5 skipped / 1,232 total** (`artifacts/test-results/unit.trx`), integration **6 passed / 0 failed / 0 skipped** (`integration.trx`), Godot import and smoke passed (four input/map, 15 station, six flight/combat phases). The five skips are archived `keys.txt` supplementals: `RT_ROGUE_KEYS_TXT` is not supplied. Full wrapper log: `artifacts/qa/selected-hull-verify.log`.
-   Final fixed-build hull regression exit 0, `python scripts/check-trackers.py` prints `TRACKERS_CONSISTENT` (397/22/375), formatting and `git diff --check` pass. Independent reviewer rerun and publication gates remain pending.

## Reproduce

With the pinned toolchain and staged canonical-derived `client/OriginalContent`:

```powershell
. ./scripts/environment.ps1
& $dotnetExe build client/RogueTrader.Client.csproj --configuration Debug --nologo
& $godotExe --headless --path client --editor --import --quit
& $godotExe --headless --path client -- --verify-hull-sprites
```

Native-delivery/login/runtime-wrapper tests and Windows export are outside this rendering fix; no milestone boundary or native code changed. No separate gameplay simulations are needed beyond the repository's required client verify smoke. Original hull/pose/banking/palette/shadow/gameplay fidelity remains unfinished; no RT or milestone boxes change, registry 397/22/375.

Resume point: coordinating inspection, a fresh independent review with focused checks, then authorized publication/CI/merge gates. The author does not merge this branch.
