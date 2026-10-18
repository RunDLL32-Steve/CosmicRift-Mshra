# G9 Portal icon-role and archive-loading boundary — October 18, 2026

Scope: Markdown-only RT-F377 increment under existing `claim/g9-dependency-checkpoint`, based on main `b5fc80e7ea63d3c0a6caf7cdc4890c6fead0eae5`. Authority: [master §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype); [G9 ledger](../../scripts/original-assets/plan.d/g9.md), [existing Portal finding](../original-research/MISSING_REFERENCES_FINDINGS.md#1-portalprizecratea-rt-f377). Coordinating `gpt-6.1-sol` / XHigh inspected the pinned sources and designed this bounded interpretation; root independently read primary/raw evidence and approved this exact scope on October18. Original author `gpt-6.1-sol` / Medium transfers those verified facts without broader research or executing the referenced parser/loader/runtime.

G9 remains CLAIMED / in progress; RT-F069/F377/F378/F379 remain OPEN and unchecked. Registry397 entries/22 scoped completions/375 open unchanged. Prior [PR62 inventory](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/62), [PR63 Maker70/0](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/63) and [PR64 BallGraphic](https://github.com/RunDLL32-Steve/CosmicRift-Mshra/pull/64) are accepted dated history; their logs and results are not changed or rerun. The seven-member UtilityEffects inventory and missing PrizeCrateA pair were already proved, not rediscovered here. Existing `PortalGraphic.cfs` is not a justified replacement or evidence of warpGraphic semantics.

## Canonical raw versus pinned-loader roles

Canonical asset pin `eb1514eaae7f9b9425307d9cc043c74ecdd15e35`: coordinator/root freshly read only ITM/Rogue_mp.itm,111614 bytes,SHA256 `dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7`. This is a one-file byte/hash observation, not a new99-file/89-archive integrity pass. Row112 is type17/v57/ID26, exact name `Portal`,113 fields.

| Raw field pair | Exact raw lexemes (quotes expose leading space) | Pair after this loader's trimming | Pinned WarpItem.Load member |
|---|---|---|---|
|f23/f24|`"UtilityEffects"`, `"PrizeCrateA"`|UtilityEffects / PrizeCrateA|iconGraphic|
|f71/f72|`" None"`, `"None"`|None / None|prefireGraphic|
|f101/f102|`" None"`, `"None"`|None / None|warpGraphic|

Raw f23–30 is `UtilityEffects,PrizeCrateA,0,0,35,0,0,300`; raw f71–78 and f101–108 each begin `" None"`, `"None"` followed by six zeros. The quotes in the table expose lexemes, not edited source bytes. No native interpretation is assigned to numeric fields. The known missing pair occupies this loader's **iconGraphic** slot, not its warpGraphic slot. These are names/offsets of the pinned fixed-index loader; native field/display semantics and revision applicability remain UNKNOWN. Merely storing version57 does not prove the loader matches every native revision.

## Conditional static source chain

Server pin `bb97579efeceff1c63d781d8c1bb8dd4a5b9b849`. Immutable links identify source inspected by coordinator/root; the following are VERIFIED STATIC conditional branches, not observed runtime loading or native display acceptance.

| Source / lines | Bounded inspected behavior | What remains unproved |
|---|---|---|
|[WarpItem.cs73–80](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Itm/ItmInfo.WarpItem.cs#L73), [Graphics.cs18–28](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Itm/ItmInfo.Graphics.cs#L18)|WarpItem.Load creates iconGraphic at23,prefireGraphic71,warpGraphic101; Graphics reads archive name and member ID at start/start+1 through CSVReader.GetString.|Fixed-index source members only; no native icon/warp effect/display claim. Graphics offsets have no version branch in WarpItem.Load.|
|[CSVReader.cs18–45/79–82](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/AssetFiles/CSVReader.cs#L18), [Item.cs64–70](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Itm/ItmInfo.Item.cs#L64)|Parse and GetString trim lexemes; item loader records version57.|Normalization explains None/None without changing leading-space raw source; stored version alone is not native revision proof.|
|[ItmInfo.cs91–98](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Itm/ItmInfo.cs#L91)|Type17 registers all three graphic archive names plus two sound archives in BlobsToLoad.|This collects archive names, not selected member IDs or accepted fallback/display behavior.|
|[AssetManager.cs137–165/220–243](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/ZoneServer/Game/Assets/AssetManager.cs#L137)|Iterates BlobsToLoad, calls loadBloFile with `.blo`, then LoadBlobFromFile<BloFile>/addAssetData if non-null. loadBloFile returns for disabled bUseBlobs/null/empty; lowercases, skips none.blo/.blo/nosound.blo, deduplicates and adds archive filenames.|Conditional source paths; no universal selected-state or runtime loading claim. None skip does not establish native effect/display intent.|
|[AssetFileFactory.cs78–105](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/AssetFiles/AssetFileFactory.cs#L78), [AbstractAsset.cs54–63](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/AssetFiles/AbstractAsset.cs#L54)|Factory searches archive filename across configured folders; missing records filename/returns null, otherwise instantiates/ReadFile. BloFile has no ReadFile override, so inherited ReadFile sets Filename and CRC32.fileChecksum.|This bounded path handles archive containers without querying chosen graphic blobID. It does not prove member resolution, rendering, fallback or native eligibility.|
|[BloFile.cs34/42–66](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/dotnetcore/Assets/Blo/BloFile.cs#L34)|ReadBlobEntries is a separate member-directory/payload parser; bounded tracked-C# search below found only documentation/declaration.|No global/dynamic/native absence or no-fallback-anywhere inference.|

Exact coordinator bounded search:

```powershell
git -C .tools/references/infantry-online-server grep -n ReadBlobEntries bb97579efeceff1c63d781d8c1bb8dd4a5b9b849 -- '*.cs'
```

It returned only `dotnetcore/Assets/Blo/BloFile.cs` line34 documentation and42 declaration, with no lexical callsite under this exact pinned tracked-C# search. This is not a global/native/dynamic consumer absence proof. No executed parser experiment or runtime test was performed for this source comparison.

## Pinned Git blob provenance

Coordinator verified each working file after CRLF→LF normalization equals its pinned Git blob. Table lengths/hashes identify **pinned Git blob bytes**, not raw CRLF working bytes; the latter hashes differ. Paths are relative to `dotnetcore/` at the server pin above. Overlapping hashes retain the accepted provenance context; no whole-inventory check was rerun.

| Path | Pinned bytes | SHA256 |
|---|---:|---|
|Assets/Itm/ItmInfo.WarpItem.cs|7167|`10c68c59c334e869d8a4087b0fe8a256569c59b2bfcab3971a4cd65cdbdb4c71`|
|Assets/Itm/ItmInfo.Graphics.cs|1053|`92e1419a5f1112663d18d9260ef14616cf3ea0fd824ea8526d4cb31f1092e90f`|
|Assets/Itm/ItmInfo.cs|6403|`73364f6cf7aab5713a2d9d4e55240cb9445ef7d74c882f6933393340bc01045d`|
|Assets/Itm/ItmInfo.Item.cs|3279|`aeef305aaae352949d617d2c559da22b7ab2a09f5222e55b1eeda0bf2a045e0f`|
|Assets/AssetFiles/CSVReader.cs|3334|`394f58982a86d75ab9aa3aa3a2603801f543448e5b4e0543aaaa30949abfc400`|
|Assets/AssetFiles/AssetFileFactory.cs|4562|`f52e3e9a98f401086e1c9616efe697413295e711b1283a43e97b0110e290bf22`|
|Assets/AssetFiles/AbstractAsset.cs|1969|`b05e38682f9c8143badfb69db18348c53ef2cf4055df1cfe43d0bfee69717cb7`|
|Assets/Blo/BloFile.cs|2223|`d592bd4bb11dfb769151e5de4ce8981a1167d16499f695ea30bf883154ec279c`|
|ZoneServer/Game/Assets/AssetManager.cs|17106|`af762b26d1df51ed1c892cbeb23f2473ac94a03880446ccb47c19377d7e02362`|

Provenance method (not a new author run/tool): Python `blob=subprocess.check_output(['git','-C',ref,'show',pin+':dotnetcore/'+path])`, then `len(blob)`, `hashlib.sha256(blob).hexdigest()` and `working.read_bytes().replace(b'\r\n',b'\n') == blob`.

## Limits and resume

The known missing icon pair under this loader is not a demonstrated missing warp effect. Container registration/checksum success would not resolve PrizeCrateA, establish display/fallback or authorize substituting PortalGraphic.cfs. Matching original/native icon consumer, selected revision, display/evaluator and fallback remain UNKNOWN. No native window/effect/rendering or complete original closure acceptance; raw609/eight-residual statuses and all original bytes/names/values stay literal.

The [RT-F054 raw warp-data log](cloud/cloud-rt-f054-hyper-warp-items.md) was read as history only; its energy/target/prerequisite/LIO semantics and RT-F182/G6 ownership remain separate. No warp activation/handler/policy implementation or analysis is added here. Resume with matching original/native icon-consumer/version/display/fallback evidence; other bounded G9 references remain available, so this is not a whole-G9 blocker. User-present native prerequisites remain separate. No broader research/native/reverse-engineering, new code/scripts/tests/assets/tool/oracle or output report.

## Documentation verification

`python scripts/check-trackers.py` passed: `TRACKERS_CONSISTENT`,397 entries/22 complete/375 open. Scoped inline Python local-link validation passed 27 added/new occurrences and required fragments (new file in full plus added lines elsewhere, not deduplicated). `git diff --check` passed. No prepare/build/runtime/parser/Maker/Ball/decoder/staging/inventory/baseline/Godot/native/physics checks or prior evidence reruns. Fresh independent exact-head review/CI belongs to this increment's new PR; original author never merges.
