# Changelog

## G9 selected-catalog probe strengthening — October 18, 2026

-   Implement complete raw/actual-ID and object-lookup diagnostics in existing ArenaProbe, with selected input/digest/loaded-assembly provenance and literal missing66 check. [Readiness/evidence](docs/development-log/2026-10-18-g9-catalog-membership.md): first-author and cleared round1-rework wrapper/helper runs exit0, Accepted=true,200 exact IDs with consistent object lookups and expected missing66 Warning. No original feature/count or production rule change.

## G9 Uiart/logo documentation precision — October 18, 2026

-   Preserve known exact-name absence while removing unsupported visual/intent/shipping/fallback claims; record bounded pinned Uiart string/archive access and consolidate stable G9 summaries around its ledger. [Evidence and limits](docs/development-log/2026-10-18-g9-uiart-source.md). Markdown only; no accepted checks rerun, original changes or feature/count completion.

## G9 Portal source roles — October 18, 2026

-   Distinguish this pinned loader's Portal iconGraphic pair from its None/None prefire/warp slots and the selected archive-container loading path. [Raw/normalized roles, source evidence and limits](docs/development-log/2026-10-18-g9-portal-source.md). Markdown only; no missing-warp-effect/replacement/native-role acceptance, runtime rerun or feature/count change.

## G9 BallGraphic source applicability — October 18, 2026

-   Document the pinned C# string/archive-access boundary and nearby sprite-reader/version contexts without treating a seven-token shape as CFG grammar evidence. [Source table and limits](docs/development-log/2026-10-18-g9-ballgraphic-source.md). Markdown only; no parser/runtime rerun, original value change or acceptance/count change. G9/RT-F069/RT-F379 remain open.

## G9 bounded maker/death diagnostic — October 18, 2026

-   Trace pinned VehicleMaker83/84/85 → Computer425/426/427 sources and add nine actual-handler diagnostics to existing RuntimeChecks. Isolated packet/action/factory/death/veto controls pass within70 checks, retaining canonical bytes/values and Nanobot2023; no native client, poll or physics ticks. [Evidence and limitations](docs/development-log/2026-10-18-g9-creation-source.md). G9/RT-F069/RT-F386 remain open; no production rule or original completion claim.

## G9 dependency documentation checkpoint — October 18, 2026

-   Reproduce the existing 89-archive / 1,262-member / 609-candidate inventory in memory, retaining eight raw residuals, and reconcile current supplemental byte access with historical package/startup evidence. Record conditional Computer explosion/death source paths and remaining offline/native work in the [G9 ledger](scripts/original-assets/plan.d/g9.md). No executable behavior, canonical content, registry count or acceptance checkbox changes; G9 and RT-F069 remain in progress/open.

## Generic mining extraction — October 18, 2026

-   Queue M3: optional external deposit/equipment definitions, deterministic surface-range extraction, finite asteroid depletion and collectible-to-cargo capacity handling. All extraction tuning and rules are NEW/provisional. Existing clients omit mining definitions and keep their current behavior.
-   Replay format v2 records Mine input and hashes deposits, cooldown, generated-pile sequence and pile geometry; v1 is explicitly rejected. [Local evidence and remaining fidelity limits](docs/development-log/2026-10-18-m3-mining.md). RT-F042 and original Milestone 2 acceptance remain unchecked; registry counts unchanged.

## Documentation and task organization — October 17, 2026

-   Reconcile active source/module, merge-state, compatibility-check and parity-limit descriptions against `d14df02`; distinguish dated runs from current source and full original acceptance. Correct the BallGraphic archive/member interpretation and one vehicle-loader XML field-count comment using preserved source evidence. No executable behavior or canonical content changes.
-   Move superseded root reports/handoffs to `docs/archive/2026-10/`, add a documentation index and give each active queue item a separate source-referenced brief plus a reusable template. Keep original feature/milestone acceptance flags and full scope unchanged.
-   Record audit coverage, local-reference/tracker checks and independent review under `docs/audits/`; no builds, native desktop runs or gameplay simulations for this documentation work.

## Trading station buy/sell - October 16, 2026

