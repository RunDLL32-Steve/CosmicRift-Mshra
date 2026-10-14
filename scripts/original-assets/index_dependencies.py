#!/usr/bin/env python3
"""Directory-only canonical BLO inventory and explicitly provisional reference matching.

Standard library only. No pixel decoder, native engine, balance interpretation or
original mutation. BLO member IDs are original zero-based directory ordinals.
"""
from __future__ import annotations
import argparse
import csv
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import struct
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[2]
COMMIT = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35"

def sha(data):
    return hashlib.sha256(data).hexdigest()

def verify_bytes(path, data, expected_hash, expected_size):
    if len(data) != expected_size or sha(data) != expected_hash:
        raise ValueError(f"Canonical bytes differ/unhydrated LFS: {path}")

def config_components(value):
    return next(csv.reader([value], skipinitialspace=True, strict=True))

def git(source, *args):
    return subprocess.run(["git", "--no-optional-locks", "-C", str(source), *args],
                          check=True, capture_output=True, timeout=30).stdout

def directory(data):
    if len(data) < 8:
        raise ValueError("Truncated BLO header")
    version, count = struct.unpack_from("<II", data)
    if version not in (1, 2):
        raise ValueError(f"Unsupported BLO version {version}")
    name_size = 14 if version == 1 else 32
    end = 8 + count * (name_size + 8)
    if end > len(data):
        raise ValueError("Truncated BLO directory")
    members, names, spans = [], set(), []
    for ordinal in range(count):
        pos = 8 + ordinal * (name_size + 8)
        raw_name = data[pos:pos + name_size]
        name = raw_name.split(b"\0", 1)[0].decode("cp1252")
        if not name or name in names or name in (".", "..") or any(c in name for c in '\\/:*?"<>|'):
            raise ValueError(f"Unsafe/duplicate BLO member {name!r}")
        offset, size = struct.unpack_from("<II", data, pos + name_size)
        if offset < end or offset > len(data) or size > len(data) - offset:
            raise ValueError(f"Invalid BLO span for {name}")
        names.add(name)
        spans.append((offset, offset + size))
        members.append(dict(objectId=ordinal, objectName=name, rawNameHex=raw_name.hex(),
                            dataOffset=offset, sizeBytes=size, sha256=sha(data[offset:offset + size])))
    spans.sort()
    if any(a[1] > b[0] for a, b in zip(spans, spans[1:])):
        raise ValueError("Overlapping BLO payloads")
    return version, members

def guarded_output(source, output):
    result = output.resolve()
    if result == source or result.is_relative_to(source):
        raise ValueError("Derived output must remain outside originals")
    if result.suffix.lower() != ".json":
        raise ValueError("Output must be JSON")
    return result

def resolve_members(archives, token):
    matches = []
    for archive in archives:
        for member in archive["members"]:
            exact = member["objectName"].casefold() == token.casefold()
            stem = PurePosixPath(member["objectName"]).stem.casefold() == token.casefold()
            if exact or stem:
                matches.append({"originalPath": archive["originalPath"], **member,
                                "exactCase": member["objectName"] == token,
                                "matchMethod": "case_insensitive_exact_name" if exact else "case_insensitive_exact_stem"})
    if not matches:
        return "missing_member", matches
    return ("unique_member_candidate" if len(matches) == 1 else "ambiguous_member_candidate"), matches

