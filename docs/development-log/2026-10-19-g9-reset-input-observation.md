# G9 initialized StartGame reset input observation — October 19, 2026

Authority: [master §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); [G9 ledger](../../scripts/original-assets/plan.d/g9.md). Author `gpt-6.1-sol` / Medium and coordinator `gpt-6.1-sol` / XHigh are the explicit user substitutions. This record starts at the historical author readiness stage before build/run clearance; it is not original-feature acceptance.

## Narrow contract and source basis

Only executable change: existing ArenaProbe Program.cs captures actual `server._zoneConfig.startGame.vehicleReset` once, after server.init and accepted catalog/provenance inspection, strictly before newArena. `StartGameResetInput` records stage `AfterServerInitBeforeNewArena`, selected standalone initialized-object scope, observed value, expected false and acceptance. The same snapshot is included in successful JSON. Unexpected true writes top-level Accepted=false JSON with the snapshot and existing complete catalog identities, then uses the existing controlled rejection path before arena construction.

Expected false is source-backed: canonical pin `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`, CFG/rogue.cfg2145–2147 has [StartGame]/VehicleReset=0. Pinned server `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849` ZoneServer.cs185–201 selects/loads the CFG; CfgInfo.cs67–184 loads section dictionaries and constructs StartGame at144. CfgInfo.StartGame.cs17–21 reads GetBool(VehicleReset), Git blob `3aa4c2f0478b550d86dfa1fcee844adb9abd1213`; GetBool229–247 uses Int16.Parse/Convert.ToBoolean and false on missing/error. These are reused coordinator-inspected source facts, not new baseline hashes or native evidence. Existing catalog guard pins the selected CFG48399 bytes/SHA256 `82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90` and canonical ITM.

This observes neither ScriptArena's private cached class reference nor gameStart/gameReset behavior. Arbitrary mutation/custom scripts, later sessions, indirect callback effects, missing66 eligibility and native behavior remain unknown. No ItemInfo.Load replay, injected maps/items/config, forced mocks, reflection/cache observation, helper/wrapper/csproj/parser/upstream change or synthetic66. Existing selected200-ID/digest/reference/66-null Warning and SourceFiles/LoadedAssemblies provenance remain unchanged. The older post-arena InitialHidesSetting is distinct.

## Author readiness and focused verification

Historical author readiness: clean branch claim/g9-dependency-checkpoint at main `a0aa59aebc32978356b27b4592a3829b92d9ef62`; PR85 source-only and PR76 selected-catalog acceptance remain immutable dated history. At that readiness stage no build/run had occurred. Coordinator/root subsequently inspected the corrected actual diff, issue-owned setup and preflight, then explicitly cleared one serial existing `run-arena-probe.ps1 -Build`. Existing seven-patch prepared source is reused only if matching; no prepare authorized without mismatch. Preserve prior accepted source/DLL/report/control/log artifacts; new author evidence uses ignored `artifacts/g9-initialized-reset-input/author/`. Capture wrapper/build/helper logs and exits, full JSON, actual source/loaded DLL bytes/hashes and all45 source/pristine/active identities for15 approved inputs before/after.

Only this completed observation needs the focused bounded offline initializer; no baseline/Maker/RuntimeChecks/native/login/Godot/flight/physics/simulation/decoder/inventory reruns. True rejection receives source review only. The human's sprint integration rule applies; actual attempts and the unavailable result are recorded below, with no borrowed PR82 implementation or invented sprint worktree.

G9 stays claimed/in progress. RT-F069/F386/F377/F378/F379 and broader original acceptance remain open;397/22/375/raw609/eight unchanged. Live workflow status belongs to the [branch log](cloud/claim-g9-dependency-checkpoint.md) and its current PR; this dated record describes author-stage evidence, not approval/CI/merge.

## Observed author run — October 19 Chicago / October 20 UTC

After explicit actual-code/setup clearance, the author ran exactly once:

```powershell
powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File scripts/compatibility/run-arena-probe.ps1 -Build
```

Only process-scoped policy was used. Wrapper exit0, helper/control exit0, report exists; control UTC `2026-10-20T00:56:43.7142000Z`, owned helper PID27400. Build1487 warnings/0 errors; helper stderr0 bytes. Full report top Accepted=true, CatalogMembership.Accepted=true and StartGameResetInput.Accepted=true; observed=false, expected=false, stage AfterServerInitBeforeNewArena, explicit selected standalone initialized-object scope. This newly measures the actual initialized input only; it does not execute gameStart/gameReset or establish native/reset/no-drop eligibility. The unexpected-true controlled JSON branch was source-reviewed only, not executed.

