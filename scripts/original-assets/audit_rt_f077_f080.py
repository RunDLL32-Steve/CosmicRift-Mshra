#!/usr/bin/env python3
"""Read-only audit for RT-F077..RT-F080 (evidence for tests/Unit/OriginalMicroMissileChildTests.cs).

Usage: python3 -I audit_rt_f077_f080.py <assets-root>   (cosmic-rift-assets at the pinned commit eb1514ea)

Independently of the C# resolver (OriginalItemReferenceResolver, RT-F076) it verifies the SHA-256 of ITM/Rogue_mp.itm, VEH/Rogue.veh,
RPG/Rogue.rpg and CFG/rogue.cfg and prints, for the four ChildProjectiles rows (ids 1045, 1048, 1046, 1049, rows 2..5): the identity
and pricing fields, f[20] / f[31] / f[142] / f[147], every text-slot pair (archive, member) with the integers that follow (up to six),
whether BLO/<archive>.blo exists with the EXACT case and with a case-insensitive match (an LFS pointer is enough; content is never
read), every record that names the item (ITM f[31], f[142], type-8 entries from f[97] stride 6 with the raw offset at +4, VEH f[68]
drop item, VEH f[129..131] addinv/wipeinv by exact name), and the fields that differ between the four rows (raw values, no
interpretation). It also shows the parent chain (launcher 1047 -> ammo 2002 -> blast 3008). The same four ids in the other ITM files
are listed by file and line only. Never writes to the assets. Field names are upstream-parser names, not original-executable evidence.
"""
import hashlib, os, re, sys

PINNED = {
    'ITM/Rogue_mp.itm': 'dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7',
    'VEH/Rogue.veh': '9eefa85a04587867616be786fffc664f3ebc0fb1d50ba6f6fd303a7a8a988572',
    'RPG/Rogue.rpg': '44d5e6d3c78bfa445c723260086235017bd1973557103003aa8f3b71e30ae123',
    'CFG/rogue.cfg': '82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90',
}
IDS = (1045, 1048, 1046, 1049)
FIELD = re.compile(r'[ \t]*("[^"]*"|[^,]*)[ \t]*(,|$)')
INT = re.compile(r'^-?\d+$')


def text(root, rel):
    data = open(os.path.join(root, *rel.split('/')), 'rb').read()
    if rel in PINNED and hashlib.sha256(data).hexdigest() != PINNED[rel]:
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


def text_pairs(f):
    idx = [i for i in range(7, len(f)) if not INT.match(f[i])]
    pairs, i = [], 0
    while i < len(idx):
        j = i
        while j + 1 < len(idx) and idx[j + 1] == idx[j] + 1:
            j += 1
        if j - i + 1 == 2:
            pairs.append(idx[i])
        i = j + 1
    return pairs, idx


def main(root):
    itm = list(rows(text(root, 'ITM/Rogue_mp.itm')))
    veh = list(rows(text(root, 'VEH/Rogue.veh')))
    text(root, 'RPG/Rogue.rpg')
    text(root, 'CFG/rogue.cfg')
    byid = {int(f[2]): (n, f) for n, f in itm}
    blo_dir = os.path.join(root, 'BLO')
    blo_files = os.listdir(blo_dir)
    sel = []
    for item_id in IDS:
        row, f = byid[item_id]
        sel.append(f)
        print(f'== ITM row {row} id {item_id} type {f[0]} version {f[1]} fields {len(f)} name {f[3]!r} category {f[4]!r} logic f[5]={f[5]!r}')
        print(f'   description f[6]={f[6]!r} mass f[7]={f[7]} buy f[8]={f[8]} sell f[15]={f[15]} f[20]={f[20]} f[31] ammo={f[31]} f[142] explode={f[142]} f[147] event={f[147]!r}')
        pairs, idx = text_pairs(f)
        for p in pairs:
            stop = min([i for i in idx if i > p + 1] + [len(f)])
            k = min(6, stop - (p + 2))
            a = f[p]
            exact = os.path.exists(os.path.join(blo_dir, a + '.blo'))
            ci = [x for x in blo_files if x.lower() == (a + '.blo').lower()]
            print(f'   pair f[{p}],f[{p+1}] = {a!r},{f[p+1]!r} ints f[{p+2}..{p+1+k}]={f[p+2:p+2+k]} BLO/{a}.blo exact-case={exact} case-insensitive={ci}')
        print('   referenced by:')
        for n, g in itm:
            if g[0] == '6' and len(g) == 227:
                if int(g[31]) == item_id: print(f'     ITM row {n} id {g[2]} {g[3]!r} f[31] ammo')
                if int(g[142]) == item_id: print(f'     ITM row {n} id {g[2]} {g[3]!r} f[142] explode')
            if g[0] == '8':
                for i in range(97, len(g) - 4, 6):
                    if INT.match(g[i]) and int(g[i]) == item_id:
                        print(f'     ITM row {n} id {g[2]} {g[3]!r} type-8 entry f[{i}] raw offset f[{i+4}]={g[i+4]}')
        for n, g in veh:
            if g[0] == '2' and len(g) > 131:
                if g[68] == str(item_id): print(f'     VEH row {n} id {g[2]} {g[3]!r} f[68] drop item')
                for kk in range(129, 132):
                    for a in g[kk].split(','):
                        if '=' in a and a.split('=', 1)[0].lower() in ('addinv', 'wipeinv') and a.split('=', 1)[1] == f[3]:
                            print(f'     VEH row {n} id {g[2]} {g[3]!r} f[{kk}] {a}')
    print('== fields that differ between the four rows (raw values in id order 1045, 1048, 1046, 1049):')
    for i in range(227):
        v = [f[i] for f in sel]
        if len(set(v)) > 1:
            print(f'   f[{i}] = {v}')
    print('== parent chain:')
    for pid in (1047, 2002, 3008):
        n, g = byid[pid]
        extra = f' f[31]={g[31]} f[142]={g[142]}' if g[0] == '6' and len(g) == 227 else ''
        print(f'   ITM row {n} id {pid} type {g[0]} {g[3]!r} fields {len(g)}{extra}')
    n, g = byid[3008]
    print('   3008 type-8 entries (field, id, raw offset f[+4]):', [(i, g[i], g[i + 4]) for i in range(97, len(g) - 4, 6) if INT.match(g[i]) and int(g[i]) != 0])
    print('== same ids in the other ITM files (line numbers, raw text only):')
    for rel in sorted(os.listdir(os.path.join(root, 'ITM'))):
        if not rel.endswith('.itm'):
            continue
        for n, g in rows(text(root, 'ITM/' + rel)):
            if len(g) > 4 and g[2].strip().isdigit() and int(g[2]) in IDS:
                print(f'   ITM/{rel}:{n} id {g[2]} category {g[4]!r} fields {len(g)}')


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else os.environ.get('RT_ORIGINAL_ASSETS_ROOT'))
