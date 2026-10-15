# ROGUE TRADER 2.0
## Master Game Design & Development Specification

**Version:** 0.1.0  
**Date:** October 12, 2026  
**Project Type:** 2D Persistent Multiplayer Space Combat / Mining / Trading RPG  
**Target Platform:** Windows PC  
**Primary Language:** C#  
**Development Environment:** Godot 4 + .NET for the initial testbed; original InfantryOnline engine compatibility under evaluation  
**Development Assistant:** OpenAI Codex

---

# 1. PROJECT OBJECTIVE

Build Rogue Trader 2.0, a modern spiritual successor to the original *Cosmic Rift: Rogue Trader*, originally published by Sony Online Entertainment.

The project must faithfully reconstruct the original gameplay wherever reliable historical evidence exists, then expand upon it with modern functionality.

The finished game should feature:

- Classic 2D overhead spacecraft gameplay.
- Momentum-based, zero-gravity movement.
- Multiple playable spacecraft with unique characteristics.
- Physics-based projectile combat.
- Mining and mineral collection.
- Trading stations and a persistent economy.
- Permanent ship upgrades and progression.
- Player-versus-player combat.
- AI-controlled enemy spacecraft.
- Multiplayer gameplay with dedicated servers.
- Multiple interconnected sectors.
- Faction warfare, bounties, and territory control.
- Modern graphical effects while retaining the original visual identity.

**Core philosophy:**

> Recreate the original experience first. Expand its possibilities second. Improve its presentation third.

The game must remain enjoyable as a straightforward space shooter without requiring players to participate in every RPG or economic system.

## 1.1 Instructions for Codex

Treat this document as the project's master specification.

Codex must:

1. Create actual, runnable source code.
2. Work incrementally through the development milestones.
3. Establish a functional game loop as early as possible.
4. Avoid implementing the entire game in a single untested iteration.
5. Use modular, maintainable architecture.
6. Implement automated tests for critical systems.
7. Maintain a running development log.
8. Never claim original gameplay behavior is historically verified without supporting evidence.
9. Prefer functional placeholder graphics over delaying implementation for visual polish.
10. Produce working builds at the completion of each milestone.

Do not substitute screenshots, architectural descriptions, or pseudocode for functioning implementations.

**Important:** This document authorizes development of an original game inspired by Cosmic Rift. Do not redistribute proprietary client code, original artwork, music, or trademarks without appropriate rights or permission.

---

## 1.2 User fidelity clarification and implementation tracking — October 12, 2026

