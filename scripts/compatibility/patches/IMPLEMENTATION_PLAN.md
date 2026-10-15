# Derived packet-length transform

Master: [§18 RT-F397](../../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). Parent: [compatibility plan](../IMPLEMENTATION_PLAN.md).

- [x] Complete outgoing length/queue scope — RT-F397 removes exactly two outgoing byte-array ushort casts. Original receive constructor and serialized bytes remain unchanged. `prepare-derived-source.ps1` reapplies from pristine pinned source and records both hashes; the fixed wrapper verifies the exact transform.
Root baseline/fixed checks pass46 canonical packets and60 inert queue fixtures; root technical review accepted; local commit and user final testing/merge remain pending; see [issue log](../../../docs/development-log/2026-10-14-rt-f397.md). Native delivery and other pending branches are outside this fix.
