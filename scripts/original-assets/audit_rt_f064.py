#!/usr/bin/env python3
"""RT-F064 audit (data clause only): reproduces the pinned LOS/Arena/Radar CFG keys, the vision/radar VEH fields
f[65], f[132..141] of all 30 VEH rows, and NWS lines 258/305-315/325 with their dated headings, from the raw files.
Stdlib only. Usage: audit_rt_f064.py <assets-root>   (or RT_ORIGINAL_ASSETS_ROOT).
VEH field names are upstream-parser names (InfantryOnline bb97579e), not original-executable evidence.
Prints PASS/FAIL per check and exits 1 on any FAIL."""
import os, re, sys

root = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("RT_ORIGINAL_ASSETS_ROOT")
if not root:
    sys.exit("usage: audit_rt_f064.py <assets-root>")
fails = 0


def check(name, ok, detail=""):
    global fails
    print(("PASS " if ok else "FAIL ") + name + (" " + detail if detail and not ok else ""))
    fails += 0 if ok else 1


def read(rel):
    return open(os.path.join(root, rel), encoding="latin-1", newline="").read().replace("\r\n", "\n").split("\n")


cfg = read("CFG/rogue.cfg")
sec = None
entries = {}
for n, l in enumerate(cfg, 1):
    if l.startswith("["):
        sec = l[1:l.index("]")]
        entries[(sec, None)] = n
    elif "=" in l:
        k, v = l.split("=", 1)
        entries.setdefault((sec, k), (n, v))

exp = {
    ("LOS", None): 118, ("LOS", "DefaultDistance"): (119, "250"), ("LOS", "DefaultAngle"): (120, "200"),
    ("LOS", "DefaultXray"): (121, "0"), ("LOS", "TeamSharing"): (122, "1"),
    ("Arena", "TeamVisionDistance"): (32, "1200"), ("Arena", "SpecsSeeEnergy"): (60, "1"),
    ("Arena", "FriendlySeeEnergy"): (61, "1"), ("Arena", "EnemySeeEnergy"): (62, "0"),
    ("Level", "RadarPrizeDistance"): (23, "2000"), ("Flag", "CarrierOnRadar"): (866, "0"),
    ("Flag", "CarrierLineOfSight"): (867, "0"), ("Soccer", "ShowRadar"): (2099, "1"),
    ("Uiart", "Vision"): (2739, "cruiart,vision"), ("Uiart", "WarFog"): (2742, "cruiart,warfog"),
    ("Uiart", "RadarBorder"): (2751, "cruiart,newradar"), ("Radar", None): 2276,
    ("Radar", "ColorSoccerBall"): (2277, "0xffffff"), ("Radar", "ColorSelf"): (2278, "0x00ff00"),
    ("Radar", "ColorFriendly"): (2279, "0xffff00"), ("Radar", "ColorEnemy"): (2280, "0xff4040"),
    ("Radar", "ColorFlag"): (2281, "0xffffff"), ("Radar", "FontColorSelf"): (2282, "2"),
    ("Radar", "FontColorFriendly"): (2283, "4"), ("Radar", "FontColorEnemy"): (2284, "1"),
}
for k, v in exp.items():
    check(f"cfg {k[0]}.{k[1] or '[header]'} = {v}", entries.get(k) == v, f"got {entries.get(k)}")

vd = [(i, entries[("LosType%d" % i, "VisibleDistance")][1], entries[("LosType%d" % i, "Solid")][1], entries[("LosType%d" % i, None)]) for i in range(8)]
check("LosType VisibleDistance", [x[1] for x in vd] == ["-1", "-1", "-1", "0", "4", "-1", "12", "16"], str(vd))
check("LosType Solid", [x[2] for x in vd] == ["0", "1", "1", "0", "0", "0", "0", "0"])
check("LosType header lines", [x[3] for x in vd] == [2767 + 12 * i for i in range(8)])

vis = [k for (s, k) in entries if k and re.search(r"xray|fog|sensor|sight", k, re.I)]
check("only DefaultXray/CarrierLineOfSight/WarFog match xray|fog|sensor|sight", sorted(vis) == ["CarrierLineOfSight", "DefaultXray", "WarFog"], str(vis))

# VEH: parse with a quote-aware splitter (descriptions contain commas)
def fields(line):
    out, cur, q = [], "", False
    for ch in line:
        if ch == '"':
            q = not q
        elif ch == "," and not q:
            out.append(cur.strip()); cur = ""
        else:
            cur += ch
    out.append(cur.strip())
    return out

rows = [fields(l) for l in read("VEH/Rogue.veh") if l.strip()]
check("30 VEH rows", len(rows) == 30, str(len(rows)))
check("f132..134 all 0", all(r[132:135] == ["0", "0", "0"] for r in rows))
check("f138..141 all 1,-1,1,-1", all(r[138:142] == ["1", "-1", "1", "-1"] for r in rows))
check("f135 all 0", all(r[135] == "0" for r in rows))
check("f137 non-zero only 409=20, 413=10", [(r[2], r[137]) for r in rows if r[137] != "0"] == [("409", "20"), ("413", "10")], str([(r[2], r[137]) for r in rows if r[137] != "0"]))
check("f65 -1 on 402, hulls, spectator", [r[2] for r in rows if r[65] == "-1"] == ["402"] + [str(i) for i in (2000, 2001, 2002, 2003, 2004, 2005, 2006, 2008, 2009, 2010, 2011, 2012)] + ["2030"])
check("field counts", [len(r) for r in rows] == [477] * 17 + [454] * 12 + [202], str([len(r) for r in rows]))

nws = read("NWS/Rogue.nws")
check("nws 258", nws[257] == "Re-Enabled Team vision sharing." and nws[248] == "********2/27/03")
check("nws 261 blank", nws[260] == "")
check("nws 325", nws[324].startswith("Base LOS has been narrowed to 225 degrees.") and nws[292] == "********2/24/03")
check("nws 305-315", nws[304] == "New Item Added: Stealth - Active" and nws[314] == "New Item Added: Terran Combat Sensors")
itm = "\n".join(read("ITM/Rogue_mp.itm")).lower()
check("itm has no sensor/stealth/distortion text", not re.search(r"sensor|stealth|distortion", itm))
print("FAILED" if fails else "ALL PASS")
sys.exit(1 if fails else 0)
