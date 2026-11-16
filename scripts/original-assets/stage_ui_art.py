#!/usr/bin/env python3
"""Stage the original radar frame art (RT-F064 radar rendering step).

Reads the pinned reference checkout only: CFG/rogue.cfg names the radar frame in [Uiart] RadarBorder
(cruiart,newradar), and BLO/cruiart.blo holds it. Both are checked against their pinned SHA-256 (CFG from the
canonical manifest, the BLO from shared/RogueTrader.OriginalData/PinnedBloArchives.cs) before and after reading.
Output goes to client/OriginalContent/ui/ (git-ignored): the indexed PNG atlas with its original palette, and
ui.json with each frame's crop rectangle inside its cell, exactly as the CFS frame directory gives it.

How the client joins the eight pieces into a frame is RECONSTRUCTED (see client/UI/MinimapView.cs); this script
only copies the pieces.
"""
from __future__ import annotations
import argparse
import json
from pathlib import Path
import struct
import sys
from decode_blo import parse_blo, decode_cfs, FormatError
from stage_level_art import COMMIT, ROOT, child, pinned_blo_hashes, read_pinned, sha

CFG_PATH = "CFG/rogue.cfg"
BORDER_KEY = ("Uiart", "RadarBorder")


def cfg_value(text: str, section: str, key: str) -> tuple[int, str]:
    """(line number, raw value) of one key; throws when the section or key is missing."""
    current = None
    for number, line in enumerate(text.splitlines(), 1):
        line = line.strip()
        if line.startswith("[") and "]" in line:
            current = line[1:line.index("]")]
        elif current == section and line.partition("=")[0] == key:
            return number, line.partition("=")[2]
    raise ValueError(f"{CFG_PATH} has no [{section}] {key}")


def border_reference(text: str) -> tuple[int, str, str]:
    line, value = cfg_value(text, *BORDER_KEY)
    archive, comma, member = value.partition(",")
    if not comma or not archive or not member or "," in member:
        raise ValueError(f"[Uiart] RadarBorder is not 'archive,member': {value!r}")
    return line, archive, member


def stage(source: Path, manifest_path: Path, out: Path) -> dict:
    source = source.resolve(strict=True)
    out = out.resolve()
    if out == source or out.is_relative_to(source):
        raise ValueError("Output must stay outside the originals")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    if manifest["canonicalSource"]["commit"] != COMMIT:
        raise ValueError("Wrong canonical manifest commit")
    item = next(a for a in manifest["assets"] if a["originalPath"] == CFG_PATH)
    cfg_bytes = read_pinned(source, CFG_PATH, item["originalSizeBytes"], item["originalSha256"])
    line, archive, member = border_reference(cfg_bytes.decode("latin-1"))

    pins = pinned_blo_hashes()
    pinned = next((key for key in pins if key.casefold() == f"BLO/{archive}.blo".casefold()), None)
    if pinned is None:
        raise ValueError(f"Archive is not a pinned canonical BLO: {archive}")
    data = read_pinned(source, pinned, *pins[pinned])
    _, directory = parse_blo(data)
    entry = next((e for e in directory if e["objectName"].casefold() == f"{member}.cfs".casefold()), None)
    if entry is None:
        raise ValueError(f"Member {member}.cfs is absent from {pinned}")
    metadata, image = decode_cfs(data[entry["dataOffset"]:entry["dataOffset"] + entry["sizeBytes"]])
    if metadata["columnCount"] != 1:
        raise ValueError("The radar frame atlas is expected to hold one piece per row")

    png_relative = f"{Path(pinned).stem}/{Path(entry['objectName']).stem}.png"
    png = child(out, png_relative)
    png.parent.mkdir(parents=True, exist_ok=True)
    image.save(png, optimize=False)
    pieces = [{"frame": f["frameIndex"], "x": f["x"], "y": f["y"], "width": f["width"], "height": f["height"]} for f in metadata["frames"]]
    document = {"schemaVersion": 1, "canonicalCommit": COMMIT,
        "source": {"config": CFG_PATH, "configSha256": sha(cfg_bytes), "configLine": line, "archive": pinned,
            "archiveSha256": sha(data), "member": entry["objectName"], "memberSha256": entry["sha256"]},
        "radarBorder": {"png": png_relative, "pngSha256": sha(png.read_bytes()), "cellWidth": metadata["width"],
            "cellHeight": metadata["height"], "pieces": pieces}}

    read_pinned(source, CFG_PATH, len(cfg_bytes), sha(cfg_bytes))
    read_pinned(source, pinned, *pins[pinned])
    child(out, "ui.json").write_text(json.dumps(document, indent=1) + "\n", encoding="utf-8")
    print("UI_ART_STAGE_PASS " + json.dumps({"radarBorder": f"{pinned}:{entry['objectName']}", "pieces": len(pieces), "output": str(out)}))
    return document


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-root", type=Path, default=ROOT / ".tools/references/cosmic-rift-assets")
    parser.add_argument("--manifest", type=Path, default=ROOT / "docs/original-research/ASSET_MANIFEST.json")
    parser.add_argument("--out", type=Path, default=ROOT / "client/OriginalContent/ui")
    args = parser.parse_args()
    try:
        stage(args.source_root, args.manifest, args.out)
        return 0
    except (OSError, ValueError, KeyError, StopIteration, FormatError, struct.error) as exception:
        print(f"UI_ART_STAGE_FAILED: {exception}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
