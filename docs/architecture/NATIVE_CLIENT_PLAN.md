# Native client dependency and compatibility plan

Master authority: [§1.2/§1.3 and §18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md). Related: [legacy compatibility plan](LEGACY_COMPATIBILITY_PLAN.md), [engine evaluation](ENGINE_EVALUATION.md), [RT-F069 dependency closure](../original-research/FEATURE_CATALOG.md#rt-f069). Canonical Rogue Trader data remains pinned fowlmouth originals unchanged. Extracted binaries are ignored local references, excluded from release payloads.

Current stage: **Investigating — bounded static inventory complete; native acceptance open**, October 13, 2026. The next missing prerequisite is authoritative supported engine build/version identity and an isolated complete profile/content route. No installer, launcher, native client or server was launched in this slice. No gameplay simulation ran. Stage history: Investigating (distribution metadata), In progress (static extraction), Testing (PE/hash/manifest inventory), static inventory Complete (evidence below); full compatibility remains Investigating.

## Active acceptance queue

- [ ] Resolve actual supported engine version/build identity. Executables have no FileVersion/ProductVersion; installer/launcher versions are distinct. Pin the chosen channel and exact hash before later compatibility execution.
- [ ] Resolve installer manifest versus executable mismatch and current update-channel behavior; do not silently replace one unversioned engine binary with another.
- [ ] Establish isolated registry/profile/settings and directory-free local zone listing before any future launch. Native profile strings use `SOFTWARE\HarmlessGames\%s\Profile%d`; launching a copied executable does not by itself isolate user settings.
- [ ] Close original Rogue Trader graphics/audio/help/LVL/LIO dependencies separately from generic package content (RT-F069). The installer contains other zones' content; it is not the canonical Rogue Trader source.
- [ ] Validate a compatible control zone, then original Rogue Trader entry/unspectating and archived SilentClientCrash reproduction/fix.
- [ ] Validate original bank/palette/blit/timing, movement/combat, economy/persistence and authority requirements before choosing the production engine.

Native gameplay/features remain unchecked. Root reconciles discoveries/status with the catalog/master; no new original gameplay mechanic was inferred from package metadata.

## Completed static inventory and evidence

The [vendor download page](https://www.freeinfantry.com/download/) currently recommends Steam and links [maintainer installation source](https://github.com/spiffyguy/FreeInfantry-Client-Setup/blob/main/Linux/play-on-linux-freeinfantry.sh). That source identifies the other installer endpoint. The [server quickstart at the pinned commit](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/quick-start.md) documents direct `Infantry.exe`, clearing directory addresses and editing `Infantry.lst`; this remains a prospective local route, not an executed compatibility result.

| Reference | Static finding |
|---|---|
| Preserved `.tools/compatibility/Infantry-Online-Setup.exe` | 6,181,473 bytes; SHA-256 `e3770b201bda7df06c8e1f939cc57b66f248ef58a219468c00238e7c96e3577a`; installer metadata 2.1.0.9 unchanged. Full 7-Zip could not identify its archive; no installer execution attempted. |
| [Official installer endpoint](https://www.freeinfantry.com/download/win/latest/Install-FreeInfantry.exe) | 420,002,146 bytes; SHA-256 `10b06a37b2bff8b2af03dc59296765d2c55ad6fb0e58247786c8ad7db2828401`; NSIS-3 Unicode/Deflate. Selective static extraction yielded 18 code/config/text/XML files (8,588,634 bytes), not the entire asset package. |
| Extracted `infantry.exe` | Actual x86 native PE executable, 2,441,216 bytes; SHA-256 `33d97aab9ffad1ff62252cfa0bf1acdf091b71da246460bfe74f55e3e901b1ba`; MD5 `55ea5563d3c7a08b43540e03ea34ee82`. CLR directory absent. FileVersion/ProductVersion absent. PE timestamp March 9, 2010 is header metadata, not proof of release date/version. |
| Extracted launcher | `InfantryLauncher.exe` metadata 2.2.0.0; separate managed launcher, not engine version. |
| Extracted rendering wrapper | `ddraw.dll`: cnc-ddraw 6.1.0.0; accompanied by `ddraw.ini`, configuration executable and shader package references. Wrapper version is not engine version. |
| Engine import table | DirectDraw, DirectSound, WinMM, Winsock32, IP Helper, common controls, kernel/user/GDI/shell/advapi/OLE32. These are statically referenced DLLs, not verified runtime availability. |
| Installer `manifest.xml` | 397 entries. Its `infantry.exe` size agrees but expected MD5 `489fb783850cb121a0c2655ac00a13be` differs from extracted bytes. This is an observed package metadata mismatch, not established client corruption or SilentClientCrash cause. |
| [Live core manifest](https://assets.freeinfantry.com/manifest.xml) | Advertises no `.exe` entries at inspection time. Launcher manifest separately advertises launcher and patcher binaries. |
| [Direct historical client endpoint](https://assets.freeinfantry.com/infantry.exe.gz) | Decompressed x86 PE 2,618,368 bytes; SHA-256 `d8dd7de3233af511388356a3bbbbd4a78298af22d9b2905237f9ddb4daac733e`; no FileVersion/ProductVersion. Timestamp April 6, 2024 is header metadata. Its presence alone does not identify the vendor's current supported engine build; no launch/substitution attempted. |

Static inventory completes executable acquisition/architecture/hash identification and dependency-route evidence. Semantic engine version, actual local entry, native Rogue Trader compatibility and engine selection remain open. The newer pinned launcher source invokes `FreeInfantry.exe`, while inspected installer contains `infantry.exe`; reconcile channel differences before launcher use. The extracted settings still contain public directory/account endpoints; they were read only, never used to connect.

Ignored evidence: `.tools/native-client-inspection/inventory.json`, `current-installer-list.txt`, `preserved-installer-list.txt`, `extraction.log`, original downloaded packages, `current-payload/`, live manifests, local original inspection scripts. `inventory.json` records every extracted file's hash/length and PE imports; 18 files inventoried. Preserved installer SHA-256 rechecked unchanged. Root canonical asset integrity is handled separately by `scripts/verify-reference-assets.ps1`.

## Extraction provenance and limits

Portable 7-Zip 26.04 came from the [official download page](https://7-zip.org/download.html) and [official release metadata](https://api.github.com/repos/ip7z/7zip/releases/tags/26.04). Published SHA-256 digests matched `7zr.exe`, `7z2604-extra.7z` and `7z2604-x64.exe`. The 7-Zip installer was extracted as an archive, never run; no system utility was installed. Local tool hashes are in `inventory.json`. Neither client installer was executed; extracted client/configuration binaries were never executed.

The extracted `License.txt` is an informal welcome text, not a demonstrated redistribution grant. Native binary/media terms remain distinct from source-component licenses. No external code/assets were added to release exports, no upstream reference changed, and no production engine choice was made.
