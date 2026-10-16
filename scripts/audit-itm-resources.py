#!/usr/bin/env python3
"""Independent check of the RT-F013..F019 oracle against ITM/Rogue_mp.itm (read-only).
Usage: python scripts/audit-itm-resources.py <assets-root>   Prints ITM_RESOURCES_OK on success."""
import csv, hashlib, sys
root = sys.argv[1]
data = open(f"{root}/ITM/Rogue_mp.itm", "rb").read()
assert len(data) == 111614 and hashlib.sha256(data).hexdigest() == "dd85dea54055108ec7c2996716c430a49c815baa937d36bc2a0a9b09b68334c7", "pin mismatch"
rows = [(i + 1, next(csv.reader([l], skipinitialspace=True))) for i, l in enumerate(data.decode("latin1").splitlines()) if l.strip()]
assert len(rows) == 200 and all(f[1] == "v57" and len(f) >= 31 for _, f in rows)
ids = [f[2] for _, f in rows]; assert len(set(ids)) == 200
res = [(n, f[2], f[3], int(f[7]), int(f[8]), int(f[15])) for n, f in rows if f[0] == "4" and f[4] == "Resources"]
want = [(68,"2003","Tellerium",50000,0,0),(69,"2004","Refined Tellerium",25000,0,-1),(70,"2005","Krandazite",100000,0,0),
        (71,"2006","Refined Krandazite",50000,0,-1),(87,"2008","Wreckage",200000,0,0),(166,"2019","Iron Ore",100000,0,0),(167,"2020","Alloy",75000,0,0)]
assert res == want, res
print("ITM_RESOURCES_OK", len(res))
