#!/usr/bin/env python3
"""Independent audit for queue G7 (all 30 VEH rows) and RT-F055 (Rogue Trader Outpost data clause).

Stdlib only; does not use the C# loader. Reads RT_ORIGINAL_ASSETS_ROOT or argv[1] (a cosmic-rift-assets checkout) and
prints the values pinned by tests/Unit/OriginalVehicleCatalogTests.cs and tests/Unit/OriginalOutpostTests.cs.
"""
import hashlib
import os
import sys


def split(line):
    out, cur, quoted = [], "", False
    for c in line:
        if c == '"':
            quoted = not quoted
        elif c == "," and not quoted:
            out.append(cur.strip())
            cur = ""
        else:
            cur += c
    out.append(cur.strip())
    return out


def read(root, rel, size, sha):
    data = open(os.path.join(root, rel), "rb").read()
    assert len(data) == size, (rel, len(data))
    assert hashlib.sha256(data).hexdigest() == sha, rel
    return data.decode("latin-1")


def rows(text):
    # physical line numbers, blank lines counted but skipped (matches RawRecordReader)
    return [(i, split(l)) for i, l in enumerate(text.replace("\r\n", "\n").split("\n"), 1) if l.strip()]


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("RT_ORIGINAL_ASSETS_ROOT")
    if not root:
        sys.exit("usage: audit_rt_f055_g7.py <assets root> (or set RT_ORIGINAL_ASSETS_ROOT)")
    veh = rows(read(root, "VEH/Rogue.veh", 41075, "9eefa85a04587867616be786fffc664f3ebc0fb1d50ba6f6fd303a7a8a988572"))
    itm = rows(read(root, "ITM/Rogue_mp.itm", 111614, "dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7"))
    nws_text = read(root, "NWS/Rogue.nws", 18753, "7fff837b85d52a78b93d50b5b8721d6e567b72b5989c5a96f4212add90aaa895")
    nws = nws_text.replace("\r\n", "\n").split("\n")

    print("VEH rows (row,type,version,id,name,fieldCount):", len(veh))
    for r, f in veh:
        print(f"  {r},{f[0]},{f[1]},{f[2]},{f[3]},{len(f)}")
    from collections import Counter
    mold = [l for r, l in enumerate(read(root, "VEH/Rogue.veh", 41075, "9eefa85a04587867616be786fffc664f3ebc0fb1d50ba6f6fd303a7a8a988572").replace("\r\n", "\n").split("\n"), 1) if r == 9][0]
    print("Solar Mold: quote-aware fields", len(split(mold)), "naive comma-split pieces", len(mold.split(",")), "quoted field with comma: f5 =", split(mold)[5][:40], "...")
    print("VEH record types:", dict(Counter(f[0] for _, f in veh)))
    print("VEH field counts:", dict(Counter((f[0], len(f)) for _, f in veh)))

    byid = {f[2]: (r, f) for r, f in itm}
    print("ITM type-7 rows (row,id,name,buy f8,sell f15,f93,f34,f35,fields):")
    t7 = [(r, f) for r, f in itm if f[0] == "7"]
    vids = {f[2]: r for r, f in veh if f[0] == "5"}
    for r, f in t7:
        print(f"  {r},{f[2]},{f[3]},{f[8]},{f[15]},{f[93]},{f[34]},{f[35]},{len(f)}  veh row of f93={vids.get(f[93])}")
    print("type-7 f93 all resolve to unique type-5 VEH ids:", all(f[93] in vids for _, f in t7), len(t7))

    r169 = [f for r, f in itm if r == 169][0]
    print("ITM row 169:", r169[2], r169[3], "type", r169[0], "mass f7", r169[7], "buy f8", r169[8], "sell f15", r169[15])
    r199 = [f for r, f in itm if r == 199][0]
    print("ITM row 199:", r199[2], r199[3], "type", r199[0], "fields", len(r199), "mass f7", r199[7], "buy f8", r199[8], "sell f15", r199[15])

    r13 = [f for r, f in veh if r == 13][0]
    print("VEH row 13:", r13[2], r13[3], "f5", r13[5], "fields", len(r13))
    print("VEH row 13 blocks (f, name, f+1, f+2, f+5, f+6, f+7):")
    for k in range(8):
        f = 253 + 14 * k
        print(f"  {f},{r13[f]},{r13[f+1]},{r13[f+2]},{r13[f+5]},{r13[f+6]},{r13[f+7]}  item={byid[r13[f+1]][1][3]}({byid[r13[f+1]][1][0]}) input={byid[r13[f+5]][1][3]}")
    print("VEH rows containing 2023 beyond f3:", [(r, k) for r, f in veh for k, x in enumerate(f) if k > 3 and x == "2023"])
    print("NWS lines 106-110 and 112-120:")
    for n in list(range(106, 111)) + list(range(112, 121)):
        print(f"  {n}: {nws[n-1]!r}")
    print("NWS line count:", len(nws_text.replace('\r\n', '\n').split('\n')))


main()
