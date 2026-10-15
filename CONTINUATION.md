# Current continuation — October 14, 2026

Read PROJECT_START.md, AGENTS.md, the complete ROGUE_TRADER_2_PLAN.md and the affected folder plan. Master/catalog:397 feature entries,22 scoped completions,375 open. Commits/merges to main go through the CI/CD review process; no push unless the user asks.

## Accomplished

- M0/M1 engineering prototype acceptance is complete; its41 unit/source/export input/world results are historical.
- Original content remains pinned/unchanged at eb1514eaae7f9b9425307d9cc043c74ecdd15e35.46 foundation records and99 distinct canonical hashes are verified by the respective inventories.
- Original viewer and updated Windows export pass:12 hulls,231 sprites,1999×1998 overview; source/standalone input and three rendered pages at world tick0. Artifact: artifacts/windows/OriginalContent-2026-10-13-win-x64.zip.
- All89 canonical BLO directories/1,262 members indexed;609 raw candidates audited. Full runtime closure remains open.
- Actual parser/package audit validates200 items,30 vehicles,12 hull links,81 LIO records,19,198 supported values and222 map references.
- Original arena initializer passes:25 computers,100 mineral drops,51 teams,42 hides,zero clients/polls/physics. Bounded full-asset host passes with two loopback sockets and unchanged files; its separate ready report lists26 initial vehicles.
- VehicleMaker original field18 fix restores exactly eight values;allother exports unchanged.
- Authentic shared Soccer archive resolves full-asset startup without changing89 canonical archives.
- Native client reached welcome;Start had no confirmed entry. Desktop testing stopped while user played;owned processes/private registry test keys cleaned up.
- RT-F388/RT-F390: derived sale quantity/cash ordering and null-script capacity fixes pass actual-handler checks. Canonical item values, original buy branch and upstream references unchanged.
- RT-F395: shared NEW/provisional radar config fixes prior swallowed divide-by-zero errors; host rejects invalid divisors/logged runtime exceptions. Final headless10.25s host exit0,zero exceptions/clients,unchanged originals.35/35 focused checks pass against16 reproduced baseline failures.

## Next

- RT-F001-RT-F012 raw hull catalog: `shared/RogueTrader.OriginalData` (branch feat/original-hull-catalog) loads the 12 RPG hulls linked to VEH rows with a SHA-256 guard; 81 unit tests (41 existing + 40 new). Hull boxes stay open: next is native ship choice/loadout/movement and interpreting units, energy and cargo before any value is applied.
- RT-F397 narrow acceptance complete and merged to main (8b34c38): root baseline/fixed builds verify46 canonical packets and60 inert queue fixtures;29 canonical/24 synthetic length defects and16 wrong queue choices become zero. No desktop interaction. See [issue log](docs/development-log/2026-10-14-rt-f397.md); F387/F393 broader acceptance remains unchecked.

- [ ] In progress:diagnose native Start/profile behavior,confirm compatible engine,join local arena,choose ship/unspectate when user is available.
- [ ] Testing:actual native asset transfer/full LVL trailer and offline patch-announcement guard.
- [ ] Investigated:Portal/PrizeCrateA,Uiart/logo,numeric BallGraphic and missing item66 — all four genuinely absent/unknown. Findings: docs/original-research/MISSING_REFERENCES_FINDINGS.md.
- [x] RT-F396 (merged to main, ff3397d): derived buy patch widens price math, rejects negative prices and ushort stack overflow, revalidates cash/limits after Shop.Buy, delivers exactly or rolls back, debits only after delivery; 50/50 actual-handler checks pass (14 buy checks; 6 fail on pristine upstream).
- [x] RT-F389 packet-shop location gate (branch fix/rt-f389-store-proximity): derived canUseStore applies the original /buy,/sell rule (StoreEnabled terrain, or spectator with SpectatorStore); 11 Store.* checks, 61/61 fixed, 8 fail pre-patch. Not covered: skill shop (terrain skillEnabled), durable transactions, production shutdown, native store UI.
- [ ] Not started:verify original movement/combat/mining/refining/trading/repair/upgrades/rewards/native appearance and audio.
- [ ] Not started:add persistent progression;standalone does not save.
- [ ] Open:multiple native clients and final original playable-package acceptance;engine choice remains open.

- [x] RT-F391 closed as validated optional admin policy (no code/stub); see docs/development-log/2026-10-15-rt-f391.md. Skill-shop location gate (terrain SkillEnabled) deliberately NOT built: the pinned server parses the flag but never enforces it, so a gate would invent a rule.

## Evidence and rules

Maintained delegation rule: explicitly select effort per task (low mechanical docs/checks;medium bounded research/simple implementation;high complex work;Ultra only when justified),reassess for changed assignments,and review every agent's actual work/evidence before acceptance. Full rule:AGENTS.md,maintained agent qualification and review section. This persists across sessions.

Plans:docs/architecture/NATIVE_CLIENT_PLAN.md,ROGUE_RUNTIME_PLAN.md,ROGUE_PACKAGE_AUDIT_PLAN.md;client/ORIGINAL_INSPECTOR_PLAN.md;scripts/original-assets/IMPLEMENTATION_PLAN.md andDEPENDENCY_PLAN.md;data/original domain queues;docs/original-research/FEATURE_CATALOG.md.

Logs/reports:artifacts/qa/OriginalContent,artifacts/rogue-package-audit,.tools/compatibility/reports,.tools/compatibility/local-working/session-host/reports,and.tools/native-client-inspection/native-test-cleanup.json.

Latest runtime evidence:artifacts/rogue-runtime-checks/{baseline,fixed}.json,host/{ready,exit}.json,invalid-host/exit.json,transformation-guards.json;docs/development-log/2026-10-14.md. Runtime checks require -Build;SessionHost -Start automatically compiles. Both use shared SessionHost/write-session-config.ps1;fixed checks generate fresh XML and reports hash loaded assemblies. No native UI/registry interaction occurred October14. No build remains running.

SDK8/Godot is the inspection toolchain;upstream uses isolatedSDK10.0.401. Preserve originals and distinguish generic-handler availability from verified gameplay. Simulations require a completed behavior needing verification. Documentation reconciliation is complete. Completed runtime safeguards need no repeated baseline simulations;run focused checks for the next completed feature only. Latest three documentation reports retain earlier folder reconciliation evidence.
