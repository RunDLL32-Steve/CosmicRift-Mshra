# Rogue Trader 2.0 — First Contact

Windows / Godot C# flight-and-combat testbed for Milestones 0 and 1 of [the master plan](ROGUE_TRADER_2_PLAN.md). Read [the startup instructions](PROJECT_START.md) first.

The user's target is the gameplay and assets shown in [Cosmic Rift — Chaos Zone](https://www.youtube.com/watch?v=A6u1ljFgQVU), applied to **Rogue Trader mode**. The initial testbed uses original geometric placeholders and provisional tuning. It does not yet reproduce the archived artwork, map, ship catalog or Rogue Trader economy. The [InfantryOnline research](docs/original-research/INFANTRYONLINE.md) records actual recovered files and the next fidelity work.

## Build and play

Run PowerShell from this repository. Setup downloads official, checksum-verified portable tools into ignored `.tools/`, without requiring a system SDK upgrade.

```powershell
./scripts/bootstrap.ps1 -IncludeExportTemplates
./scripts/verify.ps1
./scripts/run-client.ps1
./scripts/export-windows.ps1
```

Pinned toolchain: Godot **4.5.1 .NET x64**, .NET SDK **8.0.419**, C# 12 / `net8.0`. This is a reproducible compatible baseline, not a claim to be the newest release. See [compatibility and maintenance](docs/architecture/TOOLCHAIN.md).

The standalone build is `artifacts/windows/FirstContact/RogueTrader2.exe`. Distribute the entire `FirstContact` folder or `artifacts/windows/FirstContact-0.1.0-win-x64.zip`; the executable requires the adjacent packed resources and managed files. Launching it requires no development editor or SDK. Unsigned development build.

## Controls and exercise

- W / Up: forward thrust; S / Down: reverse thrust.
- A / Left and D / Right: rotate; Space: fire plasma.
- Escape: pause and open the controls/settings panel.
- R: reset the test range, including after destruction.

Use the controls panel to remap keys; bindings are saved in Godot's `user://` location. These are the master plan's modern defaults. Recovered historical keys are documented separately.

Fire toward the target directly above the spawn point until its shield and hull are exhausted. Accelerate, release thrust to drift, rotate while coasting, use reverse thrust to brake, and steer between the asteroids. The HUD shows speed, shields, hull, energy, targets and simulation ticks. There is no station, mining, economy or save-game progression in this milestone.

## Structure and status

- `client/`: Godot scenes, input, rendering and HUD.
- `server/RogueTrader.Simulation/`: Godot-independent authoritative simulation at 60 Hz.
- `shared/RogueTrader.GameData/` and `data/`: validated external JSON definitions.
- `tests/Unit/`: automated movement, collision and combat regression tests.
- `docs/original-research/`: historical evidence, archived Rogue Trader files and video observations.
- `scripts/`: pinned toolchain bootstrap, validation, launch and export.

[Development progress](docs/development-log/PROGRESS.md) reconciles all folder subplans with the [master checklist](ROGUE_TRADER_2_PLAN.md#18-development-roadmap). Later modules are reserved folders with deferred plans, not working multiplayer or persistence implementations.

Original assets are referenced for research, with provenance and reuse status recorded in [third-party notices](THIRD_PARTY_NOTICES.md). Verified derived originals are staged for local source inspection; the earlier Windows package remains the placeholder build.


## Canonical original content

The foundational source is [fowlmouth/cosmic-rift-assets](https://github.com/fowlmouth/cosmic-rift-assets) at `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`, designated by the user for 100% Rogue Trader reproduction. Preserve original bytes, values and names unchanged. Reference acquisition/verification:

```powershell
./scripts/sync-reference-assets.ps1
./scripts/verify-reference-assets.ps1
```

Originals live in ignored `.tools/references/cosmic-rift-assets/`; the committed [manifest](docs/original-research/ASSET_MANIFEST.json) records hashes and source identities. Conversion outputs must be stored separately. The original `LVL-BMP/Rogue.bmp` has been retrieved and visually inspected; it is not the placeholder playable sector. [InfantryOnline engine evaluation](docs/architecture/ENGINE_EVALUATION.md) is the next compatibility gate. The exact original reproduction remains unfinished.

Current verified result: 41 unit tests, three native-input/UI smoke phases, six flight/combat phases and a self-contained Windows launch with rendered QA.

## Current checkpoint — October 13, 2026

Research/implementation stopped at the user's usage-window boundary. Resume from [CONTINUATION.md](CONTINUATION.md).372 original features have master checkboxes and specific canonical-domain queues under data/original. Completed tasks move out of active lists into evidence/history and major-feature changelog.

The source client now displays all12 hulls,231 sprite objects and canonical1999×1998 overview. Run scripts/run-client.ps1 and press F2/ORIGINAL CONTENT; world stays paused during inspection. Python3.11+ with Pillow runs scripts/original-assets/decode_blo.py then stage_original_assets.py to reproduce staged resources. Source compile/3 rendered pages passed;14 importer checks and actual parser/loopback proof passed. Original flight/economy/native blit/banking still pending; updated standalone export has not been produced.
