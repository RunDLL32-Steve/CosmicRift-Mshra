#!/usr/bin/env python3
"""Verify and stage exact generated sprites plus a lossless canonical map preview.

Original BLO/BMP inputs and generated source exports are read-only. Runtime/map
metadata is written only in the chosen staging folder, outside the originals.
Pillow decodes the original BMP; its dimensions and decoded RGB pixels are retained.
"""
from __future__ import annotations
import argparse
import hashlib
import csv
import json
from pathlib import Path, PurePosixPath
import shutil
import sys
from PIL import Image
from decode_blo import parse_blo, decode_cfs

ROOT=Path(__file__).resolve().parents[2]
COMMIT="eb1514eaae7f9b9425307d9cc043c74ecdd15e35"
MAP_PATH="LVL-BMP/Rogue.bmp"


def sha(data:bytes)->str:return hashlib.sha256(data).hexdigest()


def child(root:Path,relative:str)->Path:
    parsed=PurePosixPath(relative)
    if parsed.is_absolute() or ".." in parsed.parts or "\\" in relative:
        raise ValueError(f"Unsafe relative content path: {relative!r}")
    path=(root/Path(*parsed.parts)).resolve()
    if not path.is_relative_to(root):raise ValueError("Content child resolves outside its root")
    return path


def verify_original(source:Path,relative:str,assets:dict)->tuple[Path,str]:
    if relative not in assets:raise ValueError(f"Original path absent from manifest: {relative}")
    metadata=assets[relative]
    if metadata.get("commit")!=COMMIT:raise ValueError(f"Original asset has wrong pinned commit: {relative}")
    path=child(source,relative);data=path.read_bytes();digest=sha(data)
    if digest!=metadata["originalSha256"] or len(data)!=metadata["originalSizeBytes"]:
        raise ValueError(f"Exact original hash/size mismatch: {relative}")
    return path,digest


