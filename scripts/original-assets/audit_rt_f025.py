#!/usr/bin/env python3
"""Independent audit for RT-F025 (data only): CFG/rogue.cfg [Uiart], [Uiwav], [Render] and BLO filename existence.

Stdlib only. Usage: audit_rt_f025.py [assets_root]   (default: $RT_ORIGINAL_ASSETS_ROOT)
Prints the values pinned by tests/Unit/OriginalUiArtTests.cs. BLO member existence is NOT checked (BLO content is LFS-only).
"""
import hashlib, os, sys

root = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("RT_ORIGINAL_ASSETS_ROOT")
if not root:
    sys.exit("usage: audit_rt_f025.py <assets_root> (or set RT_ORIGINAL_ASSETS_ROOT)")
path = os.path.join(root, "CFG", "rogue.cfg")
data = open(path, "rb").read()
print("rogue.cfg bytes", len(data), "sha256", hashlib.sha256(data).hexdigest())
lines = data.decode("latin-1").split("\n")

secs = {}
cur = None
for i, l in enumerate(lines, 1):
    l = l.rstrip("\r")
    if not l:
        continue
    if l.startswith("["):
        cur = l[1:l.index("]")]
        secs.setdefault(cur, {"line": i, "entries": []})
        continue
    k, _, v = l.partition("=")
    secs[cur]["entries"].append((i, k, v))

blo = {f.lower() for f in os.listdir(os.path.join(root, "BLO"))}
print("BLO dir files", len(blo))
for name in ("Uiart", "Uiwav"):
    s = secs[name]
    print(f"[{name}] header line {s['line']}, {len(s['entries'])} entries, lines {s['entries'][0][0]}-{s['entries'][-1][0]}")
    archives = []
    for i, k, v in s["entries"]:
        if v == "":
            print(f"  {i}: {k} = <explicitly empty>")
            continue
        a, m = v.split(",")
        assert a and m and not any(c.isspace() for c in v)
        if a.lower() not in [x.lower() for x in archives]:
            archives.append(a)
        print(f"  {i}: {k} = {a} , {m}")
    for a in archives:
        print(f"  archive {a}: BLO/{a}.blo listed = {a.lower() + '.blo' in blo}")
r = secs["Render"]
print(f"[Render] header line {r['line']}")
for i, k, v in r["entries"]:
    print(f"  {i}: {k}={v}")
