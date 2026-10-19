#!/usr/bin/env python3
"""Prettier gate for PR validation.

Checks (or with --fix applies) the repo's Prettier config (.prettierrc) to the
md/json/yml/yaml files this branch changed relative to origin/main. Files in
.prettierignore (canonical originals, manifests, toolchain locks) are skipped
by Prettier itself. Use --all to cover every tracked file.

Prints FORMAT_CLEAN and exits 0 when nothing needs formatting, otherwise lists
the offending files, prints FORMAT_DIRTY and exits 1. Needs `npm ci` first.
"""
import argparse
import os
import subprocess
import sys

EXTS = (".md", ".json", ".yml", ".yaml")
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def git(*args):
    r = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True)
    if r.returncode != 0:
        sys.exit(f"git {' '.join(args)} failed: {r.stderr.strip()}")
    return r.stdout


def changed_since(base):
    """Files changed on this branch vs base. CI checkouts are shallow and may lack the base ref: fetch it, and when
    there is no common ancestor in the shallow history compare the trees directly (two-dot)."""
    if subprocess.run(["git", "rev-parse", "--verify", "--quiet", base], cwd=ROOT, capture_output=True).returncode:
        remote, _, branch = base.partition("/")
        subprocess.run(["git", "fetch", "--no-tags", "--depth=1", remote, f"{branch}:refs/remotes/{remote}/{branch}"], cwd=ROOT, capture_output=True)
        if subprocess.run(["git", "rev-parse", "--verify", "--quiet", base], cwd=ROOT, capture_output=True).returncode:
            sys.exit(f"cannot resolve {base}; fetch it first (git fetch origin main)")
    mb = subprocess.run(["git", "merge-base", base, "HEAD"], cwd=ROOT, capture_output=True, text=True)
    ref = mb.stdout.strip() if mb.returncode == 0 and mb.stdout.strip() else base
    return git("diff", "--name-only", "--diff-filter=d", ref).splitlines()


def target_files(base, everything):
    if everything:
        names = git("ls-files").splitlines()
    else:
        names = changed_since(base)
        names += git("ls-files", "--others", "--exclude-standard").splitlines()
    return sorted(
        {
            n
            for n in names
            if n.lower().endswith(EXTS) and os.path.isfile(os.path.join(ROOT, n))
        }
    )


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--fix", action="store_true", help="rewrite files")
    p.add_argument("--all", action="store_true", help="all tracked files")
    p.add_argument("--base", default="origin/main")
    a = p.parse_args()
    exe = os.path.join(ROOT, "node_modules", "prettier", "bin", "prettier.cjs")
    if not os.path.isfile(exe):
        sys.exit("Prettier missing: run `npm ci` in the repository root.")
    files = target_files(a.base, a.all)
    if not files:
        print("FORMAT_CLEAN (no md/json/yml files in scope)")
        return 0
    mode = "--write" if a.fix else "--list-different"
    r = subprocess.run(
        ["node", exe, mode, "--end-of-line", "auto", "--ignore-unknown", "--log-level", "warn", *files],
        cwd=ROOT,
        capture_output=True,
        text=True,
    )
    out = (r.stdout + r.stderr).strip()
    if a.fix:
        if r.returncode != 0:
            print(out)
            return r.returncode
        print("Formatted files in scope; review `git diff` and commit.")
        print("FORMAT_CLEAN")
        return 0
    if r.returncode == 0:
        print(f"FORMAT_CLEAN ({len(files)} files checked)")
        return 0
    print(out)
    print("FORMAT_DIRTY: run `python scripts/format-check.py --fix` and commit.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
