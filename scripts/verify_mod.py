#!/usr/bin/env python3
"""Repository-only package checks; does not claim in-game validation."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

for path in ROOT.rglob("*.xml"):
    if any(part in {".git", "obj", "bin"} for part in path.parts):
        continue
    try:
        ET.parse(path)
    except ET.ParseError as exc:
        errors.append(f"Invalid XML {path.relative_to(ROOT)}: {exc}")

about = ET.parse(ROOT / "About/About.xml").getroot()
package_id = about.findtext("packageId")
versions = {node.text for node in about.findall("supportedVersions/li")}
if package_id != "ArliT1F.ColonyOS":
    errors.append("Unexpected packageId")
if versions != {"1.6", "1.7"}:
    errors.append(f"Supported versions are incomplete: {versions}")
if not about.findall("modDependencies/li[packageId='brrainz.harmony']"):
    errors.append("Harmony dependency is not declared")

load_folders = (ROOT / "LoadFolders.xml").read_text()
for version in versions:
    if f"<v{version}>" not in load_folders:
        errors.append(f"Missing load folder for {version}")
    if not (ROOT / version / "Assemblies").exists():
        errors.append(f"Missing {version}/Assemblies directory")

main_def = (ROOT / "Defs/MainButtonDefs/ColonyOS.xml").read_text()
if "ColonyOS.UI.ColonyOSWindow" not in main_def:
    errors.append("Main button does not reference the dashboard window")

for source in (ROOT / "Source").rglob("*.cs"):
    text = source.read_text()
    for match in re.finditer(r'Load\("([A-Za-z]+)"\)', text):
        asset = ROOT / "Textures/ColonyOS/UI" / f"{match.group(1)}.png"
        if not asset.exists():
            errors.append(f"Missing UI asset: {asset.relative_to(ROOT)}")

for folder in ("1.6/Assemblies", "1.7/Assemblies"):
    for dll in (ROOT / folder).glob("*.dll"):
        if dll.name != "ColonyOS.dll":
            errors.append(f"Third-party or unexpected DLL bundled: {dll.relative_to(ROOT)}")

required_docs = ["ARCHITECTURE.md", "RESEARCH.md", "TESTING.md"]
for doc in required_docs:
    if not (ROOT / "docs" / doc).exists():
        errors.append(f"Missing documentation: {doc}")

if errors:
    print("Package verification failed:", file=sys.stderr)
    for error in errors:
        print(f"  - {error}", file=sys.stderr)
    sys.exit(1)

print("PASS: XML, metadata, load folders, assets, and package hygiene")
