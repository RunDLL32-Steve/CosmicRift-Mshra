# Documentation reconciliation handoff — October 13, 2026

Authority: [master §18](ROGUE_TRADER_2_PLAN.md#18-development-roadmap), [startup](PROJECT_START.md), and [development conventions](AGENTS.md). Canonical feature tracking: [FEATURE_CATALOG.md](docs/original-research/FEATURE_CATALOG.md) and [FEATURE_MASTER_CHECKLIST.md](docs/original-research/FEATURE_MASTER_CHECKLIST.md). Dates follow the user's America/Chicago project checkpoint.

This chat used exactly two subagents with separate documentation ownership. At the user's usage-window checkpoint, both were asked to finish only their current edits and stop. No code changes, builds, launches, gameplay simulations, commits, pushes or messages to other chats were performed. Existing project files were all untracked in Git at inspection; no committed baseline was available. Existing implementation content and evidence were preserved rather than treating those files as disposable.

The implementation chat owns root instruction/status documents and the actively edited content research, client, original-asset and compatibility work. Its wrap-up message confirms it is reconciling `ROGUE_TRADER_2_PLAN.md`, `AGENTS.md`, `README.md`, `CHANGELOG.md`, `PROGRESS.md` and the current dated log. This handoff supplies exact proposals for that reconciliation; it does not overwrite those concurrent edits.

## Documentation ownership and changed files

Runtime/test documentation subagent: existing plans in [Simulation](server/RogueTrader.Simulation/IMPLEMENTATION_PLAN.md), [GameData](shared/RogueTrader.GameData/IMPLEMENTATION_PLAN.md), [Protocol](shared/RogueTrader.Protocol/IMPLEMENTATION_PLAN.md), [Server](server/RogueTrader.Server/IMPLEMENTATION_PLAN.md), [Persistence](server/RogueTrader.Persistence/IMPLEMENTATION_PLAN.md), [AI](server/RogueTrader.AI/IMPLEMENTATION_PLAN.md), [Unit](tests/Unit/IMPLEMENTATION_PLAN.md), [Integration](tests/Integration/IMPLEMENTATION_PLAN.md), [Network](tests/Network/IMPLEMENTATION_PLAN.md), and [LoadTests](tests/LoadTests/IMPLEMENTATION_PLAN.md).

Tooling/design documentation subagent: [scripts plan](scripts/IMPLEMENTATION_PLAN.md), [original index plan](scripts/ORIGINAL_CONTENT_INDEX_PLAN.md), [CI plan](.github/workflows/IMPLEMENTATION_PLAN.md), [balance plan](docs/balance/IMPLEMENTATION_PLAN.md), [design plan](docs/game-design/IMPLEMENTATION_PLAN.md), [architecture plan](docs/architecture/IMPLEMENTATION_PLAN.md), [First Contact design](docs/game-design/FIRST_CONTACT.md), [provisional values ledger](docs/balance/PROVISIONAL_VALUES.md). The [toolchain documentation](docs/architecture/TOOLCHAIN.md) was inspected but left unchanged at wrap-up.

Root: this handoff and review of those documentation changes. Completed prototype tasks move into concise completion history with evidence; current outstanding checklists use unchecked entries and explicit Not started, Investigating, In progress, Testing or Complete status. No new original feature was discovered by this documentation-only reconciliation; existing RT-F IDs are reused.

## Evidence reviewed without rerunning acceptance

| Evidence | Supported conclusion | Limit |
|---|---|---|
| [October 12 acceptance log](docs/development-log/2026-10-12.md), [progress record](docs/development-log/PROGRESS.md), [master M0/M1 history](ROGUE_TRADER_2_PLAN.md#m0m1-completion-evidence--engineering-prototype) | Complete prototype engineering acceptance: 41 prior unit cases, three native-input/UI phases and six world phases in source/export; standalone rendered launch without SDK/runtime discovery. | Historical runs; no new runs here. NEW Interceptor/plasma/First Contact fixtures do not verify original hulls, weapons or map behavior. |
| [raw index output](artifacts/research/original-content/index.json), [index plan](scripts/ORIGINAL_CONTENT_INDEX_PLAN.md) | Complete raw indexing: 3,086 records, 12 RPG/VEH hull matches, 200 ITM entries, seven type-4 resources; source hashes retained unchanged. | The original index's 545 member candidates are its baseline unresolved count, not a newly recomputed residual after sprite decoding. Complete dependency closure and operational interpretation remain open. |
| [decoder/staging evidence](scripts/original-assets/IMPLEMENTATION_PLAN.md), [derived index](artifacts/original-assets/index.json), [staged metadata](client/OriginalContent/runtime.json) | Complete supported sprite export/staging slice: 11 archives, 231 objects, 36,516 frames; 12 Small/Tiny hull pairs with 64 rows and 11 columns; documented 10 decoder + four staging tests. Exact indexed pixels/palette and staged map RGB retained. | Native palette/HSV/shadow blits, banking/pose selection, timing, remaining audio/members and complete visual parity remain unfinished. Integrated inspector acceptance belongs to the implementation chat. |
| [parser report](.tools/compatibility/reports/pristine-load-report.json), [successful loader log](.tools/compatibility/logs/loader-fix-run.stdout.log), [loader build](.tools/compatibility/logs/loader-fix-build.log) | Complete narrow original-file loading: CFG, 200 ITM v57, 30 VEH v53, LVL 1999×1998 / 3,994,002 tiles, 81 LIO, 12 RPG v6; FailureCount 0 and AssetsUnchanged true. Loader rebuild reports zero warnings/errors. | Accepted parsing does not establish field units, sentinels, evaluator semantics, full record consumption fidelity, native client behavior or a running zone. |
| [old failed run](.tools/compatibility/logs/pristine-loader-run.log), [runner regression](.tools/compatibility/reports/runner-error-regression.json), [exception regression](.tools/compatibility/reports/malformed-exception-regression.json) | Complete documented runner repair: unique diagnostic shards, outer catch and field export replace the escaping IOException / by-ref serialization failure. Invalid args exit 2; malformed cases exit 1; the exception regression records stderr bytes 0. | Old failed logs remain incident history. They must not be cited as the current pristine-parser result. |
| [level metadata](.tools/compatibility/reports/rogue-level-metadata.json), [field interpretation](docs/original-research/FIELD_INTERPRETATION.md) | Investigating RT-F021/027/029: native tile stride 16, offsets 3/3 and 116,065 nonzero physics tiles are retained. | Trailing graphical entities are not decoded; raw nonzero physics is not verified per-vehicle collision/traversal. Prototype world-unit scaling is separate. |
| [ZoneServer build](.tools/compatibility/logs/zone-server-build.log), [probe stdout](.tools/compatibility/logs/server-probe.stdout.log), [probe stderr](.tools/compatibility/logs/server-probe.stderr.log) | Compiled derived upstream ZoneServer: zero errors, 1,320 warnings in recorded build. Runtime compatibility remains Investigating. | Probe snapshot rejected initialization for missing keys.txt, infcmdlst.txt, comms.txt, rules.txt and mods.txt. Check the implementation chat's final reports before claiming any later startup result. |

Canonical originals remain pinned to `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`; the parser/server reference is `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849`. [Compatibility manifest](.tools/compatibility/compatibility-manifest.json) records separate unchanged canonical copies and derived patches. Documentation inspection does not establish redistribution permission or require modifying original bytes, names, categories or values.

## Proposed exact master reconciliation

Preserve all existing M0/M1 completed checkmarks as the engineering audit record. Keep M2 gameplay, M3 multiplayer and full exact reproduction unchecked. Add a documentation completion-history entry linking this handoff and the updated subplans; do not create a new gameplay completion checkbox for documentation work.

Split master §1.3's broad decode/map/parser bullets into reviewable supporting completions and unfinished original acceptance. Suggested history entries are:

- [x] Complete the selected BLO/CFS parser/export/staging slice: 11 archives / 231 objects / 36,516 frames, original indexed pixels/palette, 12 hull identities, native-size map RGB and documented 14 focused tests. Scope supports RT-F070–072; remaining archive closure and native rendering remain separate.
- [x] Compile the pinned native Assets loader and accept unchanged CFG/ITM/VEH/LVL/LIO/RPG files, with exact record/dimension counts and source hashes in the pristine report. This completes data loading only.
- [x] Repair and verify controlled compatibility-runner errors after the diagnostic-path/serialization incident; cite the corrected logs and regression reports.

Retain these active unchecked entries with explicit current status:

- [ ] Investigating — RT-F069/070: complete record/member/child/audio dependency closure. Distinguish the original 545 candidates from newly validated selected members; do not claim that all 545 are still unresolved or now resolved without a residual audit.
- [ ] Investigating — RT-F074/075 and RT-F023/024: exact native palette/HSV/shadow/light blending, animation, bank/pose choice and effect/audio layering.
- [ ] Investigating — RT-F021 and RT-F287–367: native LVL graphical entities, LIO actions/conditions and final terrain/collision/world-placement semantics.
- [ ] Investigating — RT-F027–041/067: original units, negative sentinels, weighted inheritance, shield/energy behavior and skill/event evaluators. All original numeric fields stay raw until validated.
- [ ] In progress or Testing, according to the implementation owner's final evidence — RT-F073 original-content inspector and RT-F020/022 map presentation. Do not check these from derived staging alone.
- [ ] Investigating — native server/client compatibility and production engine decision. A parser or ZoneServer compile cannot finish this gate.
- [ ] Not started — completed original mining/refining/trading/upgrades/persistence/NPC loop and full M2 acceptance. Original resources, actors, records and restrictions take precedence over invented mining or replacement catalogs.

The owner should preserve all individual catalog IDs unchecked until each published acceptance profile passes. A narrow supporting completion above cannot finish the twelve HULL features, seven RESOURCE lifecycles, all original ITEM/ACTOR/OBJECT behavior or complete native appearance.

## Proposed changelog additions for the implementation owner

Existing indexing and source-based engine-evaluation entries already exist; avoid repeating them. Major newly completed entries, once reconciled with the owner's final reports:

- Add original BLO/CFS decoding and derived runtime staging for 11 archives, 231 objects and 36,516 frames, with preserved pixels/palette/source identities and focused malformed-data/pixel-integrity checks; keep native palette/shadow/banking parity open.
- Add exact RGB staging of the canonical 1999×1998 Rogue Trader map overview with independent source and pixel hashes; collision/topology remain separate.
- Add pinned InfantryOnline original-file loader compatibility for CFG, 200 items, 30 vehicles, 81 level-object records, 12 hull skills and the full tile-grid dimensions, preserving raw values and unchanged originals.
- Fix compatibility-runner diagnostic filename collisions and unsupported by-ref JSON export; malformed cases now return controlled errors and pristine loading completes.

## Resume checkpoint

Read PROJECT_START.md, AGENTS.md and the complete current master first, then this handoff and the latest dated implementation log. Reconcile the stale [legacy compatibility plan](docs/architecture/LEGACY_COMPATIBILITY_PLAN.md): its incident section still says the repaired loader validation is pending, although corrected logs/reports now pass. Preserve the failed-run history but replace current pending status with the narrowly verified result. Treat [engine evaluation](docs/architecture/ENGINE_EVALUATION.md)'s no-execution wording as the historical source-review scope, not the status of all later compatibility work.

The immediate unfinished work is the implementation owner's current inspector validation, server support-file/dependency investigation and native-client route. Resume from its final checkpoint; do not rerun baseline flight/combat simulations for documentation, asset indexing or parsing. No extra ship classes, sectors, invented resources, modern effects or production networking should be started at this checkpoint.

## Final owner checkpoint received during wrap-up

The implementation chat reports a new Debug client compile with zero warnings/errors and three rendered original-inspector captures (ships/map/sprites), each verifying 12 hulls, 231 atlases, a 1999×1998 map and zero world ticks. This is owner-reported source-viewer acceptance, not a run performed in this documentation chat. Reconcile RT-F073's source inspector status with that owner's final paths/results; updated standalone original-content export remains pending. Preserve full hull/native-appearance/gameplay gates unchecked. The owner is updating the root master, AGENTS, README, CHANGELOG, progress and dated checkpoint now; this chat made no edits to those files.

An initial read-only documentation link/anchor check covered 20 documents and 157 local links with no broken targets. It ran while the subagents were saving their last changes; no builds, acceptance suites, probes or simulations were run for this reconciliation. Any files the agents did not finish are explicitly identified in their final wrap-up below.

Final saved runtime/test scope: the runtime/test subagent confirms all ten plans listed above are saved. No owned plan was left unfinished. M0/M1 engineering completions retain prior evidence; original features and deferred modules remain unchecked with explicit statuses and existing catalog IDs. No code or other-path edits were made by that subagent.


Final saved tooling/design scope: the tooling/design subagent confirms eight documents are saved: scripts/IMPLEMENTATION_PLAN.md, scripts/ORIGINAL_CONTENT_INDEX_PLAN.md, .github/workflows/IMPLEMENTATION_PLAN.md, docs/balance/IMPLEMENTATION_PLAN.md, docs/balance/PROVISIONAL_VALUES.md, docs/game-design/IMPLEMENTATION_PLAN.md, docs/game-design/FIRST_CONTACT.md and docs/architecture/IMPLEMENTATION_PLAN.md. TOOLCHAIN.md was read but not edited. Together, the two subagents saved 18 existing documentation files; root saved this handoff. The final broader review was curtailed by the immediate wrap instruction. No new original catalog IDs were required.

Final documentation verification after both subagents saved: 19 changed documents (18 existing files plus this handoff), 286 local file/section links checked, zero broken targets. This was a read-only Markdown path/anchor check. Both subagents have finished; no new work is assigned. Root master/instruction/status updates remain with the implementation chat as explicitly confirmed during wrap-up.
