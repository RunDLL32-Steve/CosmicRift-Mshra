#!/usr/bin/env python3
"""Validate the checked-out sprint branch for bugs and errors: run every check that applies to what changed.

Checks follow the AGENTS.md "Test selection" map. Every check ends as pass / fail / not-verified (needs original
assets, tooling or a runtime this machine lacks). Not-verified is never a pass:
    SPRINT_VALID       every applicable check passed, nothing unverified, integration report clean   (exit 0)
    SPRINT_INCOMPLETE  nothing failed but something is not-verified, or no integration report given (exit 2)
    SPRINT_INVALID     a check failed, or the integration report has conflicts / failed fetches     (exit 1)
Always run: git diff --check, conflict-marker scan, scripts/check-trackers.py, the review-queue unit tests.
Conditional on changed paths (vs --base):
    shared/, server/RogueTrader*, tests/Unit, tests/Network   -> dotnet test tests/Unit (isolated .tools environment)
    client/ (or --full)                                       -> scripts/verify.ps1 (build, tests, Godot smoke)
    scripts/compatibility/*native-start*                      -> python scripts/compatibility/test-native-start.py
    shop patches, RuntimeChecks/, null-script capacity, radar routing -> not-verified (needs original assets)
    packet-length patch, NativeDeliveryChecks/, asset packet code     -> not-verified (needs original assets)
Usage:  python scripts/sprint-validate.py [--root WORKTREE] [--base origin/main] [--full]
                                        [--integrate-report artifacts/sprint-integrate.json] [--report PATH]
Report: <root>/artifacts/sprint-validate.json. Standard library only.
"""
import argparse, json, os, re, shutil, subprocess, sys, time

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROOT = HERE
results = []


def sh(name, args, env=None, timeout=1800):
    t = time.time()
    try:
        p = subprocess.run(args, cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace",
                           timeout=timeout, env=env)
        status, tail = ("pass" if p.returncode == 0 else "fail"), ((p.stdout or "") + (p.stderr or "")).strip()[-600:]
    except (OSError, subprocess.TimeoutExpired) as e:
        status, tail = "fail", str(e)
    results.append({"check": name, "status": status, "seconds": round(time.time() - t, 1),
                    "detail": tail if status == "fail" else "", "out": tail})


def note(name, status, detail=""):
    results.append({"check": name, "status": status, "seconds": 0, "detail": detail})


def git(*args):
    return subprocess.run(["git", "-c", "core.quotepath=false", *args], cwd=ROOT, capture_output=True, text=True,
                          encoding="utf-8", errors="replace").stdout


