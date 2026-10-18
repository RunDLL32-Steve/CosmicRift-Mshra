# Native delivery headless checks

Purpose: bounded evidence for actual server cache/packet serialization, outgoing packet accounting and the derived offline ready guard. No sockets, authentication, accounts, transport polling, physics or native UI are invoked. Master: [§18 fidelity gate, RT-F387/RT-F393/RT-F397](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); [parent plan](../IMPLEMENTATION_PLAN.md), [native plan](../../../docs/architecture/NATIVE_CLIENT_PLAN.md), [package audit](../../../docs/architecture/ROGUE_PACKAGE_AUDIT_PLAN.md).

## Implemented scope and existing evidence

[Program.cs](Program.cs) invokes actual `AssetManager.Cache.prepareCache(List<AssetInfo>)`, cache lookup and `SC_AssetUpdate.MakeSerialized(null, null)` for 46 canonical files from the audited inventory. It independently decodes zlib, checks CRC32/SHA-256, packet name/header/length, and the full 380,052-byte original LVL trailer, then rehashes originals. The offline-ready fixture invokes actual `Logic_Login.Handle_CS_Ready` with inert server/client objects: offline `1` suppresses the external patch announcement; unset/`0` controls retain it. This is in-memory server evidence, not native delivery or handshake acceptance.

- [ ] Testing — RT-F387: native receiver/download, transport fragmentation/reassembly and full original LVL rendering still require a connected client.
- [ ] Testing — RT-F393: actual native readiness/handshake behavior remains unverified despite the inert handler checks.
- [x] Complete outgoing length/queue scope — RT-F397: the derived transform removes exactly two outgoing byte-array ushort casts. Both overloads have 60 inert reliable-queue fixtures, including indexed offset/sentinels, zero/small controls, 65,535/65,536/65,537-byte boundaries and CRC/default/exact/one-over thresholds. Receive parsing and wire bytes remain unchanged. The fix is merged to main (8b34c38); [issue evidence](../../../docs/development-log/2026-10-14-rt-f397.md).

The recorded baseline/fixed runs cover 46 canonical packets and 60 synthetic fixtures: 29 canonical metadata discrepancies, 24 synthetic length failures and 16 wrong queue choices become zero. All 60 synthetic wire hashes match between modes. The existing primary-checkout `artifacts/native-delivery-checks/fixed.json` reports `Accepted=true`; a clean checkout lacks this ignored artifact. This documentation audit read existing evidence and ran no helper.

## Reproduction and acceptance

Prerequisites: prepared ignored `.tools/compatibility/server-working`, isolated SDK 10.0.401 and cached packages, unchanged pinned originals and `artifacts/rogue-package-audit/index.json`. This project references local derived assemblies; it does not place upstream source/assets in tracked distributable outputs. The source-license audit establishes no repository-wide redistribution grant.

After a relevant packet-code change, root runs `./scripts/compatibility/run-native-delivery-checks.ps1 -Mode fixed`, serially with other shared-output builds. The wrapper compiles current source, verifies original/derived PacketBase provenance against the exact two-site transform, supplies mandatory `RT_COMPATIBILITY_OFFLINE=1`, and runs a hidden owned helper with a 30-second bound. Reports go under ignored `artifacts/native-delivery-checks/`; logs use `.tools/compatibility/native-delivery-checks/`. It records Client.cs and loaded-assembly hashes.

`-Mode baseline` is historical defect reproduction only: it requires pristine derived PacketBase and no applied PacketBase manifest entry. The wrapper does not restore that state. Do not rerun baseline against completed fixes during documentation work.

Exit 0 means the selected bounded checks passed. Baseline accepts the exact old metadata/queue behavior while reporting failed desired length invariants separately; fixed requires correct length/queue accounting for all fixtures. Unexpected integrity/ready/serialization/length results exit 2. The wrapper caches the process handle and treats a blank/nonzero exit as a real failure; the JSON `Accepted` field is the evidence of record. No native receiver, graphical rendering or full original-parity checkbox is completed by either mode.
