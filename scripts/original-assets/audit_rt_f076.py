#!/usr/bin/env python3
"""Read-only audit for RT-F076 (evidence for tests/Unit/OriginalItemReferencesTests.cs).

Usage: python3 -I audit_rt_f076.py <assets-root> [item-id]   (cosmic-rift-assets at the pinned commit eb1514ea; default id 1004)

Independently of the C# resolver it verifies the SHA-256 of ITM/Rogue_mp.itm, VEH/Rogue.veh and RPG/Rogue.rpg, then prints
for the chosen ITM row: the identity/pricing fields, f[20] and the CFG [HeldCategory] row at that id, the skill-logic text,
f[31]/f[142] (projectile layout), every text-slot pair (archive, member) with the integers that follow (up to six, fewer when
the next text field or the record end comes first), whether BLO/<archive>.blo exists in the package (an LFS pointer is enough;
the content is never read), every record whose reference fields name the item (ITM f[31], f[142], type-8 entries from f[97]
stride 6, VEH f[68] drop item, VEH f[129..131] addinv/wipeinv by exact name), and every non-zero integer field of the row that
equals some ITM or VEH id (numeric coincidences: NOT references, listed only so nobody mistakes them for one).
It also checks, over all rows, that every type-6/227-field row has the same text-field positions. Never writes to the assets.
"""
import collections, hashlib, os, re, sys

PINNED = {
    'ITM/Rogue_mp.itm': 'dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7',
    'VEH/Rogue.veh': '9eefa85a04587867616be786fffc664f3ebc0fb1d50ba6f6fd303a7a8a988572',
    'RPG/Rogue.rpg': '44d5e6d3c78bfa445c723260086235017bd1973557103003aa8f3b71e30ae123',
    'CFG/rogue.cfg': '82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90',
}
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
        fields, quoted, pos = [], [], 0
        while True:
            m = FIELD.match(line, pos)
            v = m.group(1)
            quoted.append(v.startswith('"'))
            fields.append(v[1:-1] if v.startswith('"') else v)
            if m.group(2) == '':
                break
            pos = m.end()
        yield n, fields, quoted


def text_pairs(f):
    """Runs of exactly two consecutive non-integer, non-empty, unquoted-in-source fields after f[6]: (archive, member)."""
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


def main(root, item_id):
    itm = list(rows(text(root, 'ITM/Rogue_mp.itm')))
    veh = list(rows(text(root, 'VEH/Rogue.veh')))
    text(root, 'RPG/Rogue.rpg')
    cfg = text(root, 'CFG/rogue.cfg')
    byid = {int(f[2]): (n, f) for n, f, q in itm}
    row, f = byid[item_id]
    print(f'ITM row {row} id {item_id} type {f[0]} version {f[1]} fields {len(f)} name {f[3]!r} category {f[4]!r} logic f[5]={f[5]!r}')
    print(f'description f[6]={f[6]!r} mass f[7]={f[7]} buy f[8]={f[8]} sell f[15]={f[15]} f[20]={f[20]}')
    sec = re.search(r'\[HeldCategory\](.*?)(?=\n\[|\Z)', cfg, re.S).group(1)
    held = dict(re.findall(r'^(\w+)=(-?\d+)', sec, re.M))
    k = f[20]
    print(f'CFG [HeldCategory] Limit{k}={held.get("Limit" + k)} ExtendedLimit{k}={held.get("ExtendedLimit" + k)} (id mapping is a candidate, not proof)')
    if f[0] == '6' and len(f) == 227:
        print(f'f[31] ammo={f[31]} f[142] explode={f[142]} f[147] event={f[147]!r}')

    pairs, idx = text_pairs(f)
    print('text fields (index > 6):', idx)
    for n, p in enumerate(pairs):
        nxt = pairs[n + 1] if n + 1 < len(pairs) else len(f)
        extra = [i for i in idx if i > p + 1 and i < nxt]
        stop = min([i for i in idx if i > p + 1] + [len(f)])
        k = min(6, stop - (p + 2))
        ints = f[p + 2:p + 2 + k]
        blo = os.path.join(root, 'BLO', f[p] + '.blo')
        print(f'  pair f[{p}],f[{p+1}] = {f[p]!r},{f[p+1]!r} ints f[{p+2}..{p+1+k}]={ints} BLO/{f[p]}.blo present={os.path.exists(blo)}')

    shapes = collections.Counter()
    for n, g, q in itm:
        if g[0] == '6' and len(g) == 227:
            shapes[tuple(text_pairs(g)[1])] += 1
    print('type-6/227 text-field position sets:', {len(k): v for k, v in shapes.items()}, 'distinct', len(shapes))

    print('referenced by:')
    for n, g, q in itm:
        gid = int(g[2])
        if g[0] == '6' and len(g) == 227:
            if int(g[31]) == item_id: print(f'  ITM row {n} id {gid} {g[3]!r} f[31] ammo')
            if int(g[142]) == item_id: print(f'  ITM row {n} id {gid} {g[3]!r} f[142] explode')
        if g[0] == '8':
            for i in range(97, len(g) - 4, 6):
                if INT.match(g[i]) and int(g[i]) == item_id:
                    print(f'  ITM row {n} id {gid} {g[3]!r} type-8 entry f[{i}]')
    for n, g, q in veh:
        if g[0] == '2' and len(g) > 131:
            if g[68] == str(item_id): print(f'  VEH row {n} id {g[2]} {g[3]!r} f[68] drop item')
            for kk in range(129, 132):
                for a in g[kk].split(','):
                    if '=' in a and a.split('=', 1)[0].lower() in ('addinv', 'wipeinv') and a.split('=', 1)[1] == f[3]:
                        print(f'  VEH row {n} id {g[2]} {g[3]!r} f[{kk}] {a}')
    nws = open(os.path.join(root, 'NWS', 'Rogue.nws'), 'rb').read().decode('latin-1').split('\n')
    print('NWS/Rogue.nws text lines seen (raw text, not parsed or pinned by the C# reader; the file has no "Plasma" and no id 1004):')
    for ln in (34, 144, 146, 251, 253, 254):
        print(f'  line {ln}: {nws[ln-1].rstrip()[:150]}')
    ids_itm = {int(g[2]): g[3] for n, g, q in itm}
    ids_veh = {int(g[2]): g[3] for n, g, q in veh}
    print('numeric coincidences (non-zero ints equal to an id; NOT references):')
    for i, v in enumerate(f):
        if INT.match(v) and int(v) > 0 and i not in (0, 2):
            if int(v) in ids_itm: print(f'  f[{i}]={v} equals ITM id of {ids_itm[int(v)]!r}')
            if int(v) in ids_veh: print(f'  f[{i}]={v} equals VEH id of {ids_veh[int(v)]!r}')


if __name__ == '__main__':
    root = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('RT_ORIGINAL_ASSETS_ROOT')
    main(root, int(sys.argv[2]) if len(sys.argv) > 2 else 1004)
