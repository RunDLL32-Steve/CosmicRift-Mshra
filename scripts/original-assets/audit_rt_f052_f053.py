#!/usr/bin/env python3
"""Independent audit for RT-F052/RT-F053 (stdlib only): CFG [TeamInfoN] blocks, [Arena] AllowPrivateFrequencies,
plus a read-only raw listing of LIO/Rogue.lio record-type-4 rows (evidence only; the C# LIO reader belongs to queue G6).
Usage: audit_rt_f052_f053.py [assets_root]   (default $RT_ORIGINAL_ASSETS_ROOT)"""
import hashlib, os, re, sys

root = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("RT_ORIGINAL_ASSETS_ROOT")
if not root:
    sys.exit("pass the assets root or set RT_ORIGINAL_ASSETS_ROOT")
cfg_path = os.path.join(root, "CFG", "rogue.cfg")
data = open(cfg_path, "rb").read()
print("cfg bytes", len(data), "sha256", hashlib.sha256(data).hexdigest())
lines = data.decode("latin-1").split("\n")

sections = []  # (name, header_line, [(lineno, key, value)])
for no, raw in enumerate(lines, 1):
    l = raw.rstrip("\r")
    if not l:
        continue
    if l.startswith("["):
        sections.append((l[1:l.index("]")], no, []))
    else:
        k, _, v = l.partition("=")
        sections[-1][2].append((no, k, v))

arena = next(s for s in sections if s[0] == "Arena")
print("\n[Arena] header line", arena[1])
for no, k, v in arena[2]:
    if k in ("AllowPrivateFrequencies", "FrequencyMax", "MaxPerFrequency", "DesiredFrequencies"):
        print(f"  line {no}: {k}={v}")

teams = {}
for name, hdr, ents in sections:
    m = re.fullmatch(r"TeamInfo(\d+)", name)
    if m:
        teams[int(m.group(1))] = (hdr, ents)
print("\nTeamInfo sections:", len(teams), "indices", min(teams), "..", max(teams), "contiguous", sorted(teams) == list(range(len(teams))))
orders = {tuple(k for _, k, _ in e) for _, e in teams.values()}
print("distinct key orders:", len(orders), "; entries per block:", {len(e) for _, e in teams.values()})
for i in sorted(teams):
    hdr, ents = teams[i]
    d = {k: v for _, k, v in ents}
    nameline = next(no for no, k, _ in ents if k == "Name")
    rts = [d[f"RtsState{n}"] for n in range(8)]
    print(f"  T{i:<2} hdr={hdr} nameline={nameline} Name={d['Name']!r} MaxPlayers={d['MaxPlayers']} DefaultVehicle={d['DefaultVehicle']} "
          f"Colors={d['Colors']} RelVeh={d['RelativeVehicle']} Radar={d['RadarColor']} NameFont={d['NameFontColor']} Event={d['EventString']!r} "
          f"h/s/v={d['hAdjust']}/{d['sAdjust']}/{d['vAdjust']} NoWinFlag={d['DisallowWinFlagGame']} Rts={','.join(rts)} StartEvt={d['TeamEventStartGame']!r}")
names = {i: {k: v for _, k, v in teams[i][1]}["Name"] for i in teams}
mp = {i: int({k: v for _, k, v in teams[i][1]}["MaxPlayers"]) for i in teams}
print("blank names:", [i for i, n in names.items() if n == ""])
print("MaxPlayers>0:", [i for i in sorted(mp) if mp[i] > 0])
print("Name == 'Public<i>':", [i for i in sorted(names) if names[i] == f"Public{i}"][:3], "...", sum(names[i] == f"Public{i}" for i in names), "total")
print("distinct Colors:", {dict((k, v) for _, k, v in e)["Colors"] for _, e in teams.values()})
print("distinct (RelVeh,Radar,NameFont,DefVeh,s,v,NoWin,Rts*,StartEvt,Event):", len({tuple(v for _, k, v in e if k not in ("Name", "MaxPlayers", "hAdjust", "Colors")) for _, e in teams.values()}))

lio_path = os.path.join(root, "LIO", "Rogue.lio")
lio = open(lio_path, "rb").read().decode("latin-1").split("\n")
print("\nLIO/Rogue.lio raw record-type-4 rows (first field == 4; no interpretation):")
n4 = 0
for no, raw in enumerate(lio, 1):
    l = raw.rstrip("\r")
    if l.split(",", 1)[0] == "4":
        n4 += 1
        print(f"  line {no}: {l}")
print("  type-4 rows:", n4)
print("LIO rows naming 'Warp' (raw):")
for no, raw in enumerate(lio, 1):
    if "Warp" in raw:
        print(f"  line {no}: {raw.rstrip(chr(13))}")
