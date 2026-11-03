# Flight, weapon, control and HUD validation (queue G10, cloud slice)

Reviewed November 3, 2026, for queue [G10](../tasks/g10.md) on branch `claude/project-thread-ipoxc4`, built on the G6 level reader (PR #111). This is the first bounded slice of G10: it compares what the prototype does today with what the pinned originals say through the InfantryOnline emulator's unit chain. It changes no game value and checks no RT-F box. Every "observed" number for the prototype is measured by running `WorldSimulation` in `tests/Unit/G10FlightValidationTests.cs`, and every "expected" number is computed from the pinned files by `EmulatorFlightReading` (flight) or read raw from `OriginalProjectile` (weapons). The test `TableVerdictsAreCurrent` fails if either column moves, so this table cannot go stale silently.

No native client was run and no footage was measured. "Expected" therefore means "the original value read the way the emulator reads it" (RECONSTRUCTED, see [FLIGHT_FIELD_EVIDENCE.md](FLIGHT_FIELD_EVIDENCE.md) section 1), not a measurement of the native game. The questions in section 6 need the original game on Steve's desktop.

## How to read the table

-   **MATCH**: the prototype agrees with the emulator reading within 2 %.
-   **MISMATCH**: it does not. The prototype value is NEW (invented for the Milestone 1 testbed) unless the row says otherwise.
-   **MISSING**: the original has the feature and the prototype does not.
-   **OPEN**: the original value is known but its unit or meaning is not, so no verdict is possible yet.

Sources: VEH = `VEH/Rogue.veh`, ITM = `ITM/Rogue.itm`, CFG = `CFG/rogue.cfg`, all fowlmouth/cosmic-rift-assets `eb1514e`; IOS = InfantryOnline/Infantry-Online-Server `bb97579`, `dotnetcore/`. The prototype ship is `data/ships/starter_interceptor.json` and its weapon `data/weapons/plasma_cannon.json` (both NEW); the original row compared is the Asp (VEH row 18, terrain 0), the hull a new player starts in, and its starting Plasma Cannon (ITM id 1004, RT-F035).

## 1. Flight (Asp, open space)

| Row | Feature                       | Prototype, measured                                    | Original through the emulator's units                                                                                    | Verdict      | Evidence for the expected value                                                                                                     |
| --- | ----------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------ | ------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| F1  | Top speed                     | 360 units/s                                            | RollTopSpeed 3250 → 325 px/s                                                                                             | MISMATCH     | IOS `MovementController.cs` clamps the velocity length to the raw value; velocity unit 0.1 px/s (`Math.cs:216`, three paths agree)  |
| F2  | Forward acceleration          | 120 units/s² (thrust 12000 / mass 100)                 | RollThrust 45 × 4800 / 128 = 1687 units/s → 168.7 px/s²                                                                  | MISMATCH     | IOS bot thrust factor; low confidence (FLIGHT_FIELD_EVIDENCE f[204])                                                                |
| F3  | Reverse thrust equals forward | yes (12000 both)                                       | BackwardThrust −1 read as "same as RollThrust"                                                                           | MATCH        | Owner decision, November 2, 2026 (FLIGHT_FIELD_EVIDENCE section 4). The emulator bot multiplies −1 literally and is not followed    |
| F4  | Time from rest to top speed   | 3.00 s                                                 | 325 / 168.7 = 1.93 s                                                                                                     | MISMATCH     | Follows from F1 and F2                                                                                                              |
| F5  | Turn rate                     | 3.3 rad/s = 189.1°/s                                   | RollRotate 1380 / 10000 yaw units per ms × 1.5° = 207°/s (one turn in 1.74 s)                                            | MISMATCH     | IOS `MovementController.cs` rotation and `Vector2.cs` 1.5° per yaw unit; about 9 % slow                                             |
| F6  | Drag in open space            | none: a coasting ship keeps 100 % of its speed         | RollFriction 0 → keeps 100 %                                                                                             | MATCH        | IOS friction formula with raw 0; COMMUNITY H11 (momentum flight)                                                                    |
| F7  | Strafe                        | not present                                            | StrafeThrust 22 → 82.5 px/s² sideways                                                                                    | MISSING      | VEH f[207]; keys.txt "Strafe Left/Right" tokens 6/7 (RT-F026)                                                                       |
| F8  | Hyper thrust                  | not present                                            | HyperTopSpeed 3500 (350 px/s if the same unit), HyperThrust 50, HyperEnergyCost 1100                                     | MISSING/OPEN | No emulator code reads these fields, so the unit is an assumption; keys.txt "Hyper Thrust Forward/Backwards" tokens 8/9             |
| F9  | Bounce off an obstacle        | keeps 25 % (`asteroidRestitution` 0.25, NEW asteroids) | BouncePercent 727 / 1000 → keeps 72.7 % off a blocked tile                                                               | MISMATCH     | IOS `MovementController.cs` bounce; the original obstacles are LVL wall tiles (G6), which the prototype does not have yet           |
| F10 | Time to cross the map         | 1,400-unit practice sector at 360 units/s: 3.9 s       | 1999 tiles × 16 px = 31,984 px at 325 px/s: 98.4 s                                                                       | MISMATCH     | The practice sector draws the original map at 1,400 / 32,000 (`OriginalMineralFieldBuilder.MapScale`) but flies at near-pixel speed |
| F11 | Ship size                     | radius 14 units                                        | VEH f[35] = 21 (all twelve hulls), meaning a CANDIDATE (RT-F029)                                                         | OPEN         | No emulator reading of f[35] as a radius was found                                                                                  |
| F12 | Dock and trade-zone terrains  | not present: the prototype has no terrain              | Terrains 9/10: top 300 px/s, 150°/s, keeps 24 % of speed per second; terrain 15: 195°/s, keeps 39 % per second; no hyper | MISSING      | VEH terrain blocks 9, 10, 15; the bot friction depends on its update step (24 % at 10 ms, 22 % at 100 ms), so the rate is not exact |

The other eleven hulls read the same way (Excelsior top 357.5 px/s; Hornet 187.5 px/s² and 248.4°/s; Mastakith 269.1°/s; Ravager 157.5 px/s²); `OtherHullsReadTheSameWay` pins four of them. The prototype has one ship, so these have nothing to compare against yet.

F10 is the biggest gap. The prototype ship flies at roughly pixel speed, but the map it flies in (minerals, and soon the G6 walls) is shrunk about 23 times, so the world feels about 25 times smaller than the original. Fixing F1, F2 and F5 alone would not fix this; the sector scale has to change with them.

## 2. Weapon (Plasma Cannon, ITM id 1004)

| Row | Feature             | Prototype                    | Original raw                 | Reading                                                                                                                                                                                                                                  | Verdict  |
| --- | ------------------- | ---------------------------- | ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------- |
| W1  | Time between shots  | 0.20 s (cooldown, measured)  | fireDelay 25                 | 250 ms: IOS bot multiplies by 10 ms (`Vehicle.Computer.cs:84`) and the anti-spam check in `ScriptArena.cs:1253` compares raw fireDelay with the difference between two update tick counts, which agrees if those ticks are 10 ms. Medium | MISMATCH |
| W2  | Projectile speed    | 700 units/s                  | muzzleVelocity 2200          | The IOS lead-aim code treats it as the same unit as ship velocity (`Math.cs:229`), which would be 220 px/s, slower than the ship. Only one path, comment-level: low                                                                      | OPEN     |
| W3  | Projectile lifetime | 2.5 s                        | aliveTime 550                | No emulator code moves projectiles; 5.5 s if it were 10 ms units, unverified                                                                                                                                                             | OPEN     |
| W4  | Inherits ship speed | yes                          | inheritedSpeed 1000          | Per-mille reading (CFG `DefaultInheritSpeedPercent=1000` for the ball): full inheritance. Analogy only (G5 audit)                                                                                                                        | OPEN     |
| W5  | Damage, energy cost | 28 damage, 6 energy per shot | see the G5 audit and RT-F036 | The energy scale is unresolved (CFG `[Soul]` is about 1000× the hull values)                                                                                                                                                             | OPEN     |

## 3. Controls

| Row | Feature                    | Prototype default                                | Original                                                                                                                                         | Verdict |
| --- | -------------------------- | ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------ | ------- |
| K1  | Rotate left/right          | A / D and arrows                                 | keys.txt "Rotate Left/Right" `~~0key / ~~1key`                                                                                                   | OPEN    |
| K2  | Thrust forward/backward    | W / S and arrows                                 | "Thrust Forward/Backwards" `~~2key / ~~3key`                                                                                                     | OPEN    |
| K3  | Strafe left/right          | none                                             | "Strafe Left/Right" `~~6key / ~~7key`                                                                                                            | MISSING |
| K4  | Hyper forward/backward     | none                                             | "Hyper Thrust Forward/Backwards" `~~8key / ~~9key`                                                                                               | MISSING |
| K5  | Light weapon               | Space                                            | "Light Weapon" `~~11key`                                                                                                                         | OPEN    |
| K6  | Other actions in keys.txt  | dock (F), reset (R), pause (Esc), content viewer | 57 actions in 5 sections (heavy weapon, mine, repulsor, ECM, store, macros…); F11 chooses a ship (CFG line 41), Home places a Trading Post (NWS) | MISSING |
| K7  | Rebinding and saved layout | yes, saved per user                              | Unknown                                                                                                                                          | OPEN    |

The keys.txt tokens are placeholders the native client fills in from its own binding table, which is not in any pinned file, so which physical key each action used is not known (RT-F026). Section 6 asks for it.

## 4. HUD

| Row | Feature                                   | Prototype                      | Original                                                                                                        | Verdict |
| --- | ----------------------------------------- | ------------------------------ | --------------------------------------------------------------------------------------------------------------- | ------- |
| H1  | Speed readout                             | `000.0 u/s` text               | Unknown                                                                                                         | OPEN    |
| H2  | Hull, shield, energy                      | `H S E` numbers in a text line | CFG `[Uiart]` names 25 `cruiart` gauge/panel members (RT-F025); their pixels sit in the LFS-only BLO, undecoded | OPEN    |
| H3  | Radar / minimap                           | none                           | Unknown for Rogue Trader; no pinned file or measured footage shows one                                          | OPEN    |
| H4  | Credits, cargo weight, upgrades           | text panel                     | Unknown layout                                                                                                  | OPEN    |
| H5  | Developer telemetry (tick, FPS, position) | shown                          | Not part of the original                                                                                        | NEW     |

The project's own screenshots (`m8/hud.png` and others) show the prototype, not the original, so they are not evidence for this table.

## 5. What this slice settles and what it does not

Settled: the prototype's flight, weapon, control and HUD values are measured and compared row by row. Two rows match the original reading (F3 reverse thrust, F6 no drag in space). Everything else in the prototype is NEW and differs from, or is missing next to, the emulator's reading of the originals.

Not settled: the native meaning of any unit. The emulator is a reconstruction; a local run against the original client (G1, and FLIGHT_FIELD_EVIDENCE section 5) is still the only way to accept a row. No catalog or master box is ticked.

Proposed next change (not in this branch, and only with Steve's go-ahead): switch the prototype's ship to the Asp values from `EmulatorFlightReading` (F1, F2, F4, F5) and draw the practice sector at the original pixel scale (F10), then add strafe (F7, K3). That is a gameplay change, so it waits for the owner's decision and for the C7 prediction work, which shares the movement code, to land first.

## 6. Questions for Steve (need the original game or his memory)

1.  Default keys: which keys did rotate, thrust, strafe, hyper and the light weapon use in the original client (K1–K5)?
2.  Strafe and hyper: did you use them in Rogue Trader, and how was hyper switched on (held key or toggle)? Did it drain energy while held?
3.  Feel: did crossing the whole Rogue map take about a minute and a half at full speed (F10)? Did ships bounce off asteroid walls and keep most of their speed (F9)?
4.  Docks: did a ship slow down by itself inside a repair dock or trade zone (F12)?
5.  Plasma Cannon: did its shots fly slower than a ship at full speed, or faster (W2)? Roughly how far did they travel before vanishing (W3)?
6.  HUD: was there a radar, and where were the hull, shield and energy gauges on the screen (H2, H3)? A screenshot or recording from the original would settle most of section 4.

## Reproduction

-   `dotnet test tests/Unit --filter G10FlightValidationTests` with the pinned assets in `.tools/references/cosmic-rift-assets` (the asset-backed tests skip with a reason when they are missing).
-   IOS lines: clone InfantryOnline/Infantry-Online-Server at `bb97579` and read `ZoneServer/Game/Arena/Bot/MovementController.cs` (constructor, `updateState`), `ZoneServer/Protocol/Helpers/Math.cs:207–233`, `ZoneServer/Game/Objects/Vehicle.Computer.cs:75–84` and `ZoneServer/Game/Arena/ScriptArena.cs:1250–1254`.
