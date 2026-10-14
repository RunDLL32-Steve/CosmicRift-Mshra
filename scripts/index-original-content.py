#!/usr/bin/env python3
"""Index canonical Rogue Trader text content without altering or interpreting it.

Only Python's standard library is used. Raw records and field lexemes are retained;
no original numeric value is converted to modern gameplay units. BLO references
are candidates matched by filename/stem, not decoded archive-member dependencies.
"""
from __future__ import annotations

import argparse
import collections
import csv
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import tempfile
from typing import Any

EXPECTED_COMMIT = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35"
INPUT_PATHS = (
    "CFG/rogue.cfg", "VEH/Rogue.veh", "ITM/Rogue_mp.itm",
    "RPG/Rogue.rpg", "LIO/Rogue.lio",
)
REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SOURCE = REPOSITORY_ROOT / ".tools/references/cosmic-rift-assets"
DEFAULT_MANIFEST = REPOSITORY_ROOT / "docs/original-research/ASSET_MANIFEST.json"
DEFAULT_OUTPUT = REPOSITORY_ROOT / "artifacts/research/original-content/index.json"
FILE_EXTENSION = re.compile(r"\.(?:blo|bmp|cfg|itm|lio|lvl|rpg|veh)$", re.IGNORECASE)
LFS_HEADER = b"version https://git-lfs.github.com/spec/v1"


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def decode_text(data: bytes) -> tuple[str, str]:
    """Choose a lossless compatible text decoding; the byte hash is never normalized."""
    if data.startswith(LFS_HEADER):
        raise ValueError("A canonical text input is still a Git LFS pointer, not payload bytes.")
    if data.startswith(b"\xef\xbb\xbf"):
        return data.decode("utf-8-sig"), "utf-8-sig"
    try:
        return data.decode("utf-8"), "utf-8"
    except UnicodeDecodeError:
        return data.decode("cp1252"), "cp1252"


def safe_source_path(source_root: Path, relative: str) -> Path:
    path = PurePosixPath(relative)
    if path.is_absolute() or ".." in path.parts or "\\" in relative:
        raise ValueError(f"Unsafe manifest/source path: {relative!r}")
    result = (source_root / Path(*path.parts)).resolve()
    if not result.is_relative_to(source_root):
        raise ValueError(f"Source path resolves outside the original folder: {relative}")
    return result


def validate_output_path(source_root: Path, output: Path, manifest: Path) -> Path:
    resolved = output.resolve()
    if resolved == source_root or resolved.is_relative_to(source_root):
        raise ValueError("Output must remain outside the original source folder.")
    if resolved == manifest.resolve():
        raise ValueError("Output cannot overwrite the asset manifest.")
    if resolved.suffix.casefold() != ".json":
        raise ValueError("The generated index output must be a .json file.")
    return resolved


def verify_revision(source_root: Path, manifest: dict[str, Any]) -> dict[str, Any]:
    canonical = manifest.get("canonicalSource", {})
    if canonical.get("commit") != EXPECTED_COMMIT:
        raise ValueError(f"Manifest is not pinned to canonical commit {EXPECTED_COMMIT}.")
    result: dict[str, Any] = {
        "expectedCommit": EXPECTED_COMMIT,
        "manifestCommitVerified": True,
        "checkoutCommit": None,
        "checkoutCommitVerified": False,
    }
    if (source_root / ".git").exists():
        git = shutil.which("git")
        if git is None:
            raise ValueError("A Git checkout was provided but git is unavailable for revision validation.")
        command = subprocess.run(
            [git, "--no-optional-locks", "-C", str(source_root), "rev-parse", "HEAD"],
            check=True, capture_output=True, text=True, timeout=10,
        )
        commit = command.stdout.strip()
        if commit != EXPECTED_COMMIT:
            raise ValueError(f"Source checkout is at {commit}, expected {EXPECTED_COMMIT}.")
        result.update(checkoutCommit=commit, checkoutCommitVerified=True)
    else:
        result["note"] = "No Git metadata; exact manifest byte hashes validate the five text inputs."
    return result


def raw_csv_tokens(raw_record: str) -> list[str]:
    """Retain original whitespace and CSV quotes while locating field boundaries."""
    text = raw_record.rstrip("\r\n")
    tokens: list[str] = []
    start = 0
    quoted = False
    index = 0
    while index < len(text):
        char = text[index]
        if char == '"':
            if quoted and index + 1 < len(text) and text[index + 1] == '"':
                index += 2
                continue
            if quoted or not text[start:index].strip():
                quoted = not quoted
        elif char == "," and not quoted:
            tokens.append(text[start:index])
            start = index + 1
        index += 1
    if quoted:
        raise ValueError("Unterminated quoted CSV field.")
    tokens.append(text[start:])
    return tokens


