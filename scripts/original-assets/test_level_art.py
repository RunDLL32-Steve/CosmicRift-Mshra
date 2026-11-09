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
    table = b"".join(struct.pack("<hhIIIB", x, y, *bits) for x, y, *bits in
        ((e + (0, 0, 0, 0))[:6] for e in entities))
    return bytes(header) + refs + planes + table


class LevelArtTests(unittest.TestCase):
    def test_entities_read_position_object_frame_and_animation(self):
        # Rock: AnimationTime 250 at BitsA 20-29, TimeDelta 5; safe-zone marking: object 1, FrameIndex 3, still.
        objects, floors, entities = level_art.parse_level(lvl_fixture([(16, 48, 250 << 20, 5), (304, 16, 1 | 3 << 13)]))
        self.assertEqual(objects[1], ("o_crmisc.blo", "safezone.cfs"))
        self.assertEqual(floors, [("f_default.blo", "default.cfs")])
        self.assertEqual(entities[0], {"x": 16, "y": 48, "object": 0, "frame": 0, "animationTime": 250, "timeDelta": 5,
            "drawOrder": 0, "translucency": 0, "smartTranslucency": 0, "value": 0, "saturation": 0, "hue": 0, "light": 0,
            "paletteShift": 0})
        self.assertEqual((entities[1]["object"], entities[1]["frame"], entities[1]["animationTime"]), (1, 3, 0))

    def test_frame_index_uses_all_seven_bits(self):
        _, _, entities = level_art.parse_level(lvl_fixture([(0, 0, 0x7F << 13 | 1)]))
        self.assertEqual((entities[0]["object"], entities[0]["frame"], entities[0]["animationTime"]), (1, 127, 0))

    def test_entities_read_draw_order_and_translucency_flags(self):
        # Gas cloud record: BitsB = 0x94000000 (DrawOrder 5, SmartTransluency); BitsA top bits = Transluency 50%.
        _, _, entities = level_art.parse_level(lvl_fixture([(0, 0, 0x40000000 | 150 << 20, 0x94000000), (0, 0, 0, 0x10000000)]))
        self.assertEqual((entities[0]["drawOrder"], entities[0]["translucency"], entities[0]["smartTranslucency"]), (5, 1, 1))
        self.assertEqual(entities[0]["animationTime"], 150)
        self.assertEqual((entities[1]["drawOrder"], entities[1]["translucency"], entities[1]["smartTranslucency"]), (4, 0, 0))

    def test_entities_read_value_saturation_hue_light_and_palette_shift(self):
        # Value -10 (0xF6), Saturation -3 (0xFD), Hue 90, light 50; PaletteShift 7 with the unknown top bits set.
        bits_c = 0xF6 | 0xFD << 8 | 90 << 16 | 50 << 23
        _, _, entities = level_art.parse_level(lvl_fixture([(0, 0, 0, 0, bits_c, 0xC7)]))
        e = entities[0]
        self.assertEqual((e["value"], e["saturation"], e["hue"], e["light"], e["paletteShift"]), (-10, -3, 90, 50, 7))

    def test_uncovered_blocked_tiles_counts_walls_under_no_sprite_cell(self):
        physics = bytes([1, 1, 0, 1])  # 2 x 2 tiles; three blocked
        entities = [{"x": 0, "y": 0, "object": 0}]
        self.assertEqual(level_art.uncovered_blocked_tiles(2, 2, physics, entities, [(16, 16)]), 2)
        self.assertEqual(level_art.uncovered_blocked_tiles(2, 2, physics, entities, [(32, 32)]), 0)
        self.assertEqual(level_art.uncovered_blocked_tiles(2, 2, physics, [], [(16, 16)]), 3)

    def test_physics_plane_decodes_the_third_plane(self):
        width, height, physics = level_art.physics_plane(lvl_fixture([]))
        self.assertEqual((width, height, physics), (2, 2, bytes(4)))

    def test_unknown_object_is_refused(self):
        with self.assertRaises(ValueError):
            level_art.parse_level(lvl_fixture([(0, 0, 2)]))

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
