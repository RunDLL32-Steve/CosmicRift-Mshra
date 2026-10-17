#!/usr/bin/env python3
"""Local-hold gate for the cloud routines.

The owner says "I am local" -> a session runs `cloud-gate.py on`, which opens an issue labelled
`local-hold`. Cloud routines run `cloud-gate.py check` FIRST; exit code 10 / output SKIP means stop
immediately (no tokens spent). `off` closes the issue when the owner is done.

  check   prints HOLD_ACTIVE (exit 10) or CLEAR (exit 0)
  on      sets the hold (idempotent)
  off     clears the hold (idempotent)
"""
import json, subprocess, sys

REPO = "RunDLL32-Steve/CosmicRift-Mshra"
LABEL = "local-hold"
TITLE = "LOCAL HOLD: owner is working locally, cloud routines must skip"


def gh(*args):
    r = subprocess.run(["gh", "api", *args], capture_output=True, text=True)
    if r.returncode != 0:
        raise SystemExit(f"gh api failed: {r.stderr.strip()}")
    return json.loads(r.stdout) if r.stdout.strip() else None


def open_holds():
    issues = gh(f"repos/{REPO}/issues?labels={LABEL}&state=open&per_page=20")
    return [i for i in issues if "pull_request" not in i]


def main():
    cmd = sys.argv[1] if len(sys.argv) > 1 else "check"
    if cmd == "check":
        if open_holds():
            print("HOLD_ACTIVE: owner is local, skip this run")
            return 10
        print("CLEAR")
        return 0
    if cmd == "on":
        if not open_holds():
            try:
                gh(f"repos/{REPO}/labels/{LABEL}")
            except SystemExit:
                gh(f"repos/{REPO}/labels", "-f", f"name={LABEL}", "-f", "color=5319e7")
            gh(f"repos/{REPO}/issues", "-f", f"title={TITLE}", "-f", f"labels[]={LABEL}",
               "-f", "body=Set by scripts/cloud-gate.py on. Cloud routines skip while this is open.")
        print("HOLD_SET")
        return 0
    if cmd == "off":
        for i in open_holds():
            gh(f"repos/{REPO}/issues/{i['number']}", "-X", "PATCH", "-f", "state=closed")
        print("HOLD_CLEARED")
        return 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main())
