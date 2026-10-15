# Task queue (ordered; single source for "what next")

Order follows ROGUE_TRADER_2_PLAN.md section 18: the exact-reproduction fidelity gate first, then Milestone 2. The plan's Milestone 2 list is unordered, so section B is sequenced by dependency. Unattended cloud runs and local sessions both pick from this file. Rules: AGENTS.md ("Agent and branch workflow", "Model and effort matrix", "Test selection and marking off work", "Cloud routines").

## How to use

- **Status** is one of `OPEN`, `CLAIMED <branch>` (a branch exists, see its log), `REVIEW <branch>` (pushed, under senior review), `MERGED <commit>`, `BLOCKED <why>`. Update the row on your branch in the same commit that finishes the work, together with the checklist/catalog/plan/CONTINUATION mark-off. `python scripts/check-trackers.py` must pass.
- **Cloud runs** take the first `OPEN` row marked `cloud` whose dependencies are `MERGED` on `origin/main`, skipping anything claimed under the claim rules below. A status beginning with `OPEN` counts as `OPEN`. Branch name: `cloud/<rt-id-or-q-id>-<slug>`.
- **Local only** rows need the ignored local toolchain (`.tools/`, Godot, pinned upstream, native client) or the desktop.
- Original data comes from the pinned public repo fowlmouth/cosmic-rift-assets commit `eb1514eaae7f9b9425307d9cc043c74ecdd15e35` (hashes in docs/original-research/ASSET_MANIFEST.json). A cloud run may shallow-clone it into a temp folder OUTSIDE the checkout if the network allows; if it cannot, asset-dependent tests must report SKIPPED and the work is recorded "not verified in cloud - local CI required". Never modify canonical files. No invented balance, units or art: unknown meaning stays a raw field.

## Claims: how local and cloud work never collide

The cloud only sees `origin`. A claim therefore has to exist on origin before work starts, and must stay there.

