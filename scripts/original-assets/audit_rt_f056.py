#!/usr/bin/env python3
"""Read-only audit for RT-F056 (evidence for tests/Unit/OriginalProductionItemTests.cs).

Usage: python3 -I audit_rt_f056.py <assets-root>   (cosmic-rift-assets at the pinned commit)
Prints ITM type-7 ids 46..49 with f[7], f[8], f[93], the linked VEH v53 type-5 row and VEH f[6],
and the Build blocks of VEH id 413 (Rogue Trader Outpost, f[253+14k]) that carry the raw cost candidates.
Never writes to the assets.
"""
import sys, os

def rows(path):
    with open(path, encoding='latin-1') as fh:
        return [l.rstrip('\r') for l in fh.read().split('\n') if l.strip()]

def fields(line):
    return [x.strip() for x in line.split(',')]

def main(root):
    itm = rows(os.path.join(root, 'ITM', 'Rogue_mp.itm'))
    veh = rows(os.path.join(root, 'VEH', 'Rogue.veh'))
    for n, line in enumerate(itm, 1):
        f = fields(line)
        if f[0] == '7' and f[2] in ('46', '47', '48', '49'):
            v = [(i, fields(l)) for i, l in enumerate(veh, 1) if fields(l)[0] == '5' and fields(l)[2] == f[93]]
            print('ITM row', n, 'id', f[2], f[3], 'fields', len(f), 'f7', f[7], 'f8', f[8], 'f93', f[93],
                  '-> VEH row', v[0][0], 'f6', v[0][1][6], 'matches', len(v))
    outpost = [fields(l) for l in veh if fields(l)[0] == '5' and fields(l)[2] == '413'][0]
    for k in range(4, 8):
        b = 253 + 14 * k
        print('VEH 413 block', b, outpost[b], 'item', outpost[b + 1], 'f+5', outpost[b + 5], 'f+6', outpost[b + 6], 'f+7', outpost[b + 7])

if __name__ == '__main__':
    main(sys.argv[1])
