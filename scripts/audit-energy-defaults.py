#!/usr/bin/env python3
"""RT-F032 energy defaults audit (read-only, evidence only; no interpretation of units).
Usage: python scripts/audit-energy-defaults.py <assets-root>   Prints ENERGY_DEFAULTS_OK on success.
Reads CFG/rogue.cfg [Soul], VEH/Rogue.veh hull rows f[94..96], ITM/Rogue_mp.itm power-mod rows (f[33]/f[34])."""
import csv, sys
root = sys.argv[1]
def rows(p):
    return [(i + 1, next(csv.reader([l], skipinitialspace=True))) for i, l in enumerate(open(p, encoding="latin1").read().splitlines()) if l.strip()]
# CFG [Soul]
soul, sec = {}, None
for l in open(f"{root}/CFG/rogue.cfg", encoding="latin1").read().splitlines():
    l = l.strip()
    if l.startswith("["): sec = l; continue
    if sec == "[Soul]" and "=" in l:
        k, v = l.split("=", 1); soul[k] = v
assert soul["EnergyDefaultMax"] == "1700000" and soul["EnergyDefaultRate"] == "250", soul
# hulls: all twelve carry the same f[94..96]
hulls = [(n, f) for n, f in rows(f"{root}/VEH/Rogue.veh") if f[0] == "2"]
assert len(hulls) == 12
for n, f in hulls:
    assert f[94:97] == ["1700", "1050", "1700"], (n, f[94:97])
# mods (type 13 Power Mods): (row, id, name, f[33] capacity delta, f[34] recharge delta)
want = {26: ("9", "Turbo Capacitor", 150, 0), 27: ("10", "Super Capacitor", 250, 0), 28: ("11", "Turbocharger", 0, 100),
        29: ("12", "Supercharger", 0, 200), 91: ("20", "Advanced Moltor Core", 100, 250), 93: ("22", "Hyper Capacitor", 400, 0)}
itm = dict(rows(f"{root}/ITM/Rogue_mp.itm"))
for n, (i, name, cap, rate) in want.items():
    f = itm[n]
    assert (f[0], f[2], f[3], f[4]) == ("13", i, name, "Power Mods"), (n, f[:5])
    assert (f[31], f[32], int(f[33]), int(f[34])) == ("1", "8", cap, rate), (n, f[31:35])
power = [n for n, f in itm.items() if f[0] == "13" and f[4] == "Power Mods"]
assert sorted(power) == sorted(want), power  # no other Power Mods rows
print("ENERGY_DEFAULTS_OK", len(hulls), "hulls", len(want), "mods")