def stage(generated:Path,source:Path,manifest_path:Path,out:Path)->dict:
    generated=generated.resolve(strict=True);source=source.resolve(strict=True);out=out.resolve()
    if out==source or out.is_relative_to(source):raise ValueError("Staging output must remain outside originals")
    if out==generated or out.is_relative_to(generated) or generated.is_relative_to(out):
        raise ValueError("Staging output must be separate from generated source exports")
    manifest=json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    if manifest["canonicalSource"]["commit"]!=COMMIT:raise ValueError("Wrong canonical manifest commit")
    assets={item["originalPath"]:item for item in manifest["assets"]}
    runtime_path=generated/"runtime.json";runtime_bytes=runtime_path.read_bytes();runtime_hash=sha(runtime_bytes)
    runtime=json.loads(runtime_bytes)
    if runtime["canonicalCommit"]!=COMMIT:raise ValueError("Generated runtime uses the wrong canonical commit")
    before={}
    pngs={}
    source_archives={}
    for sprite in runtime["sprites"]:
        relative=sprite["sourcePath"]
        if relative not in before:
            _,before[relative]=verify_original(source,relative,assets)
        if sprite["sourceSha256"]!=before[relative]:raise ValueError("Sprite archive reference hash does not match original")
        if relative not in source_archives:
            archive_bytes=child(source,relative).read_bytes();_,directory=parse_blo(archive_bytes)
            source_archives[relative]=(archive_bytes,{entry["objectName"]:entry for entry in directory})
        archive_bytes,directory=source_archives[relative]
        entry=directory.get(sprite["objectName"])
        if entry is None or entry["objectId"]!=sprite["objectId"] or sprite["assetKey"]!=relative+"::"+entry["objectName"]:
            raise ValueError("Sprite member/table identity does not match canonical BLO")
        original_metadata,original_atlas=decode_cfs(archive_bytes[entry["dataOffset"]:entry["dataOffset"]+entry["sizeBytes"]])
        for field in ("cfsVersion","width","height","rowCount","columnCount","frameCount","animationTime","rowMeaning","blitMode","lightCount","shadowCount","compressionFlags","transparentIndex"):
            if sprite[field]!=original_metadata[field]:raise ValueError("Sprite metadata differs from canonical CFS: "+field)
        png_relative=sprite["atlasPng"]
        png=child(generated,png_relative);png_bytes=png.read_bytes()
        if sha(png_bytes)!=sprite["pngSha256"]:raise ValueError(f"Generated PNG hash mismatch: {png_relative}")
        if png_relative in pngs and pngs[png_relative]!=sprite["pngSha256"]:
            raise ValueError("Conflicting duplicate sprite PNG reference")
        with Image.open(png) as image:
            if image.mode!="P" or image.size!=(sprite["width"]*sprite["columnCount"],sprite["height"]*sprite["rowCount"]):
                raise ValueError(f"Generated PNG does not preserve indexed atlas dimensions: {png_relative}")
            if image.tobytes()!=original_atlas.tobytes() or image.getpalette()!=original_atlas.getpalette() or image.info.get("transparency") not in (0,bytes([0]+[255]*255)):
                raise ValueError(f"Generated PNG pixels/palette/transparency differ from canonical CFS: {png_relative}")
        pngs[png_relative]=sprite["pngSha256"]
    for ship in runtime["ships"]:
        relative=ship["sourcePath"]
        if relative not in before:_,before[relative]=verify_original(source,relative,assets)
        if ship["sourceSha256"]!=before[relative]:raise ValueError("Ship record source hash does not match original")
        source_rows=list(csv.reader(child(source,relative).read_text(encoding="utf-8-sig").splitlines(),skipinitialspace=True))
        values=source_rows[ship["sourceRow"]-1]
        if values[2]!=ship["vehicleId"] or values[3]!=ship["name"] or values[382:384]!=[ship["archiveToken"],ship["memberToken"]]:
            raise ValueError("Ship identity/reference tokens differ from canonical VEH row")
        if not any(sprite["assetKey"]==ship["assetKey"] for sprite in runtime["sprites"]):
            raise ValueError("Ship sprite asset key is unresolved")
    map_original,map_hash=verify_original(source,MAP_PATH,assets);before[MAP_PATH]=map_hash
    with Image.open(map_original) as original:
        if original.format!="BMP":raise ValueError("Canonical map source is not a BMP image")
        rgb=original.convert("RGB")
        map_dimensions=rgb.size;pixel_hash=sha(rgb.tobytes())
    out.mkdir(parents=True,exist_ok=True)
    for png_relative,expected_hash in pngs.items():
        target=child(out,png_relative);target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(child(generated,png_relative),target)
        if sha(target.read_bytes())!=expected_hash:raise ValueError("Staged PNG bytes differ from generated original-pixel export")
    map_png=child(out,"Rogue.png");rgb.save(map_png,optimize=False)
    with Image.open(map_png) as check:
        if check.mode!="RGB" or check.size!=map_dimensions or sha(check.tobytes())!=pixel_hash:
            raise ValueError("Map PNG RGB/dimension round-trip failed")
    runtime["map"]={"sourcePath":MAP_PATH,"sourceSha256":map_hash,"sourceCommit":COMMIT,
        "originalSizeBytes":assets[MAP_PATH]["originalSizeBytes"],"width":map_dimensions[0],"height":map_dimensions[1],
        "png":"Rogue.png","pngSha256":sha(map_png.read_bytes()),"pixelRgbSha256":pixel_hash,
        "role":"Canonical map overview only; no gameplay collision or terrain inferred from preview"}
    runtime["staging"]={"generatedRuntimeSha256":runtime_hash,"spritesCopiedByteForByte":True,
        "sourceHashesUnchanged":True,"mapRgbPixelsUnchanged":True,"canonicalCfsPixelsAndPaletteVerified":True}
    for relative,expected_hash in before.items():
        if sha(child(source,relative).read_bytes())!=expected_hash:raise ValueError(f"Original changed during staging: {relative}")
    if sha(runtime_path.read_bytes())!=runtime_hash:raise ValueError("Generated runtime metadata changed during staging")
    child(out,"runtime.json").write_text(json.dumps(runtime,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    summary={"stagedSprites":len(pngs),"originalSourcesVerified":len(before),"ships":len(runtime["ships"]),
        "mapDimensions":list(map_dimensions),"output":str(out)}
    print("ORIGINAL_ASSET_STAGE_PASS "+json.dumps(summary))
    return runtime


def main()->int:
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--generated",type=Path,default=ROOT/"artifacts/original-assets")
    parser.add_argument("--source-root",type=Path,default=ROOT/".tools/references/cosmic-rift-assets")
    parser.add_argument("--manifest",type=Path,default=ROOT/"docs/original-research/ASSET_MANIFEST.json")
    parser.add_argument("--out",type=Path,default=ROOT/"client/OriginalContent")
    args=parser.parse_args()
    try:
        stage(args.generated,args.source_root,args.manifest,args.out)
        return 0
    except (ValueError,KeyError,OSError) as exception:
        print(f"ORIGINAL_ASSET_STAGE_FAILED: {exception}",file=sys.stderr);return 1

if __name__=="__main__":raise SystemExit(main())