def parse_csv_records(relative: str, text: str) -> list[dict[str, Any]]:
    lines = text.splitlines(keepends=True)
    reader = csv.reader(io.StringIO(text, newline=""), skipinitialspace=True, strict=True)
    previous_end = 0
    records: list[dict[str, Any]] = []
    for values in reader:
        start, end = previous_end + 1, reader.line_num
        raw_record = "".join(lines[previous_end:end])
        previous_end = end
        if not values:
            continue
        raw_fields = raw_csv_tokens(raw_record)
        if len(raw_fields) != len(values):
            raise ValueError(f"{relative}:{start}: raw and parsed field boundaries disagree.")
        if relative.startswith("RPG/"):
            if len(values) < 40:
                raise ValueError(f"{relative}:{start}: incomplete observed RPG header/name layout.")
            version, identifier, record_type, name = values[0], values[1], values[2], values[39]
            header_columns = {"version": 0, "identifier": 1, "recordType": 2, "name": 39}
            schema = "RPG v6 header/name positions observed; field semantics unvalidated"
        else:
            if len(values) < 4:
                raise ValueError(f"{relative}:{start}: incomplete type/version/id/name header.")
            record_type, version, identifier, name = values[:4]
            header_columns = {"recordType": 0, "version": 1, "identifier": 2, "name": 3}
            schema = "type/version/id/name positions observed; field semantics unvalidated"
        if not version.startswith("v") or not version[1:].isdigit():
            raise ValueError(f"{relative}:{start}: unrecognized version token {version!r}.")
        records.append({
            "recordKey": f"{relative}:{start}", "sourcePath": relative,
            "row": start, "endRow": end, "recordType": record_type,
            "version": version, "identifier": identifier, "name": name,
            "headerColumns": header_columns, "schemaStatus": schema,
            "fieldCount": len(values), "fields": values,
            "rawFieldTokens": raw_fields, "rawRecordText": raw_record,
        })
    return records


def parse_config(text: str) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    section = ""
    records: list[dict[str, Any]] = []
    structure: list[dict[str, Any]] = []
    for row, raw_line in enumerate(text.splitlines(keepends=True), 1):
        line = raw_line.rstrip("\r\n")
        stripped = line.strip()
        if stripped.startswith("[") and stripped.endswith("]"):
            section = stripped[1:-1]
            structure.append({"row": row, "kind": "section", "name": section, "rawText": raw_line})
        elif not stripped or stripped.startswith((";", "#", "//")):
            structure.append({"row": row, "kind": "blank" if not stripped else "comment", "rawText": raw_line})
        elif "=" in line:
            key_raw, value_raw = line.split("=", 1)
            records.append({
                "recordKey": f"CFG/rogue.cfg:{row}", "sourcePath": "CFG/rogue.cfg", "row": row,
                "endRow": row, "recordType": "configEntry", "version": None,
                "identifier": key_raw.strip(), "name": key_raw.strip(), "section": section,
                "fieldCount": 2, "fields": [key_raw, value_raw],
                "rawFieldTokens": [key_raw, value_raw], "rawRecordText": raw_line,
                "schemaStatus": "INI section/key/value preserved; evaluator/units/sentinels unvalidated",
            })
        else:
            structure.append({"row": row, "kind": "unparsed", "rawText": raw_line})
    return records, structure


def manifest_lookup(manifest: dict[str, Any]) -> tuple[dict[str, dict[str, Any]], dict[str, list[str]]]:
    by_path: dict[str, dict[str, Any]] = {}
    by_token: dict[str, list[str]] = collections.defaultdict(list)
    for asset in manifest["assets"]:
        relative = asset["originalPath"]
        if relative in by_path:
            raise ValueError(f"Duplicate manifest path: {relative}")
        by_path[relative] = asset
        name = PurePosixPath(relative).name
        by_token[name.casefold()].append(relative)
        if name.casefold().endswith(".blo"):
            by_token[PurePosixPath(name).stem.casefold()].append(relative)
    return by_path, dict(by_token)


