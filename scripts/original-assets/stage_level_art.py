#!/usr/bin/env python3
"""Stage the original Rogue map art (asteroids, stations, walls, gas clouds) and mineral prize sprites.

Reads the pinned reference checkout only: LVL/Rogue.lvl, the BLO archives its object table names, and
ITM/Rogue_mp.itm for the mineral prize graphics. Every original is checked against its pinned SHA-256
(LVL/ITM from the canonical manifest, BLOs from shared/RogueTrader.OriginalData/PinnedBloArchives.cs)
before it is read, and again afterwards. Output goes to client/OriginalContent/level/ (git-ignored):
indexed PNG atlases with original palettes, plus level.json, which the client draws from.

LVL entity reading (FORMAT SOURCE: Gibbed.Infantry.FileFormats Level/Entity.cs at the pinned InfantryOnline tools commit
93001c5, which reads each 17-byte record as int16 X, int16 Y, uint32 BitsA, uint32 BitsB, uint32 BitsC, byte BitsD):
BitsA bits 0-12 = object index, 13-19 = FrameIndex (starting frame), 20-29 = AnimationTime, 30-31 = Transluency
(0 opaque, 1 = 50%, 2 = 33%, 3 = 25%); BitsB bits 0-12 = TimeDelta, 13-25 = ExtraData, 26-28 = DrawOrder (0-2 static
first/sorted/last, 3-5 dynamic first/sorted/last), 29-30 = DetailLevel, 31 = SmartTransluency; BitsC bits 0-7 = Value
(signed), 8-15 = Saturation (signed), 16-22 = Hue, 23-29 = light; BitsD bits 0-5 = PaletteShift.
X and Y are the sprite cell's top-left in map pixels (RECONSTRUCTED from a test render: the station trusses join up and
every rock sits exactly on its collision tiles).

Animation timing (RECONSTRUCTED; the format source names the fields but not their units): the entity's AnimationTime and
the CFS header's AnimationTime are read as milliseconds per frame. Gibbed's Blob Editor plays a CFS by stepping one frame
per AnimationTime milliseconds (CfsPreviewControl, a WinForms Timer interval), and Rogue.lvl's entity values (150 gas
clouds, 250/300 rocks, 100 map border) sit in the same range as the CFS values (100 gas clouds, 200 rocks, 50 map border),
so both are taken as the same unit. An entity AnimationTime of 0 is a still frame: the 1,194 safe-zone markings carry 0
and their five frames are a corner, edges and a dotted fill, not an animation. Earlier schemas read uint16 at byte 6 as
hundredths of a second per cycle; that word is AnimationTime x 16 (plus FrameIndex bits 3-6), and byte 14 is Hue, not a
start phase.

In Rogue.lvl every entity has Transluency 0; the gas clouds gasCloud, gasCloud1_, gasCloud2_ and gasCloud3sm_ (1,408
entities) are the only objects other than 61 safe-zone markings with SmartTransluency set, and they use DrawOrder 5
(dynamic last, over ships). gasCloud4sm_ (275 entities) is opaque DrawOrder 4 (dynamic sorted). How strong the native
smart translucency is, and when the client applies it, is not in the format source; the client's 50% is RECONSTRUCTED.
927 gas clouds carry Value -10; the client darkens by Value percent (RECONSTRUCTED). Hue, Saturation and light are staged
but not applied: their scale is unknown and Hue would recolour every station and wall.
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
DRAW_ORDER_DYNAMIC_LAST = 5
ENTITY_FIELDS = ("x", "y", "object", "frame", "animationTime", "timeDelta", "drawOrder", "translucency", "smartTranslucency",
    "value", "saturation", "hue", "light", "paletteShift")
SCHEMA_VERSION = 3
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


def signed_byte(value: int) -> int:
    value &= 0xFF
    return value - 256 if value > 127 else value


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
        x, y, bits_a, bits_b, bits_c, bits_d = struct.unpack("<hhIIIB", raw)
        obj = bits_a & OBJECT_MASK
        if obj >= object_count:
            raise ValueError(f"Entity {index} names object {obj}; the table has {object_count}")
        entities.append({"x": x, "y": y, "object": obj, "frame": (bits_a >> 13) & 0x7F, "animationTime": (bits_a >> 20) & 0x3FF,
            "timeDelta": bits_b & 0x1FFF, "drawOrder": (bits_b >> 26) & 7, "translucency": bits_a >> 30,
            "smartTranslucency": bits_b >> 31, "value": signed_byte(bits_c), "saturation": signed_byte(bits_c >> 8),
            "hue": (bits_c >> 16) & 0x7F, "light": (bits_c >> 23) & 0x7F, "paletteShift": bits_d & 0x3F})
    return refs[:object_count], refs[object_count:], entities


def physics_plane(data: bytes) -> tuple[int, int, bytes]:
    """Width, height and the third RLE plane (low 5 bits = physics; non-zero blocks), as the G6 reader decodes it."""
    version, width, height, _, floor_count, object_count = struct.unpack_from("<6i", data, 0)
    offset = HEADER_SIZE + (floor_count + object_count) * 64
    tiles = width * height
    planes = []
    for _ in range(3):
        plane = bytearray()
        while len(plane) < tiles:
            length = data[offset]
            offset += 1
            if length == 0:
                length = (data[offset] << 8) | data[offset + 1]
                offset += 2
            if length == 0 or len(plane) + length > tiles:
                raise ValueError("Invalid LVL plane run")
            plane += bytes([data[offset]]) * length
            offset += 1
        planes.append(bytes(plane))
    return width, height, planes[2]


def uncovered_blocked_tiles(width: int, height: int, physics: bytes, entities: list[dict], cells: list[tuple[int, int]]) -> int:
    """Blocked tiles that no placed sprite's cell overlaps; the client draws these with the NEW wall tint."""
    covered = bytearray(width * height)
    for entity in entities:
        cell_width, cell_height = cells[entity["object"]]
        x0, y0 = max(0, entity["x"] // 16), max(0, entity["y"] // 16)
        x1, y1 = min(width - 1, (entity["x"] + cell_width - 1) // 16), min(height - 1, (entity["y"] + cell_height - 1) // 16)
        for ty in range(y0, y1 + 1):
            covered[ty * width + x0:ty * width + x1 + 1] = b"\1" * (x1 - x0 + 1)
    return sum(1 for i, value in enumerate(physics) if value & 0x1F and not covered[i])


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
    width, height, physics = physics_plane(level_bytes)
    blocked = sum(1 for value in physics if value & 0x1F)
    uncovered = uncovered_blocked_tiles(width, height, physics, entities, [(o["width"], o["height"]) for o in object_table])
    flat = []
    for entity in entities:
        flat += [entity[field] for field in ENTITY_FIELDS]

    over_ships = sum(1 for entity in entities if entity["drawOrder"] == DRAW_ORDER_DYNAMIC_LAST)
    translucent = sum(1 for entity in entities if entity["translucency"] or entity["smartTranslucency"])
    level = {"schemaVersion": SCHEMA_VERSION, "canonicalCommit": COMMIT,
        "source": {"level": LVL_PATH, "levelSha256": sha(level_bytes), "items": ITM_PATH, "itemsSha256": sha(itm_bytes)},
        "entityReading": "FORMAT SOURCE (Gibbed Level/Entity.cs): object = BitsA 0-12, frame = BitsA 13-19, animationTime = "
            "BitsA 20-29, translucency = BitsA 30-31, timeDelta = BitsB 0-12, drawOrder = BitsB 26-28, smartTranslucency = BitsB 31, "
            "value/saturation = signed BitsC 0-7/8-15, hue = BitsC 16-22, light = BitsC 23-29, paletteShift = BitsD 0-5. "
            "RECONSTRUCTED: x,y = top-left of the sprite cell in map pixels; animationTime (entity and CFS) = milliseconds per frame, "
            "entity 0 = still.",
        "blockedTiles": blocked, "uncoveredBlockedTiles": uncovered,
        "floors": [{"archive": a, "member": m} for a, m in floors],
        "objects": object_table, "minerals": mineral_table,
        "entitiesOverShips": over_ships, "translucentEntities": translucent,
        "entityStride": len(ENTITY_FIELDS), "entityFields": list(ENTITY_FIELDS), "entities": flat}

    for relative, (size, digest) in [(LVL_PATH, (len(level_bytes), sha(level_bytes))), (ITM_PATH, (len(itm_bytes), sha(itm_bytes)))]:
        read_pinned(source, relative, size, digest)
    for data, _, pinned in archives.values():
        read_pinned(source, pinned, *blo_pins[pinned])
    child(out, "level.json").write_text(json.dumps(level, separators=(",", ":")) + "\n", encoding="utf-8")
    summary = {"objects": len(object_table), "minerals": len(mineral_table), "entities": len(entities),
        "archives": len(archives), "blockedTiles": blocked, "uncoveredBlockedTiles": uncovered,
        "entitiesOverShips": over_ships, "translucentEntities": translucent, "output": str(out)}
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
