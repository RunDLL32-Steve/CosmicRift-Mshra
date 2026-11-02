#!/usr/bin/env python3
"""Read-only G5 value audit of the weapon/ammo/child-chain records (evidence for docs/original-research/G5_WEAPON_VALUE_AUDIT.md).

Usage: python3 -I audit_g5_weapon_values.py <assets-root>   (cosmic-rift-assets at the pinned commit eb1514ea)

Verifies the SHA-256 of ITM/Rogue_mp.itm and CFG/rogue.cfg, then prints, with exact row/field/raw value:
  1. every ammo item referenced through f[31] (upstream name useAmmoID) by a type-6 or type-8 row, with the referencing
     rows' f[32] ammoUsedPerShot and f[33] ammoCapacity, and the ammo row's own general fields;
  2. value distributions over the 92 type-6/227-field rows for the launch, lifetime, trigger and collision-related fields,
     listing every row that carries a negative value;
  3. unnamed type-6 fields (indices the upstream Projectile loader skips) that are non-zero in any row;
  4. every type-8 row's child entries (f[97] stride 6) with all six raw ints;
  5. the same negative values of f[94]/f[110] in the other ITM files of the pinned repository (pattern only);
  6. the rogue.cfg keys named in the audit.
Field names are the upstream InfantryOnline parser names (ItmInfo.Projectile.cs / ItmInfo.MultiUse.cs at bb97579e), not
original-executable evidence. Nothing is interpreted, scaled or written; the assets are never modified.
"""
import collections, hashlib, os, re, sys

PINNED = {
    'ITM/Rogue_mp.itm': 'dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7',
    'CFG/rogue.cfg': '82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90',
}
FIELD = re.compile(r'[ \t]*("[^"]*"|[^,]*)[ \t]*(,|$)')
INT = re.compile(r'^-?\d+$')

# Upstream ItmInfo.Projectile.Load names for the fields audited here.
NAMES = {
    54: 'fireDelay', 55: 'fireDelayOther', 56: 'maxFireDelay', 57: 'entryFireDelay', 58: 'reloadDelayNormal',
    68: 'prefireDelay', 69: 'reliability', 93: 'horizontalFriction', 94: 'inheritedSpeed', 95: 'inheritZSpeed',
    96: 'startHeightAdjust', 97: 'lowRotationAngle', 98: 'highRotationAngle', 99: 'lowFireAngle', 100: 'highFireAngle',
    101: 'muzzleVelocity', 102: 'gravityAcceleration', 103: 'horizontalBounceSpeed', 104: 'bounceCount',
    105: 'floorBounceVerticalSpeed', 106: 'floorBounceHorizontalSpeed', 107: 'floorBounceCount', 108: 'proximityRadius',
    109: 'triggerWeight', 110: 'aliveTime', 111: 'rotationalStartTime', 112: 'rotationalEndTime', 113: 'rotationalSpeed',
    114: 'rotationalAcceleration', 115: 'inactiveTime', 116: 'damageMode', 117: 'damageAccessible', 119: 'trailDelay',
    141: 'preventPointBlank', 142: 'explodeItem', 143: 'projectileRadarMode', 150: 'vehicleGravity',
}
# Indices below 175 that the upstream Projectile loader never reads (175..226 are graphics/sounds).
UNNAMED = [64, 118, 121, 144, 145, 149]
CFG_KEYS = ('ExplodeItemDistance', 'InheritSpeedOnDefaultChange', 'DefaultInheritSpeedPercent', 'DefaultInheritZSpeedPercent')


def read(root, rel, verify=True):
    data = open(os.path.join(root, *rel.split('/')), 'rb').read()
    if verify and rel in PINNED and hashlib.sha256(data).hexdigest() != PINNED[rel]:
        sys.exit('hash mismatch: ' + rel)
    return data.decode('latin-1')


def rows(t):
    for n, line in enumerate(t.split('\n'), 1):
        line = line.rstrip('\r')
        if not line.strip():
            continue
        fields, pos = [], 0
        while True:
            m = FIELD.match(line, pos)
            v = m.group(1)
            fields.append(v[1:-1] if v.startswith('"') else v)
            if m.group(2) == '':
                break
            pos = m.end()
        yield n, fields


def ival(f, i):
    return int(f[i]) if i < len(f) and INT.match(f[i]) else None


