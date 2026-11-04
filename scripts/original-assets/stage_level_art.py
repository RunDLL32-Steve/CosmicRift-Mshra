#!/usr/bin/env python3
"""Stage the original Rogue map art (asteroids, stations, walls, gas clouds) and mineral prize sprites.

Reads the pinned reference checkout only: LVL/Rogue.lvl, the BLO archives its object table names, and
ITM/Rogue_mp.itm for the mineral prize graphics. Every original is checked against its pinned SHA-256
(LVL/ITM from the canonical manifest, BLOs from shared/RogueTrader.OriginalData/PinnedBloArchives.cs)
before it is read, and again afterwards. Output goes to client/OriginalContent/level/ (git-ignored):
indexed PNG atlases with original palettes, plus level.json, which the client draws from.

LVL entity reading (RECONSTRUCTED, not native evidence): each 17-byte record starts with int16 X and
int16 Y in map pixels (the sprite cell's top-left corner, the same frame as the 16-pixel tile grid),
then uint16 whose low 13 bits index the object table and whose top 3 bits pick the starting frame
(only the safe-zone markings use them: their 5 frames are a corner, edges and a dotted fill, not an
animation). A test render of Rogue.lvl shows the station trusses joining up and every rock sitting
exactly on its collision tiles with this reading. uint16 at byte 6 is read as the entity's animation
cycle in hundredths of a second; it is non-zero only on things that move (rocks, gas clouds, animated
walls), and 0 means a still frame. Byte 14 is kept as a start phase in percent. These readings are
provisional; the raw 17 bytes are not needed by the client and are not copied.
"""
from __future__ import annotations
import argparse
import csv
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import struct
import sys
from decode_blo import parse_blo, decode_cfs

ROOT = Path(__file__).resolve().parents[2]
COMMIT = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35"
LVL_PATH = "LVL/Rogue.lvl"
ITM_PATH = "ITM/Rogue_mp.itm"
PINNED_BLO_SOURCE = ROOT / "shared/RogueTrader.OriginalData/PinnedBloArchives.cs"
HEADER_SIZE = 5152
ENTITY_SIZE = 17
OBJECT_MASK = 0x1FFF
# Prize graphic of each mineral item: ITM item fields f[23]/f[24] (blob archive, member).
MINERAL_ITEM_IDS = ("2003", "2005", "2019")


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def child(root: Path, relative: str) -> Path:
    parsed = PurePosixPath(relative)
    if parsed.is_absolute() or ".." in parsed.parts or "\\" in relative or ":" in relative:
        raise ValueError(f"Unsafe relative path: {relative!r}")
    path = (root / Path(*parsed.parts)).resolve()
    if not path.is_relative_to(root):
        raise ValueError("Path resolves outside its root")
    return path


def pinned_blo_hashes(source: Path = PINNED_BLO_SOURCE) -> dict[str, tuple[int, str]]:
    text = source.read_text(encoding="utf-8")
    pins = {m[1]: (int(m[2]), m[3]) for m in re.finditer(r'new\("(BLO/[^"]+)",\s*(\d+),\s*"([0-9a-f]{64})"\)', text)}
    if len(pins) != 89:
        raise ValueError(f"Expected 89 pinned BLO archives, found {len(pins)}")
    return pins


def read_pinned(source: Path, relative: str, size: int, digest: str) -> bytes:
    data = child(source, relative).read_bytes()
    if len(data) != size or sha(data) != digest:
        hint = " (an un-fetched Git LFS pointer? run scripts/sync-reference-assets.ps1)" if len(data) < 1024 else ""
        raise ValueError(f"Exact original hash/size mismatch: {relative}{hint}")
    return data


def cstring(raw: bytes) -> str:
    return raw.split(b"\0", 1)[0].decode("ascii")


def parse_level(data: bytes) -> tuple[list[tuple[str, str]], list[tuple[str, str]], list[dict]]:
    """Object references, floor references and entities of a version-6 LVL."""
    version, width, height, entity_count, floor_count, object_count = struct.unpack_from("<6i", data, 0)
    if version != 6 or width <= 0 or height <= 0 or min(entity_count, floor_count, object_count) < 0:
        raise ValueError("Unsupported LVL header")
    offset = HEADER_SIZE
    refs = []
    for _ in range(object_count + floor_count):
        refs.append((cstring(data[offset:offset + 32]), cstring(data[offset + 32:offset + 64])))
        offset += 64
    tail = len(data) - entity_count * ENTITY_SIZE
    if tail < offset:
        raise ValueError("LVL entity table overlaps the header")
    entities = []
    for index in range(entity_count):
        raw = data[tail + index * ENTITY_SIZE:tail + (index + 1) * ENTITY_SIZE]
        x, y, word, animation = struct.unpack_from("<hhHH", raw, 0)
        obj = word & OBJECT_MASK
        if obj >= object_count:
            raise ValueError(f"Entity {index} names object {obj}; the table has {object_count}")
        entities.append({"x": x, "y": y, "object": obj, "animationTime": animation, "phase": raw[14], "frame": word >> 13})
    return refs[:object_count], refs[object_count:], entities


def mineral_graphics(itm_text: str) -> dict[str, tuple[str, str, str]]:
    found = {}
    for values in csv.reader(itm_text.splitlines(), skipinitialspace=True):
        if len(values) > 24 and values[0] == "4" and values[2] in MINERAL_ITEM_IDS:
            found[values[2]] = (values[3], values[23], values[24])
    if set(found) != set(MINERAL_ITEM_IDS):
        raise ValueError(f"Mineral item rows missing from {ITM_PATH}: {sorted(set(MINERAL_ITEM_IDS) - set(found))}")
    return found


