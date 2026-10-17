#!/usr/bin/env python3
"""RT-F066 ambient/decorative world data audit (read-only, evidence only; no blob/audio resolution or field semantics).
Usage: python scripts/audit-ambient-world.py <assets-root>   Prints AMBIENT_WORLD_OK on success."""
import csv, sys
root = sys.argv[1]
lio = [(i + 1, next(csv.reader([l], skipinitialspace=True))) for i, l in enumerate(open(f"{root}/LIO/Rogue.lio", encoding="latin1").read().splitlines()) if l.strip()]
byrow = dict(lio)
# type 9 decorative sprites: rows 1-5 = Jupiter, Mars, LNebula2, Moon1, Moon0 (blob group/name at f[13]/f[14], f[19] = raw per-sprite value kept uninterpreted)
t9 = [(n, f) for n, f in lio if f[0] == "9"]
assert [n for n, f in t9] == [1, 2, 3, 4, 5] and all(len(f) == 21 for n, f in t9), [n for n, f in t9]
sprites = [(f[3], f[4], f[5], f[13], f[14], f[19]) for n, f in t9]
assert sprites[0] == ("Planets - Jupiter", "7824", "5504", "Planets", "jupiter", "0")
assert sprites[1][3:] == ("Planets", "mars", "-5") and sprites[2][3:] == ("BackgroundSprites", "Lnebula2", "-20")
assert sprites[3][3:] == ("Planets", "moon1", "-2") and sprites[4][3:] == ("Planets", "moon0", "-7")
assert [f[10] for n, f in t9] == ["35", "35", "30", "35", "40"]
# type 8 description areas: rows 35 (Scorpius Nebula) and 59 (Marianas FreePort) with free-text description
t8 = {n: f for n, f in lio if f[0] == "8"}
assert byrow[35][3] == "Scorpius Nebula" and byrow[35][0] == "8" and byrow[59][3] == "Marianas FreePort" and byrow[59][0] == "8"
assert [byrow[35][i] for i in (4, 5, 6, 7)] == ["16432", "1888", "2000", "1000"] and [byrow[59][i] for i in (4, 5, 6, 7)] == ["13696", "8768", "1500", "1500"]
# type 7 ambient sound rows 63/64
for n, snd in ((63, "space_radio_lp"), (64, "ambience_lp")):
    f = byrow[n]
    assert f[0] == "7" and f[3] == ("AmbientA" if n == 63 else "AmbientB") and f[-4] == "SFAmbient" and f[-3] == snd, (n, f)
assert byrow[63][4:8] == ["27006", "20942", "300", "400"] and byrow[64][4:8] == ["26558", "19438", "300", "400"]
sfa = [n for n, f in lio if "SFAmbient" in f]
assert {63, 64} <= set(sfa), sfa
print("AMBIENT_WORLD_OK", len(t9), "sprites", sorted(t8), "type8 area rows (35, 59 pinned) and SFAmbient rows", sfa, "(docking rows belong to RT-F065)")