def build(source, manifest_path, raw_path):
    manifest_bytes, raw_bytes = manifest_path.read_bytes(), raw_path.read_bytes()
    manifest, raw = json.loads(manifest_bytes), json.loads(raw_bytes)
    if manifest["canonicalSource"]["commit"] != COMMIT or git(source, "rev-parse", "HEAD").decode().strip() != COMMIT:
        raise ValueError("Canonical revision mismatch")
    if git(source, "ls-files", "--others", "--exclude-standard").strip():
        raise ValueError("Original checkout has untracked files")
    git(source, "diff", "--quiet", "--no-ext-diff", "HEAD", "--")
    if raw["canonicalSource"]["commit"] != COMMIT:
        raise ValueError("Raw inventory canonical revision mismatch")
    tree = git(source, "ls-tree", "-r", "--name-only", COMMIT).decode().splitlines()
    snapshots = {}
    def verified(path, expected_hash, expected_size):
        file = (source / path).resolve()
        if not file.is_relative_to(source):
            raise ValueError("Source path escaped originals")
        data = file.read_bytes()
        verify_bytes(path, data, expected_hash, expected_size)
        snapshots[path] = dict(sha256=expected_hash, sizeBytes=expected_size)
        return data
    for asset in manifest["assets"]:
        verified(asset["originalPath"], asset["originalSha256"], asset["originalSizeBytes"])
    for file in raw["files"]:
        verified(file["originalPath"], file["sha256"], file["sizeBytes"])
    archives = []
    for path in tree:
        if not path.lower().endswith(".blo"):
            continue
        pointer = git(source, "show", f"{COMMIT}:{path}")
        match = re.fullmatch(rb"version https://git-lfs.github.com/spec/v1\noid sha256:([0-9a-f]{64})\nsize (\d+)\n", pointer)
        if match:
            expected_hash, expected_size = match[1].decode(), int(match[2])
        else:
            expected_hash, expected_size = sha(pointer), len(pointer)
        data = verified(path, expected_hash, expected_size)
        version, members = directory(data)
        archives.append(dict(originalPath=path, sha256=expected_hash, sizeBytes=expected_size,
                             bloVersion=version, members=members))
    by_token = defaultdict(list)
    for path in tree:
        by_token[PurePosixPath(path).name.casefold()].append(path)
        if path.lower().endswith(".blo"):
            by_token[PurePosixPath(path).stem.casefold()].append(path)
    by_path = {a["originalPath"]: a for a in archives}
    dependencies = []
    baseline = {(d["sourceRecordKey"], d["fieldIndex"]) for d in raw["candidateDependencies"]}
    extension = re.compile(r"\.(blo|bmp|cfg|itm|lio|lvl|rpg|veh|nws|hlp|wav|cfs|fnt|txt)$", re.I)
    for record in raw["records"]:
        start = 1 if record["sourcePath"].startswith("CFG/") else 43 if record["sourcePath"].startswith("RPG/") else 7 if record["sourcePath"].startswith("ITM/") else 6 if record["sourcePath"].startswith("VEH/") else 4
        if record["sourcePath"].startswith("CFG/"):
            components = config_components(record["fields"][1])
            fields = [(1, i, value, components[i + 1] if i + 1 < len(components) else None)
                      for i, value in enumerate(components)]
        else:
            fields = [(column, None, record["fields"][column], record["fields"][column + 1]
                       if column + 1 < len(record["fields"]) else None)
                      for column in range(start, len(record["fields"]))]
        for column, component, value, following in fields:
            token = value.strip()
            name = PurePosixPath(token.replace("\\", "/")).name
            paths = by_token.get(name.casefold(), [])
            if not paths and not extension.search(name):
                continue
            dep = dict(sourceRecordKey=record["recordKey"], sourcePath=record["sourcePath"],
                       row=record["row"], recordVersion=record["version"], recordIdentifier=record["identifier"],
                       recordName=record["name"], fieldIndex=column, rawFieldToken=record["rawFieldTokens"][column],
                       configSection=record.get("section"), configValueComponentIndex=component,
                       token=token, candidatePaths=paths, presentInInitialInventory=(record["recordKey"], column) in baseline,
                       semantics="candidate field roles; runtime eligibility/version semantics unvalidated")
            blobs = [by_path[p] for p in paths if p in by_path]
            if not paths:
                dep.update(status="missing_filename", memberCandidates=[])
            elif blobs:
                adjacent = following.strip() if following is not None else None
                dep["candidateNextField"] = adjacent
                if adjacent is None or adjacent == "":
                    dep.update(status="member_role_unknown", memberCandidates=[])
                else:
                    status, matches = resolve_members(blobs, adjacent)
                    dep.update(status=status, memberCandidates=matches)
                    # Numeric next fields are not silently reinterpreted as ordinals.
                    if not matches and re.fullmatch(r"\d+", adjacent):
                        dep["status"] = "numeric_member_role_unknown"
                        dep["possibleOrdinalCandidates"] = [
                            {"originalPath": a["originalPath"], **a["members"][int(adjacent)]}
                            for a in blobs if int(adjacent) < len(a["members"])]
            else:
                dep.update(status="unique_filename_candidate" if len(paths) == 1 else "ambiguous_filename_candidate", memberCandidates=[])
            dependencies.append(dep)
    # Explicit record identities provide an inventory for future schema-specific links.
    ids = defaultdict(list)
    for r in raw["records"]:
        if not r["sourcePath"].startswith("CFG/"):
            ids[f'{r["sourcePath"]}::{r["identifier"]}'].append({
                k: r[k] for k in ("recordKey", "sourcePath", "version", "identifier", "name")})
    for path, snapshot in snapshots.items():
        if sha((source / path).read_bytes()) != snapshot["sha256"]:
            raise ValueError(f"Original changed during inventory: {path}")
    git(source, "diff", "--quiet", "--no-ext-diff", "HEAD", "--")
    counts = dict(sorted(Counter(d["status"] for d in dependencies).items()))
    unresolved = [d for d in dependencies if d["status"] not in ("unique_member_candidate", "unique_filename_candidate")]
    return dict(schemaVersion=1, canonicalSource=manifest["canonicalSource"], rawInventorySha256=sha(raw_bytes),
                manifestSha256=sha(manifest_bytes), sourceIntegrityBeforeAfter=snapshots,
                summary=dict(archiveCount=len(archives), memberCount=sum(len(a["members"]) for a in archives),
                             initialBlobCandidateCount=raw["summary"]["unresolvedArchiveMemberCandidates"],
                             candidateCount=len(dependencies), newCandidateCount=sum(not d["presentInInitialInventory"] for d in dependencies),
                             statuses=counts, unresolvedOrAmbiguousCandidateCount=len(unresolved),
                             initialCandidateStatuses=dict(sorted(Counter(d["status"] for d in dependencies if d["presentInInitialInventory"]).items())),
                             runtimeClosureVerified=False),
                limitations=["All field links remain candidates until version-specific schema validation.",
                             "Ordinal IDs are preserved; numeric neighboring fields are not assumed member references.",
                             "Missing exact member names are not substituted by similarly named members.",
                             "No pixel decoding, native evaluator, audio playback or gameplay simulation is performed."],
                archives=archives, dependencies=dependencies, unresolved=unresolved, recordIdentities=dict(ids))

