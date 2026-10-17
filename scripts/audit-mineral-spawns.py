#!/usr/bin/env python3
"""RT-F042 mineral spawn data audit (read-only, evidence only; no field semantics are interpreted).
Usage: python scripts/audit-mineral-spawns.py <assets-root>   Prints MINERAL_SPAWNS_OK on success."""
import csv, sys
root = sys.argv[1]
def rows(p):
    return [(i + 1, next(csv.reader([l], skipinitialspace=True))) for i, l in enumerate(open(p, encoding="latin1").read().splitlines()) if l.strip()]
lio = rows(f"{root}/LIO/Rogue.lio")
t5 = [(n, f) for n, f in lio if f[0] == "5"]
assert len(lio) == 81 and len(t5) == 42, (len(lio), len(t5))
assert all(len(f) == 28 and f[1] == "v23" for n, f in t5), "type5 v23 rows are 28 fields"
res = {f[2]: f[3] for n, f in rows(f"{root}/ITM/Rogue_mp.itm") if f[0] == "4" and f[4] == "Resources"}
# f[20] positive = item id, negative = actor id (kept raw)
mineral = [(n, f) for n, f in t5 if int(f[20]) > 0]
assert [n for n, f in mineral] == [15, 16, 17, 25, 26, 50, 51, 53, 54, 77, 78, 79], [n for n, f in mineral]
by_item = {}
for n, f in mineral:
    assert f[20] in res and f[20] in ("2003", "2005", "2019"), (n, f[20])
    by_item.setdefault(res[f[20]], []).append(n)
    # tail fields 26/27 are 350 with 15 (Tellerium/Iron) or 10 (Krandazite): preserved, not interpreted
    assert f[26] == "350" and f[27] == ("10" if f[20] == "2005" else "15"), (n, f[26:])
assert by_item == {"Tellerium": [15, 16, 25, 26, 50, 51, 54], "Krandazite": [17, 53], "Iron Ore": [77, 78, 79]}, by_item
counts = {n: (f[19], f[18]) for n, f in mineral}
assert counts[15] == ("100", "1") and counts[17] == ("50", "1") and counts[54] == ("-1", "-1"), counts  # row 54 = unusual -1/-1 spawn
# actor (negative f[20]) rows, e.g. MeteorShower row52 -402: not minerals, kept visible
actors = {n: f[20] for n, f in t5 if int(f[20]) < 0}
assert actors[52] == "-402", actors
# raw timing pair f[11]/f[12] (e.g. row15 3000/6000) and the unresolved f[14] (0/40/60/80)
timing = {n: (f[11], f[12]) for n, f in mineral}
assert timing[15] == ("3000", "6000") and timing[16] == ("4500", "4500") and timing[26] == ("3500", "8500"), timing
f14 = sorted({f[14] for n, f in mineral})
assert f14 == ["0", "40", "60", "80"], f14
cfg = open(f"{root}/CFG/rogue.cfg", encoding="latin1").read().splitlines()
assert cfg[34].strip() == "ItemPickupDistance=80", cfg[34]
nws = open(f"{root}/NWS/Rogue.nws", encoding="latin1").read().splitlines()
assert nws[469].startswith("Adjusted mineral spawns. A mineral spawn anchor has a detection radius"), nws[469]
print("MINERAL_SPAWNS_OK", len(t5), "type5 rows", len(mineral), "mineral rows", {k: len(v) for k, v in by_item.items()}, "actor rows", sorted(actors), "f14 values", f14, "pickup 80")
