"""Offline native Start preflight. Never launches a process or reads/writes HKCU.

Static success is not native entry, authentication, or profile-isolation acceptance.
Uses only the Python standard library; client/source files are opened read-only.
"""
from __future__ import annotations

import argparse
import configparser
import csv
import hashlib
import io
import ipaddress
import json
from pathlib import Path
import re
import struct
import xml.etree.ElementTree as ET


INSTALLER_SHA256 = "33d97aab9ffad1ff62252cfa0bf1acdf091b71da246460bfe74f55e3e901b1ba"
LIVE_SHA256 = "d8dd7de3233af511388356a3bbbbd4a78298af22d9b2905237f9ddb4daac733e"
NAMESPACE_OFFSETS = (1127917, 1128113)
ORIGINAL_NAMESPACE = b"HarmlessGames"
START_CAPTION = "Start Playing"
WELCOME_CAPTION = "Welcome to FreeInfantry"
LAUNCH_WARNING = b"You need to start the game by using the Station LaunchPad in order to be able to play."


def digest(data: bytes, algorithm: str = "sha256") -> str:
    return hashlib.new(algorithm, data).hexdigest()


class PE:
    """Small bounded PE/resource reader, not an executable loader."""

    def __init__(self, data: bytes):
        self.data = data
        if len(data) < 64 or data[:2] != b"MZ":
            raise ValueError("Not a DOS/PE executable")
        pe = self.unpack("<I", 60)[0]
        if self.take(pe, 4) != b"PE\0\0":
            raise ValueError("Missing PE signature")
        self.machine, count = self.unpack("<HH", pe + 4)
        optional_size = self.unpack("<H", pe + 20)[0]
        optional = pe + 24
        magic = self.unpack("<H", optional)[0]
        if magic not in (0x10B, 0x20B):
            raise ValueError("Unsupported optional-header magic")
        directory_offset = 96 if magic == 0x10B else 112
        if optional_size < directory_offset + 24 or count > 96:
            raise ValueError("Invalid PE optional header/section count")
        self.sections = []
        for index in range(count):
            offset = optional + optional_size + index * 40
            raw = self.take(offset, 40)
            name = raw[:8].rstrip(b"\0").decode("ascii", errors="replace")
            virtual_size, address, raw_size, raw_offset = struct.unpack_from("<IIII", raw, 8)
            self.take(raw_offset, raw_size)
            self.sections.append((name, address, virtual_size, raw_size, raw_offset))
        self.resource_rva, self.resource_size = self.unpack("<II", optional + directory_offset + 16)

    def take(self, offset: int, length: int) -> bytes:
        if offset < 0 or length < 0 or offset + length > len(self.data):
            raise ValueError("PE read exceeds file bounds")
        return self.data[offset:offset + length]

    def unpack(self, fmt: str, offset: int) -> tuple:
        return struct.unpack(fmt, self.take(offset, struct.calcsize(fmt)))

    def file_offset(self, rva: int, length: int) -> int:
        for _, address, _, raw_size, raw_offset in self.sections:
            if address <= rva and rva + length <= address + raw_size:
                return raw_offset + rva - address
        raise ValueError("Resource RVA is not backed by file bytes")

    def dialog_resources(self) -> list[dict]:
        if not self.resource_rva or not self.resource_size:
            return []
        base = self.file_offset(self.resource_rva, self.resource_size)
        resources = []

        def take(relative: int, length: int) -> bytes:
            if relative < 0 or relative + length > self.resource_size:
                raise ValueError("Resource directory exceeds declared bounds")
            return self.take(base + relative, length)

        def walk(relative: int, path: tuple, visited: frozenset):
            if relative in visited or len(path) > 3:
                raise ValueError("Cyclic or over-deep resource directory")
            header = take(relative, 16)
            named, ids = struct.unpack_from("<HH", header, 12)
            if named + ids > 4096:
                raise ValueError("Excessive resource directory entries")
            entries = take(relative + 16, (named + ids) * 8)
            for index in range(named + ids):
                name, target = struct.unpack_from("<II", entries, index * 8)
                # Only RT_DIALOG numeric type 5 is relevant; do not decode unrelated resources.
                if not path and name != 5:
                    continue
                key = name if not name & 0x80000000 else "named"
                next_path = path + (key,)
                if target & 0x80000000:
                    walk(target & 0x7FFFFFFF, next_path, visited | {relative})
                else:
                    if len(next_path) != 3:
                        raise ValueError("Unexpected dialog resource depth")
                    rva, size, _, _ = struct.unpack("<IIII", take(target, 16))
                    if size > 1024 * 1024:
                        raise ValueError("Excessive dialog template size")
                    offset = self.file_offset(rva, size)
                    dialog = parse_dialog(self.take(offset, size))
                    dialog.update(resource_id=next_path[1], language_id=next_path[2])
                    resources.append(dialog)

        walk(0, (), frozenset())
        return resources


