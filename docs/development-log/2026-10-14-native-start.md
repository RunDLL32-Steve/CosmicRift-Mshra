# Native Start/profile issue — October 14, 2026

Branch `codex/native-start-profile`, base `626a09f`; isolated worktree `.tools/issue-worktrees/native-start-profile`. See [issue handoff](../../NATIVE_START_HANDOFF.md) for exact paths, command and resume instructions.

Investigation found both available vendor engines share Welcome dialog102, enabled Start Playing ID1, default Quit ID2 and readonly welcome text; both also contain LaunchPad/ticket/name strings. These observations do not prove a missing-ticket cause or a compatible replacement engine. The pinned installer manifest still mismatches the unchanged extracted source engine.

Added an offline diagnostic and headless fixtures, without changing preparation/authentication, registry, original binaries/content or root files. `python scripts/compatibility/test-native-start.py` passed13/13. Actual static comparison report is `artifacts/native-start/review-preflight.json` in the issue worktree: historical candidate's two-literal derivation, local list, wrapper and blank INIs pass. The cleaned-up historical profile is not launch-ready evidence.

User usage checkpoint stopped all implementation/testing. **StartFixed=false; NativeEntryVerified=false; RT-F380/RT-F384 unchecked.** The agent's first commit attempt failed on Git metadata permissions; root then committed the diagnostic/WIP source as `26d3226`. Full root technical acceptance review and user real native test/merge remain pending. No further GUI interaction, push or automatic merge is authorized. Agent completion and a preservation commit alone do not establish review acceptance or a Start fix.
