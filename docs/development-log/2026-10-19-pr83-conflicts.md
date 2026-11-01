Review status: needs-human

# PR 83 conflict repair — October 19, 2026

Scope: merge current main `1ce1d21` into `local/prettier-format-v2`, preserving the repository-wide Prettier change and main's newer sprint rules and G9 evidence. No RT-ID acceptance or gameplay behavior changes. The prior review of `9a38f23` does not approve the new head.

Resolved `AGENTS.md`, `docs/development-log/PROGRESS.md` and `docs/tasks/g9.md` from main's substantive text, then applied the existing pinned Prettier 3.3.3 configuration. Automatically merged overlaps were checked too. All 16 paths changed on main since `540b624` match main exactly, or match Prettier's output from main exactly. This includes unchanged ArenaProbe code and sprint Python scripts. Five reformatted JSON data files retain identical parsed values.

Focused verification: `npm ci --ignore-scripts --no-audit --no-fund` succeeded; `format-check.py --all` printed `FORMAT_CLEAN (280 files checked)`; `check-trackers.py` printed `TRACKERS_CONSISTENT` (397 entries, 22 scoped completions, 375 open); all 22 review-queue tests passed. Local comparison evidence: `artifacts/pr83-conflict-evidence.json` (ignored, reproducible by comparing each main-changed file with main or its pinned Prettier output).

No gameplay simulations, original-asset checks or native runtime/delivery checks were run: this repair resolves documentation conflicts and reapplies formatting, while imported implementation is unchanged from main. Sprint integration/validation and fresh independent review remain to be recorded before the ready marker. The branch retains `needs-human` because the PR touches protected paths, including `AGENTS.md`, `scripts/verify.ps1` and `scripts/check-trackers.py`. No merge to main is authorized by this repair.

Coordination: the independent scope audit identified the same three conflicted files and eight overlapping paths. The coordinator reviewed those findings and mechanically checked all 16 main-changed paths. Repository-preferred Claude models are unavailable in this environment; the coding audit uses explicitly selected GPT-6.1-sol at High effort. A separate fresh peer will review the repair before handoff.

Resume point: finish the sprint attempt, record its actual verdict and any limitations, obtain the bounded independent repair review, publish the final evidence and BBQ marker to the existing PR branch, and verify GitHub reports no merge conflicts. Full PR approval and protected-path owner merge remain separate.

## Round 6 follow-up — November 1, 2026

Review round 6 (PR review on `0dfcfc2`) confirmed the October 19 repair: all 16 main-changed paths compare equal to main after normalising Prettier markup, and `scripts/ci/sprint.workflow.yml` parses to the same YAML object. It asked for two fixes, done on this branch at the owner's request:

-   R6-1: merged main `d865a46` (#92 and the nine oracle PRs #77-#80, #87-#91). Main's 9 new or changed markdown files were not Prettier-clean; `format-check.py --all --fix` reformatted them. Each renders to identical HTML before and after (markdown-it 15.0.3).
-   R6-2: this record, the ticked plan.d box and a BBQ marker.
-   R3-1 (round 3 nit): `format-check.py` now reconfigures stdout/stderr to UTF-8 so a non-ASCII dirty file name is printed instead of crashing on a cp1252 console.

Sprint: the only other open PRs, #84 and #53, conflict with main in `AGENTS.md` on their own (not caused by this branch); both are labelled `review-changes-requested`.
