#!/usr/bin/env python3
"""Independent audit for RT-F026 (stdlib only): prints the facts pinned by tests/Unit/OriginalKeyListingTests.cs.

usage: audit_rt_f026.py <path-to-keys.txt> [assets-root]   (assets-root defaults to RT_ORIGINAL_ASSETS_ROOT)

keys.txt is not in the fowlmouth/cosmic-rift-assets clone; fetch it with fetch_rogue_keys.py into a folder outside the repo.
Substitution tokens (~~<n>key) are placeholders, NOT key codes; this script never maps them to keys. Nothing is written.
"""
import hashlib
import os
import re
import sys

PIN_LEN, PIN_BLOB, PIN_SHA = 1942, "e8dc82bb2e4d11a6aa28d0b9ee68d5df3613d7ba", "d004b97b69a97a2f3b4917dcf911d3a93f06d406e886a29dc5dc486a07e014f3"
NWS_LEN, NWS_SHA = 18753, "7fff837b85d52a78b93d50b5b8721d6e567b72b5989c5a96f4212add90aaa895"
CFG_LEN, CFG_SHA = 48399, "82a9e40ef4fbe5787ae34953b93e02236f5a9d927ef0f09e0c3699be9cddbf90"


def main(argv):
    data = open(argv[1], "rb").read()
    blob = hashlib.sha1(b"blob %d\0" % len(data) + data).hexdigest()
    sha = hashlib.sha256(data).hexdigest()
    print(f"keys.txt length={len(data)} blob={blob} sha256={sha}")
    ok = len(data) == PIN_LEN and blob == PIN_BLOB and sha == PIN_SHA
    print("keys.txt provenance:", "MATCH" if ok else "MISMATCH")
    if not ok:
        return 1
    text = data.decode("latin-1")
    lines = text.split("\n")
    if lines and lines[-1] == "":
        lines.pop()
    print(f"lines={len(lines)} crlf={chr(13) in text}")
    strip = lambda s: re.sub(r"~[0-9A-Za-z]", "", s)
    section = -1
    nsec = nact = 0
    for no, raw in enumerate(lines, 1):
        raw = raw.rstrip("\r")
        if not raw.strip():
            continue
        if raw.startswith("~B~4"):
            section += 1
            nsec += 1
            print(f"SECTION {section} line {no}: {strip(raw)!r}  raw={raw!r}")
            continue
        label, rest = raw[1:].split("=", 1)
        toks = list(re.finditer(r"~~(\d+)key", rest))
        seps = [rest[toks[i].end():toks[i + 1].start()] for i in range(len(toks) - 1)]
        tail = rest[toks[-1].end():].strip()
        nact += 1
        note = strip(tail).strip() if tail else None
        print(f"ACTION s{section} line {no}: {strip(label).strip()!r} tokens={[int(t.group(1)) for t in toks]} "
              f"seps={seps} note={note!r}")
    print(f"sections={nsec} actions={nact}")

    root = argv[2] if len(argv) > 2 else os.environ.get("RT_ORIGINAL_ASSETS_ROOT")
    if root:
        for rel, ln, h, line in (("CFG/rogue.cfg", 41, CFG_SHA, CFG_LEN), ("NWS/Rogue.nws", 108, NWS_SHA, NWS_LEN)):
            b = open(os.path.join(root, rel), "rb").read()
            assert len(b) == line and hashlib.sha256(b).hexdigest() == h, rel
            print(f"{rel} line {ln}: {b.decode('latin-1').split(chr(10))[ln - 1].rstrip(chr(13))!r}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
