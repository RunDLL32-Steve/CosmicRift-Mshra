#!/usr/bin/env python3
"""Download the archived Rogue keys.txt and verify its pinned provenance (stdlib only).

usage: fetch_rogue_keys.py <output-folder>

keys.txt is NOT part of the pinned fowlmouth/cosmic-rift-assets clone and must NOT be committed (licence/redistribution unknown).
Source: InfantryOnline/Zone-Assets commit 401633b9b6829d9e5ae56b76a2563b1b95bbe4b9, path
"Incompatible Zone Archives/SilentClientCrash/rogue/keys.txt" (docs/original-research/SOURCE_MANIFEST.json).
The output folder must be OUTSIDE the repository; the script refuses a folder inside the repo, refuses to overwrite, and writes
the file only after length, git blob SHA-1 and SHA-256 all match. Then set RT_ROGUE_KEYS_TXT=<folder>/keys.txt for the oracle tests.
"""
import hashlib
import os
import sys
import urllib.parse
import urllib.request

COMMIT = "401633b9b6829d9e5ae56b76a2563b1b95bbe4b9"
PATH = "Incompatible Zone Archives/SilentClientCrash/rogue/keys.txt"
LENGTH = 1942
BLOB_SHA1 = "e8dc82bb2e4d11a6aa28d0b9ee68d5df3613d7ba"
SHA256 = "d004b97b69a97a2f3b4917dcf911d3a93f06d406e886a29dc5dc486a07e014f3"
URL = f"https://raw.githubusercontent.com/InfantryOnline/Zone-Assets/{COMMIT}/{urllib.parse.quote(PATH)}"


def verify(data):
    problems = []
    if len(data) != LENGTH:
        problems.append(f"length {len(data)} != {LENGTH}")
    blob = hashlib.sha1(b"blob %d\0" % len(data) + data).hexdigest()
    if blob != BLOB_SHA1:
        problems.append(f"git blob sha1 {blob} != {BLOB_SHA1}")
    sha = hashlib.sha256(data).hexdigest()
    if sha != SHA256:
        problems.append(f"sha256 {sha} != {SHA256}")
    return problems


def main(argv):
    if len(argv) != 2:
        print(__doc__)
        return 2
    out = os.path.realpath(argv[1])
    repo = os.path.realpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
    if out == repo or out.startswith(repo + os.sep):
        print(f"refusing: {out} is inside the repository; choose a folder outside it")
        return 2
    target = os.path.join(out, "keys.txt")
    if os.path.exists(target):
        print(f"refusing to overwrite {target}")
        return 2
    with urllib.request.urlopen(URL, timeout=60) as response:
        data = response.read()
    problems = verify(data)
    if problems:
        print("VERIFICATION FAILED, nothing written: " + "; ".join(problems))
        return 1
    os.makedirs(out, exist_ok=True)
    with open(target, "wb") as f:
        f.write(data)
    print(f"wrote {target} ({len(data)} bytes); length, git blob sha1 and sha256 verified")
    print(f"export RT_ROGUE_KEYS_TXT={target}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
