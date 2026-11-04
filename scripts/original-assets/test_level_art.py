"""Synthetic tests for the Rogue map art reader; no original bytes are embedded."""
import importlib.util
from pathlib import Path
import struct
import sys
import unittest

sys.path.insert(0, str(Path(__file__).parent))
spec = importlib.util.spec_from_file_location("stage_level_art", Path(__file__).with_name("stage_level_art.py"))
level_art = importlib.util.module_from_spec(spec)
spec.loader.exec_module(level_art)


def lvl_fixture(entities, objects=(("O_Asteroids.blo", "RockA.cfs"), ("o_crmisc.blo", "safezone.cfs")), floors=(("f_default.blo", "default.cfs"),)):
    header = bytearray(level_art.HEADER_SIZE)
    struct.pack_into("<6i", header, 0, 6, 2, 2, len(entities), len(floors), len(objects))
    refs = b"".join(a.encode().ljust(32, b"\0") + m.encode().ljust(32, b"\0") for a, m in list(objects) + list(floors))
    planes = bytes([4, 0]) * 3  # one RLE run of four zero tiles per plane
    table = b""
    for x, y, word, animation, phase in entities:
        record = bytearray(level_art.ENTITY_SIZE)
        struct.pack_into("<hhHH", record, 0, x, y, word, animation)
        record[14] = phase
        table += bytes(record)
    return bytes(header) + refs + planes + table


class LevelArtTests(unittest.TestCase):
    def test_entities_read_position_object_frame_and_animation(self):
        objects, floors, entities = level_art.parse_level(lvl_fixture([(16, 48, 0, 4000, 25), (304, 16, 0x6001, 0, 0)]))
        self.assertEqual(objects[1], ("o_crmisc.blo", "safezone.cfs"))
        self.assertEqual(floors, [("f_default.blo", "default.cfs")])
        self.assertEqual(entities[0], {"x": 16, "y": 48, "object": 0, "animationTime": 4000, "phase": 25, "frame": 0})
        self.assertEqual(entities[1]["object"], 1)
        self.assertEqual(entities[1]["frame"], 3)

    def test_unknown_object_is_refused(self):
        with self.assertRaises(ValueError):
            level_art.parse_level(lvl_fixture([(0, 0, 2, 0, 0)]))

    def test_unsupported_version_is_refused(self):
        data = bytearray(lvl_fixture([]))
        struct.pack_into("<i", data, 0, 5)
        with self.assertRaises(ValueError):
            level_art.parse_level(bytes(data))

    def test_mineral_prize_graphics_come_from_item_fields_23_and_24(self):
        rows = [
            '4,v57,2003,  "Tellerium","Resources","","d",  50000,0,0,1,0,  1,0,0,0,0,4259584,1,  0,15,0,0,Resources,ResourceA,0,0',
            '4,v57,2005,  "Krandazite","Resources","","d",  100000,0,0,1,0,  1,0,0,0,0,33023,1,  0,17,0,0,Resources,ResourceB,0,0',
            '4,v57,2019,  "Iron Ore","Resources","","d",  100000,0,0,1,0,  1,0,0,0,0,255,1,  0,20,0,0,Resources,IronOre,0,0',
        ]
        found = level_art.mineral_graphics("\n".join(rows))
        self.assertEqual(found["2003"], ("Tellerium", "Resources", "ResourceA"))
        self.assertEqual(found["2019"], ("Iron Ore", "Resources", "IronOre"))
        with self.assertRaises(ValueError):
            level_art.mineral_graphics(rows[0])

    def test_pinned_blo_table_is_complete(self):
        pins = level_art.pinned_blo_hashes()
        self.assertEqual(pins["BLO/O_SpaceStation.blo"][0], 361066)


if __name__ == "__main__":
    unittest.main()
