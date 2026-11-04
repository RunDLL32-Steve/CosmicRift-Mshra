# Cosmic Rift: Rogue Trader reproduction

The target is full original Rogue Trader gameplay and presentation using the unchanged canonical content. The repository currently contains a Godot flight/combat testbed, an original-content inspector, raw-data readers, generic gameplay modules and an isolated InfantryOnline compatibility toolchain. **Full original parity and native playable entry remain unverified.**

Start with [PROJECT_START.md](PROJECT_START.md), the [master specification](ROGUE_TRADER_2_PLAN.md) and [AGENTS.md](AGENTS.md). Use [CONTINUATION.md](CONTINUATION.md) for current status, [TASK_QUEUE.md](TASK_QUEUE.md) for task selection and the [documentation index](docs/README.md) for detailed plans.

## Build and play

From the repository root in PowerShell:

```powershell
./scripts/bootstrap.ps1 -IncludeExportTemplates
./scripts/verify.ps1
./scripts/run-client.ps1
./scripts/export-windows.ps1
```

The pinned inspection/testbed toolchain is Godot **4.5.1 .NET x64**, .NET SDK **8.0.419**, C# 12 / `net8.0`; see [TOOLCHAIN.md](docs/architecture/TOOLCHAIN.md). The compatibility toolchain separately pins .NET SDK 10.0.401. Bootstrap downloads checksum-verified tools to ignored `.tools/`.

Default export produces `artifacts/windows/FirstContact/RogueTrader2.exe` and `FirstContact-0.1.0-win-x64.zip`. Keep the entire export folder together. `./scripts/export-windows.ps1 -Mode OriginalContent` stages canonical-derived sprites/map data and produces the separate original-content inspection package; it requires the reference assets and Python/Pillow. The October 12 prototype and October 13 viewer launch checks are historical evidence, not checks rerun by this documentation audit.

## Current behavior

The client has provisional flight/combat controls: W/Up thrust, S/Down reverse, A/Left and D/Right rotate, Space fire, Escape pause/settings, R reset. The settings panel remaps these controls and saves bindings in Godot's `user://` location. F2 opens the original-content inspector when staged assets are available. Native historical controls remain a separate fidelity task.

Selecting a hull now displays its matching canonical sprite by vehicle ID, including Hangar switches and restored profiles. Legacy hulls without an original mapping use the geometric fallback. See the [bounded renderer regression and limits](docs/development-log/2026-11-04-selected-hull-sprite.md); original banking/pose semantics and full hull parity remain open.

The playable test range has no station UI, mining loop or save-game progression. Source modules now provide cargo pickup, optional generic mining extraction, persistence/transactions, trading, replay, binary message serialization and provisional pirate AI. [Queue M3 mining](docs/development-log/2026-10-18-m3-mining.md) uses explicitly NEW deposit/equipment definitions, finite authoritative quantities and collectible drops; original mining rules remain open. They have focused test source and dated implementation evidence, but are not connected into a complete client gameplay loop. The dedicated production server, transport/authentication, original NPC behavior and multiplayer acceptance remain unfinished.

C7 has **scoped prediction library acceptance verified**; [PR 117](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/117) is published and current-head review and CI remain pending. Bounded intent, correction and replay share the authoritative motion path; deterministic latency/loss tests cover the pure component. See the [completion audit](docs/development-log/2026-11-02-c7-completion-audit.md) and [publication evidence](docs/development-log/2026-11-03-c7-publication.md). Transport and renderer integration and master M3 acceptance remain open; rules and fixtures are NEW/provisional.

## C1 raw BLO reader — November 2, 2026

C1 scoped raw-reader acceptance is verified and [PR 116](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/116) merged. The pure C# reader preserves owned bytes, raw directory fields, ordinals, offsets, lengths and payload hashes for all **89 pinned archives / 1,262 members**, guarded by canonical path, length and SHA-256 verification. See the [contract](docs/original-research/C1_BLO_READER_CONTRACT.md) and [evidence](docs/development-log/2026-11-02-c1-blo-reader.md). Extraction, pixels, dependency resolution, native/gameplay acceptance and RT-F023/RT-F069 remain open.

## Code map

| Area                                                 | Source                                                                                                               |
| ---------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| Godot input/rendering/HUD and original inspector     | [client](client/IMPLEMENTATION_PLAN.md)                                                                              |
| Fixed 60 Hz simulation, cargo and replay             | [Simulation](server/RogueTrader.Simulation/IMPLEMENTATION_PLAN.md)                                                   |
| Raw original records and reference guards            | [OriginalData](shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md)                                               |
| Validated provisional JSON definitions               | [GameData](shared/RogueTrader.GameData/IMPLEMENTATION_PLAN.md), [data](data/IMPLEMENTATION_PLAN.md)                  |
| Profile repositories and transaction core            | [Persistence](server/RogueTrader.Persistence/IMPLEMENTATION_PLAN.md)                                                 |
| Generic station transactions                         | [Trading](server/RogueTrader.Trading/IMPLEMENTATION_PLAN.md)                                                         |
| Motion prediction; no renderer/transport integration | [Prediction](shared/RogueTrader.Prediction/IMPLEMENTATION_PLAN.md)                                                   |
| Message codec; no live transport                     | [Protocol](shared/RogueTrader.Protocol/IMPLEMENTATION_PLAN.md)                                                       |
| Provisional controller; no client integration        | [AI](server/RogueTrader.AI/IMPLEMENTATION_PLAN.md)                                                                   |
| Headless session and input queue; no transport       | [Server](server/RogueTrader.Server/IMPLEMENTATION_PLAN.md), [tick benchmark](tests/LoadTests/IMPLEMENTATION_PLAN.md) |
| Derived upstream tooling and checks                  | [compatibility](scripts/compatibility/IMPLEMENTATION_PLAN.md)                                                        |
| Focused regression source                            | [unit tests](tests/Unit/IMPLEMENTATION_PLAN.md)                                                                      |

## Canonical content and acceptance

The user-designated foundation is fowlmouth/cosmic-rift-assets at `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`. Preserve source bytes, names and values. [ASSET_MANIFEST.json](docs/original-research/ASSET_MANIFEST.json) records provenance and hashes; [third-party notices](THIRD_PARTY_NOTICES.md) record reuse limits.

```powershell
./scripts/sync-reference-assets.ps1
./scripts/verify-reference-assets.ps1
```

Originals live in ignored `.tools/references/cosmic-rift-assets/`; derived outputs stay separate. See the [engine evaluation](docs/architecture/ENGINE_EVALUATION.md) for the open production-engine decision.

G9 remains claimed/in progress; its [ledger](scripts/original-assets/plan.d/g9.md) indexes accepted dated evidence and open native/original limits. Current [initialized reset-input observation](docs/development-log/2026-10-19-g9-reset-input-observation.md) captures the selected standalone initialized object before arena creation; no session/native reset eligibility or full closure follows. The ledger-linked branch log records live workflow status. Prior lifecycle source and initializer acceptance remain dated history.

Current registry: **397 feature entries, 22 scoped completions, 375 open**. M0/M1 prototype acceptance is historical. The original mining/trading/upgrades/persistence loop and native appearance are not accepted. The [October 17 audit](docs/audits/2026-10-17-documentation-audit.md) distinguishes code inspection, recorded evidence and checks actually rerun.
