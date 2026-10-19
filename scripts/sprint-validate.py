#!/usr/bin/env python3
"""Validate the checked-out sprint branch for bugs and errors: run every check that applies to what changed.

The checks follow the AGENTS.md "Test selection" map, so nothing irrelevant runs, and every check ends as one of
pass / fail / not-verified (not-verified = needs original assets or a tool this machine lacks; never counted as pass).
Always run: git diff --check and a conflict-marker scan of the diff vs base, scripts/check-trackers.py, the
review-queue unit tests. Conditional on changed paths:
    shared/, server/RogueTrader*, tests/Unit, tests/Network   -> dotnet test tests/Unit
    client/ or shared/                                        -> scripts/verify.ps1 (build, tests, Godot smoke)
    shop patches, RuntimeChecks/, packet-length, NativeDeliveryChecks/ -> not-verified in CI (needs original assets)
Usage:  python scripts/sprint-validate.py [--root WORKTREE] [--base origin/main] [--report artifacts/sprint-validate.json] [--full]
Exit status 0 only when no check failed. Prints SPRINT_VALID or SPRINT_INVALID. --full forces verify.ps1.
Standard library only.
"""
import argparse, json, os, re, shutil, subprocess, sys, time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
results = []


def sh(name, args, timeout=1800):
    t = time.time()
    try:
        p = subprocess.run(args, cwd=ROOT, capture_output=True, text=True, timeout=timeout)
        status, tail = ("pass" if p.returncode == 0 else "fail"), (p.stdout + p.stderr).strip()[-600:]
    except (OSError, subprocess.TimeoutExpired) as e:
        status, tail = "fail", str(e)
    results.append({"check": name, "status": status, "seconds": round(time.time() - t, 1),
                    "detail": tail if status == "fail" else ""})


def note(name, status, detail=""):
    results.append({"check": name, "status": status, "seconds": 0, "detail": detail})


def main():
    global ROOT
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--base", default="origin/main")
    ap.add_argument("--report", default="artifacts/sprint-validate.json")
    ap.add_argument("--full", action="store_true")
    ap.add_argument("--root", default=ROOT, help="repository or sprint worktree to validate")
    a = ap.parse_args()
    ROOT = os.path.abspath(a.root)
    py = sys.executable
    changed = subprocess.run(["git", "diff", "--name-only", f"{a.base}...HEAD"], cwd=ROOT, capture_output=True,
                             text=True).stdout.split()
    sh("git diff --check", ["git", "diff", "--check", f"{a.base}...HEAD"])
    marker = re.compile(r"^(<{7} |>{7} |={7}$)", re.M)
    exts = (".md", ".cs", ".py", ".ps1", ".json", ".yml", ".gd", ".tscn")
    bad = [f for f in changed if f.lower().endswith(exts) and os.path.isfile(os.path.join(ROOT, f))
           and marker.search(open(os.path.join(ROOT, f), encoding="utf-8", errors="ignore").read())]
    note("conflict markers", "fail" if bad else "pass", ", ".join(bad))
    sh("check-trackers", [py, "scripts/check-trackers.py"])
    sh("review-queue tests", [py, "-m", "unittest", "scripts/test_review_queue.py"])

    def has(*pre):
        return any(f.startswith(pre) for f in changed)

    common = subprocess.run(["git", "rev-parse", "--git-common-dir"], cwd=ROOT, capture_output=True, text=True).stdout.strip()
    main_root = os.path.dirname(os.path.abspath(os.path.join(ROOT, common)))  # primary checkout owns the ignored .tools
    dotnet = next((c for c in (os.path.join(r, ".tools", "dotnet", "dotnet.exe") for r in (ROOT, main_root))
                   if os.path.exists(c)), shutil.which("dotnet"))
    if has("shared/", "server/RogueTrader", "tests/Unit", "tests/Network"):
        if dotnet:
            sh("dotnet test tests/Unit", [dotnet, "test", "tests/Unit"])
        else:
            note("dotnet test tests/Unit", "not-verified", "dotnet not found")
    pwsh = shutil.which("pwsh") or shutil.which("powershell")
    if a.full or has("client/", "shared/"):
        if pwsh and os.path.isdir(os.path.join(ROOT, ".tools")):
            sh("verify.ps1", [pwsh, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/verify.ps1"])
        elif pwsh:
            note("verify.ps1", "not-verified", "no .tools in this worktree; run bootstrap.ps1 here or rely on CI")
        else:
            note("verify.ps1", "not-verified", "PowerShell not found")
    if has("scripts/compatibility/patches/shop-", "RuntimeChecks/", "scripts/compatibility/patches/packet-length",
           "NativeDeliveryChecks/"):
        note("runtime/native-delivery checks", "not-verified",
             "needs original assets; run locally with run-runtime-checks.ps1 / run-native-delivery-checks.ps1")
    os.makedirs(os.path.dirname(os.path.join(ROOT, a.report)), exist_ok=True)
    report = {"base": a.base, "changed_files": len(changed), "results": results,
              "accepted": not any(r["status"] == "fail" for r in results)}
    json.dump(report, open(os.path.join(ROOT, a.report), "w"), indent=2)
    for r in results:
        print(f"{r['status'].upper():13} {r['check']} ({r['seconds']}s)" + (f"\n    {r['detail']}" if r["detail"] else ""))
    print("SPRINT_VALID" if report["accepted"] else "SPRINT_INVALID")
    return 0 if report["accepted"] else 1


if __name__ == "__main__":
    sys.exit(main())
