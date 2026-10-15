# Native client dependency and compatibility plan

Purpose: establish whether the preserved native Infantry client can enter and reproduce the unchanged original Rogue Trader package with the pinned emulator. Godot remains a separate engineering/asset inspector. Master authority: [§1.2 canonical fidelity](../../ROGUE_TRADER_2_PLAN.md#12-user-fidelity-clarification-and-implementation-tracking--october-12-2026), [§1.3 current checkpoint](../../ROGUE_TRADER_2_PLAN.md#13-continued-exact-reproduction--october132026), and [§18 fidelity gate](../../ROGUE_TRADER_2_PLAN.md#milestone-1--flight-and-combat-prototype). Related owners: [legacy](LEGACY_COMPATIBILITY_PLAN.md), [runtime](ROGUE_RUNTIME_PLAN.md), [package audit](ROGUE_PACKAGE_AUDIT_PLAN.md), [dependency inventory](../../scripts/original-assets/DEPENDENCY_PLAN.md).

Current status, October 13, 2026: **RT-F376 static package inventory Complete; RT-F380/RT-F384 native integration/profile isolation In progress; actual entry unverified.** The approved derived native copy reached Welcome FreeInfantry. The user clicked Start without a confirmed result. Physical Escape stopped Computer Use while the user was playing another game. Do not resume desktop interaction during this documentation task. Owned client/backend stopped and unique private test namespace removed; existing profiles/original binaries unchanged.

## Active acceptance queue

- [ ] **Investigating — RT-F376 broader compatibility/RT-F380:** establish supported engine channel/build identity; engine binaries have no FileVersion/ProductVersion, and launcher/installer metadata cannot substitute for engine identity. Static inventory itself is complete.
- [ ] **In progress — RT-F384:** verify complete private profile read/write behavior and network isolation. Two redirected literals, blank directories and cleanup are narrower evidence than all dynamically constructed accesses.
- [ ] **In progress — RT-F380:** diagnose welcome/Start behavior, reach local zone selection/login, join the actual Rogue Trader arena, choose a hull with native F11 and unspectate. Server evidence recorded maximum clients 0.
- [ ] **Testing — RT-F393:** verify actual native CS_Ready behavior with the compiled derived offline patch-announcement guard.
- [ ] **Not started — RT-F387:** prove native client asset delivery/cache/decompression and full original LVL graphical trailer reception. Prepared file copies and offline roundtrips do not establish transfer.
- [ ] **Investigating — RT-F069:** resolve Portal/PrizeCrateA, cruiart/logo and numeric Ball role, plus runtime eligibility of dependencies. Five recovered help files and shared Soccer startup are complete acquisition milestones; original menu/audio behavior remains open.
- [ ] **Not started:** compare native original map/HUD/audio/palette/blit/bank/timing, flight/combat and Rogue Trader gameplay; establish whether archived SilentClientCrash reproduces on the selected build.
- [ ] **Not started:** verify durable progression, multiple clients, safe authority/transactions/shutdown and packaging before production engine selection.

No authentication ticket/account was fabricated, and no auth/security dialog may be automated. Local content/use authorization is distinct from redistribution rights.

## Completed static inventory — RT-F376

| Reference | Evidence and limit |
|---|---|
| Preserved installer | `.tools/compatibility/Infantry-Online-Setup.exe`, 6,181,473 bytes, SHA256 e3770b201bda7df06c8e1f939cc57b66f248ef58a219468c00238e7c96e3577a, installer 2.1.0.9; archive identification failed; installer never executed. |
| Official installer | [Install-FreeInfantry.exe](https://www.freeinfantry.com/download/win/latest/Install-FreeInfantry.exe), 420,002,146 bytes, SHA256 10b06a37b2bff8b2af03dc59296765d2c55ad6fb0e58247786c8ad7db2828401. NSIS-3/Deflate; 18 selected code/config/text/XML files statically extracted. Installer never executed. |
| Extracted engine used for derived bootstrap | x86 native PE infantry.exe, 2,441,216 bytes, SHA256 33d97aab9ffad1ff62252cfa0bf1acdf091b71da246460bfe74f55e3e901b1ba; no CLR, FileVersion or ProductVersion. March 9, 2010 PE timestamp is header metadata, not demonstrated release/version identity. |
| Separate launcher/wrapper | InfantryLauncher.exe 2.2.0.0 and cnc-ddraw 6.1.0.0; their versions do not identify native engine version. DirectDraw/DirectSound/WinMM/Winsock/native OS imports are static dependencies, not runtime proof. |
| Installer manifest mismatch | 397 entries; infantry.exe size agrees, expected MD5 489fb783850cb121a0c2655ac00a13be differs from extracted engine MD5 55ea5563d3c7a08b43540e03ea34ee82. Observed metadata mismatch, not a proved crash cause. |
| Other historical endpoint | [infantry.exe.gz](https://assets.freeinfantry.com/infantry.exe.gz) yields 2,618,368-byte x86 PE, SHA256 d8dd7de3233af511388356a3bbbbd4a78298af22d9b2905237f9ddb4daac733e; unversioned, April 6, 2024 header timestamp. No substitution/launch accepted for this variant. |

Vendor [download page](https://www.freeinfantry.com/download/) and [maintainer installation source](https://github.com/spiffyguy/FreeInfantry-Client-Setup/blob/main/Linux/play-on-linux-freeinfantry.sh) supplied channel evidence. Pinned [server quickstart](https://github.com/InfantryOnline/Infantry-Online-Server/blob/bb97579efeceff1c63d781d8c1bb8dd4a5b9b849/quick-start.md) describes direct Infantry.exe, blank directories and Infantry.lst. Newer launcher source refers to FreeInfantry.exe while the installer contains infantry.exe; channel reconciliation remains open.

[Static inventory](../../.tools/native-client-inspection/inventory.json) retains hashes/lengths/imports/extraction evidence. Portable 7-Zip 26.04 binaries were checked against official release digests and extracted without installing. Native License.txt is an informal welcome, not demonstrated redistribution permission. Original binary/media remain ignored local references and excluded from project release exports.

## Prepared content and profile history — RT-F380/RT-F384

Separate `.tools/native-client-inspection/local-working/client/` has a loopback-only Infantry.lst, copied native engine/wrapper/config, five authentic generic UI/audio support files, 89 unchanged canonical BLO archives, seven unchanged canonical Rogue data files and five separately sourced original help files. Initial 109 source/target SHA256 checks passed. Launcher was not copied; INI public directory/account/update URLs and credentials were blanked in the derived tree only. Embedded native directory defaults still require actual isolation proof.

Pinned launcher Register.cs and native strings show `HKCU\Software\HarmlessGames\Infantry\Profile0..5\Options`, including SDirectoryAddress/SDirectoryAddressBackup. Copying an executable is insufficient profile isolation. Earlier disposable-profile .reg files and RTLocalTestsX adapter were static proposals, never imported/launched; they are not the current accepted runtime namespace.

The implemented [prepare-native-profile.ps1](../../scripts/compatibility/prepare-native-profile.ps1) uses a fresh 13-character `RTTest` plus seven hex character namespace; actual approved run used `RTTestce5e2bb`. It copies a separate run tree, redirects exactly two 13-byte data literals at file offsets 1,127,917 and 1,128,113, retains original game identity/code/protocol/content and creates six blank-directory profiles only under that private HKCU base. It sets the copied wrapper to windowed/devmode with savesettings disabled. Original executable hash remains unchanged. This preparation command **writes a new private registry namespace**; it is not a read-only inventory tool and does not launch native UI itself.

Static proposal invariants checked unchanged PE headers/length/code and all bytes outside 26 literal bytes, no remaining ASCII/UTF16 HarmlessGames occurrences and no certificate table. Those invariants support the narrow derivation; they do not prove every native dynamic path or wrapper/OS write is covered. [Actual run preparation](../../.tools/native-client-inspection/isolated-run-preparation.json) and [cleanup](../../.tools/native-client-inspection/native-test-cleanup.json) are the current checkpoint; proposal `launch_ready=false` documents are historical preparation evidence, not an active automatic-review rejection.

## Authentic shared engine dependency — RT-F394 Complete

Full-BLO hosting exposed missing soccer.blo before listening. Unchanged rogue.cfg [Soccer] has BallCount=0 but seven sprite/sound references; pinned CfgInfo.Soccer.cs registers them unconditionally. Authentic installer member `$_0_/soccer.blo` was selectively extracted, 155,450 bytes, SHA256 93ec29ba6ea7618b6f298253fa75ef397ec934911231370d961a960f7d050784, MD5 b393ea9cfdf1c4075a210e641f4b34a6 matching installer manifest. Nine bounded BLO members include all seven required stems Ball/Trail/Shadow/Catch/Throw/Bounce/Request. Canonical ball.blo is a different archive and was not substituted.

[Shared manifest](../../.tools/native-client-inspection/native-generic-support/shared-blo-manifest.json) records provenance. Authentic Soccer was staged into separate server/native working assets; subsequent full-asset SessionHost startup passed. Canonical archive count remains 89, with one explicitly separate authentic supplemental engine BLO. This completes acquisition/member/startup acceptance, not native Soccer or original map/audio behavior.

## Actual bounded execution and cleanup

[Host readiness](../../.tools/compatibility/local-working/session-host/reports/ready.json) records one actual RT arena, full assets and real loopback endpoints 21337/21338. [Host exit](../../.tools/compatibility/local-working/session-host/reports/exit.json) records exit 0 after 120.17 seconds, maximum clients 0 and unchanged original hashes. Native PID 4088 reached welcome; backend PID 8108 was the owned bounded host. No native entry/hull/gameplay was verified.

[Cleanup](../../.tools/native-client-inspection/native-test-cleanup.json): owned native process stopped, backend stop requested/normal exit completed, private namespace removed, existing Infantry profiles not written and Computer Use stopped by physical Escape. Next executable/native check must address the pending feature with focused evidence when the user is available; documentation work launches no programs or desktop automation.

RT-F397 issue branch passes focused outgoing packet length/reliable queue checks (46 canonical packets/60 inert fixtures); see [issue log](../development-log/2026-10-14-rt-f397.md). RT-F387/F393 remain unchecked. Desktop use is prohibited for this work; native receiver/authentication/UI acceptance cannot be established by the inert checks.
