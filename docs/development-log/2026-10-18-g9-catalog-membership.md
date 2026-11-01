# G9 actual catalog membership — October 18, 2026 (Chicago)

UTC readiness checkpoint October19. Branch `claim/g9-dependency-checkpoint`, base main `3a205e2c4f75984e6eddac79aacaf1fd48cb666e`. [Master §18](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype), [G9 ledger](../../scripts/original-assets/plan.d/g9.md), [ArenaProbe contract](../../scripts/compatibility/ArenaProbe/plan.d/g9.md). Root approved this bounded existing-probe implementation and ignored prerequisite copy; coordinating `gpt-6.1-sol` / XHigh designed uncertain source semantics, original author `gpt-6.1-sol` / Medium implements. Status: implemented and locally verified after coordinator/root actual code/setup inspection and explicit first-run clearance. The first author revision and the round1 rework revision each ran the unchanged wrapper once after explicit clearance; no prepare or unrelated suite ran. Independent reproduction remains reviewer-owned.

## Source rationale and implementation boundary

Historical ArenaProbe accepted counts; Maker diagnostics injected canonical maps. Neither established full actual initialized catalog membership. This increment inspects actual server.\_assets after successful standalone server.init, before newArena: derive selected configured inputs and raw expected IDs with existing CSVReader, compare complete sorted200 expected/actual arrays, reject duplicates and check every actual ID lookup is the same object with matching ID. One positive66 lookup must return null with exactly its expected Warning, separated by actual TLog from unexpected Error/Exception. No second ItemInfo.Load, map injection, copied parser or synthetic66. The raw ID-column2/type18 assumptions come from inspected pinned upstream loader source (VERIFIED STATIC); they are not independently confirmed original/native semantics.

Source-only rationale at server pin `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849`: AssetManager.LoadItemFiles556–592 scans MAIN nested records, appends direct child data586–587 and removes Nested590, a one-hop record expansion. AssetFileFactory separately recurses directories. Missing direct child logs a skip, but Factory61 records missing files and AssetManager194–208 rejects retained IsIncomplete; no optional-success claim. ItemFile.ReadFile28–32 delegates ItemInfo.Load; NestedItem parses filename19. ItmInfo112–116 records FilesToLoad only; coordinator's exact pinned dotnetcore tracked-C# search found declaration10/add115 and no dispatcher. No runtime-tested general nested behavior is claimed.

| Newly inspected pinned Git blob under dotnetcore/ | LF bytes | SHA256                                                             |
| ------------------------------------------------- | -------: | ------------------------------------------------------------------ |
| Assets/Itm/ItemFile.cs                            |     1038 | `d680ea0eaa8e6d7b13f9529814c0bef0157faaf76ac26a053f3e68fffcb8f29c` |
| Assets/Itm/ItmInfo.NestedItem.cs                  |      727 | `a6f5358af04daf92244a59fc8343f81e7767f38138231e11781742ddc32cfae8` |

Coordinator verified working CRLF→LF bytes equal pinned Git blobs; table hashes are pinned LF blob bytes, not raw working hashes. Accepted AssetManager/Factory/ItmInfo context is reused without old whole-hash/search/inventory or Maker/Ball/Portal/Uiart reruns. Canonical pin remains `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`.

## Approved setup readiness

Copied only12 assets: comms.txt,infcmdlst.txt,keys.txt,mods.txt,rules.txt,Rogue_mp.itm,rogue.cfg,Rogue.lio,Rogue.lvl,Rogue.nws,Rogue.rpg,Rogue.veh, plus server.xml/scripts.xml/loopback-allowlist.txt. Destination is new regular issue `.tools/compatibility/working-zones/rogue-pristine`; source is approved root pristine zone. Seven canonical hashes checked against manifest, five authentic help hashes against accepted provenance (Zone-Assets401633...), and all source/destination bytes match. No private/auth/profile material copied; keys.txt is original help.

Config source identities: server.xml1127 bytes/SHA256 `796d88a54cb3066e901621f771a9e63c92796dd3e1877f0914b40c426daf2b99`; scripts.xml24/`592ceec642c18829bd36307c2ee1cbe636e05630af8efd7c467bed12bd338390`; allowlist11/`b3d01140f3d4dbf553d900894d8ccd86887d6e0f3f5d23bd31932eabf9f403aa`. Source XML checked: only assets path,127.0.0.1 bind/allowlist, database delay0/no database2, empty gameType/news/copyServer, false blobs/pathfinding, scripts has zero entries. Initial inline copy-readiness assertion mistakenly counted the outer XML wrapper as a script entry; corrected to scripts child entries before any copy, without changing inputs.