def main():
    global ROOT
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", default=HERE, help="repository or sprint worktree to validate")
    ap.add_argument("--base", default="origin/main")
    ap.add_argument("--full", action="store_true")
    ap.add_argument("--integrate-report", help="artifacts/sprint-integrate.json from sprint-integrate.py")
    ap.add_argument("--report", default=None)
    a = ap.parse_args()
    ROOT = os.path.abspath(a.root)
    py = sys.executable
    changed = git("diff", "--name-only", f"{a.base}...HEAD").splitlines()
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

    def has_any(*subs):
        return any(s in f for f in changed for s in subs)

    # integration report: conflicts, omitted PRs and an empty sprint must not pass
    report_path = a.integrate_report
    if not report_path:  # committed by sprint-integrate.py for this sprint branch
        branch = git("rev-parse", "--abbrev-ref", "HEAD").strip()
        cand = os.path.join(ROOT, "docs", "development-log", "sprint", branch.replace("/", "-") + ".json")
        report_path = cand if branch.startswith("sprint/") and os.path.isfile(cand) else None
    if report_path:
        try:
            with open(report_path, encoding="utf-8") as fh:
                rep = json.load(fh)
        except (OSError, ValueError) as e:
            rep = {"merged": [], "failed": [{"number": 0, "title": f"unreadable integration report: {e}"}]}
        problems = [f"#{p['number']} conflicted" for p in rep.get("conflicted", [])] + \
                   [f"#{p['number']} failed to fetch" for p in rep.get("failed", [])]
        if not rep.get("merged"):
            problems.append("no PR was merged into the sprint")
        note("sprint integration report", "fail" if problems else "pass", "; ".join(problems))
    else:
        note("sprint integration report", "not-verified", "no integration report (docs/development-log/sprint/<branch>.json or --integrate-report); omitted PRs cannot be detected")

    common = git("rev-parse", "--git-common-dir").strip()
    if not common:
        sys.exit("not inside a git repository")
    main_root = os.path.dirname(os.path.abspath(os.path.join(ROOT, common)))  # the primary checkout owns .tools
    tools = next((r for r in (ROOT, main_root) if os.path.exists(os.path.join(r, ".tools", "dotnet", "dotnet.exe"))), None)
    dotnet = os.path.join(tools, ".tools", "dotnet", "dotnet.exe") if tools else shutil.which("dotnet")
    if has("shared/", "server/RogueTrader", "tests/Unit", "tests/Network"):
        if dotnet:
            env = dict(os.environ)
            if tools:  # same isolation as scripts/environment.ps1
                env["DOTNET_ROOT"] = os.path.join(tools, ".tools", "dotnet")
                env["DOTNET_CLI_HOME"] = os.path.join(tools, ".tools", "dotnet-home")
                env["NUGET_PACKAGES"] = os.path.join(tools, ".tools", "nuget")
                env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
                env["DOTNET_NOLOGO"] = "1"
                env["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1"
            sh("dotnet test tests/Unit", [dotnet, "test", "tests/Unit"], env=env)
            m = re.search(r"Passed!\s*-\s*Failed:\s*0,\s*Passed:\s*(\d+)", results[-1].get("out", ""))
            if results[-1]["status"] == "pass" and not m:
                results[-1].update(status="fail", detail="dotnet test passed but no passing test count was found (zero tests?)")
        else:
            note("dotnet test tests/Unit", "not-verified", "dotnet not found")
    pwsh = shutil.which("pwsh") or shutil.which("powershell")
    if a.full or has("client/"):
        if pwsh and os.path.isdir(os.path.join(ROOT, ".tools")):
            sh("verify.ps1", [pwsh, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/verify.ps1"])
        elif pwsh:
            note("verify.ps1", "not-verified", "no .tools in this worktree; run bootstrap.ps1 here or rely on CI")
        else:
            note("verify.ps1", "not-verified", "PowerShell not found")
    if has_any("native-start"):
        sh("test-native-start.py", [py, "scripts/compatibility/test-native-start.py"])
    asset_paths = ("scripts/compatibility/patches/shop-", "RuntimeChecks/", "null-script", "radar",
                   "scripts/compatibility/patches/packet-length", "NativeDeliveryChecks/", "asset-packet", "AssetPacket")
    if has_any(*asset_paths):
        note("runtime/native-delivery checks", "not-verified",
             "needs original assets; run locally with run-runtime-checks.ps1 / run-native-delivery-checks.ps1")
    pys = [f for f in changed if f.endswith(".py") and os.path.isfile(os.path.join(ROOT, f))]
    if pys:
        sh("py_compile changed .py", [py, "-m", "py_compile", *pys])
    covered = ("shared/", "server/", "tests/", "client/", "docs/", "data/", "scripts/", "global.json", "package", ".prettier",
               ".gitignore", ".editorconfig")
    unmapped = [f for f in changed if not f.lower().endswith(".md") and not f.startswith(covered)]
    if unmapped:
        note("unmapped changed paths", "not-verified", "no Test-selection row covers: " + ", ".join(unmapped[:10]))
    failed = any(r["status"] == "fail" for r in results)
    unverified = any(r["status"] == "not-verified" for r in results)
    verdict = "SPRINT_INVALID" if failed else "SPRINT_INCOMPLETE" if unverified else "SPRINT_VALID"
    rpath = a.report or os.path.join(ROOT, "artifacts", "sprint-validate.json")
    os.makedirs(os.path.dirname(rpath), exist_ok=True)
    with open(rpath, "w", encoding="utf-8") as f:
        json.dump({"base": a.base, "changed_files": len(changed), "results": results, "verdict": verdict}, f, indent=2)
    for r in results:
        print(f"{r['status'].upper():13} {r['check']} ({r['seconds']}s)" + (f"\n    {r['detail']}" if r["detail"] else ""))
    print(verdict)
    return {"SPRINT_VALID": 0, "SPRINT_INVALID": 1, "SPRINT_INCOMPLETE": 2}[verdict]


if __name__ == "__main__":
    sys.exit(main())
