#!/usr/bin/env python3
"""Update release metadata files for the Jellyfin plugin."""

from __future__ import annotations

import argparse
import json
import os
import re
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def update_directory_build_props(version: str) -> None:
    path = ROOT / "Directory.Build.props"
    text = path.read_text(encoding="utf-8")
    for element in ("Version", "AssemblyVersion", "FileVersion"):
        text, count = re.subn(
            rf"<{element}>[^<]+</{element}>",
            f"<{element}>{version}</{element}>",
            text,
            count=1)
        if count != 1:
            raise RuntimeError(f"{element} element not found in {path}")

    path.write_text(text, encoding="utf-8")


def update_build_yaml(version: str) -> None:
    path = ROOT / "build.yaml"
    lines = path.read_text(encoding="utf-8").splitlines()
    version_updated = False
    changelog_updated = False

    for index, line in enumerate(lines):
        match = re.match(r"^(\s*version\s*:\s*).*$", line)
        if match:
            lines[index] = f'{match.group(1)}"{version}"'
            version_updated = True
            continue

        if re.match(r"^\s*changelog\s*:\s*>\s*$", line):
            next_index = index + 1
            while next_index < len(lines) and lines[next_index].startswith("  "):
                del lines[next_index]

            lines.insert(next_index, f"  Release v{version}")
            changelog_updated = True
            break

    if not version_updated:
        raise RuntimeError(f"version field not found in {path}")

    if not changelog_updated:
        raise RuntimeError(f"changelog block not found in {path}")

    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def update_manifest(version: str, checksum: str) -> None:
    path = ROOT / "manifest.json"
    repository = os.environ.get("GITHUB_REPOSITORY", "Entree3k/Network-Studio-Images")
    timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    plugin = {
        "guid": os.environ.get("PLUGIN_GUID", "a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
        "name": os.environ.get("PLUGIN_NAME", "Network Images"),
        "description": os.environ.get(
            "DESCRIPTION",
            "Provides network and studio images from a configurable GitHub repository. Matches studios by TMDB provider ID first, then falls back to name matching."),
        "overview": os.environ.get("OVERVIEW", "Download network/studio artwork from a custom repository"),
        "owner": os.environ.get("OWNER", "Entree3k"),
        "category": "Metadata",
        "imageUrl": f"https://raw.githubusercontent.com/{repository}/refs/heads/main/Assets/logo.png",
        "versions": [
            {
                "version": version,
                "changelog": f"Release v{version}",
                "targetAbi": os.environ.get("TARGET_ABI", "10.11.0.0"),
                "sourceUrl": f"https://github.com/{repository}/releases/download/v{version}/network-images-{version}.zip",
                "checksum": checksum,
                "timestamp": timestamp
            }
        ]
    }

    path.write_text(json.dumps([plugin], indent=2) + "\n", encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--version", required=True)
    parser.add_argument("--checksum")
    parser.add_argument("--metadata-only", action="store_true")
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    update_directory_build_props(args.version)
    update_build_yaml(args.version)

    if args.metadata_only:
        return

    if not args.checksum:
        raise RuntimeError("--checksum is required unless --metadata-only is used")

    update_manifest(args.version, args.checksum)


if __name__ == "__main__":
    main()
