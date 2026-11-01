# Original field interpretation contracts

## Current implementation checkpoint — October 13, 2026

[Master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap) is the status authority; [CONTINUATION.md](../../CONTINUATION.md) records the current handoff. The observations in this October 13 checkpoint are historical evidence, not a fresh verification. Selected11 BLO archives/231 sprites/36,516 frames, canonical1999×1998 overview and source/updated standalone inspector are verified; native palette/blit/bank/HUD/audio/gameplay remain open. All89 canonical BLO directories/1,262 members and609 candidates are inventoried, with eight missing/unknown raw candidates; runtime closure is unverified. Five original help files are recovered separately from pinned Zone-Assets.

The package audit matches19,198 supported values/twelve hull links/222 map references; derived-only VehicleMaker fix restores eight field18 values. Original arena initializes25 computers/100 mineral drops/51teams/42hides; full-asset bounded host uses89 canonical BLOs plus authentic supplemental Soccer. Native test reached welcome only; Start produced no confirmed entry, private test/process cleanup is complete and desktop interaction stays stopped until the user is available. Standalone native mode does not persist progression. Generic handlers being present does not prove original runtime parity. Existing dated source-access observations below retain their historical scope; completed tasks are recorded in history/[CHANGELOG.md](../../CHANGELOG.md).

Master: [§1.2 / §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md). Tracking: [feature catalog](FEATURE_CATALOG.md), [research plan](IMPLEMENTATION_PLAN.md), [engine evaluation](../architecture/ENGINE_EVALUATION.md). Reviewed October 13, 2026. No gameplay simulation was run for this research.

The canonical source is fowlmouth commit `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`. InfantryOnline server commit `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849` supplies primary source code for its current emulator and parsers. These are separate claims: a value is present in the original archive; a parser assigns that value to a field; the current emulator applies a formula; the original native client applies that formula. The first three can be established independently without establishing the fourth.

## Confirmed decoding and identities

The compatibility agent compiled the pinned active parser and read pristine copies of the selected files: v57 ITM200 records, v53 VEH30, v6 RPG12, LIO81 and LVL1999×1998 with3,994,002 tiles. Focused reports are generated under `.tools/compatibility/reports/`; [the loader source](../../scripts/compatibility/Loader/Program.cs) and engine evaluation document their reproducible scope. Parsing proves accepted versions/assigned fields, not original operational fidelity or a launchable complete game. The active LVL parser consumes through byte910,942 and leaves380,052 graphical trailer bytes unread. All222 parsed map archive/member references and three raw3,994,002-byte tile layers are audited. Native delivery must use the entire unchanged1,290,994-byte LVL; server metadata is not a replacement file.

Asp is [RPG/Rogue.rpg][rpg] row1: first six raw fields `v6,2,1,2002,1,0`; selected skill2 → vehicle2002. Asp [VEH/Rogue.veh][veh] row20 is type2 v53. The compiled parser assigns HP50, weight10000, NormalWeight1500000, StopWeight2500000, EnergyMax1700, EnergyRate1050, EnergyMin1700. RPG parser also exposes Price1 and CashAdjustment3499 for this record; these preserve source values, without assuming how selection events combine them. Hornet skill8 → vehicle2000. All twelve identity links are in the feature catalog.

## Versioned flight mapping

For v53, active [SpeedValues.Parse][speed] reads ten fields per terrain. The compiled Asp record confirms this terrain0 block. The historical `Parsers/Vehicles/SpeedValues.cs` includes a different order/comment; use the tested active version, retain the raw source, and avoid applying legacy fixed-column guesses.

| Raw Asp field | Active parsed name   | Original value | Runtime contract still to validate                          |
| ------------- | -------------------- | -------------: | ----------------------------------------------------------- |
| f[202]        | RollAllowed          |              0 | Meaning of allowed/mode versus the separate Car.Mode field. |
| f[203]        | RollFriction         |              0 | Native friction equation and terrain/upgrade overrides.     |
| f[204]        | RollThrust           |             45 | Acceleration scale, timestep and cargo/upgrade composition. |
| f[205]        | RollRotate           |           1380 | Native yaw units, rates, bank state and wrap/rounding.      |
| f[206]        | RollTopSpeed (short) |           3250 | Native speed scale and clamp policy.                        |
| f[207]        | StrafeThrust         |             22 | Lateral vector, energy/key input and native scale.          |
| f[208]        | HyperTopSpeed        |           3500 | Hyper limit, transitions and active conditions.             |
| f[209]        | HyperThrust          |             50 | Hyper acceleration and energy expenditure.                  |
| f[210]        | BackwardThrust       |             -1 | Sentinel/fallback semantics; retain negative raw value.     |
| f[211]        | BackwardHyperThrust  |             -1 | Sentinel/fallback semantics; retain negative raw value.     |
| f[362]        | BouncePercent        |            727 | Native collision response and scale.                        |
| f[373]        | HyperEnergyCost      |           1100 | Energy/time units and consumption event.                    |

