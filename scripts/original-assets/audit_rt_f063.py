#!/usr/bin/env python3
"""Read-only audit for RT-F063 "Space Storm radiation" (evidence for tests/Unit/OriginalSpaceStormTests.cs).

Usage: python3 -I audit_rt_f063.py <assets-root>   (cosmic-rift-assets at the pinned commit eb1514ea)

Independently of the C# reader it verifies the SHA-256 of ITM/Rogue_mp.itm, VEH/Rogue.veh, RPG/Rogue.rpg,
CFG/rogue.cfg and NWS/Rogue.nws, then
  1. prints NWS lines 329-332 verbatim with the dated heading above them,
  2. searches every text (non-integer) field of the ITM, VEH and RPG rows and every CFG section name, key and value for the
     name terms (storm, radiat, discharg) and the mechanism terms (radio, grow, shrink, nebula, cloud), case-insensitive,
  3. prints the VEH type-5 (computer) rows,
  4. prints every other NWS line containing 'storm',
  5. as RAW TEXT ONLY (no LIO parser; queue G6 owns the reader): plain substring hits of the name terms in LIO/Rogue.lio and,
     for context, in every other file of LIO/ ("seen as raw text, not read/pinned"). Other LIO files are not the selected
     package (CFG/rogue.cfg LioFile=Rogue.lio); they are shown only to document where the words occur.
Never writes to the assets.
"""
import hashlib, os, re, sys

PINNED = {
    'ITM/Rogue_mp.itm': 'dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7',
    'VEH/Rogue.veh': '9eefa85a04587867616be786fffc664f3ebc0fb1d50ba6f6fd303a7a8a988572',
    'RPG/Rogue.rpg': '44d5e6d3c78bfa445c723260086235017bd1973557103003aa8f3b71e30ae123',
    'CFG/rogue.cfg': '82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90',
    'NWS/Rogue.nws': '7fff837b85d52a78b93d50b5b8721d6e567b72b5989c5a96f4212add90aaa895',
}
NAME_TERMS = ['storm', 'radiat', 'discharg']
MECHANISM_TERMS = ['radio', 'grow', 'shrink', 'nebula', 'cloud']
ALL_TERMS = NAME_TERMS + MECHANISM_TERMS
FIELD = re.compile(r'[ \t]*("[^"]*"|[^,]*)[ \t]*(,|$)')
HEADING = re.compile(r'^\*+\s*(\d{1,2}/\d{1,2}/\d{2})\s*$')
INT = re.compile(r'^[+-]?\d+$')


def text(root, rel):
    data = open(os.path.join(root, *rel.split('/')), 'rb').read()
    if rel in PINNED and hashlib.sha256(data).hexdigest() != PINNED[rel]:
        sys.exit('hash mismatch: ' + rel)
    return data.decode('latin-1')


def csv_rows(t):
    """Yield (row number = physical line number, fields) for each non-blank line; quoted fields keep no quotes."""
    for n, line in enumerate(t.split('\n'), 1):
        line = line.rstrip('\r')
        if not line.strip():
            continue
        fields, pos = [], 0
        while True:
            m = FIELD.match(line, pos)
            fields.append(m.group(1))
            if m.group(2) == '':
                break
            pos = m.end()
        yield n, [f[1:-1] if len(f) >= 2 and f[0] == '"' and f[-1] == '"' else f for f in fields]


def main(root):
    nws = text(root, 'NWS/Rogue.nws').replace('\r\n', '\n').replace('\r', '\n').split('\n')
    print('NWS lines 329-332:')
    head = max(n for n in range(1, 330) if HEADING.match(nws[n - 1]))
    print(f'  heading line {head}: {nws[head - 1]!r}')
    for n in range(329, 333):
        print(f'  {n}: {nws[n - 1]!r}')
    others = [n for n, l in enumerate(nws, 1) if 'storm' in l.lower() and not 329 <= n <= 332]
    print('other NWS lines containing storm:')
    for n in others:
        print(f'  {n}: {nws[n - 1]!r}')

    hits, counts = [], {}
    for rel in ['ITM/Rogue_mp.itm', 'VEH/Rogue.veh', 'RPG/Rogue.rpg']:
        rows = list(csv_rows(text(root, rel)))
        counts[rel] = len(rows)
        for n, f in rows:
            owner = f'{rel.split("/")[0]} {f[2]} {f[3]}' if rel != 'RPG/Rogue.rpg' else f'RPG row {n}'
            for j, v in enumerate(f):
                if INT.match(v.strip()):
                    continue
                for t in ALL_TERMS:
                    if t in v.lower():
                        hits.append((rel, n, j, t, owner, v))
    cfg_lines = 0
    sec = None
    for n, line in enumerate(text(root, 'CFG/rogue.cfg').split('\n'), 1):
        line = line.rstrip('\r')
        if not line:
            continue
        cfg_lines += 1
        if line.startswith('['):
            sec, owner, t_text = line[1:-1], f'CFG [{line[1:-1]}]', line[1:-1]
        else:
            k = line.split('=', 1)[0]
            owner, t_text = f'CFG [{sec}] {k}', line
        for t in ALL_TERMS:
            if t in t_text.lower():
                hits.append(('CFG/rogue.cfg', n, 0, t, owner, t_text))
    print('records/lines searched:', counts, 'CFG non-blank lines', cfg_lines)
    print('text-field hits (file, row, field, term, owner, text):')
    for h in hits:
        print('  ', h)
    print('name-term hits:', sum(1 for h in hits if h[3] in NAME_TERMS), ' total hits:', len(hits))
    print('name-term hits whose owner and text do not mention Bio (Skithzar BioStorm items / Biostorm Manufactory label):',
          [h for h in hits if h[3] in NAME_TERMS and 'bio' not in (h[4] + h[5]).lower()])

    print('VEH type-5 (computer) rows:')
    for n, f in csv_rows(text(root, 'VEH/Rogue.veh')):
        if f[0] == '5':
            print(f'   row {n} id {f[2]} {f[3]!r}')

    print('LIO plain-text hits (seen as raw text, not read/pinned):')
    lio_dir = os.path.join(root, 'LIO')
    for name in sorted(os.listdir(lio_dir)):
        raw = open(os.path.join(lio_dir, name), 'rb').read().decode('latin-1').split('\n')
        for n, line in enumerate(raw, 1):
            low = line.lower()
            if any(t in low for t in NAME_TERMS):
                tag = 'selected' if name == 'Rogue.lio' else 'NOT the selected package'
                print(f'  LIO/{name} line {n} [{tag}]: {line.rstrip(chr(13))[:200]}')
    rogue = open(os.path.join(lio_dir, 'Rogue.lio'), 'rb').read().decode('latin-1').lower()
    print('LIO/Rogue.lio contains storm/radiat/discharg:', any(t in rogue for t in NAME_TERMS))


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else os.environ['RT_ORIGINAL_ASSETS_ROOT'])
