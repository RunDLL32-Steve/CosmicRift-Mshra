#!/usr/bin/env python3
"""Build the sprint integration branch: merge EVERY open pull request into one branch to prove they build together.

The sprint branch is an integration check, not a release path. It starts from origin/main, merges each open PR head
(any branch prefix, any label, draft or not) with a merge commit, in ascending PR number, and records which PRs merged
cleanly and which conflicted (a conflicting PR is aborted, listed with its conflicting files, and the rest continue).
Nothing is pushed unless --push is given, only sprint/* branches are ever pushed, and main is never touched.
Review status and the merge gate in AGENTS.md are unchanged: reaching main still needs each PR's own approval.

Usage (repository root; authenticated `gh`):
    python scripts/sprint-integrate.py                       # sprint/YYYY-MM-DD in .tools/issue-worktrees, no push
    python scripts/sprint-integrate.py --name sprint/2026-10-19 --push
    python scripts/sprint-integrate.py --prs 77 78 79        # explicit subset
Writes artifacts/sprint-integrate.json and docs/development-log/sprint/<name>.md (committed on the sprint branch).
Then run `python scripts/sprint-validate.py` inside the sprint worktree. Standard library only.
"""
import argparse, datetime, json, os, subprocess, sys

DEFAULT_REPO = "RunDLL32-Steve/CosmicRift-Mshra"


def run(args, cwd=None, check=True):
    p = subprocess.run(args, cwd=cwd, capture_output=True, text=True)
    if check and p.returncode:
        sys.exit(f"command failed: {' '.join(args)}\n{p.stdout}\n{p.stderr}")
    return p


def open_prs(repo):
    out = run(["gh", "api", "--paginate", f"repos/{repo}/pulls?state=open&per_page=100"]).stdout
    text = out.replace("][", ",")  # --paginate prints one JSON array per page
    return [{"number": p["number"], "title": p["title"], "head": p["head"]["ref"], "base": p["base"]["ref"],
             "draft": p["draft"], "labels": [l["name"] for l in p["labels"]]} for p in json.loads(text)]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--repo", default=DEFAULT_REPO)
    ap.add_argument("--name", default="sprint/" + datetime.date.today().isoformat())
    ap.add_argument("--prs", nargs="*", type=int, help="explicit PR numbers (default: every open PR)")
    ap.add_argument("--worktree-root", default=".tools/issue-worktrees")
    ap.add_argument("--push", action="store_true", help="push the sprint/* branch to origin")
    a = ap.parse_args()
    if not a.name.startswith("sprint/"):
        sys.exit("branch name must start with sprint/")
    run(["git", "fetch", "origin", "main"])
    prs = sorted(open_prs(a.repo), key=lambda p: p["number"])
    if a.prs:
        prs = [p for p in prs if p["number"] in a.prs]
    wt = os.path.abspath(os.path.join(a.worktree_root, a.name.replace("/", "-")))
    if os.path.exists(wt):
        sys.exit(f"worktree already exists: {wt} (remove it or pick another --name)")
    run(["git", "worktree", "add", "-B", a.name, wt, "origin/main"])
    merged, conflicted, failed = [], [], []
    for p in prs:
        n = p["number"]
        if run(["git", "fetch", "origin", f"pull/{n}/head"], cwd=wt, check=False).returncode:
            failed.append({**p, "reason": "fetch failed"})
            continue
        m = run(["git", "merge", "--no-ff", "--no-edit", "-m", f"SPRINT merge PR #{n}: {p['title']}", "FETCH_HEAD"],
                cwd=wt, check=False)
        if m.returncode == 0:
            merged.append(p)
        else:
            files = run(["git", "diff", "--name-only", "--diff-filter=U"], cwd=wt, check=False).stdout.split()
            run(["git", "merge", "--abort"], cwd=wt, check=False)
            conflicted.append({**p, "files": files, "reason": (m.stdout + m.stderr).strip()[-300:]})
    head = run(["git", "rev-parse", "HEAD"], cwd=wt).stdout.strip()
    report = {"branch": a.name, "base": "origin/main", "head": head, "worktree": wt, "merged": merged,
              "conflicted": conflicted, "failed": failed}
    os.makedirs("artifacts", exist_ok=True)
    json.dump(report, open("artifacts/sprint-integrate.json", "w"), indent=2)
    logdir = os.path.join(wt, "docs", "development-log", "sprint")
    os.makedirs(logdir, exist_ok=True)
    lines = ([f"Sprint integration: {a.name}", "", f"Base: origin/main. Head: {head[:7]} (before this log).", "",
              "## Merged cleanly"]
             + [f"- #{p['number']} {p['title']} ({p['head']})" for p in merged]
             + ["", "## Conflicted (not in sprint; author must rebase or resolve)"]
             + [f"- #{p['number']} {p['title']}: {', '.join(p['files']) or p['reason']}" for p in conflicted]
             + ["", "## Failed to fetch"] + [f"- #{p['number']} {p['title']}" for p in failed]
             + ["", "Validate with `python scripts/sprint-validate.py`. This branch is an integration check only "
                "and never merges to main by itself."])
    open(os.path.join(logdir, a.name.replace("/", "-") + ".md"), "w", encoding="utf-8").write("\n".join(lines) + "\n")
    run(["git", "add", "docs/development-log/sprint"], cwd=wt)
    run(["git", "commit", "-q", "-m", f"SPRINT log {a.name}"], cwd=wt, check=False)
    if a.push:
        run(["git", "push", "-u", "origin", a.name], cwd=wt)
    print("merged:", [p["number"] for p in merged], "conflicted:", [(p["number"], p["files"]) for p in conflicted],
          "failed:", [p["number"] for p in failed], "worktree:", wt)
    return 0 if not conflicted and not failed else 2


if __name__ == "__main__":
    sys.exit(main())
