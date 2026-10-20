# InfantryOnline source and asset research

## Current implementation checkpoint — October 13, 2026

[Master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap) is the status authority; [CONTINUATION.md](../../CONTINUATION.md) records the current handoff. The observations in this October 13 checkpoint are historical evidence, not a fresh verification. Selected11 BLO archives/231 sprites/36,516 frames, canonical1999×1998 overview and source/updated standalone inspector are verified; native palette/blit/bank/HUD/audio/gameplay remain open. All89 canonical BLO directories/1,262 members and609 candidates are inventoried, with eight missing/unknown raw candidates; runtime closure is unverified. Five original help files are recovered separately from pinned Zone-Assets.

The package audit matches19,198 supported values/twelve hull links/222 map references; derived-only VehicleMaker fix restores eight field18 values. Original arena initializes25 computers/100 mineral drops/51teams/42hides; full-asset bounded host uses89 canonical BLOs plus authentic supplemental Soccer. Native test reached welcome only; Start produced no confirmed entry, private test/process cleanup is complete and desktop interaction stays stopped until the user is available. Standalone native mode does not persist progression. Generic handlers being present does not prove original runtime parity. Existing dated source-access observations below retain their historical scope; completed tasks are recorded in history/[CHANGELOG.md](../../CHANGELOG.md).