def parse_dialog(data: bytes) -> dict:
    """Read standard and DIALOGEX templates, preserving control ID and disabled style."""
    position = 0

    def read(fmt: str):
        nonlocal position
        size = struct.calcsize(fmt)
        if position + size > len(data):
            raise ValueError("Truncated dialog template")
        values = struct.unpack_from(fmt, data, position)
        position += size
        return values

    def field():
        first = read("<H")[0]
        if first == 0:
            return ""
        if first == 0xFFFF:
            return {"ordinal": read("<H")[0]}
        chars = [first]
        while True:
            value = read("<H")[0]
            if value == 0:
                return struct.pack("<" + "H" * len(chars), *chars).decode("utf-16le")
            chars.append(value)

    extended = len(data) >= 4 and data[:4] == b"\x01\0\xff\xff"
    if extended:
        _, _, _, _, style, count, x, y, width, height = read("<HHIIIHhhhh")
    else:
        style, _, count, x, y, width, height = read("<IIHhhhh")
    field()  # menu
    field()  # class
    caption = field()
    if style & 0x40:  # DS_SETFONT
        read("<H")
        if extended:
            read("<HBB")
        field()
    controls = []
    for _ in range(count):
        position = (position + 3) & ~3
        if extended:
            _, _, control_style, cx, cy, cw, ch, control_id = read("<IIIhhhhI")
        else:
            control_style, _, cx, cy, cw, ch, control_id = read("<IIhhhhH")
        control_class, text = field(), field()
        extra = read("<H")[0]
        if extra:
            # creation-data length includes its own WORD size field.
            if extra < 2 or position + extra - 2 > len(data):
                raise ValueError("Invalid dialog creation-data length")
            position += extra - 2
        controls.append({"id": control_id, "class": control_class, "caption": text,
                         "style": hex(control_style), "dialog_units": [cx, cy, cw, ch],
                         "disabled_in_template": bool(control_style & 0x08000000)})
    return {"caption": caption, "dialog_units": [x, y, width, height], "controls": controls}


def inspect_engine(data: bytes) -> dict:
    pe = PE(data)
    sha = digest(data)
    channel = {INSTALLER_SHA256: "preserved current installer member",
               LIVE_SHA256: "separate historical live endpoint member"}.get(sha, "unknown or derived")
    dialogs = [dialog for dialog in pe.dialog_resources() if dialog["caption"] == WELCOME_CAPTION]
    return {"sha256": sha, "md5": digest(data, "md5"), "length": len(data),
            "machine": hex(pe.machine), "known_source_identity": channel,
            "welcome_dialogs": dialogs,
            "launch_warning_present": LAUNCH_WARNING in data,
            "name_argument_literal_present": b"/name:\0" in data,
            "ticket_argument_literal_present": b"/ticket:\0" in data,
            "string_presence_proves_executed_control_flow": False}


def inspect_namespace(source: bytes, candidate: bytes) -> dict:
    if digest(source) != INSTALLER_SHA256:
        return {"verified_derivation": False, "reason": "Namespace adapter supports only the pinned installer engine; do not reuse its offsets for another build."}
    if len(source) != len(candidate):
        return {"verified_derivation": False, "reason": "Candidate length differs from pinned source."}
    names = [candidate[offset:offset + 13] for offset in NAMESPACE_OFFSETS]
    if names[0] != names[1] or not re.fullmatch(rb"RTTest[0-9a-f]{7}", names[0]):
        return {"verified_derivation": False, "reason": "Candidate is missing the two equal private namespace literals."}
    restored = bytearray(candidate)
    for offset in NAMESPACE_OFFSETS:
        restored[offset:offset + 13] = ORIGINAL_NAMESPACE
    verified = bytes(restored) == source
    return {"verified_derivation": verified,
            "reason": "Only the two approved registry data literals differ." if verified else "Additional candidate bytes differ from the pinned source; reject this derivation.",
            "namespace": names[0].decode("ascii"),
            "registry_existence_and_directory_values_checked": False,
            "runtime_profile_isolation_verified": False}


