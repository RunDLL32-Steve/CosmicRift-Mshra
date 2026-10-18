# Mining fixture plan

Authority: [master §§8 and 18 Milestone 2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay). See [M3 scope/evidence](plan.d/m3.md) and [parent data plan](../IMPLEMENTATION_PLAN.md).

`new-test-fixture.json` is an external NEW/provisional fixture: all ids, quantity 7, range 4 world units, cycle 3 ticks, yield 2 units and drop radius 1 world unit are invented solely for generic extraction verification. It is loaded by `MiningTests`, not by the running client. It neither replaces nor interprets canonical original spawns/equipment.

Next: original bindings after validated evaluator dependencies and M8 client integration; no original feature acceptance claimed.
