# Bounded original native backend implementation plan

Purpose: host an actual original Rogue Trader arena with full loaded asset dependencies for a bounded local native-client test. Master authority: [§18 fidelity gate, RT-F392/RT-F394](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); parent [compatibility plan](../IMPLEMENTATION_PLAN.md), [runtime](../../../docs/architecture/ROGUE_RUNTIME_PLAN.md), [native client](../../../docs/architecture/NATIVE_CLIENT_PLAN.md). Reconciled October 13, 2026.

## Accomplished

RT-F392 is **Complete for bounded full-asset hosting**; RT-F394 is **Complete for authentic shared Soccer acquisition/member/startup acceptance**. [Program.cs](Program.cs) requires standalone/offline/loadBlobs configuration, creates one named public RT arena and records aggregate counts without credentials/chat. Separate NEW NamedArena routing metadata supplies the local arena name while original CFG remains byte-identical.

[Ready report](../../../.tools/compatibility/local-working/session-host/reports/ready.json): real game 127.0.0.1:21337, ping 127.0.0.1:21338, original map/200 items/30 vehicles/12 hulls, 26 initial vehicles/100 initial items. The prior ArenaProbe count is specifically 25 computer vehicles. Working assets include all 89 canonical BLO archives plus authentic supplemental soccer.blo; registered transfer inventory is a smaller required subset, not all working files.

[Exit report](../../../.tools/compatibility/local-working/session-host/reports/exit.json): controlled exit 0 after 120.17 seconds, one arena, maximum native clients 0, zero synthetic inputs and unchanged original hashes. Normal bounded exit releases owned sockets while bypassing upstream shutdown rearm. Native test reached welcome only; [cleanup](../../../.tools/native-client-inspection/native-test-cleanup.json) records owned client/backend removal and private registry cleanup.

## Next and parity

- [ ] **In progress — RT-F380/RT-F384:** supported engine/profile/isolation and actual native Start/entry; resume desktop only when user is available.
- [ ] **Testing — RT-F393:** compiled derived offline CS_Ready patch-announcement guard needs actual native handshake acceptance.
- [ ] **Not started — RT-F387:** verify actual asset download/cache/receiver and full original map rendering.
- [ ] **Investigating:** production shutdown, economy/proximity/capacity risks; standalone durable persistence remains absent.
- [ ] **Not started:** native original flight/combat/mining/refining/trading/upgrades/rewards parity and multiple-client acceptance.

Build/preparation only: `./scripts/compatibility/run-native-session-host.ps1 -Build`. Explicit backend start: add `-Start -DurationSeconds 120` (10–120-second bound). The wrapper launches a hidden owned process, returns readiness/PID/report paths and launches no native UI. A `stop.request` marker under `.tools/compatibility/local-working/session-host/` requests early stop. These commands are documented for a later relevant feature check; none was run for this documentation reconciliation.