All16 terrain blocks must remain available. The canonical CFG sets `Isometric=0`; an emulator bot's assumption about isometric Y coordinates must not be imported as original overhead geometry.

The original-coordinate contract is separate from the provisional prototype's meter/unit convention. Active `LvlInfo.getTileAt`/`Arena.getTerrain` divide native coordinates by16; LVL tile width1999/height1998, offsets3/3 and LIO coordinates are retained. This establishes the current parser/server tile stride; it does not prove the whole native rendering origin/extent/collision-height policy. A one-pixel-per-tile BMP overview is not an authoritative world collision mask by image inference.

## Emulator movement reference, not native player-engine proof

[MovementController][movement] is bot movement. It hardcodes terrain0 and contains comments about unavailable map terrain and uncertain Y scaling. Actual code uses:

| Current emulator bot operation                                                           | Meaning and limit                                                                                                                                          |
| ---------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| RollThrust/StrafeThrust/BackwardThrust multiplied by4800 then integer-divided by128      | A source-proven bot conversion. Negative backward sentinels are multiplied literally here; this does not establish their original native fallback meaning. |
| Direction wraps240; rotation uses `(rawRotate/10000.0)*elapsedMilliseconds`              | Current bot yaw rate. [Vector2.createUnitVector][vector] multiplies yaw by1.5 degrees, giving240 units/revolution and up at0.                              |
| Velocity receives acceleration ×elapsedMilliseconds/1000                                 | Current bot integration, not a proof of original fixed tick.                                                                                               |
| Friction uses rawFriction×800/256 plus elapsed-time factors; speed clamps to rawTopSpeed | Preserve arithmetic/truncation order when investigating, without adopting as native behavior.                                                              |
| Position +=velocity×elapsedMilliseconds/10000; Y additionally ×0.70                      | The source explicitly questions Y scaling. This is especially unresolved for canonical overhead `Isometric=0`.                                             |
| Collision bounce multiplies velocity byBouncePercent/1000                                | Current bot implementation; original collisions/height/radius still require native evidence.                                                               |

For example, raw rotate1380 would imply138 bot yaw units/second or3.6128 radians/second under that bot formula. This is a **RECONSTRUCTED native-physics hypothesis**, not an original verified conversion. It must not silently replace the provisional rotate rate or be labeled exact Cosmic Rift physics.

The current server [player-update path][player] assigns client-provided energy, velocity, position, yaw and occupied-vehicle state. It does not reconstruct the player's acceleration/projectile motion using the bot engine. This explains why server source alone cannot close the original native client behavior gap.

## Projectile fields and negative values

The active [Projectile loader][projectile] maps f[54] fireDelay, f[94] inheritedSpeed, f[101] muzzleVelocity, f[110] aliveTime, f[131] maxLivePerPlayer, f[135..138] anti recharge/fire/thrust/rotate durations, f[142] explodeItem. Compiled pristine v57 loading accepts the records. Raw observations include:

| Item row / ID                 | fireDelay | inheritedSpeed | muzzleVelocity | aliveTime | Child / relevant special values                                                                       |
| ----------------------------- | --------: | -------------: | -------------: | --------: | ----------------------------------------------------------------------------------------------------- |
| 1 /1004 Plasma Cannon         |        25 |           1000 |           2200 |       550 | explodeItem0                                                                                          |
| 7 /1026 Plasma Bomb           |       150 |           1000 |           2100 |      2000 | explodeItem0                                                                                          |
| 6 /1047 MIRV Missile Launcher |       400 |           -550 |           2750 |       -65 | explodeItem3008; ammo item2002; negative lifetime requires trigger interpretation                     |
| 141 /1069 X-Laser             |       200 |           -550 |           4000 |       350 | Source description says passes intervening objects; exact accessible/physics policy remains to decode |
| 153 /1073 Laser Cannon        |        30 |           -550 |           4000 |       550 | Negative inheritance sentinel/weighted mode unresolved                                                |
| 61 /1001 Disruptor Mine       |       400 |              0 |              0 |      6000 | maxLivePerPlayer3; anti recharge/fire/thrust/rotate150 each                                           |
| 156 /1076 Disabler            |       450 |           1000 |           2000 |      2000 | antiThrust300, antiRotate0; description also names maneuver thrusters                                 |

Searches of active `dotnetcore` source find inheritedSpeed/aliveTime declared and parsed, but no complete native projectile-motion/sentinel evaluator. Do not turn -550 into a negative percentage or -65 into a negative TTL. Native projectile defaults, weighted inheritance, trigger conditions and lifetime modes remain open even though their actual source numbers are available.