1. **Claim before starting (local or cloud).** Push a claim branch named `claim/<queue-id lowercase>-<slug>` (a single empty commit "CLAIM <id> <task> by <local|cloud>") to origin, and confirm with `git ls-remote` that it is there before any work starts. Cloud runs push their `cloud/<id>-<slug>` branch as the claim. A push rejected because the name exists, or any other origin branch already names that ID, means the item is taken: back off. Never delete a claim branch.
2. **Anything on origin that is not `main` is a claim.** A cloud run must skip every queue ID or RT-ID that appears in the name, commit messages or log of ANY non-main origin branch (`claim/*`, `cloud/*`, `local/*`, `feat/*`, `fix/*`), except a branch whose newest commit starts with `RELEASE` (rule 4). It must also skip every row that is not `OPEN` on origin/main.
3. **Finishing locally.** Before the reviewed merge, push the work commits to the claim branch, so unmerged work survives a lost machine. After the merge, set the row to `MERGED <commit>`, mark off the trackers (AGENTS.md "marking off"), and push main (the owner's standing authorization is required for main pushes). Until origin/main shows that MERGED row, the claim branch keeps blocking the cloud from repeating the work, and rows depending on this item stay blocked for cloud runs.
4. **Abandoning.** Only the claim's owner releases a claim, and only once the owner has stopped working on it. Set the row on main to `OPEN (claim abandoned: reason)` and push a commit whose message starts `RELEASE` to the claim branch. Cloud runs see the release only once origin/main shows that row, so both steps are required.
5. **No expiry.** A claim never times out, and a cloud run never takes over another claim however old it looks. A claim whose owner has gone stays blocked until its owner releases it under rule 4.

## A. Fidelity gate (plan section 18, in order)

| ID | Task | Where | Run | Depends on | Status |
|---|---|---|---|---|---|
| G1 | Spike: isolated original-client/emulator compatibility and production-engine choice | native client | local (desktop) | - | BLOCKED needs user present (RT-F380/F384/F387/F393) |
| G2 | Original hull records: raw loader, integrity guard (RT-F001..F012 data only) | shared/RogueTrader.OriginalData | cloud/local | - | MERGED 2e76140 (loader fe879b1) |
| G3 | Original ITM item loader: all 200 records raw-preserving, typed accessors only for confirmed fields, sentinels kept | shared/RogueTrader.OriginalData | cloud | G2 merged | REVIEW cloud/rt-f013-f019-g3-g4-itm-resources |
| G4 | Original resource records (7: Tellerium, Refined Tellerium, Krandazite, Refined Krandazite, Wreckage, Iron Ore, Alloy; RT-F013..F019) from ITM with categories | OriginalData | cloud | G3 | REVIEW cloud/rt-f013-f019-g3-g4-itm-resources |
| G5 | Original weapon/ammo item records and child chains (value audit, no firing) | OriginalData | cloud | G3 | OPEN |
| G6 | Original LVL/LIO reader and collision grid (RT-F021/F022 data only) | shared/RogueTrader.OriginalLevel | cloud/local | - | CLAIMED claim/g6-level-reader (local feat/original-level-reader) |
| G7 | Original VEH/RPG remaining 18 vehicle records (computers/bots) raw loader | OriginalData | cloud | G2 merged | OPEN |
| G8 | Validate field scaling/sentinels: native flight constants vs recorded evidence, produce an evidence table (no gameplay change) | docs + OriginalData tests | local | G2, G3 | OPEN |
| G9 | BLO archive member identity and dependency closure recompute (RT-F069) | scripts/original-assets | local | - | OPEN |
| G10 | Original flight/weapons/controls/HUD validation against evidence | client + simulation | local | G2, G3, G6 | OPEN |

## B. Milestone 2 loop (plan section 18, M2 task list)

| ID | Task | Where | Run | Depends on | Status |
|---|---|---|---|---|---|
| M1 | Persistent save/load and transaction core (wallet, inventory, upgrades, idempotency, atomic file repository) | server/RogueTrader.Persistence | cloud/local | - | MERGED b2ac2bf (core 00de717, review fixes 1998530) |
| M2 | Cargo capacity rules and collectible-resource pickup in simulation (data-driven, conservation tests; values come from G4 data, test fixtures labelled NEW) | server/RogueTrader.Simulation | cloud | G2, G4 | OPEN |
| M3 | Mineral-bearing asteroids and mining equipment extraction rules (generic engine; originals plug in via data) | Simulation + GameData | cloud | G4 | OPEN |
| M4 | Trading station: buy/sell using the transaction core, proximity/docking predicate, price source | Persistence + Simulation | cloud | M1 merged, G3 | OPEN |
| M5 | Basic repairs and six equipment upgrades applied through the transaction core | Persistence + GameData | cloud | M1 merged, G3 | OPEN |
| M6 | Initial NPC pirate AI (plan section 12.1/12.2 state machine, deterministic tests) | server/RogueTrader.AI | cloud | - | OPEN |
| M7 | Initial reward system (kill/mining rewards through idempotent GrantReward) | Simulation + Persistence | cloud | M1 merged, M6 | OPEN |
| M8 | Client integration: HUD for cargo/currency, station UI, save/load flow in the Godot client | client | local (Godot) | M1, M2, M4, M5 | OPEN |
| M9 | Persisted progression across exit/relaunch acceptance test (M2 acceptance criteria) | tests/Integration | local | M1..M8 | OPEN |

## C. Additional cloud-ready work (plan Milestones 2-4 groundwork; take only after every earlier cloud-eligible row is claimed, merged or blocked)

These advance later plan milestones with code and tests that need no desktop, Godot or ignored local toolchain. They keep a long unattended session busy. Stacking on an approved or same-run dependency is allowed (see AGENTS.md "Cloud routines").

| ID | Task | Where | Run | Depends on | Status |
|---|---|---|---|---|---|
| C1 | Original BLO archive directory/member reader (89 archives, 1,262 members, hash guard, raw bytes preserved; no pixel interpretation) | shared/RogueTrader.OriginalData | cloud | G2 merged | OPEN |
| C2 | Original CFG reader: raw-preserving sections/keys including the 16 terrain blocks and Rogue arena settings (no interpretation) | shared/RogueTrader.OriginalData | cloud | G2 merged | OPEN |
| C3 | Deterministic simulation replay harness: record inputs per tick, replay, state-hash equality tests (plan 20.1) | server/RogueTrader.Simulation + tests | cloud | - | OPEN |
| C4 | Simulation tick performance benchmark harness (Stopwatch based, no new packages; plan 20.4 targets as documented thresholds, results recorded not asserted) | tests/LoadTests | cloud | C3 | OPEN |
| C5 | Network protocol messages and binary serialization (input, snapshot, transaction request/response) with round-trip, truncation and fuzz tests (plan 13) | shared/RogueTrader.Protocol | cloud | - | OPEN |
| C6 | Authoritative server tick loop and input queue, headless (plan 13.2, no sockets yet) | server/RogueTrader.Server | cloud | C3, C5 | OPEN |
| C7 | Client prediction and reconciliation as a pure library with simulated latency/loss tests (plan 13.3) | shared + tests | cloud | C5, C6 | OPEN |
| C8 | Anti-cheat input validation layer: speed/rate/teleport sanity (plan 13.5) | server/RogueTrader.Server | cloud | C5 | OPEN |
| C9 | Bounty and reputation rules engine (plan 10), data-driven, persisted through the transaction core | server | cloud | M1 merged | OPEN |
| C10 | Sector transition model and recoverable transfer protocol (plan 11.3 and 14.1) | server/RogueTrader.Persistence + Simulation | cloud | M1 merged | OPEN |

Caveat for asset-dependent rows (G3-G5, G7, C1, C2): the pinned asset repo's `.gitattributes` tracks only `*.blo`, `*.lvl` and `*.bmp` with Git LFS, so a shallow clone can yield LFS pointer files for those types only (C1 reads BLO; G6 reads LVL). The ITM, VEH and CFG files these rows read are plain files. If LFS pointer files appear, run `git lfs pull` if available; otherwise the asset-dependent tests must report SKIPPED and the work is recorded "not verified in cloud - local CI required" so the owner's local session re-runs them.

Gap: plan section 18 gate item "Decode original assets ... preserve their original appearance/behavior" (RT-F023, RT-F025) has no row. G6 covers only map and collision. Add a G row when that work is scoped; do not infer one here.

## D. Closed decisions (do not redo)

- RT-F391 closed: PrizeItems.xml is an optional granted-moderator whitelist, no file or stub (docs/development-log/2026-10-15-rt-f391.md).
- Skill-shop location gate NOT built: the pinned server never reads terrain SkillEnabled; a gate would invent a rule.
- RT-F388/F389/F390/F395/F396/F397 complete (derived emulator safeguards; see catalog).
