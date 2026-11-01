# G9 maker creation-source and diagnostic evidence — October 18, 2026

Authority: [master §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). Owner: existing `claim/g9-dependency-checkpoint`. Prior [PR62](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/62) merged `763a1395d9f3850c8c4cd17ce2c5e333ebc3928c` after independent exact-head review `93def5e`; its historical evidence is unchanged. This new code/source slice needs a fresh PR and independent review. [Approved nine-case contract](../../scripts/compatibility/RuntimeChecks/plan.d/g9.md); [persistent G9 ledger](../../scripts/original-assets/plan.d/g9.md).

## Verified static canonical facts

Coordinator physically re-read ITM and VEH bytes, matching manifest hashes at canonical pin `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`; no whole-archive inventory rerun. ITM/Rogue_mp.itm: 111614 bytes, SHA256 `dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7`. VEH/Rogue.veh:41075 bytes, SHA256 `9eefa85a04587867616be786fffc664f3ebc0fb1d50ba6f6fd303a7a8a988572`. CFG recorded pin/hash context:48399 bytes, SHA256 `82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90`; CFG was source-inspected, not freshly claimed as part of the ITM/VEH byte reread.

| ITM row / type / version / ID / exact name | Raw maker fields                                                                        | VEH row / type / version / ID / exact name |
| ------------------------------------------ | --------------------------------------------------------------------------------------- | ------------------------------------------ |
| 196 /7 /v57 /83 /Missile Manufactory       | f31=83,f32=1,f34=2023,f35=1,f85=0,f93=425; f5 empty                                     | 15 /5 /v53 /425 /Manufactory - Missile     |
| 197 /7 /v57 /84 /Munitions Manufactory     | f31=84,f32=1,f34=2023,f35=1,f85=0,f93=426; f5=`!1&!2&!3&!4&!5&!6&!8&!9&!10&!11&!15&!16` | 16 /5 /v53 /426 /Manufactory - Munitions   |
| 198 /7 /v57 /85 /Defense Manufactory       | f31=85,f32=1,f34=2023,f35=1,f85=0,f93=427; f5 empty                                     | 17 /5 /v53 /427 /Manufactory - Defense     |

ITM row199 ID2023 is exact `Nanobot Control System`. Each target VEH f68/f69 is positive66/quantity1, not an invented sentinel/default. Hitpoints250, six inventory zeros, eight child zeros, Destroyable1, DensityRadius3500, MaxTypeByPlayer-1, FrequencyMaxActive-1, FrequencyMaxType1, FrequencyDensityMaxActive-1, FrequencyDensityMaxType1 and RemoveGlobalTimer0 are corroborated by existing pristine exports read as historical evidence. Missing canonical item66 remains literal; original intent/dynamic native evaluation UNKNOWN.

## Verified static source chain

Server pin `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849`. All new inspected working files equal pinned Git blobs after CRLF→LF normalization; lengths/hashes below identify pinned Git blob bytes, not raw working bytes. Earlier nine source hashes and exact overload/death guard facts remain in the G9 ledger.

-   ItmInfo.VehicleMaker.cs63–139 parses raw f31–35/f85/f93; getAmmoType143–158 returns self-ammo/one.
-   Update/Game.cs347–415 actual Handle_CS_PlayerUseItem checks arena/item/spectator/coordinates within200/SkillCheck, then queues Action<Arena>; maker case392–394 calls actual make handler. Assets.Skills.cs25–42/141–206 resolves base class, skill membership and negation/AND. No requiredItemID2023 check is added or inferred from these routes.
-   ScriptArena.cs2118–2237 performs vehicle lookup, computer density/player/team limits, reload enqueue before cash/ammo guards, actual ammo removal/cash decrement/inventory sync, then Player.MakeVehicle event and newVehicle2235. A veto can follow consumption: source ordering, not accepted original transaction policy.
-   ArenaState.cs477–631's four-argument route has null setup/class type, creates actual Computer, no children for these eight-zero definitions, then Vehicle.Creation callback. ZoneArenas.cs145–200 selects registered game type or null; historical initializer used null. Events.cs21–43 initializes dictionary/optional attributed handlers; Scripts.cs76–126 handles registry/instance callbacks. Arbitrary scripts/custom sessions are not proven closed.
-   Selected CFG row7 is Rogue_mp.itm. Canonical ITM types1,4,6,7,8,13,15,16,17 exclude Nested18; VEH types2,4,5 exclude Nested6. AssetManager.cs111–118/556–592 selected item mapping/nested loading therefore has no nested item record for exactly these pinned inputs. This conditional input closure is not arbitrary live catalog closure. Missing lookup66 returns null at305–324.
-   Alternate ArenaComputer.cs340–372 product creation would require negative product targets -425/-426/-427; no such literal appears in canonical VEH fields and historical parsed Products have none. Other/custom/script sessions are deferred.
-   Actual Arena constructor428–522 creates trackers/queue/map state; handleEvent534–537 queues work. poll drains it but also advances unrelated timers/state, so the diagnostic never calls poll. It manually drains one actual action. Parsed maps injected into AssetManager are NEW diagnostic composition, not actual server initialization proof.

