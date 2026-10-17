#!/usr/bin/env python3
"""Independent audit for RT-F065 (safe zones and docking boundaries, data clause only). Stdlib only.

Reads CFG/rogue.cfg, NWS/Rogue.nws and LIO/Rogue.lio (pinned fowlmouth/cosmic-rift-assets eb1514ea) below
RT_ORIGINAL_ASSETS_ROOT or argv[1] and prints the values pinned by tests/Unit/OriginalSafeZonesTests.cs. LIO lines are
printed as raw text only (the LIO reader belongs to queue G6).

Usage: python3 -I scripts/original-assets/audit_rt_f065.py [assets-root]
"""
import hashlib, os, re, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("RT_ORIGINAL_ASSETS_ROOT", "")
SHA = {
    "CFG/rogue.cfg": "82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90",
    "NWS/Rogue.nws": "7fff837b85d52a78b93d50b5b8721d6e567b72b5989c5a96f4212add90aaa895",
    "LIO/Rogue.lio": "73d008c5168efa46f73eaccdd78c461a121d6ae281b6d2a5e95a73e43bec2f6a",
}
KEYS = ["Message", "Safety", "StoreEnabled", "SkillEnabled", "TrickleKill", "TeamChangeEnabled", "EnergyRate", "HealthRate",
        "RepairRate", "PrizeEnableMode", "PrizeExpire", "PrizeBountyMultiplier", "RepelVehicle", "RepelWeapons",
        "PercentOfCashReward", "PercentOfExperienceReward", "PercentOfPointsReward", "MaxTimeAllowed", "BountyAutoMax",
        "BountyAutoRate", "DeleteLiveWeapons", "EventString", "AllowChangeArena", "AllowGoToSpec", "QuitDelaySecs",
        "GoToSpecDelaySecs", "ChangeArenaDelaySecs", "AllowQuitting"]


def read(rel):
    with open(os.path.join(ROOT, rel), "rb") as fh:
        data = fh.read()
    print(rel, "sha256 ok:", hashlib.sha256(data).hexdigest() == SHA[rel], "length", len(data))
    return data.decode("latin-1")


def main():
    if not ROOT:
        sys.exit("set RT_ORIGINAL_ASSETS_ROOT or pass the assets root")
    cfg = read("CFG/rogue.cfg")
    sections, cur = {}, None
    for n, line in enumerate(cfg.split("\n"), 1):
        line = line.rstrip("\r")
        if not line:
            continue
        m = re.fullmatch(r"\[(.*)\]", line)
        if m:
            cur = m.group(1)
            sections.setdefault(cur, {"line": n, "entries": []})
            continue
        k, v = line.split("=", 1)
        sections[cur]["entries"].append((n, k, v))
    for t in range(16):
        s = sections[f"Terrain{t}"]
        d = {k: (n, v) for n, k, v in s["entries"]}
        other = [k for _, k, _ in s["entries"] if k not in KEYS]
        print(f"Terrain{t} line={s['line']} entries={len(s['entries'])} other={other}")
        print("   " + " ".join(f"{k}={d[k][1]!r}@{d[k][0]}" for k in KEYS))
    for t in range(16):
        d = {k: v for _, k, v in sections[f"Terrain{t}"]["entries"]}
        flags = [k for k in ("Safety", "EnergyRate", "MaxTimeAllowed") if d[k] != "0"]
        if flags:
            print(f"Terrain{t} {d['Message']!r}: " + ", ".join(f"{k}={d[k]}" for k in flags))
    for sec, key in [("Arena", "SpectatorVehicleId"), ("Arena", "SpectatorStore"), ("Arena", "SpectatorSkills"), ("Arena", "StartInSpectator"),
                     ("Arena", "ExitSpectatorLogic"), ("Arena", "ExitSpectatorMessage"), ("Arena", "SpectatorVisualFrequency"),
                     ("Arena", "SpectatorEnergyAmount"), ("Arena", "ExitSpectatorToggleSkills"), ("Arena", "SkillScreenEnergyNeeded"),
                     ("Arena", "AllowManualTeamSwitch"), ("Arena", "TeamSwitchMinEnergy"), ("Arena", "InactivityTimeout"),
                     ("Arena", "SuicidesAllowed"), ("Door", "AttemptTime"), ("Door", "RandomControlDelay"), ("Flag", "AllowSafety"),
                     ("Soul", "EnergyDefaultMax"), ("Soul", "EnergyDefaultRate"), ("Soul", "EnergyDefaultStart"), ("Soul", "EnergyShieldMode"),
                     ("Event", "Hold1"), ("Event", "Hold2"), ("Event", "JoinTeam"), ("Event", "ExitSpectatorMode"), ("Event", "ManualJoinTeam")]:
        n, v = next((n, v) for n, k, v in sections[sec]["entries"] if k == key)
        print(f"[{sec}] {key}={v} (line {n})")

    nws = read("NWS/Rogue.nws").replace("\r\n", "\n").replace("\r", "\n").split("\n")
    head = re.compile(r"^\*+\s*(\d{1,2}/\d{1,2}/\d{2})\s*$")
    for n in (290, 344, 345, 419, 481, 483):
        h = next(i for i in range(n, 0, -1) if head.match(nws[i - 1]))
        print(f"NWS {n} under {h}: {nws[h - 1]!r}: {nws[n - 1]!r}")
    print("'48360' in NWS:", any("48360" in l for l in nws))

    lio = read("LIO/Rogue.lio").replace("\r\n", "\n").split("\n")
    if lio and not lio[-1]:
        lio.pop()
    print("LIO lines:", len(lio))
    print("docking_proceedure lines:", [i for i, l in enumerate(lio, 1) if ",SFAmbient,docking_proceedure," in l])
    print("'door' lines:", [i for i, l in enumerate(lio, 1) if "door" in l.lower()])


if __name__ == "__main__":
    main()
