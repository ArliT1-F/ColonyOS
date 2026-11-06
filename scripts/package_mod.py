#!/usr/bin/env python3
"""Create an installable ColonyOS.zip after both versioned assemblies have been built."""
from pathlib import Path
import sys
import zipfile

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "dist" / "ColonyOS.zip"
REQUIRED_FILES = [
    "About/About.xml",
    "About/Preview.png",
    "LoadFolders.xml",
    "README.md",
    "LICENSE",
    "1.6/Assemblies/ColonyOS.dll",
    "1.7/Assemblies/ColonyOS.dll",
]
REQUIRED_TREES = ["Defs", "Languages", "Textures", "docs"]

files = []
for relative in REQUIRED_FILES:
    path = ROOT / relative
    if not path.is_file() or path.stat().st_size == 0:
        print(f"Missing or empty package input: {relative}", file=sys.stderr)
        sys.exit(1)
    files.append(path)
for tree in REQUIRED_TREES:
    files.extend(path for path in (ROOT / tree).rglob("*") if path.is_file())

OUTPUT.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(OUTPUT, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
    for path in sorted(set(files)):
        relative = path.relative_to(ROOT).as_posix()
        archive.write(path, f"ColonyOS/{relative}")

with zipfile.ZipFile(OUTPUT) as archive:
    bad = archive.testzip()
    if bad:
        print(f"Corrupt ZIP member: {bad}", file=sys.stderr)
        sys.exit(1)
    names = set(archive.namelist())
    for relative in REQUIRED_FILES:
        if f"ColonyOS/{relative}" not in names:
            print(f"Package verification omitted {relative}", file=sys.stderr)
            sys.exit(1)
    unexpected_dlls = [name for name in names if name.endswith(".dll") and name not in {
        "ColonyOS/1.6/Assemblies/ColonyOS.dll",
        "ColonyOS/1.7/Assemblies/ColonyOS.dll",
    }]
    if unexpected_dlls:
        print(f"Unexpected bundled DLLs: {unexpected_dlls}", file=sys.stderr)
        sys.exit(1)

print(f"PASS: {OUTPUT.relative_to(ROOT)} ({OUTPUT.stat().st_size:,} bytes, {len(files)} files)")
