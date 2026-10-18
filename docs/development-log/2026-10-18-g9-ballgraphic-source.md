# G9 BallGraphic source applicability — October 18, 2026

Scope: Markdown-only RT-F379 increment under existing `claim/g9-dependency-checkpoint`, based on main `71341cbcb23af3a921592543ee36ac79f39a634e`. Authority: [master §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); [G9 ledger](../../scripts/original-assets/plan.d/g9.md), [missing-reference finding](../original-research/MISSING_REFERENCES_FINDINGS.md#3-ballgraphic-numeric-reference-rt-f379). Coordinating `gpt-6.1-sol` / XHigh inspected the pinned sources and designed this bounded interpretation; root reviewed the primary-source findings and approved this documentation scope on October 18. Original author `gpt-6.1-sol` / Medium transfers those verified findings without new research, code or execution.

G9 remains CLAIMED / in progress; RT-F069/RT-F379 remain OPEN and unchecked. Registry397 entries /22 scoped completions /375 open unchanged. Prior [PR62 inventory](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/62) and [PR63 maker diagnostic](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/63) are accepted dated history, not fresh inventory/runtime results here. Their dated evidence remains unchanged. No master/catalog checkboxes, original bytes, names, values or raw609/eight-residual classifications change.

## What the pinned sources establish

Canonical CFG/rogue.cfg row2107 is exactly `BallGraphic=Soccer,Ball,0,0,0,0,35`. Soccer/Ball identity, supplemental access and archive registration were already established; this increment adds no archive/member inventory discovery. The missing evidence is a matching original CFG consumer and its selected grammar/version/evaluator. None of the five trailing numeric tokens is reclassified as HSV, animation, ordinal or another role from token shape.

Server pin: `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849`. The coordinator's exact bounded lexical search was:

```powershell
git -C .tools/references/infantry-online-server grep -n -i ballGraphic bb97579efeceff1c63d781d8c1bb8dd4a5b9b849 -- '*.cs'
```

It returned only `dotnetcore/Assets/Cfg/CfgInfo.Soccer.cs`: line24 declaration,83 Parser.GetString assignment,124 Parser.GetBlob registration. This result covers tracked C# at that pin. It does not prove global absence, rule out dynamic/unnamed/native consumers, or identify a native binary implementation. Existing RT-F025 audit tooling checks Uiart/Uiwav/Render and archive filenames; it does not supply a BallGraphic numeric-consumer join.

## Source applicability table

All source links below are immutable pinned locators. VERIFIED STATIC describes facts actually inspected by the coordinator/root; conditional comparisons are explicitly source inference, not executed parser tests or CFG validation.

| Source / exact lines | Inspected method or caller behavior | Applicability to this CFG token |
|---|---|---|
|[CfgInfo.Soccer.cs24/83/124](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Cfg/CfgInfo.Soccer.cs#L24)|Declares BallGraphic, obtains a string, registers an archive.|Establishes string/archive handling only; no trailing-number consumer is demonstrated.|
|[CfgInfo.cs219–227 and250–265](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Cfg/CfgInfo.cs#L219)|GetString trims the key and returns the stored value or empty; GetBlob splits commas and returns only the first component.|These accesses do not dispatch numeric CSV fields.|
|[SpriteBlob.cs13–21](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/SpriteBlob.cs#L13)|ParseV1 reads two strings and two shorts (four fields), creates default HSV components and sets AnimationTime0.|Method field order only. Applying it to BallGraphic would require an evidenced caller/version join.|
|[SpriteBlob.cs24–32 and35–43](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/SpriteBlob.cs#L24)|V2 reads two strings, two shorts and three HSV components (seven fields), AnimationTime0. V3 adds GetShort for AnimationTime (eight fields). [HSV.cs12–16](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/HSV.cs#L12) reads three signed bytes.|Do not assign35 or any other CFG number to these members merely because a token count fits.|
|[CsvFile.cs130–184/249–290](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/CsvFile.cs#L130)|Concrete LineParser GetSByte,GetShort,GetString/GetInstance throw InvalidOperationException at AtEnd. [ICsvParser](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/ICsvParser.cs) alone does not establish all implementations' end behavior.|No missing-short default under this particular reader; no claim about all possible readers or CFG consumers.|
|[AnimatedSprite.cs14–33](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/AnimatedSprite.cs#L14), [VehInfo.cs44/127/195–200](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Veh/VehInfo.cs#L195)|AnimatedSprite reads four short prefix fields, then dispatches V3 at version≥30,V2 at≥12,otherwiseV1. VEH loading uses CsvFile and passes the VEH record's version for two AnimatedSprite entries at version≥5.|Vehicle-record context only; neither the prefix nor the VEH version is established for BallGraphic.|
|[VehInfo.Car.cs80–116](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Veh/VehInfo.Car.cs#L80), [Computer.cs91–104](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Veh/VehInfo.Computer.cs#L91), [Dependent.cs57–73](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Veh/VehInfo.Dependent.cs#L57)|Direct SpriteBlob dispatches use the VEH record version. Car standard branches use≥30/≥12/else, while its shadow branch differs at≥30/≥19.|These are concrete vehicle callers, not a CFG join or an exhaustive global list of version branches.|

Conditional source inference with the known CsvFile.LineParser and exactly seven isolated tokens: V1 would consume four and leave three; V2 would consume seven; V3 would request an eighth token and throw under that reader. This does not imply record acceptance or select a version: [CsvFile.Load396–399](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Shared/CsvFile.cs#L396) checks whole-record end. It is not an executed parser experiment and does not validate the CFG token. The four-short AnimatedSprite prefix is another reason not to equate nearby vehicle structures with this standalone CFG string.

## Pinned Git blob provenance

The coordinator verified each working file after CRLF→LF normalization equals its pinned Git blob. The table records **pinned Git blob byte lengths/SHA256**, not raw CRLF working-file hashes. Raw working bytes differ; no raw-working-hash-equals-pinned claim is made. All paths are under `dotnetcore/` at the server pin above.

| Path | Pinned bytes | SHA256 |
|---|---:|---|
|Assets/Cfg/CfgInfo.Soccer.cs|6604|`ff9bf252998236c3742ce914fc27e54ef39ff89000a3c4a136f98501ace9e2ff`|
|Assets/Cfg/CfgInfo.cs|10370|`5f11ffc57e7c94ae44ef98ad29a2859f37438029c9da481011da670a2cbaae3b`|
|Assets/Shared/SpriteBlob.cs|1570|`a6bfad0e64966eff2838f48b8cfea8f83cf2675fc86b7ddfc7b6ff582e686f01`|
|Assets/Shared/HSV.cs|427|`fb4b6250ddb936020cba74fc9e4ce7af2d3087803898d3357fb2bb187a43b92f`|
|Assets/Shared/AnimatedSprite.cs|921|`7ac824a6f4765bd8976cfebee3a7bb784506b71012a400f56a6296f46885f487`|
|Assets/Shared/ICsvParser.cs|1054|`f643a91c389964c2646244cdac41eae17bc8bc9d4b7780924134f8aa5d136fbc`|
|Assets/Shared/CsvFile.cs|14893|`532b6f22514308f291a6c26781169ec02caef0a67588576db9db5d516e2ed854`|
|Assets/Veh/VehInfo.cs|11306|`09588c41373b3255470ccbfaec3539701a5de644b0d45dfbf1ce1d855acc4208`|
|Assets/Veh/VehInfo.Car.cs|7483|`7571532e4039d61c6bd17bce20a93dae1c6d15d97b7bfb7fb82a804e3c3abb3f`|
|Assets/Veh/VehInfo.Computer.cs|6537|`78a61cbae2453102501ba4f288827f6cefe238704311ef5a075fa3632ac25673`|
|Assets/Veh/VehInfo.Dependent.cs|4310|`142b92d2b98548ef3e52ea6a55ae4a47fd7f999369020536be90d0445b31bafc`|

Provenance method (documentation only; not a new script or a fresh author run): obtain each `blob` through Python `subprocess.check_output(['git','-C',ref,'show',pin+':dotnetcore/'+path])`; record `len(blob)` and `hashlib.sha256(blob).hexdigest()`, then compare `working.read_bytes().replace(b'\r\n',b'\n') == blob`.

## Acceptance limits and next evidence

VERIFIED STATIC: the bounded tracked-C# search result, CFG string/archive helper behavior, nearby method field orders, concrete reader end checks and vehicle-context version dispatches. SOURCE INFERENCE: conditional seven-token consumption under that reader only. UNKNOWN: the matching original CFG numeric consumer, selected grammar/version, runtime evaluator, and every BallGraphic trailing field's meaning. No native rendering/palette/animation, runtime eligibility or complete original dependency closure is accepted. Supplemental Soccer identity or historical startup cannot bridge that gap; canonical ball.blo/GameBall is no substitute.

Resume with evidence of the matching original CFG consumer and its grammar/version/evaluator before designing any parser test or numeric interpretation. Keep other G9 offline reference/caller/sentinel work available; this source boundary is not a whole-G9 blocker. User-present native checks retain their separate prerequisites. No new tool/report/oracle, research beyond the bounded pinned-source inspection or native/reverse-engineering scope was performed here.

## Documentation verification

`python scripts/check-trackers.py` passed: `TRACKERS_CONSISTENT`,397 entries/22 complete/375 open. Scoped inline Python validation passed 22 added/new local-link occurrences and required fragments (new file in full plus added lines of changed Markdown; occurrences not deduplicated). `git diff --check` passed. Prior inventory/Maker70/0 evidence was not rerun. No prepare/build/runtime/parser/decoder/staging/inventory/baseline/Godot/native or physics checks. Fresh independent exact-head review/CI belongs to [PR64](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/64); author never merges.