def inspect_zone_list(text: str, port: int) -> dict:
    rows = list(csv.reader(io.StringIO(text), strict=True))
    rows = [row for row in rows if row and any(cell.strip() for cell in row)]
    reasons = []
    if len(rows) != 1:
        reasons.append("Expected exactly one local diagnostic zone entry.")
    for row in rows:
        if len(row) != 8:
            reasons.append("Zone row must contain the eight quick-start fields.")
            continue
        try:
            # Do not resolve hostnames or accept an arbitrary loopback range: host binds 127.0.0.1.
            address = ipaddress.ip_address(row[1].strip())
            if str(address) != "127.0.0.1" or int(row[2]) != port:
                reasons.append("Zone address/port does not match the bounded 127.0.0.1 host.")
        except ValueError:
            reasons.append("Zone address/port is not a literal valid local endpoint.")
    return {"valid": not reasons, "entry_count": len(rows), "reasons": reasons}


def manifest_identity(path: Path, source: bytes) -> dict:
    entries = [entry for entry in ET.parse(path).getroot() if entry.get("Name", "").lower() == "infantry.exe"]
    if len(entries) != 1:
        return {"matches_source": False, "reason": "Manifest has no unique infantry.exe entry."}
    entry = entries[0]
    expected_size = int(entry.attrib["Size"])
    expected_md5 = entry.attrib["MD5"].lower()
    return {"matches_source": expected_size == len(source) and expected_md5 == digest(source, "md5"),
            "expected_size": expected_size, "expected_md5": expected_md5,
            "comparison_target": "unchanged source, not the namespace-derived executable"}


def inspect_launcher_settings(client: Path) -> dict:
    """Return only presence flags, never URLs, usernames, passwords, or token values."""
    result = {}
    for name in ("default.ini", "settings.ini"):
        config = configparser.ConfigParser(interpolation=None, strict=False)
        config.read_string((client / name).read_text(encoding="utf-8-sig"))
        result[name] = {"nonempty_launcher_fields": [key for key, value in config.items("Launcher") if value.strip()],
                        "credential_values_present": any(value.strip() for _, value in config.items("Credentials"))}
    return result