def candidate_references(records: list[dict[str, Any]], source_root: Path,
                         assets: dict[str, dict[str, Any]], tokens: dict[str, list[str]]) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    dependencies: list[dict[str, Any]] = []
    unresolved: list[dict[str, Any]] = []
    local_blobs = {
        path.stem.casefold(): path.relative_to(source_root).as_posix()
        for path in (source_root / "BLO").glob("*.blo")
    }
    for record in records:
        source = record["sourcePath"]
        first_field = 1 if source.startswith("CFG/") else 43 if source.startswith("RPG/") else 7 if source.startswith("ITM/") else 6 if source.startswith("VEH/") else 4
        reference_ids: list[int] = []
        for column, raw_value in enumerate(record["fields"]):
            if column < first_field:
                continue
            token = raw_value.strip()
            normalized = token.replace("\\", "/")
            basename = PurePosixPath(normalized).name
            matched = tokens.get(basename.casefold(), [])
            is_filename = bool(FILE_EXTENSION.search(basename))
            common = {
                "sourceRecordKey": record["recordKey"], "sourcePath": source,
                "row": record["row"], "fieldIndex": column,
                "rawFieldToken": record["rawFieldTokens"][column], "token": token,
            }
            if matched:
                reference_ids.append(len(dependencies))
                candidates = [{
                    "originalPath": target, "manifestSha256": assets[target]["originalSha256"],
                    "manifestSizeBytes": assets[target]["originalSizeBytes"],
                } for target in matched]
                dependencies.append({
                    **common, "candidatePaths": candidates,
                    "matchMethod": "case_insensitive_filename" if is_filename else "case_insensitive_exact_blob_stem",
                    "status": "CANDIDATE: archive member and record-field roles not validated",
                    "candidateNextField": record["fields"][column + 1] if column + 1 < record["fieldCount"] else None,
                    "memberResolved": False,
                })
            elif is_filename or basename.casefold() in local_blobs:
                unresolved.append({
                    **common, "reason": "Explicit source filename absent from manifest" if is_filename else "Local BLO stem absent from manifest",
                    "localCandidatePath": local_blobs.get(basename.casefold()),
                })
        record["candidateDependencyIndexes"] = reference_ids
    return dependencies, unresolved


def summarize_record(record: dict[str, Any]) -> dict[str, Any]:
    return {key: record[key] for key in (
        "recordKey", "sourcePath", "row", "recordType", "version", "identifier", "name", "candidateDependencyIndexes")}


def catalogs(records: list[dict[str, Any]]) -> dict[str, Any]:
    vehicles = {record["identifier"]: record for record in records if record["sourcePath"] == "VEH/Rogue.veh"}
    ships: list[dict[str, Any]] = []
    unmatched_hulls: list[dict[str, Any]] = []
    for hull in (record for record in records if record["sourcePath"] == "RPG/Rogue.rpg"):
        candidate_vehicle_id = hull["fields"][3]
        vehicle = vehicles.get(candidate_vehicle_id)
        entry = {
            **summarize_record(hull), "candidateVehicleId": candidate_vehicle_id,
            "candidateVehicleIdFieldIndex": 3, "vehicleRecordKey": vehicle["recordKey"] if vehicle else None,
            "vehicleName": vehicle["name"] if vehicle else None,
            "namesAgree": bool(vehicle and vehicle["name"] == hull["name"]),
            "status": "Header/name cross-match; class evaluator, eligibility and handling remain unvalidated",
        }
        ships.append(entry)
        if not vehicle or vehicle["name"] != hull["name"]:
            unmatched_hulls.append(entry)
    items = [record for record in records if record["sourcePath"] == "ITM/Rogue_mp.itm"]
    return {
        "ships": ships,
        "unmatchedHullCandidates": unmatched_hulls,
        "weaponAndEffectCandidates": [summarize_record(record) for record in items if record["recordType"] in {"6", "8"}],
        "resourceRecords": [summarize_record(record) for record in items if record["recordType"] == "4" and record["fields"][4] == "Resources"],
        "resourceCategoryRecords": [summarize_record(record) for record in items if record["fields"][4] == "Resources"],
        "itemTypes": {
            item_type: [summarize_record(record) for record in items if record["recordType"] == item_type]
            for item_type in sorted({record["recordType"] for record in items}, key=int)
        },
        "classificationNotes": [
            "Weapon/effect candidates retain ITM type 6/8; debris, child projectiles, NPC effects and unavailable items are included.",
            "Resource candidates retain ITM type 4 and the literal Resources category. Other Resources-category records include credits and repairs; no rarity, price, weight or mining conversion is inferred.",
            "Ship names come from RPG name field 39 and a candidate field-3 vehicle ID/name cross-match, not the historical overview's ship-count claim.",
        ],
    }


