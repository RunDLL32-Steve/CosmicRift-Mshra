# Bounded original native backend implementation plan

Purpose: host an actual original Rogue Trader arena with full loaded asset dependencies for a bounded local native-client test. Master authority: [§18 fidelity gate, RT-F392/RT-F394/RT-F395](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); parent [compatibility plan](../IMPLEMENTATION_PLAN.md), [runtime](../../../docs/architecture/ROGUE_RUNTIME_PLAN.md), [native client](../../../docs/architecture/NATIVE_CLIENT_PLAN.md). Reconciled October 14, 2026.

## Accomplished

RT-F392 is **Complete for bounded full-asset hosting**; RT-F394 is **Complete for authentic shared Soccer acquisition/member/startup acceptance**. [Program.cs](Program.cs) requires standalone/offline/loadBlobs configuration, creates one named public RT arena and records aggregate counts without credentials/chat. Separate NEW NamedArena routing metadata supplies the local arena name while original CFG remains byte-identical.

[Ready report](../../../.tools/compatibility/local-working/session-host/reports/ready.json): real game 127.0.0.1:21337, ping 127.0.0.1:21338, original map/200 items/30 vehicles/12 hulls, 26 initial vehicles/100 initial items. The prior ArenaProbe count is specifically 25 computer vehicles. Working assets include all 89 canonical BLO archives plus authentic supplemental soccer.blo; registered transfer inventory is a smaller required subset, not all working files.

[Exit report](../../../.tools/compatibility/local-working/session-host/reports/exit.json): controlled exit 0 after 120.17 seconds, one arena, maximum native clients 0, zero synthetic inputs and unchanged original hashes. Normal bounded exit releases owned sockets while bypassing upstream shutdown rearm. Native test reached welcome only; [cleanup](../../../.tools/native-client-inspection/native-test-cleanup.json) records owned client/backend removal and private registry cleanup.

### RT-F395 routing correction and healthy-host acceptance

**Complete for bounded diagnostic routing.** Investigation of the retained historical stdout found 600 swallowed arena-poll divide-by-zero exceptions: the former generated XML omitted both radar divisors. Its readiness/exit report established sockets and byte preservation, but did not establish healthy arena polling.

[write-session-config.ps1](write-session-config.ps1) now supplies one fresh configuration to both the host wrapper and fixed [RuntimeChecks](../RuntimeChecks/IMPLEMENTATION_PLAN.md). Its radar ranges 2000/4000 and cadence divisors 2/4 are explicitly NEW/provisional diagnostic settings, with existing near/weapon ranges 1000 retained; canonical Rogue CFG/data remain unchanged. The host rejects missing/nonpositive routing fields before readiness, counts logged runtime exceptions and rejects success when any occur. Start forces compilation of current derived source, including the required ScriptArena hash provenance; reports record loaded host/server/network/assets assembly hashes.

Six focused actual-helper routing checks pass in the [fixed report](../../../artifacts/rogue-runtime-checks/fixed.json), against five expected routing failures in the retained [baseline](../../../artifacts/rogue-runtime-checks/baseline.json). Separate [healthy readiness](../../../artifacts/rogue-runtime-checks/host/ready.json) and [exit](../../../artifacts/rogue-runtime-checks/host/exit.json) evidence records one actual arena, both loopback sockets, exit 0 after approximately 10.25 seconds, loaded-assembly hashes, zero clients, zero logged exceptions and unchanged staged original hashes. An [invalid-config run](../../../artifacts/rogue-runtime-checks/invalid-host/exit.json) exits 2 before readiness. This evidence is separate from the original 120.17-second reports above; no native UI or synthetic player input was used.

## Next and parity

- [ ] **In progress — RT-F380/RT-F384:** supported engine/profile/isolation and actual native Start/entry; resume desktop only when user is available.
- [ ] **Testing — RT-F393:** compiled derived offline CS_Ready patch-announcement guard needs actual native handshake acceptance.
- [ ] **Not started — RT-F387:** verify actual asset download/cache/receiver and full original map rendering.
- [ ] **Native store acceptance after RT-F389:** verify the connected native UI against the implemented packet-shop StoreEnabled/spectator guard; see [RuntimeChecks](../RuntimeChecks/IMPLEMENTATION_PLAN.md).
- [ ] **Native purchase acceptance after RT-F396:** verify connected original purchase behavior and durable transactions. Bounded purchase arithmetic/delivery/credit ordering is covered by the completed RuntimeChecks evidence.
- [ ] **Investigating:** production shutdown and durable transaction/persistence behavior; standalone durable persistence remains absent.
- [ ] **Not started:** native original flight/combat/mining/refining/trading/upgrades/rewards parity and multiple-client acceptance.

Build/preparation only: `./scripts/compatibility/run-native-session-host.ps1 -Build`. Explicit backend start: add `-Start -DurationSeconds 120` (10–120-second bound); `-Start` also forces a fresh compile. The wrapper launches a hidden owned process, returns readiness/PID/report paths and launches no native UI. A `stop.request` marker under `.tools/compatibility/local-working/session-host/` requests early stop. Root owns shared-output builds and focused executable acceptance. Native entry, asset reception, original gameplay and saving remain separate acceptance tasks.