| New inspected file under dotnetcore/      | Pinned bytes / SHA256                                                      |
| ----------------------------------------- | -------------------------------------------------------------------------- |
| Assets/Itm/ItmInfo.VehicleMaker.cs        | 8337 / `8231b532b17877c66276dd3c830f2ebeadccb64316e44d9237090b0e7c4ec539`  |
| Assets/Itm/ItmInfo.Item.cs                | 3279 / `aeef305aaae352949d617d2c559da22b7ab2a09f5222e55b1eeda0bf2a045e0f`  |
| ZoneServer/Logic/Packets/Update/Game.cs   | 22276 / `5b21fde4f2c254e20536aa10b6c2ca6462a2c881166a680c71477e909e5a400d` |
| ZoneServer/Logic/General/Assets.Skills.cs | 8815 / `5c8f8f6677eaa616a70a87b489d7af9332d0d15d8a6a9c6e81006a442aa06ff3`  |
| ZoneServer/Game/ZoneArenas.cs             | 12216 / `7419bb46b49636c96a66ad877b057cb3dab569c6d20da3db5fd829cfd277d059` |
| ZoneServer/Scripts/Events/Events.cs       | 5683 / `def77a56eac4340705b65dbdccd50a661d9a57aa190f2a203447d15379d87ea5`  |
| ZoneServer/Scripts/Scripts.cs             | 12135 / `8396855e5cb064767232aea810c86a5680722d55b1f730790ab1ad15ca213694` |
| ZoneServer/Game/Arena/ArenaComputer.cs    | 17954 / `317f9d8bb16a9a1fce398c763034f5b3c9e3e7859dfb346dfad86ee33b92bcac` |
| ZoneServer/Game/Arena/Arena.cs            | 52204 / `42b85ca1882deb9f892721f43e7c92f41de226bc92fc98474ea6f535e41b2721` |
| Assets/Itm/ItmInfo.Projectile.cs          | 17120 / `5059a0918025a1f6441521d5357b618034e551d7844424a0035067585453fd60` |
| Assets/Shared/ArmorValues.cs              | 493 / `56a158729982ca19e051eba4fd07e8292ab3e2bc6f2b57ee9050b4843f7bffb0`   |
| ZoneServer/Protocol/Helpers/Assets.cs     | 1164 / `62f0f1a3d023d848bca6a8968557c23cf9e617942a71359773cf825ae30975fb`  |
| ZoneServer/Game/Objects/Player.cs         | 60922 / `ce136bec28c3b7b4e4caba1841fc4a126d6822940a8f7ed2074e2286342edc23` |

ChunkA1002 row30/type6/v57 raw f116=0,f151–153=16/5000/3000,f154–170=0,f171=56. Projectile parser226/256–258/276 preserves these. ArmorValues.Parse order is SelfIgnore/PassIgnore/SelfReduction/Pass; VEH f9–32 holds six groups, kinetic SelfIgnore3000/SelfReduction0 unchanged. At center, helper clamps radius1, gross4875 minus3000 =1875; /1000 rounds2, so NEW runtime health1 dies. Canonical250 balance is not altered. The code asserts raw and parsed prerequisites before executing that boundary. Explosion reaches inherited TWO-argument kill/generic drop handler before Computer's later ONE-argument Destroyable-gated destroy; direct ONE-argument control is separate. Existing ledger details conditional guards and deferred callbacks.

## Implementation and execution status