Preserved full200 expected/actual arrays are identical/sorted/unique, no duplicate or lookup mismatch, actual reference consistency=true; both digest fields are `3e4b98a8f8a9afd4f5b55eb121550be978219002e1cfed3ab2b9bd7e1721f231`. One positive66 lookup returns null with exactly `Unable to get item by ID: 66`; catalog and whole initialization unexpected errors are empty. One arena, zero clients/begin/poll/physics; original map1999×1998,30 vehicle definitions/12 hull links,51 teams,25 initial Computers/100 initial drops/42 hides/0 doors/flags. No old inventory/Maker/baseline check was rerun.

Ignored `artifacts/g9-initialized-reset-input/author/` preserves wrapper.log/exit, build/stdout/stderr/control/full JSON, runtime logs, current two source files and actual three loaded DLL byte copies. `verification.json` (approved verify.py post mode exit0) confirms45 source/pristine/active identities for15 approved inputs and seven canonical identities unchanged, seven prepared patch hashes matching and regular issue-owned output trees. Current source and loaded DLL hashes equal report fields and saved bytes. Prior author/rework artifacts remain byte-equal; `prior-accepted-fixed-outputs/` separately retains the fixed paths overwritten by the existing wrapper. `execution-provenance.json` distinguishes original preliminary pre mode from corrected-source readiness mode; neither is a probe run. Preliminary draft evidence under artifacts/g9-reset-input-observation/author is explicitly labelled preliminary.

| Evidence                                    |  Bytes | SHA-256                                                            |
| ------------------------------------------- | -----: | ------------------------------------------------------------------ |
| Program.cs                                  |   8446 | `c6069bfa2940ea47f3777451e24b5fd4901e3a41d1944938d57f52b2fccf34b3` |
| CatalogMembershipChecks.cs                  |   9113 | `15e9a24d31e4f4f53ae4a2ed9853b83fa362ea532ed07a324d544e443c5bc50a` |
| Actual loaded Runner / LegacyArenaProbe.dll |  54272 | `ec123c10441163ac486a3ffbe14555f9f0d8629ba13dc3bdc7acaf46d2b802de` |
| Actual loaded Server / ZoneServer.dll       | 539648 | `21d4259bcd63e3f39bf6af1b041213c8a8bc00ff7d309cad4cd1e667d2376d01` |
| Actual loaded Assets / Assets.dll           | 128512 | `14e75f1637bcde02db36ad4847321e68979e607205a6abdcdb8ab89ee21a4305` |

Full report SHA256 `c4f6f567b30627bb1b1ed665fa9455c4e3afe502eeebd7080975a8b5748e70d0`; verification SHA256 `8481766af4459352cf15966f376189d3a410e89919c91464e041adaad6993db8`. These identify this dated author run, not reviewer reproduction or formal approval.

## Sprint integration limit

Actual `python scripts/sprint-integrate.py` exited2: file absent. Actual `python scripts/sprint-validate.py --root C:\Repos\CosmicRift\.tools\issue-worktrees\g9-dependency-checkpoint` exited2: file absent. The root argument names the current issue checkout, not a sprint result; no sprint worktree was created. `sprint-attempts.json` and both logs preserve the commands/outcomes. Sprint integration/validation is **NOT VERIFIED**, not SPRINT_VALID and not a pass; the direct human policy permits continuing with this disclosed unavailable-script limit. Both scripts belong to separate unmerged PR82; no files were borrowed/copied/merged and no sprint branch was pushed.

## Author QA and handoff scope

Author final QA: `python scripts/check-trackers.py` exit0 prints TRACKERS_CONSISTENT,397 entries/22 complete/375 open. All27 added/new local-link occurrences including fragments pass; `git diff --check` exit0. Exactly11 allowed files, no new feature checkmarks/protected paths or high-confidence secret-pattern matches. This is the author-stage local result; independent exact-head review/reproduction, required Windows CI and reviewer-only merge gates are separate. All original feature acceptance remains open and counts unchanged.

Author handoff metadata: [PR86](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/86), work commit `86d1a59`, initial BBQ `c75f746`. Coordinator/root accepted the actual diff/evidence for this handoff. This is historical author-stage QA; use PR86 and the branch log for live review/CI/merge status. No independent approval or author merge is implied.
