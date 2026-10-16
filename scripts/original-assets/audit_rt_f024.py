#!/usr/bin/env python3
"""RT-F024 independent audit (stdlib only, read-only): graphic layer tuples and sound reference tuples in the twelve
hull VEH Car rows (VEH/Rogue.veh v53, 454 fields). Prints the values pinned by tests/Unit/OriginalVehicleLayerTests.cs.
Usage: python3 -I scripts/original-assets/audit_rt_f024.py [assets-root]   (default: $RT_ORIGINAL_ASSETS_ROOT)
Prints RT_F024_AUDIT_OK on success. BLO content is Git-LFS only in the pinned clone: only BLO/<archive>.blo FILENAME
existence is checked here (case-insensitive); member existence is NOT verified."""
import csv, os, re, sys

root = sys.argv[1] if len(sys.argv) > 1 else os.environ["RT_ORIGINAL_ASSETS_ROOT"]
INT = re.compile(r"^-?\d+$")


def rows(path):
    out = []
    for i, line in enumerate(open(path, encoding="latin1").read().splitlines(), 1):
        if line.strip():
            out.append((i, [f.strip() for f in next(csv.reader([line], skipinitialspace=True))]))
    return out


rpg = rows(f"{root}/RPG/Rogue.rpg")
veh = rows(f"{root}/VEH/Rogue.veh")
hull_ids = [f[3] for _, f in rpg if f[0] == "v6" and f[2] == "1"]
assert len(hull_ids) == 12, hull_ids
hulls = []
for vid in hull_ids:
    m = [(n, f) for n, f in veh if f[0] == "2" and f[1] == "v53" and f[2] == vid]
    assert len(m) == 1, vid
    hulls.append(m[0])

FIRST_LAYER, LAYER_STRIDE, LAYER_COUNT = 382, 8, 7
FIRST_SOUND, SOUND_STRIDE, SOUND_COUNT = 438, 4, 4
assert FIRST_LAYER + LAYER_STRIDE * LAYER_COUNT == FIRST_SOUND
assert FIRST_SOUND + SOUND_STRIDE * SOUND_COUNT == 454

# 1. Consistency: the non-integer fields at f[>=374] sit at identical indexes in all twelve rows, and the whole record
#    has the same field count.
text_positions = None
for n, f in hulls:
    assert len(f) == 454, (n, len(f))
    tp = [i for i, x in enumerate(f) if i >= 374 and not INT.match(x)]
    if text_positions is None:
        text_positions = tp
    assert tp == text_positions, (n, tp)
print("TEXT_FIELD_INDEXES_F374_PLUS", text_positions)
assert text_positions == [382, 383, 390, 391, 398, 399, 406, 407, 414, 415, 422, 423, 430, 431, 438, 439, 442, 443, 446, 447, 450, 451]

# 2. Every non-integer field anywhere in the record (outside quoted/known name fields) as <archive>,<member> pairs.
#    Fields before f[374]: report positions of non-integer text so other possible pairs are visible.
early = None
for n, f in hulls:
    ep = [(i, f[i]) for i in range(len(f)) if i < 374 and not INT.match(f[i])]
    if early is None:
        early = [i for i, _ in ep]
    assert early == [i for i, _ in ep], n
print("TEXT_FIELD_INDEXES_BEFORE_F374", early)

# 3. Per-hull tuples.
archives = set()
for n, f in hulls:
    print(f"ROW {n} id={f[2]} name={f[3]}")
    for s in range(LAYER_COUNT):
        b = FIRST_LAYER + s * LAYER_STRIDE
        ints = [int(x) for x in f[b + 2:b + LAYER_STRIDE]]
        assert len(ints) == 6
        print(f"  LAYER{s} f[{b}..{b + LAYER_STRIDE - 1}] {f[b]},{f[b + 1]},{','.join(map(str, ints))}")
        if f[b] != "None":
            archives.add(f[b])
        else:
            assert f[b + 1] == "None" and not any(ints)
    for s in range(SOUND_COUNT):
        b = FIRST_SOUND + s * SOUND_STRIDE
        ints = [int(x) for x in f[b + 2:b + SOUND_STRIDE]]
        print(f"  SOUND{s} f[{b}..{b + SOUND_STRIDE - 1}] {f[b]},{f[b + 1]},{','.join(map(str, ints))}")
        if f[b] != "None":
            archives.add(f[b])
        else:
            assert f[b + 1] == "None" and not any(ints)
    # slot-0 layer is the known roll sequence; slot 2/3 match the catalog tuples
    assert f[382] in ("TerranShipsSmall", "SkithzarShipsSmall", "GraziShipsSmall")

# catalog Asp tuples
asp = next(f for _, f in hulls if f[3] == "Asp")
assert asp[398:406] == ["ExplosionEffects", "DeathExplosion03_", "0", "0", "0", "0", "0", "85"]
assert asp[406:414] == ["ExplosionEffects", "thrust3", "0", "0", "0", "0", "-10", "51"]
assert asp[442:444] == ["UtilitySounds", "Thrust"] and asp[446:448] == ["UtilitySounds", "Rev"]

# 4. BLO filename existence only (case-insensitive).
blo = {x.lower() for x in os.listdir(f"{root}/BLO")}
for a in sorted(archives):
    print(f"BLO_FILE {a}.blo present={a.lower() + '.blo' in blo}")
print("DISTINCT_ARCHIVES", sorted(archives))
print("RT_F024_AUDIT_OK")
