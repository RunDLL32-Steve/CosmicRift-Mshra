# Focused native runtime checks

Purpose: test completed derived compatibility fixes through actual upstream handlers, without native UI, socket begins, arena polling or physics. Parent: [compatibility plan](../IMPLEMENTATION_PLAN.md). Master authority: [§18 fidelity gate RT-F388/RT-F390/RT-F395](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [§9.3/§20.2](../../../ROGUE_TRADER_2_PLAN.md#93-transaction-safety).

Current stage, October 14, 2026: **Complete for the scoped RT-F388/RT-F390/RT-F395 regressions.** Root owns builds against the shared derived server. This project uses isolated SDK 10.0.401; the Godot/testbed SDK remains 8.0.419.

- [x] RT-F388: original oversell/cash-ordering defects reproduced; 17 focused sales checks verify partial/full/free/replayed/invalid sales, script veto/ownership/cash changes, arithmetic bounds and actual serialized inventory cash.
- [x] RT-F390: generic null binding reproduced at full/over/zero capacity; 12 checks verify rejection, under-capacity flow and all existing case-insensitive sport exceptions at the team-selection boundary.
- [x] RT-F395: omitted generated host radar divisors reproduced; six checks verify actual near/radar/far/cadence and weapon audiences with fresh shared generated XML.

The retained [baseline report](../../../artifacts/rogue-runtime-checks/baseline.json) contains 35 checks with exactly 16 expected defect failures; the final [fixed report](../../../artifacts/rogue-runtime-checks/fixed.json) contains 35 checks with zero failures after shared fresh configuration and forced compilation changes. Reports preserve canonical/upstream commit identities, unchanged canonical item hash, derived arena/config hashes and hashes of the runner/server/network/assets assemblies actually loaded. [Transformation guards](../../../artifacts/rogue-runtime-checks/transformation-guards.json) pass build-required, unchanged buy-branch and repeated-patch rejection checks. The related host [healthy exit](../../../artifacts/rogue-runtime-checks/host/exit.json) and [invalid-config rejection](../../../artifacts/rogue-runtime-checks/invalid-host/exit.json) are separate executable evidence, not part of the no-poll fixture suite.

## Next and parity

- [x] RT-F389: 11 Store.* checks (in-store/out-of-store sell and buy, tile boundary, out-of-range positions, spectator store on/off, players ignore the spectator flag, missing state, hook skipped) use NEW in-memory map/terrain fixtures and the original rule; 8 fail on unpatched source ([pre-patch report](../../../artifacts/rogue-runtime-checks/f389-prepatch-defect.json)), fixed report passes 61/61.
- [ ] RT-F396: purchase fulfillment/overflow/cash ordering is separately cataloged and not implemented here.
- [ ] Verify durable transaction atomicity, native trading and persistence through their own acceptance scope.

Run `./scripts/compatibility/run-runtime-checks.ps1 -Build`; execution without `-Build` is rejected so current source is always compiled. Fixed checks stage fresh XML through [write-session-config.ps1](../SessionHost/write-session-config.ps1), shared with the native-session host. Historical defect reproduction uses `-Baseline` only against the unpatched derived arena and old generated XML; it requires the exact recorded failure set and does not restore the baseline automatically. Preserve the recorded baseline rather than rerunning it against completed fixes. Reports go to ignored `artifacts/rogue-runtime-checks/`, logs to `.tools/compatibility/runtime-checks/`.

Fixture numbers/populations/positions are NEW diagnostic boundaries, not original balance. Canonical item records are read unchanged. In-memory clients only serialize/queue packets; no client transport is polled. Capacity checks stop before team assignment/spawn. These checks do not establish durable transaction atomicity, store eligibility, original buying/trading, native entry, rendering, flight/combat or persistence.