The requested recreation target is the original Cosmic Rift gameplay and visual assets shown in [Cosmic Rift — Chaos Zone / Infantry Online](https://www.youtube.com/watch?v=A6u1ljFgQVU), applied to **Rogue Trader mode**. Use the [InfantryOnline organization](https://github.com/InfantryOnline) and [server repository](https://github.com/InfantryOnline/Infantry-Online-Server) as research inputs. The video establishes the requested presentation reference; its Chaos Zone gameplay does not establish Rogue Trader mining/economy rules.

The preserved Rogue Trader package in **InfantryOnline/Zone-Assets** supplies mode-specific help, configuration, hull and item records. Where interpreted and version-validated, recovered evidence takes precedence over the NEW placeholder ship/mineral names and values proposed elsewhere in this document. Retain unresolved units, formulas, revision differences and asset reuse status explicitly in the [research audit](docs/original-research/INFANTRYONLINE.md).

Milestones 0–1 remain the immediate engineering target. Their original geometric artwork and provisional tuning are temporary fixtures, not completed historical/visual parity. Track asset decoding, mapping, exact controls, weapon behavior, map recovery and parity separately before expanding presentation or declaring the requested recreation complete. Preserve original assets and appearance where applicable rights and mappings are established; avoid substituting an unrelated modern visual direction.

**Canonical foundation / originals unchanged:** The user identifies [fowlmouth/cosmic-rift-assets](https://github.com/fowlmouth/cosmic-rift-assets) as the complete original content source for a **100% Rogue Trader reproduction**. Pin `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`; original ships, items, configuration, map and associated asset values are authoritative. Preserve all original file bytes, names and values unchanged. Any parser, runtime adaptation or decoded display output belongs in a separate derived working area and must retain original references. The supplied [Rogue.bmp map view](https://media.githubusercontent.com/media/fowlmouth/cosmic-rift-assets/refs/heads/main/LVL-BMP/Rogue.bmp) is the map reference; LVL/LIO data requires a matching decoder. [ASSET_MANIFEST.json](docs/original-research/ASSET_MANIFEST.json) records raw hashes, LFS identities and provenance. Original-source integrity is verified with `scripts/verify-reference-assets.ps1`.

The user also proposes InfantryOnline as the engine. Evaluate its server and separate native-client requirements with a compatibility spike before selecting the production engine; see [ENGINE_EVALUATION.md](docs/architecture/ENGINE_EVALUATION.md). The Godot testbed remains an independently tested foundation, not an irreversible engine choice. Unresolved field units, sentinel values and execution semantics require interpretation of the preserved data; do not assume the values are absent or replace them with invented balance. New ships, minerals, expanded gameplay and modern presentation proposed elsewhere in the original specification are deferred until the exact reproduction acceptance target is met.

Status authority: this document's §18 checklists. [PROGRESS.md](docs/development-log/PROGRESS.md) indexes folder subplans and evidence. Every implementation subplan must link its master milestone, update its local checklist, and reconcile completed work back here. A source build, simulated acceptance test and actual exported executable validation are distinct evidence.

## 1.3 Continued exact reproduction — October13,2026

Milestones0–1 engineering acceptance is complete. Continue the original-content fidelity gate and its required implementations, then advance through M2 acceptance incrementally. Use parallel agents for independent investigation, implementation, verification and bug fixing. Every newly discovered feature must enter the [original feature catalog](docs/original-research/FEATURE_CATALOG.md) and its relevant subplan with an **unchecked** box until specifically implemented and verified. Preserve the master as the status authority.

Run simulations only after a gameplay feature is complete and requires simulation to verify it. Asset decoding, source indexing, format compatibility and rendering tasks use focused parser/integrity/image/build checks; documentation changes do not trigger repeated baseline simulations.

Current checkpoint — October 13, 2026:

- [x] Decode/index selected11 original BLO archives:231 objects/36,516 frames, original identities and indexed pixels preserved.
- [x] Verify/stage original ship/effect/resource/UI pixels and exact1999×1998 Rogue.bmp;14 focused importer/staging tests pass;46 canonical records unchanged.
- [x] Display all12 original hull identities/direction/state frames, sprite members and canonical overview in the source client. Three rendered pages pass with world tick0; Debug build zero warnings/errors.
- [x] Build/run pinned InfantryOnline parser: original CFG/200ITM/30VEH/12RPG/81LIO/1999×1998LVL accepted; raw tile/physics metadata exported.
- [x] Fix runner diagnostic-file collision/by-ref JSON export; expected failures have controlled exits.
- [x] Recover five original help dependencies with pinned corroborating provenance; bounded offline server initializes, two loopback sockets/ping verified, all owned helpers/sockets stopped.
- [x] Catalog394 stable feature entries, six original per-record domain queues, current statuses and reciprocal documentation reconciliation.
- [x] **Complete — RT-F373:** updated standalone viewer/export; native input plus3rendered pages pass exit0/worldtick0 without SDK discovery.
- [x] **Complete — RT-F374:** inspector frame retention/native editing/remaps/isolation; original native appearance remains open.
- [ ] Integrate original LVL/LIO terrain/collision/objects into playable world with validated semantics.
- [x] **Complete — RT-F375/RT-F376:** scoped directory/native-package inventories. Runtime closure,engine identity,SilentClientCrash and production engine choice remain separate open checks.
- [ ] Resolve upstream shutdown race and original evaluator/flight/combat/economy/persistence behavior.

The previous usage-window stop is historical; user has now resumed. Start from [CONTINUATION.md](CONTINUATION.md). Completed tasks leave active queues and remain in master/history/[CHANGELOG.md](CHANGELOG.md). Full original gameplay remains unfinished.

# 2. HISTORICAL GAME RESEARCH

## 2.1 Original game overview

Cosmic Rift was a 2D multiplayer space-combat game released in 2001.

It was designed by lead programmer Jeff Petersen and published by Sony Online Entertainment.

The original game contained 13 different spacecraft, with varying capabilities and unique weapons.

Players participated in different gaming zones, each featuring different objectives.

Rogue Trader was its persistent RPG-oriented mode.

The original game was shut down in March 2012.

Source:

https://en.wikipedia.org/wiki/Cosmic_Rift

Archived official game information:

https://web.archive.org/web/20070401113432/http://cosmicrift.station.sony.com/gameinfo.jsp

## 2.2 Confirmed Rogue Trader mechanics

The surviving game overview establishes the following:

| Mechanic | Historical description |
|---|---|
| Spacecraft | Multiple spacecraft with different strengths and weapons |
| Gameplay perspective | Two-dimensional overhead view |
| Mining | Players gathered minerals to earn money |
| Combat | Players earned money by destroying enemy ships |
| Progression | Money could purchase spacecraft improvements |
| Persistence | Purchased improvements persisted between gaming sessions |
| Multiplayer | The original game supported online multiplayer |

These mechanics are mandatory recreation targets.

The original game's exact ship statistics, mineral pricing, upgrade catalog, and underlying physics constants have not yet been fully recovered.

They must not be invented and subsequently described as historically accurate.

## 2.3 Additional mechanics reported by former players

The following information comes from firsthand player recollections and should be treated as valuable but not independently verified original specifications.

Community source:

https://www.reddit.com/r/longlostgamers/comments/82tn8d/cosmic_rift_looking_for_anyone_who_may_have/

Reported mechanics include:

### Spacecraft movement

- Ships could reportedly travel backward as quickly as forward.
- Combat rewarded directional thrust and careful management of momentum.
- Players could turn sideways while applying thrust to evade incoming projectiles.
- High-speed maneuvering around asteroid formations was an important gameplay skill.

### Projectile behavior

- Certain projectiles reportedly inherited the firing ship's velocity.
- The player's movement direction therefore influenced projectile trajectories.
- Projectile impact behavior reportedly rewarded accurate, direct hits.
- Laser weapon behavior differed from that of velocity-dependent projectile weapons.

### Weapons

Former players specifically recall:

- Fusion bombs.
- Laser weapons.
- MIRV-style missiles.
- Deployable stun mines.
- Ammunition costs and ammunition limitations for certain weapons.

MIRV missiles reportedly separated into multiple projectiles, making accurately timed detonation particularly dangerous.

Exact damage formulas, weapon ownership by ship, ammunition prices, and projectile velocities require further investigation.

### Mining and territory

Players recall:

- Valuable mining locations.
- Defending mining areas against opposing players.
- A resource referred to as Kran.
- Resource refineries.
- Combat around economically valuable infrastructure.
- Purchasing or constructing station outposts.

### Bounty mechanics

A former player describes a system involving accumulating bounties through combat, rewards for defeating wanted players, and additional earnings tied to existing bounty values.

The specific historical calculation is unverified.

Implement a configurable bounty system inspired by these recollections, but do not hard-code the recollected payout formula as confirmed original behavior.

## 2.4 Historical evidence classifications

All reconstructed mechanics must have an evidence classification.

| Classification | Definition |
|---|---|
| VERIFIED | Supported by original documentation or sufficiently direct surviving evidence |
| COMMUNITY | Reported by former players but not independently confirmed |
| RECONSTRUCTED | Estimated from gameplay recordings or screenshots |
| NEW | Created specifically for Rogue Trader 2.0 |
| UNKNOWN | Historical behavior has not yet been established |

Create:

`docs/original-research/MECHANICS.md`

Each historical mechanic should contain:

- Mechanic name.
- Description.
- Evidence classification.
- Source URL.
- Relevant screenshot or video timestamp, if available.
- Confidence level.
- Implementation status.
- Differences between original and recreation.

Whenever a historical detail is missing, create a configurable reconstruction instead of blocking all development.

---

# 3. GAME DESIGN PHILOSOPHY

The game must preserve five important characteristics.

### 3.1 Skill-based movement

The player should feel that they are piloting a spacecraft with mass and momentum.

The ship does not instantly change direction when the player turns.

Movement should reward practice.

### 3.2 Meaningful progression

Mining, trading, combat, and completing objectives should contribute to persistent character progression.

Upgrades must create noticeable improvements.

### 3.3 Risk and reward

Dangerous sectors should offer greater economic opportunities.

Players must have meaningful decisions regarding how much wealth and equipment they risk.

### 3.4 Distinct spacecraft

Different spacecraft should provide fundamentally different combat and economic capabilities.

Avoid allowing every ship to become identical through unrestricted upgrades.

### 3.5 Accessible gameplay

Players should be able to enjoy short sessions without studying complicated RPG mechanics.

The primary gameplay loop must remain intuitive.

---

# 4. CORE GAMEPLAY LOOP

The standard gameplay loop is:

1. Launch from a trading station.
2. Enter a sector.
3. Explore, mine, trade, or engage enemies.
4. Collect minerals, rewards, or valuable cargo.
5. Return to a station.
6. Sell collected resources.
7. Repair and resupply.
8. Purchase permanent upgrades.
9. Explore increasingly valuable or dangerous sectors.
10. Repeat.

The entire loop should be functional before major expansion features are implemented.

## 4.1 Player progression

Players begin with:

- One starter spacecraft.
- Basic weaponry.
- Limited cargo capacity.
- A small amount of currency.
- No advanced equipment.

Players gradually unlock or purchase:

- Improved engines.
- Stronger hulls.
- Additional shields.
- Better weapons.
- Increased cargo capacity.
- Mining upgrades.
- Specialized ships.
- Advanced scanners.
- Access to higher-risk regions.
- Organization and faction capabilities.

Players must be able to retain meaningful progression between sessions.

---

# 5. SPACECRAFT MOVEMENT AND PHYSICS

**Priority: Critical**

The flight model is arguably the most important gameplay system in the entire project.

Do not substitute conventional character-controller movement for spacecraft physics.

## 5.1 Movement behavior

Required capabilities:

- Forward thrust.
- Reverse thrust.
- Clockwise rotation.
- Counterclockwise rotation.
- Independent movement direction and ship orientation.
- Persistent velocity when thrust is released.
- Acceleration and deceleration.
- Ship-specific mass and thrust characteristics.
- Configurable velocity caps.
- Collision response.
- Optional inertial dampening for selected ship equipment.

The original zero-gravity movement experience is the design reference.

## 5.2 Proposed movement equations

Use fixed-timestep simulation.

```text
heading = heading + angularVelocity * deltaTime

thrustDirection = vectorFromHeading(heading)

acceleration = appliedThrust / shipMass

velocity = velocity + acceleration * deltaTime

velocity = clampMagnitude(velocity, maximumSpeed)

position = position + velocity * deltaTime
```

Reverse thrust applies acceleration opposite the ship's heading.

These equations are an initial implementation model, not a recovered original engine specification.

## 5.3 Physics requirements

- Target simulation rate: 60 ticks per second.
- Simulation independent of rendering frame rate.
- Maintain stable behavior at different client FPS values.
- Use explicit game-world units.
- Do not depend on Godot rendering-node state as the authoritative gameplay state.
- Separate collision calculation from visual effects.

## 5.4 Default controls

| Input | Action |
|---|---|
| W / Up | Forward thrust |
| S / Down | Reverse thrust |
| A / Left | Rotate counterclockwise |
| D / Right | Rotate clockwise |
| Space | Primary weapon |
| Left Shift | Secondary weapon |
| E | Interact / collect |
| M | Sector map |
| I | Inventory |
| U | Ship upgrades |
| Tab | Target selection |
| Escape | Main menu |

All controls must be remappable.

For the exact reproduction, recover and implement original Rogue Trader action bindings and modifiers from the canonical data and compatible client. The modern table above is the engineering testbed preset; an alternative modern preset may remain optional after original controls are validated.

## 5.5 Movement acceptance criteria

A player must be able to:

- Accelerate forward.
- Release thrust and continue drifting.
- Rotate without automatically changing their direction of travel.
- Apply reverse thrust.
- Fight while moving laterally.
- Navigate between asteroids.
- Experience noticeably different handling between a light and heavy spacecraft.

Movement must produce consistent results in automated fixed-step simulation tests.

---

# 6. SPACECRAFT AND LOADOUT SYSTEM

The production recreation must use the canonical original Rogue Trader catalog and unchanged original assets/values, as clarified in §1.2. The pinned RPG package contains twelve selectable hull entries: Asp, Excelsior, Ravager, Hornet, Skithist, Mastakith, Tarthist, Kragath, Rak'Kir, Rip'Pir, Zap'Pir and Zag'Gir. Their names match the candidate vehicle IDs in the canonical v53 definitions; full evaluator/physics interpretation remains a fidelity task. See [ORIGINAL_CATALOG.md](data/ORIGINAL_CATALOG.md) for actual source identities.

The initial M1 testbed has a NEW provisional Interceptor solely for engineering validation. The earlier proposed Vanguard/Prospector catalog is superseded for the recreation. Newly designed ships may be considered only after original parity is complete.

## 6.1 Ship configuration

All ships must use external data definitions.

Example:

```json
{
  "id": "starter_interceptor",
  "name": "Interceptor",
  "role": "light_combat",
  "physics": {
    "mass": 100,
    "forwardThrust": 900,
    "reverseThrust": 900,
    "rotationSpeed": 3.8,
    "maximumSpeed": 350
  },
  "defense": {
    "maximumHull": 100,
    "maximumShield": 50
  },
  "cargo": {
    "capacity": 20
  },
  "equipment": {
    "primaryWeaponSlots": 1,
    "secondaryWeaponSlots": 1,
    "utilitySlots": 2
  }
}
```

All numerical values above are provisional.

## 6.2 Equipment categories

Implement:

- Primary weapons.
- Secondary weapons.
- Reactors.
- Engines.
- Shield generators.
- Armor.
- Cargo expansions.
- Mining equipment.
- Radar/scanners.
- Utility modules.

Equipment must respect ship-specific installation restrictions.

Ships should retain distinct identities even when fully upgraded.

---

# 7. COMBAT SYSTEM

## 7.1 Essential combat features

Implement:

- Directional firing.
- Projectile velocity.
- Weapon cooldowns.
- Projectile collisions.
- Hull damage.
- Shield damage.
- Ship destruction.
- Weapon energy or ammunition consumption.
- Combat effects.
- Damage attribution.

The server is authoritative for combat in multiplayer.

## 7.2 Projectile physics

Support velocity inheritance.

For eligible weapons:

```text
projectileVelocity =
    shipVelocity +
    projectileDirection * weaponMuzzleVelocity
```

Projectile behavior must be configurable per weapon.

Not every weapon should inherit ship velocity.

This is especially important because community recollections specifically distinguish velocity-dependent projectiles from lasers.

## 7.3 Initial weapon catalog

| Weapon | Behavior | Classification |
|---|---|---|
| Plasma cannon | Standard projectile weapon | New provisional baseline |
| Laser | Straight-line energy weapon | Community-inspired |
| Fusion bomb | Heavy projectile / explosive weapon | Community-inspired |
| MIRV missile | Splits into multiple projectiles | Community-inspired |
| Stun mine | Deployable movement-disruption device | Community-inspired |
| Mining beam | Extracts asteroid resources | New implementation |

Do not assume these weapons all appeared in Rogue Trader or were available to all spacecraft.

Original equipment availability must be researched.

## 7.4 Combat extensions

Later milestones introduce:

- Weapon upgrades.
- Ship-specific special abilities.
- Multiple damage types.
- Energy management.
- Electronic warfare.
- Bounty hunting.
- Station defenses.
- Faction combat.
- Salvaging destroyed vessels.

## 7.5 Destruction rules

The initial proposed behavior:

- Destroyed ships respawn at their designated station.
- Permanent currency and purchased upgrades remain saved.
- Unbanked cargo can be lost.
- Equipment loss depends on sector-specific rules.
- Protected starter sectors limit punishment.
- Dangerous sectors increase potential losses and rewards.

These are Rogue Trader 2.0 design decisions, not verified original death mechanics.

---

# 8. MINING AND RESOURCE COLLECTION

Mining must be a substantial part of the game.

Do not implement it merely as clicking an asteroid and instantly receiving currency.

## 8.1 Mining process

1. Locate a resource-bearing asteroid.
2. Approach mining range.
3. Use compatible mining equipment.
4. Damage or extract the resource deposit.
5. Generate collectible minerals.
6. Collect minerals into cargo.
7. Return to a compatible station.
8. Sell or refine the minerals.

## 8.2 Original resource catalog

Use the unchanged canonical item definitions. The indexed type-4 resource records are:

| Original record | Source |
|---|---|
| Tellerium | Rogue_mp.itm v57 |
| Refined Tellerium | Rogue_mp.itm v57 |
| Krandazite | Rogue_mp.itm v57 |
| Refined Krandazite | Rogue_mp.itm v57 |
| Wreckage | Rogue_mp.itm v57 |
| Iron Ore | Rogue_mp.itm v57 |
| Alloy | Rogue_mp.itm v57 |

Kran is the community nickname corresponding to archived Krandazite. Retain original spelling, item IDs, source values, refining links and categories; engine interpretation and economic execution still require validation. The previously proposed Ferrite/Copperite/Iridium additions are deferred until exact reproduction is complete. Ammunition, repair and credit records must not be silently recategorized as minerals. See [ORIGINAL_CATALOG.md](data/ORIGINAL_CATALOG.md).

## 8.3 Asteroid behavior

Asteroids should support:

- Collision boundaries.
- Resource quantities.
- Resource type.
- Mining resistance.
- Depletion.
- Regeneration or respawning.
- Sector-specific distribution.
- Visually distinguishable resource deposits.

## 8.4 Cargo

Cargo must have:

- Maximum capacity.
- Item type.
- Quantity.
- Unit weight.
- Stack behavior.
- Secure storage at stations.

Cargo capacity and weight must be server-authoritative.

A player cannot collect more resources than the ship can carry.

## 8.5 Future mining expansions

- Specialized mining ships.
- Improved scanners.
- Rich deposits.
- Contested mining outposts.
- Defensive structures.
- Cooperative mining.
- Resource-processing infrastructure.

---

# 9. TRADING AND ECONOMY

## 9.1 Currency

Use a single primary currency during initial development.

All balances and transactions must be controlled by the server.

Never allow the client to submit an arbitrary updated balance.

## 9.2 Stations

The initial trading station must support:

- Docking or proximity-based interaction.
- Buying.
- Selling.
- Repairs.
- Resupply.
- Ship upgrades.
- Inventory storage.
- Saving persistent progression.

## 9.3 Transaction safety

Every purchase or sale must:

1. Validate the player's identity.
2. Validate proximity or docking requirements.
3. Verify inventory and available currency.
4. Calculate the authoritative price.
5. Update balances and inventory atomically.
6. Prevent duplicate transaction processing.
7. Persist the resulting state.

Use integer currency units rather than floating-point balances.

## 9.4 Economy expansion

Later milestones should add:

- Regional commodity prices.
- Supply and demand.
- Refining.
- Station ownership.
- Resource sinks.
- Trading contracts.
- NPC commercial traffic.
- Player-to-player trading.
- Economic statistics for balancing.

The economy must prevent unlimited currency creation through repeated buy/sell transactions.

---

# 10. BOUNTY AND REPUTATION SYSTEM

The bounty mechanic should be implemented early enough to influence PvP design.

## 10.1 Proposed bounty rules

- Players accumulate bounties for selected hostile actions.
- Bounties are attached to persistent player identities.
- Defeating a wanted player can generate a reward.
- Repeated hostile behavior can increase a player's wanted status.
- Protected sectors restrict bounty exploitation.
- Self-kills, alternate accounts, and repeated coordinated kills must not generate infinite rewards.

Bounty payout formulas should be configurable.

## 10.2 Reputation

Future reputation categories:

- Civilian.
- Trader.
- Miner.
- Mercenary.
- Pirate.
- Faction-specific standing.

Reputation influences:

- Station access.
- Contracts.
- NPC hostility.
- Equipment availability.
- Sector permissions.

---

# 11. UNIVERSE AND SECTOR DESIGN

Start with one complete sector, then expand into a connected universe.

## 11.1 Proposed initial universe

| Sector | Primary function | Risk |
|---|---|---|
| Haven | New-player introduction | Low |
| Asteroid Belt | Resource collection | Low |
| Frontier | Trade routes and advanced resources | Moderate |
| The Rift | Contested territory and bounty hunting | High |
| Abyss | Rare resources and advanced encounters | Very high |

These are proposed Rogue Trader 2.0 sector names, not reconstructed original map names.

## 11.2 Sector requirements

Each sector can define:

- Sector dimensions.
- Spawn locations.
- Asteroid distribution.
- Resource availability.
- Trading stations.
- NPC spawn definitions.
- Environmental hazards.
- PvP restrictions.
- Travel connections.
- Risk and reward modifiers.

## 11.3 Sector transitions

Travel between sectors must:

- Be validated by the server.
- Preserve player ship state.
- Avoid inventory duplication.
- Support failed or interrupted transfers.
- Spawn players in valid destinations.

Sector transfer logic must be idempotent and tested against disconnects.

---

# 12. ARTIFICIAL INTELLIGENCE

## 12.1 Initial NPC pirate

The first NPC must demonstrate:

- Target acquisition.
- Pursuit.
- Thrust and rotation.
- Weapon firing.
- Range management.
- Damage reception.
- Ship destruction.
- Reward attribution.

## 12.2 AI states

Recommended initial state machine:

```text
IDLE
  |
PATROL
  |
DETECT_TARGET
  |
CHASE
  |
ATTACK
  |
EVADE / RETREAT
  |
RETURN_TO_PATROL
```

NPCs must follow the same fundamental movement and combat rules as players.

Avoid cheating through instantaneous turning, unexplained acceleration, or arbitrary collision immunity.

## 12.3 Future AI roles

- Miners.
- Pirates.
- Traders.
- Escorts.
- Faction patrols.
- Bounty hunters.
- Station defenders.
- Elite enemies.

---

# 13. MULTIPLAYER ARCHITECTURE

The game should eventually support a persistent dedicated multiplayer world.

Do not build multiplayer by synchronizing arbitrary Godot scene nodes without a coherent server-authoritative simulation.

## 13.1 Initial architecture

| Component | Technology |
|---|---|
| Client | Godot 4 with C# |
| Shared simulation | C# class library |
| Dedicated server | Supported .NET LTS release |
| Database | PostgreSQL |
| Network transport | ENet or suitable UDP transport |
| Configuration | JSON |
| Deployment | Docker |
| Automated testing | xUnit or equivalent |
| CI | GitHub Actions |

Pin compatible engine, runtime, and package versions when creating the actual project.

## 13.2 Server responsibilities

The server controls:

- Ship positions.
- Velocity.
- Player inputs.
- Combat.
- Damage.
- Asteroids.
- Resource extraction.
- Inventory.
- Currency.
- Upgrades.
- NPC AI.
- Sector transitions.
- Persistent state.

## 13.3 Client responsibilities

The client controls:

- Player input capture.
- Rendering.
- Audio.
- UI.
- Local prediction.
- Reconciliation.
- Interpolation.
- Visual effects.

## 13.4 Network targets

Initial performance targets:

- 60 simulation ticks per second.
- 20 authoritative snapshots per second.
- 16–32 concurrent players in the first multiplayer world.

These are performance goals, not guaranteed capabilities.

## 13.5 Anti-cheat principles

Never trust client-submitted:

- Currency.
- Inventory quantities.
- Damage amounts.
- Successful mining claims.
- Purchased upgrades.
- Player positions without validation.
- Item creation requests.

All sensitive actions require authoritative server validation.

---

# 14. PERSISTENT DATA

Create a PostgreSQL data model supporting:

```text
Accounts
Characters
Ships
ShipEquipment
Inventory
Wallets
WalletLedger
PlayerProgression
Stations
SectorDefinitions
PlayerLocations
Bounties
Reputation
Transactions
```

Use schema migrations.

Currency and inventory operations must be transactional.

## 14.1 Persistence requirements

- Purchased upgrades survive logout.
- Currency survives logout.
- Cargo and equipment survive logout according to game rules.
- Interrupted transactions do not create duplicate items.
- Sector transfers are recoverable.
- Invalid or corrupted saves do not silently erase player progress.
- Backups and recovery procedures are documented.

The local single-player prototype may initially use an isolated save repository before PostgreSQL is introduced.

However, gameplay systems should depend on persistence interfaces rather than directly depending on a particular database.

---

# 15. USER INTERFACE

Preserve a practical, information-dense arcade style while supporting modern resolutions.

## 15.1 Main HUD

Display:

- Hull.
- Shields.
- Current speed.
- Energy.
- Currency.
- Cargo capacity.
- Primary weapon.
- Secondary weapon.
- Ammunition.
- Radar.
- Current sector.

## 15.2 Required interfaces

- Main menu.
- Ship selection.
- Gameplay HUD.
- Inventory.
- Trading station.
- Upgrade screen.
- Sector map.
- Settings.
- Controls.
- Multiplayer connection screen.
- Debug interface.

## 15.3 Resolution support

Target:

- 1920x1080.
- 2560x1440.
- 3840x2160.
- Ultrawide resolutions.
- Adjustable UI scaling.
- Windowed and fullscreen modes.

Game-world zoom must be independent of HUD scaling.

---

# 16. GRAPHICAL DIRECTION

Recreate the visual feel of a classic overhead space shooter while introducing optional modern effects.

Initial graphics:

- 2D spacecraft sprites.
- Asteroids.
- Projectiles.
- Stars.
- Stations.
- Engine exhaust.
- Explosions.
- Shield impacts.

Later enhancements:

- Dynamic lighting.
- Particle effects.
- Improved explosions.
- Animated engine trails.
- Damage indicators.
- Atmospheric backgrounds.
- Optional retro graphics preset.

Visual improvements must not compromise combat clarity or projectile visibility.

Use original or appropriately licensed assets.

---

# 17. REPOSITORY STRUCTURE

Create the following initial structure:

```text
RogueTrader2/
|
|-- README.md
|-- ROGUE_TRADER_2_PLAN.md
|-- AGENTS.md
|-- CHANGELOG.md
|-- docker-compose.yml
|
|-- client/
|   |-- project.godot
|   |-- Scenes/
|   |-- Scripts/
|   |-- UI/
|   |-- Assets/
|   |-- Audio/
|   |-- Shaders/
|
|-- server/
|   |-- RogueTrader.Server/
|   |-- RogueTrader.Simulation/
|   |-- RogueTrader.Persistence/
|   |-- RogueTrader.AI/
|
|-- shared/
|   |-- RogueTrader.Protocol/
|   |-- RogueTrader.GameData/
|
|-- data/
|   |-- ships/
|   |-- weapons/
|   |-- minerals/
|   |-- upgrades/
|   |-- sectors/
|
|-- tests/
|   |-- Unit/
|   |-- Integration/
|   |-- Network/
|   |-- LoadTests/
|
|-- docs/
|   |-- original-research/
|   |-- architecture/
|   |-- game-design/
|   |-- development-log/
|   |-- balance/
|
|-- scripts/
|
|-- .github/
|   |-- workflows/
```

Use the shared simulation library wherever practical so the client and server do not develop different gameplay rules.

The server must remain authoritative.

---

# 18. DEVELOPMENT ROADMAP

Development should be incremental.

Do not begin subsequent milestones until the previous milestone satisfies its mandatory acceptance criteria.

## MILESTONE 0 — Repository and historical research

**Estimated duration:** 1–2 weeks.

Tasks:

- [x] Create repository.
- [x] Establish Godot and .NET compatibility.
- [x] Create project structure.
- [x] Create automated build workflow.
- [x] Document original gameplay evidence.
- [x] Catalog community-reported mechanics.
- [x] Create historical mechanics matrix.
- [x] Record provisional gameplay values.
- [x] Establish development conventions.

Deliverable: Functional project skeleton and historical research foundation.

## MILESTONE 1 — Flight and combat prototype

**Estimated duration:** 2–3 weeks.

Tasks:

- [x] Launchable Windows client.
- [x] Render one playable spacecraft.
- [x] Implement fixed-timestep simulation.
- [x] Forward thrust.
- [x] Reverse thrust.
- [x] Rotation.
- [x] Momentum.
- [x] Projectile firing.
- [x] Projectile collisions.
- [x] Hull and shield damage.
- [x] Basic asteroids.
- [x] Camera tracking.
- [x] Debug HUD.
- [x] Movement tests.
- [x] Combat tests.

Acceptance criteria:

The player can launch the game, pilot a spacecraft, drift through space, navigate asteroids, fire a weapon, and destroy a target.

The executable must work without launching a development editor.

### M0/M1 completion evidence — engineering prototype

All M0 and M1 task checkboxes above refer to the initial, explicitly provisional engineering prototype. The complete solution builds; **41 unit tests** pass; the source and exported clients pass **three native-input/UI phases** and **six flight/combat phases**. The exported Windows executable was also launched with SDK/runtime discovery removed and its rendered frame inspected. Reproducible package: `artifacts/windows/FirstContact-0.1.0-win-x64.zip`. Evidence and reciprocal subplans: [PROGRESS.md](docs/development-log/PROGRESS.md).

These completed tasks do not establish original-content parity. The exact reproduction gate remains:

- [x] Select the user-designated canonical original content repository and pinned commit.
- [x] Acquire unchanged original reference assets and Rogue Trader map view outside distributable testbed assets.
- [x] Record and verify SHA-256/byte-length integrity of 46 foundational records.
- [x] Record source evidence, exact map reference, and original-data precedence over provisional proposals.
- [x] Complete source-based InfantryOnline engine feasibility evaluation and a concrete compatibility spike plan.
- [ ] Run an isolated original-client/emulator Rogue Trader compatibility spike and choose the production engine.
- [ ] Validate original format versions, field scaling, sentinel values and executable behavior.
- [x] Index original raw content and candidate dependencies: 3,086 records, 12 matched hulls, 200 items, seven resource records; original input hashes unchanged.
- [ ] Resolve exact BLO archive-member/record identities and validate complete dependency closure (545 was the initial raw-index candidate count; residual closure has not been recomputed).
- [ ] Decode original assets, map/object/collision data and preserve their original appearance/behavior.
- [ ] Validate original flight, weapons, controls, HUD and map against original evidence.
- [ ] Implement the Rogue Trader mining/trading/upgrades/persistence loop using original content and values in M2.
- [ ] Confirm full original Rogue Trader reproduction; no provisional replacement ships/minerals or unrelated expansions.

### Original feature registry — October 13 checkpoint

395 entries;16 scoped completions,379 open. Source/acceptance:[FEATURE_CATALOG.md](docs/original-research/FEATURE_CATALOG.md). Completed audit entries remain checked;active queues contain unfinished work. Native playable entry remains unverified.

- [ ] RT-F001 - [Asp](docs/original-research/FEATURE_CATALOG.md#rt-f001)
- [ ] RT-F002 - [Excelsior](docs/original-research/FEATURE_CATALOG.md#rt-f002)
- [ ] RT-F003 - [Ravager](docs/original-research/FEATURE_CATALOG.md#rt-f003)
- [ ] RT-F004 - [Hornet](docs/original-research/FEATURE_CATALOG.md#rt-f004)
- [ ] RT-F005 - [Skithist](docs/original-research/FEATURE_CATALOG.md#rt-f005)
- [ ] RT-F006 - [Mastakith](docs/original-research/FEATURE_CATALOG.md#rt-f006)
- [ ] RT-F007 - [Tarthist](docs/original-research/FEATURE_CATALOG.md#rt-f007)
- [ ] RT-F008 - [Kragath](docs/original-research/FEATURE_CATALOG.md#rt-f008)
- [ ] RT-F009 - [Rak'Kir](docs/original-research/FEATURE_CATALOG.md#rt-f009)
- [ ] RT-F010 - [Rip'Pir](docs/original-research/FEATURE_CATALOG.md#rt-f010)
- [ ] RT-F011 - [Zap'Pir](docs/original-research/FEATURE_CATALOG.md#rt-f011)
- [ ] RT-F012 - [Zag'Gir](docs/original-research/FEATURE_CATALOG.md#rt-f012)
- [ ] RT-F013 - [Tellerium](docs/original-research/FEATURE_CATALOG.md#rt-f013)
- [ ] RT-F014 - [Refined Tellerium](docs/original-research/FEATURE_CATALOG.md#rt-f014)
- [ ] RT-F015 - [Krandazite](docs/original-research/FEATURE_CATALOG.md#rt-f015)
- [ ] RT-F016 - [Refined Krandazite](docs/original-research/FEATURE_CATALOG.md#rt-f016)
- [ ] RT-F017 - [Wreckage](docs/original-research/FEATURE_CATALOG.md#rt-f017)
- [ ] RT-F018 - [Iron Ore](docs/original-research/FEATURE_CATALOG.md#rt-f018)
- [ ] RT-F019 - [Alloy](docs/original-research/FEATURE_CATALOG.md#rt-f019)
- [x] RT-F020 - [Canonical original map overview display](docs/original-research/FEATURE_CATALOG.md#rt-f020)
- [ ] RT-F021 - [Original world topology/collision](docs/original-research/FEATURE_CATALOG.md#rt-f021)
- [ ] RT-F022 - [Map viewer and grid/coordinates](docs/original-research/FEATURE_CATALOG.md#rt-f022)
- [ ] RT-F023 - [Full hull appearance/pose roster](docs/original-research/FEATURE_CATALOG.md#rt-f023)
- [ ] RT-F024 - [Native ship thrust/explosion/sound layers](docs/original-research/FEATURE_CATALOG.md#rt-f024)
- [ ] RT-F025 - [Original HUD/store/selection art](docs/original-research/FEATURE_CATALOG.md#rt-f025)
- [ ] RT-F026 - [Original controls](docs/original-research/FEATURE_CATALOG.md#rt-f026)
- [ ] RT-F027 - [Momentum/thrust/rotation](docs/original-research/FEATURE_CATALOG.md#rt-f027)
- [ ] RT-F028 - [Strafe, hyper thrust and reverse defaults](docs/original-research/FEATURE_CATALOG.md#rt-f028)
- [ ] RT-F029 - [Collision/bounce/point-blank rules](docs/original-research/FEATURE_CATALOG.md#rt-f029)
- [ ] RT-F030 - [Cargo changes handling](docs/original-research/FEATURE_CATALOG.md#rt-f030)
- [ ] RT-F031 - [Shield energy protects hull](docs/original-research/FEATURE_CATALOG.md#rt-f031)
- [ ] RT-F032 - [Energy use/recharge/capacity and upgrade effects](docs/original-research/FEATURE_CATALOG.md#rt-f032)
- [ ] RT-F033 - [Slotted inventory/category limits](docs/original-research/FEATURE_CATALOG.md#rt-f033)
- [ ] RT-F034 - [Class/skill logic](docs/original-research/FEATURE_CATALOG.md#rt-f034)
- [ ] RT-F035 - [Starting loadouts/hull change](docs/original-research/FEATURE_CATALOG.md#rt-f035)
- [ ] RT-F036 - [Beam/cannon/bomb weighted momentum](docs/original-research/FEATURE_CATALOG.md#rt-f036)
- [ ] RT-F037 - [Original firing delay/life/muzzle scales](docs/original-research/FEATURE_CATALOG.md#rt-f037)
- [ ] RT-F038 - [Mines and simultaneous limits](docs/original-research/FEATURE_CATALOG.md#rt-f038)
- [ ] RT-F039 - [Stun/EMP/disable fields](docs/original-research/FEATURE_CATALOG.md#rt-f039)
- [ ] RT-F040 - [MIRV chain and negative trigger](docs/original-research/FEATURE_CATALOG.md#rt-f040)
- [ ] RT-F041 - [Damage types and armor/removal](docs/original-research/FEATURE_CATALOG.md#rt-f041)
- [ ] RT-F042 - [Mineral generation/pickup/traps](docs/original-research/FEATURE_CATALOG.md#rt-f042)
- [ ] RT-F043 - [Wreckage/drop lifecycle](docs/original-research/FEATURE_CATALOG.md#rt-f043)
- [ ] RT-F044 - [Public refinery conversion](docs/original-research/FEATURE_CATALOG.md#rt-f044)
- [ ] RT-F045 - [Public trading posts/credit effects](docs/original-research/FEATURE_CATALOG.md#rt-f045)
- [ ] RT-F046 - [Original SpaceDock store/resupply](docs/original-research/FEATURE_CATALOG.md#rt-f046)
- [ ] RT-F047 - [Repair services](docs/original-research/FEATURE_CATALOG.md#rt-f047)
- [ ] RT-F048 - [Upgrade families](docs/original-research/FEATURE_CATALOG.md#rt-f048)
- [ ] RT-F049 - [Original buy/resale/drop policy](docs/original-research/FEATURE_CATALOG.md#rt-f049)
- [ ] RT-F050 - [Spectator/team-change prune](docs/original-research/FEATURE_CATALOG.md#rt-f050)
- [ ] RT-F051 - [Bounty/cash/points variants](docs/original-research/FEATURE_CATALOG.md#rt-f051)
- [ ] RT-F052 - [Public militia spawn/strongholds](docs/original-research/FEATURE_CATALOG.md#rt-f052)
- [ ] RT-F053 - [Private/free trader teams](docs/original-research/FEATURE_CATALOG.md#rt-f053)
- [ ] RT-F054 - [HyperCore and warp routes](docs/original-research/FEATURE_CATALOG.md#rt-f054)
- [ ] RT-F055 - [Rogue Trader Outpost construction](docs/original-research/FEATURE_CATALOG.md#rt-f055)
- [ ] RT-F056 - [Satellite/utility production](docs/original-research/FEATURE_CATALOG.md#rt-f056)
- [ ] RT-F057 - [Manufactories and required computers](docs/original-research/FEATURE_CATALOG.md#rt-f057)
- [ ] RT-F058 - [Stealable damaged structures](docs/original-research/FEATURE_CATALOG.md#rt-f058)
- [ ] RT-F059 - [Distinct Null NPCs](docs/original-research/FEATURE_CATALOG.md#rt-f059)
- [ ] RT-F060 - [Distinct Pirate NPC/base](docs/original-research/FEATURE_CATALOG.md#rt-f060)
- [ ] RT-F061 - [Meteor hazard](docs/original-research/FEATURE_CATALOG.md#rt-f061)
- [ ] RT-F062 - [Solar Mold/spore ecology](docs/original-research/FEATURE_CATALOG.md#rt-f062)
- [ ] RT-F063 - [Space Storm radiation](docs/original-research/FEATURE_CATALOG.md#rt-f063)
- [ ] RT-F064 - [LOS/team radar](docs/original-research/FEATURE_CATALOG.md#rt-f064)
- [ ] RT-F065 - [Safe zones and docking boundaries](docs/original-research/FEATURE_CATALOG.md#rt-f065)
- [ ] RT-F066 - [Ambient/decorative world](docs/original-research/FEATURE_CATALOG.md#rt-f066)
- [ ] RT-F067 - [Native event mini-language](docs/original-research/FEATURE_CATALOG.md#rt-f067)
- [ ] RT-F068 - [Original character/progression policy](docs/original-research/FEATURE_CATALOG.md#rt-f068)
- [ ] RT-F069 - [Complete original dependency closure](docs/original-research/FEATURE_CATALOG.md#rt-f069)
- [x] RT-F070 - [Selected11 original BLO member indexes](docs/original-research/FEATURE_CATALOG.md#rt-f070)
- [x] RT-F071 - [Supported CFSv4 decoding on selected11 BLO batch](docs/original-research/FEATURE_CATALOG.md#rt-f071)
- [x] RT-F072 - [Derived RGB/RGBA export/staging for supported batch](docs/original-research/FEATURE_CATALOG.md#rt-f072)
- [x] RT-F073 - [Original-content frame inspector](docs/original-research/FEATURE_CATALOG.md#rt-f073)
- [ ] RT-F074 - [Native original palette/HSV/shadow blitting](docs/original-research/FEATURE_CATALOG.md#rt-f074)
- [ ] RT-F075 - [Native banking/pose choice](docs/original-research/FEATURE_CATALOG.md#rt-f075)
- [ ] RT-F076 - [Plasma Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f076)
- [ ] RT-F077 - [MicroMissile-Child-Left1](docs/original-research/FEATURE_CATALOG.md#rt-f077)
- [ ] RT-F078 - [MicroMissile-Child-Left2](docs/original-research/FEATURE_CATALOG.md#rt-f078)
- [ ] RT-F079 - [MicroMissile-Child-Right1](docs/original-research/FEATURE_CATALOG.md#rt-f079)
- [ ] RT-F080 - [MicroMissile-Child-Right2](docs/original-research/FEATURE_CATALOG.md#rt-f080)
- [ ] RT-F081 - [MIRV Missile Launcher](docs/original-research/FEATURE_CATALOG.md#rt-f081)
- [ ] RT-F082 - [Plasma Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f082)
- [ ] RT-F083 - [Repulsor](docs/original-research/FEATURE_CATALOG.md#rt-f083)
- [ ] RT-F084 - [Light Mass Driver Pellets](docs/original-research/FEATURE_CATALOG.md#rt-f084)
- [ ] RT-F085 - [Mass Driver](docs/original-research/FEATURE_CATALOG.md#rt-f085)
- [ ] RT-F086 - [Meson Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f086)
- [ ] RT-F087 - [Meson Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f087)
- [ ] RT-F088 - [Plasma Mine](docs/original-research/FEATURE_CATALOG.md#rt-f088)
- [ ] RT-F089 - [Moltor Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f089)
- [ ] RT-F090 - [Moltor Mine](docs/original-research/FEATURE_CATALOG.md#rt-f090)
- [ ] RT-F091 - [MIRV Missile-Blast](docs/original-research/FEATURE_CATALOG.md#rt-f091)
- [ ] RT-F092 - [Tachyon Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f092)
- [ ] RT-F093 - [SuperBoosters](docs/original-research/FEATURE_CATALOG.md#rt-f093)
- [ ] RT-F094 - [TurboBoosters](docs/original-research/FEATURE_CATALOG.md#rt-f094)
- [ ] RT-F095 - [HyperBoosters](docs/original-research/FEATURE_CATALOG.md#rt-f095)
- [ ] RT-F096 - [Duraplas Armor](docs/original-research/FEATURE_CATALOG.md#rt-f096)
- [ ] RT-F097 - [Carbolux Armor](docs/original-research/FEATURE_CATALOG.md#rt-f097)
- [ ] RT-F098 - [Maxilite Armor](docs/original-research/FEATURE_CATALOG.md#rt-f098)
- [ ] RT-F099 - [Turbo Maneuver Thrusters](docs/original-research/FEATURE_CATALOG.md#rt-f099)
- [ ] RT-F100 - [Super Maneuver Thrusters](docs/original-research/FEATURE_CATALOG.md#rt-f100)
- [ ] RT-F101 - [Turbo Capacitor](docs/original-research/FEATURE_CATALOG.md#rt-f101)
- [ ] RT-F102 - [Super Capacitor](docs/original-research/FEATURE_CATALOG.md#rt-f102)
- [ ] RT-F103 - [Turbocharger](docs/original-research/FEATURE_CATALOG.md#rt-f103)
- [ ] RT-F104 - [Supercharger](docs/original-research/FEATURE_CATALOG.md#rt-f104)
- [ ] RT-F105 - [ChunkA](docs/original-research/FEATURE_CATALOG.md#rt-f105)
- [ ] RT-F106 - [ChunkB](docs/original-research/FEATURE_CATALOG.md#rt-f106)
- [ ] RT-F107 - [ChunkC](docs/original-research/FEATURE_CATALOG.md#rt-f107)
- [ ] RT-F108 - [ChunkD](docs/original-research/FEATURE_CATALOG.md#rt-f108)
- [ ] RT-F109 - [ChunkE](docs/original-research/FEATURE_CATALOG.md#rt-f109)
- [ ] RT-F110 - [ChunkF](docs/original-research/FEATURE_CATALOG.md#rt-f110)
- [ ] RT-F111 - [Debris](docs/original-research/FEATURE_CATALOG.md#rt-f111)
- [ ] RT-F112 - [DebrisB](docs/original-research/FEATURE_CATALOG.md#rt-f112)
- [ ] RT-F113 - [DebrisC](docs/original-research/FEATURE_CATALOG.md#rt-f113)
- [ ] RT-F114 - [DebrisD](docs/original-research/FEATURE_CATALOG.md#rt-f114)
- [ ] RT-F115 - [DebrisE](docs/original-research/FEATURE_CATALOG.md#rt-f115)
- [ ] RT-F116 - [DebrisF](docs/original-research/FEATURE_CATALOG.md#rt-f116)
- [ ] RT-F117 - [ExplosionDebrisSparks](docs/original-research/FEATURE_CATALOG.md#rt-f117)
- [ ] RT-F118 - [SkithzarExplosionDebris](docs/original-research/FEATURE_CATALOG.md#rt-f118)
- [ ] RT-F119 - [TerranExplosionDebris](docs/original-research/FEATURE_CATALOG.md#rt-f119)
- [ ] RT-F120 - [SkithChunkA](docs/original-research/FEATURE_CATALOG.md#rt-f120)
- [ ] RT-F121 - [SkithChunkB](docs/original-research/FEATURE_CATALOG.md#rt-f121)
- [ ] RT-F122 - [SkithChunkC](docs/original-research/FEATURE_CATALOG.md#rt-f122)
- [ ] RT-F123 - [SkithChunkD](docs/original-research/FEATURE_CATALOG.md#rt-f123)
- [ ] RT-F124 - [Bio Burst Piece](docs/original-research/FEATURE_CATALOG.md#rt-f124)
- [ ] RT-F125 - [Bio Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f125)
- [ ] RT-F126 - [Bio Mine](docs/original-research/FEATURE_CATALOG.md#rt-f126)
- [ ] RT-F127 - [BioShrapnel Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f127)
- [ ] RT-F128 - [Burst Piece](docs/original-research/FEATURE_CATALOG.md#rt-f128)
- [ ] RT-F129 - [ClusterMine-Child](docs/original-research/FEATURE_CATALOG.md#rt-f129)
- [ ] RT-F130 - [D-Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f130)
- [ ] RT-F131 - [Shard Mine](docs/original-research/FEATURE_CATALOG.md#rt-f131)
- [ ] RT-F132 - [Shard Piece](docs/original-research/FEATURE_CATALOG.md#rt-f132)
- [ ] RT-F133 - [Shrapnel Piece](docs/original-research/FEATURE_CATALOG.md#rt-f133)
- [ ] RT-F134 - [Skithzar ClusterSchrap](docs/original-research/FEATURE_CATALOG.md#rt-f134)
- [ ] RT-F135 - [Tetron Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f135)
- [ ] RT-F136 - [Disruptor Mine](docs/original-research/FEATURE_CATALOG.md#rt-f136)
- [ ] RT-F137 - [Concussion Mine](docs/original-research/FEATURE_CATALOG.md#rt-f137)
- [ ] RT-F138 - [Sillicate Armor](docs/original-research/FEATURE_CATALOG.md#rt-f138)
- [ ] RT-F139 - [BioResonance Chamber](docs/original-research/FEATURE_CATALOG.md#rt-f139)
- [ ] RT-F140 - [BioBooster Pod](docs/original-research/FEATURE_CATALOG.md#rt-f140)
- [ ] RT-F141 - [Tiathamate Armor](docs/original-research/FEATURE_CATALOG.md#rt-f141)
- [ ] RT-F142 - [Tachyon Rocket Launcher](docs/original-research/FEATURE_CATALOG.md#rt-f142)
- [ ] RT-F143 - [Meteor](docs/original-research/FEATURE_CATALOG.md#rt-f143)
- [ ] RT-F144 - [BaseStationCannon](docs/original-research/FEATURE_CATALOG.md#rt-f144)
- [ ] RT-F145 - [DeathOrbCannon](docs/original-research/FEATURE_CATALOG.md#rt-f145)
- [ ] RT-F146 - [Mega Moltor Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f146)
- [ ] RT-F147 - [ClusterRocketLeftA](docs/original-research/FEATURE_CATALOG.md#rt-f147)
- [ ] RT-F148 - [ClusterRocketLeftB](docs/original-research/FEATURE_CATALOG.md#rt-f148)
- [ ] RT-F149 - [ClusterRocketRightA](docs/original-research/FEATURE_CATALOG.md#rt-f149)
- [ ] RT-F150 - [ClusterRocketRightB](docs/original-research/FEATURE_CATALOG.md#rt-f150)
- [ ] RT-F151 - [Cluster Rocket Launcher](docs/original-research/FEATURE_CATALOG.md#rt-f151)
- [ ] RT-F152 - [Cluster Rockets](docs/original-research/FEATURE_CATALOG.md#rt-f152)
- [ ] RT-F153 - [MIRV Missiles](docs/original-research/FEATURE_CATALOG.md#rt-f153)
- [ ] RT-F154 - [Tachyon Rockets](docs/original-research/FEATURE_CATALOG.md#rt-f154)
- [ ] RT-F155 - [Krandazite Credits](docs/original-research/FEATURE_CATALOG.md#rt-f155)
- [ ] RT-F156 - [Tellerium Credits](docs/original-research/FEATURE_CATALOG.md#rt-f156)
- [ ] RT-F157 - [Light Mass Driver](docs/original-research/FEATURE_CATALOG.md#rt-f157)
- [ ] RT-F158 - [LargeExplosion](docs/original-research/FEATURE_CATALOG.md#rt-f158)
- [ ] RT-F159 - [SpaceStationExplode](docs/original-research/FEATURE_CATALOG.md#rt-f159)
- [ ] RT-F160 - [Scrap Credits](docs/original-research/FEATURE_CATALOG.md#rt-f160)
- [ ] RT-F161 - [Advanced Moltor Core](docs/original-research/FEATURE_CATALOG.md#rt-f161)
- [ ] RT-F162 - [Bio Repellor Pods](docs/original-research/FEATURE_CATALOG.md#rt-f162)
- [ ] RT-F163 - [Hyper Capacitor](docs/original-research/FEATURE_CATALOG.md#rt-f163)
- [ ] RT-F164 - [Heavy Mass Driver](docs/original-research/FEATURE_CATALOG.md#rt-f164)
- [ ] RT-F165 - [DeathRay](docs/original-research/FEATURE_CATALOG.md#rt-f165)
- [ ] RT-F166 - [Dual Bio Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f166)
- [ ] RT-F167 - [Dual Light Mass Drivers](docs/original-research/FEATURE_CATALOG.md#rt-f167)
- [ ] RT-F168 - [PirateStationCannon](docs/original-research/FEATURE_CATALOG.md#rt-f168)
- [ ] RT-F169 - [HEL Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f169)
- [ ] RT-F170 - [Particle Beam](docs/original-research/FEATURE_CATALOG.md#rt-f170)
- [ ] RT-F171 - [Shards](docs/original-research/FEATURE_CATALOG.md#rt-f171)
- [ ] RT-F172 - [Shrapnel Missile Launcher](docs/original-research/FEATURE_CATALOG.md#rt-f172)
- [ ] RT-F173 - [Shrapnel Missiles](docs/original-research/FEATURE_CATALOG.md#rt-f173)
- [ ] RT-F174 - [Disruptor Projector](docs/original-research/FEATURE_CATALOG.md#rt-f174)
- [ ] RT-F175 - [BioPulse Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f175)
- [ ] RT-F176 - [Moltor Crystal Launcher](docs/original-research/FEATURE_CATALOG.md#rt-f176)
- [ ] RT-F177 - [Moltor Crystals](docs/original-research/FEATURE_CATALOG.md#rt-f177)
- [ ] RT-F178 - [Bio Carapace](docs/original-research/FEATURE_CATALOG.md#rt-f178)
- [ ] RT-F179 - [Enhanced Bio Carapace](docs/original-research/FEATURE_CATALOG.md#rt-f179)
- [ ] RT-F180 - [Hyper Jump](docs/original-research/FEATURE_CATALOG.md#rt-f180)
- [ ] RT-F181 - [HyperCore](docs/original-research/FEATURE_CATALOG.md#rt-f181)
- [ ] RT-F182 - [Portal](docs/original-research/FEATURE_CATALOG.md#rt-f182)
- [ ] RT-F183 - [Warp-Start](docs/original-research/FEATURE_CATALOG.md#rt-f183)
- [ ] RT-F184 - [Warp-Team](docs/original-research/FEATURE_CATALOG.md#rt-f184)
- [ ] RT-F185 - [Skithzar ClusterBurst-Child](docs/original-research/FEATURE_CATALOG.md#rt-f185)
- [ ] RT-F186 - [Hyper Beacon System](docs/original-research/FEATURE_CATALOG.md#rt-f186)
- [ ] RT-F187 - [Controlled Hyper Jump](docs/original-research/FEATURE_CATALOG.md#rt-f187)
- [ ] RT-F188 - [Hyper Recall](docs/original-research/FEATURE_CATALOG.md#rt-f188)
- [ ] RT-F189 - [Hyper Gate](docs/original-research/FEATURE_CATALOG.md#rt-f189)
- [ ] RT-F190 - [MiniBioMis-Child-Left](docs/original-research/FEATURE_CATALOG.md#rt-f190)
- [ ] RT-F191 - [MiniBioMis-Child-Rt](docs/original-research/FEATURE_CATALOG.md#rt-f191)
- [ ] RT-F192 - [Bio Missiles](docs/original-research/FEATURE_CATALOG.md#rt-f192)
- [ ] RT-F193 - [Bio Missile Launcher](docs/original-research/FEATURE_CATALOG.md#rt-f193)
- [ ] RT-F194 - [BioCloud-Child-LT](docs/original-research/FEATURE_CATALOG.md#rt-f194)
- [ ] RT-F195 - [BioCloud-Child-RT](docs/original-research/FEATURE_CATALOG.md#rt-f195)
- [ ] RT-F196 - [BioMissile Storm Launcher](docs/original-research/FEATURE_CATALOG.md#rt-f196)
- [ ] RT-F197 - [BioStorm-Blast](docs/original-research/FEATURE_CATALOG.md#rt-f197)
- [ ] RT-F198 - [BioStorm Missiles](docs/original-research/FEATURE_CATALOG.md#rt-f198)
- [ ] RT-F199 - [BioCloud-Child-LT2](docs/original-research/FEATURE_CATALOG.md#rt-f199)
- [ ] RT-F200 - [BioCloud-Child-RT2](docs/original-research/FEATURE_CATALOG.md#rt-f200)
- [ ] RT-F201 - [Turbo Reaction Thrusters](docs/original-research/FEATURE_CATALOG.md#rt-f201)
- [ ] RT-F202 - [Super Reaction Thrusters](docs/original-research/FEATURE_CATALOG.md#rt-f202)
- [ ] RT-F203 - [Bio Reactor Pods](docs/original-research/FEATURE_CATALOG.md#rt-f203)
- [ ] RT-F204 - [Mass Driver Pellets](docs/original-research/FEATURE_CATALOG.md#rt-f204)
- [ ] RT-F205 - [Heavy Mass Driver Pellets](docs/original-research/FEATURE_CATALOG.md#rt-f205)
- [ ] RT-F206 - [Ablative Shell](docs/original-research/FEATURE_CATALOG.md#rt-f206)
- [ ] RT-F207 - [Absorption Shell](docs/original-research/FEATURE_CATALOG.md#rt-f207)
- [ ] RT-F208 - [Reflex Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f208)
- [ ] RT-F209 - [Moltor Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f209)
- [ ] RT-F210 - [Moltor Rocks](docs/original-research/FEATURE_CATALOG.md#rt-f210)
- [ ] RT-F211 - [X-Laser](docs/original-research/FEATURE_CATALOG.md#rt-f211)
- [ ] RT-F212 - [Dual Moltor Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f212)
- [ ] RT-F213 - [MoltorShowerSm-Child](docs/original-research/FEATURE_CATALOG.md#rt-f213)
- [ ] RT-F214 - [MoltorShowerMd-Child](docs/original-research/FEATURE_CATALOG.md#rt-f214)
- [ ] RT-F215 - [MoltorClusterSm](docs/original-research/FEATURE_CATALOG.md#rt-f215)
- [ ] RT-F216 - [MoltorClusterMd](docs/original-research/FEATURE_CATALOG.md#rt-f216)
- [ ] RT-F217 - [Moltor Shower Launcher](docs/original-research/FEATURE_CATALOG.md#rt-f217)
- [ ] RT-F218 - [Moltor Stones](docs/original-research/FEATURE_CATALOG.md#rt-f218)
- [ ] RT-F219 - [Minor Repair](docs/original-research/FEATURE_CATALOG.md#rt-f219)
- [ ] RT-F220 - [Major Repair](docs/original-research/FEATURE_CATALOG.md#rt-f220)
- [ ] RT-F221 - [Remove Armor](docs/original-research/FEATURE_CATALOG.md#rt-f221)
- [ ] RT-F222 - [ArmorRemoveHack](docs/original-research/FEATURE_CATALOG.md#rt-f222)
- [ ] RT-F223 - [Laser Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f223)
- [ ] RT-F224 - [Dual Laser Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f224)
- [ ] RT-F225 - [Blackout Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f225)
- [ ] RT-F226 - [Disabler](docs/original-research/FEATURE_CATALOG.md#rt-f226)
- [ ] RT-F227 - [Dual Rocket Pods](docs/original-research/FEATURE_CATALOG.md#rt-f227)
- [ ] RT-F228 - [Rocket Pod](docs/original-research/FEATURE_CATALOG.md#rt-f228)
- [ ] RT-F229 - [Rocket Pod Ammo](docs/original-research/FEATURE_CATALOG.md#rt-f229)
- [ ] RT-F230 - [Repulsor Mine](docs/original-research/FEATURE_CATALOG.md#rt-f230)
- [ ] RT-F231 - [Orion Booster](docs/original-research/FEATURE_CATALOG.md#rt-f231)
- [ ] RT-F232 - [Full Repair](docs/original-research/FEATURE_CATALOG.md#rt-f232)
- [ ] RT-F233 - [Fusion Bomb](docs/original-research/FEATURE_CATALOG.md#rt-f233)
- [ ] RT-F234 - [Wake Caster](docs/original-research/FEATURE_CATALOG.md#rt-f234)
- [ ] RT-F235 - [Wake Caster-Child](docs/original-research/FEATURE_CATALOG.md#rt-f235)
- [ ] RT-F236 - [Alloy Credits](docs/original-research/FEATURE_CATALOG.md#rt-f236)
- [ ] RT-F237 - [Trading Post](docs/original-research/FEATURE_CATALOG.md#rt-f237)
- [ ] RT-F238 - [Micro Refinery](docs/original-research/FEATURE_CATALOG.md#rt-f238)
- [ ] RT-F239 - [GunSat](docs/original-research/FEATURE_CATALOG.md#rt-f239)
- [ ] RT-F240 - [BeamSat](docs/original-research/FEATURE_CATALOG.md#rt-f240)
- [ ] RT-F241 - [Computer Laser Cannon](docs/original-research/FEATURE_CATALOG.md#rt-f241)
- [ ] RT-F242 - [Computer Particle Beam](docs/original-research/FEATURE_CATALOG.md#rt-f242)
- [ ] RT-F243 - [Fusion Torch](docs/original-research/FEATURE_CATALOG.md#rt-f243)
- [ ] RT-F244 - [Bio Orbitor-child-LT](docs/original-research/FEATURE_CATALOG.md#rt-f244)
- [ ] RT-F245 - [Bio Orbitor](docs/original-research/FEATURE_CATALOG.md#rt-f245)
- [ ] RT-F246 - [Bio Orbitor-child-RT](docs/original-research/FEATURE_CATALOG.md#rt-f246)
- [ ] RT-F247 - [Micro Repair Dock](docs/original-research/FEATURE_CATALOG.md#rt-f247)
- [ ] RT-F248 - [MoldSporeBeam](docs/original-research/FEATURE_CATALOG.md#rt-f248)
- [ ] RT-F249 - [Hornet](docs/original-research/FEATURE_CATALOG.md#rt-f249)
- [ ] RT-F250 - [Asp](docs/original-research/FEATURE_CATALOG.md#rt-f250)
- [ ] RT-F251 - [Ravager](docs/original-research/FEATURE_CATALOG.md#rt-f251)
- [ ] RT-F252 - [Excelsior](docs/original-research/FEATURE_CATALOG.md#rt-f252)
- [ ] RT-F253 - [Rip'Pir](docs/original-research/FEATURE_CATALOG.md#rt-f253)
- [ ] RT-F254 - [Rak'Kir](docs/original-research/FEATURE_CATALOG.md#rt-f254)
- [ ] RT-F255 - [Zap'Pir](docs/original-research/FEATURE_CATALOG.md#rt-f255)
- [ ] RT-F256 - [Zag'Gir](docs/original-research/FEATURE_CATALOG.md#rt-f256)
- [ ] RT-F257 - [Tarthist](docs/original-research/FEATURE_CATALOG.md#rt-f257)
- [ ] RT-F258 - [Skithist](docs/original-research/FEATURE_CATALOG.md#rt-f258)
- [ ] RT-F259 - [Kragath](docs/original-research/FEATURE_CATALOG.md#rt-f259)
- [ ] RT-F260 - [Mastakith](docs/original-research/FEATURE_CATALOG.md#rt-f260)
- [ ] RT-F261 - [Turbo AfterBurners](docs/original-research/FEATURE_CATALOG.md#rt-f261)
- [ ] RT-F262 - [Super AfterBurners](docs/original-research/FEATURE_CATALOG.md#rt-f262)
- [ ] RT-F263 - [Hyper AfterBurners](docs/original-research/FEATURE_CATALOG.md#rt-f263)
- [ ] RT-F264 - [Missile Manufactory](docs/original-research/FEATURE_CATALOG.md#rt-f264)
- [ ] RT-F265 - [Munitions Manufactory](docs/original-research/FEATURE_CATALOG.md#rt-f265)
- [ ] RT-F266 - [Defense Manufactory](docs/original-research/FEATURE_CATALOG.md#rt-f266)
- [ ] RT-F267 - [Nanobot Control System](docs/original-research/FEATURE_CATALOG.md#rt-f267)
- [ ] RT-F268 - [DeathRay v2](docs/original-research/FEATURE_CATALOG.md#rt-f268)
- [ ] RT-F269 - [Refinery](docs/original-research/FEATURE_CATALOG.md#rt-f269)
- [ ] RT-F270 - [Null Death Orb](docs/original-research/FEATURE_CATALOG.md#rt-f270)
- [ ] RT-F271 - [MeteorShower](docs/original-research/FEATURE_CATALOG.md#rt-f271)
- [ ] RT-F272 - [PirateStation](docs/original-research/FEATURE_CATALOG.md#rt-f272)
- [ ] RT-F273 - [Trading Outpost](docs/original-research/FEATURE_CATALOG.md#rt-f273)
- [ ] RT-F274 - [Null Kill Station](docs/original-research/FEATURE_CATALOG.md#rt-f274)
- [ ] RT-F275 - [Repair Dock](docs/original-research/FEATURE_CATALOG.md#rt-f275)
- [ ] RT-F276 - [Micro Repair Dock](docs/original-research/FEATURE_CATALOG.md#rt-f276)
- [ ] RT-F277 - [Solar Mold](docs/original-research/FEATURE_CATALOG.md#rt-f277)
- [ ] RT-F278 - [GunSat](docs/original-research/FEATURE_CATALOG.md#rt-f278)
- [ ] RT-F279 - [BeamSat](docs/original-research/FEATURE_CATALOG.md#rt-f279)
- [ ] RT-F280 - [Micro Refinery](docs/original-research/FEATURE_CATALOG.md#rt-f280)
- [ ] RT-F281 - [Rogue Trader Outpost](docs/original-research/FEATURE_CATALOG.md#rt-f281)
- [ ] RT-F282 - [Mold Spore](docs/original-research/FEATURE_CATALOG.md#rt-f282)
- [ ] RT-F283 - [Manufactory - Missile](docs/original-research/FEATURE_CATALOG.md#rt-f283)
- [ ] RT-F284 - [Manufactory - Munitions](docs/original-research/FEATURE_CATALOG.md#rt-f284)
- [ ] RT-F285 - [Manufactory - Defense](docs/original-research/FEATURE_CATALOG.md#rt-f285)
- [ ] RT-F286 - [Spectator](docs/original-research/FEATURE_CATALOG.md#rt-f286)
- [ ] RT-F287 - [Planets - Jupiter](docs/original-research/FEATURE_CATALOG.md#rt-f287)
- [ ] RT-F288 - [Planets - Mars](docs/original-research/FEATURE_CATALOG.md#rt-f288)
- [ ] RT-F289 - [LNebula2](docs/original-research/FEATURE_CATALOG.md#rt-f289)
- [ ] RT-F290 - [Planets - Moon1](docs/original-research/FEATURE_CATALOG.md#rt-f290)
- [ ] RT-F291 - [Planets - Moon0](docs/original-research/FEATURE_CATALOG.md#rt-f291)
- [ ] RT-F292 - [Cold Harbor SpawnA](docs/original-research/FEATURE_CATALOG.md#rt-f292)
- [ ] RT-F293 - [Northern CrossA](docs/original-research/FEATURE_CATALOG.md#rt-f293)
- [ ] RT-F294 - [Northern CrossB](docs/original-research/FEATURE_CATALOG.md#rt-f294)
- [ ] RT-F295 - [Northern CrossC](docs/original-research/FEATURE_CATALOG.md#rt-f295)
- [ ] RT-F296 - [HyperBuoyWarp](docs/original-research/FEATURE_CATALOG.md#rt-f296)
- [ ] RT-F297 - [ColdHarbor](docs/original-research/FEATURE_CATALOG.md#rt-f297)
- [ ] RT-F298 - [ColdHarborDock](docs/original-research/FEATURE_CATALOG.md#rt-f298)
- [ ] RT-F299 - [RefineryA](docs/original-research/FEATURE_CATALOG.md#rt-f299)
- [ ] RT-F300 - [RefineryB](docs/original-research/FEATURE_CATALOG.md#rt-f300)
- [ ] RT-F301 - [Global Tellerium SpawnA](docs/original-research/FEATURE_CATALOG.md#rt-f301)
- [ ] RT-F302 - [Global Tellerium SpawnC](docs/original-research/FEATURE_CATALOG.md#rt-f302)
- [ ] RT-F303 - [Global Krandazite SpawnA](docs/original-research/FEATURE_CATALOG.md#rt-f303)
- [ ] RT-F304 - [MeteorShowerA](docs/original-research/FEATURE_CATALOG.md#rt-f304)
- [ ] RT-F305 - [DeathOrbSpawn](docs/original-research/FEATURE_CATALOG.md#rt-f305)
- [ ] RT-F306 - [Pirate Base](docs/original-research/FEATURE_CATALOG.md#rt-f306)
- [ ] RT-F307 - [TradingPostA](docs/original-research/FEATURE_CATALOG.md#rt-f307)
- [ ] RT-F308 - [TradingPostB](docs/original-research/FEATURE_CATALOG.md#rt-f308)
- [ ] RT-F309 - [KillStationSpawn](docs/original-research/FEATURE_CATALOG.md#rt-f309)
- [ ] RT-F310 - [NorthernCross](docs/original-research/FEATURE_CATALOG.md#rt-f310)
- [ ] RT-F311 - [Global Tellerium SpawnB](docs/original-research/FEATURE_CATALOG.md#rt-f311)
- [ ] RT-F312 - [Global Tellerium SpawnD](docs/original-research/FEATURE_CATALOG.md#rt-f312)
- [ ] RT-F313 - [RandomHyperJump](docs/original-research/FEATURE_CATALOG.md#rt-f313)
- [ ] RT-F314 - [DeepSpaceWarpA](docs/original-research/FEATURE_CATALOG.md#rt-f314)
- [ ] RT-F315 - [CassandraA](docs/original-research/FEATURE_CATALOG.md#rt-f315)
- [ ] RT-F316 - [YankeeStationA](docs/original-research/FEATURE_CATALOG.md#rt-f316)
- [ ] RT-F317 - [YankeeStationB](docs/original-research/FEATURE_CATALOG.md#rt-f317)
- [ ] RT-F318 - [YankeeStationC](docs/original-research/FEATURE_CATALOG.md#rt-f318)
- [ ] RT-F319 - [Yankee Station](docs/original-research/FEATURE_CATALOG.md#rt-f319)
- [ ] RT-F320 - [Cassandra Station](docs/original-research/FEATURE_CATALOG.md#rt-f320)
- [ ] RT-F321 - [Scorpius Nebula](docs/original-research/FEATURE_CATALOG.md#rt-f321)
- [ ] RT-F322 - [RepairDockA](docs/original-research/FEATURE_CATALOG.md#rt-f322)
- [ ] RT-F323 - [RepairDockB](docs/original-research/FEATURE_CATALOG.md#rt-f323)
- [ ] RT-F324 - [RepairDockC](docs/original-research/FEATURE_CATALOG.md#rt-f324)
- [ ] RT-F325 - [RepairDockD](docs/original-research/FEATURE_CATALOG.md#rt-f325)
- [ ] RT-F326 - [Northern Cross DockA](docs/original-research/FEATURE_CATALOG.md#rt-f326)
- [ ] RT-F327 - [Northern Cross DockB](docs/original-research/FEATURE_CATALOG.md#rt-f327)
- [ ] RT-F328 - [Northern Cross DockC](docs/original-research/FEATURE_CATALOG.md#rt-f328)
- [ ] RT-F329 - [Northern Cross DockD](docs/original-research/FEATURE_CATALOG.md#rt-f329)
- [ ] RT-F330 - [Cassandra Dock](docs/original-research/FEATURE_CATALOG.md#rt-f330)
- [ ] RT-F331 - [YankeeDockA](docs/original-research/FEATURE_CATALOG.md#rt-f331)
- [ ] RT-F332 - [YankeeDockB](docs/original-research/FEATURE_CATALOG.md#rt-f332)
- [ ] RT-F333 - [YankeeDockC](docs/original-research/FEATURE_CATALOG.md#rt-f333)
- [ ] RT-F334 - [YankeeDockD](docs/original-research/FEATURE_CATALOG.md#rt-f334)
- [ ] RT-F335 - [MeteorShowerB](docs/original-research/FEATURE_CATALOG.md#rt-f335)
- [ ] RT-F336 - [Global Tellerium SpawnE](docs/original-research/FEATURE_CATALOG.md#rt-f336)
- [ ] RT-F337 - [Global Tellerium SpawnF](docs/original-research/FEATURE_CATALOG.md#rt-f337)
- [ ] RT-F338 - [MeteorShowerC](docs/original-research/FEATURE_CATALOG.md#rt-f338)
- [ ] RT-F339 - [Global Krandazite SpawnB](docs/original-research/FEATURE_CATALOG.md#rt-f339)
- [ ] RT-F340 - [Global Tellerium SpawnG](docs/original-research/FEATURE_CATALOG.md#rt-f340)
- [ ] RT-F341 - [Northern CrossD](docs/original-research/FEATURE_CATALOG.md#rt-f341)
- [ ] RT-F342 - [YankeeStationD](docs/original-research/FEATURE_CATALOG.md#rt-f342)
- [ ] RT-F343 - [MeteorShowerD](docs/original-research/FEATURE_CATALOG.md#rt-f343)
- [ ] RT-F344 - [RepairDockE](docs/original-research/FEATURE_CATALOG.md#rt-f344)
- [ ] RT-F345 - [Marianas FreePort](docs/original-research/FEATURE_CATALOG.md#rt-f345)
- [ ] RT-F346 - [MarianasDockA](docs/original-research/FEATURE_CATALOG.md#rt-f346)
- [ ] RT-F347 - [MarianasDockB](docs/original-research/FEATURE_CATALOG.md#rt-f347)
- [ ] RT-F348 - [MarianasDockC](docs/original-research/FEATURE_CATALOG.md#rt-f348)
- [ ] RT-F349 - [AmbientA](docs/original-research/FEATURE_CATALOG.md#rt-f349)
- [ ] RT-F350 - [AmbientB](docs/original-research/FEATURE_CATALOG.md#rt-f350)
- [ ] RT-F351 - [ColdHarbourDefSatA](docs/original-research/FEATURE_CATALOG.md#rt-f351)
- [ ] RT-F352 - [ColdHarbourDefSatB](docs/original-research/FEATURE_CATALOG.md#rt-f352)
- [ ] RT-F353 - [ColdHarbourDefSatC](docs/original-research/FEATURE_CATALOG.md#rt-f353)
- [ ] RT-F354 - [NorthernDefSatA](docs/original-research/FEATURE_CATALOG.md#rt-f354)
- [ ] RT-F355 - [NorthernDefSatB](docs/original-research/FEATURE_CATALOG.md#rt-f355)
- [ ] RT-F356 - [NorthernDefSatC](docs/original-research/FEATURE_CATALOG.md#rt-f356)
- [ ] RT-F357 - [CassandraDefSatA](docs/original-research/FEATURE_CATALOG.md#rt-f357)
- [ ] RT-F358 - [CassandraDefSatB](docs/original-research/FEATURE_CATALOG.md#rt-f358)
- [ ] RT-F359 - [CassandraDefSatC](docs/original-research/FEATURE_CATALOG.md#rt-f359)
- [ ] RT-F360 - [YankeeDefStatA](docs/original-research/FEATURE_CATALOG.md#rt-f360)
- [ ] RT-F361 - [YankeeDefStatB](docs/original-research/FEATURE_CATALOG.md#rt-f361)
- [ ] RT-F362 - [YankeeDefStatC](docs/original-research/FEATURE_CATALOG.md#rt-f362)
- [ ] RT-F363 - [Global Iron SpawnA](docs/original-research/FEATURE_CATALOG.md#rt-f363)
- [ ] RT-F364 - [Global Iron SpawnB](docs/original-research/FEATURE_CATALOG.md#rt-f364)
- [ ] RT-F365 - [Global Iron SpawnC](docs/original-research/FEATURE_CATALOG.md#rt-f365)
- [ ] RT-F366 - [Solar Mold Spawn](docs/original-research/FEATURE_CATALOG.md#rt-f366)
- [ ] RT-F367 - [MoldSporeSpawn](docs/original-research/FEATURE_CATALOG.md#rt-f367)
- [ ] RT-F368 - [Original Keystroke Listing](docs/original-research/FEATURE_CATALOG.md#rt-f368)
- [ ] RT-F369 - [Original Command Listing](docs/original-research/FEATURE_CATALOG.md#rt-f369)
- [ ] RT-F370 - [Original Messaging System](docs/original-research/FEATURE_CATALOG.md#rt-f370)
- [ ] RT-F371 - [Original Rules menu](docs/original-research/FEATURE_CATALOG.md#rt-f371)
- [ ] RT-F372 - [Original Moderators menu](docs/original-research/FEATURE_CATALOG.md#rt-f372)
- [x] RT-F373 - [Original-content standalone Windows package](docs/original-research/FEATURE_CATALOG.md#rt-f373)
- [x] RT-F374 - [Inspector input/navigation and frame-selection regression](docs/original-research/FEATURE_CATALOG.md#rt-f374)
- [x] RT-F375 - [All-archive directory and raw dependency inventory](docs/original-research/FEATURE_CATALOG.md#rt-f375)
- [x] RT-F376 - [Actual native client package inventory and version route](docs/original-research/FEATURE_CATALOG.md#rt-f376)
- [ ] RT-F377 - [Portal PrizeCrateA graphic reference](docs/original-research/FEATURE_CATALOG.md#rt-f377)
- [ ] RT-F378 - [Original Uiart logo reference](docs/original-research/FEATURE_CATALOG.md#rt-f378)
- [ ] RT-F379 - [Numeric BallGraphic member role](docs/original-research/FEATURE_CATALOG.md#rt-f379)
- [ ] RT-F380 - [Isolated native-client integration preparation](docs/original-research/FEATURE_CATALOG.md#rt-f380)
- [x] RT-F381 - [Original Rogue package links and transfer audit](docs/original-research/FEATURE_CATALOG.md#rt-f381)
- [x] RT-F382 - [Generic Rogue arena/event runtime source audit](docs/original-research/FEATURE_CATALOG.md#rt-f382)
- [x] RT-F383 - [Original-data arena initialization](docs/original-research/FEATURE_CATALOG.md#rt-f383)
- [ ] RT-F384 - [Derived native registry-namespace adapter](docs/original-research/FEATURE_CATALOG.md#rt-f384)
- [x] RT-F385 - [VehicleMaker original bounty field18 parser fix](docs/original-research/FEATURE_CATALOG.md#rt-f385)
- [ ] RT-F386 - [Computer DropItem66 reference eligibility](docs/original-research/FEATURE_CATALOG.md#rt-f386)
- [ ] RT-F387 - [Untouched full LVL native delivery](docs/original-research/FEATURE_CATALOG.md#rt-f387)
- [ ] RT-F388 - [Shop sell quantity and credit ordering](docs/original-research/FEATURE_CATALOG.md#rt-f388)
- [ ] RT-F389 - [Server store proximity validation](docs/original-research/FEATURE_CATALOG.md#rt-f389)
- [ ] RT-F390 - [Null-script full-arena join handling](docs/original-research/FEATURE_CATALOG.md#rt-f390)
- [ ] RT-F391 - [Optional admin-prize whitelist policy](docs/original-research/FEATURE_CATALOG.md#rt-f391)
- [x] RT-F392 - [Bounded original-content native session host](docs/original-research/FEATURE_CATALOG.md#rt-f392)
- [ ] RT-F393 - [Offline legacy patch announcement guard](docs/original-research/FEATURE_CATALOG.md#rt-f393)
- [x] RT-F394 - [Authentic shared Soccer engine BLO](docs/original-research/FEATURE_CATALOG.md#rt-f394)
- [x] RT-F397 - [Large asset packet length accounting](docs/original-research/FEATURE_CATALOG.md#rt-f397)

RT-F397 issue branch: both outgoing byte-array length casts are removed and verified:46 canonical cache/packets and60 inert queue fixtures pass. Baseline reproduced29 canonical metadata discrepancies,24 synthetic length defects and16 incorrect queue choices; fixed has zero failures. This branch has395 actual catalog entries; IDs395/396 belong to separate pending issue work and are not entries here. Native asset reception and ready handshake remain unverified. Evidence/status: [issue log](docs/development-log/2026-10-14-rt-f397.md).

## MILESTONE 2 — Rogue Trader core gameplay

**Estimated duration:** 3–5 weeks.

Tasks:




- [ ] Original Rogue Trader spacecraft catalog (12 RPG hull records), retaining original assets, values and installation restrictions.
- [ ] Mineral-bearing asteroids.
- [ ] Original resource catalog: Tellerium, Refined Tellerium, Krandazite, Refined Krandazite, Wreckage, Iron Ore and Alloy; preserve original item categories.
- [ ] Mining equipment.
- [ ] Collectible resources.
- [ ] Cargo capacity.
- [ ] Trading station.
- [ ] Currency.
- [ ] Buying and selling.
- [ ] Basic repairs.
- [ ] Six equipment upgrades.
- [ ] Persistent save/load.
- [ ] Initial NPC pirate.
- [ ] Initial reward system.

Acceptance criteria:

The player can mine minerals, collect cargo, sell resources, purchase an upgrade, fight an enemy, exit the game, and return with progression intact.

This is the first complete Rogue Trader gameplay loop.

## MILESTONE 3 — Dedicated multiplayer

**Estimated duration:** 4–6 weeks.

Tasks:

- [ ] Dedicated .NET server.
- [ ] Client/server protocol.
- [ ] Authentication.
- [ ] Player spawning.
- [ ] Authoritative movement.
- [ ] Prediction and reconciliation.
- [ ] Multiplayer combat.
- [ ] Shared mining state.
- [ ] Shared NPCs.
- [ ] Server-side economy.
- [ ] PostgreSQL integration.
- [ ] Reconnection handling.
- [ ] Basic anti-cheat validation.
- [ ] Multiplayer integration tests.

Acceptance criteria:

Two or more independent clients can join a world, move, fight, mine, trade, and retain their progression without state duplication or unexplained desynchronization.

## MILESTONE 4 — Expanded universe

**Estimated duration:** 4–6 weeks.

Tasks:

- [ ] Five sectors.
- [ ] Sector travel.
- [ ] Sector-specific resources.
- [ ] Multiple stations.
- [ ] Additional enemies.
- [ ] Higher-tier equipment.
- [ ] Contract system.
- [ ] Basic bounty system.
- [ ] PvP sector rules.
- [ ] Expanded economy.
- [ ] Regional progression.

Acceptance criteria:

Players can travel through a connected universe and pursue multiple progression strategies.

## MILESTONE 5 — Advanced multiplayer systems

**Estimated duration:** 4–8 weeks.

Tasks:

- [ ] Factions.
- [ ] Player organizations.
- [ ] Cooperative mining.
- [ ] Group combat.
- [ ] Bounty hunting.
- [ ] Resource outposts.
- [ ] Territory control.
- [ ] Station defenses.
- [ ] Expanded trading.
- [ ] Admin controls.
- [ ] Server monitoring.
- [ ] Exploit prevention.
- [ ] Performance testing.

Acceptance criteria:

The universe supports meaningful cooperation, competition, economic activity, and persistent multiplayer progression.

## MILESTONE 6 — Release preparation

**Estimated duration:** 4–8 weeks.

Tasks:

- [ ] Final graphics pass.
- [ ] Audio pass.
- [ ] Tutorial.
- [ ] Gameplay balancing.
- [ ] UI improvements.
- [ ] Performance optimization.
- [ ] Accessibility.
- [ ] Crash reporting.
- [ ] Backup and recovery.
- [ ] Windows packaging.
- [ ] Versioning.
- [ ] Regression testing.
- [ ] Release documentation.

Deliverable: Distributable Windows release candidate.

---

# 19. FIRST PLAYABLE BUILD — VERSION 0.1

**Codename: First Contact**

This is the immediate development target.

Scope:

| Feature | Quantity |
|---|---:|
| Playable ships | 1 initially, expanding to 3 |
| Sectors | 1 |
| Basic weapon | 1 |
| Asteroid environment | 1 |
| Debug interface | 1 |
| Automated movement tests | Required |

Build v0.1 in vertical slices.

### Vertical Slice A

Launch the client and control a spacecraft.

### Vertical Slice B

Add physically consistent rotation, acceleration, reverse thrust, and drifting.

### Vertical Slice C

Fire projectiles and destroy stationary targets.

### Vertical Slice D

Add asteroids, collisions, and a usable sector environment.

### Vertical Slice E

Package a runnable Windows build and execute automated tests.

Do not begin building the five-sector universe before this prototype is functional.

---

# 20. QUALITY ASSURANCE

Automated tests are required for all critical gameplay systems.

## 20.1 Physics tests

Verify:

- Identical inputs produce consistent simulation results.
- Velocity persists when thrust is released.
- Reverse thrust behaves correctly.
- Rotation does not unintentionally alter velocity.
- Collision behavior remains stable across simulation steps.
- Projectile velocity inheritance behaves as configured.

## 20.2 Economy tests

Verify:

- Players cannot spend money they do not have.
- Cargo quantities cannot become negative.
- Duplicate transaction requests do not create duplicate rewards.
- Buying and selling calculate prices correctly.
- Save/load preserves currency and inventory.
- Simultaneous transactions cannot double-spend funds.

## 20.3 Multiplayer tests

Verify:

- Multiple players can connect.
- Player input cannot directly overwrite authoritative world state.
- Combat events are validated.
- Disconnects do not duplicate cargo.
- Invalid sector transitions are rejected.
- Reconnection restores the correct player state.

## 20.4 Performance requirements

Target smooth gameplay at 60 FPS on a reasonably modern Windows gaming PC.

Profile actual CPU, GPU, memory, and networking behavior before increasing the supported player count.

---

# 21. CODEX DEVELOPMENT RULES

Create an `AGENTS.md` file based on these rules.

### Rule 1: Implement incrementally

Never attempt to implement the entire game in one change.

Complete small, testable features.

### Rule 2: Keep the project runnable

At the end of each implementation phase, the project should compile and launch.

If external dependencies are unavailable, clearly document the limitation.

### Rule 3: Use clean architecture

Separate:

- Simulation.
- Rendering.
- Input.
- Networking.
- Persistence.
- AI.
- Game configuration.

Avoid giant monolithic classes.

### Rule 4: No fabricated historical accuracy

Do not claim gameplay values are original unless supported by documented evidence.

Mark reconstructed values as provisional.

### Rule 5: Prioritize gameplay feel

Movement and combat take priority over elaborate UI and visual effects.

### Rule 6: Test every completed system

Run relevant automated tests after changes.

Include tests for bug fixes to prevent regression.

### Rule 7: Maintain documentation

Update:

- README.md
- CHANGELOG.md
- Development log
- Relevant technical documentation
- Implementation checklist

### Rule 8: Maintain build artifacts

Produce runnable builds at milestone boundaries.

Provide reproducible build instructions.

### Rule 9: Avoid unnecessary infrastructure

Do not introduce microservices, orchestration platforms, paid hosting dependencies, or complex frameworks without a demonstrated requirement.

A modular monolith is acceptable for the first dedicated server.

### Rule 10: Preserve user control

Do not silently replace the original game vision with unrelated gameplay systems.

New mechanics should improve the core experience.

---

# 22. HISTORICAL AND COMPARATIVE REFERENCES

These references should be retained within the repository so future development agents can understand the actual gameplay inspiration.

## Reference A — Original Cosmic Rift overview

https://en.wikipedia.org/wiki/Cosmic_Rift

Documents:

- Original publisher and designer.
- Release history.
- 13 playable spacecraft.
- Multiple game modes.
- Rogue Trader's mining, combat, purchasing, and persistent progression.

**Evidence classification:** Secondary historical reference with links to archived contemporary materials.

## Reference B — Archived official Cosmic Rift game information

https://web.archive.org/web/20070401113432/http://cosmicrift.station.sony.com/gameinfo.jsp

The archived official game information is a priority source for reconstructing original rules.

**Evidence classification:** Archived primary source.

## Reference C — Former player discussion

https://www.reddit.com/r/longlostgamers/comments/82tn8d/cosmic_rift_looking_for_anyone_who_may_have/

Contains firsthand recollections of:

- Rogue Trader.
- Mining territory.
- Kran.
- Resource refineries.
- Reverse thrust.
- Velocity-dependent combat.
- Lasers.
- Fusion bombs.
- MIRV missiles.
- Stun mines.
- Bounty accumulation.
- Outpost purchasing.

**Evidence classification:** Community testimony. Exact details require verification.

## Reference D — Cosmic Drift: Spiritual Successor

https://erodegames.itch.io/cosmic-drift/devlog/1663177/dogfight-your-friends-in-2d-space-ship-combat

Erode Games describes Cosmic Drift as a spiritual successor to Cosmic Rift.

Its development notes identify zero-friction momentum-based dogfighting as a central inspiration.

**Evidence classification:** Contemporary comparative project, not original Cosmic Rift documentation.

## Reference E — Cosmic Drift Rogue Trader Playtest

https://erodegames.itch.io/cosmic-drift/devlog/1676273/v060-rogue-trader-playtest-live

Describes another developer's interpretation of Rogue Trader, including:

- Choosing a starting hull.
- Mining minerals.
- Refining resources.
- Selling minerals.
- Trading posts.
- Equipment upgrades.
- Pirate encounters.

These are useful design comparisons but must not automatically be attributed to the historical Rogue Trader mode.

**Evidence classification:** Modern implementation reference.

## Reference F — Original gameplay footage research

Research targets:

- Cosmic Rift gameplay recordings.
- Rogue Trader multiplayer recordings.
- Original ship-selection interfaces.
- Mining demonstrations.
- Trading and upgrade screens.
- Combat footage showing projectile behavior.
- Original weapon and ship statistics.

For every discovered video, record:

```text
Title:
URL:
Date:
Relevant timestamps:
Observed mechanic:
Evidence classification:
Confidence:
Implementation implications:
```

Do not invent gameplay-video URLs or claim footage has been reviewed when it has not.

---

# 23. POST-RELEASE EXPANSION ROADMAP

Once the recreated gameplay loop and persistent multiplayer infrastructure are stable, consider adding:

- Additional ship classes.
- Expanded faction warfare.
- Player-controlled trade organizations.
- Advanced mining equipment.
- Rare ship components.
- Deep-space exploration.
- Dynamic economic events.
- Large NPC encounters.
- More sectors.
- Seasonal content.
- Dedicated community servers.
- Optional modding support.
- Additional classic-inspired game modes.

These features must not delay delivery of the foundational game.

---

# 24. DEFINITION OF SUCCESS

The initial recreation is successful when a player can:

1. Launch the game.
2. Fly a spacecraft using satisfying momentum-based physics.
3. Fight another spacecraft.
4. Mine asteroids.
5. Collect minerals.
6. Visit a station.
7. Sell resources.
8. Purchase a permanent upgrade.
9. Return to space with improved capabilities.
10. Leave the game and return with progress intact.

The multiplayer recreation is successful when multiple players can perform these activities together in a persistent world.

Rogue Trader 2.0 is successful when these foundational mechanics support a broader universe of exploration, trading, combat, cooperation, and competition without losing the arcade gameplay that inspired the project.

---

# 25. IMMEDIATE EXECUTION INSTRUCTIONS FOR CODEX

Begin development with Milestone 0 and Milestone 1.

Perform the following work in order:

1. Inspect the existing repository, if present.
2. If the repository is empty, establish the complete initial project structure.
3. Select and pin compatible stable Godot 4, C#, and .NET versions.
4. Create the Godot client.
5. Create the shared simulation library.
6. Create the initial test project.
7. Implement the fixed-timestep physics system.
8. Implement forward/reverse thrust and rotation.
9. Implement the first playable spacecraft.
10. Implement the initial camera and HUD.
11. Implement basic projectile firing and collisions.
12. Create the first asteroid environment.
13. Build and execute relevant tests.
14. Generate reproducible Windows build instructions.
15. Update the development log and checklist.

Do not introduce persistent multiplayer before the fundamental flight and combat systems are working.

Use original placeholder assets initially.

**First required deliverable: A functional Windows game where the player can launch, fly, drift, rotate, reverse, shoot, and navigate a field of asteroids.**

Do not stop at planning. Begin implementing the actual project.

## Native-engine continuation status — October 13, 2026

Original package/link/runtime audits and actual arena initializer now pass (F381/F382/F383); bounded full-asset loopback hosting and authenticSoccer dependency pass (F392/F394). DerivedVehicleMaker field18 fix passes exact8-value regression (F385). Native copy reached welcome with private namespace, but Start produced no confirmed entry; Computer Use stopped by physicalEscape while user was playing another game. Owned client/server/test keys cleaned up. F380/F384/nativeentry/actualdelivery remain unchecked. Use NATIVE_CLIENT_PLAN,ROGUE_PACKAGE_AUDIT_PLAN,ROGUE_RUNTIME_PLAN for proof/gaps; no more desktop interaction until user is available.
