> Historical snapshot. Preserved for dated evidence; its instructions, counts, branch state and next steps may be superseded. Use [current continuation](../../../CONTINUATION.md), [task queue](../../../TASK_QUEUE.md) and [documentation index](../../README.md) for current work.

# Native Start/profile handoff — October 14, 2026

Issue branch: `codex/native-start-profile`, based on `626a09f`. Worktree: `C:\Repos\CosmicRift\.tools\issue-worktrees\native-start-profile`. Diagnostic/WIP source committed by root as `26d3226` after the agent's Git metadata permission failure. Implementation/testing stopped at the user's usage checkpoint. Root's full technical acceptance review, user native test and user merge remain pending; this commit is not an accepted Start fix. No further GUI interaction, push or automatic merge is authorized.

## Current source state

-   `scripts/compatibility/inspect-native-start.py`: new standard-library, offline diagnostic. Reads candidate/source PE resources, known binary hashes, the exact two-literal private namespace derivation, the single loopback zone-list entry, relevant wrapper overrides and blank launcher INIs. Reports credential presence flags only; never values. Optional reports must be new JSON files under this checkout's ignored `.tools/` or `artifacts/`. It launches no native client, touches no registry and uses no network.
-   `scripts/compatibility/test-native-start.py`: 13 headless fixture tests for standard/extended dialog templates, disabled controls, malformed PE/resource bounds/cycles, namespace changes outside the approved literals, unknown-build rejection, local endpoint validation, manifest identity, wrapper overrides, privacy and explicit unverified status.
-   No preparation script, original binary, authentication code, ticket, credential, canonical content or root source file was changed. No build, simulation, native launch, profile write, commit or push was performed for this branch task.

## Evidence and remaining uncertainty

Both existing vendor variants were read locally: installer engine SHA256 `33d97aab9ffad1ff62252cfa0bf1acdf091b71da246460bfe74f55e3e901b1ba` (2,441,216 bytes), and separate endpoint engine SHA256 `d8dd7de3233af511388356a3bbbbd4a78298af22d9b2905237f9ddb4daac733e` (2,618,368 bytes). Both expose the same Welcome resource: dialog102, language1033, enabled Start Playing ID1/style`0x50010000`, Quit ID2/style`0x50010001` (template default button), and large readonly multiline text control1001/style`0x50a10804`. Both contain the LaunchPad warning and ticket/name literals. Static resources do not establish executed event/authentication behavior; swapping builds has no demonstrated Start benefit.

The pinned quick-start describes direct native launch, blank directory fields and local `Infantry.lst`. Launcher source separately obtains a real account ticket before passing ticket/name arguments. Server standalone acceptance does not prove the native client's launch prerequisites. No account/ticket was fabricated. An authentication/security prompt belongs to the user.

Read-only actual preflight of historical `RTTest967a188` passed static preparation checks: candidate differs only in the two approved namespace literals; list points to `127.0.0.1:21337`; wrapper/INI checks pass. Installer manifest MD5 still does not match the unchanged installer engine. That historical private registry key was cleaned up; this report does not authorize or establish readiness to launch the old directory. Complete profile isolation and supported channel remain unverified.

**StartFixed=false; NativeEntryVerified=false. RT-F380 and RT-F384 remain unchecked.** Exact Start no-op cause is unproven. This branch is a diagnostic, not a Start fix.

## Verification already run

`python scripts/compatibility/test-native-start.py` — **13/13 pass** (headless; no native files or registry required).

Actual offline report: `artifacts/native-start/review-preflight.json` (ignored, in this worktree). Command already run from the worktree:

```powershell
python scripts/compatibility/inspect-native-start.py --client-directory C:\Repos\CosmicRift\.tools\native-client-inspection\isolated-runtime\RTTest967a188 --source-executable C:\Repos\CosmicRift\.tools\native-client-inspection\local-working\client\infantry.exe --comparison-executable C:\Repos\CosmicRift\.tools\native-client-inspection\live-infantry.exe --installer-manifest 'C:\Repos\CosmicRift\.tools\native-client-inspection\current-payload\$_0_\manifest.xml' --output artifacts/native-start/review-preflight.json
```

The report writer refuses to overwrite that existing report. Earlier exploratory reports in the same ignored folder are superseded by `review-preflight.json`.

## Resume step

Root first completes technical review of the two committed Python files and this evidence, including diagnostic error/output guards and the distinction between static checks and readiness. Do not resume coding, checks or native interaction merely because this handoff exists. The user owns real native testing and merging. A later user test should use a fresh approved private profile, the canonical executable name and its containing directory as launch working directory; explicitly click visible Start rather than assuming Enter activates it, and record the actual next window/message. Do not supply fabricated auth arguments or modify authentication to get past a prompt.
