#!/usr/bin/env python3
"""Independently written, read-only BLO/CFS decoder; preserves indexed original pixels.

Format facts were checked against pinned InfantryOnline/Gibbed primary sources.
No upstream implementation is copied or executed. Source files must match the
canonical manifest. PNG exports keep the raw palette and index-zero transparency;
shadow/light rendering modes remain explicit metadata pending native-client parity.
"""
from __future__ import annotations
import argparse
from collections import Counter
from dataclasses import dataclass
import hashlib
import csv
import json
from pathlib import Path
import re
import struct
import sys
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SOURCE_COMMIT = "eb1514eaae7f9b9425307d9cc043c74ecdd15e35"
FORMAT_COMMIT = "93001c57845269e87b32a2bf042257c7ef61298c"
FORMAT_BASE = f"https://github.com/InfantryOnline/Infantry-Online-Tools/blob/{FORMAT_COMMIT}/InfantryOnline.Tools/Tools.External/Gibbed"
DEFAULT_ARCHIVES = ["TerranShipsSmall", "SkithzarShipsSmall", "GraziShipsSmall", "rogueships", "Ebombs", "Resources", "ExplosionEffects", "weaponeffects", "rogueweapons", "cruiart", "O_Asteroids"]
FLAG_NAMES = {1:"DupeRowsVertically", 2:"DupeRowsHorizontally", 4:"ColumnsAreHalfRotation", 8:"ColumnsAreQuarterRotation",16:"NoPixels",32:"RowsAreHalfRotation",64:"RowsAreQuarterRotation",128:"NoCompression",256:"Unknown8"}
MAX_PIXELS = 64_000_000

class FormatError(ValueError):
    pass


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def checked_slice(data: bytes, offset: int, length: int, context: str) -> bytes:
    if offset < 0 or length < 0 or offset > len(data) or length > len(data) - offset:
        raise FormatError(f"{context}: truncated or out-of-bounds data")
    return data[offset:offset+length]


def unpack(fmt: str, data: bytes, offset: int, context: str):
    return struct.unpack(fmt, checked_slice(data, offset, struct.calcsize(fmt), context))


def cstring(data: bytes) -> str:
    return data.split(b"\0", 1)[0].decode("cp1252")


def parse_blo(data: bytes) -> tuple[int, list[dict]]:
    version, count = unpack("<II", data, 0, "BLO header")
    if version not in (1, 2):
        raise FormatError(f"Unsupported BLO version {version}")
    name_length = 32 if version == 2 else 14
    entry_size = name_length + 8
    table_end = 8 + count * entry_size
    checked_slice(data, 8, count * entry_size, "BLO directory")
    entries = []
    names = set()
    for object_id in range(count):
        row = 8 + object_id * entry_size
        name = cstring(checked_slice(data, row, name_length, "BLO name"))
        if not name or name in names or Path(name).name != name or any(char in name for char in '\\/:*?"<>|') or name in (".", ".."):
            raise FormatError(f"Invalid or duplicate archive entry name {name!r}")
        names.add(name)
        offset, size = unpack("<II", data, row + name_length, "BLO entry span")
        if offset < table_end:
            raise FormatError(f"BLO entry overlaps directory: {name}")
        checked_slice(data, offset, size, f"BLO entry {name}")
        entries.append({"objectId":object_id,"objectName":name,"dataOffset":offset,"sizeBytes":size,"sha256":digest(data[offset:offset+size])})
    spans = sorted((entry["dataOffset"],entry["dataOffset"]+entry["sizeBytes"]) for entry in entries)
    for previous, current in zip(spans,spans[1:]):
        if previous[1] > current[0]:
            raise FormatError("Overlapping BLO entry payloads")
    return version, entries