Reviewed: October 12, 2026. Master references: [§2 historical research](../../ROGUE_TRADER_2_PLAN.md#2-historical-game-research), §§5–8, §18 M0/M1 and §22. Tracking: [research subplan](IMPLEMENTATION_PLAN.md) and [development progress](../development-log/PROGRESS.md).

The user supplied [Infantry-Online-Server](https://github.com/InfantryOnline/Infantry-Online-Server), the [InfantryOnline organization](https://github.com/InfantryOnline), and a [Chaos Zone video](https://www.youtube.com/watch?v=A6u1ljFgQVU), then specified an exact Cosmic Rift presentation and asset target for **Rogue Trader mode**. The user subsequently designated [fowlmouth/cosmic-rift-assets](COSMIC_RIFT_ASSETS.md) as the canonical foundation, original bytes unchanged, for a 100% reproduction. InfantryOnline remains corroborating preservation evidence and a schema/format reference. The current NEW placeholder prototype is an engineering foundation; it does not satisfy the reproduction target.

## Repository provenance and actual access

Public GitHub API metadata and complete recursive trees were read, followed by selected raw files at the immutable commits below. No external server/parser code was copied into the project. The later [fowlmouth archive comparison](COSMIC_RIFT_ASSETS.md) streamed four overlapping binary payloads in memory solely to compare hashes; the lead agent then retrieved the user-designated canonical originals into an unchanged working reference checkout. The initial access was inventory-only. Current selected11 BLO batch is decoded/inspected, all89 directories are inventoried and separate derived resources are included in the local source/standalone inspector; the NEW training simulation remains provisional.

[SOURCE_MANIFEST.json](SOURCE_MANIFEST.json) records selected exact paths, sizes, Git blob hashes and immutable URLs. `tree_metadata_only` explicitly marks binary entries whose contents were not inspected; the manifest is a reference inventory, not an asset pack or a gameplay import.

| Repository                                                                         | Pinned commit inspected                                                                                                                            | Date exposed by commit API       | Relevant finding                                                                                                                                                                         |
| ---------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [Infantry-Online-Server](https://github.com/InfantryOnline/Infantry-Online-Server) | [bb97579efeceff1c63d781d8c1bb8dd4a5b9b849](https://github.com/InfantryOnline/Infantry-Online-Server/tree/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849) | September 26, 2026, 15:47:31 UTC | Emulator, projectile/vehicle schemas and parsers; no path named Cosmic/Rift/Rogue in the complete tree. Absence of matching names is not proof the shared engine lacks relevant support. |
| [Zone-Assets](https://github.com/InfantryOnline/Zone-Assets)                       | [401633b9b6829d9e5ae56b76a2563b1b95bbe4b9](https://github.com/InfantryOnline/Zone-Assets/tree/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9)            | September 10, 2025, 21:47:17 UTC | Preserved Rogue Trader definitions/help/configuration plus orphan ship, weapon, UI and station blobs/maps.                                                                               |
| [Playable-Zones](https://github.com/InfantryOnline/Playable-Zones)                 | [bfb8dc358e50523147b9f184640109991af50585](https://github.com/InfantryOnline/Playable-Zones/tree/bfb8dc358e50523147b9f184640109991af50585)         | November 11, 2022, 15:44:57 UTC  | Complete tree has no Cosmic/Rift/Rogue-named paths; not the discovered RT archive.                                                                                                       |
| [Infantry-Online-Tools](https://github.com/InfantryOnline/Infantry-Online-Tools)   | [93001c57845269e87b32a2bf042257c7ef61298c](https://github.com/InfantryOnline/Infantry-Online-Tools/tree/93001c57845269e87b32a2bf042257c7ef61298c)  | August 2, 2020, 04:36:23 UTC     | Blob editor, Gibbed format tools, map/decompile tools; some subcomponents have explicit permissive notices.                                                                              |
| [Editor](https://github.com/InfantryOnline/Editor)                                 | [02cfc66e454d848d1f188cad432c448e073fb8b1](https://github.com/InfantryOnline/Editor/tree/02cfc66e454d848d1f188cad432c448e073fb8b1)                 | May 8, 2025, 20:55:23 UTC        | Tree includes a vehicle editor tab; inventory-only review, not a validated import/conversion path.                                                                                       |

Organization/repository page update labels can differ from the commit date. The pinned commits and tree paths are the provenance authority for this research snapshot.

## Preserved Rogue Trader package

The [archive directory][rogue-dir] is under `Incompatible Zone Archives/SilentClientCrash/rogue`. This is a preservation candidate, not a tested launchable zone. Its [README][asset-readme] explains that incompatible archives came from old game-file backups and their folder names describe observed incompatibility categories.

| Path relative to that directory                | Actual inspection                                                               | Evidence / use                                                                                                                                                             |
| ---------------------------------------------- | ------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [rogue.cfg][rogue-cfg]                         | Text read; level, soul, vehicle, bounty/cash and event fields inspected         | Points to `Rogue.lvl`, `Rogue.veh`, `Rogue_mp.itm`, `Rogue.rpg`, `Rogue.lio`, `Rogue.nws`. `Isometric=0` identifies overhead mode.                                         |
| [Rogue.nws][rogue-news]                        | Full text read                                                                  | Mode help and dated 2001–2003 patch history. Strong preserved-artifact evidence for the RT loop, equipment, shields/hull, refining, cargo, outposts and changing formulas. |
| [keys.txt][rogue-keys]                         | Full text read                                                                  | Named movement, hyper-thrust, strafe, combat and UI actions. Bindings are substitution tokens such as `~~11key`, not literal keys.                                         |
| [rules.txt][rogue-rules]                       | Full text read                                                                  | Shared Infantry/Cosmic Rift conduct rules, internally dated November 3, 2009. Not the RT economic specification.                                                           |
| [Rogue.rpg][rogue-rpg]                         | Text read                                                                       | Twelve selectable hull entries: Asp, Excelsior, Ravager, Hornet, Skithist, Mastakith, Tarthist, Kragath, Rak'Kir, Rip'Pir, Zap'Pir, Zag'Gir.                               |
| [Rogue.veh][rogue-veh]                         | All record names and selected CSV rows inspected                                | Twelve hull records plus refineries, outposts, docks, satellites, manufactories and hazards; version `v53`. Full version-aware physics interpretation remains pending.     |
| [Rogue_mp.itm][rogue-itm]                      | Selected records parsed in memory; names and raw field positions inspected      | Version `v57`; Plasma Cannon, Plasma Bomb, MIRV, X-Laser/Laser Cannon, Disabler, minerals and equipment are present. Full catalog and item-logic resolution pending.       |
| [Rogue.lvl][rogue-lvl], [Rogue.lio][rogue-lio] | LVL metadata plus hash-only stream; LIO text equality comparison with fowlmouth | Map and level-object candidates; no visual inspection, collision extraction or object interpretation claimed.                                                              |

The archive snapshot does not establish an official distribution date for the whole package. Its help, patch notes, configs and assets may span different revisions. Classification can be VERIFIED for what a preserved file explicitly says or contains; the same fact is not automatically verified for every live RT version.

## Recovered qualitative mechanics

The following are summaries of [Rogue.nws][rogue-news], with section names as stable text locators. No screenshot/video measurements are involved.

| Section                      | Preserved-file evidence                                                                                  | Implementation consequence                                                                                                                                         |
| ---------------------------- | -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Game Overview                | Hull selection, slotted equipment; shield energy protects hull; combat produces wreckage.                | Retain equipment restrictions and distinguish shield drain from hull damage; verify whether a hit exhausting shields also damages hull before claiming parity.     |
| Trade System                 | Krandazite, Tellerium, Iron Ore and Wreckage; refining and trading; cargo mass affects handling.         | Prefer this recovered catalog for fidelity work over the master's explicitly NEW Ferrite/Copperite/Iridium proposal; reconcile the master before changing content. |
| Weapon Systems               | Beam, cannon and bomb momentum contributions differ.                                                     | Weighted inheritance is a future fidelity requirement; the prototype's boolean flag and exact additive formula remain NEW.                                         |
| Build System / Ship Upgrades | Trading-post construction; satellites, refineries and repair docks; engines, power, thrusters and armor. | Prepare content mapping for later master milestones; do not expand the first slice into the complete base/economy system.                                          |
| Bounty System / dated notes  | Combat payouts and death/resale rules change over time.                                                  | Select and document a target revision; reconcile help, config and item definitions instead of choosing one formula blindly.                                        |

The nickname Kran in COMMUNITY recollections corresponds to an archived resource named **Krandazite**. That name is now directly supported for this package. Ship hull and weapon ownership can be researched from actual logic strings rather than guessed from the generic publisher catalog.

Between-session progression remains mandatory under the master specification. A historical persistence implementation or exact save policy has not been proven from the files inspected so far.

## Raw recovered values, not prototype constants

These values are transcribed observations of the pinned archive. They are **not** the NEW/provisional values used by Rogue Trader 2.0. Their units, sentinel meanings and operational interpretation remain pending. No conversion to meters, seconds or prices should be made merely from a field name.

| Preserved source / locator | Raw recorded values                                                                               | Limitation                                                                                                        |
| -------------------------- | ------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `rogue.cfg` / `[Bounty]`   | `Start=1`; `PercentToKillerBounty=80`; `PercentToAssistBounty=20`; `FixedToKillerBounty=0`        | The percentage scale and reward execution path need validation; older news revisions differ.                      |
| `rogue.cfg` / `[Cash]`     | `ShareRadius=0`; `SharePercent=0`; `PercentOfKiller=100`; `PercentOfTarget=1000`; `KillReward=75` | Do not assume ordinary percent scaling or adopt the formula without the evaluator.                                |
| `rogue.cfg` / `[Soul]`     | `EnergyDefaultMax=1700000`; `EnergyDefaultRate=250`; `EnergyShieldMode=2`                         | Energy/time scaling and shield semantics need engine validation.                                                  |
| `rogue.cfg` / `[Vehicle]`  | `PhysicsDelay=400`; `InheritSpeedOnDefaultChange=1`                                               | PhysicsDelay is not proven to be a simulation timestep; default-change inheritance is not projectile inheritance. |

Selected projectile records were parsed with a CSV reader that honors quoted fields. [Projectile schema][projectile-doc] numbers elements from one; [the emulator parser][projectile-parser] indexes from zero. The candidate columns agree at `fireDelay[54]`, `inheritedSpeed[94]`, `muzzleVelocity[101]`, `aliveTime[110]`, `explodeItem[142]`; this establishes a mapping hypothesis to validate against legacy behavior.

| `Rogue_mp.itm` record | Version | Raw fireDelay | Raw inheritedSpeed | Raw muzzleVelocity | Raw aliveTime | Raw explodeItem |
| --------------------- | ------- | ------------: | -----------------: | -----------------: | ------------: | --------------: |
| Plasma Cannon         | v57     |            25 |               1000 |               2200 |           550 |               0 |
| Plasma Bomb           | v57     |           150 |               1000 |               2100 |          2000 |               0 |
| MIRV Missile Launcher | v57     |           400 |               -550 |               2750 |           -65 |            3008 |
| X-Laser               | v57     |           200 |               -550 |               4000 |           350 |               0 |
| Laser Cannon          | v57     |            30 |               -550 |               4000 |           550 |               0 |

Negative values in the missile/beam rows are a reason to investigate engine conventions, not evidence that inheritance is literally negative or absent. `explodeItem=3008` is a link to resolve before reconstructing MIRV submunitions. The [SpeedValues parser][speed-parser] itself contains a note about differing parse orders; vehicle terrain fields must be validated against the chosen `v53` file and not copied by column guesswork.

## Asset and format candidates

All links below identify files whose existence was confirmed in the complete tree. At the initial October12 inventory, blob records/appearance/palette/directions/frames were uninspected. Current selected11 decoding and original inspector acceptance are recorded above; native appearance remains open. Hash-only comparison established identical preserved `rogueships`, `RogueWeapons` and `cruiart` payloads in the later fowlmouth source; a filename or byte match does not establish visual content.

| Candidate purpose      | Preserved exact paths / immutable URLs                                                                                                                                   | Next fidelity work                                                                                  |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------- |
| RT ships               | [Orphaned Archive Files/blo/rogueships.blo][blob-rogueships]                                                                                                             | Index records, confirm IDs referenced by `Rogue.veh`, inspect directions and visual scale.          |
| Ship families          | [TerranShips.blo][blob-terran], [SkithzarShips.blo][blob-skithzar], [GraziShips.blo][blob-grazi] in the orphan blob folder; corresponding `*Small` variants also present | Resolve the actual target variant; preserve original scale/aspect rather than choosing by filename. |
| RT and faction weapons | [RogueWeapons.blo][blob-rogueweapons], [terranweapons.blo][blob-terranweapons], [Ebombs.blo][blob-ebombs]                                                                | Match projectile/effect IDs from the item definitions.                                              |
| UI and hull selection  | [cruiart.blo][blob-ui], [ShipSelectMov.blo][blob-select]                                                                                                                 | Match HUD/store/selection references; compare with the supplied video's narrow green panels.        |
| Economy objects        | [spacestations.blo][blob-stations], [Resources.blo][blob-resources]                                                                                                      | Inspect station/resource records and resolve `Rogue.veh`/ITM references.                            |
| Audio                  | [WeaponSounds.blo][blob-audio]                                                                                                                                           | Inventory-only; do not assume sprite blobs or audio redistribution permission.                      |
| RT map variants        | Package [Rogue.lvl][rogue-lvl]; orphan [roguetrader.lvl][map-lvl] and [roguetrader.lvb][map-lvb]                                                                         | Compare versions and recover collision/material metadata separately from presentation.              |

Relevant conversion/research tools include the [BlobEditor tree][blob-editor], [Gibbed.Infantry.FileFormats][file-formats], [Gibbed.Infantry.DecompileLVL][decompile-lvl] and [GenerateBLOTable][blob-table]. These are researched candidates, not tools already built or executed for Rogue Trader 2.0.

## Licensing findings and reuse boundary

GitHub API metadata reports `license: null` for all inspected repositories. Complete trees showed no root LICENSE/COPYING for the server, assets, tools or editor. Selected server/parser files had no GPL grant in their inspected headers; the [Parsers assembly metadata][parser-copyright] contains a 2010 copyright notice. **A GPL license for Infantry-Online-Server has not been established by this audit.** Third-party package licenses in its tree apply to their components and do not establish a repository-wide grant.

The Tools repository does contain an explicit [Gibbed notice][gibbed-license]: a zlib-style permissive grant with origin, altered-version and notice-preservation conditions. This is file/component-scoped evidence; it does not grant rights to every editor file or the original game artwork/audio.

The Zone-Assets README invites development use and explains preservation provenance, but the inspected materials do not establish original publisher ownership or a blanket original-asset redistribution grant. Public availability alone settles neither permission nor infringement. Before bundling preserved sprites/audio/maps, record the applicable rights/permissions for the exact files. Research, asset mapping and an original-code importer design can progress while that question remains documented.

## Completed history and active parity backlog

Pinned repository/schema/license inventory, canonical provenance, original raw/indexed and compiled-parser records, selected pixel decoder/inspector/export, full directory/candidate inventory, package/runtime audits and original arena/bounded host are completed scoped work. See the current checkpoint and [HANDOFF.md](HANDOFF.md); prior counts/access metadata remain historical evidence.

-   [ ] **Investigating — RT-F380/F384:** compatible native channel/profile, welcome Start/entry, original hull choice and unspectating; resume desktop work only when the user is available.
-   [ ] **Investigating — RT-F069/F387:** actual original graphics/audio/help/full-LVL delivery and remaining dependency/evaluator eligibility.
-   [ ] **Investigating — RT-F027–F041/F067:** native unit/sentinel/grammar execution; compiled parser success is narrower.
-   [ ] **M2 native acceptance/RT-F068 integration open:** original cargo/refining/trade/upgrades/actors/rewards and durable progression are not accepted in the native/M2 loop. The persistence core and reconstructed original-ID save/load fixture exist separately; standalone native hosting does not save.
-   [ ] **Native trading/join acceptance after RT-F388/RT-F389/RT-F390/RT-F396:** bounded sale/store/capacity/purchase guards have actual-handler evidence in [RuntimeChecks](../../scripts/compatibility/RuntimeChecks/IMPLEMENTATION_PLAN.md); connected behavior, durable transactions and production shutdown remain unresolved.
-   [ ] **Investigating — native presentation/release:** complete bank/blit/HUD/audio/controls and exact-file rights/provenance.

[asset-readme]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/README.md
[rogue-dir]: https://github.com/InfantryOnline/Zone-Assets/tree/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue
[rogue-cfg]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue/rogue.cfg
[rogue-news]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue/Rogue.nws
[rogue-keys]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue/keys.txt
[rogue-rules]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue/rules.txt
[rogue-rpg]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue/Rogue.rpg
[rogue-veh]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue/Rogue.veh
[rogue-itm]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue/Rogue_mp.itm
[rogue-lvl]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue/Rogue.lvl
[rogue-lio]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Incompatible%20Zone%20Archives/SilentClientCrash/rogue/Rogue.lio
[projectile-doc]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/Docs/Itm/Projectile.txt
[projectile-parser]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Itm/ItmInfo.Projectile.cs
[speed-parser]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/Parsers/Vehicles/SpeedValues.cs
[parser-copyright]: https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/Parsers/Properties/AssemblyInfo.cs
[blob-rogueships]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/rogueships.blo
[blob-terran]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/TerranShips.blo
[blob-skithzar]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/SkithzarShips.blo
[blob-grazi]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/GraziShips.blo
[blob-rogueweapons]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/RogueWeapons.blo
[blob-terranweapons]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/terranweapons.blo
[blob-ebombs]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/Ebombs.blo
[blob-ui]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/cruiart.blo
[blob-select]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/ShipSelectMov.blo
[blob-stations]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/spacestations.blo
[blob-resources]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/Resources.blo
[blob-audio]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/blo/WeaponSounds.blo
[map-lvl]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/lvl/roguetrader.lvl
[map-lvb]: https://github.com/InfantryOnline/Zone-Assets/blob/401633b9b6829d9e5ae56b76a2563b1b95bbe4b9/Orphaned%20Archive%20Files/lvb/roguetrader.lvb
[blob-editor]: https://github.com/InfantryOnline/Infantry-Online-Tools/tree/93001c57845269e87b32a2bf042257c7ef61298c/InfantryOnline.Tools/Tools.BlobEditor
[file-formats]: https://github.com/InfantryOnline/Infantry-Online-Tools/tree/93001c57845269e87b32a2bf042257c7ef61298c/InfantryOnline.Tools/Tools.External/Gibbed/Gibbed.Infantry.FileFormats
[decompile-lvl]: https://github.com/InfantryOnline/Infantry-Online-Tools/tree/93001c57845269e87b32a2bf042257c7ef61298c/InfantryOnline.Tools/Tools.External/Gibbed/Gibbed.Infantry.DecompileLVL
[blob-table]: https://github.com/InfantryOnline/Infantry-Online-Tools/tree/93001c57845269e87b32a2bf042257c7ef61298c/InfantryOnline.Tools/Tools.External/Gibbed/GenerateBLOTable
[gibbed-license]: https://github.com/InfantryOnline/Infantry-Online-Tools/blob/93001c57845269e87b32a2bf042257c7ef61298c/InfantryOnline.Tools/Tools.External/Gibbed/license.txt