Ignored issue `artifacts/g9-catalog-membership/setup.json` records all15 input size/hash/equality and planned output paths. Active arena-working/Global/reports/logs, ArenaProbe bin/obj, derived project outputs, copied NuGet cache and CLIhome resolve within issue root. Only SDK10.0.401 and two reference junctions point at shared read inputs. All seven existing derived patch hashes match issue manifest and global pin is10.0.401/rollForward disable. No whole.tools junction, shared mutable outputs, move/delete or prepare operation.

## Execution contract and status

After explicit first-run clearance, unchanged `run-arena-probe.ps1 -Build` builds/runs one hidden offline helper bounded30 seconds. Full verbose output/log/report must be retained; report actual exit/Accepted, full expected/actual IDs, duplicate/reference checks,66warning, identity digests, input/source and loaded-assembly identities plus existing initialized-state/zero-client/begin/poll/physics limits. Digest algorithm: lowercase SHA256 over UTF8 without BOM, invariant decimal sorted IDs joined LF WITH trailing LF. Preserve author report/result/logs under distinct ignored issue evidence before reviewer reproduction; independently rebuilt assembly hashes need not be byte-identical.

Selected CFG/ITM hashes/lengths are hard guards: CFG48399/`82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90`; ITM111614/`dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7`. Actual configured names and actual AssetFileFactory resolution must remain within staged assets. Actual loaded assembly Locations and hashes plus final Program/helper source hashes are reported, not guessed output files.

## Original author run — preserved history

Coordinator/root inspected actual helper/Program, setup inputs/output ownership and cleared first execution. Existing `run-arena-probe.ps1 -Build` ran once in Windows PowerShell, fully redirected to issue-owned wrapper.log. Build log records1487 warnings/0 errors; no warning-free build claim. Captured helper stderr is empty. Wrapper exit0 and controlled helper exit0; top-level Accepted=true and CatalogMembership.Accepted=true. Full expected/actual arrays each contain200 sorted IDs and agree exactly; duplicate expected/actual and lookup mismatch arrays are empty, lookup reference consistency true. Both identity digests are `3e4b98a8f8a9afd4f5b55eb121550be978219002e1cfed3ab2b9bd7e1721f231` under the recorded invariant UTF8/LF/trailing-LF algorithm. Actual positive66 returns null with exactly one `Unable to get item by ID: 66` Warning; zero unexpected catalog/initialization errors.

Standalone actual initialization retains one arena, zero clients/begin/poll/physics ticks,30 vehicle definitions/12 hull links,51 teams,25 initial vehicles,100 initial item drops,42 hides and zero doors/flags; loadBlobs=false and null script binding remain explicit. It proves only this selected initialized catalog and bounded initial state, not arbitrary/live/custom/native catalogs or original gameplay.

All15 approved source/pristine/active copied input hashes were checked unchanged after the run. Actual server config selects rogue.cfg and loaded level.itmFile selects Rogue_mp.itm; actual search paths contain only issue-owned staged assets. Report preserves selected configured names/resolved paths/raw hashes for server.xml/CFG/ITM/scripts.xml/allowlist. No second ItemInfo.Load/injection or synthetic66; known canonical expected IDs were derived during the run rather than hardcoded from an external digest.

Complete author report/result/wrapper/build/stdout/stderr logs and source/loaded DLL byte copies are separately preserved at ignored issue `artifacts/g9-catalog-membership/author/` before reviewer reproduction overwrites wrapper defaults. Author report `original-arena-probe.json` SHA256 `8532c3187e0ce7ae12b336cd1afbfd4f7da5540479b1ef54d877a815c55b0ee2`. Independently rebuilt assembly hashes may differ; no byte-identical-build claim.

| First author loaded assembly (Location recorded in original report) | SHA256                                                             |
| ------------------------------------------------------------------- | ------------------------------------------------------------------ |
| Runner                                                              | `8b85b3ae913ae57b58a841d1963d384c382a4f5ffe132e0b341d1ceb577eb33e` |
| Server                                                              | `9aaf8674216586e2e3b9e0c0ce9bcf4919a87e56d0fb2b327fc44f735b837404` |
| Assets                                                              | `5aa1a57d5db2e5d4de5a286b9c0ed22878079812aa9d7f6529c43ab850254d93` |

| First author reported source | SHA256                                                             |
| ---------------------------- | ------------------------------------------------------------------ |
| Program.cs                   | `616fca44b511f14a2a83302002fe132027ca6a91f57e689df32d8bd124e78d73` |
| CatalogMembershipChecks.cs   | `dcb256dcbb8a3e3b53eb75b1e3a65db95c4fa06fcda46c5a018e9291aa8f007a` |