def parse_header(data: bytes) -> tuple[dict, int]:
    version, = unpack("<H",data,0,"CFS version")
    if version not in (2,3,4,5):
        raise FormatError(f"Unsupported CFS version {version}")
    header = {"cfsVersion":version,"blitMode":0,"rowMeaning":0,"ySortAdjust":0,"sortTransform":0,"userPaletteStart":0,"userPalette":0,"unknown20":0,"category":"","description":""}
    basics = ("frameCount","animationTime","width","height","rowCount","columnCount","lightCount","shadowCount","userDataSize")
    if version < 5:
        values=unpack("<9HBB",data,2,"CFS old header")
        header.update(zip(basics,values[:9]))
        header["compressionFlags"],header["maxSolidIndex"] = values[9:]
        if version == 2:
            header["dataSize"],=unpack("<I",data,28,"CFS v2 size")
            header["unknownBytesHex"]=checked_slice(data,22,6,"CFS v2 unknowns").hex()
            offset=32
        else:
            header["dataSize"],=unpack("<I",data,22,"CFS old size")
            header["category"]=cstring(checked_slice(data,26,16,"CFS old category"))
            header["blitMode"],header["rowMeaning"],header["ySortAdjust"],header["sortTransform"],header["userPaletteStart"],header["userPalette"]=unpack("<BBHBBB",data,42,"CFS old rendering")
            if version == 3:
                header["unknownBytesHex"]=checked_slice(data,49,9,"CFS v3 unknowns").hex()
                offset=58
            else:
                header["description"]=cstring(checked_slice(data,49,48,"CFS description"))
                header["unknownBytesHex"]=checked_slice(data,97,32,"CFS v4 unknowns").hex()
                offset=129
    else:
        values=unpack("<I10HI3H4B",data,2,"CFS v5 header")
        keys=("dataSize",)+basics+("ySortAdjust","compressionFlags","blitMode","rowMeaning","unknown20","maxSolidIndex","sortTransform","userPaletteStart","userPalette")
        header.update(zip(keys,values))
        header["category"]=cstring(checked_slice(data,40,32,"CFS v5 category"))
        header["description"]=cstring(checked_slice(data,72,48,"CFS v5 description"))
        header["unknownBytesHex"]=checked_slice(data,120,64,"CFS v5 unknowns").hex()
        offset=184
    if header["lightCount"] not in (0,32) or header["shadowCount"] not in (0,8):
        raise FormatError("Unsupported light/shadow index counts")
    if not header["width"] or not header["height"] or not header["rowCount"] or not header["columnCount"]:
        raise FormatError("Zero CFS canvas dimensions")
    if header["frameCount"] != header["rowCount"]*header["columnCount"]:
        raise FormatError("CFS frame count does not match row/column layout")
    if header["width"]*header["height"]*header["frameCount"]>MAX_PIXELS:
        raise FormatError("CFS canvas exceeds decoder resource bound")
    flags=header["compressionFlags"]
    if flags & ~0x1ff:
        raise FormatError("Unknown CFS compression flags")
    if flags&128 and flags & ~(16|128):
        raise FormatError("NoCompression combined with incompatible flags")
    if header["unknown20"] not in (0,3):
        raise FormatError("Unsupported CFS unknown20 value")
    header["compressionFlagNames"]=[name for flag,name in FLAG_NAMES.items() if flags&flag]
    return header,offset


def decode_frame(payload:bytes,frame:dict,flags:int) -> bytes:
    width,height=frame["width"],frame["height"]
    if not width or not height:
        return b""
    position=frame["dataOffset"]
    if flags&128:
        pixels=bytearray(checked_slice(payload,position,width*height,"CFS uncompressed pixels"))
    else:
        lengths=[]
        for _ in range(height):
            length,=unpack("<B",payload,position,"CFS scanline length");position+=1
            if length==255:
                length,=unpack("<H",payload,position,"CFS long scanline length");position+=2
            lengths.append(length)
        pixels=bytearray(width*height)
        for y,length in enumerate(lengths):
            commands=checked_slice(payload,position,length,"CFS RLE scanline");position+=length
            command_pos=0;column=0
            while command_pos<len(commands):
                marker=commands[command_pos];command_pos+=1
                column+=marker>>4
                literal_count=marker&15
                if column>width or literal_count>width-column:
                    raise FormatError("CFS RLE run exceeds frame scanline")
                literals=checked_slice(commands,command_pos,literal_count,"CFS RLE literals")
                pixels[y*width+column:y*width+column+literal_count]=literals
                command_pos+=literal_count;column+=literal_count
    if frame["signedWidth"]<0:
        pixels=bytearray(b"".join(pixels[y*width:(y+1)*width][::-1] for y in range(height)))
    if frame["signedHeight"]<0:
        pixels=bytearray(b"".join(pixels[y*width:(y+1)*width] for y in range(height-1,-1,-1)))
    return bytes(pixels)


