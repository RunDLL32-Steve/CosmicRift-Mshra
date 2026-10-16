#!/usr/bin/env python3
"""RT-F031 shield/energy/hull raw-field audit (read-only, evidence only; no interpretation of units or hit order).
Usage: python scripts/audit-shield-energy.py <assets-root>   Prints SHIELD_ENERGY_OK on success.
Reads CFG/rogue.cfg ([Soul]/[Arena]/[Vehicle]/[Terrain0..15] and every other Shield/Energy/Health key), the 31 CFG/*.cfg
EnergyShieldMode values, VEH/Rogue.veh Car-row f[6] and f[94..96], and the NWS/ text rows the catalog cites."""
import csv, glob, os, sys

root = sys.argv[1]


def read_lines(p):
    return open(p, encoding="latin1").read().split("\n")


def cfg_entries(path):
    """[(line_no, section, key, value)] for every key=value line; raw, untrimmed values (CR stripped)."""
    out, sec = [], None
    for i, l in enumerate(read_lines(path)):
        l = l.rstrip("\r")
        if l.startswith("["):
            sec = l[1:l.index("]")]
        elif "=" in l and sec is not None:
            k, v = l.split("=", 1)
            out.append((i + 1, sec, k, v))
    return out


rogue = cfg_entries(f"{root}/CFG/rogue.cfg")
print("## CFG/rogue.cfg shield/energy/health keys (raw, line numbers)\n")
print("| line | section | key | value |\n|---|---|---|---|")
want = lambda k: any(w in k.lower() for w in ("shield", "energy", "health"))
terrain_keys = {"EnergyRate", "HealthRate"}
for ln, sec, k, v in rogue:
    if want(k) and not (sec.startswith("Terrain") and k in terrain_keys):
        print(f"| {ln} | {sec} | {k} | {v} |")
print("\n(Terrain EnergyRate/HealthRate/RepairRate follow in the next table; key-name matches elsewhere in other sections, such as"
      " [Attribute], are listed above for visibility only and are not interpreted.)\n")

soul = {k: (ln, v) for ln, sec, k, v in rogue if sec == "Soul"}
assert soul["EnergyShieldMode"] == (100, "2"), soul["EnergyShieldMode"]
assert soul["EnergyDefaultStart"] == (108, "-1"), soul["EnergyDefaultStart"]
assert soul["EnergyDefaultMax"] == (97, "1700000") and soul["EnergyDefaultRate"] == (98, "250")

print("## [Terrain0..15] EnergyRate / HealthRate / RepairRate\n")
print("| terrain | EnergyRate (line) | HealthRate (line) | RepairRate (line) |\n|---|---|---|---|")
terr = []
for t in range(16):
    d = {k: (ln, int(v)) for ln, sec, k, v in rogue if sec == f"Terrain{t}" and k in ("EnergyRate", "HealthRate", "RepairRate")}
    assert set(d) == {"EnergyRate", "HealthRate", "RepairRate"}, (t, d)
    terr.append(d)
    print(f"| {t} | {d['EnergyRate'][1]} ({d['EnergyRate'][0]}) | {d['HealthRate'][1]} ({d['HealthRate'][0]}) | {d['RepairRate'][1]} ({d['RepairRate'][0]}) |")
neg = [t for t in range(16) if terr[t]["EnergyRate"][1] != 0]
assert neg == [9, 10, 15] and all(terr[t]["EnergyRate"][1] == -2000 for t in neg), neg
assert all(terr[t]["HealthRate"][1] == 0 and terr[t]["RepairRate"][1] == 0 for t in range(16))
assert [terr[t]["EnergyRate"][0] for t in (0, 9, 10, 15)] == [256, 589, 626, 811]
print(f"\nNonzero EnergyRate terrains: {neg} (all -2000, sign preserved, units unresolved); HealthRate and RepairRate are 0 in all 16.\n")

print("## EnergyShieldMode across CFG/*.cfg\n")
modes = {}
for p in sorted(glob.glob(f"{root}/CFG/*.cfg")):
    e = [(ln, v) for ln, sec, k, v in cfg_entries(p) if k == "EnergyShieldMode"]
    modes[os.path.basename(p)] = e
print("| file | line | value |\n|---|---|---|")
for n, e in modes.items():
    print(f"| {n} | {e[0][0]} | {e[0][1]} |")
assert len(modes) == 31 and all(len(e) == 1 for e in modes.values())
values = sorted({e[0][1] for e in modes.values()})
assert values == ["2"], values
print(f"\n{len(modes)} files, each with exactly one EnergyShieldMode line; distinct values: {values}\n")

print("## VEH/Rogue.veh Car rows: f[6] and f[94..96]\n")
rows = [(i + 1, next(csv.reader([l], skipinitialspace=True))) for i, l in enumerate(read_lines(f"{root}/VEH/Rogue.veh")) if l.strip()]
hulls = [(n, f) for n, f in rows if f[0] == "2"]
assert len(hulls) == 12
print("| row | name | f[6] | f[94] | f[95] | f[96] |\n|---|---|---|---|---|---|")
for n, f in hulls:
    print(f"| {n} | {f[3]} | {f[6]} | {f[94]} | {f[95]} | {f[96]} |")
    assert f[1] == "v53" and f[6] == "50" and f[94:97] == ["1700", "1050", "1700"], (n, f[:7], f[94:97])
asp = [f for n, f in hulls if f[3] == "Asp"]
assert len(asp) == 1 and asp[0][6] == "50"
print()

print("## NWS/Rogue.nws cited rows (verbatim)\n")
nws = [l.rstrip("\r") for l in read_lines(f"{root}/NWS/Rogue.nws")]
cited = {39: "Your ship in ~BRogue Trader~B also differs from other Cosmic Rift zones because it has Hull Points alloted to it.  As long as your ship",
         40: "has Shield energy, it will take no damage from weapons, but when your Shield Energy is at 0, you will take Hull Damage. When",
         41: "your Hull Points are at 0, you will blow up and leave a small piece of wreckage where you were.",
         131: "All weapons do Shield Drain Damage. This damage effects your shield energy level. As long as you have any Shield Energy you will take no Hull Point Damage",
         132: "",
         133: "All weapons will do one of 3 types of Hull Point Damage: Kinetic, Explosive or Energy. You can purchase armor that has a protective effect against one or"}
for ln, text in cited.items():
    print(f"- NWS/Rogue.nws:{ln}: `{nws[ln - 1]}`")
    assert nws[ln - 1] == text, (ln, nws[ln - 1])
print(f"- NWS/Rogue.nws:134: `{nws[133]}` (continuation of row 133)")
assert nws[133] == "2 of these damage types, but not all 3."
shield_files = {os.path.basename(p): sum('shield' in l.lower() for l in read_lines(p)) for p in sorted(glob.glob(f"{root}/NWS/*.nws"))}
hits = {k: v for k, v in shield_files.items() if v}
print(f"\nCase-insensitive 'shield' line counts per NWS file (nonzero only): {hits}")
assert hits.get("Rogue.nws") == 5
print("(pz.nws and smear.nws hits are the Power Zone 'Welcome' paragraph (line 3: the Shield Flag), not the hull/shield rules; not cited.)\n")

print("SHIELD_ENERGY_OK", f"EnergyShieldMode=2 line 100 in rogue.cfg and all {len(modes)} CFG files;",
      "EnergyDefaultStart=-1 line 108;", f"{len(neg)} terrains EnergyRate=-2000;", "HealthRate/RepairRate 0 x16;",
      f"{len(hulls)} hulls f[6]=50 f[94..96]=1700,1050,1700; 6 NWS rows verbatim")
