# Core-data server probe implementation plan

Purpose: verify the pinned server's offline initialization and actual loopback game/ping sockets using original core/help files. Master authority: [§18 fidelity gate](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); parent [compatibility plan](../IMPLEMENTATION_PLAN.md). Reconciled October 13, 2026.

## Accomplished

The core-data host check is **complete for its bounded acceptance**. RT-F073 tracks the original-content frame inspector, not this socket probe. [Program.cs](Program.cs) catches main/worker failures, requires offline configuration, initializes with `loadBlobs=false`, observes game 127.0.0.1:21337 and ping 127.0.0.1:21338 and verifies the eight-byte count/token ping. Zero arenas/clients were created. [Report](../../../.tools/compatibility/reports/local-server-probe.json), [result](../../../.tools/compatibility/reports/local-server-probe-result.json) and [cleanup](../../../.tools/compatibility/reports/server-probe-cleanup.json) record exit 0 and released owned sockets. Five authentic help dependencies were recovered separately from pinned Zone-Assets; no original CFG was edited.

Earlier missing-help controlled failure and upstream shutdown cancellation/rearm failure are recorded in [startup incidents](../../../.tools/compatibility/reports/server-startup-incidents.json). Bounded normal process exit avoids the upstream `Server.end` path; this does not fix production shutdown. Full original arena/assets now have separate [ArenaProbe](../ArenaProbe/IMPLEMENTATION_PLAN.md) and [SessionHost](../SessionHost/IMPLEMENTATION_PLAN.md) evidence.

## Next and parity

-   [ ] **Investigating:** production shutdown receive-rearm race, required before a durable host.
-   [ ] **Not started:** production remote bind/security/persistence configuration after native runtime acceptance.

No routine rerun is needed during documentation work. After a relevant completed host fix, root may use `./scripts/compatibility/run-local-server-probe.ps1 -Build`, which builds and executes this hidden, bounded probe. Graphics/native entry/gameplay are outside this program's acceptance scope.
