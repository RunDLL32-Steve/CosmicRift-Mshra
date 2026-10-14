#!/usr/bin/env python3
"""Verify the derived VehicleMaker field18 fix changes exactly eight original values."""
import argparse, copy, hashlib, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
IDS={45,46,47,48,49,83,84,85}
def read(path): return json.loads(path.read_text(encoding="utf-8-sig"))
def sha(data): return hashlib.sha256(data).hexdigest()
def canonical(value): return json.dumps(value,sort_keys=True,separators=(",",":"))
def check(baseline,fixed,output):
    if baseline.resolve()==fixed.resolve(): raise ValueError("Fixed exports must be separate from pristine evidence")
    original=ROOT/".tools/references/cosmic-rift-assets"
    if output.resolve().is_relative_to(original.resolve()) or output.resolve().is_relative_to(baseline.resolve()) or output.resolve().is_relative_to(fixed.resolve()):
        raise ValueError("Output must remain separate from originals/evidence")
    raw=read(ROOT/"artifacts/research/original-content/index.json")
    rows={int(r["identifier"]):r for r in raw["records"] if r["sourcePath"].startswith("ITM/")}
    before=read(baseline/"pristine-items.json")
    after=read(fixed/"pristine-items.json")
    expected=copy.deepcopy(before)
    changes=[]
    for item in expected:
        obj=item["Record"]
        if obj["id"] in IDS:
            row=rows[obj["id"]]
            if item["Kind"]!="VehicleMaker" or obj["prizeBountyPoints"]!=0 or int(row["fields"][18])!=1:
                raise ValueError("Pristine expected omission/raw value changed")
            obj["prizeBountyPoints"]=int(row["fields"][18])
            changes.append(dict(itemId=obj["id"],row=row["row"],version=row["version"],fieldIndex=18,
                                rawFieldToken=row["rawFieldTokens"][18],before=0,after=obj["prizeBountyPoints"]))
    if {c["itemId"] for c in changes}!=IDS: raise ValueError("Expected all eight original VehicleMaker items")
    if canonical(after)!=canonical(expected): raise ValueError("Fixed item exports differ beyond eight allowed field18 changes")
    unchanged=[]
    for name in ("pristine-vehicles.json","pristine-skills.json","rogue-lio-records.json","rogue-level-metadata.json","pristine-config.json"):
        if canonical(read(baseline/name))!=canonical(read(fixed/name)): raise ValueError(f"Unexpected changed export: {name}")
        unchanged.append(name)
    hashes=read(ROOT/"artifacts/original-dependencies/index.json")["sourceIntegrityBeforeAfter"]
    for path,entry in hashes.items():
        data=(original/path).read_bytes()
        if sha(data)!=entry["sha256"] or len(data)!=entry["sizeBytes"]: raise ValueError(f"Changed canonical original: {path}")
    result=dict(feature="RT-F385",status="verified",allowedChanges=changes,
                originalItemCount=len(after),allOtherItemFieldsUnchanged=True,unchangedExports=unchanged,
                canonicalFileHashesVerified=len(hashes),baselineItemsSha256=sha((baseline/"pristine-items.json").read_bytes()),
                fixedItemsSha256=sha((fixed/"pristine-items.json").read_bytes()),runtimeParityVerified=False)
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    print(f"VEHICLEMAKER_FIX_PASS: {len(changes)} exact field18 changes; all other200-item fields + five other exports unchanged; {len(hashes)} canonical hashes unchanged.")
    return 0
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("--baseline",type=Path,default=ROOT/".tools/compatibility/reports")
    p.add_argument("--fixed",type=Path,default=ROOT/".tools/compatibility/reports/vehiclemaker-fixed")
    p.add_argument("--output",type=Path,default=ROOT/"artifacts/rogue-package-audit/vehiclemaker-fix.json")
    args=p.parse_args();return check(args.baseline,args.fixed,args.output)
if __name__=="__main__":
    try:sys.exit(main())
    except (ValueError,OSError,KeyError) as exc:
        print(f"VEHICLEMAKER_FIX_FAIL: {exc}",file=sys.stderr);sys.exit(1)