Current emulator [WeaponController][weapon] multiplies fireDelay/reloadDelay by10 for bot millisecond scheduling and divides muzzleVelocity by1000 for integer lead aiming. These are **current bot behavior**. A candidate Plasma Cannon interval250ms follows that reference, not validated original player firing. Track field/raw value, candidate conversion, evaluator provenance and native acceptance separately.

## Shield, energy and cargo semantics

Canonical `[Soul]` has EnergyDefaultMax1700000, EnergyDefaultRate250, EnergyShieldMode2, WeightNormalMax25000, WeightStopMax40000 and EnergyDefaultStart-1. The active parser reads these; the selected Asp overrides parsed energy values1700/1050/1700 and weight thresholds1500000/2500000. That scale difference must be explained by native object/default interpretation before conversion.

NWS explicitly says shield energy protects hull while any shield remains. It does not establish prototype spillover when a hit exhausts shields or a separate automatic hull-regeneration pool. Determine hit order, energy use/recharge, override scales and damage-type protection from selected native behavior. Negative default values remain visible. The prototype's separate energy/shield state and geometric collider model remain NEW engineering tuning.

Cargo behavior is present in both NWS and VEH. The unfinished work is interpreting mass/threshold and modulation formulas, not inventing absent capacity data. Preserve per-hull NormalWeight/StopWeight and each original item's weight. Older NWS capacity text differs from later changes; do not choose a convenient kilogram interpretation to hide those differences.

## Current emulator rewards versus historical variants

[Rewards.calculatePlayerKillRewards][rewards] has a source-proven `/1000` scale for its floating proportional cash/point terms. With selected CFG values, current emulator cash reward is integer truncation of `75 + targetBounty×1 + killerBounty×0.1`. Points are integer truncation of `targetBounty×1 + killerBounty×0.1`. The killer bounty then adds integer truncation of `fixedToKillerBounty/1000 + killerPoints×0.08`; the fixed term itself uses integer division. Cash/points are calculated before this bounty update. This differs from blindly treating80 as80% of the victim's bounty. Assist bounty uses killerPoints×0.02, while cash/point sharing have their own radii/percentages.

This proves **current emulator execution**, not which historical news variant the original native RT backend used. NWS overview says100% target bounty added; datedJanuary13,2002 says50%; selected CFG uses80 with a per-thousand evaluator in current server. Team kills return without the normal reward. Bot and turret rewards use separate functions/config fields; do not reuse the player formula for every original actor. Resolve original backend/revision before checking RT-F051.

## Graphics and pose mapping

Active [SpriteBlob.ParseV3][sprite] reads BlobName,BlobId,LightPermutation,PaletteOffset,HSV andAnimationTime. The Asp roll sequence `TerranShipsSmall,Asp,0,0,0,0,5,0` therefore means HSV Value5, **not column5**. Parsed broken/thrust sequences identify original animation/time values85/51 and thrust HSV Value-10. These exact original fields should survive import.

The decoder agent inspected actual Smallships CFSv4 records: frame54×54, rowCount64,columnCount11,frameCount704,RowMeaning1,BlitMode0,MaxSolidIndex247,ShadowCount8,LightCount0,CompressionFlags0,AnimationTime0; per-frame crop X/Y retained. Its pinned header schema has no ColumnOffset/ColumnMeaning. Contact sheets establish observed row0up/16right/32down/48left and visually different columns; center5 appears balanced. Original neutral/bank selection, animation transitions, direction rounding, palette/HSV/light/shadow blits remain unproved. Preserve all columns; label current center-column display RECONSTRUCTED.

## Import boundary and focused verification

Use an immutable-source adapter contract: original commit/path/row/type/version/ID; complete raw tokens and source hash; parsed version-aware fields; unresolved sentinel/mode labels; explicit candidate conversions with provenance; separate derived runtime assets. Unknown semantics must fail exact acceptance or remain visibly unresolved; do not silently coerce them to default values. All original files remain unchanged.

Parser, hash, bounds, image and source checks are appropriate during research/import. Gameplay simulation is only appropriate once the particular completed feature needs behavior validation. Full original parity requires its own acceptance evidence; no parser count, current-emulator formula, contact sheet or provisional unit suite completes it.

[rpg]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/RPG/Rogue.rpg#L1
[veh]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/VEH/Rogue.veh#L20
[speed]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/SpeedValues.cs
[movement]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/Arena/Bot/MovementController.cs
[vector]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Math/Vector2.cs#L365
[player]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/Arena/ScriptArena.cs#L1295
[projectile]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Itm/ItmInfo.Projectile.cs
[weapon]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/Arena/Bot/WeaponController.cs#L120
[rewards]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Logic/General/Rewards.cs#L157
[sprite]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/SpriteBlob.cs