class FixtureTests(unittest.TestCase):
    def fixture(self, name="A", offset=48, size=2, version=2):
        return struct.pack("<II", version, 1) + name.encode().ljust(32, b"\0") + struct.pack("<II", offset, size) + b"AB"
    def test_directory_identity(self):
        version, members = directory(self.fixture())
        self.assertEqual(version, 2)
        self.assertEqual(members[0]["sha256"], sha(b"AB"))
        self.assertEqual(members[0]["objectId"], 0)
    def test_invalid_directory(self):
        for data in (b"", self.fixture(version=3), self.fixture(offset=8), self.fixture(size=3), self.fixture(name="../A"), self.fixture()[:30]):
            with self.subTest(data=data), self.assertRaises(ValueError):
                directory(data)
    def test_duplicate_and_overlap(self):
        row = lambda name, offset: name.encode().ljust(32, b"\0") + struct.pack("<II", offset, 2)
        for rows in (row("A", 88) + row("A", 90), row("A", 88) + row("B", 89)):
            with self.assertRaises(ValueError):
                directory(struct.pack("<II", 2, 2) + rows + b"ABCD")
    def test_v1_directory(self):
        data = struct.pack("<II", 1, 1) + b"A.cfs".ljust(14, b"\0") + struct.pack("<II", 30, 2) + b"AB"
        self.assertEqual(directory(data)[1][0]["objectName"], "A.cfs")
    def test_canonical_integrity_rejects_same_size_mutation(self):
        verify_bytes("A", b"AB", sha(b"AB"), 2)
        for data in (b"AC", b"A"):
            with self.assertRaises(ValueError):
                verify_bytes("A", data, sha(b"AB"), 2)
    def test_cfg_csv_components_and_raw_values(self):
        self.assertEqual(config_components('"Keystroke Listing","keys.txt"'), ["Keystroke Listing", "keys.txt"])
        self.assertEqual(config_components("cruiart,logo"), ["cruiart", "logo"])
    def test_name_ambiguity_and_no_fuzzy_substitution(self):
        archives = [{"originalPath": "A.blo", "members": [{"objectName": "Ship", "objectId": 0}, {"objectName": "ship", "objectId": 1}]}]
        self.assertEqual(resolve_members(archives, "SHIP")[0], "ambiguous_member_candidate")
        self.assertEqual(resolve_members(archives, "Ship1")[0], "missing_member")
        archive = [{"originalPath": "A.blo", "members": [{"objectName": "Ship.cfs", "objectId": 7}]}]
        self.assertEqual(resolve_members(archive, "Ship")[1][0]["objectName"], "Ship.cfs")
        self.assertEqual(resolve_members(archive, "Ship")[1][0]["objectId"], 7)
        self.assertEqual(resolve_members(archive, "7")[0], "missing_member")
    def test_output_guard(self):
        source = (ROOT / ".tools/references/cosmic-rift-assets").resolve()
        with self.assertRaises(ValueError):
            guarded_output(source, source / "inventory.json")

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-root", type=Path, default=ROOT / ".tools/references/cosmic-rift-assets")
    parser.add_argument("--manifest", type=Path, default=ROOT / "docs/original-research/ASSET_MANIFEST.json")
    parser.add_argument("--raw-index", type=Path, default=ROOT / "artifacts/research/original-content/index.json")
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts/original-dependencies/index.json")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(FixtureTests))
        return 0 if result.wasSuccessful() else 1
    source = args.source_root.resolve()
    output = guarded_output(source, args.output)
    if output in (args.manifest.resolve(), args.raw_index.resolve(), Path(__file__).resolve()):
        raise ValueError("Output cannot replace inputs")
    report = build(source, args.manifest, args.raw_index)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps(report["summary"], indent=2))
    print(f"DEPENDENCY_INVENTORY_PASS: {output}")
    return 0

if __name__ == "__main__":
    try:
        sys.exit(main())
    except (ValueError, OSError, subprocess.SubprocessError, KeyError, csv.Error) as exc:
        print(f"DEPENDENCY_INVENTORY_FAIL: {exc}", file=sys.stderr)
        sys.exit(1)