These first-run source and loaded assembly hashes identify the preserved original author revision, not the later round1 rework revision. The original author evidence directory is retained untouched; the separate round1 run below establishes the reworked source identities.

G9/RT-F069/F386/F377/F378/F379 remain open,397/22/375 and raw609/eight residuals unchanged. This is selected standalone initializer evidence only; no arbitrary/live/custom/native catalog closure, original gameplay or production rule acceptance. No baseline/Maker/RuntimeChecks/native/login/Godot/flight/simulation/inventory suites. Markdown tracker/link/whitespace QA is recorded in Final documentation QA below; old dated logs remain unchanged.

## Author rework round 1 — new source revision

Actual senior review5566540965 and all inline findings were read in full. R1-2 adds comment-only authority traces to ASSET_MANIFEST assets entries CFG/rogue.cfg and ITM/Rogue_mp.itm (originalSizeBytes/originalSha256), preserving guard values/logic and no new dependency. R1-3 labels ID-column2/type18 assumptions as inspected pinned upstream static-source facts, not independently confirmed original/native semantics, in code and this log. R1-4 compares the two fully qualified Windows paths after TrimEndingDirectorySeparator using OrdinalIgnoreCase, preserving path ownership/reparse guards. R1-5 only re-indents the existing try body; optional new failure behavior was not added. R1-6 explicitly dates the continuation's no-gameplay-rerun sentence to the October17 documentation audit.

Coordinator/root inspected the narrow actual fix diff and explicitly cleared one serialized rework `run-arena-probe.ps1 -Build`; reviewer2 held execution throughout. Wrapper/control exit0; top/catalog Accepted=true. Full200 expected/actual IDs match, no duplicate or lookup mismatch IDs, reference consistency true; both identity digests remain `3e4b98a8f8a9afd4f5b55eb121550be978219002e1cfed3ab2b9bd7e1721f231`. One positive66 lookup is null with exactly one expected Warning, no unexpected errors, clients/begin/poll/physics0. All15 root-source/pristine/active input hashes remain unchanged. Build1487 warnings/0 errors; no warning-free claim.

Complete rework report/control/wrapper/build/stdout/stderr and source/loaded-DLL copies are separate at ignored `artifacts/g9-catalog-membership/author-rework-round1/`; report SHA256 `c9f6eff26ac5fc555ce7c1b087c7ca2546ddae4bc955707220b6d0f6d4c8efa1`. The original `author/` directory/report8532c318... remains unchanged. These are two author runs of two source revisions, not independent reviewer verification. R1-1 remains outstanding until the fresh reviewer reproduces the final head and issues its own verdict.

| Rework actual loaded assembly | SHA256                                                             |
| ----------------------------- | ------------------------------------------------------------------ |
| Runner                        | `7aa2382aace483b01a731d12a88a860de5cd9fd724297ed74ab349ce0c0f46ba` |
| Server                        | `75037a4816abc8f07f41f12dfac29c80a99101f54a8bb40db0bb7c8b385944f3` |
| Assets                        | `9b2fed7fa7376f88c0357719928e92e38b63450232aaf0176be7d3eef5c82f80` |

| Final reworked source      | SHA256                                                             |
| -------------------------- | ------------------------------------------------------------------ |
| Program.cs                 | `77867f51bbf8852995b417a3fed55b9372696d6f89fc973a90ccfee8b729db18` |
| CatalogMembershipChecks.cs | `15e9a24d31e4f4f53ae4a2ed9853b83fa362ea532ed07a324d544e443c5bc50a` |

These final source and loaded-assembly hashes match current/preserved rework bytes; earlier table hashes label only the original revision. No C# changes followed this run. No baseline/other probe/RuntimeChecks/Maker/native suite ran. Fresh independent reproduction/CI/approval remains required.

## Final documentation QA

`TRACKERS_CONSISTENT`:397 entries/22 complete/375 open. Scoped local-link validation passed 24 added/new occurrences and fragments (new Markdown in full plus added lines elsewhere; not deduplicated); `git diff --check` passed. Original and rework source identities are preserved in their respective reports; current C# matches the rework report. Round1 rework QA passed `TRACKERS_CONSISTENT`397/22/375, 26 full-PR added/new local-link occurrences/fragments and `git diff --check`; current C# matches the separate rework report and the original report8532c318... remains unchanged. R1-1 fresh independent reproduction, exact-head review and CI remain required in [PR76](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/76); author never merges.
