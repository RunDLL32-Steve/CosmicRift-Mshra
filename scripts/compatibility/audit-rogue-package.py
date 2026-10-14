#!/usr/bin/env python3
"""Bounded read-only audit of canonical Rogue package and pinned parser/transfer evidence."""
from __future__ import annotations
import argparse, base64, collections, hashlib, json, re, struct, subprocess, sys, unittest, zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CANON = ROOT / ".tools/references/cosmic-rift-assets"
SERVER = ROOT / ".tools/references/infantry-online-server"
SERVER_COMMIT = "bb97579efeceff1c63d781d8c1bb8dd4a5b9b849"
REPORTS = ROOT / ".tools/compatibility/reports"

def sha(data): return hashlib.sha256(data).hexdigest()
def read_json(path): return json.loads(path.read_text(encoding="utf-8-sig"))
def source(relative): return (SERVER / "dotnetcore" / relative).read_text(encoding="utf-8-sig")
def rle(data, offset, length):
    result = bytearray()
    while len(result) < length:
        if offset >= len(data): raise ValueError("Truncated LVL RLE")
        count = data[offset]; offset += 1
        if count == 0:
            if offset + 2 > len(data): raise ValueError("Truncated extended RLE")
            count = int.from_bytes(data[offset:offset+2], "big"); offset += 2
        if not count or offset >= len(data) or count > length-len(result): raise ValueError("Invalid RLE run")
        result.extend(bytes([data[offset]])*count); offset += 1
    return bytes(result), offset

