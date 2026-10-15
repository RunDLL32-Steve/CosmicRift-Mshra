"""Headless fixtures for inspect-native-start.py; no native files or registry needed."""
import importlib.util
from pathlib import Path
import struct
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("native_start", Path(__file__).with_name("inspect-native-start.py"))
native = importlib.util.module_from_spec(spec)
spec.loader.exec_module(native)


def text(value):
    return value.encode("utf-16le") + b"\0\0"


def dialog(extended=False, disabled=False):
    style = 0x40  # DS_SETFONT exercises both font formats.
    if extended:
        data = bytearray(struct.pack("<HHIIIHhhhh", 1, 0xFFFF, 0, 0, style, 1, 0, 0, 304, 250))
    else:
        data = bytearray(struct.pack("<IIHhhhh", style, 0, 1, 0, 0, 304, 250))
    data += b"\0\0\0\0" + text(native.WELCOME_CAPTION)
    data += struct.pack("<H", 8)
    if extended:
        data += struct.pack("<HBB", 400, 0, 1)
    data += text("MS Sans Serif")
    data += bytes((-len(data)) % 4)
    control_style = 0x50010000 | (0x08000000 if disabled else 0)
    if extended:
        data += struct.pack("<IIIhhhhI", 0, 0, control_style, 43, 229, 66, 14, 1)
    else:
        data += struct.pack("<IIhhhhH", control_style, 0, 43, 229, 66, 14, 1)
    data += struct.pack("<HH", 0xFFFF, 0x80) + text(native.START_CAPTION) + b"\0\0"
    return bytes(data)


def pe_file(template=None):
    template = dialog() if template is None else template
    resource = bytearray(88) + template
    for offset in (0, 24, 48):
        struct.pack_into("<HH", resource, offset + 12, 0, 1)
    struct.pack_into("<II", resource, 16, 5, 0x80000000 | 24)
    struct.pack_into("<II", resource, 40, 102, 0x80000000 | 48)
    struct.pack_into("<II", resource, 64, 1033, 72)
    struct.pack_into("<IIII", resource, 72, 0x1000 + 88, len(template), 0, 0)
    data = bytearray(0x200) + resource
    data[:2] = b"MZ"
    struct.pack_into("<I", data, 60, 0x80)
    data[0x80:0x84] = b"PE\0\0"
    struct.pack_into("<HH", data, 0x84, 0x14C, 1)
    struct.pack_into("<H", data, 0x94, 224)
    struct.pack_into("<H", data, 0x98, 0x10B)
    struct.pack_into("<II", data, 0x98 + 112, 0x1000, len(resource))
    section = 0x98 + 224
    data[section:section + 8] = b".rsrc\0\0\0"
    struct.pack_into("<IIII", data, section + 8, len(resource), 0x1000, len(resource), 0x200)
    return bytes(data)


