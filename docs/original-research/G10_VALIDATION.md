# Flight, weapon, control and HUD validation (queue G10)

Reviewed November 3, 2026, for queue [G10](../tasks/g10.md) on branch `claude/project-thread-ipoxc4`, built on the G6 level reader (PR #111). The first slice (PR #113) compared the prototype with what the pinned originals say through the InfantryOnline emulator's unit chain. The second slice (same branch name, after #113 merged, on Steve's instruction "Continue and implement the original models", November 3, 2026) switched the shipped ship and weapon to those values: the Asp's flight through `OriginalShipPhysics.From` and the Plasma Cannon's fire interval. The table below shows the prototype after that switch. No RT-F box is checked. Every "observed" number for the prototype is measured by running `WorldSimulation` in `tests/Unit/G10FlightValidationTests.cs`, and every "expected" number is computed from the pinned files by `EmulatorFlightReading` (flight) or read raw from `OriginalProjectile` (weapons). The test `TableVerdictsAreCurrent` fails if either column moves, so this table cannot go stale silently.

No native client was run and no footage was measured. "Expected" therefore means "the original value read the way the emulator reads it" (RECONSTRUCTED, see [FLIGHT_FIELD_EVIDENCE.md](FLIGHT_FIELD_EVIDENCE.md) section 1), not a measurement of the native game. The questions in section 6 need the original game on Steve's desktop.

## How to read the table

-   **MATCH**: the prototype agrees with the emulator reading within 2 %.
-   **MISMATCH**: it does not. The prototype value is NEW (invented for the Milestone 1 testbed) unless the row says otherwise.
-   **MISSING**: the original has the feature and the prototype does not.
-   **OPEN**: the original value is known but its unit or meaning is not, so no verdict is possible yet.

Sources: VEH = `VEH/Rogue.veh`, ITM = `ITM/Rogue.itm`, CFG = `CFG/rogue.cfg`, all fowlmouth/cosmic-rift-assets `eb1514e`; IOS = InfantryOnline/Infantry-Online-Server `bb97579`, `dotnetcore/`. The prototype ship is `data/ships/starter_interceptor.json` and its weapon `data/weapons/plasma_cannon.json` (both NEW); the original row compared is the Asp (VEH row 18, terrain 0), the hull a new player starts in, and its starting Plasma Cannon (ITM id 1004, RT-F035).

## 1. Flight (Asp, open space)

| Row | Feature                       | Prototype, measured                                                 | Original through the emulator's units                                                                                    | Verdict      | Evidence for the expected value                                                                                                     |
| --- | ----------------------------- | ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ | ------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| F1  | Top speed                     | 325 px/s (was 360)                                                  | RollTopSpeed 3250 → 325 px/s                                                                                             | MATCH        | IOS `MovementController.cs` clamps the velocity length to the raw value; velocity unit 0.1 px/s (`Math.cs:216`, three paths agree)  |
| F2  | Forward acceleration          | 168.7 px/s² (thrust 16870 / mass 100; was 120)                      | RollThrust 45 × 4800 / 128 = 1687 units/s → 168.7 px/s²                                                                  | MATCH        | IOS bot thrust factor; low confidence (FLIGHT_FIELD_EVIDENCE f[204])                                                                |
| F3  | Reverse thrust equals forward | yes (16870 both)                                                    | BackwardThrust −1 read as "same as RollThrust"                                                                           | MATCH        | Owner decision, November 2, 2026 (FLIGHT_FIELD_EVIDENCE section 4). The emulator bot multiplies −1 literally and is not followed    |
| F4  | Time from rest to top speed   | 1.93 s (116 ticks; was 3.00 s)                                      | 325 / 168.7 = 1.93 s                                                                                                     | MATCH        | Follows from F1 and F2                                                                                                              |
| F5  | Turn rate                     | 3.6128 rad/s = 207°/s (was 189.1°/s)                                | RollRotate 1380 / 10000 yaw units per ms × 1.5° = 207°/s (one turn in 1.74 s)                                            | MATCH        | IOS `MovementController.cs` rotation and `Vector2.cs` 1.5° per yaw unit                                                             |
| F6  | Drag in open space            | none: a coasting ship keeps 100 % of its speed                      | RollFriction 0 → keeps 100 %                                                                                             | MATCH        | IOS friction formula with raw 0; COMMUNITY H11 (momentum flight)                                                                    |
| F7  | Strafe                        | not present                                                         | StrafeThrust 22 → 82.5 px/s² sideways                                                                                    | MISSING      | VEH f[207]; keys.txt "Strafe Left/Right" tokens 6/7 (RT-F026)                                                                       |
| F8  | Hyper thrust                  | not present                                                         | HyperTopSpeed 3500 (350 px/s if the same unit), HyperThrust 50, HyperEnergyCost 1100                                     | MISSING/OPEN | No emulator code reads these fields, so the unit is an assumption; keys.txt "Hyper Thrust Forward/Backwards" tokens 8/9             |
| F9  | Bounce off an obstacle        | keeps 72.7 % (`asteroidRestitution` 0.727, NEW asteroids; was 25 %) | BouncePercent 727 / 1000 → keeps 72.7 % off a blocked tile                                                               | MATCH        | IOS `MovementController.cs` bounce; the original obstacles are LVL wall tiles (G6), which the prototype does not have yet           |
| F10 | Time to cross the map         | 1,400-unit practice sector at 325 px/s: 4.3 s                       | 1999 tiles × 16 px = 31,984 px at 325 px/s: 98.4 s                                                                       | MISMATCH     | The practice sector draws the original map at 1,400 / 32,000 (`OriginalMineralFieldBuilder.MapScale`) but flies at near-pixel speed |
| F11 | Ship size                     | radius 14 units                                                     | VEH f[35] = 21 (all twelve hulls), meaning a CANDIDATE (RT-F029)                                                         | OPEN         | No emulator reading of f[35] as a radius was found                                                                                  |
| F12 | Dock and trade-zone terrains  | not present: the prototype has no terrain                           | Terrains 9/10: top 300 px/s, 150°/s, keeps 24 % of speed per second; terrain 15: 195°/s, keeps 39 % per second; no hyper | MISSING      | VEH terrain blocks 9, 10, 15; the bot friction depends on its update step (24 % at 10 ms, 22 % at 100 ms), so the rate is not exact |

The other eleven hulls read the same way (Excelsior top 357.5 px/s; Hornet 187.5 px/s² and 248.4°/s; Mastakith 269.1°/s; Ravager 157.5 px/s²); `OtherHullsReadTheSameWay` pins four of them. `OriginalShipPhysics.From(hull)` gives each of them as `ShipPhysics`, ready for the owned-ships roster; the prototype still flies one ship.

F10 is the remaining flight gap. The ship now flies at the original pixel speed, but the map it flies in (minerals, the refinery) is shrunk about 23 times, so the world feels about 23 times smaller than the original. Drawing the sector at the original scale moves the refinery about 16,000 px and the nearest mineral spawns several thousand px from the spawn point. That needs the original spawn point, the G6 walls and a radar to be playable, so it is a separate change (section 5). F9 bounce now keeps 72.7 % of the speed along the normal; the emulator also scales the sideways part by 0.727 and reflects off tile edges, not circles, so the shape still differs.

## 2. Weapon (Plasma Cannon, ITM id 1004)

| Row | Feature             | Prototype                               | Original raw                 | Reading                                                                                                                                                                                  | Verdict |
| --- | ------------------- | --------------------------------------- | ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------- |
| W1  | Time between shots  | 0.25 s (cooldown, measured; was 0.20 s) | fireDelay 25                 | 250 ms: IOS bot multiplies by 10 ms (`Vehicle.Computer.cs:75/84`). That is the only live path: the player anti-spam check at `ScriptArena.cs:1248–1254` is commented out at bb97579. Low | MATCH   |
| W2  | Projectile speed    | 700 units/s                             | muzzleVelocity 2200          | The IOS lead-aim code treats it as the same unit as ship velocity (`Math.cs:229`), which would be 220 px/s, slower than the ship. Only one path, comment-level: low                      | OPEN    |
| W3  | Projectile lifetime | 2.5 s                                   | aliveTime 550                | No emulator code moves projectiles; 5.5 s if it were 10 ms units, unverified                                                                                                             | OPEN    |
| W4  | Inherits ship speed | yes                                     | inheritedSpeed 1000          | Per-mille reading (CFG `DefaultInheritSpeedPercent=1000` for the ball): full inheritance. Analogy only (G5 audit)                                                                        | OPEN    |
| W5  | Damage, energy cost | 28 damage, 6 energy per shot            | see the G5 audit and RT-F036 | The energy scale is unresolved (CFG `[Soul]` is about 1000× the hull values)                                                                                                             | OPEN    |

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

Settled: the prototype's flight, weapon, control and HUD values are measured and compared row by row. After the second slice, eight rows match the original reading (F1–F6, F9, W1). F10 still differs because of the sector scale, and the rest are missing or open.

Not settled: the native meaning of any unit. The emulator is a reconstruction; a local run against the original client (G1, and FLIGHT_FIELD_EVIDENCE section 5) is still the only way to accept a row. No catalog or master box is ticked.

Done in the second slice: F1, F2, F4, F5, F9 and W1 now use the original values (Steve's go-ahead, November 3, 2026). Still to do, each a separate change: draw the sector at the original pixel scale with the G6 walls, the original spawn point and a radar (F10, F12); strafe and hyper once Steve answers questions 1 and 2 (F7, F8, K3, K4). W2–W5 keep their NEW values until their units are known.

## 6. Questions for Steve (need the original game or his memory)

1.  Default keys: which keys did rotate, thrust, strafe, hyper and the light weapon use in the original client (K1–K5)?
2.  Strafe and hyper: did you use them in Rogue Trader, and how was hyper switched on (held key or toggle)? Did it drain energy while held?
3.  Feel: did crossing the whole Rogue map take about a minute and a half at full speed (F10)? Did ships bounce off asteroid walls and keep most of their speed (F9)?
4.  Docks: did a ship slow down by itself inside a repair dock or trade zone (F12)?
5.  Plasma Cannon: did its shots fly slower than a ship at full speed, or faster (W2)? Roughly how far did they travel before vanishing (W3)?
6.  HUD: was there a radar, and where were the hull, shield and energy gauges on the screen (H2, H3)? A screenshot or recording from the original would settle most of section 4.

## Reproduction

-   `dotnet test tests/Unit --filter G10FlightValidationTests` with the pinned assets in `.tools/references/cosmic-rift-assets` (the asset-backed tests skip with a reason when they are missing).
-   IOS lines: clone InfantryOnline/Infantry-Online-Server at `bb97579` and read `ZoneServer/Game/Arena/Bot/MovementController.cs` (constructor, `updateState`), `ZoneServer/Protocol/Helpers/Math.cs:207–233`, `ZoneServer/Game/Objects/Vehicle.Computer.cs:75–84` (and `ZoneServer/Game/Arena/ScriptArena.cs:1248–1254`, a fire-timing check that is commented out at bb97579 and so is not evidence).