Root approval October18 covers isolated local diagnostic implementation/setup under the nine-case contract, with no additional user permission required for that reversible scope. MakerChecks.cs and minimal Program.cs report integration use actual handlers and log/packet/state observations; no copied decision logic or production fix. Completed focused verification: `prepare-derived-source.ps1` exit0, then final `run-runtime-checks.ps1 -Build` exit0. Actual JSON `Accepted=true`, `CheckCount=70`, `FailureCount=0`: existing61 plus nine Maker cases. The unchanged wrapper also verified46 canonical foundation hashes; no all89 inventory rerun occurred. Report: issue-worktree ignored `artifacts/rogue-runtime-checks/fixed.json`; logs/build: `.tools/compatibility/runtime-checks/fixed*`. These ignored outputs are local evidence, not tracked deliverables.

Positive83/84/85 each observed actual Computer425/426/427, one ammo consumed, Nanobot2023 retained, cash0 unchanged, reload/inventory serialized, make/creation/death hooks1 each, Destroyed1, health0/condemned and zero drops. Actual lookup logged missing66 once each. NoAmmo83 observed reload-only/no make; MakeVehicle veto consumed before callback and serialized but created no vehicle. Skill84 packet rejection preceded queue/ammo; direct make from that state created/consumed. Death veto still destroyed with no missing66 warning; direct one-argument kill had no generic death hook. All condemned objects remain in no-poll trackers. Log event subscriptions classify actual TLog because upstream sinkMessage cascades warning to lower event names; expected warnings/security are distinct from error/exception capture. No unexpected error/exception was logged.

Initial author-side fixture failures were corrected: Program BOM encoding first caused compile failure; first actual run produced70 checks/6 failed creation-dependent cases because uninitialized static Arena.maxVehicles=0 rejected creation. NEW diagnostic capacity10 now restores its prior value together with Helpers.\_server and AssetManager singleton on constructor failure and Dispose. The first70/6 JSON was overwritten by the wrapper before a preservation request arrived; recorded tool output establishes that observation, and no historical report was recreated. A follow-up compile failure from qualifying the static member through the Arena instance was corrected. After a passing70/0 run, log classification was improved and the final source was rebuilt/rerun70/0. No original data or production guard/factory/decision logic changed.

Source-only, handler execution and native/original acceptance remain separate. No socket begin/poll/physics ticks/native clients; actual explosion radius/damage math did execute at NEW runtime health1. Root-approved isolation uses only three SDK/reference read-input junctions; active build outputs, own copied NuGet cache/CLIhome/logs/reports live within this issue worktree. Pinned archive historical top-level binaries/old obj files were never executed. Root inspected isolation; independent exact-head review remains required. G9/RT-F069/RT-F386 remain open,397/22/375 unchanged. No native/original execution, full closure, M5 or feature completion is claimed.

## Final local report identity

After inspection hardening, snapshots precede diagnostic composition, partial constructor failure uses rollback, and each of the nine Check wrappers asserts server/singleton/capacity restoration outside the Fixture after Dispose. This is restore-contract coverage/hardening, not evidence of a reproduced AssetManager constructor-sideeffect defect: `_singleton=this` belongs to load(), which this fixture does not call. The latest focused rebuild/run after the final code change exits0 / Accepted=true70/0.

Report SHA256 `7227a1d89e734c21bc3231e377c6557c42d3ff555270d5e0dd0d27232f4d5b90`. All11 reported source hashes match final current files (including Program and MakerChecks); canonical ITM/VEH hashes before/after match the values above. DerivedArena SHA256 `67d6121589d5df5aca4960332d4dc05a689bbac2db252ae386574417ebee0122`.

| Loaded assembly | SHA256                                                             |
| --------------- | ------------------------------------------------------------------ |
| Runner          | `8cf9dd62f22482d52c63fc01875e21506a2837152d60e3e83469f31bf726d0aa` |
| Server          | `e3c8929a0bc5ed5b31793c52678acea16c6e581479c8ee00f5babfc71177f2a7` |
| Network         | `e1027073437166f3898c4c7f88ec8997d7fc7baa9fee1f3f73c10cc5f4469cd3` |
| Assets          | `2ff0f88a3479adb3d27bcb4df6631f39fa19bbfb107579a9f2049455cd43b913` |

Documentation QA: `TRACKERS_CONSISTENT`397/22/375; 27 added/new local-link occurrences and fragments resolve; `git diff --check` passes. Baseline/native-delivery/login/Godot/flight/simulation suites skipped as unrelated. [PR63](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/63) exact-head review/CI remains required; original feature checkboxes remain unchecked.
