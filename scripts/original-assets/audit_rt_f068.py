#!/usr/bin/env python3
"""Read-only audit for RT-F068 (evidence for tests/Unit/OriginalProgressionTests.cs).

Usage: python3 -I audit_rt_f068.py [<assets-root>]   (cosmic-rift-assets at the pinned commit eb1514ea; default RT_ORIGINAL_ASSETS_ROOT)

Independently of the C# reader it verifies the SHA-256 of the five package text files, then prints: NWS lines 26-29, 399, 218
and 220 with the nearest heading above each (first preceding line starting with '*', and its M/D/YY date if dated); the CFG
[StartGame], [PublicProfile], [Owner] Wipe/Profile, [Stat] Track* and the progression-related [Event] entries; RPG f[38] of
every hull row; the hull vehicle ids (RPG f[3] of rows with f[2] == 1), the ITM id count and the RT-F048 upgrade ids; and a
case-insensitive keyword scan (save/persistence words) of all five files. It finds no save-record schema; it never writes.
"""
import hashlib, os, re, sys

PINNED = {
    'RPG/Rogue.rpg': '44d5e6d3c78bfa445c723260086235017bd1973557103003aa8f3b71e30ae123',
    'VEH/Rogue.veh': '9eefa85a04587867616be786fffc664f3ebc0fb1d50ba6f6fd303a7a8a988572',
    'ITM/Rogue_mp.itm': 'dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7',
    'CFG/rogue.cfg': '82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90',
    'NWS/Rogue.nws': '7fff837b85d52a78b93d50b5b8721d6e567b72b5989c5a96f4212add90aaa895',
}
KEYWORDS = ['wipe', 'character', 'save', 'persist', 'profile', 'reset', 'account', 'login', 'logout']
EVENT_KEYS = ['Hold1', 'Hold2', 'SysopWipe', 'SelfWipe', 'FirstTimeInvSetup', 'KilledByTeam', 'KilledByEnemy',
              'EnterSpawnNoScore', 'ChangeDefaultVehicle', 'FirstTimeSkillSetup']
UPGRADE_FAMILIES = ['Engine Mods', 'Thruster Mods', 'Power Mods', 'HyperDrive Systems']
DATED = re.compile(r'^\*+\s*(\d{1,2}/\d{1,2}/\d{2})\s*$')
FIELD = re.compile(r'[ \t]*("[^"]*"|[^,]*)[ \t]*(,|$)')


def text(root, rel):
    data = open(os.path.join(root, *rel.split('/')), 'rb').read()
    if hashlib.sha256(data).hexdigest() != PINNED[rel]:
        sys.exit('hash mismatch: ' + rel)
    return data.decode('latin-1')


def lines(t):
    out = re.split(r'\r\n|\n|\r', t)
    return out[:-1] if out and out[-1] == '' else out


def csv_rows(t):
    for n, line in enumerate(lines(t), 1):
        if not line.strip():
            continue
        fields, pos = [], 0
        while True:
            m = FIELD.match(line, pos)
            fields.append(m.group(1).strip('"'))
            if m.group(2) == '':
                break
            pos = m.end()
        yield n, fields


def main(root):
    texts = {rel: text(root, rel) for rel in PINNED}
    nws = lines(texts['NWS/Rogue.nws'])
    for n in [26, 27, 28, 29, 399, 218, 220]:
        h = next(k for k in range(n - 1, 0, -1) if nws[k - 1].startswith('*'))
        m = DATED.match(nws[h - 1])
        print(f'NWS {n}: {nws[n - 1]!r}  heading {h} {nws[h - 1]!r} date={m.group(1) if m else None}')

    sections, sec = {}, None
    for n, line in enumerate(lines(texts['CFG/rogue.cfg']), 1):
        if line.startswith('['):
            sec = line[1:line.index(']')]
            sections.setdefault(sec, [])
        elif '=' in line:
            k, v = line.split('=', 1)
            sections[sec].append((n, k, v))
    pick = lambda s, keys=None: [e for e in sections[s] if keys is None or e[1] in keys]
    print('[StartGame]', pick('StartGame'))
    print('[PublicProfile]', pick('PublicProfile'))
    print('[Owner]', pick('Owner', ['Wipe', 'Profile']))
    print('[Stat]', pick('Stat', ['TrackDaily', 'TrackWeekly', 'TrackMonthly', 'TrackYearly']))
    ev = {e[1]: e for e in sections['Event']}
    for k in EVENT_KEYS:
        print('[Event]', ev[k])

    hulls = [(f[39], f[3], f[38]) for _, f in csv_rows(texts['RPG/Rogue.rpg']) if f[2].strip() == '1']
    print('RPG hulls (name, vehicle id f[3], f[38]):', hulls)
    items = [(int(f[2]), f[3], f[4]) for _, f in csv_rows(texts['ITM/Rogue_mp.itm'])]
    upgrades = sorted(str(i) for i, _, c in items if c in UPGRADE_FAMILIES)
    print('hull ids', sorted(v for _, v, _ in hulls), 'ITM ids', len(items), 'unique', len({i for i, _, _ in items}))
    print('upgrade ids', len(upgrades), upgrades)
    for i in (2000, 2011, 1004, 2003, 2):
        print('ITM', i, [x for x in items if x[0] == i])

    for rel in PINNED:
        hits = [(n, [k for k in KEYWORDS if k in l.lower()]) for n, l in enumerate(lines(texts[rel]), 1)
                if any(k in l.lower() for k in KEYWORDS)]
        print('keyword hits', rel, len(hits), hits)
    print("CFG lines containing 'wipecharacter':", sum('wipecharacter' in l.lower() for l in lines(texts['CFG/rogue.cfg'])))


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else os.environ['RT_ORIGINAL_ASSETS_ROOT'])
