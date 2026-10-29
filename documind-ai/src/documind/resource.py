"""Locate bundled read-only resources (works from source and from a PyInstaller build)."""

from __future__ import annotations

import sys
from pathlib import Path


def resource_dir() -> Path:
    base = getattr(sys, "_MEIPASS", None)
    return (Path(base) / "documind" / "resources") if base else Path(__file__).parent / "resources"


def resource_path(*parts: str) -> Path:
    return resource_dir().joinpath(*parts)
