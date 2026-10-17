#!/usr/bin/env python3
"""Read-only audit for RT-F067 (evidence for tests/Unit/OriginalEventStringsTests.cs).

Usage: python3 -I audit_rt_f067.py <assets-root>   (cosmic-rift-assets at the pinned commit eb1514ea)

Independently of the C# reader it verifies the SHA-256 of VEH/Rogue.veh, CFG/rogue.cfg and ITM/Rogue_mp.itm, then
extracts every event-string slot: VEH f[129..131] of every row (quote-aware split; all must be quoted), CFG [Event]
keys plus every EventString / TeamEventStartGame key, ITM type-6 f[147]. It splits non-empty strings on ',' and each
action at the first '=', and prints the strings, exact and lower-cased verb counts, unknown-grammar actions, item-name
resolution of addinv/wipeinv arguments (exact vs case-insensitive) and event<Name> call resolution against [Event].
It also confirms that no other VEH/ITM field and no LIO/Rogue.lio line contains '='. Never writes to the assets.
"""
import collections, hashlib, os, re, sys

PINNED = {
    'VEH/Rogue.veh': '9eefa85a04587867616be786fffc664f3ebc0fb1d50ba6f6fd303a7a8a988572',
    'CFG/rogue.cfg': '82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90',
    'ITM/Rogue_mp.itm': 'dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7',
}
ACTION = re.compile(r'^[A-Za-z][A-Za-z0-9]*(=[A-Za-z0-9 \-]+)?$')
FIELD = re.compile(r'[ \t]*("[^"]*"|[^,]*)[ \t]*(,|$)')


def text(root, rel):
    data = open(os.path.join(root, *rel.split('/')), 'rb').read()
    if rel in PINNED and hashlib.sha256(data).hexdigest() != PINNED[rel]:
        sys.exit('hash mismatch: ' + rel)
    return data.decode('latin-1')


def csv_rows(t):
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
        yield n, fields


def main(root):
    slots = []  # (label, raw)
    for n, f in csv_rows(text(root, 'VEH/Rogue.veh')):
        for k in range(3):
            v = f[129 + k]
            assert v.startswith('"') and v.endswith('"'), (n, 129 + k, v)
            slots.append((f'VEH row {n} f[{129 + k}] id {f[2]} {f[3].strip(chr(34))}', v[1:-1]))
        for j, v in enumerate(f):
            if '=' in v and not 129 <= j <= 131:
                print('UNEXPECTED VEH = at row', n, 'field', j, v)
    sec = None
    for n, line in enumerate(text(root, 'CFG/rogue.cfg').split('\n'), 1):
        line = line.rstrip('\r')
        if line.startswith('['):
            sec = line[1:-1]
            continue
        if '=' not in line:
            continue
        key, val = line.split('=', 1)
        if sec == 'Event' or key in ('EventString', 'TeamEventStartGame'):
            slots.append((f'CFG line {n} [{sec}] {key}', val))
    items = {}
    for n, f in csv_rows(text(root, 'ITM/Rogue_mp.itm')):
        items[f[3].strip('"')] = (n, f[2])
        if f[0].strip() == '6':
            v = f[147]
            assert v.startswith('"') and v.endswith('"'), (n, v)
            slots.append((f'ITM row {n} f[147] id {f[2]}', v[1:-1]))
        for j, v in enumerate(f):
            if '=' in v:
                print('UNEXPECTED ITM = at row', n, 'field', j, v)
    lio = os.path.join(root, 'LIO', 'Rogue.lio')
    if os.path.exists(lio):
        print("LIO/Rogue.lio lines containing '=':", sum('=' in l for l in open(lio, encoding='latin-1')))

    kinds = collections.Counter(s[0].split(' ')[0] + (' [' + s[0].split('[', 1)[1].split(']')[0].rstrip('0123456789') + ']' if s[0].startswith('CFG') else '') for s in slots)
    nonempty = [s for s in slots if s[1]]
    print('slots scanned', len(slots), dict(kinds), 'non-empty', len(nonempty))
    exact, folded, unknown = collections.Counter(), collections.Counter(), []
    names_ci = {k.lower(): (k, v) for k, v in items.items()}
    event_keys = [s[0].rsplit(' ', 1)[1] for s in slots if '[Event]' in s[0]]
    for label, raw in nonempty:
        print(f'  {label}: {raw!r}')
        for a in raw.split(','):
            if not ACTION.match(a) or (('=' in a) and (a.split('=', 1)[1][0] == ' ' or a.endswith(' '))):
                unknown.append((label, a))
                continue
            verb, _, arg = a.partition('=')
            exact[verb] += 1
            folded[verb.lower()] += 1
            if arg and verb.lower() in ('addinv', 'wipeinv'):
                ex = items.get(arg)
                ci = names_ci.get(arg.lower())
                print(f'      item {arg!r}: exact={ex} case-insensitive={ci}')
            if not arg and verb.lower().startswith('event') and len(verb) > 5:
                t = verb[5:]
                print(f'      call {t!r}: exact={t in event_keys} case-insensitive={t.lower() in [k.lower() for k in event_keys]}')
    print('actions', sum(exact.values()), 'exact verbs', len(exact), dict(exact))
    print('folded verbs', len(folded), dict(folded))
    print('unknown actions', unknown)


if __name__ == '__main__':
    main(sys.argv[1])
