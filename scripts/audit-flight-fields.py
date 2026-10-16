#!/usr/bin/env python3
"""RT-F027/RT-F028 flight-field audit (read-only, evidence only; no interpretation of units or sentinel meaning).
Usage: python scripts/audit-flight-fields.py <assets-root>   Prints FLIGHT_FIELDS_OK on success.
Reads RPG/Rogue.rpg hull rows (f[2]=="1", f[3]=vehicle id) and VEH/Rogue.veh Car rows (f[0]=="2"): per hull 16 terrain
blocks x 10 raw fields at f[202+10t..211+10t], plus f[362] and f[373]."""
import csv, sys
from collections import Counter, defaultdict
root = sys.argv[1]
def rows(p):
    return [(i + 1, next(csv.reader([l], skipinitialspace=True))) for i, l in enumerate(open(p, encoding="latin1").read().splitlines()) if l.strip()]
NAMES = ["RollAllowed", "RollFriction", "RollThrust", "RollRotate", "RollTopSpeed", "StrafeThrust", "HyperTopSpeed",
         "HyperThrust", "BackwardThrust", "BackwardHyperThrust"]
veh = {int(f[2]): (n, f) for n, f in rows(f"{root}/VEH/Rogue.veh") if f[0] == "2"}
assert len(veh) == 12
hulls = []
for n, f in rows(f"{root}/RPG/Rogue.rpg"):
    if len(f) > 39 and f[2] == "1" and int(f[3]) in veh:
        hulls.append((f[39], veh[int(f[3])]))
assert len(hulls) == 12, len(hulls)

blocks = {}                   # (hull, t) -> tuple of 10 ints
neg = defaultdict(Counter)    # block position 0..9 -> Counter(value) of negatives in f[202..361]
neg_fields = Counter()        # absolute field index -> count of hulls with a negative there
nonzero = {}
f373 = {}
for name, (rown, f) in hulls:
    assert len(f) > 373
    for t in range(16):
        b = tuple(int(x) for x in f[202 + 10 * t: 212 + 10 * t])
        blocks[(name, t)] = b
        for k, v in enumerate(b):
            if v < 0:
                neg[k][v] += 1
                neg_fields[202 + 10 * t + k] += 1
    nonzero[name] = [t for t in range(16) if any(blocks[(name, t)][:8])]   # "active": some value in positions 0..7 is non-zero (every block ends -1,-1)
    f373[name] = int(f[373])
    # also any negative elsewhere in f[202..361] outside blocks is impossible (blocks cover the range)

print("## Hull overview\n")
print("| Hull | VEH row | active terrain blocks | f[362] | f[373] |")
print("|---|---|---|---|---|")
for name, (rown, f) in hulls:
    print(f"| {name} | {rown} | {nonzero[name]} | {f[362]} | {f[373]} |")

print("\n## Terrain 0 (f[202..211]) per hull\n")
print("| Hull | " + " | ".join(NAMES) + " |")
print("|---|" + "---|" * 10)
for name, _ in hulls:
    print(f"| {name} | " + " | ".join(str(v) for v in blocks[(name, 0)]) + " |")

print("\n## F028 group per non-zero terrain [StrafeThrust, HyperTopSpeed, HyperThrust, BackwardThrust, BackwardHyperThrust]\n")
for name, _ in hulls:
    print(f"- {name}: " + "; ".join(f"t{t}={list(blocks[(name, t)][5:10])}" for t in nonzero[name]))

zero = tuple([0] * 8 + [-1, -1])
shapes = Counter(b for b in blocks.values() if any(b[:8]))
print(f"\n## Distinct active block shapes: {len(shapes)} ({sum(shapes.values())} active blocks; {sum(1 for b in blocks.values() if b == zero)} blocks are only 0*8,-1,-1; {len(blocks)} blocks total)\n")
for s, c in shapes.most_common():
    print(f"- x{c}: {list(s)}")

print("\n## Negative values in f[202..361] by position within the 10-field block\n")
for k in sorted(neg):
    print(f"- position {k} ({NAMES[k]}): {dict(neg[k])}")
print(f"- distinct absolute field indexes holding a negative value: {len(neg_fields)}")
print(f"- f[373] values: {sorted(set(f373.values()))} (negative: {sum(1 for v in f373.values() if v < 0)})")

# --- pinned results ---
nzblocks = sum(len(v) for v in nonzero.values())
assert all(v == nonzero["Asp"] for v in nonzero.values())          # identical non-zero terrain set for all hulls
assert set(neg) == {8, 9} and all(set(c) == {-1} for c in neg.values())   # negatives only at positions 8/9 and only -1
assert nonzero["Asp"] == [0, 9, 10, 15]
assert {n: v for n, v in f373.items()} == {"Asp": 1100, "Excelsior": 1100, "Ravager": 1100, "Hornet": 1100, "Skithist": 990,
    "Mastakith": 1200, "Tarthist": 990, "Kragath": 990, "Rak'Kir": 1100, "Rip'Pir": 1100, "Zap'Pir": 1100, "Zag'Gir": 1100}
assert sum(1 for b in blocks.values() if b == zero) == 144 and len(shapes) == 12 and nzblocks == 48
for name, _ in hulls:   # terrains 9 and 10 identical in all hulls; terrain 15 differs only for Hornet (RollThrust 50, RollRotate 1656)
    assert blocks[(name, 9)] == blocks[(name, 10)] == (1, 45, 45, 1000, 3000, 15, 0, 0, -1, -1), name
    assert blocks[(name, 15)] == ((1, 30, 50, 1656, 3000, 15, 0, 0, -1, -1) if name == "Hornet" else (1, 30, 45, 1300, 3000, 15, 0, 0, -1, -1)), name
assert all(b[8] == -1 and b[9] == -1 for b in blocks.values())
for name, (rown, f) in hulls:
    t0 = blocks[(name, 0)]
    assert t0[0] == 0 and t0[1] == 0 and t0[5] == 22 and t0[8] == -1 and t0[9] == -1, (name, t0)

print(f"\nFLIGHT_FIELDS_OK {len(hulls)} hulls {nzblocks} nonzero-blocks")
