# Development progress — October 17, 2026

Authority: [master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap). Current handoff: [CONTINUATION.md](../../CONTINUATION.md). **397 feature entries, 22 scoped completions, 375 open.** No original feature or milestone acceptance flag changed in this documentation audit.

## Current source versus accepted gameplay

| Area | Existing source / recorded scope | Remaining acceptance |
|---|---|---|
| M0/M1 engineering prototype | Historical October 12 flight/combat source/export/input/world acceptance; 41 tests at that revision | Original movement, controls, combat, visuals and values |
| Original content tooling | [G9 ledger](../../scripts/original-assets/plan.d/g9.md) indexes dated evidence; [conditional reset lifecycle source](2026-10-19-g9-reset-lifecycle-source.md) joins guarded callers and selected direct bodies; [PR85](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/85) records live workflow status; PR76 catalog acceptance remains dated ledger history | G9/RT-F069/F377/F378/F379/F386 open; native entry/delivery, original consumers/evaluators, full closure and engine decision |
| Raw original data | RPG/VEH/ITM/CFG readers, LIO field/reference views, field audits and original-ID progression evidence | Correct native units/effects/lifecycle and full original acceptance |
| Persistence (queue M1) | Repositories, idempotent/CAS transactions and optional original-ID membership validator | Client save flow, historical retention rules and exit/relaunch progression |
| Cargo (queue M2) | Generic capacity and swept-path collectible pickup, merged `7684b05` | Original masses/thresholds/pickup/spawn behavior; client integration |
| Mining (queue M3) | Optional generic finite deposits/equipment, deterministic extraction/pickup and replay v2; merged `1037592dc48991d61ccc8900374b1f371da58368` in [PR 60](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/60) ([evidence](2026-10-18-m3-mining.md)) | Original RT-F042 generation/traps/equipment/beam semantics, M8 client and transport integration |
| Trading (queue M4) | Generic docking/price/cargo/transaction service, merged `3e29142` | Original station rules, repairs/upgrades and client UI |
| AI (queue M6) | NEW/provisional PirateController, merged `759a66a` | NPC integration, damage/rewards and original pirates |
| Replay (queue C3) | Same-runtime recording/state-digest harness, merged `952d29d` | Cross-machine determinism and original physics |
| Protocol (queue C5) | NEW bounded binary codec, merged `403499f` | Transport/authentication/server/client integration and multiplayer |
| Dedicated production server | Plan-only module | Master M3 independent-client/persistent-service acceptance |

Queue IDs name scoped work items; they are not master milestone numbers. Earlier descriptions saying cargo/trading awaited review, RT-F397 awaited merge, or Protocol/AI/Persistence were empty reserved folders are superseded by the current source inventory. The client still runs the provisional range, not a complete Rogue Trader loop.

## Subplan index

| Area | Plan |
|---|---|
| Client, input/rendering/inspector | [Client](../../client/IMPLEMENTATION_PLAN.md), [inspector](../../client/ORIGINAL_INSPECTOR_PLAN.md) |
| Simulation, cargo and replay | [Simulation](../../server/RogueTrader.Simulation/IMPLEMENTATION_PLAN.md) |
| Original records and provisional definitions | [OriginalData](../../shared/RogueTrader.OriginalData/IMPLEMENTATION_PLAN.md), [GameData](../../shared/RogueTrader.GameData/IMPLEMENTATION_PLAN.md), [data](../../data/IMPLEMENTATION_PLAN.md) |
| Persistence, trading and AI | [Persistence](../../server/RogueTrader.Persistence/IMPLEMENTATION_PLAN.md), [Trading](../../server/RogueTrader.Trading/IMPLEMENTATION_PLAN.md), [AI](../../server/RogueTrader.AI/IMPLEMENTATION_PLAN.md) |
| Protocol and production server | [Protocol](../../shared/RogueTrader.Protocol/IMPLEMENTATION_PLAN.md), [Server](../../server/RogueTrader.Server/IMPLEMENTATION_PLAN.md) |
| Tests | [Unit](../../tests/Unit/IMPLEMENTATION_PLAN.md), [Integration](../../tests/Integration/IMPLEMENTATION_PLAN.md), [Network](../../tests/Network/IMPLEMENTATION_PLAN.md), [Load](../../tests/LoadTests/IMPLEMENTATION_PLAN.md) |
| Original import/dependencies | [Importer](../../scripts/original-assets/IMPLEMENTATION_PLAN.md), [dependencies](../../scripts/original-assets/DEPENDENCY_PLAN.md) |
| Compatibility and focused runtime checks | [Compatibility](../../scripts/compatibility/IMPLEMENTATION_PLAN.md), [RuntimeChecks](../../scripts/compatibility/RuntimeChecks/IMPLEMENTATION_PLAN.md), [NativeDeliveryChecks](../../scripts/compatibility/NativeDeliveryChecks/IMPLEMENTATION_PLAN.md) |
| Architecture and research | [Architecture](../architecture/IMPLEMENTATION_PLAN.md), [research](../original-research/IMPLEMENTATION_PLAN.md) |
| Build/export and CI | [Scripts](../../scripts/IMPLEMENTATION_PLAN.md), [workflow](../architecture/CI_PLAN.md) |
| Design/balance/documentation | [Design](../game-design/IMPLEMENTATION_PLAN.md), [balance](../balance/IMPLEMENTATION_PLAN.md), [docs](../IMPLEMENTATION_PLAN.md) |

<a id="final-validation-evidence"></a>

## Verification evidence

Historical prototype and original-viewer runs remain in [October 12](2026-10-12.md), [October 13](2026-10-13.md) and the [changelog](../../CHANGELOG.md). Historical handler/host acceptance is in [October 14](2026-10-14.md) and [RT-F397](2026-10-14-rt-f397.md). Generic cargo/trading results are in their October 16 logs; later raw-data audit evidence is in the corresponding `plan.d` and cloud logs. Those totals are revision-specific, not today's suite size.

The recorded initializer's 25 computers and the separate host-ready report's 26 vehicles are distinct. Native Welcome did not prove Start/arena entry. Reported historical cleanup is not a current process inventory. Ignored artifact/report paths may be absent in a clean checkout.

Source inspection found current RuntimeChecks has 15 Buy.* checks. The parent fixed report records Accepted=true with 61 checks and zero failures; the earlier 35- and 50-check reports describe narrower historical revisions. The two PowerShell wrappers now cache process handles and check exit codes; a blank/non-zero exit is a failure.

Hosted CI was observed read-only for baseline `d14df02`: the [Windows check](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/actions/runs/39051255053/job/117450136234) concluded success. That does not establish the result of this PR's later head, asset-dependent oracle execution, or native acceptance.

This documentation audit ran tracker/local-reference checks and source/evidence inspection; it did not launch builds, gameplay simulations or desktop/native clients. See [audit and review record](../audits/2026-10-17-documentation-audit.md) for exact results and remaining evidence limits.

## Next work

Use [TASK_QUEUE.md](../../TASK_QUEUE.md) and its separate briefs, checking live claims before starting. Preserve all unchecked master/catalog scope; do not repeat merged generic slices. Select focused checks only after the relevant implementation is complete. New verification must identify its revision, acceptance clause, result, skips and independent review.
