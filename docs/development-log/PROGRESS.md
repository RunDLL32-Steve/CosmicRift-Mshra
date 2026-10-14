# Development progress — master reconciliation

Updated: October 12, 2026 (America/Chicago). Authority: [ROGUE_TRADER_2_PLAN.md §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap). Startup: [PROJECT_START.md](../../PROJECT_START.md).

Current scope: Milestone 0 foundation and Milestone 1 First Contact. The user's exact Cosmic Rift assets/presentation target for Rogue Trader is recorded in master §1.2. Prototype acceptance and historical/visual parity are tracked separately.

## Milestone status

| Milestone | Status | Completion evidence / remaining gate |
|---|---|---|
| 0 — repository and research | COMPLETE (foundation) | Local Git, four-project solution, pinned portable tools, build workflow, evidence matrix, archive research and conventions |
| 1 — flight and combat | COMPLETE (engineering prototype) | 41 unit cases pass; three native-input/UI and six world phases pass in source and standalone builds; rendered source/controls/export QA inspected; standalone launch without SDK/runtime discovery passes |
| 2 — core gameplay | Not started | Mining, cargo, trade, upgrades, persistence and NPC loop |
| 3 — dedicated multiplayer | Not started | Dedicated server, authenticated protocol, database and network acceptance |
| 4–6 | Not started | Complete earlier gates first |
| Exact content/presentation fidelity | Research underway; not complete | Canonical original source acquired unchanged; 46 foundational hashes verified. InfantryOnline engine spike, format interpretation, asset/map decoding and exact gameplay/visual parity remain pending |

## Subplan index

Every path below is an implementation checklist with a reciprocal reference to the master. Checkboxes describe actual completed work; deferred work remains unchecked.

| Folder / subplan | Master scope |
|---|---|
| [Client](../../client/IMPLEMENTATION_PLAN.md) | M1 / §19 slices A–E, input, HUD, camera and standalone client |
| [Simulation](../../server/RogueTrader.Simulation/IMPLEMENTATION_PLAN.md) | M0/M1, §§5/7, flight and combat |
| [Game data](../../shared/RogueTrader.GameData/IMPLEMENTATION_PLAN.md) | M0/M1, §6.1 external validated definitions |
| [Data catalog](../../data/IMPLEMENTATION_PLAN.md) | M0/M1, provisional ships/weapons/sector |
| [Unit tests](../../tests/Unit/IMPLEMENTATION_PLAN.md) | M1 / §20.1 physics and combat |
| [Original content index](../../scripts/ORIGINAL_CONTENT_INDEX_PLAN.md) | §1.2 fidelity gate / original raw records and candidate dependencies |
| [Build and export](../../scripts/IMPLEMENTATION_PLAN.md) | M0/M1, reproducible tools and Windows artifact |
| [CI workflow](../../.github/workflows/IMPLEMENTATION_PLAN.md) | M0 automated build; hosted run unobserved |
| [Historical research](../original-research/IMPLEMENTATION_PLAN.md) | M0 / §§2/22 plus user-supplied archives/video |
| [Balance ledger](../balance/IMPLEMENTATION_PLAN.md) | M0 / §§5–8 provisional values and recovered raw values |
| [First Contact design](../game-design/IMPLEMENTATION_PLAN.md) | M1 slices A–E and fidelity acceptance |
| [Development log](IMPLEMENTATION_PLAN.md) | M0/M1 / §21 maintenance and reconciliation |
| [Architecture](../architecture/IMPLEMENTATION_PLAN.md) | M0/M1 / §§5.3/13/17/21 |
| [Server](../../server/RogueTrader.Server/IMPLEMENTATION_PLAN.md) | Deferred M3 |
| [Persistence](../../server/RogueTrader.Persistence/IMPLEMENTATION_PLAN.md) | Deferred M2/M3 |
| [AI](../../server/RogueTrader.AI/IMPLEMENTATION_PLAN.md) | Deferred M2 |
| [Protocol](../../shared/RogueTrader.Protocol/IMPLEMENTATION_PLAN.md) | Deferred M3 |
| [Integration](../../tests/Integration/IMPLEMENTATION_PLAN.md) | Deferred M2/M3 |
| [Network](../../tests/Network/IMPLEMENTATION_PLAN.md) | Deferred M3 |
| [Load tests](../../tests/LoadTests/IMPLEMENTATION_PLAN.md) | Deferred M5 |

