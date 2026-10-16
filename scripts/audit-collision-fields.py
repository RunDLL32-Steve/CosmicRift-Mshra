#!/usr/bin/env python3
"""RT-F029 collision/bounce/point-blank raw-field audit (read-only, evidence only; no interpretation of units).
Usage: python scripts/audit-collision-fields.py <assets-root>   Prints COLLISION_FIELDS_OK on success.
Reads VEH/Rogue.veh Car rows (f[0]=="2") f[35] (physical radius CANDIDATE, catalog name) and f[362] (BouncePercent),
CFG/rogue.cfg [Arena] PointBlankDamageMultiplier, [Vehicle] PhysicsDelay and per-terrain RepelVehicle/RepelWeapons,
and reports whether those CFG keys vary across all CFG/*.cfg files."""
import csv, glob, os, sys
root = sys.argv[1]
def rows(p):
    return [(i + 1, next(csv.reader([l], skipinitialspace=True))) for i, l in enumerate(open(p, encoding="latin1").read().splitlines()) if l.strip()]
def cfg_lines(p):
    """[(line, section, key, value)] for every key=value line; section is the last [header]."""
    out, sec = [], None
    for i, l in enumerate(open(p, encoding="latin1").read().split("\n")):
        l = l.rstrip("\r")
        if not l: continue
        if l.startswith("["): sec = l[1:l.index("]")]; continue
        k, v = l.split("=", 1)
        out.append((i + 1, sec, k, v))
    return out

# --- hulls
hulls = [(n, f) for n, f in rows(f"{root}/VEH/Rogue.veh") if f[0] == "2"]
assert len(hulls) == 12
print("## VEH Car rows (f[35] physical radius CANDIDATE, f[362] BouncePercent candidate)\n")
print("| VEH row | id f[2] | name f[3] | f[35] | f[362] |\n|---|---|---|---|---|")
for n, f in hulls:
    print(f"| {n} | {f[2]} | {f[3]} | {f[35]} | {f[362]} |")
r35 = sorted({int(f[35]) for _, f in hulls}); r362 = sorted({int(f[362]) for _, f in hulls})
print(f"\ndistinct f[35]: {r35}\ndistinct f[362]: {r362}")
asp = [(n, f) for n, f in hulls if f[3] == "Asp"]
assert len(asp) == 1 and asp[0][0] == 20 and asp[0][1][2] == "2002"
assert asp[0][1][35] == "21" and asp[0][1][362] == "727"
assert all(int(f[35]) > 0 and int(f[362]) > 0 for _, f in hulls)  # no negative sentinels in either field

# --- rogue.cfg
rogue = cfg_lines(f"{root}/CFG/rogue.cfg")
def one(sec, key):
    m = [(l, v) for l, s, k, v in rogue if s == sec and k == key]
    assert len(m) == 1, (sec, key, m)
    return m[0]
pb, pd = one("Arena", "PointBlankDamageMultiplier"), one("Vehicle", "PhysicsDelay")
assert pb == (63, "500") and pd == (125, "400"), (pb, pd)
print(f"\n## rogue.cfg\n\n- [Arena] PointBlankDamageMultiplier={pb[1]} (line {pb[0]})\n- [Vehicle] PhysicsDelay={pd[1]} (line {pd[0]})\n")
print("| Terrain | RepelVehicle (line) | RepelWeapons (line) |\n|---|---|---|")
rep = []
for t in range(16):
    rv, rw = one(f"Terrain{t}", "RepelVehicle"), one(f"Terrain{t}", "RepelWeapons")
    rep.append((t, rv, rw))
    print(f"| {t} | {rv[1]} ({rv[0]}) | {rw[1]} ({rw[0]}) |")
rv0 = [t for t, rv, rw in rep if rv[1] == "0"]
assert rv0 == [10, 15] and all(rw[1] == "1" for _, _, rw in rep)
assert all(rv[1] in ("0", "1") for _, rv, _ in rep)
assert [(t, rv[0], rw[0]) for t, rv, rw in rep][0] == (0, 261, 262) and rep[10][1][0] == 631 and rep[15][1][0] == 816

# --- variation across all CFGs
files = sorted(glob.glob(f"{root}/CFG/*.cfg"))
assert len(files) == 31, len(files)
print(f"\n## Variation across {len(files)} CFG files\n")
def collect(path):
    d = {}
    for l, s, k, v in cfg_lines(path):
        if (s, k) in (("Arena", "PointBlankDamageMultiplier"), ("Vehicle", "PhysicsDelay")) or (s and s.startswith("Terrain") and k in ("RepelVehicle", "RepelWeapons")):
            d.setdefault((s, k), []).append(v)
    return d
allv = {os.path.basename(p): collect(p) for p in files}
PB_COUNTS = {"500": 1, "1000": 18, "1250": 1, "1500": 11}  # set from observed counts (checked below)
keys = [("Arena", "PointBlankDamageMultiplier"), ("Vehicle", "PhysicsDelay")]
for key in keys:
    vals = {name: d.get(key) for name, d in allv.items()}
    dist = sorted({str(v) for v in vals.values()})
    print(f"- {key[0]}.{key[1]}: distinct value lists across files = {dist}; files lacking key = {[n for n, v in vals.items() if v is None]}")
    counts = {}
    for n, v in vals.items(): counts.setdefault(v[0], []).append(n)
    print("    value -> file count: " + ", ".join(f"{k}: {len(v)}" for k, v in sorted(counts.items(), key=lambda kv: int(kv[0]))))
    if key[1].startswith("Point"):
        assert {k: len(v) for k, v in counts.items()} == PB_COUNTS and counts["500"] == ["rogue.cfg"], counts
        print("    value 500 appears only in: " + str(counts["500"]))
    else:
        assert {k: len(v) for k, v in counts.items()} == {"400": 31}
for k in ("RepelVehicle", "RepelWeapons"):
    sigs = {}
    for name, d in allv.items():
        sigs.setdefault(tuple(tuple(d.get((f"Terrain{t}", k), ["<absent>"])) for t in range(16)), []).append(name)
    print(f"- Terrain0..15 {k}: {len(sigs)} distinct pattern(s) across files")
    for sig, names in sigs.items():
        print(f"    {len(names)} file(s) {names[:3]}{'...' if len(names)>3 else ''}: {''.join(s[0] if len(s)==1 else '?' for s in sig)}")
print()
pbd = {tuple(d.get(keys[0], ["<absent>"])) for d in allv.values()}
pdd = {tuple(d.get(keys[1], ["<absent>"])) for d in allv.values()}
print("COLLISION_FIELDS_OK", len(hulls), "hulls", "f35", r35, "f362", r362, "pointblank", pb[1], "physicsdelay", pd[1],
      "repelvehicle0_terrains", rv0, "cfgs", len(files), "pb_variants", len(pbd), "pd_variants", len(pdd))
