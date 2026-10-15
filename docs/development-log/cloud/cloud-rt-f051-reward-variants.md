Review status: UNREVIEWED

# cloud/rt-f051-reward-variants

- Claims: RT-F051 (data clauses only: raw CFG [Bounty], [Cash], [Point] reward keys); gate items 2 and 5 (data). No queue row.
- Stacked on cloud/rt-f046-c2-cfg-terrain-access (PR #15, review-pending; provides OriginalCfg). Merge that first.
- Status: done for the scoped data part. No registry box ticked.

## Acceptance clauses
Done (data): raw typed read of rogue.cfg [Bounty] (line 176: Start=1, PercentToKillerBounty=80, PercentToAssistBounty=20, FixedToKillerBounty=0), [Cash] (234: 0/0/100/1000/75), [Experience] (241: all 0), [Point] (844: 500/250/100/1000/0), checked against an independent read of the pinned file.
Remaining: choosing the documented operational revision (NWS 195-196 vs 384-387), percent scales, truncation/order, cash/points/bounty and assist semantics, native verification of completed rewards.

## Files
shared/RogueTrader.OriginalData/OriginalRewardConfig.cs, tests/Unit/OriginalRewardConfigTests.cs, shared/RogueTrader.OriginalData/plan.d/rt-f051.md.

## Verified (cloud)
- apt dotnet-sdk-8.0 8.0.131 (global.json moved aside, restored), pinned assets eb1514ea: dotnet test tests/Unit -> Passed 219, Failed 0, Skipped 0.
- python scripts/check-trackers.py -> see PR.
## Not verified in cloud - local CI required
- Pinned SDK 8.0.419 run; native behaviour.

## Resume point
Read NWS 195-196 and 384-387 and decide which revision rogue.cfg embodies before interpreting any scale.