-   Add `server/RogueTrader.Trading` (queue item M4): a dock-radius proximity predicate in world units, a `StationPriceTable` price source (explicit tables or a provisional binding of the original ITM buy/sell fields with sentinels kept raw), `CatalogCargoPolicy` (the production `ICargoPolicy` over the simulation `CargoCatalog`) and `TradingService`, which passes the server-side docking verdict to the Persistence transaction core. Selling credits and removes atomically, buying checks wallet and cargo capacity, and replays stay idempotent. A new small project was unavoidable because Persistence and Simulation do not reference each other.
-   28 new unit tests (553 total, 5 pre-existing skips) cover success, insufficient funds, cargo full, out of range, replay, atomicity under injected faults and conservation. Every dock radius, capacity and price is NEW/provisional; the raw ITM sell value -1 is treated as not traded only in this provisional rule. RT-F044, RT-F045 and RT-F049 stay open (original station inventories, units and proximity are unresolved); nothing is wired into the client.

## Generic cargo capacity and collectible pickup - October 16, 2026

-   Add `CargoCatalog`/`CargoHold` to `server/RogueTrader.Simulation`: data-driven (JSON or code) hard weight capacity and per-item unit weights; pickup adds as many units as fit, refuses the rest, never creates or destroys units. `WorldSimulation` collects `CollectibleState` piles along the swept ship path each fixed tick, in catalog order, leaving any remainder in space; an optional initial inventory seeds the hold from a persisted profile.
-   19 new unit tests (525 total) cover capacity edges, partial/refused pickup, per-tick conservation, seeded random invariants, replay and 30 Hz determinism, JSON validation, `ICargoPolicy` equivalence and the seven original Resources masses. All capacities, quantities and radii are NEW/provisional. RT-F030 and RT-F042 stay open: the native threshold/overload formula, pickup range and LIO spawn semantics are unresolved, and nothing is wired into the client or transaction processor yet.

## Original hull raw-data loader - October 15, 2026

-   Add `shared/RogueTrader.OriginalData`: raw CSV record reader (every field by index, source file, 1-based row, version token), SHA-256/length guard for RPG/Rogue.rpg, VEH/Rogue.veh and ITM/Rogue_mp.itm, and a loader that links the twelve RPG hull rows to VEH Car rows by vehicle id. Typed accessors return raw unscaled values with their field index; negative values are flagged, never replaced; all sixteen terrain speed blocks stay available.
-   40 new unit tests (81 total) assert the RT-F001-RT-F012 catalog values; assets are located from the environment, repository or git and tests are reported as skipped if absent. Originals unchanged. No hull checkbox ticked: native selection, loadout and movement remain unverified.

## Persistence and transaction core — October 15, 2026

-   Add `server/RogueTrader.Persistence`: immutable player profile, in-memory and isolated file repositories (atomic temp-and-replace write, previous-generation backup, SHA-256 checksum envelope, quarantine instead of deletion, versioned schema with migration hook, single-owner lock) and an idempotent, per-profile serialized, revision-checked transaction processor (buy, sell, buy upgrade, grant reward) with typed rejection reasons.
-   129 new unit tests cover the master 20.2 economy bullets, concurrent double-spend, crash recovery and corruption handling (210/210 total with the hull loader). Review fixes: a save file holding another (or case-variant) profile id is refused instead of served, Windows device names are invalid profile ids, ids with unpaired surrogates are rejected instead of being altered on save, reward items are snapshotted, and the in-memory repository is thread-safe across profiles. The layer is data-agnostic and not wired into gameplay; no original-parity feature or master M2 box is checked.

## Native runtime safeguards — October 14, 2026

-   Fix derived emulator sales: validate owned quantity and widened cash arithmetic, recheck script mutations, remove inventory before crediting and sync the committed balance.
-   Fix null-script capacity crashes while retaining existing sport queue exceptions.
-   Correct bounded-host radar configuration; reject invalid divisors and logged runtime exceptions. Share fresh configuration generation and compile before execution to prevent stale verification.
-   Verify 35 actual-handler checks against 16 reproduced baseline failures, plus invalid-config rejection and a 10-second full-asset host with zero exceptions. Canonical/upstream files remain unchanged.
-   Catalog purchase overflow/fulfillment/cash-ordering separately as open RT-F396. Native entry, store eligibility, durable progression and full original gameplay remain unverified.

