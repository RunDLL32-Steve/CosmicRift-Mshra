# Station fixture implementation plan

Authority: [master §18 Milestone 2](../../ROGUE_TRADER_2_PLAN.md#milestone-2--rogue-trader-core-gameplay), [data plan](../IMPLEMENTATION_PLAN.md) and queue [M8 brief](../../docs/tasks/m8.md). November 1, 2026.

## Purpose

`first_contact_station.json` is one **NEW/provisional** testbed station for the Godot client's HUD, station screen and save/load flow. The build stages it into `client/GameData/stations/`; `RogueTrader.Trading.StationDefinition` loads and validates it. It is not original content and completes no original feature.

## Values (all NEW/provisional)

| Value | Setting | Meaning |
|---|---:|---|
| Station position | (90, 20) | World units; places the station beside the First Contact launch point. |
| Dock radius | 150 | World units, inclusive; the player spawn (0, 0) is inside it so a new pilot can trade at once. |
| Starting wallet | 500 | Credits for a newly created profile. |
| Cargo capacity | 60 | Weight units; the world hold and the transaction core share this catalog. |
| Raw ore | weight 2, buy 12, sell 8 | Fixture good. |
| Ice crystals | weight 1, buy 6, sell 4 | Fixture good. |
| Refined alloy | weight 3, buy 40, sell 30 | Fixture good. |
| Hull patch | 40 credits, 25% of maximum hull | Fixture repair. |
| Full overhaul | 120 credits, 100% of maximum hull | Fixture repair. |
| Cargo racks / Thruster tune / Shield tuner | 300 / 350 / 450 credits | Fixture upgrades; ownership is saved, effects are not modelled. |
| Launch pile | 4 raw ore at (0, -70), radius 14 | Collectible on the route to the first target. |
| West pile | 8 ice crystals at (-170, 20), radius 14 | Collectible. |

## Open

- [ ] Replace with original station inventories, dock/proximity rule, repair amounts and upgrade effects once RT-F045/F047/F048/F049 are decoded. The original data names five upgrade groups (see [M5 entry](../../server/RogueTrader.Persistence/plan.d/m5.md)); these three fixture upgrades do not represent them.
- [ ] Collectible piles respawn on every sector reset; the original respawn rule is unknown.
