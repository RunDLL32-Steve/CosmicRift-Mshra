#!/usr/bin/env python3
"""RT-F030 cargo-weight data audit (read-only, evidence only; no formula interpretation).
Usage: python scripts/audit-cargo-weights.py <assets-root>   Prints CARGO_WEIGHTS_OK on success."""
import csv, sys
root = sys.argv[1]
def rows(p):
    return [(i + 1, next(csv.reader([l], skipinitialspace=True))) for i, l in enumerate(open(p, encoding="latin1").read().splitlines()) if l.strip()]
soul = {}
for l in open(f"{root}/CFG/rogue.cfg", encoding="latin1").read().splitlines():
    if l.strip().startswith("["): sec = l.strip(); continue
    if sec == "[Soul]" and "=" in l: k, v = l.strip().split("=", 1); soul[k] = v
assert (soul["WeightNormalMax"], soul["WeightStopMax"]) == ("25000", "40000"), soul
# per-hull: f[6] hp, f[7] weight, f[47] NormalWeight, f[48] StopWeight
hulls = {f[3]: (int(f[7]), int(f[47]), int(f[48])) for n, f in rows(f"{root}/VEH/Rogue.veh") if f[0] == "2"}
assert len(hulls) == 12 and hulls["Asp"] == (10000, 1500000, 2500000), hulls
assert all(v[1] == 1500000 for v in hulls.values())
stop = {k: v[2] for k, v in hulls.items()}
assert sorted(set(stop.values())) == [2500000, 3000000, 4000000], stop
# per-item weight f[7] for the 7 resources
itm = {f[3]: int(f[7]) for n, f in rows(f"{root}/ITM/Rogue_mp.itm") if f[0] == "4" and f[4] == "Resources"}
want = {"Tellerium": 50000, "Refined Tellerium": 25000, "Krandazite": 100000, "Refined Krandazite": 50000, "Wreckage": 200000, "Iron Ore": 100000, "Alloy": 75000}
assert itm == want, itm
print("CARGO_WEIGHTS_OK", len(hulls), "hulls", len(itm), "resources", "StopWeight", sorted(stop.items()))