## Reconciliation rules

1. Implement a scoped feature and update its folder checklist.
2. Run the relevant tests; record the exact command, result and material limitation.
3. Verify the runnable artifact when required. A headless rules test does not establish visual parity or keyboard behavior.
4. Update the corresponding §18 master checkbox and this table in the same phase.
5. Record scope changes in the dated log and CHANGELOG. Recovered evidence must keep its source, classification and revision context.

## Next fidelity work

- [x] Compare archive files at fixed commits; select `fowlmouth/cosmic-rift-assets` as canonical unchanged foundation. Six text records and four blob/map payloads corroborate the snapshot.
- [ ] Evaluate InfantryOnline native-client/server compatibility before the next production engine decision.
- [ ] Recover complete ship/item references and validate legacy field units and sentinel values against the actual engine behavior.
- [ ] Decode and inspect original sprite directions, palettes, animation and sound records; record applicable asset reuse status.
- [ ] Recover RT map/object/collision layers and port the documented rendering/content mapping.
- [ ] Review the supplied video systematically with timestamped observations. Watch is installed separately; only one frame at 0:25 has been inspected so far.
- [ ] Validate original flight/weapon/HUD parity before expanding presentation; M2 then adds the actual RT mining/trading/progression loop using recovered mode evidence.

M1 currently has no working mining, station, economy, progression save, AI or networking. The placeholder build must not be presented as the finished recreation.

## Final validation evidence

- `scripts/verify.ps1`: full Debug solution build succeeds, 41 unit cases pass, source client native-input and world smoke both pass (exit 0). A cached NuGet vulnerability-feed retrieval warning is present; package restore/compile/tests succeed.
- `scripts/export-windows.ps1`: ExportRelease build has zero warnings/errors; export succeeds; standalone input/world smoke passes (exit 0); self-contained zip produced with engine/runtime notices.
- Exported executable launched with DOTNET_ROOT pointing to a missing location and PATH reduced to Windows system directories: original placeholder gameplay renders and capture succeeds (exit 0). No editor or installed .NET SDK/runtime discovery is required.
- `artifacts/qa/first-contact.png`, `controls.png` and `standalone-first-contact.png`: actual rendered frames inspected; geometry/telemetry and all controls-menu actions visible at 1280×720. No original-art parity is claimed.
- `scripts/verify-reference-assets.ps1`: 46 original records match exact SHA-256 and byte length at the pinned canonical commit. All original BLO payloads and the RT map are reference-only; decoded/derived outputs must remain outside the reference checkout.

Remaining milestone checkboxes in the master are unchanged except the user-directed original-content clarification in M2. No M2 gameplay or M3 multiplayer is complete.

Engine evaluation complete: [ENGINE_EVALUATION.md](../architecture/ENGINE_EVALUATION.md) distinguishes the server emulator and separate client, active .NET 10 requirements, archived compatibility issues and a measured engine decision. The actual legacy compatibility spike is pending.


Original-content index verified: 3,086 records, 12 RPG/VEH hull name/ID cross-matches, 200 item records, seven original resources and 551 candidate references. All source hashes unchanged. Raw indexing is complete; 545 BLO archive-member candidates and evaluator/units remain unvalidated. Master §§6/8 and M2 now use the original catalog instead of proposed replacement ships/resources.


## October 13 final checkpoint

User requested stop researching and wrap documentation. Resume: [CONTINUATION.md](../../CONTINUATION.md). Master§1.3/§18 now records372 individual feature checks;5 narrow completions,367 open. Six canonical-domain plans cover ships/weapons/equipment/resources/sectors/actors. Two documentation agents reconciled remaining folders; see DOCS_RECONCILIATION_HANDOFF.md.

Complete:14 importer/staging tests;11 archives/231objects/36,516frames;46 originals unchanged;source inspector12hulls/map/231resources rendered across3 pages with tick0;Debug zero warnings/errors. Actual pinned parser and bounded loopback/ping host pass;original help dependencies recovered with provenance;host/endpoints/ownedSDK10 helpers stopped. Runner crash repaired;upstream shutdown race remains open.

Pending:updated standalone export,wider inspector input,native client/SilentClientCrash,full dependencies,LVL playable integration,native appearance/evaluators/original flight/combat andM2 loop. Historical41-unit/native-input/world smoke was not rerun for assets/docs. Completed active tasks are history/changelog/master audit, not outstanding queues.
