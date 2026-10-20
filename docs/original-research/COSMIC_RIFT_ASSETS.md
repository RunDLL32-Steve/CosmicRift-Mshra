# fowlmouth Cosmic Rift asset archive

## Current implementation checkpoint — October 13, 2026

[Master §18](../../ROGUE_TRADER_2_PLAN.md#18-development-roadmap) is the status authority; [CONTINUATION.md](../../CONTINUATION.md) records the current handoff. The observations in this October 13 checkpoint are historical evidence, not a fresh verification. Selected11 BLO archives/231 sprites/36,516 frames, canonical1999×1998 overview and source/updated standalone inspector are verified; native palette/blit/bank/HUD/audio/gameplay remain open. All89 canonical BLO directories/1,262 members and609 candidates are inventoried, with eight missing/unknown raw candidates; runtime closure is unverified. Five original help files are recovered separately from pinned Zone-Assets.

The package audit matches19,198 supported values/twelve hull links/222 map references; derived-only VehicleMaker fix restores eight field18 values. Original arena initializes25 computers/100 mineral drops/51teams/42hides; full-asset bounded host uses89 canonical BLOs plus authentic supplemental Soccer. Native test reached welcome only; Start produced no confirmed entry, private test/process cleanup is complete and desktop interaction stays stopped until the user is available. Standalone native mode does not persist progression. Generic handlers being present does not prove original runtime parity. Existing dated source-access observations below retain their historical scope; completed tasks are recorded in history/[CHANGELOG.md](../../CHANGELOG.md).

User-supplied source: [fowlmouth/cosmic-rift-assets](https://github.com/fowlmouth/cosmic-rift-assets). Reviewed: October 12, 2026. Master authority: [ROGUE_TRADER_2_PLAN.md](../../ROGUE_TRADER_2_PLAN.md), §1.2 exact-fidelity steering, §2, §§5–8, §16 and §18. Tracking: [research subplan](IMPLEMENTATION_PLAN.md) and [development progress](../development-log/PROGRESS.md).

The user has designated this repository as the **correct canonical foundation** and requested a **100% Rogue Trader reproduction**, using its original content **without altering the original files**. Its repository description attributes the SOE zone assets to Military Police's preservation contribution. Actual files corroborate the Rogue Trader package already found through [InfantryOnline](INFANTRYONLINE.md); the matching copies are not independent confirmation of every original rule or a second historical version.

[ASSET_MANIFEST.json](ASSET_MANIFEST.json) is the canonical asset manifest: exact pinned source, foundation flag, preserve-original-bytes policy, original paths/types, raw text SHA-256, Git blob identities, LFS payload OIDs/sizes, candidate RT dependencies, InfantryOnline counterparts and the user-designated map URL. Original content remains unchanged; any future decoded exports/adapters belong in separate derived locations with provenance.

## Provenance, formats and access

| Item                    | Observed result                                                                                                                                                                                                                                                                                                                                                                                                                         |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Pinned commit           | [eb1514eaae7f9b9425307d9cc043c74ecdd15e35](https://github.com/fowlmouth/cosmic-rift-assets/tree/eb1514eaae7f9b9425307d9cc043c74ecdd15e35), November 9, 2023, 17:20:19 UTC                                                                                                                                                                                                                                                               |
| Branch                  | `main`; commit, not the moving branch, is the research reference                                                                                                                                                                                                                                                                                                                                                                        |
| Tree access             | Complete recursive GitHub tree read; `truncated=false`                                                                                                                                                                                                                                                                                                                                                                                  |
| Inventory               | 327 files: 89 BLO, 53 BMP, 58 LVL, 31 CFG, 12 ITM, 36 LIO, 19 NWS, 12 RPG, 16 VEH, plus `.gitattributes`                                                                                                                                                                                                                                                                                                                                |
| README / notices        | No README, LICENSE, COPYING or NOTICE path found anywhere in the inspected complete tree                                                                                                                                                                                                                                                                                                                                                |
| GitHub license metadata | `null`; no positive redistribution grant established                                                                                                                                                                                                                                                                                                                                                                                    |
| LFS configuration       | [.gitattributes][attributes] explicitly applies Git LFS to `*.blo`, `*.lvl`, `*.bmp`                                                                                                                                                                                                                                                                                                                                                    |
| Standard sprite images  | No PNG, SVG or JPG files in the tree                                                                                                                                                                                                                                                                                                                                                                                                    |
| BMP images              | `LVL-BMP/` contains map-preview candidates; [Rogue.bmp][rogue-bmp] points to an 11,988,054-byte payload now retrieved and viewed by the lead agent; collision/object semantics are not decoded from the image                                                                                                                                                                                                                           |
| Binary access           | Selected pointer texts read; four overlapping InfantryOnline payloads streamed in memory solely for SHA-256 comparison. Lead agent later fetched all 89 BLO payloads plus the RT LVL/LIO/BMP in the pinned working reference checkout; source Git status remained clean. Current selected11 BLO batch is decoded/visually inspected; all89 archive directories are inventoried. Native appearance and full runtime closure remain open. |
| Working reference       | `.tools/references/cosmic-rift-assets`, pinned at the recorded commit; originals unchanged and kept separately from the prototype's bundled assets                                                                                                                                                                                                                                                                                      |

Raw Git URLs for these LFS-tracked assets return short pointer files. A checkout without LFS payload resolution is not an asset pack. BLO files require a format decoder/record extractor before they become Godot textures or audio. The RT BMP payload is now retrieved and viewed; it is a standard map image, and does not supply gameplay collision/object data by itself.

## Rogue Trader definitions and overlap

The following text files were read from both repositories in memory and compared after replacing CRLF with LF and removing any leading byte-order mark. All six comparisons matched exactly. The differing Git blob hashes/sizes in these text copies reflect newline encoding; they do not establish a gameplay revision difference.

| This archive                  | InfantryOnline counterpart at commit `401633b9…`               | Comparison result                                               |
| ----------------------------- | -------------------------------------------------------------- | --------------------------------------------------------------- |
| [CFG/rogue.cfg][rogue-cfg]    | `Incompatible Zone Archives/SilentClientCrash/rogue/rogue.cfg` | Equal after newline normalization                               |
| [NWS/Rogue.nws][rogue-news]   | Same RT folder / `Rogue.nws`                                   | Equal after newline normalization                               |
| [RPG/Rogue.rpg][rogue-rpg]    | Same RT folder / `Rogue.rpg`                                   | Equal after newline normalization                               |
| [VEH/Rogue.veh][rogue-veh]    | Same RT folder / `Rogue.veh`                                   | Equal after newline normalization                               |
| [ITM/Rogue_mp.itm][rogue-itm] | Same RT folder / `Rogue_mp.itm`                                | Equal after newline normalization                               |
| [LIO/Rogue.lio][rogue-lio]    | Same RT folder / `Rogue.lio`                                   | Equal after newline normalization; object semantics not decoded |

[ITM/Rogue.itm][rogue-alternative-itm] is also present. It is a separate candidate whose differences from `Rogue_mp.itm` have not yet been analyzed. Do not silently substitute it for the item file selected by `CFG/rogue.cfg`.

The recovered mechanics and raw field values in [INFANTRYONLINE.md](INFANTRYONLINE.md) therefore apply to the matching files here with the same snapshot/version limitations. Configuration, news and item/vehicle schemas still need reconciliation with original engine evaluation.

## Payload identity checks

The Git LFS `oid sha256` in each fowlmouth pointer was compared with SHA-256 computed from the corresponding InfantryOnline binary stream. All four checks matched; that comparison saved or imported no binaries. The lead agent's subsequent pinned working-reference fetch is recorded separately above.

| fowlmouth path                  | InfantryOnline path                                            | SHA-256 / LFS OID                                                  | Result                             |
| ------------------------------- | -------------------------------------------------------------- | ------------------------------------------------------------------ | ---------------------------------- |
| [BLO/rogueships.blo][ships]     | `Orphaned Archive Files/blo/rogueships.blo`                    | `9dae17f9e2e989992579f3048081a03311509424b5a4e7273fb639d53a76c6be` | Identical payload; 3,974,968 bytes |
| [BLO/rogueweapons.blo][weapons] | `Orphaned Archive Files/blo/RogueWeapons.blo`                  | `b84037b07cf40d9ae1a30dd11ab195423ca6845811a36bcb2cecee34bdb29fd7` | Identical payload; 877,991 bytes   |
| [BLO/cruiart.blo][ui]           | `Orphaned Archive Files/blo/cruiart.blo`                       | `eaefbbbbc065a38811ae1431b734099dce414f9e69a18dbd8d5063a75cf4179e` | Identical payload; 686,200 bytes   |
| [LVL/Rogue.lvl][rogue-level]    | `Incompatible Zone Archives/SilentClientCrash/rogue/Rogue.lvl` | `7034476858b86e9b11a41eaaa0d636003eb432a4b682e45d264def8b425cf02f` | Identical payload; 1,290,994 bytes |

The [research inventory](SOURCE_MANIFEST.json) preserves the earlier exact commit paths/Git blob identities; the [canonical manifest](ASSET_MANIFEST.json) adds raw text-byte hashes and LFS payload identities. Git blob SHA-1 of an LFS pointer is not the SHA-256 identity of its payload. The canonical source snapshot has been chosen; operating units, sentinels and historical version semantics still require validation.

## User-designated Rogue Trader map view

Supplied URL: [Rogue Trader map view](https://media.githubusercontent.com/media/fowlmouth/cosmic-rift-assets/refs/heads/main/LVL-BMP/Rogue.bmp). Immutable reference: [Rogue.bmp at the canonical commit](https://media.githubusercontent.com/media/fowlmouth/cosmic-rift-assets/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/LVL-BMP/Rogue.bmp). The payload hash is `605488d681fec3069d4b26863669072df2a44fd08d055f640cfcde9607ad2722`.

The lead agent inspected the retrieved map view and reported its original dimensions as 1999 × 1998. It shows a black near-square map with broad curving asteroid belts, including gold along the top, green along the left and pink near the bottom, structure clusters centrally and toward corners, and colored square markers. Those are visible observations only; their labels, function, coordinates and navigation/collision semantics have not been established from the image alone. Use the LVL/LIO sources for playable geometry/object reconstruction.

## Fidelity implementation implications

This source is the foundation for the requested original RT hull/weapon/UI/map content. Candidate [TerranShips.blo][terran], [SkithzarShips.blo][skithzar], [GraziShips.blo][grazi], and their small variants, plus [O_Asteroids.blo][asteroids], are present. Canonical RT text currently references the `*Small` ship variants; `rogueships.blo` is present but not yet proven to be that config's active dependency. Selected11 contents/palette/frame metadata are decoded and inspected; all89 directories are inventoried. Exact runtime eligibility, native blitting and full dependency closure remain open. The manifest's blob candidates come from a case-insensitive filename-stem scan of canonical text; that is a preparatory dependency inventory, not a completed record decoder.

Use the recovered RT configuration as the dependency root: decode the versioned VEH/ITM/RPG/LIO records, resolve their blob/object IDs, validate map collision/material data, and map original assets to rendering without changing flight authority. Compare UI layout, world scale, ship directions and effects against [the limited video reference and parity checklist](VIDEO_REFERENCE.md). Retain the existing NEW prototype values until a validated target-version mapping can replace them.

No inspected file establishes a blanket original-art/audio/map redistribution grant. The manifest preserves that provenance field separately from the user's canonical-foundation decision; it is not a finding that the files are unlawful. Immutable working-reference use, decoder design and source reconciliation proceed under the user's instruction, with exact-file rights/notices retained as a release provenance task.

## Completed history and active follow-up

Canonical selection, pinned provenance, six text/four binary comparisons, original payload acquisition, BMP inspection, selected importer/staging/source/standalone display, full directory inventory and bounded package/arena audits are complete within the scopes linked above. Source hashes remain unchanged. The reference is available locally; completed availability/decoding tasks are removed from active work.

-   [ ] **Investigating — RT-F069/F377–F379:** remaining raw/runtime reference eligibility, native audio/effects and actual delivery.
-   [ ] **Investigating — RT-F021/F022/F387:** full raw LVL native delivery, graphical trailer, world/grid/collision and object meaning; parsed tile layers/222 map links are already audited.
-   [ ] **Investigating — RT-F023–F026/F074/F075:** original native appearance, pose, control/HUD/menu semantics.
-   [ ] **Not started — canonical per-record queues:** validate original ship/item/actor/resource interactions and durable progression through the native engine route.
-   [ ] **Investigating — release provenance:** exact-file rights/notices and optional alternative Rogue.itm revision evidence; keep Rogue_mp.itm selected by canonical CFG unchanged.

[attributes]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/.gitattributes
[rogue-cfg]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/CFG/rogue.cfg
[rogue-news]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/NWS/Rogue.nws
[rogue-rpg]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/RPG/Rogue.rpg
[rogue-veh]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/VEH/Rogue.veh
[rogue-itm]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/ITM/Rogue_mp.itm
[rogue-alternative-itm]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/ITM/Rogue.itm
[rogue-lio]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/LIO/Rogue.lio
[rogue-level]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/LVL/Rogue.lvl
[rogue-bmp]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/LVL-BMP/Rogue.bmp
[ships]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/BLO/rogueships.blo
[weapons]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/BLO/rogueweapons.blo
[ui]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/BLO/cruiart.blo
[terran]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/BLO/TerranShips.blo
[skithzar]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/BLO/SkithzarShips.blo
[grazi]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/BLO/GraziShips.blo
[asteroids]: https://github.com/fowlmouth/cosmic-rift-assets/blob/eb1514eaae7f9b9425307d9cc043c74ecdd15e35/BLO/O_Asteroids.blo