def audit(reports=REPORTS):
    commit = subprocess.run(["git", "-C", str(SERVER), "rev-parse", "HEAD"], check=True, capture_output=True, text=True).stdout.strip()
    if commit != SERVER_COMMIT: raise ValueError("Pinned parser source revision mismatch")
    raw = read_json(ROOT / "artifacts/research/original-content/index.json")
    inventory = read_json(ROOT / "artifacts/original-dependencies/index.json")
    inputs = read_json(reports / "pristine-load-report.json")
    if inputs["UpstreamCommit"] != SERVER_COMMIT or inputs["FailureCount"] != 0: raise ValueError("Parser evidence invalid")
    hashes = inventory["sourceIntegrityBeforeAfter"]
    for path, info in hashes.items():
        data = (CANON/path).read_bytes()
        if sha(data) != info["sha256"] or len(data) != info["sizeBytes"]: raise ValueError(f"Altered original {path}")
    records = raw["records"]
    domains = {prefix: [r for r in records if r["sourcePath"].startswith(prefix+"/")] for prefix in ("ITM","VEH","RPG","LIO")}
    parsed = {
        "ITM": [r["Record"] for r in read_json(reports / "pristine-items.json")],
        "VEH": [r["Record"] for r in read_json(reports / "pristine-vehicles.json")],
        "RPG": read_json(reports / "pristine-skills.json"),
        "LIO": [r["Record"]["GeneralData"] for r in read_json(reports / "rogue-lio-records.json")["Records"]]}
    id_fields = {"ITM":"id","VEH":"Id","RPG":"SkillId","LIO":"Id"}
    identity = {}
    for domain, rows in domains.items():
        expected = {int(r["identifier"]): r for r in rows}
        actual = {int(r[id_fields[domain]]): r for r in parsed[domain]}
        if len(actual) != len(rows) or set(actual) != set(expected): raise ValueError(f"{domain} identity mismatch")
        for key, record in expected.items():
            obj=actual[key]
            version_key="version" if domain=="ITM" else "Version"
            if str(obj[version_key]).lstrip("v")!=str(record["version"]).lstrip("v"):
                raise ValueError(f"{domain} version mismatch {key}")
            name_key="name" if domain=="ITM" else "Name"
            if obj[name_key].strip('"')!=record["name"].strip('"'):
                raise ValueError(f"{domain} name mismatch {key}")
        identity[domain] = dict(rawCount=len(rows), parsedCount=len(actual), exactIds=True)
    ids = {d:set(int(r["identifier"]) for r in rows) for d,rows in domains.items()}
    links = []
    def link(domain, parent, role, target_domain, target, field=None, raw_value=None):
        links.append(dict(sourceDomain=domain, parentId=parent, role=role, targetDomain=target_domain,
                          targetId=target, sourceFieldIndex=field, rawValue=raw_value,
                          status="sentinel_or_default_unvalidated" if target <= 0 else "present" if target in ids[target_domain] else "missing"))
    hulls = []
    raw_skills = {int(r["identifier"]):r for r in domains["RPG"]}
    vehicles = {v["Id"]:v for v in parsed["VEH"]}
    for skill in parsed["RPG"]:
        r=raw_skills[skill["SkillId"]]
        if skill["DefaultVehicleId"] != int(r["fields"][3]): raise ValueError("RPG vehicle raw mismatch")
        veh=vehicles[skill["DefaultVehicleId"]]
        if skill["Name"] != veh["Name"]: raise ValueError("Hull name mismatch")
        hulls.append(dict(skillId=skill["SkillId"],vehicleId=veh["Id"],name=skill["Name"],rpgRow=r["row"]))
        link("RPG",skill["SkillId"],"DefaultVehicleId","VEH",veh["Id"],3,r["fields"][3])
        for m in skill["InventoryMutators"]: link("RPG",skill["SkillId"],"InventoryMutators.ItemId","ITM",m["ItemId"])
    item_rows = {int(r["identifier"]):r for r in domains["ITM"]}
    item_exports = read_json(reports / "pristine-items.json")
    value_checks, coverage, common_omissions = [], [], []
    for wrapped in item_exports:
        obj,kind=wrapped["Record"],wrapped["Kind"]
        row=item_rows[obj["id"]]
        path=f"Assets/Itm/ItmInfo.{kind}.cs"
        if kind == "Item": path="Assets/Itm/ItmInfo.Item.cs"
        text=source(path)
        # Source-supported direct literal CSV assignments; not a general C# evaluator.
        regex=r'\b\w+\.(\w+)\s*=\s*(?:\([^)]*\))?CSVReader\.(GetInt|GetFloat|GetBool|GetQuotedString|GetString)\(values\[(\d+)\](?:\.Trim\(\x27v\x27\))?\)'
        common = re.findall(regex,source("Assets/Itm/ItmInfo.Item.cs"))
        mappings = re.findall(regex,(source("Assets/Itm/ItmInfo.Item.cs") if "LoadGeneralSettings1" in text else "")+text)
        mappings=list({m[0]:m for m in mappings}.values())
        if "LoadGeneralSettings1" not in text:
            assigned={m[0] for m in mappings}
            for prop,conversion,index in common:
                index=int(index)
                if prop not in assigned and prop in obj and row["fields"][index].strip() not in ("0", "", "-1") and obj[prop] != int(row["fields"][index]):
                    common_omissions.append(dict(itemId=obj["id"],kind=kind,property=prop,sourceFieldIndex=index,
                                                 rawValue=row["rawFieldTokens"][index],parserDefault=obj[prop],source=path))
        for prop,conversion,index in mappings:
            index=int(index)
            if prop not in obj or index >= row["fieldCount"]: continue
            token=row["fields"][index].strip()
            if prop=="version": token=token.lstrip("v")
            if conversion in ("GetInt","GetFloat"): expected=float(token) if conversion=="GetFloat" else int(token)
            elif conversion=="GetBool": expected=int(token)!=0
            else:
                original_token=row["rawFieldTokens"][index].strip()
                expected=original_token.replace('"',' ').strip() if conversion=="GetQuotedString" else original_token
            value_checks.append(dict(itemId=obj["id"],property=prop,fieldIndex=index,raw=row["rawFieldTokens"][index],
                                     parserValue=obj[prop],status="equal" if obj[prop]==expected else "mismatch",source=path))
        for prop,target_domain in {"useAmmoID":"ITM","ammoID":"ITM","requiredItem":"ITM","requiredItemID":"ITM","explodeItem":"ITM","vehicleID":"VEH"}.items():
            if prop in obj:
                match=next((m for m in mappings if m[0]==prop),None)
                field=int(match[2]) if match else None
                link("ITM",obj["id"],prop,target_domain,obj[prop],field,row["fields"][field] if field is not None else None)
        for child in obj.get("childItems",[]): link("ITM",obj["id"],"childItems.id","ITM",child["id"])
        for skill in obj.get("skills",[]): link("ITM",obj["id"],"skills.ID","RPG",skill["ID"])
        indexes=[int(i) for i in re.findall(r'values\[(\d+)\]',text)] + list(range(23))
        for class_name,start in re.findall(r'new (Graphics|Sound)\(ref values, (\d+)\)',text):
            indexes.extend(range(int(start),int(start)+(8 if class_name=="Graphics" else 3)))
        if kind=="MultiUse": indexes.extend(range(97,97+len(obj["childItems"])*6))
        if kind=="SkillItem": indexes.extend(range(31,61))
        if kind=="MultiItem": indexes.extend(range(36,52))
        covered=set(i for i in indexes if i<row["fieldCount"])
        coverage.append(dict(itemId=obj["id"],kind=kind,rawFieldCount=row["fieldCount"],literalOrKnownHelperFields=len(covered),
                             fieldsNotStaticallyAccountedFor=[i for i in range(row["fieldCount"]) if i not in covered],
                             limitation="Literal/helper indexing evidence only; does not establish evaluator correctness or prove every hole is ignored."))
    for v in parsed["VEH"]:
        for prop in ("PickupItemId","ExplodeItemId","DropItemId"):
            if prop in v: link("VEH",v["Id"],prop,"ITM",v[prop])
        for item in v.get("InventoryItems",[]): link("VEH",v["Id"],"InventoryItems","ITM",item)
        for child in v.get("ChildVehicles",[]): link("VEH",v["Id"],"ChildVehicles","VEH",child)
    meta=read_json(reports / "rogue-level-metadata.json")
    level=(CANON/"LVL/Rogue.lvl").read_bytes()
    version,width,height,entities,floors,objects=struct.unpack_from("<6i",level)
    if version<6: raise ValueError("This bounded audit requires LVL >=6")
    if (width,height)!=(meta["Width"],meta["Height"]): raise ValueError("LVL dimensions differ")
    offset=5152+(floors+objects)*64
    layers=[]
    for key in ("RawUnknown0Base64","RawUnknown1Base64","RawPhysicsVisionBase64"):
        layer,offset=rle(level,offset,width*height)
        if layer!=base64.b64decode(meta[key]): raise ValueError(f"LVL layer differs: {key}")
        layers.append(dict(key=key,sha256=sha(layer),length=len(layer)))
    archive_lookup={Path(a["originalPath"]).name.casefold():a for a in inventory["archives"]}
    model_blob_links=[]
    def inspect_blobs(node,path):
        if isinstance(node,list):
            for i,value in enumerate(node): inspect_blobs(value,f"{path}[{i}]")
        elif isinstance(node,dict):
            for name_key,id_key in (("blobName","blobID"),("BlobName","BlobId"),("ParallaxBlobName","ParallaxBlobId")):
                if name_key in node and id_key in node:
                    name,member=node[name_key],node[id_key]
                    if name and name.casefold() not in ("none","nosound"):
                        a=archive_lookup.get((name+".blo").casefold())
                        matches=[m for m in a["members"] if m["objectName"].casefold()==member.casefold() or Path(m["objectName"]).stem.casefold()==member.casefold()] if a else []
                        model_blob_links.append(dict(modelPath=path,archiveToken=name,memberToken=member,
                                                     status="present" if len(matches)==1 else "missing_or_ambiguous",matches=matches))
            for key,value in node.items(): inspect_blobs(value,f"{path}.{key}")
    inspect_blobs(item_exports,"ITM")
    inspect_blobs(read_json(reports/"pristine-vehicles.json"),"VEH")
    inspect_blobs(parsed["RPG"],"RPG")
    inspect_blobs(read_json(reports/"rogue-lio-records.json")["Records"],"LIO")
    map_links=[]
    for group in ("ObjectBlobs","FloorBlobs"):
        for ref in meta[group]:
            archive=archive_lookup.get(ref["FileName"].casefold())
            members=[m for m in archive["members"] if m["objectName"].casefold()==ref["Id"].casefold()] if archive else []
            map_links.append(dict(group=group,**ref,status="present" if len(members)==1 else "missing_or_ambiguous",matches=members))
    cfg=read_json(reports/"pristine-config.json")
    core=[]
    for field in ("lvlFile","vehFile","itmFile","rpgFile","lioFile","nwsFile"):
        filename=cfg["level"][field]
        path=next(p for p in hashes if Path(p).name==filename)
        if inputs["Inputs"][filename]!=hashes[path]["sha256"]: raise ValueError("Parser input hash mismatch")
        core.append(path)
    expected=set(core+["CFG/rogue.cfg"])
    for dep in inventory["dependencies"]:
        expected.update(p for p in dep["candidatePaths"] if p.lower().endswith(".blo"))
    for ref in map_links:
        archive=archive_lookup.get(ref["FileName"].casefold())
        if archive: expected.add(archive["originalPath"])
    transfer=[]
    for path in sorted(expected):
        data=(CANON/path).read_bytes()
        compressed=zlib.compress(data)
        if zlib.decompress(compressed)!=data: raise ValueError("Offline zlib roundtrip changed bytes")
        transfer.append(dict(originalPath=path,filename=Path(path).name,sha256=sha(data),crc32=zlib.crc32(data)&0xffffffff,
                             length=len(data),offlineZlibRoundtrip=True))
    evidence_paths=["ZoneServer/Game/Assets/AssetManager.cs","ZoneServer/Game/Assets/AssetManager.Cache.cs",
                    "ZoneServer/Logic/Packets/Login/Assets.cs","ZoneServer/Protocol/Packets/Login/SC_AssetInfo.cs",
                    "Assets/AssetFiles/Checksum.cs","Assets/Lvl/LvlInfo.cs"]
    for path,info in hashes.items():
        if sha((CANON/path).read_bytes())!=info["sha256"]: raise ValueError("Original changed during audit")
    return dict(schemaVersion=1,parserReportDirectory=str(reports.resolve()),parserEvidenceKind="pristine" if reports.resolve()==REPORTS.resolve() else "derived_fixed",canonicalCommit=raw["canonicalSource"]["commit"],serverCommit=SERVER_COMMIT,
                summary=dict(identityCounts={k:v["rawCount"] for k,v in identity.items()},hullLinks=len(hulls),
                             directItemValueChecks=len(value_checks),itemValueMismatches=sum(c["status"]=="mismatch" for c in value_checks),
                             nonzeroCommonFieldOmissions=len(common_omissions),
                             relationships=len(links),missingPositiveRelationships=sum(l["status"]=="missing" for l in links),
                             mapMemberLinks=len(map_links),missingMapMemberLinks=sum(l["status"]!="present" for l in map_links),
                             modeledBlobLinks=len(model_blob_links),missingModeledBlobLinks=sum(l["status"]!="present" for l in model_blob_links),
                             expectedCanonicalTransferFiles=len(transfer),originalHashesBeforeAfter=len(hashes),nativeDeliveryVerified=False),
                identity=identity,hulls=hulls,itemValueChecks=value_checks,itemFieldCoverage=coverage,nonzeroCommonFieldOmissions=common_omissions,relationships=links,
                level=dict(version=version,width=width,height=height,entityCount=entities,floorCount=floors,objectCount=objects,
                           layers=layers,parserConsumedEnd=offset,graphicalTrailerBytes=len(level)-offset,
                           graphicalTrailerSha256=sha(level[offset:]),nativeStride=16),mapMemberLinks=map_links,
                modeledBlobLinks=model_blob_links,expectedTransferFiles=transfer,sourceEvidence={p:sha((SERVER/"dotnetcore"/p).read_bytes()) for p in evidence_paths},
                transferBoundary="Static original source route plus independent CRC/zlib roundtrip; actual server cache/packet/client download remains unverified.",
                runtimeGaps=["Native client must receive entire untouched LVL including graphical trailer; server parser ignores it.",
                             "AssetManager lowercases BLO filenames, but Cache keys are case-sensitive basenames; verify native requests/case locally.",
                             "All expected canonical files plus five separately recovered help files must be staged with unchanged values.",
                             "Positive present ID links do not establish negative/zero sentinel evaluator semantics.",
                             "Item field indexing coverage is bounded static evidence; VEH version parser/evaluator end coverage is not fully audited."])

