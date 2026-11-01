# Dedicated server implementation plan

Reviewed October 13, 2026 against the current source tree and saved compatibility reports; no server/build/test run. Status authority: [master §13.2 authority](../../ROGUE_TRADER_2_PLAN.md#132-server-responsibilities), [M1 fidelity gate and original registry](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [M2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay) and [M3](../../ROGUE_TRADER_2_PLAN.md#milestone-3--dedicated-multiplayer).

## Purpose and current code

**Not started in this folder.** No dedicated RogueTrader server service/project exists here. The separate ignored pinned InfantryOnline working source and compatibility harness are the active candidate engine route, documented by [runtime](../../docs/architecture/ROGUE_RUNTIME_PLAN.md), [native client](../../docs/architecture/NATIVE_CLIENT_PLAN.md) and [package audit](../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md).

## Completed compatibility history

-   RT-F381/F382: bounded package/model/transfer-source audit and generic original-event/runtime route audit completed; 19,198 literal values matched. These are source/data proofs.
-   RT-F383: original arena initializes with 25 computers, 100 mineral drops, 51 teams and 42 hides; [report](../../.tools/compatibility/reports/original-arena-probe.json). The initializer runs no physics/poll/socket-begin calls.
-   RT-F385: derived VehicleMaker field18 parser fix preserves exactly eight canonical bounty values; every other parsed property remains unchanged.
-   RT-F392/F394: full-asset bounded host accepts canonical 89 BLOs plus authentic shared Soccer, initializes one original arena and binds 127.0.0.1:21337/:21338. [Readiness](../../.tools/compatibility/local-working/session-host/reports/ready.json) and [controlled exit](../../.tools/compatibility/local-working/session-host/reports/exit.json): 120.17 seconds, exit 0, max clients 0, original hashes unchanged, no synthetic player inputs.

These do not satisfy native entry, gameplay, authority, persistence or final engine selection. The native client reached Welcome FreeInfantry; Start gave no confirmed entry. Owned processes/private test namespace were cleaned: [cleanup](../../.tools/native-client-inspection/native-test-cleanup.json).

## Active outstanding work

-   [ ] **Investigating — RT-F380/F384:** actual native engine channel, profile isolation and welcome/Start route. No desktop interaction while the user is playing; actual entry remains unverified.
-   [ ] **Investigating — RT-F387/F069:** native receiver delivery/cache casing/full unchanged LVL graphical trailer and runtime dependency eligibility. Offline zlib/hash proof is not receiver proof.
-   [ ] **Investigating — RT-F393 and broader runtime:** offline announcement and upstream cancellation/rearm shutdown remain unresolved. Narrow sale/proximity/null-script and buy safeguards RT-F388/F389/F390/F396 have historical acceptance; see master §18 and issue evidence. Controlled bounded process exit does not prove production shutdown.
-   [ ] **Investigating — original runtime:** original evaluator, motion/combat/energy, resources/events/actors/ownership and progression. Existing generic handlers and initialized objects are not full behavior acceptance.
-   [ ] **Not started — persistence acceptance:** standalone mode currently has no durable progression.
-   [ ] **Not started — master M2/M3 acceptance:** real original mine/collect/sell/upgrade/fight/save loop, then independent clients and authoritative validation through the selected engine. The implemented Protocol codec, AI controller, Persistence core and Trading composition remain separate libraries with no dedicated server entry point here.

The emulator player-update path accepts client motion; do not describe it as validated final server-authoritative physics. Preserve every canonical source byte/name/value. Reconcile only specifically verified features to the master/catalog; master counts and individual audit completions do not establish full reproduction.