def preflight(client: Path, source_path: Path, comparisons: list[Path], manifest: Path | None, port: int) -> dict:
    source = source_path.read_bytes()
    candidate = (client / "infantry.exe").read_bytes()
    report = {"scope": "offline static native Start diagnostic; no launch, registry access, credentials, or network",
              "native_start_fixed": False, "native_entry_verified": False,
              "authentication_and_profile_runtime_acceptance": "requires human native testing",
              "source": inspect_engine(source), "candidate": inspect_engine(candidate),
              "comparison_engines": [inspect_engine(path.read_bytes()) for path in comparisons],
              "namespace": inspect_namespace(source, candidate),
              "user_launch_context": {"executable": str((client / "infantry.exe").resolve()),
                                      "working_directory": str(client.resolve()),
                                      "arguments": "No synthetic authentication arguments; any required authentication belongs to the user."},
              "blockers": [], "observations": []}
    blockers = report["blockers"]
    if not report["namespace"]["verified_derivation"]:
        blockers.append(report["namespace"]["reason"])
    if not any(control["caption"] == START_CAPTION and not control["disabled_in_template"]
               for dialog in report["candidate"]["welcome_dialogs"] for control in dialog["controls"]):
        blockers.append("No enabled-in-template Welcome Start Playing control was established.")
    # The native executable derives its zone-list basename from game identity. Keep canonical name/CWD.
    list_path = client / "Infantry.lst"
    report["zone_list"] = inspect_zone_list(list_path.read_text(encoding="utf-8-sig"), port)
    blockers.extend(report["zone_list"]["reasons"])
    report["launcher_settings"] = inspect_launcher_settings(client)
    if any(values["nonempty_launcher_fields"] or values["credential_values_present"]
           for values in report["launcher_settings"].values()):
        blockers.append("Prepared launcher INIs contain nonblank launcher/credential values; values are deliberately omitted from this report.")
    report["launcher_executable_present"] = any((client / name).exists() for name in ("InfantryLauncher.exe", "FreeInfantryLauncher.exe"))
    if report["launcher_executable_present"]:
        blockers.append("Prepared local test tree contains a launcher executable; direct-native isolation was not established.")
    wrapper = configparser.ConfigParser(interpolation=None, strict=False)
    wrapper.read_string((client / "ddraw.ini").read_text(encoding="utf-8-sig"))
    sections = [section for section in wrapper.sections() if section.lower() in ("ddraw", "infantry")]
    report["wrapper_sections"] = {section: {key: wrapper[section].get(key) for key in
                                  ("windowed", "devmode", "savesettings") if key in wrapper[section]}
                                  for section in sections}
    for key, expected in (("windowed", "true"), ("devmode", "true"), ("savesettings", "0")):
        values = [wrapper[section][key].strip().lower() for section in sections if key in wrapper[section]]
        if not values or any(value != expected for value in values):
            blockers.append(f"Prepared wrapper {key} must be {expected} in each relevant section that specifies it.")
    if manifest:
        report["installer_manifest"] = manifest_identity(manifest, source)
        if not report["installer_manifest"]["matches_source"]:
            report["observations"].append("Installer manifest does not identify the inspected source bytes; supported engine channel remains unresolved.")
    report["observations"].extend([
        "An enabled resource template does not prove runtime event handling, focus, or click behavior.",
        "LaunchPad/ticket/name string presence does not prove a runtime prerequisite; swapping engine builds is not evidence of a Start fix.",
        "Private namespace bytes do not establish live HKCU existence, blank directory settings, or runtime write coverage.",
        "If Start reveals login/account/LaunchPad/security UI, the user must handle it; no ticket or credential may be synthesized.",
        "The pinned quick-start supports direct native launch with blank directory fields and a local list; server standalone mode alone does not prove native launch authentication semantics.",
    ])
    report["static_preparation_checks_pass"] = not blockers
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--client-directory", type=Path, required=True)
    parser.add_argument("--source-executable", type=Path, required=True)
    parser.add_argument("--comparison-executable", type=Path, action="append", default=[])
    parser.add_argument("--installer-manifest", type=Path)
    parser.add_argument("--port", type=int, default=21337)
    parser.add_argument("--output", type=Path, help="Optional report under this checkout's ignored artifacts/ or .tools/ only")
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("port must be in 1..65535")
    if args.output:
        repo = Path(__file__).resolve().parents[2]
        target = args.output.resolve()
        if not any(target.is_relative_to((repo / folder).resolve()) for folder in ("artifacts", ".tools")):
            parser.error("output must remain in this checkout's ignored artifacts/ or .tools/")
        # Do not allow overwriting an original or client input, even under ignored folders.
        if target.suffix.lower() != ".json" or target.exists():
            parser.error("output must be a new .json report; existing files are never overwritten")
        inputs = [args.source_executable, *args.comparison_executable, args.client_directory]
        if args.installer_manifest:
            inputs.append(args.installer_manifest)
        if any(target == path.resolve() or (path.is_dir() and target.is_relative_to(path.resolve())) for path in inputs):
            parser.error("output cannot be within a client input directory or replace an input")
    try:
        report = preflight(args.client_directory, args.source_executable, args.comparison_executable,
                           args.installer_manifest, args.port)
    except (OSError, ValueError, csv.Error, configparser.Error, ET.ParseError, KeyError, struct.error) as error:
        report = {"static_preparation_checks_pass": False, "native_start_fixed": False,
                  "native_entry_verified": False, "error": str(error)}
    serialized = json.dumps(report, indent=2) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open("x", encoding="utf-8") as stream:
            stream.write(serialized)
    print(serialized, end="")
    return 0 if report["static_preparation_checks_pass"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