## RT-F397 isolated issue branch — October 14, 2026

-   Prepare a derived-only correction for full int length accounting in both outgoing byte-array writers, preserving serialized bytes and received-packet parsing. Add canonical cache/packet checks and60 whole/indexed boundary/queue fixtures. Root baseline exposed29 canonical length discrepancies,24 synthetic length defects and16 incorrect queue choices; fixed passes all46 canonical packets/60 fixtures with unchanged wire bytes and originals; native receiver delivery is unverified.

## Original package and native-route verification — October 13, 2026

-   Produce standalone OriginalContent Windows package;source/export nativeUI frame/navigation/remap regression andthree renderedpages pass atworldtick0 withoutSDK/runtime discovery.
-   Inventoryall89 archives/1,262members and609candidates;retainmissing/unknownreferences withoutsubstitution. Crosscheck12hulls,200items,30vehicles,81LIO,19,198originalpropertyvalues and222maprefs;preserve fullLVL graphicaltrailer.
-   Initialize actualoriginal genericarena:25computers,100materialdrops,51teams,42hides withzero gameplayticks/clientinputs.
-   FixderivedVehicleMaker originalfield18 omission:exactly8 bountyvalues corrected,allotherexports unchanged;originals intact.
-   RecoverauthenticsharedSoccerBLO andverifyfullasset boundedloopbackhost/controlled120sexit. Compileofflinelegacy patch-endpoint guard;nativehandshake acceptance remainsopen.
-   Prepare/run user-approved isolatednativecopy;welcomeobserved,Start/entrynotverified. Desktopautomation stoppedwhileuserplaying;onlyownedprocesses/testkeys cleanedup.394featurechecks/subplans reconciled.

## Original fidelity checkpoint — October 13, 2026

-   Add verified original BLO/CFS importer/stager:11 archives,231 objects,36,516 frames;14 focused pixel/format/hash checks passed.
-   Add source-client inspector:all12 original hulls/direction/state frames, sprite members and exact map overview;3 rendered pages verify tick0/resources and Debug build has zero warnings/errors.
-   Build/run actual pinned InfantryOnline parser and bounded offline loopback/ping host; original help dependencies recovered with provenance, owned processes/sockets cleaned up.
-   Fix compatibility runner unhandled diagnostic IOException/by-ref JSON serialization; retain separate upstream shutdown race/native-client parity as open.
-   Catalog372 stable original features and six per-record domain plans; reconcile folder docs with two additional agents and save continuation checkpoint at user's usage-window boundary.
-   Updated standalone export, native appearance/gameplay/economy and full dependency closure remain unfinished.

## 0.1.0 — First Contact — October 12, 2026

-   Establish Godot/.NET solution, portable pinned toolchain, local Git, modular folders, conventions, scripts and automated Windows workflow.
-   Implement 60 Hz momentum flight and projectile combat in a Godot-independent C# simulation, driven by validated external JSON.
-   Add playable Godot client with camera, telemetry HUD, asteroids/targets, geometric placeholder graphics, remappable controls and pause/reset.
-   Add automated movement, combat and data validation tests plus source/export runtime acceptance harnesses and rendered QA captures.
-   Document original Rogue Trader evidence, InfantryOnline archive/parser/asset candidates, supplied gameplay reference and historical-vs-provisional differences.
-   Reconcile master scope with the requested original gameplay and assets for Rogue Trader; add folder subplans and a central progress index.
-   Install the requested Watch skill separately from the game project.

Windows prototype acceptance passed: 41 unit tests, native-input/world smoke in source and export, and rendered standalone launch without SDK/runtime discovery. Original asset/content fidelity, Rogue Trader economy/progression and multiplayer remain subsequent work.

Canonical fowlmouth original assets are now acquired as an unchanged reference foundation with 46 hash/byte-length checks. Original reproduction and InfantryOnline engine compatibility remain separate open gates.

-   Add unchanged original-content indexing and canonical hull/resource catalog; validate3,086 raw records and preserve source hashes.
-   Evaluate InfantryOnline/native-client compatibility and record a concrete original-content engine spike before committing to the production engine.
