"""Synthetic tests for the radar frame staging; no original bytes are embedded."""
import importlib.util
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).parent))
spec = importlib.util.spec_from_file_location("stage_ui_art", Path(__file__).with_name("stage_ui_art.py"))
ui_art = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ui_art)

CFG = "[Radar]\r\nColorSelf=0x00ff00\r\n\r\n[Uiart]\r\nVision=cruiart,vision\r\nRadarBorder=cruiart,newradar\r\n"


class UiArtTests(unittest.TestCase):
    def test_radar_border_reference_reads_archive_member_and_line(self):
        self.assertEqual(ui_art.border_reference(CFG), (6, "cruiart", "newradar"))

    def test_key_in_another_section_is_not_taken(self):
        with self.assertRaises(ValueError):
            ui_art.border_reference("[Radar]\r\nRadarBorder=cruiart,newradar\r\n")

    def test_malformed_reference_is_refused(self):
        for value in ("cruiart", ",newradar", "cruiart,", "a,b,c"):
            with self.assertRaises(ValueError):
                ui_art.border_reference(f"[Uiart]\r\nRadarBorder={value}\r\n")


if __name__ == "__main__":
    unittest.main()
