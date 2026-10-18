# Legacy engine compatibility implementation plan

Purpose: evaluate the unchanged Rogue Trader content in the pinned InfantryOnline emulator plus separate native client. Master authority: [§1.2 canonical fidelity](../../ROGUE_TRADER_2_PLAN.md#12-user-fidelity-clarification-and-implementation-tracking--october-12-2026), [§1.3 checkpoint](../../ROGUE_TRADER_2_PLAN.md#13-continued-exact-reproduction--october132026), [§18 M1 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). Reconciled October 13, 2026; [scripts/compatibility](../../scripts/compatibility/IMPLEMENTATION_PLAN.md) owns executable tools. Production engine choice remains open.

## Accomplished and bounded evidence

| Accepted feature | Completion and evidence |
|---|---|
| Reproducible isolated source/tools | Server pinned bb97579efeceff1c63d781d8c1bb8dd4a5b9b849, separate derived working source; official SDK 10.0.401 SHA512 verified, independent of root SDK 8/Godot. [Compatibility manifest](../../.tools/compatibility/compatibility-manifest.json) retains source/working hashes and patches. |
| Actual parser/runner | Original CFG/200 ITM/30 VEH/12 RPG/81 LIO/LVL 1999×1998 accepted, controlled exit 0 and unchanged files. Raw 3,994,002 tiles/116,065 masks exported. [Pristine load report](../../.tools/compatibility/reports/pristine-load-report.json); [Loader plan](../../scripts/compatibility/Loader/IMPLEMENTATION_PLAN.md). |
| Runner incident repair | Unique diagnostic shards/field-only JSON/outer catch avoid by-ref serialization and retained-stream collision. Argument and malformed records have controlled exits 2/1. [Runner regression](../../.tools/compatibility/reports/runner-error-regression.json), [exception regression](../../.tools/compatibility/reports/malformed-exception-regression.json). Initial 0xE0434352 matches generic reported popup code, but screenshot process was not independently identified. |
| Supplemental original help | Five unchanged keys/infcmdlst/comms/rules/mods files recovered separately from pinned Zone-Assets Rogue archive with provenance; completes acquisition, not native menu behavior. |
| Core-data loopback probe | Standalone, loadBlobs=false, actual game/ping 127.0.0.1:21337/:21338 and eight-byte native ping; exit 0, zero arenas/clients, sockets released. [ServerProbe plan](../../scripts/compatibility/ServerProbe/IMPLEMENTATION_PLAN.md). |
| RT-F375/RT-F376 inventories | All 89 canonical BLO/1,262 members/609 candidates and native package variants statically inventoried. Full dependency/native compatibility remain open. [Dependencies](../../scripts/original-assets/DEPENDENCY_PLAN.md), [native](NATIVE_CLIENT_PLAN.md). |
| RT-F381/RT-F382 audits | 19,198 direct item values, 12 hull links, 222 map member links, 46-file offline roundtrip; bounded CFG event/generic economy/login/persistence route audited. [Package](ROGUE_PACKAGE_AUDIT_PLAN.md), [runtime](ROGUE_RUNTIME_PLAN.md). |
| RT-F383 actual original arena | Actual generic original initializer verifies 25 computers/100 material drops/51 teams/42 hides, zero begin/poll/physics/client input; exit 0, original hashes unchanged. [ArenaProbe plan](../../scripts/compatibility/ArenaProbe/IMPLEMENTATION_PLAN.md). |
| RT-F385 derived parser repair | Preserves eight VehicleMaker original field18 bounty values; all other parsed properties unchanged. [Focused report](../../artifacts/rogue-package-audit/vehiclemaker-fix.json). |
| RT-F392/RT-F394 full assets/backend | All 89 canonical BLOs plus authentic shared Soccer, one actual RT arena, both loopbacks, exit 0 after 120.17 seconds, maximum clients 0. [SessionHost plan](../../scripts/compatibility/SessionHost/IMPLEMENTATION_PLAN.md), [ready](../../.tools/compatibility/local-working/session-host/reports/ready.json), [exit](../../.tools/compatibility/local-working/session-host/reports/exit.json). |

Completed tasks remain in history, not the active queue. Original source names/bytes/values are unchanged. Historical upstream nullable warning counts accompany successful zero-error compatibility builds; they are not the Godot zero-warning build claim.

## Current open queue

- [ ] **In progress — RT-F380/RT-F384:** supported native build/channel, complete private profile/network isolation and actual native Start/entry/hull/unspectate. Approved copy reached welcome only; user clicked Start/no confirmed result. Desktop automation stopped while user played another game; owned client/backend/private key cleaned up.
- [ ] **Testing — RT-F393:** compiled offline patch-announcement guard awaits actual native CS_Ready acceptance.
- [ ] **Not started — RT-F387:** actual client/server transfer, full original map graphical trailer, native rendering/audio/HUD.
- [ ] **Investigating — RT-F069/RT-F386:** missing Portal/PrizeCrateA, cruiart/logo, numeric Ball role and absent drop-item66 eligibility. Do not invent replacements or rewrite canonical values.
- [ ] **Investigating:** original versioned evaluator/sentinel/scaling, map entities/collision traversal, hull/strafe/hyper/reverse/cargo/energy/combat semantics. Raw parser success is narrower than native behavior.
- [ ] **Remaining native trading/join acceptance after RT-F388/RT-F389/RT-F390/RT-F396:** the derived sale/buy/store/capacity guards have actual-handler checks; connected native behavior and durable atomicity remain unverified. See [RuntimeChecks](../../scripts/compatibility/RuntimeChecks/IMPLEMENTATION_PLAN.md).

RT-F391 is closed by [source validation](../development-log/2026-10-15-rt-f391.md): PrizeItems.xml is an optional moderator whitelist, not a canonical gameplay drop table; no stub or code change was needed.

- [ ] **Not started:** original mining/refining/trading/repairs/upgrades/rewards/death/persistence loop, multiple-client acceptance and production authority reconciliation.
- [ ] **Investigating:** upstream Server.end pending-receive/disposed-socket rearm race. Bounded process exit avoids it without fixing production shutdown.
- [ ] **Not started:** choose/record production engine from measured native parity and maintenance/packaging evidence.

The active server targets net10.0, distinct from the old root README solution. Derived source fixes both wildcard socket sites to loopback, suppresses remote news/directory access under mandatory offline flag, repairs VehicleMaker field18 and guards external patch announcements. Upstream source remains unchanged; future production bind/security semantics need their own acceptance. Native update handlers accept client state/energy, which must be reconciled with the master's server authority requirement.

## Reproduction and continuation

[Tooling parent plan](../../scripts/compatibility/IMPLEMENTATION_PLAN.md) identifies exact commands and whether they execute helpers. Root coordinates builds to avoid shared-output races. Focused parser/hash/image/build checks are enough for data/render work; simulations follow completed gameplay behavior needing verification. No probe, build, native UI or simulation was run for this documentation reconciliation.

Next, when the user is available, address native welcome/profile/Start/entry with actual backend delivery evidence. Preserve pristine reports while writing derived regression reports separately. Recorded automatic-review refusal before missing Soccer recovery is historical; backend prerequisite was repaired and approved bounded test executed. No approval refusal currently blocks documentation or marks native compatibility complete. Inspect component licenses before redistribution; ignored references/working copies are not release payloads.
