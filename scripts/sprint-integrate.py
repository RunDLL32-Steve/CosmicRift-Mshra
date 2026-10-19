#!/usr/bin/env python3
"""Build the sprint integration branch: merge EVERY open pull request into one branch to prove they build together.

The sprint branch is an integration check, not a release path. It starts from origin/main, merges each open PR head
(any branch prefix, any label, draft or not) with a merge commit, in ascending PR number, and records which PRs merged
cleanly and which conflicted (a conflicting PR is aborted, listed with its conflicting files, and the rest continue).
Nothing is pushed unless --push is given. Only sprint/* branches are ever pushed, never forced, and main is never
touched. Review status and the merge gate in AGENTS.md are unchanged. PR code is untrusted: it is merged and
validated locally in a throwaway worktree, nothing from a PR is executed by this script itself.

Usage (any directory inside the repository; authenticated `gh`):
    python scripts/sprint-integrate.py                       # sprint/YYYY-MM-DD-HHMM in .tools/issue-worktrees, no push
    python scripts/sprint-integrate.py --name sprint/2026-10-19-a --push
    python scripts/sprint-integrate.py --prs 77 78 79        # explicit subset
Refuses to reuse an existing local branch or worktree of the same name (use a new --name, or --reset to replace it).
Writes <repo>/artifacts/sprint-integrate.json and docs/development-log/sprint/<name>.md (committed on the sprint
branch). Then run: python scripts/sprint-validate.py --root <worktree> --integrate-report artifacts/sprint-integrate.json
Standard library only.
"""
import argparse, datetime, json, os, subprocess, sys

DEFAULT_REPO = "RunDLL32-Steve/CosmicRift-Mshra"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def run(args, cwd=None, check=True):
    p = subprocess.run(args, cwd=cwd or ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and p.returncode:
        sys.exit(f"command failed: {' '.join(args)}\n{p.stdout}\n{p.stderr}")
    return p


def parse_pages(text):
    """`gh api --paginate` prints one JSON array per page; decode them one after another (titles may contain '][')."""
    dec, i, out = json.JSONDecoder(), 0, []
    while i < len(text):
        while i < len(text) and text[i].isspace():
            i += 1
        if i >= len(text):
            break
        page, i = dec.raw_decode(text, i)
        out.extend(page)
    return out


def open_prs(repo):
    out = run(["gh", "api", "--paginate", f"repos/{repo}/pulls?state=open&per_page=100"]).stdout
    return [{"number": p["number"], "title": p["title"], "head": p["head"]["ref"], "base": p["base"]["ref"],
             "draft": p["draft"], "labels": [l["name"] for l in p["labels"]]} for p in parse_pages(out)]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--repo", default=DEFAULT_REPO)
    ap.add_argument("--name", default="sprint/" + datetime.datetime.now().strftime("%Y-%m-%d-%H%M"))
    ap.add_argument("--prs", nargs="*", type=int, help="explicit PR numbers (default: every open PR)")
    ap.add_argument("--worktree-root", default=os.path.join(ROOT, ".tools", "issue-worktrees"))
    ap.add_argument("--push", action="store_true", help="push the sprint/* branch to origin (never forced)")
    ap.add_argument("--reset", action="store_true", help="replace an existing local branch and worktree of this name")
    a = ap.parse_args()
    if not a.name.startswith("sprint/") or ".." in a.name or " " in a.name:
        sys.exit("branch name must start with sprint/ and contain no spaces or '..'")
    run(["git", "fetch", "origin", "main"])
    prs = sorted(open_prs(a.repo), key=lambda p: p["number"])
    if a.prs:
        prs = [p for p in prs if p["number"] in a.prs]
    wt = os.path.abspath(os.path.join(a.worktree_root, a.name.replace("/", "-")))
    exists = run(["git", "rev-parse", "--verify", "--quiet", f"refs/heads/{a.name}"], check=False).returncode == 0
    if (exists or os.path.exists(wt)) and not a.reset:
        sys.exit(f"branch or worktree already exists ({a.name}, {wt}); pick another --name or pass --reset")
    if a.reset:
        run(["git", "worktree", "remove", "--force", wt], check=False)
        run(["git", "branch", "-D", a.name], check=False)
    run(["git", "worktree", "add", "--no-track", "-b", a.name, wt, "origin/main"])
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
            files = run(["git", "-c", "core.quotepath=false", "diff", "--name-only", "--diff-filter=U"], cwd=wt,
                        check=False).stdout.splitlines()
            run(["git", "merge", "--abort"], cwd=wt, check=False)
            conflicted.append({**p, "files": files, "reason": (m.stdout + m.stderr).strip()[-300:]})
    head = run(["git", "rev-parse", "HEAD"], cwd=wt).stdout.strip()
    report = {"branch": a.name, "base": "origin/main", "head": head, "worktree": wt,
              "open_prs": [p["number"] for p in prs], "merged": merged, "conflicted": conflicted, "failed": failed}
    os.makedirs(os.path.join(ROOT, "artifacts"), exist_ok=True)
    rpath = os.path.join(ROOT, "artifacts", "sprint-integrate.json")
    with open(rpath, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    logdir = os.path.join(wt, "docs", "development-log", "sprint")
    os.makedirs(logdir, exist_ok=True)
    lines = ([f"Sprint integration: {a.name}", "", f"Base: origin/main. Head: {head[:7]} (before this log).", "",
              "## Merged cleanly"]
             + [f"- #{p['number']} {p['title']} ({p['head']})" for p in merged]
             + ["", "## Conflicted (not in sprint; author must rebase or resolve)"]
             + [f"- #{p['number']} {p['title']}: {', '.join(p['files']) or p['reason']}" for p in conflicted]
             + ["", "## Failed to fetch"] + [f"- #{p['number']} {p['title']}" for p in failed]
             + ["", "Validate with `python scripts/sprint-validate.py --root <worktree> --integrate-report "
                "artifacts/sprint-integrate.json`. This branch is an integration check only and never merges to main "
                "by itself."])
    with open(os.path.join(logdir, a.name.replace("/", "-") + ".md"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    run(["git", "add", "docs/development-log/sprint"], cwd=wt)
    run(["git", "commit", "-q", "-m", f"SPRINT log {a.name}"], cwd=wt, check=False)
    if a.push:
        run(["git", "push", "-u", "origin", a.name], cwd=wt)  # refspec is the sprint/* branch only; never forced
    print("merged:", [p["number"] for p in merged], "conflicted:", [(p["number"], p["files"]) for p in conflicted],
          "failed:", [p["number"] for p in failed], "worktree:", wt, "report:", rpath)
    return 0 if not conflicted and not failed else 2


if __name__ == "__main__":
    sys.exit(main())