def stage(source: Path, manifest_path: Path, out: Path) -> dict:
    source = source.resolve(strict=True)
    out = out.resolve()
    if out == source or out.is_relative_to(source):
        raise ValueError("Output must stay outside the originals")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    if manifest["canonicalSource"]["commit"] != COMMIT:
        raise ValueError("Wrong canonical manifest commit")
    assets = {item["originalPath"]: item for item in manifest["assets"]}
    blo_pins = pinned_blo_hashes()

    def manifest_bytes(relative: str) -> bytes:
        item = assets[relative]
        return read_pinned(source, relative, item["originalSizeBytes"], item["originalSha256"])

    level_bytes = manifest_bytes(LVL_PATH)
    itm_bytes = manifest_bytes(ITM_PATH)
    objects, floors, entities = parse_level(level_bytes)
    minerals = mineral_graphics(itm_bytes.decode("utf-8-sig"))

    wanted = {(archive, member) for archive, member in objects}
    wanted |= {(archive + ".blo", member + ".cfs") for _, archive, member in minerals.values()}
    archives: dict[str, tuple[bytes, dict]] = {}
    for archive in sorted({archive for archive, _ in wanted}):
        pinned = next((key for key in blo_pins if key.casefold() == ("BLO/" + archive).casefold()), None)
        if pinned is None:
            raise ValueError(f"Archive is not a pinned canonical BLO: {archive}")
        data = read_pinned(source, pinned, *blo_pins[pinned])
        _, directory = parse_blo(data)
        archives[archive.casefold()] = (data, {entry["objectName"].casefold(): entry for entry in directory}, pinned)

    out.mkdir(parents=True, exist_ok=True)
    sprites: dict[tuple[str, str], dict] = {}

    def sprite(archive: str, member: str) -> dict:
        key = (archive.casefold(), member.casefold())
        if key in sprites:
            return sprites[key]
        data, directory, pinned = archives[archive.casefold()]
        entry = directory.get(member.casefold())
        if entry is None:
            raise ValueError(f"Member {member} is absent from {pinned}")
        metadata, image = decode_cfs(data[entry["dataOffset"]:entry["dataOffset"] + entry["sizeBytes"]])
        stem = Path(pinned).stem
        png_relative = f"{stem}/{Path(entry['objectName']).stem}.png"
        png = child(out, png_relative)
        png.parent.mkdir(parents=True, exist_ok=True)
        image.save(png, optimize=False)
        sprites[key] = {"archive": pinned, "member": entry["objectName"], "png": png_relative,
            "width": metadata["width"], "height": metadata["height"], "rowCount": metadata["rowCount"],
            "columnCount": metadata["columnCount"], "frameCount": metadata["frameCount"],
            "animationTime": metadata["animationTime"], "blitMode": metadata["blitMode"],
            "pngSha256": sha(png.read_bytes())}
        return sprites[key]

    object_table = [dict(index=index, **sprite(archive, member)) for index, (archive, member) in enumerate(objects)]
    mineral_table = [dict(itemId=item_id, itemName=name, **sprite(archive + ".blo", member + ".cfs"))
        for item_id, (name, archive, member) in sorted(minerals.items())]
    flat = []
    for entity in entities:
        flat += [entity["x"], entity["y"], entity["object"], entity["animationTime"], entity["phase"], entity["frame"]]

    level = {"schemaVersion": 1, "canonicalCommit": COMMIT,
        "source": {"level": LVL_PATH, "levelSha256": sha(level_bytes), "items": ITM_PATH, "itemsSha256": sha(itm_bytes)},
        "entityReading": "RECONSTRUCTED: x,y = top-left of the sprite cell in map pixels; object = uint16@4 & 0x1FFF; "
            "frame = uint16@4 >> 13 (starting frame); animationTime = uint16@6 (hundredths of a second per cycle, 0 = still); "
            "phase = byte 14 (percent).",
        "floors": [{"archive": a, "member": m} for a, m in floors],
        "objects": object_table, "minerals": mineral_table,
        "entityStride": 6, "entityFields": ["x", "y", "object", "animationTime", "phase", "frame"], "entities": flat}

    for relative, (size, digest) in [(LVL_PATH, (len(level_bytes), sha(level_bytes))), (ITM_PATH, (len(itm_bytes), sha(itm_bytes)))]:
        read_pinned(source, relative, size, digest)
    for data, _, pinned in archives.values():
        read_pinned(source, pinned, *blo_pins[pinned])
    child(out, "level.json").write_text(json.dumps(level, separators=(",", ":")) + "\n", encoding="utf-8")
    summary = {"objects": len(object_table), "minerals": len(mineral_table), "entities": len(entities),
        "archives": len(archives), "output": str(out)}
    print("LEVEL_ART_STAGE_PASS " + json.dumps(summary))
    return level


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-root", type=Path, default=ROOT / ".tools/references/cosmic-rift-assets")
    parser.add_argument("--manifest", type=Path, default=ROOT / "docs/original-research/ASSET_MANIFEST.json")
    parser.add_argument("--out", type=Path, default=ROOT / "client/OriginalContent/level")
    args = parser.parse_args()
    try:
        stage(args.source_root, args.manifest, args.out)
        return 0
    except (OSError, ValueError, KeyError, struct.error) as exception:
        print(f"LEVEL_ART_STAGE_FAILED: {exception}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