class Tests(unittest.TestCase):
    def test_rle_bounds(self):
        self.assertEqual(rle(b'\x03A',0,3),(b'AAA',2))
        self.assertEqual(rle(b'\0\0\x03A',0,3),(b'AAA',4))
        for value in (b'',b'\x04A',b'\0\0',b'\0\0\0A'):
            with self.assertRaises(ValueError): rle(value,0,3)
    def test_crc_standard_vector(self): self.assertEqual(zlib.crc32(b'123456789'),0xcbf43926)

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test",action="store_true")
    parser.add_argument("--parser-reports",type=Path,default=REPORTS)
    parser.add_argument("--output",type=Path,default=ROOT/"artifacts/rogue-package-audit/index.json")
    args=parser.parse_args()
    if args.self_test:
        return 0 if unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(Tests)).wasSuccessful() else 1
    output=args.output.resolve()
    if (output.is_relative_to(CANON.resolve()) or output.is_relative_to(SERVER.resolve())
        or output.is_relative_to(REPORTS.resolve())
        or output in ((ROOT/"artifacts/research/original-content/index.json").resolve(),
                      (ROOT/"artifacts/original-dependencies/index.json").resolve())
        or output.suffix!=".json"): raise ValueError("Unsafe derived output")
    report=audit(args.parser_reports)
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print(json.dumps(report["summary"],indent=2)); print(f"ROGUE_PACKAGE_AUDIT_PASS: {output}")
    return 0

if __name__=="__main__":
    try: sys.exit(main())
    except (ValueError,OSError,KeyError,subprocess.SubprocessError) as exc:
        print(f"ROGUE_PACKAGE_AUDIT_FAIL: {exc}",file=sys.stderr); sys.exit(1)
