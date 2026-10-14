"""Focused staging checks against independently inspectable original-byte fixtures."""
import copy
import importlib.util
import json
from pathlib import Path
import struct
import tempfile
import unittest
from PIL import Image
import decode_blo
from test_decoder import cfs_fixture
import stage_original_assets as staging


class StagingTests(unittest.TestCase):
    def make_fixture(self,root):
        source=root/"originals";generated=root/"generated";out=root/"staged"
        (source/"BLO").mkdir(parents=True);(source/"VEH").mkdir();(source/"LVL-BMP").mkdir();(generated/"TerranShipsSmall").mkdir(parents=True)
        cfs=cfs_fixture();blo=struct.pack("<II",2,1)+b"Asp.cfs".ljust(32,b"\0")+struct.pack("<II",48,len(cfs))+cfs
        (source/"BLO/TerranShipsSmall.blo").write_bytes(blo)
        fields=["0"]*400;fields[:4]=["2","v53","2002","Asp"];fields[382:384]=["TerranShipsSmall","Asp"]
        (source/"VEH/Rogue.veh").write_text(",".join(fields)+"\n",encoding="utf-8")
        map_image=Image.new("RGB",(2,1));map_image.putdata([(1,2,3),(250,100,50)]);map_image.save(source/"LVL-BMP/Rogue.bmp")
        metadata,atlas=decode_blo.decode_cfs(cfs);png=generated/"TerranShipsSmall/Asp.png";atlas.save(png)
        sprite={key:metadata[key] for key in ("cfsVersion","width","height","rowCount","columnCount","frameCount","animationTime","rowMeaning","blitMode","lightCount","shadowCount","compressionFlags","transparentIndex")}
        sprite.update(objectName="Asp.cfs",objectId=0,sourcePath="BLO/TerranShipsSmall.blo",sourceSha256=staging.sha(blo),assetKey="BLO/TerranShipsSmall.blo::Asp.cfs",atlasPng="TerranShipsSmall/Asp.png",pngSha256=staging.sha(png.read_bytes()))
        ship={"sourcePath":"VEH/Rogue.veh","sourceSha256":staging.sha((source/"VEH/Rogue.veh").read_bytes()),"sourceRow":1,"vehicleId":"2002","name":"Asp","archiveToken":"TerranShipsSmall","memberToken":"Asp","assetKey":sprite["assetKey"]}
        runtime={"canonicalCommit":staging.COMMIT,"sprites":[sprite],"ships":[ship]}
        (generated/"runtime.json").write_text(json.dumps(runtime))
        assets=[{"originalPath":relative,"commit":staging.COMMIT,"originalSha256":staging.sha((source/relative).read_bytes()),"originalSizeBytes":(source/relative).stat().st_size} for relative in ("BLO/TerranShipsSmall.blo","VEH/Rogue.veh","LVL-BMP/Rogue.bmp")]
        manifest=root/"manifest.json";manifest.write_text(json.dumps({"canonicalSource":{"commit":staging.COMMIT},"assets":assets}))
        return source,generated,out,manifest,runtime

    def test_stage_preserves_sprite_bytes_map_pixels_and_sources(self):
        with tempfile.TemporaryDirectory() as directory:
            source,generated,out,manifest,runtime=self.make_fixture(Path(directory))
            original={path.relative_to(source):path.read_bytes() for path in source.rglob("*") if path.is_file()}
            runtime_before=(generated/"runtime.json").read_bytes()
            result=staging.stage(generated,source,manifest,out)
            self.assertEqual((generated/"TerranShipsSmall/Asp.png").read_bytes(),(out/"TerranShipsSmall/Asp.png").read_bytes())
            with Image.open(out/"Rogue.png") as image:self.assertEqual(list(image.getdata()),[(1,2,3),(250,100,50)])
            self.assertEqual(result["map"]["width"],2);self.assertEqual(result["map"]["height"],1)
            self.assertTrue(result["staging"]["canonicalCfsPixelsAndPaletteVerified"])
            self.assertEqual(runtime_before,(generated/"runtime.json").read_bytes())
            self.assertEqual(original,{path.relative_to(source):path.read_bytes() for path in source.rglob("*") if path.is_file()})

    def test_reject_changed_generated_pixels_even_with_updated_png_hash(self):
        with tempfile.TemporaryDirectory() as directory:
            source,generated,out,manifest,runtime=self.make_fixture(Path(directory));png=generated/"TerranShipsSmall/Asp.png"
            with Image.open(png) as image:
                image.load();image.putpixel((1,0),99);image.save(png)
            runtime["sprites"][0]["pngSha256"]=staging.sha(png.read_bytes());(generated/"runtime.json").write_text(json.dumps(runtime))
            with self.assertRaisesRegex(ValueError,"pixels/palette/transparency differ"):staging.stage(generated,source,manifest,out)

    def test_reject_modified_originals_and_unresolved_member_ids(self):
        with tempfile.TemporaryDirectory() as directory:
            source,generated,out,manifest,runtime=self.make_fixture(Path(directory))
            original=source/"BLO/TerranShipsSmall.blo";before=original.read_bytes();original.write_bytes(before+b"x")
            with self.assertRaisesRegex(ValueError,"original hash/size mismatch"):staging.stage(generated,source,manifest,out)
            original.write_bytes(before);runtime["sprites"][0]["objectId"]=7;(generated/"runtime.json").write_text(json.dumps(runtime))
            with self.assertRaisesRegex(ValueError,"member/table identity"):staging.stage(generated,source,manifest,out)

    def test_output_root_and_child_guards(self):
        with tempfile.TemporaryDirectory() as directory:
            source,generated,out,manifest,runtime=self.make_fixture(Path(directory))
            with self.assertRaisesRegex(ValueError,"outside originals"):staging.stage(generated,source,manifest,source/"bad")
            with self.assertRaisesRegex(ValueError,"separate from generated"):staging.stage(generated,source,manifest,generated)
            with self.assertRaises(ValueError):staging.child(out,"../originals/bad.png")


if __name__=="__main__":unittest.main()