class NativeStartTests(unittest.TestCase):
    def test_standard_dialog_reads_actual_command_and_enabled_state(self):
        result = native.PE(pe_file()).dialog_resources()[0]
        self.assertEqual((result["resource_id"], result["language_id"]), (102, 1033))
        self.assertEqual(result["controls"][0]["id"], 1)
        self.assertFalse(result["controls"][0]["disabled_in_template"])

    def test_extended_dialog_and_disabled_button_are_not_misread(self):
        result = native.PE(pe_file(dialog(extended=True, disabled=True))).dialog_resources()[0]
        self.assertEqual(result["caption"], native.WELCOME_CAPTION)
        self.assertTrue(result["controls"][0]["disabled_in_template"])

    def test_truncated_dialog_rejected(self):
        with self.assertRaises(ValueError):
            native.PE(pe_file(dialog()[:-3])).dialog_resources()

    def test_resource_cycle_rejected(self):
        data = bytearray(pe_file())
        struct.pack_into("<I", data, 0x200 + 20, 0x80000000)
        with self.assertRaisesRegex(ValueError, "Cyclic"):
            native.PE(bytes(data)).dialog_resources()

    def test_unbacked_resource_rva_rejected(self):
        data = bytearray(pe_file())
        struct.pack_into("<I", data, 0x200 + 72, 0xFFFFFFF0)
        with self.assertRaisesRegex(ValueError, "RVA"):
            native.PE(bytes(data)).dialog_resources()

    def test_non_pe_and_truncated_section_are_rejected(self):
        for data in (b"not an engine", pe_file()[:400]):
            with self.subTest(length=len(data)), self.assertRaises(ValueError):
                native.PE(data)

    def test_warning_presence_is_explicitly_not_execution_proof(self):
        result = native.inspect_engine(pe_file() + native.LAUNCH_WARNING)
        self.assertTrue(result["launch_warning_present"])
        self.assertFalse(result["string_presence_proves_executed_control_flow"])

    def test_namespace_guard_rejects_non_namespace_changes(self):
        original = bytearray(b"x" * 100)
        for offset in (10, 40):
            original[offset:offset + 13] = native.ORIGINAL_NAMESPACE
        candidate = bytearray(original)
        for offset in (10, 40):
            candidate[offset:offset + 13] = b"RTTest123abcd"
        with patch.object(native, "NAMESPACE_OFFSETS", (10, 40)), patch.object(native, "INSTALLER_SHA256", native.digest(original)):
            self.assertTrue(native.inspect_namespace(bytes(original), bytes(candidate))["verified_derivation"])
            candidate[80] ^= 1
            self.assertFalse(native.inspect_namespace(bytes(original), bytes(candidate))["verified_derivation"])

    def test_unknown_engine_cannot_reuse_pinned_namespace_offsets(self):
        self.assertFalse(native.inspect_namespace(b"different build", b"different build")["verified_derivation"])

    def test_zone_list_requires_single_exact_loopback_endpoint(self):
        valid = '"Local","127.0.0.1",21337,1,0,"Description, with comma",50,0\n'
        self.assertTrue(native.inspect_zone_list(valid, 21337)["valid"])
        for invalid in (valid + valid, valid.replace("127.0.0.1", "localhost"),
                        valid.replace("127.0.0.1", "203.0.113.1"), valid.replace("21337", "1337"),
                        valid.replace("127.0.0.1", "127.0.0.2"), '"Local","127.0.0.1",21337\n', ""):
            with self.subTest(value=invalid):
                self.assertFalse(native.inspect_zone_list(invalid, 21337)["valid"])

    def test_manifest_compares_source_content_and_requires_unique_entry(self):
        with tempfile.TemporaryDirectory() as temporary:
            manifest = Path(temporary) / "manifest.xml"
            source = b"original engine bytes"
            entry = f'<File Name="infantry.exe" MD5="{native.digest(source, "md5")}" Size="{len(source)}"/>'
            manifest.write_text(f"<FIPatcher>{entry}</FIPatcher>")
            self.assertTrue(native.manifest_identity(manifest, source)["matches_source"])
            self.assertFalse(native.manifest_identity(manifest, source + b"changed")["matches_source"])
            manifest.write_text(f"<FIPatcher>{entry}{entry}</FIPatcher>")
            self.assertFalse(native.manifest_identity(manifest, source)["matches_source"])

    def test_preflight_never_claims_start_fixed_and_checks_wrapper_overrides(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = pe_file()
            (root / "infantry.exe").write_bytes(source)
            (root / "Infantry.lst").write_text('"Local","127.0.0.1",21337,1,0,"Test",50,0')
            for name in ("default.ini", "settings.ini"):
                (root / name).write_text("[Launcher]\nAccounts=\n[Credentials]\nUsername=\nPassword=\n")
            (root / "ddraw.ini").write_text("[ddraw]\nwindowed=true\ndevmode=true\nsavesettings=0\n[infantry]\ndevmode=false\n")
            result = native.preflight(root, root / "infantry.exe", [], None, 21337)
            self.assertFalse(result["native_start_fixed"])
            self.assertFalse(result["native_entry_verified"])
            self.assertTrue(any("devmode" in reason for reason in result["blockers"]))
            self.assertFalse(result["static_preparation_checks_pass"])

    def test_launcher_setting_report_does_not_expose_values(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for name in ("default.ini", "settings.ini"):
                (root / name).write_text("[Launcher]\nAccounts=https://private.invalid\n[Credentials]\nUsername=private-alias\nPassword=private-secret\n")
            result = native.inspect_launcher_settings(root)
            self.assertTrue(result["settings.ini"]["credential_values_present"])
            self.assertEqual(result["settings.ini"]["nonempty_launcher_fields"], ["accounts"])
            self.assertNotIn("private", str(result))


if __name__ == "__main__":
    unittest.main(verbosity=2)
