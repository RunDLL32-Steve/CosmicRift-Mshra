Review status: ready-for-review

# Branch log: claude/project-thread-d15n1f (release the C1 claim)

Scope: release the abandoned queue C1 claim so Codex can pick up C1 (owner request, Steve, November 3, 2026). Docs only. G6, G9 and C7 are not touched; the G6 claim is handled separately.

Live check (November 3, 2026, `git fetch` of every origin branch and the open PR list):

-   `origin/claim/c1-blo-reader` holds two empty commits, `6bb12cf` and `03ba4c3` ("CLAIM C1 ... by local", October 15, 2026), on top of pre-rewrite history that shares no merge base with current main. No code, no log and no PR.
-   No other origin branch names C1 or has a commit message naming C1 (C10 to C19 excluded). Open PRs are #108 (mineral cap) and #109 (zone assets); neither touches C1.

Changes: TASK_QUEUE.md C1 row to "OPEN (claim abandoned ...)", a release note under the historical claim notes, and the C1 brief's status and dependency lines. Per the release procedure in TASK_QUEUE.md, the owner-authorised empty commit beginning `RELEASE C1` is pushed to `claim/c1-blo-reader`, so the branch no longer claims C1.

Checks: `python scripts/check-trackers.py`.

Resume point: review, then Steve merges; Codex takes C1 after that.
