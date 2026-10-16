#!/usr/bin/env python3
"""Independent audit for RT-F054 (HyperCore and warp item rows). Stdlib only.

Reads ITM/Rogue_mp.itm (pinned fowlmouth/cosmic-rift-assets eb1514ea) below RT_ORIGINAL_ASSETS_ROOT or argv[1], and
prints the values pinned by tests/Unit/OriginalWarpItemTests.cs. LIO/Rogue.lio and NWS/Rogue.nws lines are printed as
evidence only (the LIO reader belongs to queue G6; no LIO values are decoded here).

Usage: python3 -I scripts/original-assets/audit_rt_f054.py [assets-root]
"""
import csv, hashlib, os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("RT_ORIGINAL_ASSETS_ROOT", "")
ITM_SHA = "dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7"


def read(rel):
    with open(os.path.join(ROOT, rel), "rb") as fh:
        return fh.read()


def rows_of(data):
    # physical line numbers (blank lines count), CRLF/LF/CR tolerant like the C# reader
    text = data.decode("latin-1").replace("\r\n", "\n").replace("\r", "\n")
    out = {}
    for n, line in enumerate(text.split("\n"), 1):
        if line.strip(" \t"):
            out[n] = [f.strip() for f in next(csv.reader([line], skipinitialspace=True))]
    return out


def is_int(s):
    try:
        int(s)
        return True
    except ValueError:
        return False


def main():
    if not ROOT:
        sys.exit("set RT_ORIGINAL_ASSETS_ROOT or pass the assets root")
    raw = read("ITM/Rogue_mp.itm")
    print("ITM sha256 ok:", hashlib.sha256(raw).hexdigest() == ITM_SHA, "length", len(raw))
    rows = rows_of(raw)
    print("rows:", len(rows))

    t17 = {n: r for n, r in rows.items() if r[0] == "17"}
    print("type-17 rows (n, id, name, category, fields):")
    for n, r in t17.items():
        print("  ", n, r[2], repr(r[3]), repr(r[4]), len(r))
    print("HyperCore row:", [(n, r[2], r[3], r[4], len(r)) for n, r in rows.items() if r[2] == "2011" and r[0] == "4"])

    print("\nCatalog row claims (row -> id, name):")
    for n in (110, 111, 112, 113, 114, 115, 116, 117, 118, 119):
        r = rows[n]
        print("  ", n, r[0], r[2], repr(r[3]), repr(r[4]), len(r))

    first = list(t17.values())
    width = {len(r) for r in first}
    print("\ntype-17 field counts:", sorted(width))
    n_int, n_text = [], []
    for i in range(113):
        vals = [r[i] for r in first]
        if len(set(vals)) > 1 and i != 2:
            (n_int if all(is_int(v) for v in vals) else n_text).append(i)
    print("differing int fields (excluding id f[2]):", n_int)
    print("differing non-int (text) fields:", n_text)
    print("f[0],f[1] identical:", len({r[0] for r in first}) == 1, len({r[1] for r in first}) == 1)

    order = [110, 111, 112, 113, 114, 116, 117, 118, 119]
    print("\nPer-row distinguishing values (row, id, name):")
    for n in order:
        r = rows[n]
        ints = [(i, int(r[i])) for i in n_int if i < len(r)]
        print(f"  row {n} id {r[2]} {r[3]!r}")
        print("    f[5] =", repr(r[5]))
        print("    ints =", ints)
        if n != 111:
            print("    f[36..51] =", [int(x) for x in r[36:52]])
            print("    text f[23,24,101,102,109,110] =", [r[i] for i in (23, 24, 101, 102, 109, 110)])
        else:
            print("    (type 4, 31 fields) f[7,8,13,15] =", [r[i] for i in (7, 8, 13, 15)])

    print("\nRows with value 2011 outside f[2]:", [(n, i) for n, r in rows.items() for i, v in enumerate(r) if v == "2011" and i != 2])
    print("Rows with f[34]=2011 and f[35]:", [(n, r[2], r[35]) for n, r in t17.items() if r[34] == "2011"])
    print("Rows with f[36] == -2:", [(n, r[2]) for n, r in rows.items() if len(r) > 36 and r[36] == "-2"])
    print("Category 'HyperDrive Systems':", [(n, r[0], r[2]) for n, r in rows.items() if r[4] == "HyperDrive Systems"])
    print("Categories BaseLimitedUse/BaseWarp:", [(n, r[0], r[2], r[3], r[4]) for n, r in rows.items() if r[4] in ("BaseLimitedUse", "BaseWarp")])
    print("VEH ids == 2011 (id namespace is per file):",
          [l.split(",")[3] for l in read("VEH/Rogue.veh").decode("latin-1").splitlines() if l.split(",")[2:3] == ["2011"]])

    print("\nLIO/Rogue.lio evidence lines (raw, NOT decoded; reader is queue G6):")
    for n, l in enumerate(read("LIO/Rogue.lio").decode("latin-1").splitlines(), 1):
        if any(k in l for k in ("HyperBuoyWarp", "RandomHyperJump", "DeepSpaceWarp")):
            print("  ", n, l)
    print("\nNWS/Rogue.nws evidence lines:")
    for n, l in enumerate(read("NWS/Rogue.nws").decode("latin-1").splitlines(), 1):
        if any(k in l for k in ("HyperCore installed", "Hyper Buoy", "Hyper Recall", "Hyper Jump Randomly")):
            print("  ", n, l.strip())


if __name__ == "__main__":
    main()