def build_index(source_root: Path, manifest: dict[str, Any], manifest_bytes: bytes) -> dict[str, Any]:
    revision = verify_revision(source_root, manifest)
    assets, tokens = manifest_lookup(manifest)
    source_hashes: dict[str, str] = {}
    files: list[dict[str, Any]] = []
    records: list[dict[str, Any]] = []
    config_structure: list[dict[str, Any]] = []
    for relative in INPUT_PATHS:
        path = safe_source_path(source_root, relative)
        asset = assets.get(relative)
        if asset is None or asset.get("commit") != EXPECTED_COMMIT:
            raise ValueError(f"Missing canonical manifest entry: {relative}")
        raw = path.read_bytes()
        digest = sha256(raw)
        if digest != asset["originalSha256"] or len(raw) != asset["originalSizeBytes"]:
            raise ValueError(f"Exact original-byte hash/size validation failed for {relative}.")
        source_hashes[relative] = digest
        text, encoding = decode_text(raw)
        if relative.endswith(".cfg"):
            parsed, config_structure = parse_config(text)
        else:
            parsed = parse_csv_records(relative, text)
        records.extend(parsed)
        files.append({
            "originalPath": relative, "sha256": digest, "sizeBytes": len(raw),
            "encoding": encoding, "hashVerified": True, "recordCount": len(parsed),
            "recordTypes": dict(sorted(collections.Counter(record["recordType"] for record in parsed).items())),
            "versions": dict(collections.Counter(record["version"] or "not_declared" for record in parsed)),
            "fieldCounts": dict(sorted(collections.Counter(str(record["fieldCount"]) for record in parsed).items(), key=lambda pair: int(pair[0]))),
        })
    dependencies, unresolved = candidate_references(records, source_root, assets, tokens)
    catalog = catalogs(records)
    for relative, before in source_hashes.items():
        if sha256(safe_source_path(source_root, relative).read_bytes()) != before:
            raise ValueError(f"Original source changed during indexing: {relative}")
    return {
        "schemaVersion": 1, "canonicalSource": manifest["canonicalSource"],
        "revisionValidation": revision, "manifestSha256": sha256(manifest_bytes),
        "policy": {"readOnlyOriginals": True, "sourceHashesUnchangedAfterIndexing": True,
                   "numericFieldsRemainStrings": True, "unitConversions": None,
                   "fieldIndexesAreZeroBased": True, "sourceRowsAreOneBased": True},
        "interpretationStatus": "Raw content inventory and candidate references; gameplay schemas, formulas, units, sentinel values, archive members and runtime eligibility remain unvalidated.",
        "summary": {"recordCount": len(records), "fileRecordCounts": {file["originalPath"]: file["recordCount"] for file in files},
                    "ships": len(catalog["ships"]), "weaponAndEffectCandidates": len(catalog["weaponAndEffectCandidates"]),
                    "resourceRecords": len(catalog["resourceRecords"]), "candidateDependencies": len(dependencies),
                    "unresolvedReferences": len(unresolved),
                    "unresolvedArchiveMemberCandidates": sum(any(candidate["originalPath"].casefold().endswith(".blo") for candidate in dependency["candidatePaths"]) for dependency in dependencies),
                    "unmatchedHullCandidates": len(catalog["unmatchedHullCandidates"])},
        "files": files, "configStructure": config_structure, "records": records,
        "catalogs": catalog, "candidateDependencies": dependencies, "unresolvedReferences": unresolved,
    }


def write_index(output: Path, index: dict[str, Any]) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary: Path | None = None
    try:
        with tempfile.NamedTemporaryFile("w", encoding="utf-8", newline="\n", dir=output.parent,
                                         prefix=f".{output.name}.", suffix=".tmp", delete=False) as stream:
            temporary = Path(stream.name)
            json.dump(index, stream, ensure_ascii=False, indent=2)
            stream.write("\n")
        os.replace(temporary, output)
        temporary = None
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-root", type=Path, default=DEFAULT_SOURCE)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--out", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--validate-only", action="store_true", help="Read, verify and index in memory without writing output.")
    args = parser.parse_args()
    try:
        source_root = args.source_root.resolve(strict=True)
        output = validate_output_path(source_root, args.out, args.manifest)
        manifest_bytes = args.manifest.read_bytes()
        manifest = json.loads(manifest_bytes.decode("utf-8-sig"))
        index = build_index(source_root, manifest, manifest_bytes)
        if not args.validate_only:
            write_index(output, index)
        print(json.dumps({"status": "validated" if args.validate_only else "indexed", "output": None if args.validate_only else str(output), **index["summary"]}, indent=2))
        print("Original ship names: " + ", ".join(ship["name"] for ship in index["catalogs"]["ships"]))
        print("Original Resources records: " + ", ".join(item["name"] for item in index["catalogs"]["resourceRecords"]))
        return 0
    except (OSError, ValueError, KeyError, csv.Error, subprocess.SubprocessError) as exception:
        print(f"INDEX_FAILED: {exception}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())


