# G9 Uiart/logo evidence boundary — October 18, 2026 (Chicago)

Markdown-only checkpoint on `claim/g9-dependency-checkpoint`, based on main `76633126df74731ce236cc27c0a403b258370d93`. Authority: [master §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); current [G9 ledger](../../scripts/original-assets/plan.d/g9.md). Coordinating `gpt-6.1-sol` / XHigh designed this bounded source interpretation; root independently checked primary/raw evidence and approved the correction and stable-summary consolidation. Original author `gpt-6.1-sol` / Medium transfers those findings and runs Markdown QA only. Checkpoint date: October18 Chicago / October19 UTC.

G9 remains CLAIMED / in progress; RT-F069/F377/F378/F379/F386 remain OPEN. Registry397 entries/22 scoped completions/375 open unchanged. No original bytes/names/values, raw609/eight-residual classifications or master/catalog checkboxes change. Dated PR62 inventory,PR63 Maker70/0,PR64 BallGraphic andPR70 Portal evidence remains unchanged and was not rerun.

## Known absence versus new source facts

Accepted prior inventory: canonical cruiart.blo has33 members and lacks the exact `logo` member name. This checkpoint does not repeat that inventory or discover another missing member. Exact-name absence does not identify the appearance of other members or prove shipping history, native use, intended-but-unshipped content or a default fallback. No other member is identified as a logo or chosen as substitute artwork.

Coordinator/root freshly read one canonical file at asset pin `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`: CFG/rogue.cfg,48399 bytes,SHA256 `82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90`. Raw row2752 is `Logo=cruiart,logo`. This is one-file byte evidence, not a99-file/all89 archive check; prior exact-member inventory was not rerun.

At server pin `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849`, [CfgInfo.Uiart.cs32/88–89](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Cfg/CfgInfo.Uiart.cs#L32) declares string logo, assigns Parser.GetString("Logo") and registers Parser.GetBlob(logo). Its constructor contains no local logo display/fallback branch. This is VERIFIED STATIC source access/registration, not native display/member lookup or proof the field is unused.

The coordinator's exact bounded search returned only those three occurrences:

```powershell
git -C .tools/references/infantry-online-server grep -n -i -w logo bb97579efeceff1c63d781d8c1bb8dd4a5b9b849 -- '*.cs'
```

This is a whole-word lexical result for pinned tracked C# only; dynamic, unnamed or native consumers and other global paths are not ruled out. The referenced source was read, not executed.

| New pinned Git blob (under dotnetcore/) | LF bytes | SHA256 |
|---|---:|---|
|Assets/Cfg/CfgInfo.Uiart.cs|4566|`7d409abcac9f589b4a14ed65c6f40ef754dd260e3bfbcdae4fc36a41cc0930c9`|

The server HEAD matches that pin. Coordinator/root verified working bytes after CRLF→LF normalization equal pinned Git blob bytes; raw CRLF working hashes differ. Reproduction method (not a new author run/tool): obtain `blob=subprocess.check_output(['git','-C',ref,'show',pin+':dotnetcore/Assets/Cfg/CfgInfo.Uiart.cs'])`; record `len(blob)`/`hashlib.sha256(blob).hexdigest()` and compare `working.read_bytes().replace(b'\r\n',b'\n') == blob`.

## Reused evidence and limits

The [PR64 source table](2026-10-18-g9-ballgraphic-source.md#source-applicability-table) already records GetString/GetBlob helper behavior; [PR70 selected container chain](2026-10-18-g9-portal-source.md#conditional-static-source-chain) distinguishes inspected archive-container operations from selected-member/native display/fallback proof. Their11/nine source hashes and paths were not retraced or rehashed for this increment.

The [RT-F025 data-slice log](cloud/cloud-rt-f025-cfg-uiart-render.md) and its audit/view/tests were read as accepted history, never run. That scope explicitly excludes RT-F378 and covers raw Uiart/Uiwav/Render plus archive filename existence, not member existence or native interpretation. Its historical “BLO content LFS-only” availability statement describes that run, not present physical availability. No RT-F025 acceptance is promoted to logo/native acceptance; its files are unchanged.

Matching original/native logo consumer, revision applicability, display/evaluator and fallback remain UNKNOWN. Resume with matching evidence before interpreting native use or proposing artwork changes; keep other bounded G9 work available and user-present native prerequisites separate. This documentation correction does not complete a feature, release G9 or establish a whole-G9 blocker. No broader research, native/reverse-engineering, code/scripts/tests/assets/newtool/oracle/outputreport or runtime work.

## Documentation QA

`python scripts/check-trackers.py` passed: `TRACKERS_CONSISTENT`,397 entries/22 complete/375 open. Scoped inline Python validation passed 21 added/new local-link occurrences with required fragments (new file in full plus added lines elsewhere; not deduplicated). `git diff --check` passed. No tests/builds/runtime/parser/oracle/decoder/inventory/Maker70/0/native/physics checks or accepted evidence reruns. Fresh independent exact-head review/CI belongs to [PR75](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/75); author never approves or merges their own work.
