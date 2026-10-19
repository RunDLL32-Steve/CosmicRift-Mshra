#!/usr/bin/env python3
"""Verify the master checklist, catalog and plan agree, and live status lines carry current counts.

Exit 0 = consistent; 1 = a live tracker disagrees (so finished work is not duplicated or forgotten).
Standard library only; read-only. Run from the repo root: python scripts/check-trackers.py
"""
import re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CHECKLIST = ROOT / "docs/original-research/FEATURE_MASTER_CHECKLIST.md"
CATALOG = ROOT / "docs/original-research/FEATURE_CATALOG.md"
PLAN = ROOT / "ROGUE_TRADER_2_PLAN.md"
LIVE = ["CONTINUATION.md", "PROJECT_START.md", "README.md", "IMPLEMENTATION_PLAN.md", "docs/IMPLEMENTATION_PLAN.md",
        "docs/original-research/IMPLEMENTATION_PLAN.md", "docs/development-log/PROGRESS.md",
        "docs/original-research/FEATURE_MASTER_CHECKLIST.md", "docs/original-research/FEATURE_CATALOG.md", "ROGUE_TRADER_2_PLAN.md"]

def read(p): return p.read_text(encoding="utf-8")

def boxes(text, pattern):
    """Return {RT-ID: checked} for lines matching pattern (first occurrence wins)."""
    out = {}
    for m in re.finditer(pattern, text, re.M):
        out.setdefault(m.group("id"), m.group("c") == "x")
    return out

check = boxes(read(CHECKLIST), r"^-\s+\[(?P<c>[ x])\] (?P<id>RT-F\d+)\b")
plan = boxes(read(PLAN), r"^-\s+\[(?P<c>[ x])\] (?P<id>RT-F\d+)\b")
cat = boxes(read(CATALOG), r'RT-F\d+ ?\[(?P<c>[ x])\]'.replace("RT-F\d+", "(?P<id>RT-F\d+)"))
total, done = len(check), sum(check.values())
openn = total - done
problems = []
for name, other in (("plan", plan), ("catalog", cat)):
    for rid, state in check.items():
        if rid in other and other[rid] != state:
            problems.append(f"{rid}: checklist={'x' if state else ' '} but {name}={'x' if other[rid] else ' '}")
    missing = [r for r in check if r not in other]
    if missing: problems.append(f"{name} is missing {len(missing)} checklist IDs (first: {missing[0]})")

want = re.compile(rf"(?<!\d){total}(?!\d)[^\n]{{0,60}}?(?<!\d){done}(?!\d)[^\n]{{0,60}}?(?<!\d){openn}(?!\d)")
for rel in LIVE:
    t = read(ROOT / rel)
    if not want.search(t):
        problems.append(f"{rel}: no status line with current counts {total}/{done}/{openn}")
    for m in re.finditer(r"(?<!\d)(\d{3}) ?(?:feature entries|catalog/master entries|stable feature IDs|features|entries)[^\n]{0,60}?(?<!\d)(\d{1,3}) ?scoped", t):
        if (int(m.group(1)), int(m.group(2))) != (total, done):
            problems.append(f"{rel}: stale count line '{m.group(0)[:70]}'")

print(f"checklist: {total} entries, {done} complete, {openn} open")
if problems:
    print("TRACKER MISMATCHES:"); [print(" -", p) for p in problems]; sys.exit(1)
print("TRACKERS_CONSISTENT")
