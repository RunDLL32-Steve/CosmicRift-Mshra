"""Parser and original-pixel tests; no engine/world simulation is involved."""
import importlib.util
from pathlib import Path
import struct
import tempfile
import unittest

spec=importlib.util.spec_from_file_location("decode_blo",Path(__file__).with_name("decode_blo.py"))
decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)


def cfs_fixture(pixels=b"\x03\x02\x12\x0b\x16\x21\x21",signed_width=4,signed_height=2,flags=0):
    header=bytearray(127)
    struct.pack_into("<9HBBI",header,0,1,100,4,2,1,1,0,0,0,flags,255,len(pixels))
    palette=struct.pack("<256I",*[index*0x010101 for index in range(256)])
    directory=struct.pack("<HHhhI",0,0,signed_width,signed_height,0)
    return struct.pack("<H",4)+header+palette+directory+pixels


class DecoderTests(unittest.TestCase):
    def test_rle_skip_and_literal_pixels_preserved(self):
        metadata,image=decoder.decode_cfs(cfs_fixture())
        self.assertEqual(image.tobytes(),bytes([0,11,22,0,0,0,33,0]))
        self.assertEqual(metadata["paletteRawU32"][22],0x161616)
        self.assertEqual(image.getpalette()[66:69],[22,22,22])
        self.assertEqual(image.info["transparency"][0],0)
        self.assertTrue(all(alpha==255 for alpha in image.info["transparency"][1:]))

    def test_signed_dimensions_reflect_pixels(self):
        _,horizontal=decoder.decode_cfs(cfs_fixture(signed_width=-4))
        _,vertical=decoder.decode_cfs(cfs_fixture(signed_height=-2))
        self.assertEqual(horizontal.tobytes(),bytes([0,22,11,0,0,33,0,0]))
        self.assertEqual(vertical.tobytes(),bytes([0,0,33,0,0,11,22,0]))

    def test_uncompressed_indexed_pixels(self):
        _,image=decoder.decode_cfs(cfs_fixture(bytes(range(8)),flags=128))
        self.assertEqual(image.tobytes(),bytes(range(8)))

    def test_long_scanline_lengths(self):
        _,image=decoder.decode_cfs(cfs_fixture(b"\xff\x03\x00\x02\x12\x0b\x16\x21\x21"))
        self.assertEqual(image.tobytes(),bytes([0,11,22,0,0,0,33,0]))

    def test_reject_truncated_header_palette_directory_or_pixels(self):
        data=cfs_fixture()
        for end in (0,1,14,127,129,500,1154,len(data)-1):
            with self.subTest(end=end),self.assertRaises(decoder.FormatError):
                decoder.decode_cfs(data[:end])

    def test_reject_rle_scanline_overflow_or_missing_literals(self):
        for payload in (b"\x01\x00\xf0",b"\x01\x00\x05",b"\x02\x00\x03\x01"):
            with self.subTest(payload=payload),self.assertRaises(decoder.FormatError):
                decoder.decode_cfs(cfs_fixture(payload))

    def test_reject_invalid_layout_flags_trailing_bytes(self):
        for change in ("count","crop","offset","flags","trailing"):
            data=bytearray(cfs_fixture())
            if change=="count":struct.pack_into("<H",data,2,2)
            elif change=="crop":struct.pack_into("<H",data,1153,5)
            elif change=="offset":struct.pack_into("<I",data,1161,999999)
            elif change=="flags":data[20]=129
            elif change=="trailing":data+=b"x"
            with self.subTest(change=change),self.assertRaises(decoder.FormatError):decoder.decode_cfs(bytes(data))

    def test_blo_versions_and_payload_identity(self):
        for version,name_length in ((1,14),(2,32)):
            payload=b"abc";offset=8+name_length+8
            data=struct.pack("<II",version,1)+b"test.cfs".ljust(name_length,b"\0")+struct.pack("<II",offset,len(payload))+payload
            parsed,entries=decoder.parse_blo(data)
            self.assertEqual(parsed,version)
            self.assertEqual(entries[0]["objectName"],"test.cfs")
            self.assertEqual(entries[0]["sha256"],decoder.digest(payload))
            for bad in (data[:6],data[:-1],struct.pack("<I",9)+data[4:]):
                with self.assertRaises(decoder.FormatError):decoder.parse_blo(bad)

    def test_reject_blo_table_overlap_duplicate_and_unsafe_names(self):
        for name,offset in ((b"test.cfs",1),(b"../bad.cfs",48)):
            data=struct.pack("<II",2,1)+name.ljust(32,b"\0")+struct.pack("<II",offset,1)+b"x"
            with self.assertRaises(decoder.FormatError):decoder.parse_blo(data)
        row=b"test.cfs".ljust(32,b"\0")+struct.pack("<II",88,1)
        with self.assertRaises(decoder.FormatError):decoder.parse_blo(struct.pack("<II",2,2)+row+row+b"x")

    def test_output_guards(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory).resolve();source=root/"original";source.mkdir();out=root/"derived";out.mkdir()
            with self.assertRaises(decoder.FormatError):decoder.output_root_checked(source,source/"generated")
            with self.assertRaises(decoder.FormatError):decoder.safe_output_path(out,"../original/no.png")
            self.assertEqual(decoder.safe_output_path(out,"ships/Asp.png"),out/"ships/Asp.png")


if __name__=="__main__":unittest.main()