def main(root):
    itm = list(rows(read(root, 'ITM/Rogue_mp.itm')))
    cfg = read(root, 'CFG/rogue.cfg')
    print('hashes verified:', ', '.join(PINNED))
    byid = {int(f[2]): (n, f) for n, f in itm}
    proj = [(n, f) for n, f in itm if f[0] == '6' and len(f) == 227]
    multi = [(n, f) for n, f in itm if f[0] == '8']
    print(f'type-6/227 rows: {len(proj)}; type-8 rows: {len(multi)}')

    print('\n== 1. ammo items (f[31] useAmmoID, not self) ==')
    refs = collections.defaultdict(list)
    for n, f in proj + multi:
        a = ival(f, 31)
        if a and a != int(f[2]):
            refs[a].append((n, f))
    for a in sorted(refs):
        n, g = byid[a]
        print(f'ammo id {a} row {n} type {g[0]} fields {len(g)} name {g[3]!r} category {g[4]!r} weight f[7]={g[7]} buy f[8]={g[8]} droppable f[10]={g[10]} '
              f'maxAllowed f[13]={g[13]} pickupMode f[14]={g[14]} sell f[15]={g[15]} expireTimer f[16]={g[16]} heldCategory f[20]={g[20]}')
        for rn, f in refs[a]:
            print(f'    used by row {rn} id {f[2]} type {f[0]} {f[3]!r}: f[32] ammoUsedPerShot={f[32]} f[33] ammoCapacity={f[33]} f[34] requiredItem={f[34]}')
    selfref = [(n, f) for n, f in proj + multi if ival(f, 31) == int(f[2])]
    print(f'self-referencing f[31] rows: {len(selfref)}:', [(n, int(f[2])) for n, f in selfref])

    print('\n== 2. type-6 value distributions (raw) ==')
    for i, name in NAMES.items():
        c = collections.Counter(int(f[i]) for _, f in proj)
        neg = [(n, int(f[2]), f[3], int(f[i])) for n, f in proj if int(f[i]) < 0]
        top = ', '.join(f'{v}x{k}' for k, v in sorted(c.items(), key=lambda kv: (-kv[1], kv[0]))[:12])
        print(f'f[{i}] {name}: {len(c)} distinct; {top}{" ..." if len(c) > 12 else ""}')
        if len(neg) > 20:
            print(f'    negative in {len(neg)} rows: {dict(collections.Counter(r[3] for r in neg))}')
        else:
            for r in neg:
                print(f'    negative: row {r[0]} id {r[1]} {r[2]!r} = {r[3]}')

    print('\n== 2b. rows with negative f[94], f[104] or f[110], with launch/trigger context and the f[6] description ==')
    for n, f in proj:
        if int(f[94]) < 0 or int(f[104]) < 0 or int(f[110]) < 0:
            ctx = ' '.join(f'{NAMES[i]}={f[i]}' for i in (94, 101, 104, 108, 109, 110, 115, 116, 117, 141, 142))
            print(f'  row {n} id {f[2]} {f[3]!r} cat {f[4]!r}: {ctx}\n      f[6]={f[6]!r}')

    print('\n== 3. unnamed type-6 fields (skipped by the upstream loader) ==')
    for i in UNNAMED:
        c = collections.Counter(f[i] for _, f in proj)
        print(f'f[{i}]: {dict(c)}')

    print('\n== 4. type-8 child entries (id, deltaX, deltaY, deltaZ, deltaAngle, multiLinkMode) ==')
    for n, f in multi:
        ents = []
        for i in range(97, len(f), 6):
            e = f[i:i + 6]
            if len(e) < 6 or not all(INT.match(x) for x in e):
                ents.append((i, 'partial', e))
                continue
            if int(e[0]) != 0 or any(int(x) for x in e[1:]):
                ents.append((i, tuple(int(x) for x in e)))
        print(f'row {n} id {f[2]} {f[3]!r} fields {len(f)} (97+6k: {(len(f) - 97) % 6 == 0}):')
        for e in ents:
            print('    f[%d]' % e[0], e[1:])

    print('\n== 5. negative f[94]/f[110] in other pinned ITM files (pattern only, no hash pin) ==')
    for rel in sorted(os.listdir(os.path.join(root, 'ITM'))):
        if not rel.endswith('.itm') or rel == 'Rogue_mp.itm':
            continue
        c94, c110, tot = collections.Counter(), collections.Counter(), 0
        for n, f in rows(read(root, 'ITM/' + rel, verify=False)):
            if f[0].strip() == '6' and len(f) > 110:
                tot += 1
                for i, c in ((94, c94), (110, c110)):
                    v = ival(f, i)
                    if v is not None and v < 0:
                        c[v] += 1
        if c94 or c110:
            print(f'{rel}: {tot} type-6 rows; f[94]<0 {dict(c94)}; f[110]<0 {dict(c110)}')

    print('\n== 6. rogue.cfg keys ==')
    section = ''
    for n, line in enumerate(cfg.split('\n'), 1):
        s = line.strip()
        if s.startswith('['):
            section = s
        elif s.split('=', 1)[0] in CFG_KEYS:
            print(f'CFG/rogue.cfg line {n} {section} {s}')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    main(sys.argv[1])