def decode_cfs(data:bytes) -> tuple[dict,Image.Image]:
    header,position=parse_header(data)
    palette_raw=list(unpack("<256I",data,position,"CFS palette"));position+=1024
    user_data=checked_slice(data,position,header["userDataSize"],"CFS user data");position+=len(user_data)
    frames=[]
    for index in range(header["frameCount"]):
        x,y,signed_width,signed_height,offset=unpack("<HHhhI",data,position,"CFS frame directory");position+=12
        frame={"frameIndex":index,"row":index//header["columnCount"],"column":index%header["columnCount"],"x":x,"y":y,"signedWidth":signed_width,"signedHeight":signed_height,"width":abs(signed_width),"height":abs(signed_height),"dataOffset":offset}
        if x+frame["width"]>header["width"] or y+frame["height"]>header["height"]:
            raise FormatError("CFS frame crop exceeds original cell dimensions")
        frames.append(frame)
    payload=checked_slice(data,position,header["dataSize"],"CFS image payload")
    if any(frame["dataOffset"] > len(payload) for frame in frames):
        raise FormatError("CFS frame offset exceeds image payload")
    if position+len(payload)!=len(data):
        raise FormatError("Unexpected trailing bytes after CFS payload")
    atlas=Image.new("P",(header["width"]*header["columnCount"],header["height"]*header["rowCount"]),0)
    palette_rgb=[component for color in palette_raw for component in ((color>>16)&255,(color>>8)&255,color&255)]
    atlas.putpalette(palette_rgb)
    atlas.info["transparency"]=bytes([0]+[255]*255)
    for frame in frames:
        pixels=decode_frame(payload,frame,header["compressionFlags"])
        frame["pixelIndexSha256"]=digest(pixels)
        if pixels:
            frame_image=Image.frombytes("P",(frame["width"],frame["height"]),pixels)
            atlas.paste(frame_image,(frame["column"]*header["width"]+frame["x"],frame["row"]*header["height"]+frame["y"]))
    header.update(paletteRawU32=palette_raw,paletteRgb=palette_rgb,userDataHex=user_data.hex(),frames=frames,
        atlasWidth=atlas.width,atlasHeight=atlas.height,pixelIndexSha256=digest(atlas.tobytes()),transparentIndex=0,
        palettePolicy="Original RGB palette unchanged; index 0 transparent. Shadow/light palette indices and blit mode retained raw, not approximated.",
        rotationStatus="RowMeaning and raw row/column order preserved; native heading and state selection require separate validation")
    return header,atlas


def output_root_checked(source_root:Path,output:Path)->Path:
    output=output.resolve()
    if output==source_root or output.is_relative_to(source_root):
        raise FormatError("Derived output must remain outside the original reference directory")
    return output


def safe_output_path(output:Path,relative:str)->Path:
    destination=(output/relative).resolve()
    if not destination.is_relative_to(output):
        raise FormatError("Derived child output resolves outside the generated output root")
    return destination


def write_runtime_catalog(index:dict,source_root:Path,assets:dict,output:Path)->None:
    sprites=[]
    for archive in index["archives"]:
        archive_name=Path(archive["sourcePath"]).stem
        for entry in archive["entries"]:
            sprite={key:entry[key] for key in ("objectId","objectName","cfsVersion","width","height","rowCount","columnCount","frameCount","animationTime","rowMeaning","blitMode","lightCount","shadowCount","compressionFlags","transparentIndex","atlasPng","pngSha256")}
            sprite.update(archiveName=archive_name,sourcePath=archive["sourcePath"],sourceSha256=archive["sha256"],assetKey=archive["sourcePath"]+"::"+entry["objectName"])
            if archive_name in {"TerranShipsSmall","SkithzarShipsSmall","GraziShipsSmall","rogueships"}:
                sprite["directionObservation"]={"rowZero":"up","row16":"right","row32":"down","row48":"left","rowsAdvance":"clockwise","basis":"Direction-strip pixel inspection; original state-column selection still unvalidated"}
            sprites.append(sprite)
    relative="VEH/Rogue.veh";original=(source_root/relative).read_bytes();expected=assets[relative]
    if digest(original)!=expected["originalSha256"] or len(original)!=expected["originalSizeBytes"]:
        raise FormatError("Vehicle source changed or fails canonical exact-byte validation")
    ships=[]
    for row_number,values in enumerate(csv.reader(original.decode("utf-8-sig").splitlines(),skipinitialspace=True),1):
        if values[0]!="2":continue
        archive_token,member_token=values[382:384]
        matches=[sprite for sprite in sprites if sprite["archiveName"].casefold()==archive_token.casefold() and Path(sprite["objectName"]).stem.casefold()==member_token.casefold()]
        if len(matches)!=1:
            raise FormatError(f"Vehicle sprite reference does not resolve uniquely: {values[3]} ({archive_token},{member_token})")
        ships.append({"vehicleId":values[2],"name":values[3],"sourcePath":relative,"sourceRow":row_number,"sourceSha256":digest(original),"archiveToken":archive_token,"memberToken":member_token,"sourceFieldIndexes":[382,383],"assetKey":matches[0]["assetKey"]})
    runtime={"schemaVersion":1,"canonicalCommit":SOURCE_COMMIT,"palettePolicy":"Original indexed RGB retained; index0 transparent. Native user-palette/light/shadow/blit/state semantics remain pending.","atlasPathBase":"relative to generated asset directory","sprites":sprites,"ships":ships}
    safe_output_path(output,"runtime.json").write_text(json.dumps(runtime,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")

def contact_sheet(archive_name:str,entries:list[dict],images:list[Image.Image],out:Path)->str:
    cell_w,cell_h=160,135
    columns=6
    sheet=Image.new("RGB",(columns*cell_w,((len(entries)+columns-1)//columns)*cell_h),(11,16,25))
    draw=ImageDraw.Draw(sheet)
    for index,(entry,image) in enumerate(zip(entries,images)):
        x=(index%columns)*cell_w;y=(index//columns)*cell_h
        frame=image.crop((0,0,entry["width"],entry["height"])).convert("RGBA")
        if frame.width>140 or frame.height>96:
            frame.thumbnail((140,96),Image.Resampling.NEAREST)
        sheet.paste(frame,(x+10,y+24),frame)
        draw.text((x+6,y+5),entry["objectName"][:24],fill=(219,226,233))
        draw.text((x+6,y+118),f'{entry["width"]}x{entry["height"]} {entry["rowCount"]}r/{entry["columnCount"]}c',fill=(124,155,173))
    name=f"qa/{archive_name}-contact.png";path=safe_output_path(out,name);path.parent.mkdir(parents=True,exist_ok=True);sheet.save(path)
    return name


def direction_sheets(archive_name:str,entries:list[dict],images:list[Image.Image],output:Path)->list[str]:
    names=[]
    for page_start in range(0,len(entries),4):
        page_entries=entries[page_start:page_start+4];page_images=images[page_start:page_start+4]
        sheet=Image.new("RGB",(8*76,len(page_entries)*248),(11,16,25));draw=ImageDraw.Draw(sheet)
        for ship_index,(entry,atlas) in enumerate(zip(page_entries,page_images)):
            top=ship_index*248
            draw.text((6,top+3),entry["objectName"]+" / original column 0, middle, last",fill=(220,227,231))
            rgba=atlas.convert("RGBA");width,height=entry["width"],entry["height"]
            for state,column in enumerate((0,entry["columnCount"]//2,entry["columnCount"]-1)):
                for direction,row in enumerate(range(0,64,8)):
                    x=direction*76;y=top+20+state*76
                    frame=rgba.crop((column*width,row*height,(column+1)*width,(row+1)*height))
                    sheet.paste(frame,(x+10,y+17),frame)
                    draw.text((x+6,y+2),f"r{row} c{column}",fill=(150,176,192))
        page_suffix="" if len(entries)<=4 else f"-{page_start//4+1}"
        relative=f"qa/directions-{archive_name}{page_suffix}.png"
        sheet.save(safe_output_path(output,relative));names.append(relative)
    return names

def run(source_root:Path,output:Path,manifest_path:Path,archives:list[str])->dict:
    source_root=source_root.resolve(strict=True);output=output_root_checked(source_root,output)
    manifest_raw=manifest_path.read_bytes();manifest=json.loads(manifest_raw.decode("utf-8-sig"))
    if manifest["canonicalSource"]["commit"]!=SOURCE_COMMIT:
        raise FormatError("The manifest must use the pinned canonical commit")
    assets={asset["originalPath"]:asset for asset in manifest["assets"]}
    result={"schemaVersion":1,"canonicalSource":manifest["canonicalSource"],"formatReferenceCommit":FORMAT_COMMIT,
        "formatSources":[FORMAT_BASE+"/Gibbed.Infantry.FileFormats/"+name for name in ["BlobFile.cs","SpriteFile.cs","Sprite/Header.cs","Sprite/OldHeader2.cs","Sprite/OldHeader3.cs","Sprite/OldHeader4.cs","Sprite/FrameInfo.cs","Sprite/CompressionFlags.cs"]],
        "decoderProvenance":"Original Python implementation based on pinned format facts; no upstream code or binaries copied or executed.",
        "manifestSha256":digest(manifest_raw),"archives":[],"unsupportedEntries":[],"sourceHashesUnchanged":False}
    before={}
    output.mkdir(parents=True,exist_ok=True)
    for archive in archives:
        if not re.fullmatch(r"[A-Za-z0-9_-]+",archive):
            raise FormatError("Use canonical archive stems without paths")
        relative=f"BLO/{archive}.blo"
        if relative not in assets:
            raise FormatError(f"Source missing from canonical manifest: {relative}")
        original=(source_root/relative).resolve()
        if not original.is_relative_to(source_root):
            raise FormatError("Original archive path resolves outside source root")
        data=original.read_bytes();expected=assets[relative]
        if digest(data)!=expected["originalSha256"] or len(data)!=expected["originalSizeBytes"]:
            raise FormatError(f"Exact source hash/size mismatch: {relative}")
        before[relative]=digest(data)
        version,directory=parse_blo(data)
        archive_index={"sourcePath":relative,"sha256":digest(data),"version":version,"sizeBytes":len(data),"entries":[]}
        images=[]
        for entry in directory:
            if not entry["objectName"].casefold().endswith(".cfs"):
                result["unsupportedEntries"].append({"sourcePath":relative,**entry,"reason":"Non-CFS payload; audio/other formats not decoded"})
                continue
            metadata,image=decode_cfs(data[entry["dataOffset"]:entry["dataOffset"]+entry["sizeBytes"]])
            name=Path(entry["objectName"]).stem
            png_relative=f"{archive}/{name}.png"
            png_path=safe_output_path(output,png_relative);png_path.parent.mkdir(parents=True,exist_ok=True)
            image.save(png_path,optimize=False)
            with Image.open(png_path) as check:
                if check.mode!="P" or check.size!=image.size or check.tobytes()!=image.tobytes() or check.getpalette()!=image.getpalette():
                    raise FormatError(f"PNG indexed pixels/palette round-trip failed: {png_relative}")
                transparency=check.info.get("transparency")
                if transparency not in (0,bytes([0]+[255]*255)):
                    raise FormatError(f"PNG transparency round-trip failed: {png_relative}")
            final={**entry,**metadata,"atlasPng":png_relative,"pngSha256":digest(png_path.read_bytes())}
            archive_index["entries"].append(final);images.append(image)
        if images:
            archive_index["qaContactSheet"]=contact_sheet(archive,archive_index["entries"],images,output)
            if archive in {"TerranShipsSmall","SkithzarShipsSmall","GraziShipsSmall","rogueships"}:
                archive_index["qaDirectionSheets"]=direction_sheets(archive,archive_index["entries"],images,output)
        result["archives"].append(archive_index)
        print(f'DECODED {relative}: {len(archive_index["entries"])} CFS entries / {sum(entry["frameCount"] for entry in archive_index["entries"])} frames')
    for relative,expected_hash in before.items():
        if digest((source_root/relative).read_bytes())!=expected_hash:
            raise FormatError(f"Original changed during export: {relative}")
    result["sourceHashesUnchanged"]=True
    result["summary"]={"archiveCount":len(result["archives"]),"cfsObjects":sum(len(archive["entries"]) for archive in result["archives"]),"frames":sum(entry["frameCount"] for archive in result["archives"] for entry in archive["entries"]),"unsupportedEntries":len(result["unsupportedEntries"])}
    full_preset = set(archives) == set(DEFAULT_ARCHIVES)
    index_name = "index.json" if full_preset else "index-selected-"+digest("|".join(sorted(archives)).encode())[:12]+".json"
    result["indexFile"] = index_name
    safe_output_path(output,index_name).write_text(json.dumps(result,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    if full_preset:
        write_runtime_catalog(result,source_root,assets,output)
    print("ORIGINAL_ASSET_DECODE_PASS "+json.dumps(result["summary"]))
    return result


def main()->int:
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-root",type=Path,default=ROOT/".tools/references/cosmic-rift-assets")
    parser.add_argument("--manifest",type=Path,default=ROOT/"docs/original-research/ASSET_MANIFEST.json")
    parser.add_argument("--out",type=Path,default=ROOT/"artifacts/original-assets")
    parser.add_argument("--archives",nargs="+",default=DEFAULT_ARCHIVES)
    args=parser.parse_args()
    try:
        run(args.source_root,args.out,args.manifest,args.archives)
        return 0
    except (OSError,ValueError,KeyError,struct.error) as exception:
        print(f"ORIGINAL_ASSET_DECODE_FAILED: {exception}",file=sys.stderr)
        return 1

if __name__=="__main__":
    raise SystemExit(main())




