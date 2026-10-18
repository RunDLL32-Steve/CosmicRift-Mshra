# Client implementation plan

Source-audited October 17, 2026; dated acceptance records below remain historical. This documentation review runs no build, client, GUI or simulation.

Status authority: [master §18 M1 engineering acceptance/fidelity gate](../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [§1.3 current work](../ROGUE_TRADER_2_PLAN.md#13-continued-exact-reproduction--october132026), [§15 UI](../ROGUE_TRADER_2_PLAN.md#15-user-interface) and [§16 appearance](../ROGUE_TRADER_2_PLAN.md#16-graphical-direction). Original acceptance is tracked in the [feature catalog](../docs/original-research/FEATURE_CATALOG.md); the master is the status authority.

## Purpose and current code

Godot 4.5.1/.NET 8 is the local engineering testbed and original-content inspector. It is not the selected production engine. [GameClient.cs](Scripts/GameClient.cs) loads NEW/provisional ship, plasma and First Contact sector JSON, calls the pure C# world at 60 Hz, and connects rendering, camera, input, HUD and bounded QA modes. [WorldView.cs](Scripts/WorldView.cs) displays derived canonical Asp/Hornet pixels when staged; fallback ships, the training sector, rocks, projectiles, hit effects and handling remain provisional.

[OriginalSpriteLibrary.cs](Scripts/OriginalSpriteLibrary.cs) reads a separate derived manifest and validates paths, pinned provenance and atlas dimensions. F2 or ORIGINAL CONTENT opens a paused inspector; no gameplay ticks advance while it is visible. All 12 hull identities, 64 direction rows and 11 preview columns, 231 sprite atlases and the canonical 1999×1998 overview are available. This does not implement original map traversal or select native banking rules.

## Accomplished with evidence

| Scope | Completed evidence and limits |
|---|---|
| M0/M1 prototype client, camera, HUD, controls, lifecycle and Windows export | Historical 41-unit acceptance plus source/export three native-input/UI and six world phases; [October 12 acceptance](../docs/development-log/2026-10-12.md#final-acceptance-and-preservation-reconciliation). One provisional playable ship, three stationary targets and 12 static rocks. |
| Canonical overview/derived frame inspection, RT-F020/RT-F073 | Source Debug build zero warnings/errors and three rendered pages at world tick 0; [source ships log](../artifacts/qa/original-ships.log), [map log](../artifacts/qa/original-map.log), [sprites log](../artifacts/qa/original-sprites.log). |
| Standalone original viewer, RT-F373 | [Updated local Windows zip](../artifacts/windows/OriginalContent-2026-10-13-win-x64.zip); packaged ships/map/sprites pages pass resource/render checks at tick 0 with SDK/runtime discovery disabled. [Ships](../artifacts/qa/OriginalContent/standalone-ships.log), [map](../artifacts/qa/OriginalContent/standalone-map.log), [sprites](../artifacts/qa/OriginalContent/standalone-sprites.log). This is a local inspection artifact, not a complete gameplay release or redistribution grant. |
| Inspector navigation/input, RT-F374 | [Source record](../artifacts/qa/original-input-source-record.json) and [standalone input log](../artifacts/qa/OriginalContent/standalone-input.log): all 8,448 hull frame selections, 231 selector signals, retained selections, native editing, remaps, physical Escape/Close and tick 0 isolation. [Detailed completion history](ORIGINAL_INSPECTOR_PLAN.md). |

## Active outstanding work

- [ ] **Investigating — RT-F023–RT-F026/RT-F074–RT-F075:** native bank/state selection, palette/HSV/shadow/blit, animation/timing, layered effects/audio and original HUD/controls. Column 5 and direction rounding are preview choices.
- [ ] **Investigating — RT-F021/RT-F022/RT-F069:** original LVL/LIO coordinates, terrain/collision, objects and runtime dependency eligibility. The overview is an image; it is not a playable level.
- [ ] **Investigating — native engine route:** the separate native client reached Welcome FreeInfantry; Start gave no confirmed entry. No native arena/player/asset-delivery acceptance. Test processes and private profile were cleaned; do not interact with the desktop while the user is playing. See [native plan](../docs/architecture/NATIVE_CLIENT_PLAN.md).
- [ ] **Not started — original Godot runtime integration if required by engine choice:** canonical hull/weapon/loadout/economy/progression behavior. The shared prototype schema is not a conversion of all original records. Cargo/pickup, Persistence, Trading, Protocol and AI libraries now exist separately; `GameClient` still constructs `new WorldSimulation(_catalog)` without cargo configuration and has no session, transaction or NPC-controller integration.
- [ ] **Not started — M2/M3 final acceptance:** mining, trading, repairs, upgrades, saves and independent multiplayer through the selected engine; preserve original records rather than implementing unrelated proposals.

## Folder map and reproduction

[Scripts](Scripts/IMPLEMENTATION_PLAN.md), [UI](UI/IMPLEMENTATION_PLAN.md), [Scenes](Scenes/IMPLEMENTATION_PLAN.md), [Assets](Assets/IMPLEMENTATION_PLAN.md), [Audio](Audio/IMPLEMENTATION_PLAN.md) and [Shaders](Shaders/IMPLEMENTATION_PLAN.md) document their current modules and next work. Ignored client/GameData is build-staged JSON; client/OriginalContent is derived content; .godot is generated. They are documented here rather than treated as source tasks. Empty editor template/profile/theme directories contain no implemented feature.

Prototype bindings: W/Up forward, S/Down reverse, A/Left and D/Right turn, Space fire, Escape pause/controls, R reset, F2 original inspector. All eight actions are remappable; settings persist in user://controls.cfg. Reset replaces the training world; it does not save progression.

When an implemented feature needs re-verification, choose the focused mode: --verify-original-input or --inspect-original-content --verify-original-content --original-page=ships|map|sprites (optional --capture with an absolute PNG path). Original-content modes verify resources/input without advancing the world. Reproduce its package with scripts/export-windows.ps1 -Mode OriginalContent; the default Prototype mode performs historical gameplay smoke and is not needed for documentation/assets. Preserve original sources and keep outputs separate.
