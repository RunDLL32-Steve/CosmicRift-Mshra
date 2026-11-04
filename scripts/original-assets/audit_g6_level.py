#!/usr/bin/env python3
"""Read-only G6 oracle for the LVL/LIO reader tests (tests/Unit/OriginalLevelTests.cs).

Usage: python3 -I audit_g6_level.py <assets-root>   (cosmic-rift-assets at the pinned commit eb1514ea, with the Git LFS
content of LVL/Rogue.lvl pulled; the same bytes are in InfantryOnline/Zone-Assets 401633b)

Verifies the SHA-256 of LVL/Rogue.lvl and LIO/Rogue.lio, then decodes the LVL independently of the C# reader with the
layout of the InfantryOnline emulator (Infantry-Online-Server bb97579, dotnetcore/Assets/Lvl/LvlInfo*.cs): 5,152-byte
header, 64-byte blob references for version 6, three RLE planes of width x height bytes, then the entity table. Prints
the header values, blob reference counts, plane offsets and SHA-256s, entity table size, physics/vision/terrain
histograms and the terrain under the station hide rows of the LIO. Nothing is interpreted beyond the emulator's field
names; nothing is written; the assets are never modified.
"""
import collections, hashlib, os, struct, sys

PINNED = {
    'LVL/Rogue.lvl': '7034476858b86e9b11a41eaaa0d636003eb432a4b682e45d264def8b425cf02f',
    'LIO/Rogue.lio': '73d008c5168efa46f73eaccdd78c461a121d6ae281b6d2a5e95a73e43bec2f6a',
}


def read(root, rel):
    data = open(os.path.join(root, *rel.split('/')), 'rb').read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != PINNED[rel]:
        sys.exit(f'{rel}: SHA-256 {digest} != pinned {PINNED[rel]} (is LVL still a Git LFS pointer?)')
    return data


def rle(b, p, n):
    out = bytearray()
    while len(out) < n:
        r = b[p]; p += 1
        if r == 0:
            r = (b[p] << 8) | b[p + 1]; p += 2
            if r == 0:
                sys.exit(f'zero-length run at {p - 3}')
        v = b[p]; p += 1
        if len(out) + r > n:
            sys.exit(f'run overruns plane at {p}')
        out += bytes([v]) * r
    return out, p


def main(root):
    b = read(root, 'LVL/Rogue.lvl')
    lio = read(root, 'LIO/Rogue.lio').decode('latin-1')
    ver, w, h, ents, floors, objs = struct.unpack_from('<6i', b, 0)
    print('version width height entities floors objects:', ver, w, h, ents, floors, objs)
    print('OffsetX/Y:', struct.unpack_from('<2i', b, 2072))
    lookup = struct.unpack_from('<128i', b, 2080)
    print('TerrainLookup[0..8]:', lookup[:9], 'rest all zero:', not any(lookup[9:]))
    print('PhysicsLow:', struct.unpack_from('<32h', b, 2592))
    print('PhysicsHigh:', struct.unpack_from('<32h', b, 2656))
    print('LightColors:', [hex(x) for x in struct.unpack_from('<4I', b, 2720)])
    print('MinimapPalette[0..3]:', struct.unpack_from('<4I', b, 24))
    print('header padding non-zero bytes:', sum(1 for x in b[2736:5152] if x))
    p = 5152
    refs = []
    for _ in range(objs + floors):
        refs.append((b[p:p + 32].split(b'\0')[0].decode('latin-1'), b[p + 32:p + 64].split(b'\0')[0].decode('latin-1')))
        p += 64
    print('object refs by file:', dict(collections.Counter(r[0] for r in refs[:objs])))
    print('first/last object ref:', refs[0], refs[objs - 1], 'first/last floor ref:', refs[objs], refs[-1])
    planes = []
    for k in range(3):
        start = p
        plane, p = rle(b, p, w * h)
        planes.append(plane)
        print(f'plane{k} offset {start} sha256 {hashlib.sha256(plane).hexdigest()}')
    tail = b[p:]
    print('entity table offset', p, 'bytes', len(tail), '= entities x 17:', len(tail) == ents * 17)
    print('entity table sha256', hashlib.sha256(tail).hexdigest(), 'first', tail[:17].hex(), 'last', tail[-17:].hex())
    print('physics histogram:', sorted(collections.Counter(x & 31 for x in planes[2]).items()))
    print('vision histogram:', sorted(collections.Counter(x >> 5 for x in planes[2]).items()))
    print('terrain histogram:', sorted(collections.Counter(lookup[x & 127] % 16 for x in planes[0]).items()))
    print('blocked tiles:', sum(1 for x in planes[2] if x & 31))
    for line in lio.splitlines():
        f = [x.strip().strip('"') for x in line.split(',')]
        if len(f) > 5 and f[0] == '5' and f[3].startswith(('Refinery', 'TradingPost', 'RepairDock')):
            x, y = int(f[4]), int(f[5])
            i = (y // 16) * w + x // 16
            print(f'{f[3]} ({x},{y}) terrain {lookup[planes[0][i] & 127] % 16} physics {planes[2][i] & 31}')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    main(sys.argv[1])
